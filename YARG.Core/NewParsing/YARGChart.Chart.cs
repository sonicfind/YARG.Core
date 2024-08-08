using MoonscraperChartEditor.Song;
using System;
using System.Collections.Generic;
using System.IO;
using YARG.Core.Chart;
using YARG.Core.IO;
using YARG.Core.IO.Ini;
using YARG.Core.Logging;
using YARG.Core.Song;

namespace YARG.Core.NewParsing
{
    public partial class YARGChart
    {
        private const string SOLO = "solo";
        private const string SOLOEND = "soloend";
        private const string SECTION = "section ";
        private const string LYRIC = "lyric ";
        private const string PHRASE_START = "phrase_start";
        private const string PHRASE_END = "phrase_end";

        public static YARGChart LoadChart(string chartPath, HashSet<Instrument>? activeTracks)
        {
            var iniPath = Path.Combine(Path.GetDirectoryName(chartPath), "song.ini");

            IniModifierCollection modifiers;
            var drumsType = DrumsType.Any;
            if (File.Exists(iniPath))
            {
                modifiers = SongIniHandler.ReadSongIniFile(iniPath);
                if (modifiers.Extract("five_lane_drums", out bool fiveLane))
                {
                    drumsType = fiveLane ? DrumsType.FiveLane : DrumsType.FourOrPro;
                }

                if (modifiers.Extract("proDrums", out bool proDrums) && drumsType != DrumsType.FiveLane)
                {
                    // We don't want to just immediately set the value to one or the other
                    // on the chance that we still need to test for FiveLane.
                    // We just know what the .ini explicitly tells us it *isn't*
                    if (proDrums)
                    {
                        drumsType -= DrumsType.FourLane;
                    }
                    else
                    {
                        drumsType -= DrumsType.ProDrums;
                    }
                }
            }
            else
            {
                modifiers = new IniModifierCollection();
            }

            using var bytes = FixedArray.LoadFile(chartPath);
            var chart = LoadChart(bytes, in SongMetadata.Default, in LoaderSettings.Default, modifiers, drumsType, activeTracks);
            if (!modifiers.Extract("hopo_frequency", out chart.Settings.HopoThreshold) || chart.Settings.HopoThreshold <= 0)
            {
                if (modifiers.Extract("eighthnote_hopo", out bool eighthNoteHopo))
                {
                    chart.Settings.HopoThreshold = chart.Sync.Tickrate / (eighthNoteHopo ? 2 : 3);
                }
                else if (modifiers.Extract("hopofreq", out long hopoFreq))
                {
                    int denominator = hopoFreq switch
                    {
                        0 => 24,
                        1 => 16,
                        2 => 12,
                        3 => 8,
                        4 => 6,
                        5 => 4,
                        _ => throw new NotImplementedException($"Unhandled hopofreq value {hopoFreq}!")
                    };
                    chart.Settings.HopoThreshold = 4 * chart.Sync.Tickrate / denominator;
                }
                else
                {
                    chart.Settings.HopoThreshold = chart.Sync.Tickrate / 3;
                }

                // With a 192 resolution, .chart has a HOPO threshold of 65 ticks, not 64,
                // so we need to scale this factor to different resolutions (480 res = 162.5 threshold).
                // Why?... idk, but I hate it.
                const float DEFAULT_RESOLUTION = 192;
                chart.Settings.HopoThreshold += (long) (chart.Sync.Tickrate / DEFAULT_RESOLUTION);
            }

            // .chart defaults to no cutting off sustains whatsoever if the ini does not define the value.
            // Since a failed `TryGet` sets the value to zero, we would need no additional work
            modifiers.Extract("sustain_cutoff_threshold", out chart.Settings.SustainCutoffThreshold);
            return chart;
        }

        public static YARGChart LoadChart(FixedArray<byte> file, in SongMetadata metadata, in LoaderSettings settings, IniModifierCollection? modifiers, DrumsType drumsInChart, HashSet<Instrument>? activeTracks)
        {
            YARGChart chart;
            if (YARGTextReader.TryUTF8(file, out var byteContainer))
            {
                chart = Initialize_Chart(ref byteContainer, in metadata, in settings, modifiers);
                LoadTracks_Chart(chart, ref byteContainer, drumsInChart, activeTracks);
            }
            else
            {
                using var chars = YARGTextReader.TryUTF16Cast(file);
                if (chars != null)
                {
                    var charContainer = YARGTextReader.CreateUTF16Container(chars);
                    chart = Initialize_Chart(ref charContainer, in metadata, in settings, modifiers);
                    LoadTracks_Chart(chart, ref charContainer, drumsInChart, activeTracks);
                }
                else
                {
                    using var ints = YARGTextReader.CastUTF32(file);
                    var intContainer = YARGTextReader.CreateUTF32Container(ints);
                    chart = Initialize_Chart(ref intContainer, in metadata, in settings, modifiers);
                }
            }
            return chart;
        }

        private static YARGChart Initialize_Chart<TChar>(ref YARGTextContainer<TChar> container, in SongMetadata metadata, in LoaderSettings settings, IniModifierCollection? miscellaneous)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            const long DEFAULT_TICKRATE = 192;
            if (!YARGChartFileReader.ValidateTrack(ref container, YARGChartFileReader.HEADERTRACK))
            {
                throw new Exception("[Song] track expected at the start of the file");
            }

            var modifiers = YARGChartFileReader.ExtractModifiers(ref container);
            if (!modifiers.Extract("Resolution", out long tickrate) || tickrate <= 0)
            {
                tickrate = DEFAULT_TICKRATE;
            }

            var chart = new YARGChart(tickrate, in metadata, in settings, miscellaneous);
            if (chart.Miscellaneous != null)
            {
                chart.Miscellaneous.Union(modifiers);
                SongMetadata.FillFromIni(ref chart.Metadata, chart.Miscellaneous);
            }

            if (YARGChartFileReader.ValidateTrack(ref container, YARGChartFileReader.SYNCTRACK))
            {
                DotChartEvent ev = default;
                while (YARGChartFileReader.TryParseEvent(ref container, ref ev))
                {
                    unsafe
                    {
                        switch (ev.Type)
                        {
                            case ChartEventType.Bpm:
                                chart.Sync.TempoMarkers.GetLastOrAppend(ev.Position)->MicrosPerQuarter = YARGChartFileReader.ExtractMicrosPerQuarter(ref container);
                                break;
                            case ChartEventType.Anchor:
                                if (ev.Position > 0)
                                {
                                    chart.Sync.TempoMarkers.GetLastOrAppend(ev.Position)->Anchor = YARGChartFileReader.ExtractWithWhitespace<TChar, long>(ref container);
                                }
                                break;
                            case ChartEventType.Time_Sig:
                                chart.Sync.TimeSigs.AppendOrUpdate(ev.Position, YARGChartFileReader.ExtractTimeSig(ref container));
                                break;
                        }
                    }
                }
                YARGChartFinalizer.FinalizeAnchors(chart.Sync);
            }
            DualTime.SetTruncationLimit(settings, 1);
            return chart;
        }

        private static void LoadTracks_Chart<TChar>(YARGChart chart, ref YARGTextContainer<TChar> container, DrumsType drumsInChart, HashSet<Instrument>? activeTracks)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            if (activeTracks == null || activeTracks.Contains(Instrument.Vocals))
            {
                chart.LeadVocals = new VocalTrack2(1);
            }

            InstrumentTrack2<DifficultyTrack2<UnknownLaneDrums>>? unknownDrums = null;
            while (YARGChartFileReader.IsStartOfTrack(in container))
            {
                if (!SelectTrack_Chart(ref container, chart, ref drumsInChart, activeTracks, ref unknownDrums))
                {
                    YARGChartFileReader.SkipToNextTrack(ref container);
                }
            }

            if (unknownDrums != null && !unknownDrums.IsEmpty())
            {
                if ((drumsInChart & DrumsType.FourLane) == DrumsType.FourLane)
                {
                    chart.FourLaneDrums ??= new InstrumentTrack2<DifficultyTrack2<FourLaneDrums>>();
                    unknownDrums.ConvertToFourLane(chart.FourLaneDrums, false);
                }
                else if ((drumsInChart & DrumsType.ProDrums) == DrumsType.ProDrums)
                {
                    chart.FourLaneDrums ??= new InstrumentTrack2<DifficultyTrack2<FourLaneDrums>>();
                    unknownDrums.ConvertToFourLane(chart.FourLaneDrums, true);
                }
                else
                {
                    chart.FiveLaneDrums ??= new InstrumentTrack2<DifficultyTrack2<FiveLaneDrums>>();
                    unknownDrums.ConvertToFiveLane(chart.FiveLaneDrums);
                }
            }
            YARGChartFinalizer.FinalizeBeats(chart);
        }

        private static bool LoadEventsTrack_Chart<TChar>(ref YARGTextContainer<TChar> container, YARGChart chart)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            if (!chart.Globals.IsEmpty() || !chart.Sections.IsEmpty() || (chart.LeadVocals != null && !chart.LeadVocals.IsEmpty()))
            {
                YargLogger.LogInfo("[Events] track appears multiple times. Not parsing repeats...");
                return false;
            }

            // Used to lesson the impact of the ticks-seconds search algorithm as the the position
            // gets larger by tracking the previous position.
            int tempoIndex = 0;
            var phrase = DualTime.Inactive;

            DotChartEvent ev = default;
            DualTime position = default;
            while (YARGChartFileReader.TryParseEvent(ref container, ref ev))
            {
                if (ev.Type == ChartEventType.Text)
                {
                    position.Ticks = ev.Position;
                    position.Seconds = chart.Sync.ConvertToSeconds(ev.Position, ref tempoIndex);

                    string str = YARGTextReader.ExtractText(ref container, true);
                    if (str.StartsWith(SECTION))
                    {
                        chart.Sections.AppendOrUpdate(in position, str[SECTION.Length..]);
                    }
                    else if (str.StartsWith(LYRIC))
                    {
                        if (chart.LeadVocals != null)
                        {
                            chart.LeadVocals[0].Lyrics.AppendOrUpdate(in position, str[LYRIC.Length..]);
                        }
                    }
                    else if (str == PHRASE_START)
                    {
                        if (chart.LeadVocals != null)
                        {
                            if (phrase.Ticks >= 0 && position.Ticks > phrase.Ticks)
                            {
                                chart.LeadVocals.VocalPhrases_1.Append(in phrase, position - phrase);
                            }
                            phrase = position;
                        }
                    }
                    else if (str == PHRASE_END)
                    {
                        // No need for LeadVocals null check
                        if (phrase.Ticks >= 0)
                        {
                            if (position.Ticks > phrase.Ticks)
                            {
                                chart.LeadVocals!.VocalPhrases_1.Append(in phrase, position - phrase);
                            }
                            phrase.Ticks = -1;
                        }
                    }
                    else
                    {
                        chart.Globals.GetLastOrAppend(in position).Add(str);
                    }
                }
            }

            if (chart.LeadVocals != null && chart.LeadVocals.IsEmpty())
            {
                chart.LeadVocals = null;
            }
            return true;
        }

        private static bool SelectTrack_Chart<TChar>(ref YARGTextContainer<TChar> container, YARGChart chart, ref DrumsType drumsInChart, HashSet<Instrument>? activeTracks, ref InstrumentTrack2<DifficultyTrack2<UnknownLaneDrums>>? unknownDrums)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            if (YARGChartFileReader.ValidateTrack(ref container, YARGChartFileReader.EVENTTRACK))
            {
                return LoadEventsTrack_Chart(ref container, chart);
            }

            if (!YARGChartFileReader.ValidateInstrument(ref container, out var instrument, out var difficulty))
            {
                return false;
            }

            // The note track label for drums will only return the
            // four lanes enum value
            //
            // We expect to the `activeTracks` parameter to hold the matching
            // drums instrument value to `drumsInChart` if drums are active
            if (instrument == Instrument.FourLaneDrums)
            {
                switch (drumsInChart)
                {
                case DrumsType.ProDrums: instrument = Instrument.ProDrums; break;
                case DrumsType.FiveLane: instrument = Instrument.FiveLaneDrums; break;
                case DrumsType.FourLane: break;
                default:
                    if (activeTracks == null)
                    {
                        unsafe
                        {
                            UnknownLaneDrums.DrumType = drumsInChart;
                            bool result = LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref unknownDrums);
                            drumsInChart = UnknownLaneDrums.DrumType;
                            return result;
                        }
                    }
                    break;
                }
            }

            if (activeTracks != null && !activeTracks.Contains(instrument))
            {
                return false;
            }

            unsafe
            {
                return instrument switch
                {
                    Instrument.FiveFretGuitar =>     LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref chart.FiveFretGuitar),
                    Instrument.FiveFretBass =>       LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref chart.FiveFretBass),
                    Instrument.FiveFretRhythm =>     LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref chart.FiveFretRhythm),
                    Instrument.FiveFretCoopGuitar => LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref chart.FiveFretCoopGuitar),
                    Instrument.SixFretGuitar =>      LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref chart.SixFretGuitar),
                    Instrument.SixFretBass =>        LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref chart.SixFretBass),
                    Instrument.SixFretRhythm =>      LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref chart.SixFretRhythm),
                    Instrument.SixFretCoopGuitar =>  LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref chart.SixFretCoopGuitar),
                    Instrument.FourLaneDrums or
                    Instrument.ProDrums =>           LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref chart.FourLaneDrums),
                    Instrument.FiveLaneDrums =>      LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref chart.FiveLaneDrums),
                    Instrument.Keys =>               LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref chart.Keys),
                    _ => false,
                };
            }
        }

        private enum SpecialPhraseType
        {
            FaceOff_Player1 = 0,
            FaceOff_Player2 = 1,
            StarPower = 2,
            BRE = 64,
            Tremolo = 65,
            Trill = 66,
        }

        private static unsafe bool LoadInstrumentTrack_Chart<TChar, TNote>(ref YARGTextContainer<TChar> container, SyncTrack2 sync, Difficulty difficulty, ref InstrumentTrack2<DifficultyTrack2<TNote>>? track)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
            where TNote : unmanaged, IInstrumentNote, IDotChartLoadable
        {
            track ??= new InstrumentTrack2<DifficultyTrack2<TNote>>();

            ref var difficultyTrack = ref track[difficulty];
            if (difficultyTrack != null)
            {
                return false;
            }

            difficultyTrack = new DifficultyTrack2<TNote>();
            difficultyTrack.Notes.Capacity = 5000;

            // Used to lesson the impact of the ticks-seconds search algorithm as the the position
            // gets larger by tracking the previous position.
            int tempoIndex = 0;

             var soloQueue = stackalloc DualTime[2] { DualTime.Inactive, DualTime.Inactive };

            DotChartEvent ev = default;
            DualTime position = default;
            DualTime duration = default;
            void AddSpecialPhrase(YARGNativeSortedList<DualTime, DualTime> phrases)
            {
                if (phrases.Count > 0)
                {
                    ref var last = ref phrases.Data[phrases.Count - 1];
                    if (last.Key + last.Value > position)
                    {
                        last.Value = position - last.Key;
                    }
                }
                phrases.Append(in position, duration);
            }

            while (YARGChartFileReader.TryParseEvent(ref container, ref ev))
            {
                position.Ticks = ev.Position;
                position.Seconds = sync.ConvertToSeconds(ev.Position, ref tempoIndex);
                switch (ev.Type)
                {
                    case ChartEventType.Note:
                        {
                            var note = difficultyTrack.Notes.GetLastOrAppend(in position);

                            int lane = YARGChartFileReader.ExtractWithWhitespace<TChar, int>(ref container);
                            long tickDuration = YARGChartFileReader.ExtractWithWhitespace<TChar, long>(ref container);
                            if (tickDuration == 0)
                            {
                                tickDuration = 1;
                            }

                            duration.Ticks = tickDuration;
                            duration.Seconds = sync.ConvertToSeconds(position.Ticks + tickDuration, tempoIndex) - position.Seconds;
                            if (!note->SetFromDotChart(lane, in duration))
                            {
                                if (note->GetNumActiveLanes() == 0)
                                {
                                    difficultyTrack.Notes.Pop();
                                }
                            }
                            break;
                        }
                    case ChartEventType.Special:
                        {
                            var type = (SpecialPhraseType)YARGChartFileReader.ExtractWithWhitespace<TChar, int>(ref container);
                            long tickDuration = YARGChartFileReader.ExtractWithWhitespace<TChar, long>(ref container);
                            if (tickDuration > 0)
                            {
                                duration.Ticks = tickDuration;
                                duration.Seconds = sync.ConvertToSeconds(position.Ticks + tickDuration, tempoIndex) - position.Seconds;
                                switch (type)
                                {
                                    case SpecialPhraseType.FaceOff_Player1: AddSpecialPhrase(difficultyTrack.Faceoff_Player1); break;
                                    case SpecialPhraseType.FaceOff_Player2: AddSpecialPhrase(difficultyTrack.Faceoff_Player2); break;
                                    case SpecialPhraseType.StarPower:       AddSpecialPhrase(difficultyTrack.Overdrives); break;
                                    case SpecialPhraseType.BRE:             AddSpecialPhrase(difficultyTrack.BREs); break;
                                    case SpecialPhraseType.Tremolo:         AddSpecialPhrase(difficultyTrack.Tremolos); break;
                                    case SpecialPhraseType.Trill:           AddSpecialPhrase(difficultyTrack.Trills); break;
                                }
                            }
                            break;
                        }
                    case ChartEventType.Text:
                        string str = YARGTextReader.ExtractText(ref container, true);
                        if (str == SOLOEND)
                        {
                            if (soloQueue[0].Ticks != -1)
                            {
                                // .chart handles solo phrases with *inclusive ends*, so we have to add one tick.
                                // The only exception will be if another solo starts on the same exact tick.
                                if (soloQueue[1] == position)
                                {
                                    difficultyTrack.Soloes.Append(in soloQueue[0], soloQueue[1] - soloQueue[0]);
                                    soloQueue[0] = soloQueue[1];
                                    soloQueue[1] = DualTime.Inactive;
                                }
                                else
                                {
                                    ++position.Ticks;
                                    position.Seconds = sync.ConvertToSeconds(position.Ticks, tempoIndex);
                                    difficultyTrack.Soloes.Append(in soloQueue[0], position - soloQueue[0]);
                                }
                            }
                        }
                        else if (str == SOLO)
                        {
                            unsafe
                            {
                                bool useBackup = soloQueue[0].Ticks != -1;
                                soloQueue[*(byte*)&useBackup] = position;
                            }
                        }
                        else
                        {
                            difficultyTrack.Events.GetLastOrAppend(in position).Add(str);
                        }
                        break;
                }
            }

            difficultyTrack.TrimExcess();
            return true;
        }
    }
}

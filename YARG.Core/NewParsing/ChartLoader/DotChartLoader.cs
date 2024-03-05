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
    public static partial class YARGDotChartLoader
    {
        private const string SOLO = "solo";
        private const string SOLOEND = "soloend";
        private const string SECTION = "section ";
        private const string LYRIC = "lyric ";
        private const string PHRASE_START = "phrase_start";
        private const string PHRASE_END = "phrase_end ";

        public static YARGChart Load(string chartPath, HashSet<Instrument>? activeTracks)
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
            var chart = Load(bytes, in SongMetadata.Default, in LoaderSettings.Default, modifiers, drumsType, activeTracks);
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

        public static YARGChart Load(FixedArray<byte> file, in SongMetadata metadata, in LoaderSettings settings, IniModifierCollection? modifiers, DrumsType drumsInChart, HashSet<Instrument>? activeTracks)
        {
            YARGChart chart;
            if (YARGTextReader.TryUTF8(file, out var byteContainer))
            {
                chart = Initialize(ref byteContainer, in metadata, in settings, modifiers);
                LoadTracks(chart, ref byteContainer, drumsInChart, activeTracks);
            }
            else
            {
                using var chars = YARGTextReader.TryUTF16Cast(file);
                if (chars != null)
                {
                    var charContainer = YARGTextReader.CreateUTF16Container(chars);
                    chart = Initialize(ref charContainer, in metadata, in settings, modifiers);
                    LoadTracks(chart, ref charContainer, drumsInChart, activeTracks);
                }
                else
                {
                    using var ints = YARGTextReader.CastUTF32(file);
                    var intContainer = YARGTextReader.CreateUTF32Container(ints);
                    chart = Initialize(ref intContainer, in metadata, in settings, modifiers);
                }
            }
            return chart;
        }

        private static YARGChart Initialize<TChar>(ref YARGTextContainer<TChar> container, in SongMetadata metadata, in LoaderSettings settings, IniModifierCollection? miscellaneous)
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
                    switch (ev.Type)
                    {
                        case ChartEventType.Bpm:
                            chart.Sync.TempoMarkers.GetLastOrAppend(ev.Position).MicrosPerQuarter = YARGChartFileReader.ExtractMicrosPerQuarter(ref container);
                            break;
                        case ChartEventType.Anchor:
                            chart.Sync.TempoMarkers.GetLastOrAppend(ev.Position).Anchor = ev.Position > 0 ? YARGChartFileReader.ExtractWithWhitespace<TChar, long>(ref container) : 0;
                            break;
                        case ChartEventType.Time_Sig:
                            chart.Sync.TimeSigs.GetLastOrAppend(ev.Position) = YARGChartFileReader.ExtractTimeSig(ref container);
                            break;
                    }
                    YARGTextReader.GotoNextLine(ref container);
                }
                YARGChartFinalizer.FinalizeAnchors(chart.Sync);
            }
            DualTime.SetTruncationLimit(settings, 1);
            return chart;
        }

        private static void LoadTracks<TChar>(YARGChart chart, ref YARGTextContainer<TChar> container, DrumsType drumsInChart, HashSet<Instrument>? activeTracks)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            if (activeTracks == null || activeTracks.Contains(Instrument.Vocals))
            {
                chart.LeadVocals = new VocalTrack2(1);
            }

            BasicInstrumentTrack2<ProDrumNote2<FiveLane>>? unknownDrums = null;
            while (YARGChartFileReader.IsStartOfTrack(in container))
            {
                if (YARGChartFileReader.ValidateTrack(ref container, YARGChartFileReader.EVENTTRACK)
                    ? !LoadEventsTrack(ref container, chart)
                    : !SelectChartTrack(ref container, chart, ref drumsInChart, activeTracks, ref unknownDrums))
                {
                    if (YARGTextReader.SkipLinesUntil(ref container, TextConstants<TChar>.CLOSE_BRACE))
                    {
                        YARGTextReader.GotoNextLine(ref container);
                    }
                }
            }

            if (unknownDrums != null && unknownDrums.IsOccupied())
            {
                if ((drumsInChart & DrumsType.FourLane) == DrumsType.FourLane)
                {
                    chart.FourLaneDrums ??= new BasicInstrumentTrack2<DrumNote2<FourLane>>();
                    UnknownDrumTrackConverter.ConvertTo<DrumNote2<FourLane>, FourLane>(chart.FourLaneDrums, unknownDrums);
                }
                else if ((drumsInChart & DrumsType.ProDrums) == DrumsType.ProDrums)
                {
                    chart.ProDrums ??= new BasicInstrumentTrack2<ProDrumNote2<FourLane>>();
                    UnknownDrumTrackConverter.ConvertTo<ProDrumNote2<FourLane>, FourLane>(chart.ProDrums, unknownDrums);
                }
                else
                {
                    chart.FiveLaneDrums ??= new BasicInstrumentTrack2<DrumNote2<FiveLane>>();
                    UnknownDrumTrackConverter.ConvertTo<DrumNote2<FiveLane>, FiveLane>(chart.FiveLaneDrums, unknownDrums);
                }
            }
            YARGChartFinalizer.FinalizeBeats(chart);
        }

        private static bool LoadEventsTrack<TChar>(ref YARGTextContainer<TChar> container, YARGChart chart)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            if (chart.Events.IsOccupied() || (chart.LeadVocals != null && chart.LeadVocals.IsOccupied()))
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

                    string str = YARGTextReader.ExtractText(ref container, false);
                    if (str.StartsWith(SECTION))
                    {
                        chart.Events.Sections.GetLastOrAppend(position) = str[SECTION.Length..];
                    }
                    else if (str.StartsWith(LYRIC))
                    {
                        if (chart.LeadVocals != null)
                        {
                            chart.LeadVocals[0].GetLastOrAppend(position).Lyric = str[LYRIC.Length..];
                        }
                    }
                    else if (str == PHRASE_START)
                    {
                        if (chart.LeadVocals != null)
                        {
                            if (phrase.Ticks >= 0)
                            {
                                chart.LeadVocals.SpecialPhrases[phrase].TryAdd(SpecialPhraseType.LyricLine, new(position - phrase));
                            }
                            phrase = position;
                        }
                    }
                    else if (str == PHRASE_END)
                    {
                        // No need for doVocals check
                        if (phrase.Ticks >= 0)
                        {
                            chart.LeadVocals!.SpecialPhrases[phrase].TryAdd(SpecialPhraseType.LyricLine, new(position - phrase));
                            phrase.Ticks = -1;
                        }
                    }
                    else
                    {
                        chart.Events.Globals.GetLastOrAppend(position).Add(str);
                    }
                }
                YARGTextReader.GotoNextLine(ref container);
            }

            if (chart.LeadVocals != null && !chart.LeadVocals.IsOccupied())
            {
                chart.LeadVocals = null;
            }
            return true;
        }

        private static bool SelectChartTrack<TChar>(ref YARGTextContainer<TChar> container, YARGChart chart, ref DrumsType drumsInChart, HashSet<Instrument>? activeTracks, ref BasicInstrumentTrack2<ProDrumNote2<FiveLane>>? unknownDrums)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
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
                            _unknownDrumType = drumsInChart;
                            bool result = LoadChartTrack(ref container, chart.Sync, difficulty, ref unknownDrums, &Set);
                            drumsInChart = _unknownDrumType;
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
                    Instrument.FiveFretGuitar =>     LoadChartTrack(ref container, chart.Sync, difficulty, ref chart.FiveFretGuitar, &Set),
                    Instrument.FiveFretBass =>       LoadChartTrack(ref container, chart.Sync, difficulty, ref chart.FiveFretBass, &Set),
                    Instrument.FiveFretRhythm =>     LoadChartTrack(ref container, chart.Sync, difficulty, ref chart.FiveFretRhythm, &Set),
                    Instrument.FiveFretCoopGuitar => LoadChartTrack(ref container, chart.Sync, difficulty, ref chart.FiveFretCoopGuitar, &Set),
                    Instrument.SixFretGuitar =>      LoadChartTrack(ref container, chart.Sync, difficulty, ref chart.SixFretGuitar, &Set),
                    Instrument.SixFretBass =>        LoadChartTrack(ref container, chart.Sync, difficulty, ref chart.SixFretBass, &Set),
                    Instrument.SixFretRhythm =>      LoadChartTrack(ref container, chart.Sync, difficulty, ref chart.SixFretRhythm, &Set),
                    Instrument.SixFretCoopGuitar =>  LoadChartTrack(ref container, chart.Sync, difficulty, ref chart.SixFretCoopGuitar, &Set),
                    Instrument.FourLaneDrums =>      LoadChartTrack(ref container, chart.Sync, difficulty, ref chart.FourLaneDrums, &Set),
                    Instrument.ProDrums =>           LoadChartTrack(ref container, chart.Sync, difficulty, ref chart.ProDrums, &Set),
                    Instrument.FiveLaneDrums =>      LoadChartTrack(ref container, chart.Sync, difficulty, ref chart.FiveLaneDrums, &Set),
                    Instrument.Keys =>               LoadChartTrack(ref container, chart.Sync, difficulty, ref chart.Keys, &Set),
                    _ => false,
                };
            }
        }

        private static unsafe bool LoadChartTrack<TChar, TNote>(ref YARGTextContainer<TChar> container, SyncTrack2 sync, Difficulty difficulty, ref BasicInstrumentTrack2<TNote>? track, delegate*<ref TNote, int, DualTime, bool> loader)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
            where TNote : unmanaged, IInstrumentNote
        {
            track ??= new BasicInstrumentTrack2<TNote>();

            ref var difficultyTrack = ref track[difficulty];
            if (difficultyTrack != null)
            {
                return false;
            }

            difficultyTrack = new DifficultyTrack2<TNote>(5000);

            // Used to lesson the impact of the ticks-seconds search algorithm as the the position
            // gets larger by tracking the previous position.
            int tempoIndex = 0;

             var soloQueue = stackalloc DualTime[2] { DualTime.Inactive, DualTime.Inactive };

            DotChartEvent ev = default;
            DualTime position = default;
            DualTime duration = default;
            while (YARGChartFileReader.TryParseEvent(ref container, ref ev))
            {
                position.Ticks = ev.Position;
                position.Seconds = sync.ConvertToSeconds(ev.Position, ref tempoIndex);
                switch (ev.Type)
                {
                    case ChartEventType.Note:
                        {
                            ref var note = ref difficultyTrack.Notes.GetLastOrAppend(position);

                            int lane = YARGChartFileReader.ExtractWithWhitespace<TChar, int>(ref container);
                            long tickDuration = YARGChartFileReader.ExtractWithWhitespace<TChar, long>(ref container);

                            duration.Ticks = tickDuration;
                            duration.Seconds = sync.ConvertToSeconds(tickDuration, tempoIndex);
                            if (!loader(ref note, lane, duration))
                            {
                                if (note.GetNumActiveLanes() == 0)
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
                            switch (type)
                            {
                                case SpecialPhraseType.FaceOff_Player1:
                                case SpecialPhraseType.FaceOff_Player2:
                                case SpecialPhraseType.StarPower:
                                case SpecialPhraseType.BRE:
                                case SpecialPhraseType.Tremolo:
                                case SpecialPhraseType.Trill:
                                    duration.Ticks = tickDuration;
                                    duration.Seconds = sync.ConvertToSeconds(tickDuration, tempoIndex);
                                    difficultyTrack.SpecialPhrases.GetLastOrAppend(position).TryAdd(type, new SpecialPhraseInfo(duration));
                                    break;
                            }
                            break;
                        }
                    case ChartEventType.Text:
                        string str = YARGTextReader.ExtractText(ref container, false);
                        if (str.StartsWith(SOLOEND))
                        {
                            if (soloQueue[0].Ticks != -1)
                            {
                                // .chart handles solo phrases with *inclusive ends*, so we have add one tick
                                var soloEnd = position;
                                ++soloEnd.Ticks;
                                soloEnd.Seconds = sync.ConvertToSeconds(soloEnd.Ticks, tempoIndex);

                                difficultyTrack.SpecialPhrases[soloQueue[0]].TryAdd(SpecialPhraseType.Solo, new SpecialPhraseInfo(soloEnd - soloQueue[0]));
                                soloQueue[0] = soloQueue[1] == position ? soloQueue[1] : DualTime.Inactive;
                                soloQueue[1] = DualTime.Inactive;
                            }
                        }
                        else if (str.StartsWith(SOLO))
                        {
                            unsafe
                            {
                                bool useBackup = soloQueue[0].Ticks != -1;
                                soloQueue[*(byte*)&useBackup] = position;
                            }
                        }
                        else
                        {
                            difficultyTrack.Events.GetLastOrAppend(position).Add(str);
                        }
                        break;
                }
                YARGTextReader.GotoNextLine(ref container);
            }

            difficultyTrack.TrimExcess();
            return true;
        }
    }
}

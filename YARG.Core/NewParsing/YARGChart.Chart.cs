using System;
using System.Collections.Generic;
using System.IO;
using YARG.Core.Chart;
using YARG.Core.IO;
using YARG.Core.IO.Disposables;
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

        public static YARGChart LoadChart(FileInfo chartInfo, HashSet<Instrument>? activeTracks)
        {
            var iniInfo = new FileInfo(Path.Combine(chartInfo.DirectoryName, "song.ini"));

            SongMetadata metadata;
            ParseSettings settings;
            if (iniInfo.Exists)
            {
                var modifiers = SongIniHandler.ReadSongIniFile(iniInfo);
                metadata = new SongMetadata(modifiers, string.Empty);

                var drums = DrumsType.Unknown;
                if (modifiers.TryGet("five_lane_drums", out bool fiveLane))
                {
                    drums = fiveLane ? DrumsType.FiveLane : DrumsType.Unknown;
                }
                settings = new ParseSettings(modifiers, drums);
            }
            else
            {
                metadata = SongMetadata.Default;
                settings = ParseSettings.Default;
            }

            using var bytes = MemoryMappedArray.Load(chartInfo);
            return LoadChart(bytes, metadata, settings, activeTracks);
        }

        public static YARGChart LoadChart(FixedArray<byte> file, in SongMetadata metadata, in ParseSettings settings, HashSet<Instrument>? activeTracks)
        {
            if (YARGTextReader.IsUTF8(file, out var byteContainer))
            {
                return Process_Chart(ref byteContainer, in metadata, in settings, activeTracks);
            }

            using var chars = YARGTextReader.ConvertToUTF16(file, out var charContainer);
            if (chars != null)
            {
                return Process_Chart(ref charContainer, in metadata, in settings, activeTracks);
            }

            using var ints = YARGTextReader.ConvertToUTF32(file, out var intContainer);
            return Process_Chart(ref intContainer, in metadata, in settings, activeTracks);
        }

        private static YARGChart Process_Chart<TChar>(ref YARGTextContainer<TChar> container, in SongMetadata metadata, in ParseSettings settings, HashSet<Instrument>? activeTracks)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            uint tickrate = ParseTickrate_Chart(ref container);
            var sync = ReadSynctrack_Chart(ref container, tickrate);
            var chart = new YARGChart(sync, metadata, settings);
            if (activeTracks == null || activeTracks.Contains(Instrument.Vocals))
            {
                chart.LeadVocals = new VocalTrack2(1);
            }

            InstrumentTrack2<DifficultyTrack2<UnknownLaneDrums>>? unknownDrums = null;
            if (activeTracks == null && settings.DrumsType is DrumsType.Unknown)
            {
                unknownDrums = new InstrumentTrack2<DifficultyTrack2<UnknownLaneDrums>>();
                UnknownLaneDrums.DrumType = settings.DrumsType;
            }

            DualTime.SetTruncationLimit(settings, 1);
            while (YARGChartFileReader.IsStartOfTrack(in container))
            {
                if (!SelectTrack_Chart(ref container, chart, activeTracks, unknownDrums))
                {
                    if (YARGTextReader.SkipLinesUntil(ref container, TextConstants<TChar>.CLOSE_BRACE))
                    {
                        YARGTextReader.GotoNextLine(ref container);
                    }
                }
            }

            if (unknownDrums != null && !unknownDrums.IsEmpty())
            {
                switch (UnknownLaneDrums.DrumType)
                {
                    case DrumsType.ProDrums:
                        chart.FourLaneDrums ??= new InstrumentTrack2<DifficultyTrack2<FourLaneDrums>>();
                        unknownDrums.ConvertToFourLane(chart.FourLaneDrums, true);
                        chart.Settings.DrumsType = DrumsType.ProDrums;
                        break;
                    case DrumsType.FiveLane:
                        chart.FiveLaneDrums ??= new InstrumentTrack2<DifficultyTrack2<FiveLaneDrums>>();
                        unknownDrums.ConvertToFiveLane(chart.FiveLaneDrums);
                        chart.Settings.DrumsType = DrumsType.FiveLane;
                        break;
                    default:
                        chart.FourLaneDrums ??= new InstrumentTrack2<DifficultyTrack2<FourLaneDrums>>();
                        unknownDrums.ConvertToFourLane(chart.FourLaneDrums, false);
                        chart.Settings.DrumsType = DrumsType.FourLane;
                        break;
                }
            }

            FinalizeDeserialization(chart);
            return chart;
        }

        private const uint DEFAULT_TICKRATE = 192;
        private static uint ParseTickrate_Chart<TChar>(ref YARGTextContainer<TChar> container)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            uint tickrate = DEFAULT_TICKRATE;
            if (!YARGChartFileReader.ValidateTrack(ref container, YARGChartFileReader.HEADERTRACK))
            {
                return tickrate;
            }

            var modifiers = YARGChartFileReader.ExtractModifiers(ref container);
            if (modifiers.TryGetValue("Resolution", out var tickrates))
            {
                for (int i = 0; i < tickrates.Count; i++)
                {
                    unsafe
                    {
                        var mod = tickrates[0];
                        uint rate = *(uint*) mod.Buffer;

                        if (rate != 0)
                        {
                            tickrate = rate;
                            break;
                        }
                    }
                }
            }
            return tickrate;
        }

        private static unsafe SyncTrack2 ReadSynctrack_Chart<TChar>(ref YARGTextContainer<TChar> container, uint tickrate)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            var sync = new SyncTrack2(tickrate);
            if (!YARGChartFileReader.ValidateTrack(ref container, YARGChartFileReader.SYNCTRACK))
            {
                return sync;
            }

            DotChartEvent ev = default;
            while (YARGChartFileReader.TryParseEvent(ref container, ref ev))
            {
                switch (ev.Type)
                {
                    case ChartEventType.Bpm:
                        sync.TempoMarkers.GetLastOrAppend(ev.Position)->MicrosPerQuarter = YARGChartFileReader.ExtractMicrosPerQuarter(ref container);
                        break;
                    case ChartEventType.Anchor:
                        sync.TempoMarkers.GetLastOrAppend(ev.Position)->Anchor = ev.Position > 0 ? YARGTextReader.ExtractInt64AndWhitespace(ref container) : 0;
                        break;
                    case ChartEventType.Time_Sig:
                        sync.TimeSigs.AppendOrUpdate(ev.Position, YARGChartFileReader.ExtractTimeSig(ref container));
                        break;
                }
            }
            FinalizeAnchors(sync);
            return sync;
        }

        private static unsafe bool LoadEventsTrack_Chart<TChar>(ref YARGTextContainer<TChar> container, YARGChart chart)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            if (!chart.Globals.IsEmpty() || !chart.Sections.IsEmpty() || (chart.LeadVocals != null && !chart.LeadVocals.IsEmpty()))
            {
                YargLogger.LogInfo("[Events] track appears multiple times. Not parsing repeats...");
                return false;
            }

            var tempoTracker = new TempoTracker(chart.Sync);
            var ev = default(DotChartEvent);
            var position = default(DualTime);
            var phrase = DualTime.Inactive;
            while (YARGChartFileReader.TryParseEvent(ref container, ref ev))
            {
                if (ev.Type == ChartEventType.Text)
                {
                    position.Ticks = ev.Position;
                    position.Seconds = tempoTracker.Traverse(ev.Position);

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

        private static bool SelectTrack_Chart<TChar>(ref YARGTextContainer<TChar> container, YARGChart chart, HashSet<Instrument>? activeTracks, InstrumentTrack2<DifficultyTrack2<UnknownLaneDrums>>? unknownDrums)
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

            if (activeTracks == null)
            {
                if (instrument == Instrument.FourLaneDrums)
                {
                    var drumType = chart.Settings.DrumsType;
                    if (unknownDrums != null)
                    {
                        if (unknownDrums[difficulty] != null)
                        {
                            return false;
                        }

                        if (UnknownLaneDrums.DrumType is DrumsType.Unknown) unsafe
                        {
                            return LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref unknownDrums);
                        }

                        drumType = UnknownLaneDrums.DrumType;
                    }

                    if (drumType == DrumsType.ProDrums)
                    {
                        instrument = Instrument.ProDrums;
                    }
                    else if (drumType == DrumsType.FiveLane)
                    {
                        instrument = Instrument.FiveLaneDrums;
                    }
                }
            }
            else
            {
                if (instrument == Instrument.FourLaneDrums)
                {
                    if (chart.Settings.DrumsType == DrumsType.ProDrums)
                    {
                        instrument = Instrument.ProDrums;
                    }
                    else if (chart.Settings.DrumsType == DrumsType.FiveLane)
                    {
                        instrument = Instrument.FiveLaneDrums;
                    }
                }

                if (!activeTracks.Contains(instrument))
                {
                    return false;
                }
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


            var ev = default(DotChartEvent);
            var position = default(DualTime);
            var duration = default(DualTime);
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
            // Keeps tracks of soloes that start on the same tick when another solo ends
            var soloPosition = DualTime.Inactive;
            var nextSoloPosition = DualTime.Inactive;

            var tempoTracker = new TempoTracker(sync);
            while (YARGChartFileReader.TryParseEvent(ref container, ref ev))
            {
                position.Ticks = ev.Position;
                position.Seconds = tempoTracker.Traverse(ev.Position);
                switch (ev.Type)
                {
                    case ChartEventType.Note:
                        {
                            var note = difficultyTrack.Notes.GetLastOrAppend(in position);

                            int lane = YARGTextReader.ExtractInt32AndWhitespace(ref container);
                            long tickDuration = YARGTextReader.ExtractInt64AndWhitespace(ref container);
                            if (tickDuration == 0)
                            {
                                tickDuration = 1;
                            }

                            duration.Ticks = tickDuration;
                            duration.Seconds = tempoTracker.UnmovingConvert(position.Ticks + tickDuration) - position.Seconds;
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
                            var type = (SpecialPhraseType) YARGTextReader.ExtractInt32AndWhitespace(ref container);
                            long tickDuration = YARGTextReader.ExtractInt64AndWhitespace(ref container);
                            if (tickDuration > 0)
                            {
                                duration.Ticks = tickDuration;
                                duration.Seconds = tempoTracker.UnmovingConvert(position.Ticks + tickDuration) - position.Seconds;
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
                        if (str == SOLO)
                        {
                            if (soloPosition.Ticks == -1)
                            {
                                soloPosition = position;
                            }
                            else
                            {
                                nextSoloPosition = position;
                            }
                        }
                        else if (str == SOLOEND)
                        {
                            if (soloPosition.Ticks != -1)
                            {
                                // .chart handles solo phrases with *inclusive ends*, so we have to add one tick.
                                // The only exception will be if another solo starts on the same exact tick.
                                //
                                // Comparing to the current tick instead of against uint.MaxValue ensures
                                // that the we don't allow overlaps
                                if (nextSoloPosition != position)
                                {
                                    ++position.Ticks;
                                    position.Seconds = tempoTracker.UnmovingConvert(position.Ticks);
                                    difficultyTrack.Soloes.Append(in soloPosition, position - soloPosition);
                                    soloPosition = DualTime.Inactive;
                                }
                                else
                                {
                                    difficultyTrack.Soloes.Append(in soloPosition, nextSoloPosition - soloPosition);
                                    soloPosition = nextSoloPosition;
                                    nextSoloPosition = DualTime.Inactive;
                                }
                            }
                        }
                        else 
                        {
                            difficultyTrack.Events.GetLastOrAppend(in position).Add(str);
                        }
                        break;
                }
            }
            return true;
        }
    }
}

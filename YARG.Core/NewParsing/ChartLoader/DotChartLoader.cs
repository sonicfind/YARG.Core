using System;
using System.Collections.Generic;
using System.IO;
using YARG.Core.Chart;
using YARG.Core.Game;
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

            IniSection modifiers;
            var metadata = SongMetadata.Default;
            var settings = LoaderSettings.Default;

            var drumsType = DrumsType.Unknown;
            if (File.Exists(iniPath))
            {
                modifiers = SongIniHandler.ReadSongIniFile(iniPath);
                metadata = new SongMetadata(modifiers, string.Empty);

                if (modifiers.TryGet("five_lane_drums", out bool fiveLane))
                {
                    drumsType = fiveLane ? DrumsType.FiveLane : DrumsType.Unknown;
                }
            }
            else
            {
                modifiers = new IniSection();
            }

            using var bytes = FixedArray<byte>.Load(chartPath);
            var chart = Load(bytes, in metadata, in settings, drumsType, activeTracks);
            if (!modifiers.TryGet("hopo_frequency", out settings.HopoThreshold) || settings.HopoThreshold <= 0)
            {
                if (modifiers.TryGet("eighthnote_hopo", out bool eighthNoteHopo))
                {
                    chart.Settings.HopoThreshold = chart.Sync.Tickrate / (eighthNoteHopo ? 2 : 3);
                }
                else if (modifiers.TryGet("hopofreq", out long hopoFreq))
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
            modifiers.TryGet("sustain_cutoff_threshold", out chart.Settings.SustainCutoffThreshold);
            return chart;
        }

        public static YARGChart Load(in FixedArray<byte> file, in SongMetadata metadata, in LoaderSettings settings, DrumsType drumsInChart, HashSet<Instrument>? activeTracks)
        {
            if (YARGTextReader.IsUTF8(in file, out var byteContainer))
            {
                return Process(ref byteContainer, in metadata, in settings, drumsInChart, activeTracks);
            }

            using var chars = YARGTextReader.ConvertToUTF16(in file, out var charContainer);
            if (chars.IsAllocated)
            {
                return Process(ref charContainer, in metadata, in settings, drumsInChart, activeTracks);
            }

            using var ints = YARGTextReader.ConvertToUTF32(in file, out var intContainer);
            return Process(ref intContainer, in metadata, in settings, drumsInChart, activeTracks);
        }

        private const long DEFAULT_TICKRATE = 192;
        private static YARGChart Process<TChar>(ref YARGTextContainer<TChar> container, in SongMetadata metadata, in LoaderSettings settings, DrumsType drumsInChart, HashSet<Instrument>? activeTracks)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            var chart = LoadHeaderAndSync(ref container, in metadata, in settings);
            if (activeTracks == null || activeTracks.Contains(Instrument.Vocals))
            {
                chart.LeadVocals = new VocalTrack2(1);
            }

            BasicInstrumentTrack2<ProDrumNote2<FiveLane>>? unknownDrums = null;
            DualTime.SetTruncationLimit(settings, 1);
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
                switch (drumsInChart)
                {
                    case DrumsType.ProDrums:
                        chart.ProDrums ??= new BasicInstrumentTrack2<ProDrumNote2<FourLane>>();
                        UnknownDrumTrackConverter.ConvertTo<ProDrumNote2<FourLane>, FourLane>(chart.ProDrums, unknownDrums);
                        break;
                    case DrumsType.FiveLane:
                        chart.FiveLaneDrums ??= new BasicInstrumentTrack2<DrumNote2<FiveLane>>();
                        UnknownDrumTrackConverter.ConvertTo<DrumNote2<FiveLane>, FiveLane>(chart.FiveLaneDrums, unknownDrums);
                        break;
                    default:
                        chart.FourLaneDrums ??= new BasicInstrumentTrack2<DrumNote2<FourLane>>();
                        UnknownDrumTrackConverter.ConvertTo<DrumNote2<FourLane>, FourLane>(chart.FourLaneDrums, unknownDrums);
                        break;
                }
            }

            YARGChartFinalizer.FinalizeBeats(chart);
            return chart;
        }

        private static YARGChart LoadHeaderAndSync<TChar>(ref YARGTextContainer<TChar> container, in SongMetadata metadata, in LoaderSettings settings)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            long tickrate = DEFAULT_TICKRATE;
            var miscellaneous = new Dictionary<string, IniModifier>();
            if (YARGChartFileReader.ValidateTrack(ref container, YARGChartFileReader.HEADERTRACK))
            {
                foreach (var node in YARGChartFileReader.ExtractModifiers(ref container))
                {
                    if (node.Key != "Resolution")
                    {
                        miscellaneous.Add(node.Key, node.Value[0]);
                    }

                    unsafe
                    {
                        var mod = node.Value[0];
                        if (mod.Buffer[0] != 0)
                        {
                            tickrate = mod.Buffer[0];
                        }
                    }
                }
            }

            var sync = new SyncTrack2(tickrate);
            if (YARGChartFileReader.ValidateTrack(ref container, YARGChartFileReader.SYNCTRACK))
            {
                DotChartEvent ev = default;
                while (YARGChartFileReader.TryParseEvent(ref container, ref ev))
                {
                    switch (ev.Type)
                    {
                        case ChartEventType.Bpm:
                            sync.TempoMarkers.GetLastOrAppend(ev.Position).MicrosPerQuarter = YARGChartFileReader.ExtractMicrosPerQuarter(ref container);
                            break;
                        case ChartEventType.Anchor:
                            sync.TempoMarkers.GetLastOrAppend(ev.Position).Anchor = ev.Position > 0 ? YARGTextReader.ExtractInt64AndWhitespace(ref container) : 0;
                            break;
                        case ChartEventType.Time_Sig:
                            sync.TimeSigs.GetLastOrAppend(ev.Position) = YARGChartFileReader.ExtractTimeSig(ref container);
                            break;
                    }
                    YARGTextReader.GotoNextLine(ref container);
                }
                YARGChartFinalizer.FinalizeAnchors(sync);
            }
            return new YARGChart(sync, metadata, settings, miscellaneous);
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
                if (drumsInChart == DrumsType.ProDrums)
                {
                    instrument = Instrument.ProDrums;
                }
                else if (drumsInChart == DrumsType.FiveLane)
                {
                    instrument = Instrument.FiveLaneDrums;
                }
                else if (drumsInChart == DrumsType.Unknown && activeTracks == null)
                {
                    unsafe
                    {
                        _unknownDrumType = DrumsType.Unknown;
                        bool result = LoadChartTrack(ref container, chart.Sync, difficulty, ref unknownDrums, &Set);
                        drumsInChart = _unknownDrumType;
                        return result;
                    }
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

        private static unsafe bool LoadChartTrack<TChar, TNote>(ref YARGTextContainer<TChar> container, SyncTrack2 sync, Difficulty difficulty, ref BasicInstrumentTrack2<TNote>? track, delegate*<ref TNote, int, in DualTime, bool> loader)
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

                            int lane = YARGTextReader.ExtractInt32AndWhitespace(ref container);
                            long tickDuration = YARGTextReader.ExtractInt64AndWhitespace(ref container);

                            duration.Ticks = tickDuration;
                            duration.Seconds = sync.ConvertToSeconds(position.Ticks + tickDuration, tempoIndex) - position.Seconds;
                            if (!loader(ref note, lane, in duration))
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
                            var type = (SpecialPhraseType) YARGTextReader.ExtractInt32AndWhitespace(ref container);
                            long tickDuration = YARGTextReader.ExtractInt64AndWhitespace(ref container);
                            switch (type)
                            {
                                case SpecialPhraseType.FaceOff_Player1:
                                case SpecialPhraseType.FaceOff_Player2:
                                case SpecialPhraseType.StarPower:
                                case SpecialPhraseType.BRE:
                                case SpecialPhraseType.Tremolo:
                                case SpecialPhraseType.Trill:
                                    duration.Ticks = tickDuration;
                                    duration.Seconds = sync.ConvertToSeconds(position.Ticks + tickDuration, tempoIndex) - position.Seconds;
                                    difficultyTrack.SpecialPhrases.GetLastOrAppend(position).TryAdd(type, new SpecialPhraseInfo(in duration));
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

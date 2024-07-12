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
    public partial class YARGChart
    {
        private const string SOLO = "solo";
        private const string SOLOEND = "soloend";
        private const string SECTION = "section ";
        private const string LYRIC = "lyric ";
        private const string PHRASE_START = "phrase_start";
        private const string PHRASE_END = "phrase_end ";

        private YARGChart(SyncTrack2 sync, SongMetadata metadata, LoaderSettings settings, Dictionary<string, IniModifier> miscellaneous)
        {
            Sync = sync;
            Metadata = metadata;
            Settings = settings;
            Miscellaneous = miscellaneous;
        }

        public static YARGChart LoadChart(string chartPath, HashSet<Instrument>? activeTracks)
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
            var chart = LoadChart(bytes, in metadata, in settings, drumsType, activeTracks);
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

        public static YARGChart LoadChart(in FixedArray<byte> file, in SongMetadata metadata, in LoaderSettings settings, DrumsType drumsInChart, HashSet<Instrument>? activeTracks)
        {
            if (YARGTextReader.IsUTF8(in file, out var byteContainer))
            {
                return Process_Chart(ref byteContainer, in metadata, in settings, drumsInChart, activeTracks);
            }

            using var chars = YARGTextReader.ConvertToUTF16(in file, out var charContainer);
            if (chars.IsAllocated)
            {
                return Process_Chart(ref charContainer, in metadata, in settings, drumsInChart, activeTracks);
            }

            using var ints = YARGTextReader.ConvertToUTF32(in file, out var intContainer);
            return Process_Chart(ref intContainer, in metadata, in settings, drumsInChart, activeTracks);
        }
        
        private static YARGChart Process_Chart<TChar>(ref YARGTextContainer<TChar> container, in SongMetadata metadata, in LoaderSettings settings, DrumsType drumsInChart, HashSet<Instrument>? activeTracks)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            var chart = LoadHeaderAndSync_Chart(ref container, in metadata, in settings);
            if (activeTracks == null || activeTracks.Contains(Instrument.Vocals))
            {
                chart.LeadVocals = new VocalTrack2(1);
            }

            BasicInstrumentTrack2<DrumNote2<FiveLane<DrumPad_Pro>, DrumPad_Pro>>? unknownDrums = null;
            DualTime.SetTruncationLimit(settings, 1);
            while (YARGChartFileReader.IsStartOfTrack(in container))
            {
                if (YARGChartFileReader.ValidateTrack(ref container, YARGChartFileReader.EVENTTRACK)
                    ? !LoadEventsTrack_Chart(ref container, chart)
                    : !SelectTrack_Chart(ref container, chart, ref drumsInChart, activeTracks, ref unknownDrums))
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
                        chart.ProDrums ??= new BasicInstrumentTrack2<DrumNote2<FourLane<DrumPad_Pro>, DrumPad_Pro>>();
                        unknownDrums.ConvertTo(chart.ProDrums);
                        break;
                    case DrumsType.FiveLane:
                        chart.FiveLaneDrums ??= new BasicInstrumentTrack2<DrumNote2<FiveLane<DrumPad>, DrumPad>>();
                        unknownDrums.ConvertTo(chart.FiveLaneDrums);
                        break;
                    default:
                        chart.FourLaneDrums ??= new BasicInstrumentTrack2<DrumNote2<FourLane<DrumPad>, DrumPad>>();
                        unknownDrums.ConvertTo(chart.FourLaneDrums);
                        break;
                }
            }

            YARGChartFinalizer.FinalizeBeats(chart);
            return chart;
        }

        private const long DEFAULT_TICKRATE = 192;
        private static unsafe YARGChart LoadHeaderAndSync_Chart<TChar>(ref YARGTextContainer<TChar> container, in SongMetadata metadata, in LoaderSettings settings)
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

                    var mod = node.Value[0];
                    if (mod.Buffer[0] != 0)
                    {
                        tickrate = mod.Buffer[0];
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
                            sync.TempoMarkers.GetLastOrAppend(ev.Position)->MicrosPerQuarter = YARGChartFileReader.ExtractMicrosPerQuarter(ref container);
                            break;
                        case ChartEventType.Anchor:
                            if (ev.Position > 0)
                            {
                                sync.TempoMarkers.GetLastOrAppend(ev.Position)->Anchor = YARGTextReader.ExtractInt64AndWhitespace(ref container);
                            }
                            break;
                        case ChartEventType.Time_Sig:
                            *sync.TimeSigs.GetLastOrAppend(ev.Position) = YARGChartFileReader.ExtractTimeSig(ref container);
                            break;
                    }
                }
                YARGChartFinalizer.FinalizeAnchors(sync);
            }
            return new YARGChart(sync, metadata, settings, miscellaneous);
        }

        private static bool LoadEventsTrack_Chart<TChar>(ref YARGTextContainer<TChar> container, YARGChart chart)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            if (!chart.Events.IsEmpty() || (chart.LeadVocals != null && chart.LeadVocals.IsOccupied()))
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
                            if (phrase.Ticks >= 0 && position.Ticks > phrase.Ticks)
                            {
                                chart.LeadVocals.SpecialPhrases[phrase].TryAdd(SpecialPhraseType.LyricLine, new(position - phrase));
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
                                chart.LeadVocals!.SpecialPhrases[phrase].TryAdd(SpecialPhraseType.LyricLine, new(position - phrase));
                            }
                            phrase.Ticks = -1;
                        }
                    }
                    else
                    {
                        chart.Events.Globals.GetLastOrAppend(position).Add(str);
                    }
                }
            }

            if (chart.LeadVocals != null && !chart.LeadVocals.IsOccupied())
            {
                chart.LeadVocals = null;
            }
            return true;
        }

        private static bool SelectTrack_Chart<TChar>(ref YARGTextContainer<TChar> container, YARGChart chart, ref DrumsType drumsInChart, HashSet<Instrument>? activeTracks, ref BasicInstrumentTrack2<DrumNote2<FiveLane<DrumPad_Pro>, DrumPad_Pro>>? unknownDrums)
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
                        bool result = LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref unknownDrums, &Set);
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
                    Instrument.FiveFretGuitar =>     LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref chart.FiveFretGuitar, &Set),
                    Instrument.FiveFretBass =>       LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref chart.FiveFretBass, &Set),
                    Instrument.FiveFretRhythm =>     LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref chart.FiveFretRhythm, &Set),
                    Instrument.FiveFretCoopGuitar => LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref chart.FiveFretCoopGuitar, &Set),
                    Instrument.SixFretGuitar =>      LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref chart.SixFretGuitar, &Set),
                    Instrument.SixFretBass =>        LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref chart.SixFretBass, &Set),
                    Instrument.SixFretRhythm =>      LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref chart.SixFretRhythm, &Set),
                    Instrument.SixFretCoopGuitar =>  LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref chart.SixFretCoopGuitar, &Set),
                    Instrument.FourLaneDrums =>      LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref chart.FourLaneDrums, &Set),
                    Instrument.ProDrums =>           LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref chart.ProDrums, &Set),
                    Instrument.FiveLaneDrums =>      LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref chart.FiveLaneDrums, &Set),
                    Instrument.Keys =>               LoadInstrumentTrack_Chart(ref container, chart.Sync, difficulty, ref chart.Keys, &Set),
                    _ => false,
                };
            }
        }

        private static unsafe bool LoadInstrumentTrack_Chart<TChar, TNote>(ref YARGTextContainer<TChar> container, SyncTrack2 sync, Difficulty difficulty, ref BasicInstrumentTrack2<TNote>? track, delegate*<ref TNote, int, in DualTime, bool> setter)
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
                            var note = difficultyTrack.Notes.GetLastOrAppend(position);

                            int lane = YARGTextReader.ExtractInt32AndWhitespace(ref container);
                            long tickDuration = YARGTextReader.ExtractInt64AndWhitespace(ref container);
                            if (tickDuration == 0)
                            {
                                tickDuration = 1;
                            }

                            duration.Ticks = tickDuration;
                            duration.Seconds = sync.ConvertToSeconds(position.Ticks + tickDuration, tempoIndex) - position.Seconds;
                            if (!setter(ref *note, lane, in duration))
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
                            }
                            break;
                        }
                    case ChartEventType.Text:
                        string str = YARGTextReader.ExtractText(ref container, true);
                        if (str == SOLOEND)
                        {
                            if (soloQueue[0].Ticks != -1)
                            {
                                // .chart handles solo phrases with *inclusive ends*, so we have to add one tick
                                var soloEnd = position;
                                ++soloEnd.Ticks;
                                soloEnd.Seconds = sync.ConvertToSeconds(soloEnd.Ticks, tempoIndex);

                                difficultyTrack.SpecialPhrases[soloQueue[0]].TryAdd(SpecialPhraseType.Solo, new SpecialPhraseInfo(soloEnd - soloQueue[0]));
                                soloQueue[0] = soloQueue[1] == position ? soloQueue[1] : DualTime.Inactive;
                                soloQueue[1] = DualTime.Inactive;
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
                            difficultyTrack.Events.GetLastOrAppend(position).Add(str);
                        }
                        break;
                }
            }

            difficultyTrack.TrimExcess();
            return true;
        }
    }
}

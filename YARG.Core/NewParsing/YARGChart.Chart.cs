using System;
using System.Collections.Generic;
using System.IO;
using YARG.Core.Chart;
using YARG.Core.Containers;
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

        /// <summary>
        /// Creates an empty chart with supplied sync, metadata, and settings.
        /// </summary>
        /// <remarks>For use solely for deserialization</remarks>
        /// <param name="sync">The backing sync track</param>
        /// <param name="metadata">The Ini and/or .chart metdata info</param>
        /// <param name="settings">The settings for converting notes to their engine types</param>
        /// <param name="miscellaneous">Container of miscellaneous modifiers from the [Song] chunk</param>
        private YARGChart(long resolution, SyncTrack2 sync, in SongMetadata metadata, in LoaderSettings settings, Dictionary<string, IniModifier> miscellaneous)
        {
            _resolution = resolution;
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
                    chart.Settings.HopoThreshold = chart.Resolution / (eighthNoteHopo ? 2 : 3);
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
                    chart.Settings.HopoThreshold = 4 * chart.Resolution / denominator;
                }
                else
                {
                    chart.Settings.HopoThreshold = chart.Resolution / 3;
                }

                // With a 192 resolution, .chart has a HOPO threshold of 65 ticks, not 64,
                // so we need to scale this factor to different resolutions (480 res = 162.5 threshold).
                // Why?... idk, but I hate it.
                const float DEFAULT_RESOLUTION = 192;
                chart.Settings.HopoThreshold += (long) (chart.Resolution / DEFAULT_RESOLUTION);
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
            var unknownDrums = InstrumentTrack2<UnknownLaneDrums>.Default;
            while (YARGChartFileReader.IsStartOfTrack(in container))
            {
                if (!SelectTrack_Chart(ref container, chart, ref drumsInChart, activeTracks, ref unknownDrums))
                {
                    if (YARGTextReader.SkipLinesUntil(ref container, TextConstants<TChar>.CLOSE_BRACE))
                    {
                        YARGTextReader.GotoNextLine(ref container);
                    }
                }
            }

            if (!unknownDrums.IsEmpty())
            {
                switch (drumsInChart)
                {
                    case DrumsType.ProDrums:
                        unknownDrums.ConvertTo(chart.FourLaneDrums, true);
                        break;
                    case DrumsType.FiveLane:
                        unknownDrums.ConvertTo(chart.FiveLaneDrums);
                        break;
                    default:
                        unknownDrums.ConvertTo(chart.FourLaneDrums, false);
                        break;
                }
            }

            FinalizeDeserialization(chart);
            chart.Settings.AllowHopoAfterChord = true;
            return chart;
        }

        private const long DEFAULT_TICKRATE = 192;
        private static unsafe YARGChart LoadHeaderAndSync_Chart<TChar>(ref YARGTextContainer<TChar> container, in SongMetadata metadata, in LoaderSettings settings)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            long resolution = DEFAULT_TICKRATE;
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
                        resolution = mod.Buffer[0];
                    }
                }
            }

            var sync = new SyncTrack2();
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
                            sync.TimeSigs.AppendOrUpdate(ev.Position, YARGChartFileReader.ExtractTimeSig(ref container));
                            break;
                    }
                }
                FinalizeAnchors(sync, resolution);
            }
            return new YARGChart(resolution, sync, in metadata, in settings, miscellaneous);
        }

        private static bool SelectTrack_Chart<TChar>(ref YARGTextContainer<TChar> container, YARGChart chart, ref DrumsType drumsInChart, HashSet<Instrument>? activeTracks, ref InstrumentTrack2<UnknownLaneDrums> unknownDrums)
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

            // Provides a more algorithmically optimal route for mapping midi ticks to seconds
            var tempoTracker = new TempoTracker(chart.Sync, chart.Resolution);
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
                    UnknownLaneDrums.DrumType = DrumsType.Unknown;
                    bool result = LoadInstrumentTrack_Chart(ref container, ref unknownDrums.Difficulties[difficulty], ref tempoTracker);
                    drumsInChart = UnknownLaneDrums.DrumType;
                    return result;
                }
            }

            if (activeTracks != null && !activeTracks.Contains(instrument))
            {
                return false;
            }

            return instrument switch
            {
                Instrument.FiveFretGuitar =>     LoadInstrumentTrack_Chart(ref container, ref chart.FiveFretGuitar.Difficulties[difficulty],     ref tempoTracker),
                Instrument.FiveFretBass =>       LoadInstrumentTrack_Chart(ref container, ref chart.FiveFretBass.Difficulties[difficulty],       ref tempoTracker),
                Instrument.FiveFretRhythm =>     LoadInstrumentTrack_Chart(ref container, ref chart.FiveFretRhythm.Difficulties[difficulty],     ref tempoTracker),
                Instrument.FiveFretCoopGuitar => LoadInstrumentTrack_Chart(ref container, ref chart.FiveFretCoopGuitar.Difficulties[difficulty], ref tempoTracker),
                Instrument.SixFretGuitar =>      LoadInstrumentTrack_Chart(ref container, ref chart.SixFretGuitar.Difficulties[difficulty],      ref tempoTracker),
                Instrument.SixFretBass =>        LoadInstrumentTrack_Chart(ref container, ref chart.SixFretBass.Difficulties[difficulty],        ref tempoTracker),
                Instrument.SixFretRhythm =>      LoadInstrumentTrack_Chart(ref container, ref chart.SixFretRhythm.Difficulties[difficulty],      ref tempoTracker),
                Instrument.SixFretCoopGuitar =>  LoadInstrumentTrack_Chart(ref container, ref chart.SixFretCoopGuitar.Difficulties[difficulty],  ref tempoTracker),
                Instrument.FourLaneDrums or
                Instrument.ProDrums =>           LoadInstrumentTrack_Chart(ref container, ref chart.FourLaneDrums.Difficulties[difficulty],      ref tempoTracker),
                Instrument.FiveLaneDrums =>      LoadInstrumentTrack_Chart(ref container, ref chart.FiveLaneDrums.Difficulties[difficulty],      ref tempoTracker),
                Instrument.Keys =>               LoadInstrumentTrack_Chart(ref container, ref chart.Keys.Difficulties[difficulty],               ref tempoTracker),
                _ => false,
            };
        }

        private static unsafe bool LoadEventsTrack_Chart<TChar>(ref YARGTextContainer<TChar> container, YARGChart chart)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
        {
            if (!chart.Globals.IsEmpty() || !chart.Sections.IsEmpty() || !chart.LeadVocals.IsEmpty())
            {
                YargLogger.LogInfo("[Events] track appears multiple times. Not parsing repeats...");
                return false;
            }

            // Provides a more algorithmically optimal route for mapping midi ticks to seconds
            var tempoTracker = new TempoTracker(chart.Sync, chart.Resolution);
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
                        chart.LeadVocals[0].Lyrics.AppendOrUpdate(in position, str[LYRIC.Length..]);
                    }
                    else if (str == PHRASE_START)
                    {
                        if (phrase.Ticks >= 0 && position.Ticks > phrase.Ticks)
                        {
                            chart.LeadVocals.VocalPhrases_1.Append(in phrase, position - phrase);
                        }
                        phrase = position;
                    }
                    else if (str == PHRASE_END)
                    {
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
            return true;
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

        private static unsafe bool LoadInstrumentTrack_Chart<TChar, TNote>(ref YARGTextContainer<TChar> container, ref DifficultyTrack2<TNote> difficultyTrack, ref TempoTracker tempoTracker)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
            where TNote : unmanaged, IInstrumentNote, IDotChartLoadable
        {
            if (!difficultyTrack.IsEmpty())
            {
                return false;
            }
            difficultyTrack.Notes.Capacity = 5000;

            // Keeps tracks of soloes that start on the same tick when another solo ends
            var soloPosition = DualTime.Inactive;
            var nextSoloPosition = DualTime.Inactive;

            var ev = default(DotChartEvent);
            var position = default(DualTime);
            while (YARGChartFileReader.TryParseEvent(ref container, ref ev))
            {
                position.Ticks = ev.Position;
                position.Seconds = tempoTracker.Traverse(ev.Position);

                switch (ev.Type)
                {
                    case ChartEventType.Note:
                        {
                            var (lane, duration) = YARGChartFileReader.ExtractLaneAndDuration(ref container, in position, in tempoTracker);
                            var note = difficultyTrack.Notes.GetLastOrAppend(in position);
                            if (!note->SetFromDotChart(lane, in duration) && note->GetNumActiveLanes() == 0)
                            {
                                difficultyTrack.Notes.Pop();
                            }
                            break;
                        }
                    case ChartEventType.Special:
                        {
                            var (lane, duration) = YARGChartFileReader.ExtractLaneAndDuration(ref container, in position, in tempoTracker);
                            switch ((SpecialPhraseType) lane)
                            {
                                case SpecialPhraseType.FaceOff_Player1: AddSpecialPhrase(ref difficultyTrack.Faceoff_Player1, in position, in duration); break;
                                case SpecialPhraseType.FaceOff_Player2: AddSpecialPhrase(ref difficultyTrack.Faceoff_Player2, in position, in duration); break;
                                case SpecialPhraseType.StarPower:       AddSpecialPhrase(ref difficultyTrack.Overdrives,      in position, in duration); break;
                                case SpecialPhraseType.BRE:             AddSpecialPhrase(ref difficultyTrack.BREs,            in position, in duration); break;
                                case SpecialPhraseType.Tremolo:         AddSpecialPhrase(ref difficultyTrack.Tremolos,        in position, in duration); break;
                                case SpecialPhraseType.Trill:           AddSpecialPhrase(ref difficultyTrack.Trills,          in position, in duration); break;
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

        private static unsafe void AddSpecialPhrase(ref YARGNativeSortedList<DualTime, DualTime> phrases, in DualTime position, in DualTime duration)
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
    }
}

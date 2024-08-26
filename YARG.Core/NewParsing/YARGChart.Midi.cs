using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime;
using System.Text;
using YARG.Core.Chart;
using YARG.Core.IO;
using YARG.Core.Logging;
using YARG.Core.IO.Ini;
using YARG.Core.NewParsing.Midi;
using YARG.Core.Song;
using YARG.Core.IO.Disposables;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public partial class YARGChart
    {
        /// <summary>
        /// Loads a YARGChart object from the provided midi file. <br></br>
        /// If an ini file is present in the same directory, its data will be pulled to use during the deserialization.
        /// </summary>
        /// <param name="chartInfo">The file path for the midi file to load</param>
        /// <param name="activeInstruments">Provides guidance over which instruments from the midi file to load. If null, all instruments will be loaded.</param>
        /// <returns>The chart data from the midi file</returns>
        public static YARGChart LoadMidi_Single(FileInfo chartInfo, HashSet<MidiTrackType>? activeInstruments)
        {
            var iniInfo = new FileInfo(Path.Combine(chartInfo.DirectoryName, "song.ini"));

            var metadata = SongMetadata.Default;
            var settings = ParseSettings.Default;
            if (iniInfo.Exists)
            {
                var modifiers = SongIniHandler.ReadSongIniFile(iniInfo);
                metadata = new SongMetadata(modifiers, string.Empty);

                var drums = DrumsType.UnknownPro;
                if (modifiers.TryGet("five_lane_drums", out bool fiveLane))
                {
                    if (fiveLane)
                    {
                        drums = DrumsType.FiveLane;
                    }
                    else if (!modifiers.TryGet("pro_drums", out bool prodrums) || prodrums)
                    {
                        drums = DrumsType.ProDrums;
                    }
                    else
                    {
                        // We can not pre-determine whether tom markers are present,
                        // so setting the type to FourLane won't work.
                        drums = DrumsType.Unknown_Four;
                    }
                }
                else if (modifiers.TryGet("pro_drums", out bool prodrums) && !prodrums)
                {
                    drums = DrumsType.Unknown;
                }
                settings = new ParseSettings(modifiers, drums);
            }

            using var file = MemoryMappedArray.Load(chartInfo);
            return LoadMidi_Single(file.ToStream(), in metadata, in settings, activeInstruments);
        }

        /// <summary>
        /// Loads a YARGChart object, using the stream to provided the midi file data.<br></br>
        /// The chart will use provided metadata and settings during initialization, with the settings also being used during deserialization.
        /// </summary>
        /// <param name="stream">The stream that contains or points to the midi file data</param>
        /// <param name="metadata">The ini metadata to initiliaze the chart with</param>
        /// <param name="settings">The settings to use during the deserialization and/or conversion of tracks to their engine variants</param>
        /// <param name="activeInstruments">Provides guidance over which instruments from the midi file to load. If null, all instruments will be loaded.</param>
        /// <returns>The chart data from the midi stream</returns>
        public static YARGChart LoadMidi_Single(Stream stream, in SongMetadata metadata, in ParseSettings settings, HashSet<MidiTrackType>? activeInstruments)
        {
            var midi = new YARGMidiFile(stream);
            var (sync, sequencename) = LoadSyncTrack_Midi(midi);
            var chart = new YARGChart(sync, metadata, settings, sequencename);
            chart.Settings.ChordHopoCancellation = true;

            DualTime.SetTruncationLimit(settings, sync.Tickrate / 3);
            MidiFiveFretLoader.SetOverdriveMidiNote(settings.StarPowerNote);

            var encoding = Encoding.UTF8;
            LoadMidiTracks(chart, sync, midi, ref encoding, activeInstruments);
            FinalizeDeserialization(chart);
            return chart;
        }

        /// <summary>
        /// Loads a YARGChart object, using multiple streams to provide all the midi file data to combine.<br></br>
        /// The chart will use provided metadata and settings during initialization, with the settings also being used during deserialization.
        /// </summary>
        /// <remarks>Any conflicting data present in any of the streams will apply with this order of precedence: Update > Upgrade > Original</remarks>
        /// <param name="mainStream">The stream that contains or points to the original midi file data</param>
        /// <param name="updateStream">The stream that contains or points to the update midi file data</param>
        /// <param name="upgradeStream">The stream that contains or points to the upgrade midi file data</param>
        /// <param name="metadata">The ini metadata to initiliaze the chart with</param>
        /// <param name="settings">The settings to use during the deserialization and/or conversion of tracks to their engine variants</param>
        /// <param name="activeInstruments">Provides guidance over which instruments from the midi file to load. If null, all instruments will be loaded.</param>
        /// <returns>The combined chart data from all the midi streams</returns>
        public static YARGChart LoadMidi_Multi(Stream mainStream, Stream? updateStream, Stream? upgradeStream, in SongMetadata metadata, in ParseSettings settings, HashSet<MidiTrackType>? activeInstruments)
        {
            var midi = new YARGMidiFile(mainStream);
            var (sync, sequencename) = LoadSyncTrack_Midi(midi);
            var chart = new YARGChart(sync, metadata, settings, sequencename);
            chart.Settings.ChordHopoCancellation = true;

            DualTime.SetTruncationLimit(settings, sync.Tickrate / 3);
            MidiFiveFretLoader.SetOverdriveMidiNote(settings.StarPowerNote);

            // Temporary start point. The settings variable may carry that information in the future.
            var encoding = Encoding.UTF8;
            if (updateStream != null)
            {
                var updateMidi = new YARGMidiFile(updateStream);
                var (updateSync, _) = LoadSyncTrack_Midi(updateMidi);
                LoadMidiTracks(chart, updateSync, updateMidi, ref encoding, activeInstruments);
            }

            if (upgradeStream != null)
            {
                var upgradeMidi = new YARGMidiFile(upgradeStream);
                var (upgradeSync, _) = LoadSyncTrack_Midi(upgradeMidi);
                LoadMidiTracks(chart, upgradeSync, upgradeMidi, ref encoding, activeInstruments);
            }

            LoadMidiTracks(chart, sync, midi, ref encoding, activeInstruments);
            FinalizeDeserialization(chart);
            return chart;
        }

        /// <summary>
        /// Loads tempo markers and time signatures from the initial midi track
        /// </summary>
        /// <remarks>
        /// The midi file should be in its default state to work properly.
        /// </remarks>
        /// <param name="midi">The untouched midi file to pull the track from</param>
        /// <returns></returns>
        private static unsafe (SyncTrack2 Sync, string? SequenceName) LoadSyncTrack_Midi(YARGMidiFile midi)
        {
            var sync = new SyncTrack2(midi.TickRate);

            using var midiTrack = midi.LoadNextTrack();
            if (midiTrack == null)
            {
                // Technically, this means there's no data, so we'll just ensure default initialization
                return (sync, string.Empty);
            }

            string? sequenceName = midiTrack.FindTrackName(Encoding.UTF8);
            while (midiTrack.ParseEvent())
            {
                switch (midiTrack.Type)
                {
                    case MidiEventType.Tempo:
                        sync.TempoMarkers.GetLastOrAppend(midiTrack.Position)->MicrosPerQuarter = midiTrack.ExtractMicrosPerQuarter();
                        break;
                    case MidiEventType.Time_Sig:
                        sync.TimeSigs.AppendOrUpdate(midiTrack.Position, midiTrack.ExtractTimeSig());
                        break;
                }
            }
            FinalizeAnchors(sync);
            return (sync, sequenceName);
        }

        /// <summary>
        /// Loops through all the tracks present in the provided midifile object
        /// </summary>
        /// <param name="chart">The chart to load data into</param>
        /// <param name="sync">The synctrack to use when loading subtracks. This may not be the synctrack present in the chart object in the case of CON files.</param>
        /// <param name="midi">The midifile containing the tracks to load</param>
        /// <param name="encoding">The encoding to use to decode midi text events to lyrics</param>
        /// <param name="activeInstruments">Provides guidance over which instruments from the midi file to load. If null, all instruments will be loaded.</param>
        private static void LoadMidiTracks(YARGChart chart, SyncTrack2 sync, YARGMidiFile midi, ref Encoding encoding, HashSet<MidiTrackType>? activeInstruments)
        {
            int count = 1;
            foreach (var midiTrack in midi)
            {
                string? trackname = midiTrack.FindTrackName(Encoding.UTF8);
                if (trackname != null && YARGMidiTrack.TRACKNAMES.TryGetValue(trackname, out var type))
                {
                    if (type == MidiTrackType.Events)
                    {
                        LoadEventsTrack_Midi(chart, sync, midiTrack);
                    }
                    else if (type == MidiTrackType.Beat)
                    {
                        LoadBeatsTrack_Midi(chart.BeatMap, sync, midiTrack);
                    }
                    else if (activeInstruments == null || activeInstruments.Contains(type))
                    {
                        LoadInstrument_Midi(chart, type, sync, midiTrack, ref encoding);
                    }
                }
                else if (trackname == null)
                {
                    YargLogger.LogInfo($"MIDI Track #{count}'s track name is ambiguous from multiple trackname events and thus could not be loaded.");
                }
                else
                {
                    YargLogger.LogInfo($"Unrecognized MIDI Track #{count}: {trackname}");
                }
                ++count;
            }
        }

        private static readonly byte[][] PREFIXES = { Encoding.ASCII.GetBytes("[section "), Encoding.ASCII.GetBytes("[prc_") };
        /// <summary>
        /// Loads sections and global event data from the EVENTS track
        /// </summary>
        /// <param name="chart">The chart that'll hold the sections and globals</param>
        /// <param name="sync">The backing sync track to use for proper positioning</param>
        /// <param name="midiTrack">The midi track containing the event data</param>
        private static void LoadEventsTrack_Midi(YARGChart chart, SyncTrack2 sync, YARGMidiTrack midiTrack)
        {
            if (!chart.Globals.IsEmpty() || !chart.Sections.IsEmpty())
            {
                YargLogger.LogInfo("EVENTS track appears multiple times. Not parsing repeats...");
                return;
            }

            var position = default(DualTime);
            var tempoTracker = new TempoTracker(sync);
            while (midiTrack.ParseEvent())
            {
                if (MidiEventType.Text <= midiTrack.Type && midiTrack.Type <= MidiEventType.Text_EnumLimit && midiTrack.Type != MidiEventType.Text_TrackName)
                {
                    position.Ticks = midiTrack.Position;
                    position.Seconds = tempoTracker.Traverse(midiTrack.Position);

                    var bytes = midiTrack.ExtractTextOrSysEx();
                    if (bytes.StartsWith(PREFIXES[0]))
                    {
                        chart.Sections.AppendOrUpdate(position, Encoding.UTF8.GetString(bytes[PREFIXES[0].Length..(bytes.Length - 1)]));
                    }
                    else if (bytes.StartsWith(PREFIXES[1]))
                    {
                        chart.Sections.AppendOrUpdate(position, Encoding.UTF8.GetString(bytes[PREFIXES[1].Length..(bytes.Length - 1)]));
                    }
                    else
                    {
                        // Other miscellaneous events are queries, so those should always be bound within ASCII
                        chart.Globals.GetLastOrAppend(position).Add(Encoding.ASCII.GetString(bytes));
                    }
                }
            }
        }

        /// <summary>
        /// Loads the beat track with measure and strong beats from the BEATS track
        /// </summary>
        /// <param name="beats">The beats track to fill/param>
        /// <param name="sync">The backing sync track to use for proper positioning</param>
        /// <param name="midiTrack">The midi track containing the beat track data</param>
        private static void LoadBeatsTrack_Midi(YARGNativeSortedList<DualTime, BeatlineType> beats, SyncTrack2 sync, YARGMidiTrack midiTrack)
        {
            const int MEASURE_BEAT = 12;
            const int STRONG_BEAT = 13;
            if (!beats.IsEmpty())
            {
                YargLogger.LogInfo("BEATS track appears multiple times. Not parsing repeats...");
                return;
            }

            var position = default(DualTime);
            var note = default(MidiNote);
            var tempoTracker = new TempoTracker(sync);
            while (midiTrack.ParseEvent())
            {
                if (midiTrack.Type == MidiEventType.Note_On)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    // No need to handle note offs
                    if (note.velocity > 0)
                    {
                        position.Ticks = midiTrack.Position;
                        position.Seconds = tempoTracker.Traverse(midiTrack.Position);

                        switch (note.value)
                        {
                            case MEASURE_BEAT: beats.AppendOrUpdate(in position, BeatlineType.Measure); break;
                            case STRONG_BEAT:  beats.AppendOrUpdate(in position, BeatlineType.Strong); break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Uses the provided track type to select the correct instrument to load the track data into
        /// </summary>
        /// <param name="chart">The chart with all the tracks... duh</param>
        /// <param name="type">The track type to load</param>
        /// <param name="sync">The backing sync track to use for proper positioning</param>
        /// <param name="midiTrack">The track containing the instrument or vocal data</param>
        /// <param name="encoding">The encoding to use to decode midi text events to lyrics</param>
        private static void LoadInstrument_Midi(YARGChart chart, MidiTrackType type, SyncTrack2 sync, YARGMidiTrack midiTrack, ref Encoding encoding) 
        {
            switch (type)
            {
                case MidiTrackType.Guitar_5:      chart.FiveFretGuitar ??=     MidiFiveFretLoader.Load(midiTrack, sync); break;
                case MidiTrackType.Bass_5:        chart.FiveFretBass ??=       MidiFiveFretLoader.Load(midiTrack, sync); break;
                case MidiTrackType.Rhythm_5:      chart.FiveFretRhythm ??=     MidiFiveFretLoader.Load(midiTrack, sync); break;
                case MidiTrackType.Coop_5:        chart.FiveFretCoopGuitar ??= MidiFiveFretLoader.Load(midiTrack, sync); break;
                case MidiTrackType.Keys:          chart.Keys ??=               MidiFiveFretLoader.Load(midiTrack, sync); break;

                case MidiTrackType.Guitar_6:      chart.SixFretGuitar ??=      MidiSixFretLoader. Load(midiTrack, sync); break;
                case MidiTrackType.Bass_6:        chart.SixFretBass ??=        MidiSixFretLoader. Load(midiTrack, sync); break;
                case MidiTrackType.Rhythm_6:      chart.SixFretRhythm ??=      MidiSixFretLoader. Load(midiTrack, sync); break;
                case MidiTrackType.Coop_6:        chart.SixFretCoopGuitar ??=  MidiSixFretLoader. Load(midiTrack, sync); break;

                case MidiTrackType.Drums:
                    switch (chart.Settings.DrumsType)
                    {
                        case DrumsType.FourLane:
                        case DrumsType.ProDrums:
                            chart.FourLaneDrums ??= MidiDrumsLoader.LoadFourLane(midiTrack, sync);
                            break;
                        case DrumsType.FiveLane:
                            chart.FiveLaneDrums ??= MidiDrumsLoader.LoadFiveLane(midiTrack, sync);
                            break;
                        default:
                            {
                                // Edna Mode: NO DUPLI-CAPES
                                if (chart.FourLaneDrums != null || chart.FiveLaneDrums != null)
                                {
                                    break;
                                }

                                // The phrases and events will be moved to the converted track, so it is safe to call dispose
                                using var track = MidiDrumsLoader.LoadUnknownDrums(midiTrack, sync, ref chart.Settings.DrumsType);
                                switch (chart.Settings.DrumsType)
                                {
                                    case DrumsType.FiveLane:
                                        chart.FiveLaneDrums = track.ConvertToFiveLane();
                                        break;
                                    case DrumsType.ProDrums:
                                    case DrumsType.UnknownPro:
                                        chart.FourLaneDrums = track.ConvertToFourLane(true);
                                        break;
                                    // Fourlane, Unknown, or Unknown_Four
                                    default:
                                        chart.FourLaneDrums = track.ConvertToFourLane(false);
                                        break;
                                }
                                break;
                            }
                    }
                    break;
                case MidiTrackType.Pro_Guitar_17: chart.ProGuitar_17Fret ??=   MidiProGuitarLoader.Load<ProFret_17>(midiTrack, sync); break;
                case MidiTrackType.Pro_Guitar_22: chart.ProGuitar_22Fret ??=   MidiProGuitarLoader.Load<ProFret_22>(midiTrack, sync); break;
                case MidiTrackType.Pro_Bass_17:   chart.ProBass_17Fret ??=     MidiProGuitarLoader.Load<ProFret_17>(midiTrack, sync); break;
                case MidiTrackType.Pro_Bass_22:   chart.ProBass_22Fret ??=     MidiProGuitarLoader.Load<ProFret_22>(midiTrack, sync); break;
                
                case MidiTrackType.Pro_Keys_X:
                case MidiTrackType.Pro_Keys_H:
                case MidiTrackType.Pro_Keys_M:
                case MidiTrackType.Pro_Keys_E:
                    {
                        chart.ProKeys ??= new InstrumentTrack2<ProKeysDifficultyTrack>();
                        // Handled per-difficulty, so we use 0-3 indexing
                        MidiProKeysLoader.Load(midiTrack, sync, chart.ProKeys, type - MidiTrackType.Pro_Keys_E);
                        break;
                    }
                case MidiTrackType.Vocals: chart.LeadVocals ??= MidiVocalsLoader.LoadPartVocals(midiTrack, sync, ref encoding); break;
                case MidiTrackType.Harm1:
                case MidiTrackType.Harm2:
                case MidiTrackType.Harm3:
                    {
                        chart.HarmonyVocals ??= new HarmonyVocalsTrack();
                        int index = type - MidiTrackType.Harm1;
                        if (chart.HarmonyVocals[index].IsEmpty())
                        {
                            MidiVocalsLoader.Load(midiTrack, sync, chart.HarmonyVocals, index, ref encoding);
                        }
                        break;
                    }
            }
        }
    }
}

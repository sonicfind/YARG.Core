using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YARG.Core.Chart;
using YARG.Core.IO;
using YARG.Core.IO.Ini;
using YARG.Core.Logging;
using YARG.Core.NewParsing.Midi;
using YARG.Core.Song;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public partial class YARGChart
    {
        /// <summary>
        /// Loads a YARGChart object from the provided midi file. <br></br>
        /// If an ini file is present in the same directory, its data will be pulled to use during the deserialization.
        /// </summary>
        /// <param name="filename">The file path for the midi file to load</param>
        /// <param name="activeInstruments">Provides guidance over which instruments from the midi file to load. If null, all instruments will be loaded.</param>
        /// <returns>The chart data from the midi file</returns>
        public static YARGChart LoadMidi_Single(string filename, HashSet<MidiTrackType>? activeInstruments)
        {
            var iniPath = Path.Combine(Path.GetDirectoryName(filename), "song.ini");

            IniModifierCollection modifiers;
            var metadata = SongMetadata.Default;
            var settings = LoaderSettings.Default;
            var drumsType = DrumsType.Any;
            if (File.Exists(iniPath))
            {
                modifiers = SongIniHandler.ReadSongIniFile(iniPath);
                if (modifiers.Extract("five_lane_drums", out bool fiveLane))
                {
                    drumsType = fiveLane ? DrumsType.FiveLane : DrumsType.FourOrPro;
                }

                if (drumsType != DrumsType.FiveLane)
                {
                    // We don't want to just immediately set the value to one of the other
                    // on the chance that we still need to test for FiveLane.
                    // We just know what the .ini explicitly tells us it *isn't*.
                    //
                    // That being said, .chart differs from .mid in that FourLane is the default state.
                    // .mid's default is ProDrums, which is why we account for when the .ini does
                    // not contain the flag.
                    if (!modifiers.Extract("pro_drums", out bool proDrums) || proDrums)
                    {
                        drumsType -= DrumsType.FourLane;
                    }
                    else
                    {
                        drumsType -= DrumsType.ProDrums;
                    }
                }

                if (!modifiers.Extract("multiplier_note", out settings.OverdiveMidiNote) || settings.OverdiveMidiNote != 103)
                {
                    settings.OverdiveMidiNote = 116;
                }
                SongMetadata.FillFromIni(ref metadata, modifiers);
            }
            else
            {
                modifiers = new IniModifierCollection();
            }

            using var data = FixedArray.LoadFile(filename);
            var chart = LoadMidi_Single(data, in metadata, in settings, modifiers, drumsType, activeInstruments);
            if (!modifiers.Extract("hopo_frequency", out chart.Settings.HopoThreshold) || chart.Settings.HopoThreshold <= 0)
            {
                if (modifiers.Extract("eighthnote_hopo", out bool eighthNoteHopo))
                {
                    chart.Settings.HopoThreshold = chart.Resolution / (eighthNoteHopo ? 2 : 3);
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
                    chart.Settings.HopoThreshold = 4 * chart.Resolution / denominator;
                }
                else
                {
                    chart.Settings.HopoThreshold = chart.Resolution / 3;
                }
            }

            if (!modifiers.Extract("sustain_cutoff_threshold", out chart.Settings.SustainCutoffThreshold))
            {
                chart.Settings.SustainCutoffThreshold = chart.Resolution / 3;
            }
            return chart;
        }

        /// <summary>
        /// Loads a YARGChart object, using the stream to provided the midi file data.<br></br>
        /// The chart will use provided metadata and settings during initialization, with the settings also being used during deserialization.
        /// </summary>
        /// <param name="data">The buffer containing all the midi data</param>
        /// <param name="metadata">The ini metadata to initiliaze the chart with</param>
        /// <param name="settings">The settings to use during the deserialization and/or conversion of tracks to their engine variants</param>
        /// <param name="drumsInChart">The type of drums parsing to apply to any drums track in the file</param>
        /// <param name="activeInstruments">Provides guidance over which instruments from the midi file to load. If null, all instruments will be loaded.</param>
        /// <returns>The chart data from the midi stream</returns>
        public static YARGChart LoadMidi_Single(FixedArray<byte> data, in SongMetadata metadata, in LoaderSettings settings, IniModifierCollection? modifiers, DrumsType drumsInChart, HashSet<MidiTrackType>? activeInstruments)
        {
            var midi = YARGMidiFile.Load(data);
            var chart = new YARGChart(midi.Resolution, in metadata, in settings, modifiers);
            MidiFiveFretLoader.SetOverdriveMidiNote(chart.Settings.OverdiveMidiNote);

            var encoding = YARGTextReader.UTF8Strict;
            LoadTracks_Midi(chart, ref midi, ref encoding, ref drumsInChart, activeInstruments);
            FinalizeDeserialization(chart);
            return chart;
        }

        /// <summary>
        /// Loads a YARGChart object, using multiple streams to provide all the midi file data to combine.<br></br>
        /// The chart will use provided metadata and settings during initialization, with the settings also being used during deserialization.
        /// </summary>
        /// <remarks>Any conflicting data present in any of the streams will apply with this order of precedence: Update > Upgrade > Original</remarks>
        /// <param name="mainData">The buffer containing the original midi file data</param>
        /// <param name="updateData">The buffer containing the update midi file data (if allocated)</param>
        /// <param name="upgradeData">The buffer containing the upgrade midi file data (if allocated)</param>
        /// <param name="metadata">The ini metadata to initiliaze the chart with</param>
        /// <param name="settings">The settings to use during the deserialization and/or conversion of tracks to their engine variants</param>
        /// <param name="drumsInChart">The type of drums parsing to apply to any drums track in the file</param>
        /// <param name="activeInstruments">Provides guidance over which instruments from the midi file to load. If null, all instruments will be loaded.</param>
        /// <returns>The combined chart data from all the midi streams</returns>
        public static YARGChart LoadMidi_Multi(FixedArray<byte> mainData, FixedArray<byte>? updateData, FixedArray<byte>? upgradeData, in SongMetadata metadata, in LoaderSettings settings, DrumsType drumsInChart, HashSet<MidiTrackType>? activeInstruments)
        {
            var mainMidi = YARGMidiFile.Load(mainData);
            var chart = new YARGChart(mainMidi.Resolution, in metadata, in settings, null);
            MidiFiveFretLoader.SetOverdriveMidiNote(chart.Settings.OverdiveMidiNote);

            // Temporary start point. The settings variable may carry that information in the future.
            var encoding = Encoding.UTF8;
            if (updateData != null)
            {
                var updateMidi = YARGMidiFile.Load(updateData);
                LoadTracks_Midi(chart, ref updateMidi, ref encoding, ref drumsInChart, activeInstruments);
            }

            if (upgradeData != null)
            {
                var upgradeMidi = YARGMidiFile.Load(upgradeData);
                LoadTracks_Midi(chart, ref upgradeMidi, ref encoding, ref drumsInChart, activeInstruments);
            }

            LoadTracks_Midi(chart, ref mainMidi, ref encoding, ref drumsInChart, activeInstruments);
            FinalizeDeserialization(chart);
            return chart;
        }

        /// <summary>
        /// Loops through all the tracks present in the provided midifile object
        /// </summary>
        /// <param name="chart">The chart to load data into</param>
        /// <param name="midi">The midifile containing the tracks to load</param>
        /// <param name="encoding">The encoding to use to decode midi text events to lyrics</param>
        /// <param name="drumsInChart">The type of drums parsing to apply to any drums track in the file</param>
        /// <param name="activeInstruments">Provides guidance over which instruments from the midi file to load. If null, all instruments will be loaded.</param>
        private static void LoadTracks_Midi(YARGChart chart, ref YARGMidiFile midi, ref Encoding encoding, ref DrumsType drumsInChart, HashSet<MidiTrackType>? activeInstruments)
        {
            while (midi.GetNextTrack(out var trackNumber, out var track))
            {
                if (!track.FindTrackName(out var trackname))
                {
                    YargLogger.LogInfo($"Duplicate MIDI Track names for Track #{trackNumber}");
                    trackname = TextSpan.Empty;
                }

                string name = trackname.GetString(Encoding.ASCII);
                if (trackNumber == 1)
                {
                    chart.MidiSequenceName = name;
                    LoadSyncTrack_Midi(track, chart.Sync, chart.Resolution);
                    continue;
                }

                if (!YARGMidiTrack.TRACKNAMES.TryGetValue(name, out var type))
                {
                    YargLogger.LogInfo($"Unrecognized MIDI Track #{trackNumber}: {name}");
                    continue;
                }

                // Provides a more algorithmically optimal route for mapping midi ticks to seconds
                var tempoTracker = new TempoTracker(chart.Sync, chart.Resolution);
                if (type == MidiTrackType.Events)
                {
                    LoadEventsTrack_Midi(chart, ref tempoTracker, ref encoding, track);
                }
                else if (type == MidiTrackType.Beat)
                {
                    LoadBeatsTrack_Midi(chart, ref tempoTracker, track);
                }
                else if (activeInstruments == null || activeInstruments.Contains(type))
                {
                    LoadInstrument_Midi(chart, type, ref drumsInChart, ref tempoTracker, track, ref encoding);
                }
            }
        }

        /// <summary>
        /// Loads tempo markers and time signatures from the initial midi track
        /// </summary>
        /// <remarks>
        /// The midi file should be in its default state to work properly.
        /// </remarks>
        /// <param name="midi">The untouched midi file to pull the track from</param>
        /// <returns></returns>
        private static void LoadSyncTrack_Midi(YARGMidiTrack midiTrack, SyncTrack2 sync, long resolution)
        {
            sync.Reset();
            var stats = default(MidiStats);
            while (midiTrack.ParseEvent(ref stats))
            {
                unsafe
                {
                    switch (stats.Type)
                    {
                        case MidiEventType.Tempo:
                            sync.TempoMarkers.GetLastOrAppend(stats.Position)->MicrosPerQuarter = midiTrack.ExtractMicrosPerQuarter();
                            break;
                        case MidiEventType.Time_Sig:
                            sync.TimeSigs.AppendOrUpdate(stats.Position, midiTrack.ExtractTimeSig());
                            break;
                    }
                }
            }
            FinalizeAnchors(sync, resolution);
        }

        private static readonly byte[][] PREFIXES = { Encoding.ASCII.GetBytes("[section "), Encoding.ASCII.GetBytes("[prc_") };
        /// <summary>
        /// Loads sections and global event data from the EVENTS track
        /// </summary>
        /// <param name="chart">The chart that'll hold the sections and globals</param>
        /// <param name="encoding">The encoding to use or update for text</param>
        /// <param name="midiTrack">The midi track containing the event data</param>
        private static void LoadEventsTrack_Midi(YARGChart chart, ref TempoTracker tempoTracker, ref Encoding encoding, YARGMidiTrack midiTrack)
        {
            if (!chart.Globals.IsEmpty() || !chart.Sections.IsEmpty())
            {
                YargLogger.LogInfo("EVENTS track appears multiple times. Not parsing repeats...");
                return;
            }

            var position = default(DualTime);
            var stats = default(MidiStats);
            while (midiTrack.ParseEvent(ref stats))
            {
                if (MidiEventType.Text <= stats.Type && stats.Type <= MidiEventType.Text_EnumLimit && stats.Type != MidiEventType.Text_TrackName)
                {
                    position.Ticks = stats.Position;
                    position.Seconds = tempoTracker.Traverse(position.Ticks);

                    var text = midiTrack.ExtractTextOrSysEx();
                    if (text.StartsWith(PREFIXES[0]))
                    {
                        chart.Sections.GetLastOrAppend(position) = text.GetValidatedString(ref encoding, PREFIXES[0].Length, text.length - PREFIXES[0].Length - 1);
                    }
                    else if (text.StartsWith(PREFIXES[1]))
                    {
                        chart.Sections.GetLastOrAppend(position) = text.GetValidatedString(ref encoding, PREFIXES[1].Length, text.length - PREFIXES[1].Length - 1);
                    }
                    else
                    {
                        chart.Globals.GetLastOrAppend(position).Add(text.GetString(Encoding.ASCII));
                    }
                }
            }
        }

        /// <summary>
        /// Loads the beat track with measure and strong beats from the BEATS track
        /// </summary>
        /// <param name="chart">The chart containing the beat map/param>
        /// <param name="midiTrack">The midi track containing the beat track data</param>
        private static void LoadBeatsTrack_Midi(YARGChart chart, ref TempoTracker tempoTracker, YARGMidiTrack midiTrack)
        {
            const int MEASURE_BEAT = 12;
            const int STRONG_BEAT = 13;
            if (!chart.BeatMap.IsEmpty())
            {
                YargLogger.LogInfo("BEATS track appears multiple times. Not parsing repeats...");
                return;
            }

            var note = default(MidiNote);
            var position = default(DualTime);
            var stats = default(MidiStats);
            while (midiTrack.ParseEvent(ref stats))
            {
                if (stats.Type == MidiEventType.Note_On)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    // No need to handle note offs
                    if (note.Velocity > 0)
                    {
                        position.Ticks = stats.Position;
                        position.Seconds = tempoTracker.Traverse(position.Ticks);
                        switch (note.Value)
                        {
                            case MEASURE_BEAT: chart.BeatMap.AppendOrUpdate(in position, BeatlineType.Measure); break;
                            case STRONG_BEAT:  chart.BeatMap.AppendOrUpdate(in position, BeatlineType.Strong); break;
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
        /// <param name="drumsInChart">
        /// The type of drum parsing to apply to any drums track in the file.<br></br>
        /// If <see cref="DrumsType.FourOrFive"/> or <see cref="DrumsType.ProOrFive"/>, the value will change after parsing a drums track.
        /// </param>
        /// <param name="midiTrack">The track containing the instrument or vocal data</param>
        /// <param name="encoding">The encoding to use to decode midi text events to lyrics</param>
        private static void LoadInstrument_Midi(YARGChart chart, MidiTrackType type, ref DrumsType drumsInChart, ref TempoTracker tempoTracker, in YARGMidiTrack midiTrack, ref Encoding encoding)
        {
            switch (type)
            {
                case MidiTrackType.Guitar_5: MidiFiveFretLoader.Load(midiTrack, chart.FiveFretGuitar, ref tempoTracker); break;
                case MidiTrackType.Bass_5:   MidiFiveFretLoader.Load(midiTrack, chart.FiveFretBass, ref tempoTracker); break;
                case MidiTrackType.Rhythm_5: MidiFiveFretLoader.Load(midiTrack, chart.FiveFretRhythm, ref tempoTracker); break;
                case MidiTrackType.Coop_5:   MidiFiveFretLoader.Load(midiTrack, chart.FiveFretCoopGuitar, ref tempoTracker); break;
                case MidiTrackType.Keys:     MidiFiveFretLoader.Load(midiTrack, chart.Keys, ref tempoTracker); break;

                case MidiTrackType.Drums:
                    switch (drumsInChart)
                    {
                    case DrumsType.FourLane: MidiDrumsLoader.Load(midiTrack, chart.FourLaneDrums, ref tempoTracker, false); break;
                    case DrumsType.ProDrums: MidiDrumsLoader.Load(midiTrack, chart.FourLaneDrums, ref tempoTracker, true); break;
                    case DrumsType.FiveLane: MidiDrumsLoader.Load(midiTrack, chart.FiveLaneDrums, ref tempoTracker); break;
                    default:
                        {
                            var track = MidiDrumsLoader.LoadUnknownDrums(midiTrack, ref tempoTracker, ref drumsInChart);
                            // Only possible if pre-type was FourOrFive AND fifth lane was not found
                            if ((drumsInChart & DrumsType.FourLane) == DrumsType.FourLane)
                            {
                                track.ConvertTo(chart.FourLaneDrums, false);
                                drumsInChart = DrumsType.FourLane;
                            }
                            // Only possible if pre-type was ProOrFive AND fifth lane was not found
                            else if ((drumsInChart & DrumsType.ProDrums) == DrumsType.ProDrums)
                            {
                                track.ConvertTo(chart.FourLaneDrums, true);
                                drumsInChart = DrumsType.ProDrums;
                            }
                            // Only possible if fifth lane is found
                            else
                            {
                                track.ConvertTo(chart.FiveLaneDrums);
                            }
                            // There's no need to call dipose OR the finalizer as everything would've already been transferred or pre-disposed
                            GC.SuppressFinalize(track);
                            break;
                        }
                    }
                    break;
                case MidiTrackType.Guitar_6: MidiSixFretLoader.Load(midiTrack, chart.SixFretGuitar,     ref tempoTracker); break;
                case MidiTrackType.Bass_6:   MidiSixFretLoader.Load(midiTrack, chart.SixFretBass,       ref tempoTracker); break;
                case MidiTrackType.Rhythm_6: MidiSixFretLoader.Load(midiTrack, chart.SixFretRhythm,     ref tempoTracker); break;
                case MidiTrackType.Coop_6:   MidiSixFretLoader.Load(midiTrack, chart.SixFretCoopGuitar, ref tempoTracker); break;

                case MidiTrackType.Pro_Guitar_17: MidiProGuitarLoader.Load(midiTrack, chart.ProGuitar_17Fret, ref tempoTracker); break;
                case MidiTrackType.Pro_Guitar_22: MidiProGuitarLoader.Load(midiTrack, chart.ProGuitar_22Fret, ref tempoTracker); break;
                case MidiTrackType.Pro_Bass_17:   MidiProGuitarLoader.Load(midiTrack, chart.ProBass_17Fret, ref tempoTracker); break;
                case MidiTrackType.Pro_Bass_22:   MidiProGuitarLoader.Load(midiTrack, chart.ProBass_22Fret, ref tempoTracker); break;
                
                case MidiTrackType.Pro_Keys_X:
                case MidiTrackType.Pro_Keys_H:
                case MidiTrackType.Pro_Keys_M: // Handled per-difficulty, so we use 0-3 indexing
                case MidiTrackType.Pro_Keys_E: MidiProKeysLoader.Load(midiTrack, ref tempoTracker, chart.ProKeys, type - MidiTrackType.Pro_Keys_E); break;

                case MidiTrackType.Vocals: MidiVocalsLoader.Load(midiTrack, chart.LeadVocals, 0, ref tempoTracker, ref encoding); break;
                case MidiTrackType.Harm1:
                case MidiTrackType.Harm2: // Handled per-vocal track, so we use 0-2 indexing
                case MidiTrackType.Harm3: MidiVocalsLoader.Load(midiTrack, chart.HarmonyVocals, type - MidiTrackType.Harm1, ref tempoTracker, ref encoding); break;
            }
        }
    }
}

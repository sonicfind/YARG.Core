using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime;
using System.Text;
using YARG.Core.Chart;
using YARG.Core.IO;
using YARG.Core.Logging;
using YARG.Core.NewParsing.Midi;
using YARG.Core.Song;

namespace YARG.Core.NewParsing
{
    public class DotMidiLoader
    {
        public static YARGChart LoadSingle(string filename, in SongMetadata metadata, in LoaderSettings settings, DrumsType drumsInChart, HashSet<MidiTrackType>? activeInstruments)
        {
            using var data = FixedArray.LoadFile(filename);
            return LoadSingle(data, in metadata, in settings, drumsInChart, activeInstruments);
        }

        public static YARGChart LoadSingle(FixedArray<byte> data, in SongMetadata metadata, in LoaderSettings settings, DrumsType drumsInChart, HashSet<MidiTrackType>? activeInstruments)
        {
            var midi = YARGMidiFile.Load(data);
            var chart = new YARGChart(midi.Resolution, in metadata, in settings);
            DualTime.SetTruncationLimit(chart.Settings, (uint) (midi.Resolution / 3));
            MidiFiveFretLoader.SetOverdriveMidiNote(chart.Settings.OverdiveMidiNote);

            var encoding = YARGTextReader.UTF8Strict;
            LoadTracks(chart, null!, ref midi, ref encoding, drumsInChart, activeInstruments);
            YARGChartFinalizer.FinalizeBeats(chart);
            return chart;
        }

        public static YARGChart LoadMulti(FixedArray<byte> mainData, FixedArray<byte>? updateData, FixedArray<byte>? upgradeData, in SongMetadata metadata, in LoaderSettings settings, DrumsType drumsInChart, HashSet<MidiTrackType>? activeInstruments)
        {
            var mainMidi = YARGMidiFile.Load(mainData);
            var chart = new YARGChart(mainMidi.Resolution, in metadata, in settings);
            DualTime.SetTruncationLimit(chart.Settings, (uint) (mainMidi.Resolution / 3));
            MidiFiveFretLoader.SetOverdriveMidiNote(chart.Settings.OverdiveMidiNote);

            var encoding = Encoding.UTF8;
            if (updateData != null)
            {
                var updateMidi = YARGMidiFile.Load(updateData);
                if (updateMidi.GetNextTrack(out _, out var track))
                {
                    var updateSync = new SyncTrack2(updateMidi.Resolution);
                    LoadSyncTrack(track, updateSync);
                    LoadTracks(chart, updateSync, ref updateMidi, ref encoding, drumsInChart, activeInstruments);
                }
            }

            if (upgradeData != null)
            {
                var upgradeMidi = YARGMidiFile.Load(upgradeData);
                if (upgradeMidi.GetNextTrack(out _, out var track))
                {
                    var upgradeSync = new SyncTrack2(upgradeMidi.Resolution);
                    LoadSyncTrack(track, upgradeSync);
                    LoadTracks(chart, upgradeSync, ref upgradeMidi, ref encoding, drumsInChart, activeInstruments);
                }
            }

            LoadTracks(chart, null!, ref mainMidi, ref encoding, drumsInChart, activeInstruments);
            YARGChartFinalizer.FinalizeBeats(chart);
            return chart;
        }

        private static void LoadTracks(YARGChart chart, SyncTrack2 sync, ref YARGMidiFile midi, ref Encoding encoding, DrumsType drumsInChart, HashSet<MidiTrackType>? activeInstruments)
        {
            while (midi.GetNextTrack(out var trackNumber, out var track))
            {
                if (!track.FindTrackName(out var trackname))
                {
                    YargLogger.LogInfo($"Duplicate MIDI Track names for Tack #{trackNumber}");
                    trackname = TextSpan.Empty;
                }

                string name = trackname.GetString(Encoding.ASCII);
                if (trackNumber == 1)
                {
                    chart.MidiSequenceName = name;
                    LoadSyncTrack(track, chart.Sync);
                    sync = chart.Sync;
                    continue;
                }

                if (!YARGMidiTrack.TRACKNAMES.TryGetValue(trackname.GetString(Encoding.ASCII), out var type))
                {
                    YargLogger.LogInfo($"Unrecognized MIDI Track: {name}");
                    continue;
                }

                if (type == MidiTrackType.Events)
                {
                    LoadEventsTrack(chart.Events, sync, ref encoding, track);
                }
                else if (type == MidiTrackType.Beat)
                {
                    LoadBeatsTrack(chart.BeatMap, sync, track);
                }
                else if (activeInstruments == null || activeInstruments.Contains(type))
                {
                    if (type != MidiTrackType.Drums)
                    {
                        LoadInstrument(chart, type, sync, track, ref encoding);
                    }
                    else if (drumsInChart == DrumsType.ProDrums)
                    {
                        chart.ProDrums ??= MidiDrumsLoader.LoadProDrums(track, sync);
                    }
                    else if (drumsInChart == DrumsType.FourLane)
                    {
                        chart.FourLaneDrums ??= MidiDrumsLoader.LoadBasic<FourLane>(track, sync);
                    }
                    else if (drumsInChart == DrumsType.FiveLane)
                    {
                        chart.FiveLaneDrums ??= MidiDrumsLoader.LoadBasic<FiveLane>(track, sync);
                    }
                }
            }
        }

        private static void LoadSyncTrack(YARGMidiTrack midiTrack, SyncTrack2 sync)
        {
            var stats = default(MidiStats);
            while (midiTrack.ParseEvent(ref stats))
            {
                switch (stats.Type)
                {
                    case MidiEventType.Tempo:
                        sync.TempoMarkers.GetLastOrAppend(stats.Position).MicrosPerQuarter = midiTrack.ExtractMicrosPerQuarter();
                        break;
                    case MidiEventType.Time_Sig:
                        sync.TimeSigs.GetLastOrAppend(stats.Position) = midiTrack.ExtractTimeSig();
                        break;
                }
            }
            YARGChartFinalizer.FinalizeAnchors(sync);
        }

        private static readonly byte[][] PREFIXES = { Encoding.ASCII.GetBytes("[section "), Encoding.ASCII.GetBytes("[prc_") };
        private static void LoadEventsTrack(TextEvents2 events, SyncTrack2 sync, ref Encoding encoding, YARGMidiTrack midiTrack)
        {
            if (!events.Globals.IsEmpty() || !events.Sections.IsEmpty())
            {
                YargLogger.LogInfo("EVENTS track appears multiple times. Not parsing repeats...");
                return;
            }

            int tempoIndex = 0;
            var position = default(DualTime);
            var stats = default(MidiStats);
            while (midiTrack.ParseEvent(ref stats))
            {
                if (stats.Type <= MidiEventType.Text_EnumLimit)
                {
                    position.Ticks = stats.Position;
                    position.Seconds = sync.ConvertToSeconds(stats.Position, ref tempoIndex);

                    var text = midiTrack.ExtractTextOrSysEx();
                    if (text.StartsWith(PREFIXES[0]))
                    {
                        events.Sections.GetLastOrAppend(position) = text.GetValidatedString(ref encoding, PREFIXES[0].Length, text.length - PREFIXES[0].Length - 1);
                    }
                    else if (text.StartsWith(PREFIXES[1]))
                    {
                        events.Sections.GetLastOrAppend(position) = text.GetValidatedString(ref encoding, PREFIXES[1].Length, text.length - PREFIXES[1].Length - 1);
                    }
                    else
                    {
                        events.Globals.GetLastOrAppend(position).Add(text.GetString(Encoding.ASCII));
                    }
                }
            }
        }

        private static void LoadBeatsTrack(YARGNativeSortedList<DualTime, BeatlineType> beats, SyncTrack2 sync, YARGMidiTrack midiTrack)
        {
            if (!beats.IsEmpty())
            {
                YargLogger.LogInfo("BEATS track appears multiple times. Not parsing repeats...");
                return;
            }

            // Used to lesson the impact of the ticks-seconds search algorithm as the the position
            // gets larger by tracking the previous position.
            int tempoIndex = 0;
            var note = default(MidiNote);
            var position = default(DualTime);
            var stats = default(MidiStats);
            while (midiTrack.ParseEvent(ref stats))
            {
                if (stats.Type == MidiEventType.Note_On)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    if (note.Velocity > 0)
                    {
                        position.Ticks = stats.Position;
                        position.Seconds = sync.ConvertToSeconds(stats.Position, ref tempoIndex);
                        beats.GetLastOrAppend(position) = note.Value == 12 ? BeatlineType.Measure : BeatlineType.Strong;
                    }
                }
            }
        }

        private static void LoadInstrument(YARGChart chart, MidiTrackType type, SyncTrack2 sync, in YARGMidiTrack midiTrack, ref Encoding encoding)
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
                        MidiProKeysLoader.Load(midiTrack, sync, chart.ProKeys, type - MidiTrackType.Pro_Keys_E);
                        break;
                    }
                case MidiTrackType.Vocals: chart.LeadVocals ??= MidiVocalsLoader.LoadPartVocals(midiTrack, sync, ref encoding); break;
                case MidiTrackType.Harm1:
                case MidiTrackType.Harm2:
                case MidiTrackType.Harm3:
                    {
                        var harmony = chart.HarmonyVocals ??= new VocalTrack2(3);
                        int index = type - MidiTrackType.Harm1;
                        if (harmony[index].IsEmpty())
                        {
                            MidiVocalsLoader.Load(midiTrack, sync, harmony, index, ref encoding);
                        }
                        break;
                    }
            }
        }
    }
}

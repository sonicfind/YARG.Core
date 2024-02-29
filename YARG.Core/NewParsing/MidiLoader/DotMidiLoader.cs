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
            using FileStream stream = new(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            return LoadSingle(stream, in metadata, in settings, drumsInChart, activeInstruments);
        }

        public static YARGChart LoadSingle(Stream stream, in SongMetadata metadata, in LoaderSettings settings, DrumsType drumsInChart, HashSet<MidiTrackType>? activeInstruments)
        {
            var midi = new YARGMidiFile(stream);
            var (sync, sequencename) = LoadSyncTrack(midi);
            var chart = new YARGChart(sync, metadata, settings, sequencename);
            DualTime.SetTruncationLimit(chart.Settings, (uint) (midi.Resolution / 3));
            MidiFiveFretLoader.SetOverdriveMidiNote(chart.Settings.OverdiveMidiNote);

            var encoding = Encoding.UTF8;
            LoadTracks(chart, sync, midi, ref encoding, drumsInChart, activeInstruments);
            YARGChartFinalizer.FinalizeBeats(chart);
            return chart;
        }

        public static YARGChart LoadMulti(Stream mainStream, Stream? updateStream, Stream? upgradeStream, in SongMetadata metadata, in LoaderSettings settings, DrumsType drumsInChart, HashSet<MidiTrackType>? activeInstruments)
        {
            var midi = new YARGMidiFile(mainStream);
            var (sync, sequencename) = LoadSyncTrack(midi);
            var chart = new YARGChart(sync, metadata, settings, sequencename);
            DualTime.SetTruncationLimit(chart.Settings, (uint) (midi.Resolution / 3));
            MidiFiveFretLoader.SetOverdriveMidiNote(chart.Settings.OverdiveMidiNote);

            var encoding = Encoding.UTF8;
            if (updateStream != null)
            {
                var updateMidi = new YARGMidiFile(updateStream);
                var (updateSync, _) = LoadSyncTrack(updateMidi);
                LoadTracks(chart, updateSync, updateMidi, ref encoding, drumsInChart, activeInstruments);
            }

            if (upgradeStream != null)
            {
                var upgradeMidi = new YARGMidiFile(upgradeStream);
                var (upgradeSync, _) = LoadSyncTrack(upgradeMidi);
                LoadTracks(chart, upgradeSync, upgradeMidi, ref encoding, drumsInChart, activeInstruments);
            }

            LoadTracks(chart, sync, midi, ref encoding, drumsInChart, activeInstruments);
            YARGChartFinalizer.FinalizeBeats(chart);
            return chart;
        }

        private static (SyncTrack2 Sync, string SequenceName) LoadSyncTrack(YARGMidiFile midi)
        {
            var sync = new SyncTrack2(midi.Resolution);

            var midiTrack = midi.LoadNextTrack()!;
            string sequenceName = midiTrack.FindTrackName(Encoding.UTF8)!;
            while (midiTrack.ParseEvent())
            {
                switch (midiTrack.Type)
                {
                    case MidiEventType.Tempo:
                        sync.TempoMarkers.GetLastOrAppend(midiTrack.Position).MicrosPerQuarter = midiTrack.ExtractMicrosPerQuarter();
                        break;
                    case MidiEventType.Time_Sig:
                        sync.TimeSigs.GetLastOrAppend(midiTrack.Position) = midiTrack.ExtractTimeSig();
                        break;
                }
            }
            YARGChartFinalizer.FinalizeAnchors(sync);
            return (sync, sequenceName);
        }

        private static void LoadTracks(YARGChart chart, SyncTrack2 sync, YARGMidiFile midi, ref Encoding encoding, DrumsType drumsInChart, HashSet<MidiTrackType>? activeInstruments)
        {
            foreach (var midiTrack in midi)
            {
                string trackname = midiTrack.FindTrackName(Encoding.UTF8)!;
                if (!YARGMidiTrack.TRACKNAMES.TryGetValue(trackname, out var type))
                {
                    YargLogger.LogInfo($"Unrecognized MIDI Track: {trackname}");
                    continue;
                }

                if (type == MidiTrackType.Events)
                    LoadEventsTrack(chart.Events, sync, midiTrack);
                else if (type == MidiTrackType.Beat)
                    LoadBeatsTrack(chart.BeatMap, sync, midiTrack);
                else
                {
                    if (activeInstruments == null || activeInstruments.Contains(type))
                    {
                        if (type != MidiTrackType.Drums)
                        {
                            LoadInstrument(chart, type, sync, midiTrack, ref encoding);
                        }
                        else if (drumsInChart == DrumsType.ProDrums)
                        {
                            chart.ProDrums ??= MidiDrumsLoader.LoadProDrums(midiTrack, sync);
                        }
                        else if (drumsInChart == DrumsType.FourLane)
                        {
                            chart.FourLaneDrums ??= MidiDrumsLoader.LoadBasic<FourLane>(midiTrack, sync);
                        }
                        else if (drumsInChart == DrumsType.FiveLane)
                        {
                            chart.FiveLaneDrums ??= MidiDrumsLoader.LoadBasic<FiveLane>(midiTrack, sync);
                        }
                    }
                }
            }
        }

        private static readonly byte[][] PREFIXES = { Encoding.ASCII.GetBytes("[section "), Encoding.ASCII.GetBytes("[prc_") };
        private static void LoadEventsTrack(TextEvents2 events, SyncTrack2 sync, YARGMidiTrack midiTrack)
        {
            if (!events.IsOccupied())
            {
                YargLogger.LogInfo("EVENTS track appears multiple times. Not parsing repeats...");
                return;
            }

            int tempoIndex = 0;
            var position = default(DualTime);
            while (midiTrack.ParseEvent())
            {
                if (midiTrack.Type <= MidiEventType.Text_EnumLimit)
                {
                    position.Ticks = midiTrack.Position;
                    position.Seconds = sync.ConvertToSeconds(midiTrack.Position, ref tempoIndex);

                    var bytes = midiTrack.ExtractTextOrSysEx();
                    if (bytes.StartsWith(PREFIXES[0]))
                    {
                        events.Sections.GetLastOrAppend(position) = Encoding.UTF8.GetString(bytes[PREFIXES[0].Length..(bytes.Length - 1)]);
                    }
                    else if (bytes.StartsWith(PREFIXES[1]))
                    {
                        events.Sections.GetLastOrAppend(position) = Encoding.UTF8.GetString(bytes[PREFIXES[1].Length..(bytes.Length - 1)]);
                    }
                    else
                    {
                        events.Globals.GetLastOrAppend(position).Add(Encoding.UTF8.GetString(bytes));
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
            while (midiTrack.ParseEvent())
            {
                if (midiTrack.Type == MidiEventType.Note_On)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    if (note.velocity > 0)
                    {
                        position.Ticks = midiTrack.Position;
                        position.Seconds = sync.ConvertToSeconds(midiTrack.Position, ref tempoIndex);
                        beats.GetLastOrAppend(position) = note.value == 12 ? BeatlineType.Measure : BeatlineType.Strong;
                    }
                }
            }
        }

        private static void LoadInstrument(YARGChart chart, MidiTrackType type, SyncTrack2 sync, YARGMidiTrack midiTrack, ref Encoding encoding) 
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

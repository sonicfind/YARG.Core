﻿using System.Collections.Generic;
using System.Text;
using YARG.Core.Chart;
using YARG.Core.IO;
using YARG.Core.IO.Ini;
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
            var modifiers = new IniModifierCollection();
            return LoadSingle(in data, in metadata, in settings, modifiers, drumsInChart, activeInstruments);
        }

        public static YARGChart LoadSingle(in FixedArray<byte> data, in SongMetadata metadata, in LoaderSettings settings, IniModifierCollection? modifiers, DrumsType drumsInChart, HashSet<MidiTrackType>? activeInstruments)
        {
            var midi = new YARGMidiFile(in data);
            var chart = new YARGChart(midi.Resolution, in metadata, in settings, modifiers);
            DualTime.SetTruncationLimit(chart.Settings, (uint) (midi.Resolution / 3));
            MidiFiveFretLoader.SetOverdriveMidiNote(chart.Settings.OverdiveMidiNote);

            var encoding = YARGTextReader.UTF8Strict;
            LoadTracks(chart, ref midi, ref encoding, drumsInChart, activeInstruments);
            YARGChartFinalizer.FinalizeBeats(chart);
            return chart;
        }

        public static YARGChart LoadMulti(in FixedArray<byte> mainData, in FixedArray<byte> updateData, in FixedArray<byte> upgradeData, in SongMetadata metadata, in LoaderSettings settings, DrumsType drumsInChart, HashSet<MidiTrackType>? activeInstruments)
        {
            var mainMidi = new YARGMidiFile(in mainData);
            var chart = new YARGChart(mainMidi.Resolution, in metadata, in settings, null);
            DualTime.SetTruncationLimit(chart.Settings, (uint) (mainMidi.Resolution / 3));
            MidiFiveFretLoader.SetOverdriveMidiNote(chart.Settings.OverdiveMidiNote);

            var encoding = Encoding.UTF8;
            if (updateData.IsAllocated)
            {
                var updateMidi = new YARGMidiFile(in updateData);
                LoadTracks(chart, ref updateMidi, ref encoding, drumsInChart, activeInstruments);
            }

            if (upgradeData.IsAllocated)
            {
                var upgradeMidi = new YARGMidiFile(in upgradeData);
                LoadTracks(chart, ref upgradeMidi, ref encoding, drumsInChart, activeInstruments);
            }

            LoadTracks(chart, ref mainMidi, ref encoding, drumsInChart, activeInstruments);
            YARGChartFinalizer.FinalizeBeats(chart);
            return chart;
        }

        private static void LoadTracks(YARGChart chart, ref YARGMidiFile midi, ref Encoding encoding, DrumsType drumsInChart, HashSet<MidiTrackType>? activeInstruments)
        {
            foreach (var midiTrack in midi)
            {
                if (!midiTrack.FindTrackName(out var trackname))
                {
                    YargLogger.LogInfo($"Duplicate MIDI Track names for Track #{midi.TrackNumber}");
                    trackname = TextSpan.Empty;
                }

                string name = trackname.GetString(Encoding.ASCII);
                if (midi.TrackNumber == 1)
                {
                    chart.MidiSequenceName = name;
                    LoadSyncTrack(midiTrack, chart.Sync);
                    continue;
                }

                if (!YARGMidiTrack.TRACKNAMES.TryGetValue(name, out var type))
                {
                    YargLogger.LogInfo($"Unrecognized MIDI Track: {name}");
                    continue;
                }

                if (type == MidiTrackType.Events)
                {
                    LoadEventsTrack(chart.Events, chart.Sync, ref encoding, midiTrack);
                }
                else if (type == MidiTrackType.Beat)
                {
                    LoadBeatsTrack(chart.BeatMap, chart.Sync, midiTrack);
                }
                else if (activeInstruments == null || activeInstruments.Contains(type))
                {
                    if (type != MidiTrackType.Drums)
                    {
                        LoadInstrument(chart, type, midiTrack, ref encoding);
                    }
                    else if (drumsInChart == DrumsType.ProDrums)
                    {
                        chart.ProDrums ??= MidiDrumsLoader.LoadProDrums(midiTrack, chart.Sync);
                    }
                    else if (drumsInChart == DrumsType.FourLane)
                    {
                        chart.FourLaneDrums ??= MidiDrumsLoader.LoadBasic<FourLane>(midiTrack, chart.Sync);
                    }
                    else if (drumsInChart == DrumsType.FiveLane)
                    {
                        chart.FiveLaneDrums ??= MidiDrumsLoader.LoadBasic<FiveLane>(midiTrack, chart.Sync);
                    }
                }
            }
        }

        private static void LoadSyncTrack(YARGMidiTrack midiTrack, SyncTrack2 sync)
        {
            sync.Reset();
            var stats = default(YARGMidiTrack.Stats);
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
            var stats = default(YARGMidiTrack.Stats);
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
            var stats = default(YARGMidiTrack.Stats);
            while (midiTrack.ParseEvent(ref stats))
            {
                if (stats.Type == MidiEventType.Note_On)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    if (note.velocity > 0)
                    {
                        position.Ticks = stats.Position;
                        position.Seconds = sync.ConvertToSeconds(stats.Position, ref tempoIndex);
                        beats.GetLastOrAppend(position) = note.value == 12 ? BeatlineType.Measure : BeatlineType.Strong;
                    }
                }
            }
        }

        private static void LoadInstrument(YARGChart chart, MidiTrackType type, in YARGMidiTrack midiTrack, ref Encoding encoding) 
        {
            switch (type)
            {
                case MidiTrackType.Guitar_5:      chart.FiveFretGuitar ??=     MidiFiveFretLoader.Load(midiTrack, chart.Sync); break;
                case MidiTrackType.Bass_5:        chart.FiveFretBass ??=       MidiFiveFretLoader.Load(midiTrack, chart.Sync); break;
                case MidiTrackType.Rhythm_5:      chart.FiveFretRhythm ??=     MidiFiveFretLoader.Load(midiTrack, chart.Sync); break;
                case MidiTrackType.Coop_5:        chart.FiveFretCoopGuitar ??= MidiFiveFretLoader.Load(midiTrack, chart.Sync); break;
                case MidiTrackType.Keys:          chart.Keys ??=               MidiFiveFretLoader.Load(midiTrack, chart.Sync); break;

                case MidiTrackType.Guitar_6:      chart.SixFretGuitar ??=      MidiSixFretLoader. Load(midiTrack, chart.Sync); break;
                case MidiTrackType.Bass_6:        chart.SixFretBass ??=        MidiSixFretLoader. Load(midiTrack, chart.Sync); break;
                case MidiTrackType.Rhythm_6:      chart.SixFretRhythm ??=      MidiSixFretLoader. Load(midiTrack, chart.Sync); break;
                case MidiTrackType.Coop_6:        chart.SixFretCoopGuitar ??=  MidiSixFretLoader. Load(midiTrack, chart.Sync); break;

                case MidiTrackType.Pro_Guitar_17: chart.ProGuitar_17Fret ??=   MidiProGuitarLoader.Load<ProFret_17>(midiTrack, chart.Sync); break;
                case MidiTrackType.Pro_Guitar_22: chart.ProGuitar_22Fret ??=   MidiProGuitarLoader.Load<ProFret_22>(midiTrack, chart.Sync); break;
                case MidiTrackType.Pro_Bass_17:   chart.ProBass_17Fret ??=     MidiProGuitarLoader.Load<ProFret_17>(midiTrack, chart.Sync); break;
                case MidiTrackType.Pro_Bass_22:   chart.ProBass_22Fret ??=     MidiProGuitarLoader.Load<ProFret_22>(midiTrack, chart.Sync); break;
                
                case MidiTrackType.Pro_Keys_X:
                case MidiTrackType.Pro_Keys_H:
                case MidiTrackType.Pro_Keys_M:
                case MidiTrackType.Pro_Keys_E:
                    {
                        chart.ProKeys ??= new InstrumentTrack2<ProKeysDifficultyTrack>();
                        MidiProKeysLoader.Load(midiTrack, chart.Sync, chart.ProKeys, type - MidiTrackType.Pro_Keys_E);
                        break;
                    }
                case MidiTrackType.Vocals: chart.LeadVocals ??= MidiVocalsLoader.LoadPartVocals(midiTrack, chart.Sync, ref encoding); break;
                case MidiTrackType.Harm1:
                case MidiTrackType.Harm2:
                case MidiTrackType.Harm3:
                    {
                        var harmony = chart.HarmonyVocals ??= new VocalTrack2(3);
                        int index = type - MidiTrackType.Harm1;
                        if (harmony[index].IsEmpty())
                        {
                            MidiVocalsLoader.Load(midiTrack, chart.Sync, harmony, index, ref encoding);
                        }
                        break;
                    }
            }
        }
    }
}

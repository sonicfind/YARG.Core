using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.IO;
using YARG.Core.IO.Ini;
using YARG.Core.Parsing.Drums;
using YARG.Core.Parsing.ProGuitar;
using YARG.Core.Parsing.ProKeys;
using System.IO;
using YARG.Core.Chart;
using YARG.Core.Parsing.Midi;

namespace YARG.Core.Parsing
{
    public static class DotMidiLoader
    {
        public static YARGChart LoadFull(string filename, ParseSettings settings, Dictionary<MidiTrackType, HashSet<Difficulty>>? activeInstruments)
        {
            using FileStream stream = new(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            return LoadFull(stream, settings, activeInstruments);
        }

        public static YARGChart LoadFull(Stream stream, ParseSettings settings, Dictionary<MidiTrackType, HashSet<Difficulty>>? activeInstruments)
        {
            YARGChart chart = new();
            DrumTrackHandler drums = new(settings.DrumsType);
            MidiTrackLoader.SetMultiplierNote(settings.StarPowerNote);
            LoadTracks(chart, drums, stream, settings.SustainCutoffThreshold, activeInstruments);
            return chart;
        }

        public static void LoadTracks(YARGChart chart, DrumTrackHandler drums, Stream stream, long sustainCutoff, Dictionary<MidiTrackType, HashSet<Difficulty>>? activeInstruments)
        {
            YARGMidiFile midiFile = new(stream);
            chart.Sync.Tickrate = midiFile.Tickrate;
            SetSustainThreshold(midiFile.Tickrate, sustainCutoff);
            foreach (var midiTrack in midiFile)
            {
                if (midiFile.TrackNumber == 1)
                {
                    if (midiTrack.Type == MidiEventType.Text_TrackName)
                        chart.MidiSequenceName = Encoding.UTF8.GetString(midiTrack.ExtractTextOrSysEx());
                    LoadSyncTrack(chart.Sync, midiTrack);
                    continue;
                }

                string name = Encoding.ASCII.GetString(midiTrack.ExtractTextOrSysEx());
                if (!YARGMidiTrack.TRACKNAMES.TryGetValue(name, out var type))
                {
                    YargTrace.LogInfo($"Unrecognized MIDI Track: {name}");
                    continue;
                }

                if (type == MidiTrackType.Events)
                    LoadEventsTrack(chart.Events, chart.Sync, midiTrack);
                else if (type == MidiTrackType.Beat)
                    LoadBeatsTrack(chart.Sync, midiTrack);
                else
                {
                    HashSet<Difficulty>? difficulties = null;
                    if (activeInstruments == null || activeInstruments.TryGetValue(type, out difficulties))
                    {
                        if (type != MidiTrackType.Drums)
                            LoadInstrument(chart, type, chart.Sync, midiTrack, difficulties);
                        else
                            drums.LoadMidi(midiTrack, chart.Sync, difficulties);
                    }
                }
            }

            var drumsTrack = drums.GetMidiTrack();
            if (drumsTrack != null)
            {
                switch (drums.Type)
                {
                    case DrumsType.ProDrums: chart.ProDrums =      (InstrumentTrack_FW<DrumNote<DrumPad_4, Pro_Drums>>)   drumsTrack; break;
                    case DrumsType.FiveLane: chart.FiveLaneDrums = (InstrumentTrack_FW<DrumNote<DrumPad_5, Basic_Drums>>) drumsTrack; break;
                    case DrumsType.FourLane: chart.FourLaneDrums = (InstrumentTrack_FW<DrumNote<DrumPad_4, Basic_Drums>>) drumsTrack; break;
                }
            }

            YARGChartFinalizer.FinalizeBeats(chart);
        }

        public static void PreloadTracks(YARGChart chart, DrumTrackHandler drums, Stream stream, long sustainCutoff, Dictionary<MidiTrackType, HashSet<Difficulty>>? activeInstruments)
        {
            YARGMidiFile midiFile = new(stream);
            SetSustainThreshold(midiFile.Tickrate, sustainCutoff);

            SyncTrack_FW sync = new();
            foreach (var midiTrack in midiFile)
            {
                if (midiFile.TrackNumber == 1)
                {
                    if (midiTrack.Type == MidiEventType.Text_TrackName)
                        chart.MidiSequenceName = Encoding.UTF8.GetString(midiTrack.ExtractTextOrSysEx());
                    LoadSyncTrack(sync, midiTrack);
                    continue;
                }

                string name = Encoding.ASCII.GetString(midiTrack.ExtractTextOrSysEx());
                if (YARGMidiTrack.TRACKNAMES.TryGetValue(name, out var type))
                {
                    if (type != MidiTrackType.Events && type != MidiTrackType.Beat)
                    {
                        HashSet<Difficulty>? difficulties = null;
                        if (activeInstruments == null || activeInstruments.TryGetValue(type, out difficulties))
                        {
                            if (type != MidiTrackType.Drums)
                                LoadInstrument(chart, type, sync, midiTrack, difficulties);
                            else
                                drums.LoadMidi(midiTrack, sync, difficulties);
                        }
                    }
                }
                else
                    YargTrace.LogInfo($"Unrecognized MIDI Track (in preparation): {name}");
            }
        }

        private static void LoadSyncTrack(SyncTrack_FW sync, YARGMidiTrack midiTrack)
        {
            while (midiTrack.ParseEvent(true))
            {
                switch (midiTrack.Type)
                {
                    case MidiEventType.Tempo:
                        sync.TempoMarkers.Get_Or_Add_Last(midiTrack.Position).Micros = midiTrack.ExtractMicrosPerQuarter();
                        break;
                    case MidiEventType.Time_Sig:
                        sync.TimeSigs.Get_Or_Add_Last(midiTrack.Position) = midiTrack.ExtractTimeSig();
                        break;
                }
            }
            YARGChartFinalizer.FinalizeTempoMap(sync);
        }

        internal static readonly byte[][] PREFIXES = { Encoding.ASCII.GetBytes("[section "), Encoding.ASCII.GetBytes("[prc_") };
        private static void LoadEventsTrack(SongEvents events, SyncTrack_FW sync, YARGMidiTrack midiTrack)
        {
            if (!events.globals.IsEmpty() || !events.sections.IsEmpty())
            {
                YargTrace.LogInfo("EVENTS track appears multiple times. Not parsing repeats...");
                return;
            }

            // Used to lesson the impact of the ticks-seconds search algorithm as the the position
            // gets larger by tracking the previous position.
            int tempoIndex = 0;
            while (midiTrack.ParseEvent(true))
            {
                if (midiTrack.Type <= MidiEventType.Text_EnumLimit)
                {
                    var dual = new DualTime(midiTrack.Position, sync.ConvertToSeconds(midiTrack.Position, ref tempoIndex));
                    var bytes = midiTrack.ExtractTextOrSysEx();
                    if (bytes.StartsWith(PREFIXES[0]))
                        events.sections.Get_Or_Add_Last(dual) = Encoding.UTF8.GetString(bytes[PREFIXES[0].Length..(bytes.Length - 1)]);
                    else if (bytes.StartsWith(PREFIXES[1]))
                        events.sections.Get_Or_Add_Last(dual) = Encoding.UTF8.GetString(bytes[PREFIXES[1].Length..(bytes.Length - 1)]);
                    else
                        events.globals.Get_Or_Add_Last(dual).Add(Encoding.UTF8.GetString(bytes));
                }
            }
        }

        private static void LoadBeatsTrack(SyncTrack_FW sync, YARGMidiTrack midiTrack)
        {
            var beats = sync.BeatMap;
            if (!beats.IsEmpty())
            {
                YargTrace.LogInfo("BEATS track appears multiple times. Not parsing repeats...");
                return;
            }

            // Used to lesson the impact of the ticks-seconds search algorithm as the the position
            // gets larger by tracking the previous position.
            int tempoIndex = 0;
            MidiNote note = new();
            while (midiTrack.ParseEvent(true))
            {
                if (midiTrack.Type == MidiEventType.Note_On)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    if (note.velocity > 0)
                    {
                        var beat = new DualTime(midiTrack.Position, sync.ConvertToSeconds(midiTrack.Position, ref tempoIndex));
                        beats.Get_Or_Add_Last(beat) = note.value == 12 ? BeatlineType.Measure : BeatlineType.Strong;
                    }
                }
            }
        }

        private static void LoadInstrument(YARGChart chart, MidiTrackType type, SyncTrack_FW sync, YARGMidiTrack midiTrack, HashSet<Difficulty>? difficulties) 
        {
            switch (type)
            {
                case MidiTrackType.Guitar_5:      chart.FiveFretGuitar ??=     Midi_FiveFretLoader.Load(midiTrack, sync, difficulties); break;
                case MidiTrackType.Bass_5:        chart.FiveFretBass ??=       Midi_FiveFretLoader.Load(midiTrack, sync, difficulties); break;
                case MidiTrackType.Rhythm_5:      chart.FiveFretRhythm ??=     Midi_FiveFretLoader.Load(midiTrack, sync, difficulties); break;
                case MidiTrackType.Coop_5:        chart.FiveFretCoopGuitar ??= Midi_FiveFretLoader.Load(midiTrack, sync, difficulties); break;
                case MidiTrackType.Guitar_6:      chart.SixFretGuitar ??=      Midi_SixFretLoader. Load(midiTrack, sync, difficulties); break;
                case MidiTrackType.Bass_6:        chart.SixFretBass ??=        Midi_SixFretLoader. Load(midiTrack, sync, difficulties); break;
                case MidiTrackType.Rhythm_6:      chart.SixFretRhythm ??=      Midi_SixFretLoader. Load(midiTrack, sync, difficulties); break;
                case MidiTrackType.Coop_6:        chart.SixFretCoopGuitar ??=  Midi_SixFretLoader. Load(midiTrack, sync, difficulties); break;
                case MidiTrackType.Pro_Guitar_17: chart.ProGuitar_17Fret ??=   Midi_ProGuitar_Loader<ProFret_17>.Load(midiTrack, sync, difficulties); break;
                case MidiTrackType.Pro_Guitar_22: chart.ProGuitar_22Fret ??=   Midi_ProGuitar_Loader<ProFret_22>.Load(midiTrack, sync, difficulties); break;
                case MidiTrackType.Pro_Bass_17:   chart.ProBass_17Fret ??=     Midi_ProGuitar_Loader<ProFret_17>.Load(midiTrack, sync, difficulties); break;
                case MidiTrackType.Pro_Bass_22:   chart.ProBass_22Fret ??=     Midi_ProGuitar_Loader<ProFret_22>.Load(midiTrack, sync, difficulties); break;
                case MidiTrackType.Keys:          chart.Keys ??=               Midi_KeysLoader.Load(midiTrack, sync, difficulties); break;
                case MidiTrackType.Pro_Keys_X:
                case MidiTrackType.Pro_Keys_H:
                case MidiTrackType.Pro_Keys_M:
                case MidiTrackType.Pro_Keys_E:
                    {
                        int index = type - MidiTrackType.Pro_Keys_E;
                        if (difficulties != null && !difficulties.Contains((Difficulty) index))
                            break;

                        chart.ProKeys ??= new InstrumentTrack_Base<ProKeysDifficulty>();
                        chart.ProKeys[index] ??= Midi_ProKeys_Loader.Load(midiTrack, sync);
                        break;
                    }
                case MidiTrackType.Vocals: chart.LeadVocals ??= MidiVocalLoader.LoadLeadVocals(midiTrack, sync); break;
                case MidiTrackType.Harm1:
                case MidiTrackType.Harm2:
                case MidiTrackType.Harm3:
                    {
                        var harmony = chart.HarmonyVocals ??= new VocalTrack_FW(3);
                        int index = type - MidiTrackType.Harm1;
                        if (harmony[index].IsEmpty())
                            MidiVocalLoader.LoadHarmonyVocals(harmony, index, midiTrack, sync);
                        break;
                    }
            }
        }

        private static void SetSustainThreshold(uint tickRate, long sustainCutoff)
        {
            DualTime.TruncationLimit = sustainCutoff != ParseSettings.SETTING_DEFAULT ? sustainCutoff : (tickRate / 3);
        }
    }
}

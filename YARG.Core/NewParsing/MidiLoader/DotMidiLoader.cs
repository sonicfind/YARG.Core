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

namespace YARG.Core.NewParsing
{
    public partial class YARGChart
    {
        public static YARGChart LoadMidi_Single(FileInfo chartInfo, Dictionary<MidiTrackType, HashSet<Difficulty>?>? activeInstruments)
        {
            var iniInfo = new FileInfo(Path.Combine(chartInfo.DirectoryName, "song.ini"));

            SongMetadata metadata;
            ParseSettings settings;
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
                        drums = DrumsType.Unknown;
                    }
                }
                else if (modifiers.TryGet("pro_drums", out bool prodrums) && !prodrums)
                {
                    drums = DrumsType.Unknown;
                }
                settings = new ParseSettings(modifiers, drums);
            }
            else
            {
                metadata = SongMetadata.Default;
                settings = ParseSettings.Default;
            }

            using var file = MemoryMappedArray.Load(chartInfo);
            return LoadMidi_Single(file.ToStream(), metadata, settings, activeInstruments);
        }

        public static YARGChart LoadMidi_Single(Stream stream, in SongMetadata metadata, in ParseSettings settings, Dictionary<MidiTrackType, HashSet<Difficulty>?>? activeInstruments)
        {
            var midi = new YARGMidiFile(stream);
            var (sync, sequencename) = LoadSyncTrack_Midi(midi);
            var chart = new YARGChart(sync, metadata, settings, sequencename);
            DualTime.SetTruncationLimit(chart.Settings, (uint) (midi.TickRate / 3));
            MidiTrackLoader.SetMultiplierNote(chart.Settings.StarPowerNote);

            LoadMidiTracks(chart, sync, midi, activeInstruments);
            YARGChartFinalizer.FinalizeBeats(chart);
            return chart;
        }

        public static YARGChart LoadMidi_Multi(Stream mainStream, Stream? updateStream, Stream? upgradeStream, in SongMetadata metadata, in ParseSettings settings, Dictionary<MidiTrackType, HashSet<Difficulty>?>? activeInstruments)
        {
            var midi = new YARGMidiFile(mainStream);
            var (sync, sequencename) = LoadSyncTrack_Midi(midi);
            var chart = new YARGChart(sync, metadata, settings, sequencename);
            DualTime.SetTruncationLimit(chart.Settings, (uint) (midi.TickRate / 3));
            MidiTrackLoader.SetMultiplierNote(chart.Settings.StarPowerNote);

            if (updateStream != null)
            {
                var updateMidi = new YARGMidiFile(updateStream);
                var (updateSync, _) = LoadSyncTrack_Midi(updateMidi);
                LoadMidiTracks(chart, updateSync, updateMidi, activeInstruments);
            }

            if (upgradeStream != null)
            {
                var upgradeMidi = new YARGMidiFile(upgradeStream);
                var (upgradeSync, _) = LoadSyncTrack_Midi(upgradeMidi);
                LoadMidiTracks(chart, upgradeSync, upgradeMidi, activeInstruments);
            }

            LoadMidiTracks(chart, sync, midi, activeInstruments);
            YARGChartFinalizer.FinalizeBeats(chart);
            return chart;
        }

        private static (SyncTrack2 Sync, string SequenceName) LoadSyncTrack_Midi(YARGMidiFile midi)
        {
            var sync = new SyncTrack2(midi.TickRate);

            using var midiTrack = midi.LoadNextTrack()!;
            string sequenceName = midiTrack.FindTrackName(Encoding.UTF8)!;
            var midiEvent = MidiEvent.Default;
            while (midiTrack.ParseEvent(true, ref midiEvent))
            {
                switch (midiEvent.Type)
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

        private static void LoadMidiTracks(YARGChart chart, SyncTrack2 sync, YARGMidiFile midi, Dictionary<MidiTrackType, HashSet<Difficulty>?>? activeInstruments)
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
                    LoadEventsTrack_Midi(chart.Events, sync, midiTrack);
                else if (type == MidiTrackType.Beat)
                    LoadBeatsTrack_Midi(chart.BeatMap, sync, midiTrack);
                else
                {
                    HashSet<Difficulty>? difficulties = null;
                    if (activeInstruments == null || activeInstruments.TryGetValue(type, out difficulties))
                    {
                        LoadInstrument_Midi(chart, type, sync, midiTrack, difficulties);
                    }
                }
            }
        }

        private static readonly byte[][] PREFIXES = { Encoding.ASCII.GetBytes("[section "), Encoding.ASCII.GetBytes("[prc_") };
        private static void LoadEventsTrack_Midi(TextEvents2 events, SyncTrack2 sync, YARGMidiTrack midiTrack)
        {
            if (!events.IsEmpty())
            {
                YargLogger.LogInfo("EVENTS track appears multiple times. Not parsing repeats...");
                return;
            }

            int tempoIndex = 0;
            var position = default(DualTime);
            var midiEvent = MidiEvent.Default;
            while (midiTrack.ParseEvent(true, ref midiEvent))
            {
                if (midiEvent.Type <= MidiEventType.Text_EnumLimit)
                {
                    position.Ticks = midiTrack.Position;
                    position.Seconds = sync.ConvertToSeconds(midiTrack.Position, ref tempoIndex);

                    var bytes = midiTrack.ExtractTextOrSysEx(in midiEvent);
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

        private static void LoadBeatsTrack_Midi(YARGNativeSortedList<DualTime, BeatlineType> beats, SyncTrack2 sync, YARGMidiTrack midiTrack)
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
            var midiEvent = MidiEvent.Default;
            while (midiTrack.ParseEvent(true, ref midiEvent))
            {
                if (midiEvent.Type == MidiEventType.Note_On)
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

        private static void LoadInstrument_Midi(YARGChart chart, MidiTrackType type, SyncTrack2 sync, YARGMidiTrack midiTrack, HashSet<Difficulty>? difficulties) 
        {
            switch (type)
            {
                case MidiTrackType.Guitar_5:      chart.FiveFretGuitar ??=     MidiFiveFretLoader.Load(midiTrack, sync, difficulties); break;
                case MidiTrackType.Bass_5:        chart.FiveFretBass ??=       MidiFiveFretLoader.Load(midiTrack, sync, difficulties); break;
                case MidiTrackType.Rhythm_5:      chart.FiveFretRhythm ??=     MidiFiveFretLoader.Load(midiTrack, sync, difficulties); break;
                case MidiTrackType.Coop_5:        chart.FiveFretCoopGuitar ??= MidiFiveFretLoader.Load(midiTrack, sync, difficulties); break;

                case MidiTrackType.Guitar_6:      chart.SixFretGuitar ??=      MidiSixFretLoader. Load(midiTrack, sync, difficulties); break;
                case MidiTrackType.Bass_6:        chart.SixFretBass ??=        MidiSixFretLoader. Load(midiTrack, sync, difficulties); break;
                case MidiTrackType.Rhythm_6:      chart.SixFretRhythm ??=      MidiSixFretLoader. Load(midiTrack, sync, difficulties); break;
                case MidiTrackType.Coop_6:        chart.SixFretCoopGuitar ??=  MidiSixFretLoader. Load(midiTrack, sync, difficulties); break;

                case MidiTrackType.Drums:
                    switch (chart.Settings.DrumsType)
                    {
                        case DrumsType.FourLane:
                            chart.FourLaneDrums ??= MidiFourLaneLoader.Load(midiTrack, sync, difficulties);
                            break;
                        case DrumsType.ProDrums:
                            chart.ProDrums ??= MidiProDrumsLoader.Load(midiTrack, sync, difficulties);
                            break;
                        case DrumsType.FiveLane:
                            chart.FiveLaneDrums ??= MidiFiveLaneLoader.Load(midiTrack, sync, difficulties);
                            break;
                        default:
                            // No `using/dipose` as events & phrases need to persist
                            var track = MidiUnkownDrumsLoader.Load(midiTrack, sync, difficulties, ref chart.Settings.DrumsType);
                            switch (chart.Settings.DrumsType)
                            {
                                case DrumsType.FourLane:
                                    chart.FourLaneDrums = track.Convert<FourLane<DrumPad>>();
                                    break;
                                case DrumsType.ProDrums:
                                    chart.ProDrums = track.Convert();
                                    break;
                                case DrumsType.FiveLane:
                                    chart.FiveLaneDrums = track.Convert<FiveLane<DrumPad>>();
                                    break;
                            }
                            break;
                    }
                    break;
                case MidiTrackType.Pro_Guitar_17: chart.ProGuitar_17Fret ??=   MidiProGuitarLoader<ProFret_17>.Load(midiTrack, sync, difficulties); break;
                case MidiTrackType.Pro_Guitar_22: chart.ProGuitar_22Fret ??=   MidiProGuitarLoader<ProFret_22>.Load(midiTrack, sync, difficulties); break;
                case MidiTrackType.Pro_Bass_17:   chart.ProBass_17Fret ??=     MidiProGuitarLoader<ProFret_17>.Load(midiTrack, sync, difficulties); break;
                case MidiTrackType.Pro_Bass_22:   chart.ProBass_22Fret ??=     MidiProGuitarLoader<ProFret_22>.Load(midiTrack, sync, difficulties); break;
                case MidiTrackType.Keys:          chart.Keys ??=               MidiKeysLoader.Load(midiTrack, sync, difficulties); break;
                case MidiTrackType.Pro_Keys_X:
                case MidiTrackType.Pro_Keys_H:
                case MidiTrackType.Pro_Keys_M:
                case MidiTrackType.Pro_Keys_E:
                    {
                        chart.ProKeys ??= new InstrumentTrack2<ProKeysDifficultyTrack>();
                        chart.ProKeys[type - MidiTrackType.Pro_Keys_E] ??= MidiProKeysLoader.Load(midiTrack, sync);
                        break;
                    }
                case MidiTrackType.Vocals: chart.LeadVocals ??= MidiVocalsLoader.LoadLeadVocals(midiTrack, sync); break;
                case MidiTrackType.Harm1:
                case MidiTrackType.Harm2:
                case MidiTrackType.Harm3:
                    {
                        var harmony = chart.HarmonyVocals ??= new VocalTrack2(3);
                        int index = type - MidiTrackType.Harm1;
                        if (harmony[index].IsEmpty())
                        {
                            MidiVocalsLoader.LoadVocalTrack(midiTrack, sync, harmony, index);
                        }
                        break;
                    }
            }
        }
    }
}

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

namespace YARG.Core.NewParsing
{
    public partial class YARGChart
    {
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
                    chart.Settings.HopoThreshold = chart.Sync.Tickrate / (eighthNoteHopo ? 2 : 3);
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
                    chart.Settings.HopoThreshold = 4 * chart.Sync.Tickrate / denominator;
                }
                else
                {
                    chart.Settings.HopoThreshold = chart.Sync.Tickrate / 3;
                }
            }

            if (!modifiers.Extract("sustain_cutoff_threshold", out chart.Settings.SustainCutoffThreshold))
            {
                chart.Settings.SustainCutoffThreshold = chart.Sync.Tickrate / 3;
            }
            return chart;
        }

        public static YARGChart LoadMidi_Single(FixedArray<byte> data, in SongMetadata metadata, in LoaderSettings settings, IniModifierCollection? modifiers, DrumsType drumsInChart, HashSet<MidiTrackType>? activeInstruments)
        {
            var midi = YARGMidiFile.Load(data);
            var chart = new YARGChart(midi.Resolution, in metadata, in settings, modifiers);
            DualTime.SetTruncationLimit(chart.Settings, (uint) (midi.Resolution / 3));
            MidiFiveFretLoader.SetOverdriveMidiNote(chart.Settings.OverdiveMidiNote);

            var encoding = YARGTextReader.UTF8Strict;
            LoadTracks_Midi(chart, ref midi, ref encoding, ref drumsInChart, activeInstruments);
            FinalizeDeserialization(chart);
            return chart;
        }

        public static YARGChart LoadMidi_Multi(FixedArray<byte> mainData, FixedArray<byte>? updateData, FixedArray<byte>? upgradeData, in SongMetadata metadata, in LoaderSettings settings, DrumsType drumsInChart, HashSet<MidiTrackType>? activeInstruments)
        {
            var mainMidi = YARGMidiFile.Load(mainData);
            var chart = new YARGChart(mainMidi.Resolution, in metadata, in settings, null);
            DualTime.SetTruncationLimit(chart.Settings, (uint) (mainMidi.Resolution / 3));
            MidiFiveFretLoader.SetOverdriveMidiNote(chart.Settings.OverdiveMidiNote);

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
                    LoadSyncTrack_Midi(track, chart.Sync);
                    continue;
                }

                if (!YARGMidiTrack.TRACKNAMES.TryGetValue(name, out var type))
                {
                    YargLogger.LogInfo($"Unrecognized MIDI Track: {name}");
                    continue;
                }

                if (type == MidiTrackType.Events)
                {
                    LoadEventsTrack_Midi(chart, ref encoding, track);
                }
                else if (type == MidiTrackType.Beat)
                {
                    LoadBeatsTrack_Midi(chart.BeatMap, chart.Sync, track);
                }
                else if (activeInstruments == null || activeInstruments.Contains(type))
                {
                    LoadInstrument_Midi(chart, type, ref drumsInChart, track, ref encoding);
                }
            }
        }

        private static void LoadSyncTrack_Midi(YARGMidiTrack midiTrack, SyncTrack2 sync)
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
            FinalizeAnchors(sync);
        }

        private static readonly byte[][] PREFIXES = { Encoding.ASCII.GetBytes("[section "), Encoding.ASCII.GetBytes("[prc_") };
        private static void LoadEventsTrack_Midi(YARGChart chart, ref Encoding encoding, YARGMidiTrack midiTrack)
        {
            if (!chart.Globals.IsEmpty() || !chart.Sections.IsEmpty())
            {
                YargLogger.LogInfo("EVENTS track appears multiple times. Not parsing repeats...");
                return;
            }

            var position = default(DualTime);
            var stats = default(MidiStats);
            var tempoTracker = new TempoTracker(chart.Sync);
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

        private static void LoadBeatsTrack_Midi(YARGNativeSortedList<DualTime, BeatlineType> beats, SyncTrack2 sync, YARGMidiTrack midiTrack)
        {
            if (!beats.IsEmpty())
            {
                YargLogger.LogInfo("BEATS track appears multiple times. Not parsing repeats...");
                return;
            }

            var note = default(MidiNote);
            var position = default(DualTime);
            var stats = default(MidiStats);
            var tempoTracker = new TempoTracker(sync);
            while (midiTrack.ParseEvent(ref stats))
            {
                if (stats.Type == MidiEventType.Note_On)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    if (note.Velocity > 0)
                    {
                        position.Ticks = stats.Position;
                        position.Seconds = tempoTracker.Traverse(position.Ticks);
                        beats.AppendOrUpdate(position, note.Value == 12 ? BeatlineType.Measure : BeatlineType.Strong);
                    }
                }
            }
        }

        private static void LoadInstrument_Midi(YARGChart chart, MidiTrackType type, ref DrumsType drumsInChart, in YARGMidiTrack midiTrack, ref Encoding encoding)
        {
            switch (type)
            {
                case MidiTrackType.Guitar_5:      chart.FiveFretGuitar ??=     MidiFiveFretLoader.Load(midiTrack, chart.Sync); break;
                case MidiTrackType.Bass_5:        chart.FiveFretBass ??=       MidiFiveFretLoader.Load(midiTrack, chart.Sync); break;
                case MidiTrackType.Rhythm_5:      chart.FiveFretRhythm ??=     MidiFiveFretLoader.Load(midiTrack, chart.Sync); break;
                case MidiTrackType.Coop_5:        chart.FiveFretCoopGuitar ??= MidiFiveFretLoader.Load(midiTrack, chart.Sync); break;
                case MidiTrackType.Keys:          chart.Keys ??=               MidiFiveFretLoader.Load(midiTrack, chart.Sync); break;

                case MidiTrackType.Drums:
                    switch (drumsInChart)
                    {
                    case DrumsType.FourLane:
                    case DrumsType.ProDrums:
                        chart.FourLaneDrums ??= MidiDrumsLoader.LoadFourLane(midiTrack, chart.Sync);
                            break;
                    case DrumsType.FiveLane:
                        chart.FiveLaneDrums ??= MidiDrumsLoader.LoadFiveLane(midiTrack, chart.Sync);
                        break;
                    default:
                        // No `using/dipose` as events & phrases need to persist
                        var track = MidiDrumsLoader.LoadUnknownDrums(midiTrack, chart.Sync, ref drumsInChart);
                        // Only possible if pre-type was FourOrFive AND fifth lane was not found
                        if ((drumsInChart & DrumsType.FourLane) == DrumsType.FourLane)
                        {
                            chart.FourLaneDrums = track.ConvertToFourLane(false);
                            drumsInChart = DrumsType.FourLane;
                        }
                        // Only possible if pre-type was ProOrFive AND fifth lane was not found
                        else if ((drumsInChart & DrumsType.ProDrums) == DrumsType.ProDrums)
                        {
                            chart.FourLaneDrums = track.ConvertToFourLane(true);
                            drumsInChart = DrumsType.ProDrums;
                        }
                        // Only possible if fifth lane is found
                        else
                        {
                            chart.FiveLaneDrums = track.ConvertToFiveLane();
                        }
                        break;
                    }
                    break;
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
                        chart.HarmonyVocals ??= new HarmonyVocalsTrack();
                        int index = type - MidiTrackType.Harm1;
                        if (chart.HarmonyVocals[index].IsEmpty())
                        {
                            MidiVocalsLoader.Load(midiTrack, chart.Sync, chart.HarmonyVocals, index, ref encoding);
                        }
                        break;
                    }
            }
        }
    }
}

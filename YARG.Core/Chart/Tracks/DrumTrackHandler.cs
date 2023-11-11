using System;
using System.Collections.Generic;
using YARG.Core.IO;
using YARG.Core.Chart.Drums;

namespace YARG.Core.Chart
{
    public class DrumTrackHandler
    {
        public DrumTrackHandler() { }
        private readonly DrumsType _initialType;
        private DrumsType _currentType;
        private Track? _track;
        private (Track, DrumsType)[]? _difficulties;

        public DrumsType Type => _currentType;

        public DrumTrackHandler(DrumsType initial)
        {
            _currentType = _initialType = initial;
        }

        public bool IsOccupied() => _track != null || _difficulties != null;

        public void LoadMidi(YARGMidiTrack midiTrack, HashSet<Difficulty>? difficulties)
        {
            if (_track != null)
                return;

            if (_initialType == DrumsType.FiveLane)
                _track = Midi_FiveLane_Loader.Load(midiTrack, difficulties);
            else if (_initialType == DrumsType.FourLane)
                _track = Midi_FourLane_Loader.Load(midiTrack, difficulties);
            else if (_initialType == DrumsType.ProDrums)
                _track = Midi_ProDrum_Loader.Load(midiTrack, difficulties);
            else
            {
                (var track, var type) = Midi_UnknownDrums_Loader.Load(midiTrack, _initialType, difficulties);
                _track = track;
                _currentType = type switch
                {
                    DrumsType.UnknownPro => DrumsType.ProDrums,
                    DrumsType.Unknown => DrumsType.FourLane,
                    _ => type,
                };
            }
        }

        public void LoadChart<TChar, TDecoder, TBase>(YARGChartFileReader<TChar, TDecoder, TBase> reader)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
            where TDecoder : IStringDecoder<TChar>, new()
            where TBase : unmanaged, IDotChartBases<TChar>
        {
            int diff = (int)reader.Difficulty - 1;
            if (_difficulties != null && _difficulties[diff].Item1 != null)
            {
                reader.SkipTrack();
                return;
            }

            _difficulties ??= new (Track, DrumsType)[4];
            _difficulties[diff].Item2 = _currentType;
            _difficulties[diff].Item1 = _currentType switch
            {
                DrumsType.ProDrums => LoadChartDifficulty<TChar, TDecoder, TBase, DrumPad_4, Pro_Drums>  (reader, Set),
                DrumsType.FiveLane => LoadChartDifficulty<TChar, TDecoder, TBase, DrumPad_5, Basic_Drums>(reader, Set),
                DrumsType.FourLane => LoadChartDifficulty<TChar, TDecoder, TBase, DrumPad_4, Basic_Drums>(reader, Set),
                _ =>                  LoadChartDifficulty<TChar, TDecoder, TBase, DrumPad_5, Pro_Drums>  (reader, Set)
            };
        }

        public Track? GetMidiTrack()
        {
            if (_track == null)
                return null;

            if (_initialType != DrumsType.Unknown && _initialType != DrumsType.UnknownPro)
                return _track;

            switch (_currentType)
            {
                case DrumsType.FourLane: return MidiConvert<DrumPad_4, Basic_Drums>();
                case DrumsType.FiveLane: return MidiConvert<DrumPad_5, Basic_Drums>();
                default:
                    {
                        _currentType = DrumsType.ProDrums;
                        return MidiConvert<DrumPad_4, Pro_Drums>();
                    }
            }
        }

        public Track? GetChartTrack()
        {
            if (_difficulties == null)
                return null;

            switch (_initialType)
            {
                case DrumsType.ProDrums: return ChartCombine<DrumPad_4, Pro_Drums>();
                case DrumsType.FiveLane: return ChartCombine<DrumPad_5, Basic_Drums>();
                case DrumsType.FourLane: return ChartCombine<DrumPad_4, Basic_Drums>();
            }

            switch (_currentType)
            {
                case DrumsType.ProDrums: return ChartCombineConvert<DrumPad_4, Pro_Drums>();
                case DrumsType.FiveLane: return ChartCombineConvert<DrumPad_5, Basic_Drums>();
                default:
                    {
                        _currentType = DrumsType.FourLane;
                        return ChartCombineConvert<DrumPad_4, Basic_Drums>();
                    }
            }
        }

        private const string SOLO = "solo";
        private const string SOLOEND = "soloend";

        private delegate bool Loader<TDrumConfig, TCymbalConfig>(ref DrumNote<TDrumConfig, TCymbalConfig> note, int lane, long duration)
            where TDrumConfig : unmanaged, IDrumPadConfig
            where TCymbalConfig : unmanaged, ICymbalConfig;

        private DifficultyTrack_FW<DrumNote<TDrumConfig, TCymbalConfig>> LoadChartDifficulty<TChar, TDecoder, TBase, TDrumConfig, TCymbalConfig>(YARGChartFileReader<TChar, TDecoder, TBase> reader, Loader<TDrumConfig, TCymbalConfig> loader)
            where TDrumConfig : unmanaged, IDrumPadConfig
            where TCymbalConfig : unmanaged, ICymbalConfig
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
            where TDecoder : IStringDecoder<TChar>, new()
            where TBase : unmanaged, IDotChartBases<TChar>
        {
            var difficulty = new DifficultyTrack_FW<DrumNote<TDrumConfig, TCymbalConfig>>(5000);
            long solo = 0;

            DotChartEvent ev = default;
            DotChartNote note = default;
            while (reader.TryParseEvent(ref ev))
            {
                switch (ev.Type)
                {
                    case ChartEventType.Note:
                        {
                            ref var drum = ref difficulty.Notes.Get_Or_Add_Last(ev.Position);
                            reader.ExtractLaneAndSustain(ref note);
                            
                            if (!loader(ref drum, note.Lane, note.Duration))
                                if (drum.GetNumActiveNotes() == 0)
                                    difficulty.Notes.Pop();
                            break;
                        }
                    case ChartEventType.Special:
                        {
                            var phrase = reader.ExtractSpecialPhrase();
                            switch (phrase.Type)
                            {
                                case SpecialPhraseType.StarPower:
                                case SpecialPhraseType.BRE:
                                case SpecialPhraseType.Tremolo:
                                case SpecialPhraseType.Trill:
                                    difficulty.SpecialPhrases.Get_Or_Add_Last(ev.Position).Add(phrase);
                                    break;
                            }
                            break;
                        }
                    case ChartEventType.Text:
                        {
                            string str = reader.ExtractText();
                            if (str.StartsWith(SOLOEND))
                                difficulty.SpecialPhrases[solo].Add(new(SpecialPhraseType.Solo, ev.Position - solo));
                            else if (str.StartsWith(SOLO))
                                solo = ev.Position;
                            else
                                difficulty.Events.Get_Or_Add_Last(ev.Position).Add(str);
                            break;
                        }
                }
                reader.NextEvent();
            }
            difficulty.TrimExcess();
            return difficulty;
        }

        private const int BASS_INDEX = 0;
        private const int FOURLANE_MAX = 5;
        private const int FIVELANE_MAX = 5;
        private const int DOUBLEBASS_INDEX = 32;
        private const int CYMBAL_YELLOW = 66;
        private const int CYMBAL_GREEN = 68;
        private const int ACCENT_MIN = 34;
        private const int ACCENT_MAX_4 = 36;
        private const int ACCENT_MAX_5 = 37;
        private const int GHOST_MIN = 40;
        private const int GHOST_MAX_4 = 42;
        private const int GHOST_MAX_5 = 43;

        private bool Set(ref DrumNote<DrumPad_5, Pro_Drums> note, int lane, long length)
        {
            if (lane == BASS_INDEX)
                note.Bass = length;
            else if (lane <= FIVELANE_MAX)
            {
                note.Pads[lane - 1].Duration = length;
                if (lane == FIVELANE_MAX)
                    _currentType = DrumsType.FiveLane;
            }
            else if (lane == DOUBLEBASS_INDEX)
                note.DoubleBass = length;
            else if (CYMBAL_YELLOW <= lane && lane <= CYMBAL_GREEN)
            {
                note.Cymbals[lane - CYMBAL_YELLOW] = true;
                _currentType = DrumsType.ProDrums;
            }
            else if (ACCENT_MIN <= lane && lane <= ACCENT_MAX_5) note.Pads[lane - ACCENT_MIN].Dynamics = DrumDynamics.Accent;
            else if (GHOST_MIN <= lane && lane <= GHOST_MAX_5)   note.Pads[lane - GHOST_MIN].Dynamics = DrumDynamics.Ghost;
            else
                return false;
            return true;
        }

        private bool Set(ref DrumNote<DrumPad_4, Pro_Drums> note, int lane, long length)
        {
            if (lane == BASS_INDEX)            note.Bass = length;
            else if (lane <= FOURLANE_MAX)     note.Pads[lane - 1].Duration = length;
            else if (lane == DOUBLEBASS_INDEX) note.ToggleDoubleBass();
            else if (CYMBAL_YELLOW <= lane && lane <= CYMBAL_GREEN) note.Cymbals[lane - CYMBAL_YELLOW] = true;
            else if (ACCENT_MIN <= lane && lane <= ACCENT_MAX_4)    note.Pads[lane - ACCENT_MIN].Dynamics = DrumDynamics.Accent;
            else if (GHOST_MIN <= lane && lane <= GHOST_MAX_4)      note.Pads[lane - GHOST_MIN].Dynamics = DrumDynamics.Ghost;
            else
                return false;
            return true;
        }

        private bool Set<TDrumConfig>(ref DrumNote<TDrumConfig, Basic_Drums> note, int lane, long length)
            where TDrumConfig : unmanaged, IDrumPadConfig
        {
            if (lane == BASS_INDEX) note.Bass = length;
            else if (lane <= DrumNote<TDrumConfig, Basic_Drums>.NUMPADS) note.Pads[lane - 1].Duration = length;
            else if (lane == DOUBLEBASS_INDEX) note.ToggleDoubleBass();
            else if (ACCENT_MIN <= lane && lane - ACCENT_MIN < DrumNote<TDrumConfig, Basic_Drums>.NUMPADS) note.Pads[lane - ACCENT_MIN].Dynamics = DrumDynamics.Accent;
            else if (GHOST_MIN  <= lane && lane - GHOST_MIN  < DrumNote<TDrumConfig, Basic_Drums>.NUMPADS) note.Pads[lane - GHOST_MIN ].Dynamics = DrumDynamics.Ghost;
            else
                return false;
            return true;
        }

        private InstrumentTrack_FW<DrumNote<TDrumConfig, TCymbalConfig>> ChartCombine<TDrumConfig, TCymbalConfig>()
            where TDrumConfig : unmanaged, IDrumPadConfig
            where TCymbalConfig : unmanaged, ICymbalConfig
        {
            InstrumentTrack_FW<DrumNote<TDrumConfig, TCymbalConfig>> track = new();
            for (int i = 0; i < 4; ++i)
            {
                if (_difficulties![i].Item1 != null)
                {
                    track[i] = (DifficultyTrack_FW<DrumNote<TDrumConfig, TCymbalConfig>>)_difficulties[i].Item1;
                }
            }
            return track;
        }

        private InstrumentTrack_FW<DrumNote<TDrumConfig, TCymbalConfig>> ChartCombineConvert<TDrumConfig, TCymbalConfig>()
            where TDrumConfig : unmanaged, IDrumPadConfig
            where TCymbalConfig : unmanaged, ICymbalConfig
        {
            InstrumentTrack_FW<DrumNote<TDrumConfig, TCymbalConfig>> track = new();
            for (int i = 0; i < 4; ++i)
            {
                if (_difficulties![i].Item1 == null)
                    continue;

                if (_difficulties[i].Item2 == _currentType)
                    track[i] = (DifficultyTrack_FW<DrumNote<TDrumConfig, TCymbalConfig>>) _difficulties[i].Item1;
                else
                {
                    var unknownDiff = (DifficultyTrack_FW<DrumNote<DrumPad_5, Pro_Drums>>) _difficulties[i].Item1;
                    track[i] = Convert<TDrumConfig, TCymbalConfig>(unknownDiff);
                    unknownDiff.Dispose();
                }
            }
            return track;
        }

        private InstrumentTrack_FW<DrumNote<TDrumConfig, TCymbalConfig>> MidiConvert<TDrumConfig, TCymbalConfig>()
            where TDrumConfig : unmanaged, IDrumPadConfig
            where TCymbalConfig : unmanaged, ICymbalConfig
        {
            var unknownTrack = (InstrumentTrack_FW<DrumNote<DrumPad_5, Pro_Drums>>) _track!;
            InstrumentTrack_FW<DrumNote<TDrumConfig, TCymbalConfig>> drums = new()
            {
                SpecialPhrases = unknownTrack.SpecialPhrases,
                Events = unknownTrack.Events,
            };

            for (int diffIndex = 0; diffIndex < 4; ++diffIndex)
            {
                var unknownDiff = unknownTrack[diffIndex];
                if (unknownDiff != null && unknownDiff.IsOccupied())
                    drums[diffIndex] = Convert<TDrumConfig, TCymbalConfig>(unknownDiff);
            }
            unknownTrack.Dispose();
            return drums;
        }

        private static DifficultyTrack_FW<DrumNote<TDrumConfig, TCymbalConfig>> Convert<TDrumConfig, TCymbalConfig>(DifficultyTrack_FW<DrumNote<DrumPad_5, Pro_Drums>> unknownDiff)
            where TDrumConfig : unmanaged, IDrumPadConfig
            where TCymbalConfig : unmanaged, ICymbalConfig
        {
            DifficultyTrack_FW<DrumNote<TDrumConfig, TCymbalConfig>> diff = new()
            {
                SpecialPhrases = unknownDiff.SpecialPhrases,
                Events = unknownDiff.Events,
            };

            var span = unknownDiff.Notes.Span;
            var notes = diff.Notes;
            notes.Capacity = span.Length;
            for (int noteIndex = 0; noteIndex < span.Length; ++noteIndex)
            {
                ref var note = ref span[noteIndex];
                notes.Add_NoReturn(note.position, new DrumNote<TDrumConfig, TCymbalConfig>(ref note.obj));
            }
            return diff;
        }
    }
}

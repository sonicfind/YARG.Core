using System;
using System.Collections.Generic;
using YARG.Core.IO;

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
                DrumsType.ProDrums => LoadChartDifficulty<TChar, TDecoder, TBase, Drum_4Pro>   (reader, Set),
                DrumsType.FiveLane => LoadChartDifficulty<TChar, TDecoder, TBase, Drum_5>      (reader, Set),
                DrumsType.FourLane => LoadChartDifficulty<TChar, TDecoder, TBase, Drum_4>      (reader, Set),
                _ =>                  LoadChartDifficulty<TChar, TDecoder, TBase, Drum_Unknown>(reader, Set)
            };
        }

        public Track? GetMidiTrack()
        {
            if (_track == null)
                return null;

            if (_initialType != DrumsType.Unknown && _initialType != DrumsType.UnknownPro)
                return _track;

            var unknownTrack = (InstrumentTrack_FW<Drum_Unknown>) _track;
            switch (_currentType)
            {
                case DrumsType.FourLane: return Convert(unknownTrack, ConvertToFourLane);
                case DrumsType.FiveLane: return Convert(unknownTrack, ConvertToFiveLane);
                default:
                    {
                        _currentType = DrumsType.ProDrums;
                        return Convert(unknownTrack, ConvertToProDrums);
                    }
            }
        }

        public Track? GetChartTrack()
        {
            if (_difficulties == null)
                return null;

            switch (_initialType)
            {
                case DrumsType.ProDrums: return Combine<Drum_4Pro>();
                case DrumsType.FiveLane: return Combine<Drum_5>();
                case DrumsType.FourLane: return Combine<Drum_4>();
            }

            switch (_currentType)
            {
                case DrumsType.ProDrums: return CombineConvert(ConvertToProDrums);
                case DrumsType.FiveLane: return CombineConvert(ConvertToFiveLane);
                default:
                    {
                        _currentType = DrumsType.FourLane;
                        return CombineConvert(ConvertToFourLane);
                    }
            }
        }

        private const string SOLO = "solo";
        private const string SOLOEND = "soloend";

        private DifficultyTrack_FW<TDrum> LoadChartDifficulty<TChar, TDecoder, TBase, TDrum>(YARGChartFileReader<TChar, TDecoder, TBase> reader, Func<TDrum, int, long, bool> loader)
            where TChar : unmanaged, IEquatable<TChar>, IConvertible
            where TDecoder : IStringDecoder<TChar>, new()
            where TBase : unmanaged, IDotChartBases<TChar>
            where TDrum : DrumNote_FW, new()
        {
            var difficulty = new DifficultyTrack_FW<TDrum>(5000);
            long solo = 0;

            DotChartEvent ev = default;
            DotChartNote note = default;
            while (reader.TryParseEvent(ref ev))
            {
                switch (ev.Type)
                {
                    case ChartEventType.Note:
                        {
                            ref var drum = ref difficulty.notes.Get_Or_Add_Last(ev.Position);
                            reader.ExtractLaneAndSustain(ref note);
                            
                            if (!loader(drum, note.Lane, note.Duration))
                                if (!drum.HasActiveNotes())
                                    difficulty.notes.Pop();
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
                                    difficulty.specialPhrases.Get_Or_Add_Last(ev.Position).Add(phrase);
                                    break;
                            }
                            break;
                        }
                    case ChartEventType.Text:
                        {
                            string str = reader.ExtractText();
                            if (str.StartsWith(SOLOEND))
                                difficulty.specialPhrases[solo].Add(new(SpecialPhraseType.Solo, ev.Position - solo));
                            else if (str.StartsWith(SOLO))
                                solo = ev.Position;
                            else
                                difficulty.events.Get_Or_Add_Last(ev.Position).Add(str);
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

        private bool Set(Drum_Unknown note, int lane, long length)
        {
            if (lane == BASS_INDEX)
                note.Bass = length;
            else if (lane <= FIVELANE_MAX)
            {
                note.GetPad(lane - 1).Duration = length;
                if (lane == FIVELANE_MAX)
                    _currentType = DrumsType.FiveLane;
            }
            else if (lane == DOUBLEBASS_INDEX)
                note.DoubleBass = length;
            else if (CYMBAL_YELLOW <= lane && lane <= CYMBAL_GREEN)
            {
                note.cymbals[lane - CYMBAL_YELLOW] = true;
                _currentType = DrumsType.ProDrums;
            }
            else if (ACCENT_MIN <= lane && lane <= ACCENT_MAX_5) note.GetPad(lane - ACCENT_MIN).Dynamics = DrumDynamics.Accent;
            else if (GHOST_MIN <= lane && lane <= GHOST_MAX_5)   note.GetPad(lane - GHOST_MIN).Dynamics = DrumDynamics.Ghost;
            else
                return false;
            return true;
        }

        private bool Set(Drum_4Pro note, int lane, long length)
        {
            if (lane == BASS_INDEX)            note.Bass = length;
            else if (lane <= FOURLANE_MAX)     note.GetPad(lane - 1).Duration = length;
            else if (lane == DOUBLEBASS_INDEX) note.DoubleBass = length;
            else if (CYMBAL_YELLOW <= lane && lane <= CYMBAL_GREEN) note.cymbals[lane - CYMBAL_YELLOW] = true;
            else if (ACCENT_MIN <= lane && lane <= ACCENT_MAX_4)    note.GetPad(lane - ACCENT_MIN).Dynamics = DrumDynamics.Accent;
            else if (GHOST_MIN <= lane && lane <= GHOST_MAX_4)      note.GetPad(lane - GHOST_MIN).Dynamics = DrumDynamics.Ghost;
            else
                return false;
            return true;
        }

        private bool Set(Drum_4 note, int lane, long length)
        {
            if (lane == BASS_INDEX)            note.Bass = length;
            else if (lane <= FOURLANE_MAX)     note.GetPad(lane - 1).Duration = length;
            else if (lane == DOUBLEBASS_INDEX) note.DoubleBass = length;
            else if (ACCENT_MIN <= lane && lane <= ACCENT_MAX_4) note.GetPad(lane - ACCENT_MIN).Dynamics = DrumDynamics.Accent;
            else if (GHOST_MIN <= lane && lane <= GHOST_MAX_4)   note.GetPad(lane - GHOST_MIN).Dynamics = DrumDynamics.Ghost;
            else
                return false;
            return true;
        }

        private bool Set(Drum_5 note, int lane, long length)
        {
            if (lane == BASS_INDEX)            note.Bass = length;
            else if (lane <= FIVELANE_MAX)     note.GetPad(lane - 1).Duration = length;
            else if (lane == DOUBLEBASS_INDEX) note.DoubleBass = length;
            else if (ACCENT_MIN <= lane && lane <= ACCENT_MAX_5) note.GetPad(lane - ACCENT_MIN).Dynamics = DrumDynamics.Accent;
            else if (GHOST_MIN <= lane && lane <= GHOST_MAX_5)   note.GetPad(lane - GHOST_MIN).Dynamics = DrumDynamics.Ghost;
            else
                return false;
            return true;
        }

        private static InstrumentTrack_FW<TDrum> Convert<TDrum>(InstrumentTrack_FW<Drum_Unknown> unknownTrack, Func<DifficultyTrack_FW<Drum_Unknown>, DifficultyTrack_FW<TDrum>> converter)
            where TDrum : DrumNote_FW, new()
        {
            InstrumentTrack_FW<TDrum> drum4 = new();
            for (int diffIndex = 0; diffIndex < 4; ++diffIndex)
            {
                var unknownDiff = unknownTrack[diffIndex];
                if (unknownDiff.IsOccupied())
                    drum4[diffIndex] = converter(unknownDiff);
            }
            return drum4;
        }

        private InstrumentTrack_FW<TDrum> Combine<TDrum>()
            where TDrum : DrumNote_FW, new()
        {
            InstrumentTrack_FW<TDrum> track = new();
            for (int i = 0; i < 4; ++i)
            {
                if (_difficulties![i].Item1 != null)
                {
                    track[i] = (DifficultyTrack_FW<TDrum>)_difficulties[i].Item1;
                }
            }
            return track;
        }

        private InstrumentTrack_FW<TDrum> CombineConvert<TDrum>(Func<DifficultyTrack_FW<Drum_Unknown>, DifficultyTrack_FW<TDrum>> converter)
            where TDrum : DrumNote_FW, new()
        {
            InstrumentTrack_FW<TDrum> track = new();
            for (int i = 0; i < 4; ++i)
            {
                if (_difficulties![i].Item1 == null)
                    continue;

                if (_difficulties[i].Item2 == _currentType)
                    track[i] = (DifficultyTrack_FW<TDrum>) _difficulties[i].Item1;
                else
                    track[i] = converter((DifficultyTrack_FW<Drum_Unknown>) _difficulties[i].Item1);
            }
            return track;
        }

        private static DifficultyTrack_FW<Drum_4> ConvertToFourLane(DifficultyTrack_FW<Drum_Unknown> unknownDiff)
        {
            DifficultyTrack_FW<Drum_4> diff = new()
            {
                specialPhrases = unknownDiff.specialPhrases,
                events = unknownDiff.events,
            };

            var data = unknownDiff.notes.Data;
            for (int noteIndex = 0; noteIndex < data.Item2; ++noteIndex)
            {
                ref var note = ref data.Item1[noteIndex];
                diff.notes.Add(note.position, new(note.obj));
            }
            return diff;
        }

        private static DifficultyTrack_FW<Drum_4Pro> ConvertToProDrums(DifficultyTrack_FW<Drum_Unknown> unknownDiff)
        {
            DifficultyTrack_FW<Drum_4Pro> diff = new()
            {
                specialPhrases = unknownDiff.specialPhrases,
                events = unknownDiff.events,
            };

            var data = unknownDiff.notes.Data;
            for (int noteIndex = 0; noteIndex < data.Item2; ++noteIndex)
            {
                ref var note = ref data.Item1[noteIndex];
                diff.notes.Add(note.position, new(note.obj));
            }
            return diff;
        }

        private static DifficultyTrack_FW<Drum_5> ConvertToFiveLane(DifficultyTrack_FW<Drum_Unknown> unknownDiff)
        {
            DifficultyTrack_FW<Drum_5> diff = new()
            {
                specialPhrases = unknownDiff.specialPhrases,
                events = unknownDiff.events,
            };

            var data = unknownDiff.notes.Data;
            for (int noteIndex = 0; noteIndex < data.Item2; ++noteIndex)
            {
                ref var note = ref data.Item1[noteIndex];
                diff.notes.Add(note.position, new(note.obj));
            }
            return diff;
        }
    }
}

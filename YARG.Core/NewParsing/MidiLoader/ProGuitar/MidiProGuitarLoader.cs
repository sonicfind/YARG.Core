using System.Collections.Generic;
using System.Text;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    internal static class MidiProGuitarLoader
    {
        internal const int NOTE_MIN = 24;
        internal const int NOTE_MAX = 106;
        internal const int VALUES_PER_DIFFICULTY = 24;
        internal const int NUM_STRINGS = 6;
        internal const int HOPO_VALUE = 6;
        internal const int SLIDE_VALUE = 7;
        internal const int ARPEGGIO_VALUE = 8;
        internal const int EMPHASIS_VALUE = 9;
        internal const int FRET_MIN = 100;

        internal const int ROOT_MIN = 4;
        internal const int ROOT_MAX = 15;

        internal static readonly int[] DIFFVALUES = new int[InstrumentTrack2.NUM_DIFFICULTIES * VALUES_PER_DIFFICULTY]{
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
        };

        internal static readonly int[] LANEVALUES = new int[InstrumentTrack2.NUM_DIFFICULTIES * VALUES_PER_DIFFICULTY]{
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23,
        };

        internal static readonly Midi_PhraseMapping SOLO = new(new[] { 115 }, SpecialPhraseType.Solo);

        internal static readonly PitchName[] ROOTS = { PitchName.E, PitchName.F, PitchName.F_Sharp_Gb, PitchName.G, PitchName.G_Sharp_Ab, PitchName.A, PitchName.A_Sharp_Bb, PitchName.B, PitchName.C, PitchName.C_Sharp_Db, PitchName.D, PitchName.D_Sharp_Eb };
    }

    public class MidiProGuitarLoader<TProFretConfig> : MidiInstrumentLoader<ProGuitarInstrumentTrack<TProFretConfig>>
        where TProFretConfig : unmanaged, IProFretConfig<TProFretConfig>
    {
        public static ProGuitarInstrumentTrack<TProFretConfig> Load(YARGMidiTrack midiTrack, SyncTrack2 sync, HashSet<Difficulty>? difficulties)
        {
            var loader = new MidiProGuitarLoader<TProFretConfig>(difficulties);
            return loader.Process(midiTrack, sync);
        }

        private class ProGuitar_MidiDiff
        {
            public readonly DualTime[] Notes = {
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive
            };

            public DualTime Arpeggio;
            public ProSlide Slide;
            public EmphasisType Emphasis;
            public bool Hopo;
        }

        private readonly ProGuitar_MidiDiff[] Difficulties = new ProGuitar_MidiDiff[InstrumentTrack2.NUM_DIFFICULTIES];
        private readonly Midi_PhraseMapping[] PhraseMappings = new Midi_PhraseMapping[]
        {
            OverdrivePhrase, MidiProGuitarLoader.SOLO, MidiBasicInstrumentLoader.TREMOLO, MidiBasicInstrumentLoader.TRILL,
        };

        private MidiProGuitarLoader(HashSet<Difficulty>? difficulties)
            : base(MidiProGuitarLoader.NUM_STRINGS)
        {
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; i++)
            {
                if (difficulties == null || difficulties.Contains((Difficulty) (i + 1)))
                {
                    Track[i] = new();
                    Difficulties[i] = new();
                }
            }
        }

        protected override void ParseNote_ON()
        {
            NormalizeNoteOnPosition();
            if (MidiProGuitarLoader.NOTE_MIN <= _note.value && _note.value <= MidiProGuitarLoader.NOTE_MAX)
            {
                ParseLaneColor();
            }
            else if (!AddPhrase_ON(PhraseMappings))
            {
                if (!ParseBRE_ON())
                {
                    ToggleExtraValues();
                }
            }
        }

        protected override void ParseNote_Off()
        {
            if (MidiProGuitarLoader.NOTE_MIN <= _note.value && _note.value <= MidiProGuitarLoader.NOTE_MAX)
            {
                ParseLaneColor_Off();
            }
            else if (!AddPhrase_Off(PhraseMappings))
            {
                ParseBRE_Off();
            }
        }

        private void ParseLaneColor()
        {
            int noteValue = _note.value - MidiProGuitarLoader.NOTE_MIN;
            int diffIndex = MidiProGuitarLoader.DIFFVALUES[noteValue];

            var midiDiff = Difficulties[diffIndex];
            if (midiDiff == null)
                return;

            int lane = MidiProGuitarLoader.LANEVALUES[noteValue];
            ref var diffTrack = ref Track[diffIndex]!;
            if (lane < MidiProGuitarLoader.NUM_STRINGS)
            {
                if (_event.Channel == 1)
                {
                    diffTrack.Arpeggios.GetLastOrAppend(_position)[lane] = _note.velocity - MidiProGuitarLoader.FRET_MIN;
                }
                else
                {
                    if (diffTrack.Notes.Capacity == 0)
                    {
                        diffTrack.Notes.Capacity = 5000;
                    }

                    unsafe
                    {
                        if (diffTrack.Notes.TryAppend(_position, out var note))
                        {
                            note->HOPO = midiDiff.Hopo;
                            note->Slide = midiDiff.Slide;
                            note->Emphasis = midiDiff.Emphasis;
                        }

                        ref var proString = ref (*note)[lane];
                        switch (_event.Channel)
                        {
                            case 2: proString.Mode = StringMode.Bend; break;
                            case 3: proString.Mode = StringMode.Muted; break;
                            case 4: proString.Mode = StringMode.Tapped; break;
                            case 5: proString.Mode = StringMode.Harmonics; break;
                            case 6: proString.Mode = StringMode.Pinch_Harmonics; break;
                        }

                        proString.Fret = _note.velocity - MidiProGuitarLoader.FRET_MIN;
                    }

                    
                    midiDiff.Notes[lane] = _position;
                }
            }
            else if (lane == MidiProGuitarLoader.HOPO_VALUE)
            {
                midiDiff.Hopo = true;
                unsafe
                {
                    if (diffTrack.Notes.TryGetLastValue(_position, out var note))
                    {
                        note->HOPO = true;
                    }
                }
                
            }
            else if (lane == MidiProGuitarLoader.SLIDE_VALUE)
            {
                midiDiff.Slide = _event.Channel == 11 ? ProSlide.Reversed : ProSlide.Normal;
                unsafe
                {
                    if (diffTrack.Notes.TryGetLastValue(_position, out var note))
                    {
                        note->Slide = midiDiff.Slide;
                    }
                }
            }
            else if (lane == MidiProGuitarLoader.ARPEGGIO_VALUE)
            {
                diffTrack.Arpeggios.GetLastOrAppend(_position);
                midiDiff.Arpeggio = _position;
            }
            else if (lane == MidiProGuitarLoader.EMPHASIS_VALUE)
            {
                switch (_event.Channel)
                {
                    case 13: midiDiff.Emphasis = EmphasisType.High; break;
                    case 14: midiDiff.Emphasis = EmphasisType.Middle; break;
                    case 15: midiDiff.Emphasis = EmphasisType.Low; break;
                    default: return;
                }

                unsafe
                {
                    if (diffTrack.Notes.TryGetLastValue(_position, out var note))
                    {
                        note->Emphasis = midiDiff.Emphasis;
                    }
                }
            }
        }

        private void ParseLaneColor_Off()
        {
            int noteValue = _note.value - MidiProGuitarLoader.NOTE_MIN;
            int diffIndex = MidiProGuitarLoader.DIFFVALUES[noteValue];
            var midiDiff = Difficulties[diffIndex];
            if (midiDiff == null)
                return;

            int lane = MidiProGuitarLoader.LANEVALUES[noteValue];
            if (lane < MidiProGuitarLoader.NUM_STRINGS)
            {
                if (_event.Channel != 1)
                {
                    ref var colorPosition = ref midiDiff.Notes[lane];
                    if (colorPosition.Ticks != -1)
                    {
                        Track[diffIndex]!.Notes.TraverseBackwardsUntil(colorPosition)[lane].Duration = DualTime.Truncate(_position - colorPosition);
                        colorPosition.Ticks = -1;
                    }
                }
            }
            else if (lane == MidiProGuitarLoader.HOPO_VALUE)
            {
                midiDiff.Hopo = false;
            }
            else if (lane == MidiProGuitarLoader.SLIDE_VALUE)
            {
                midiDiff.Slide = ProSlide.None;
            }
            else if (lane == MidiProGuitarLoader.ARPEGGIO_VALUE)
            {
                ref var arpeggioPosition = ref midiDiff.Arpeggio;
                if (arpeggioPosition.Ticks != -1)
                {
                    Track[diffIndex]!.Arpeggios.Last().Length = DualTime.Normalize(_position - arpeggioPosition);
                    arpeggioPosition.Ticks = -1;
                }
            }
            else if (lane == MidiProGuitarLoader.EMPHASIS_VALUE)
            {
                midiDiff.Emphasis = EmphasisType.None;
            }
        }
        
        private void ToggleExtraValues()
        {
            if (MidiProGuitarLoader.ROOT_MIN <= _note.value && _note.value <= MidiProGuitarLoader.ROOT_MAX)
            {
                Track.Roots.Add(_position, MidiProGuitarLoader.ROOTS[_note.value - MidiProGuitarLoader.ROOT_MIN]);
                return;
            }

            switch (_note.value)
            {
                case 16:  Track.ChordPhrases.GetLastOrAppend(_position).Add(ChordPhrase.Slash); break;
                case 17:  Track.ChordPhrases.GetLastOrAppend(_position).Add(ChordPhrase.Hide); break;
                case 18:  Track.ChordPhrases.GetLastOrAppend(_position).Add(ChordPhrase.Accidental_Switch); break;
                case 107: Track.ChordPhrases.GetLastOrAppend(_position).Add(ChordPhrase.Force_Numbering); break;
                case 108: Track.HandPositions.Append(_position).Fret = _note.velocity - MidiProGuitarLoader.FRET_MIN; break;
            }
        }
    }
}

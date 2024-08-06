using System.Text;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    public static class MidiProGuitarLoader
    {
        private const int PROGUITAR_MIN = 24;
        private const int PROGUITAR_MAX = 106;
        private const int VALUES_PER_DIFFICULTY = 24;
        private const int NUM_STRINGS = 6;
        private const int HOPO_VALUE = 6;
        private const int SLIDE_VALUE = 7;
        private const int ARPEGGIO_VALUE = 8;
        private const int EMPHASIS_VALUE = 9;
        private const int MIN_FRET_VELOCITY = 100;

        private const int FULL_CHORD_NUMBERING_MIDI = 107;
        private const int LEFT_HAND_POSITION_MIDI = 108;
        private const int SOLO_MIDI = 115;
        private const int BRE_NOTE_MIN = 120;
        private const int BRE_NOTE_MAX = 125;

        private const int ROOT_MIN = 4;
        private const int ROOT_MAX = 15;
        private const int SLASH_CHORD_MIDI = 16;
        private const int HIDE_CHORD_MIDI = 17;
        private const int ACCIDENTAL_SWITCH_MIDI = 18;

        private static readonly int[] DIFFVALUES = new int[InstrumentTrack2.NUM_DIFFICULTIES * VALUES_PER_DIFFICULTY]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
        };

        private static readonly int[] LANEVALUES = new int[InstrumentTrack2.NUM_DIFFICULTIES * VALUES_PER_DIFFICULTY]
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23,
        };

        private static readonly PitchName[] ROOTS =
        {
            PitchName.E, PitchName.F, PitchName.F_Sharp_Gb, PitchName.G, PitchName.G_Sharp_Ab, PitchName.A, PitchName.A_Sharp_Bb, PitchName.B, PitchName.C, PitchName.C_Sharp_Db, PitchName.D, PitchName.D_Sharp_Eb
        };

        private struct ProGuitarDiff
        {
            public DualTime Arpeggio;
            public ProSlide Slide;
            public EmphasisType Emphasis;
            public bool Hopo;
        }

        public static unsafe ProGuitarInstrumentTrack<TProConfig> Load<TProConfig>(YARGMidiTrack midiTrack, SyncTrack2 sync)
            where TProConfig : unmanaged, IProFretConfig<TProConfig>
        {
            var instrumentTrack = new ProGuitarInstrumentTrack<TProConfig>();
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                instrumentTrack[i] = new ProGuitarDifficultyTrack<TProConfig>();
            }
            var difficulties = stackalloc ProGuitarDiff[InstrumentTrack2.NUM_DIFFICULTIES];
            var strings = stackalloc DualTime[InstrumentTrack2.NUM_DIFFICULTIES * NUM_STRINGS]
            {
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            };

            var brePositions = stackalloc DualTime[6];
            var overdrivePosition = DualTime.Inactive;
            var soloPosition = DualTime.Inactive;

            // Gliss & trill both utilize the velocity value to determine
            // what difficulties to apply to
            //
            // Velocities of 50-41 apply to hard alongside the default of only Expert
            var tremoloPostion = DualTime.Inactive;
            int tremoloVelocity = 0;
            var trillPosition = DualTime.Inactive;
            int trillVelocity = 0;

            int tempoIndex = 0;
            var position = default(DualTime);
            var lastOnNote = default(DualTime);
            var note = default(MidiNote);
            var stats = default(MidiStats);
            while (midiTrack.ParseEvent(ref stats))
            {
                position.Ticks = stats.Position;
                position.Seconds = sync.ConvertToSeconds(position.Ticks, ref tempoIndex);
                if (stats.Type is MidiEventType.Note_On or MidiEventType.Note_Off)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    if (stats.Type == MidiEventType.Note_On && note.Velocity > 0)
                    {
                        if (lastOnNote.Ticks + MidiLoader_Constants.NOTE_SNAP_THRESHOLD > position.Ticks)
                        {
                            position = lastOnNote;
                        }
                        else
                        {
                            lastOnNote = position;
                        }

                        if (PROGUITAR_MIN <= note.Value && note.Value <= PROGUITAR_MAX)
                        {
                            int noteValue = note.Value - PROGUITAR_MIN;
                            int diffIndex = DIFFVALUES[noteValue];
                            var diffTrack = instrumentTrack[diffIndex]!;
                            ref var midiDiff = ref difficulties[diffIndex];
                            int lane = LANEVALUES[noteValue];
                            if (lane < NUM_STRINGS)
                            {
                                if (diffTrack.Notes.Capacity == 0)
                                {
                                    diffTrack.Notes.Capacity = 5000;
                                }

                                if (diffTrack.Notes.TryAppend(position, out var guitar))
                                {
                                    guitar->HOPO = midiDiff.Hopo;
                                    guitar->Slide = midiDiff.Slide;
                                    guitar->Emphasis = midiDiff.Emphasis;
                                }

                                var proString = (ProGuitarString<TProConfig>*) guitar + lane;
                                proString->Mode = stats.Channel <= 6 ? (StringMode)stats.Channel : StringMode.Normal;
                                proString->Fret = note.Velocity - MIN_FRET_VELOCITY;
                                strings[diffIndex * NUM_STRINGS + lane] = position;
                            }
                            else
                            {
                                switch (lane)
                                {
                                    case HOPO_VALUE:
                                        {
                                            midiDiff.Hopo = true;
                                            if (diffTrack.Notes.TryGetLastValue(position, out var guitar))
                                            {
                                                guitar->HOPO = true;
                                            }
                                            break;
                                        }
                                    case SLIDE_VALUE:
                                        {
                                            midiDiff.Slide = stats.Channel == 11 ? ProSlide.Reversed : ProSlide.Normal;
                                            if (diffTrack.Notes.TryGetLastValue(position, out var guitar))
                                            {
                                                guitar->Slide = midiDiff.Slide;
                                            }
                                            break;
                                        }
                                    case ARPEGGIO_VALUE:
                                        midiDiff.Arpeggio = position;
                                        break;
                                    case EMPHASIS_VALUE:
                                        midiDiff.Emphasis = stats.Channel switch
                                        {
                                            13 => EmphasisType.High,
                                            14 => EmphasisType.Middle,
                                            15 => EmphasisType.Low,
                                            _ => EmphasisType.None,
                                        };

                                        {
                                            if (diffTrack.Notes.TryGetLastValue(position, out var guitar))
                                            {
                                                guitar->Emphasis = midiDiff.Emphasis;
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                        else if (ROOT_MIN <= note.Value && note.Value <= ROOT_MAX)
                        {
                            instrumentTrack.Roots.AppendOrUpdate(position, ROOTS[note.Value - ROOT_MIN]);
                        }
                        else if (BRE_NOTE_MIN <= note.Value && note.Value <= BRE_NOTE_MAX)
                        {
                            brePositions[note.Value - BRE_NOTE_MIN] = position;
                        }
                        else
                        {
                            switch (note.Value)
                            {
                                case MidiLoader_Constants.OVERDRIVE:
                                    overdrivePosition = position;
                                    instrumentTrack.SpecialPhrases.TryAppend(position);
                                    break;
                                case SOLO_MIDI:
                                    soloPosition = position;
                                    instrumentTrack.SpecialPhrases.TryAppend(position);
                                    break;
                                case MidiLoader_Constants.TREMOLO:
                                    tremoloPostion = position;
                                    tremoloVelocity = note.Velocity;
                                    instrumentTrack.SpecialPhrases.TryAppend(position);
                                    break;
                                case MidiLoader_Constants.TRILL:
                                    trillPosition = position;
                                    trillVelocity = note.Velocity;
                                    instrumentTrack.SpecialPhrases.TryAppend(position);
                                    break;
                                case SLASH_CHORD_MIDI:          instrumentTrack.ChordPhrases.GetLastOrAppend(position).Add(ChordPhrase.Slash); break;
                                case HIDE_CHORD_MIDI:           instrumentTrack.ChordPhrases.GetLastOrAppend(position).Add(ChordPhrase.Hide); break;
                                case ACCIDENTAL_SWITCH_MIDI:    instrumentTrack.ChordPhrases.GetLastOrAppend(position).Add(ChordPhrase.Accidental_Switch); break;
                                case FULL_CHORD_NUMBERING_MIDI: instrumentTrack.ChordPhrases.GetLastOrAppend(position).Add(ChordPhrase.Force_Numbering); break;
                                case LEFT_HAND_POSITION_MIDI:   instrumentTrack.HandPositions.Append(position)->Fret = note.Velocity - MIN_FRET_VELOCITY; break;
                            }
                        }
                    }
                    else
                    {
                        if (PROGUITAR_MIN <= note.Value && note.Value <= PROGUITAR_MAX)
                        {
                            int noteValue = note.Value - PROGUITAR_MIN;
                            int diffIndex = DIFFVALUES[noteValue];
                            var diffTrack = instrumentTrack[diffIndex]!;
                            ref var midiDiff = ref difficulties[diffIndex];
                            int lane = LANEVALUES[noteValue];
                            if (lane < NUM_STRINGS)
                            {
                                if (stats.Channel != 1)
                                {
                                    ref var stringPosition = ref strings[diffIndex * NUM_STRINGS + lane];
                                    if (stringPosition.Ticks != -1)
                                    {
                                        ((ProGuitarString<TProConfig>*) diffTrack.Notes.TraverseBackwardsUntil(stringPosition))[lane].Duration = DualTime.Truncate(position - stringPosition);
                                        stringPosition.Ticks = -1;
                                    }
                                }
                            }
                            else
                            {
                                switch (lane)
                                {
                                    case HOPO_VALUE:
                                        {
                                            midiDiff.Hopo = false;
                                            if (diffTrack.Notes.TryGetLastValue(position, out var guitar))
                                            {
                                                guitar->HOPO = false;
                                            }
                                            break;
                                        }
                                    case SLIDE_VALUE:
                                        {
                                            midiDiff.Slide = ProSlide.None;
                                            if (diffTrack.Notes.TryGetLastValue(position, out var guitar))
                                            {
                                                guitar->Slide = ProSlide.None;
                                            }
                                            break;
                                        }
                                    case ARPEGGIO_VALUE:
                                        if (midiDiff.Arpeggio.Ticks != -1)
                                        {
                                            diffTrack.Arpeggios.Append(midiDiff.Arpeggio, DualTime.Normalize(position - midiDiff.Arpeggio));
                                            midiDiff.Arpeggio.Ticks = -1;
                                        }
                                        break;
                                    case EMPHASIS_VALUE:
                                        {
                                            midiDiff.Emphasis = EmphasisType.None;
                                            if (diffTrack.Notes.TryGetLastValue(position, out var guitar))
                                            {
                                                guitar->Emphasis = EmphasisType.None;
                                            }
                                            break;
                                        }
                                }
                            }
                        }
                        else if (BRE_NOTE_MIN <= note.Value && note.Value <= BRE_NOTE_MAX)
                        {
                            ref var bre = ref brePositions[note.Value - BRE_NOTE_MIN];
                            if (bre.Ticks > -1
                                && brePositions[0] == brePositions[1]
                                && brePositions[1] == brePositions[2]
                                && brePositions[2] == brePositions[3]
                                && brePositions[3] == brePositions[4]
                                && brePositions[4] == brePositions[5])
                            {
                                var duration = position - bre;
                                instrumentTrack.SpecialPhrases[bre].Add(SpecialPhraseType.BRE, (duration, 100));
                            }
                            bre.Ticks = -1;
                        }
                        else
                        {
                            switch (note.Value)
                            {
                                case MidiLoader_Constants.OVERDRIVE:
                                    if (overdrivePosition.Ticks > -1)
                                    {
                                        var duration = position - overdrivePosition;
                                        instrumentTrack.SpecialPhrases
                                                .TraverseBackwardsUntil(overdrivePosition)
                                                .Add(SpecialPhraseType.StarPower, (duration, 100));
                                        overdrivePosition.Ticks = -1;
                                    }
                                    break;
                                case SOLO_MIDI:
                                    if (soloPosition.Ticks > -1)
                                    {
                                        var duration = position - soloPosition;
                                        instrumentTrack.SpecialPhrases
                                                .TraverseBackwardsUntil(soloPosition)
                                                .Add(SpecialPhraseType.Solo, (duration, 100));
                                        soloPosition.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TREMOLO:
                                    if (tremoloPostion.Ticks > -1)
                                    {
                                        var duration = position - tremoloPostion;
                                        instrumentTrack.SpecialPhrases
                                                .TraverseBackwardsUntil(tremoloPostion)
                                                .Add(SpecialPhraseType.Tremolo, (duration, tremoloVelocity));
                                        tremoloPostion.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TRILL:
                                    if (trillPosition.Ticks > -1)
                                    {
                                        var duration = position - trillPosition;
                                        instrumentTrack.SpecialPhrases
                                                .TraverseBackwardsUntil(trillPosition)
                                                .Add(SpecialPhraseType.Trill, (duration, trillVelocity));
                                        trillPosition.Ticks = -1;
                                    }
                                    break;

                            }
                        }
                    }
                }
                else if (MidiEventType.Text <= stats.Type && stats.Type <= MidiEventType.Text_EnumLimit)
                {
                    var str = midiTrack.ExtractTextOrSysEx();
                    var ev = str.GetString(Encoding.ASCII);
                    instrumentTrack.Events
                        .GetLastOrAppend(position)
                        .Add(ev);
                }
            }
            return instrumentTrack;
        }
    }
}

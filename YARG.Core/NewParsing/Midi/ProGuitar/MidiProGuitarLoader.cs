using System.Text;
using YARG.Core.Containers;
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

        public static unsafe void Load<TProFret>(YARGMidiTrack midiTrack, ProGuitarInstrumentTrack<TProFret> instrumentTrack, ref TempoTracker tempoTracker)
            where TProFret : unmanaged, IProFret
        {
            if (!instrumentTrack.IsEmpty())
            {
                return;
            }

            using var overdrives = new YargNativeSortedList<DualTime, DualTime>();
            using var solos = new YargNativeSortedList<DualTime, DualTime>();
            using var bres = new YargNativeSortedList<DualTime, DualTime>();

            var diffModifiers = stackalloc (DualTime Arpeggio, ProSlide Slide, EmphasisType Emphasis, bool Hopo)[InstrumentTrack2.NUM_DIFFICULTIES];
            // Per-difficulty tracker of note positions
            var strings = stackalloc DualTime[InstrumentTrack2.NUM_DIFFICULTIES * NUM_STRINGS]
            {
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            };

            // Various special phrases trackers
            var brePositions = stackalloc DualTime[6];
            var overdrivePosition = DualTime.Inactive;
            var soloPosition = DualTime.Inactive;

            // Tremolo & trill utilize the velocity value to determine what difficulties to apply to
            //
            // Velocities of 50-41 apply to hard alongside the default of only Expert
            var tremoloPostion = DualTime.Inactive;
            var trillPosition = DualTime.Inactive;
            bool tremoloOnHard = false;
            bool trillOnHard = false;

            // Various chord marker positions
            var slashPosition = DualTime.Inactive;
            var hideChordPosition = DualTime.Inactive;
            var accidentalPosition = DualTime.Inactive;
            var fullChordPosition = DualTime.Inactive;

            var position = DualTime.Zero;
            var note = default(MidiNote);
            var stats = default(MidiStats);
            // Used for snapping together notes that get accidentally misaligned during authoring
            var chordSnapper = new ChordSnapper();
            while (midiTrack.ParseEvent(ref stats))
            {
                position.Ticks = stats.Position;
                position.Seconds = tempoTracker.Traverse(position.Ticks);
                if (stats.Type is MidiEventType.Note_On or MidiEventType.Note_Off)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    // Only noteOn events with non-zero velocities actually count as "ON"
                    if (stats.Type == MidiEventType.Note_On && note.Velocity > 0)
                    {
                        // If the distance between the current NoteOn and the previous NoteOn is less than a certain threshold
                        // the previous position will override the current one, to "chord" multiple notes together
                        chordSnapper.Snap(ref position);
                        if (PROGUITAR_MIN <= note.Value && note.Value <= PROGUITAR_MAX)
                        {
                            int noteValue = note.Value - PROGUITAR_MIN;
                            int diffIndex = DIFFVALUES[noteValue];
                            var diffTrack = instrumentTrack[diffIndex];
                            ref var diffMods = ref diffModifiers[diffIndex];
                            int lane = LANEVALUES[noteValue];
                            if (lane < NUM_STRINGS)
                            {
                                if (diffTrack.Notes.Capacity == 0)
                                {
                                    diffTrack.Notes.Capacity = 5000;
                                }

                                if (diffTrack.Notes.GetLastOrAdd(in position, out var guitar))
                                {
                                    guitar->HOPO = diffMods.Hopo;
                                    guitar->Slide = diffMods.Slide;
                                    guitar->Emphasis = diffMods.Emphasis;
                                }

                                var proString = (ProGuitarString<TProFret>*) guitar + lane;
                                proString->Mode = stats.Channel <= 6 ? (StringMode)stats.Channel : StringMode.Normal;
                                proString->Fret.Value = note.Velocity - MIN_FRET_VELOCITY;
                                strings[diffIndex * NUM_STRINGS + lane] = position;
                            }
                            else
                            {
                                switch (lane)
                                {
                                    case HOPO_VALUE:
                                        {
                                            diffMods.Hopo = true;
                                            if (diffTrack.Notes.TryGetLastValue(in position, out var guitar))
                                            {
                                                guitar->HOPO = true;
                                            }
                                            break;
                                        }
                                    case SLIDE_VALUE:
                                        {
                                            diffMods.Slide = stats.Channel == 11 ? ProSlide.Reversed : ProSlide.Normal;
                                            if (diffTrack.Notes.TryGetLastValue(in position, out var guitar))
                                            {
                                                guitar->Slide = diffMods.Slide;
                                            }
                                            break;
                                        }
                                    case ARPEGGIO_VALUE:
                                        diffMods.Arpeggio = position;
                                        break;
                                    case EMPHASIS_VALUE:
                                        diffMods.Emphasis = stats.Channel switch
                                        {
                                            13 => EmphasisType.High,
                                            14 => EmphasisType.Middle,
                                            15 => EmphasisType.Low,
                                            _ => EmphasisType.None,
                                        };

                                        {
                                            if (diffTrack.Notes.TryGetLastValue(in position, out var guitar))
                                            {
                                                guitar->Emphasis = diffMods.Emphasis;
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                        else if (ROOT_MIN <= note.Value && note.Value <= ROOT_MAX)
                        {
                            instrumentTrack.Roots.AddOrUpdate(in position, ROOTS[note.Value - ROOT_MIN]);
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
                                    break;
                                case SOLO_MIDI:
                                    soloPosition = position;
                                    break;
                                case MidiLoader_Constants.TREMOLO:
                                    tremoloPostion = position;
                                    tremoloOnHard = 41 <= note.Velocity && note.Velocity <= 50;
                                    break;
                                case MidiLoader_Constants.TRILL:
                                    trillPosition = position;
                                    trillOnHard = 41 <= note.Velocity && note.Velocity <= 50;
                                    break;
                                case SLASH_CHORD_MIDI:          slashPosition = position; break;
                                case HIDE_CHORD_MIDI:           hideChordPosition = position; break;
                                case ACCIDENTAL_SWITCH_MIDI:    accidentalPosition = position; break;
                                case FULL_CHORD_NUMBERING_MIDI: fullChordPosition = position; break;
                                case LEFT_HAND_POSITION_MIDI:   instrumentTrack.HandPositions.Add(in position)->Value = note.Velocity - MIN_FRET_VELOCITY; break;
                            }
                        }
                    }
                    else
                    {
                        if (PROGUITAR_MIN <= note.Value && note.Value <= PROGUITAR_MAX)
                        {
                            int noteValue = note.Value - PROGUITAR_MIN;
                            int diffIndex = DIFFVALUES[noteValue];
                            var diffTrack = instrumentTrack[diffIndex];
                            ref var diffMods = ref diffModifiers[diffIndex];
                            int lane = LANEVALUES[noteValue];
                            if (lane < NUM_STRINGS)
                            {
                                if (stats.Channel != 1)
                                {
                                    ref var stringPosition = ref strings[diffIndex * NUM_STRINGS + lane];
                                    if (stringPosition.Ticks != -1)
                                    {
                                        ((ProGuitarString<TProFret>*) diffTrack.Notes.TraverseBackwardsUntil(in stringPosition))[lane].Duration = position - stringPosition;
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
                                            diffMods.Hopo = false;
                                            if (diffTrack.Notes.TryGetLastValue(in position, out var guitar))
                                            {
                                                guitar->HOPO = false;
                                            }
                                            break;
                                        }
                                    case SLIDE_VALUE:
                                        {
                                            diffMods.Slide = ProSlide.None;
                                            if (diffTrack.Notes.TryGetLastValue(in position, out var guitar))
                                            {
                                                guitar->Slide = ProSlide.None;
                                            }
                                            break;
                                        }
                                    case ARPEGGIO_VALUE:
                                        if (diffMods.Arpeggio.Ticks != -1)
                                        {
                                            instrumentTrack[diffIndex].Arpeggios.Add(in diffMods.Arpeggio, position - diffMods.Arpeggio);
                                            diffMods.Arpeggio.Ticks = -1;
                                        }
                                        break;
                                    case EMPHASIS_VALUE:
                                        {
                                            diffMods.Emphasis = EmphasisType.None;
                                            if (diffTrack.Notes.TryGetLastValue(in position, out var guitar))
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
                                bres.Add(in bre, position - bre);
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
                                        overdrives.Add(in overdrivePosition, position - overdrivePosition);
                                        overdrivePosition.Ticks = -1;
                                    }
                                    break;
                                case SOLO_MIDI:
                                    if (soloPosition.Ticks > -1)
                                    {
                                        solos.Add(in soloPosition, position - soloPosition);
                                        soloPosition.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TREMOLO:
                                    if (tremoloPostion.Ticks > -1)
                                    {
                                        var duration = position - tremoloPostion;
                                        instrumentTrack.Expert.Tremolos.Add(in tremoloPostion, duration);
                                        if (tremoloOnHard)
                                        {
                                            instrumentTrack.Hard.Tremolos.Add(in tremoloPostion, duration);
                                            tremoloOnHard = false;
                                        }
                                        tremoloPostion.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TRILL:
                                    if (trillPosition.Ticks > -1)
                                    {
                                        var duration = position - trillPosition;
                                        instrumentTrack.Expert.Trills.Add(in trillPosition, duration);
                                        if (trillOnHard)
                                        {
                                            instrumentTrack.Hard.Trills.Add(in trillPosition, duration);
                                            trillOnHard = false;
                                        }
                                        trillPosition.Ticks = -1;
                                    }
                                    break;
                                case SLASH_CHORD_MIDI:
                                    if (slashPosition.Ticks > -1)
                                    {
                                        instrumentTrack.SlashChords.Add(in slashPosition, position - slashPosition);
                                        slashPosition.Ticks = -1;
                                    }
                                    break;
                                case HIDE_CHORD_MIDI:
                                    if (hideChordPosition.Ticks > -1)
                                    {
                                        instrumentTrack.HideChords.Add(in hideChordPosition, position - hideChordPosition);
                                        hideChordPosition.Ticks = -1;
                                    }
                                    break;
                                case ACCIDENTAL_SWITCH_MIDI:
                                    if (accidentalPosition.Ticks > -1)
                                    {
                                        instrumentTrack.AccidentalSwitches.Add(in accidentalPosition, position - accidentalPosition);
                                        accidentalPosition.Ticks = -1;
                                    }
                                    break;
                                case FULL_CHORD_NUMBERING_MIDI:
                                    if (fullChordPosition.Ticks > -1)
                                    {
                                        instrumentTrack.Force_ChordNumbering.Add(in fullChordPosition, position - fullChordPosition);
                                        fullChordPosition.Ticks = -1;
                                    }
                                    break;

                            }
                        }
                    }
                }
                else if (MidiEventType.Text <= stats.Type && stats.Type <= MidiEventType.Text_EnumLimit && stats.Type != MidiEventType.Text_TrackName)
                {
                    // Unless, for some stupid-ass reason, this track contains lyrics,
                    // all actually useful events will utilize ASCII encoding for state
                    var str = midiTrack.ExtractTextOrSysEx();
                    var ev = str.GetString(Encoding.ASCII);
                    instrumentTrack.Events
                        .GetLastOrAdd(position)
                        .Add(ev);
                }
            }

            foreach (var diff in instrumentTrack)
            {
                diff.Overdrives.CopyFrom(overdrives);
                diff.Soloes.CopyFrom(solos);
                diff.BREs.CopyFrom(bres);
            }
        }
    }
}

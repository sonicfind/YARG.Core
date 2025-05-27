using System;
using System.Diagnostics;
using YARG.Core.Containers;
using YARG.Core.Game;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public class DrumTrack : IDisposable
    {
        public int                            NumLanes      { get; }
        public YargNativeList<DualTime>       NotePositions { get; }
        public YargNativeList<DrumNoteGroup>  NoteGroups    { get; }
        public YargNativeList<DrumRoll>       Rolls         { get; }
        public YargNativeList<HittablePhrase> Overdrives    { get; }
        public YargNativeList<HittablePhrase> Solos         { get; }

        public DrumTrack Clone()
        {
            return new DrumTrack
            (
                NumLanes,
                new YargNativeList<DualTime>(NotePositions),
                new YargNativeList<DrumNoteGroup>(NoteGroups),
                new YargNativeList<DrumRoll>(Rolls),
                new YargNativeList<HittablePhrase>(Overdrives),
                new YargNativeList<HittablePhrase>(Solos)
            );
        }

        public void Dispose()
        {
            NoteGroups.Dispose();
            Rolls.Dispose();
            Overdrives.Dispose();
            Solos.Dispose();
        }

        private DrumTrack(
            int numLanes,
            YargNativeList<DualTime>       notePositions,
            YargNativeList<DrumNoteGroup>  noteGroups,
            YargNativeList<DrumRoll>       rolls,
            YargNativeList<HittablePhrase> overdrives,
            YargNativeList<HittablePhrase> solos
        )
        {
            NotePositions = notePositions;
            NumLanes = numLanes;
            NoteGroups = noteGroups;
            Rolls = rolls;
            Overdrives = overdrives;
            Solos = solos;
        }

        private const int KICK         = 0;
        private const int SNARE        = 1;
        private const int TOM_1        = 2;
        private const int TOM_2        = 3;
        private const int TOM_3        = 4;
        private const int CYMBAL_1     = 5;
        private const int CYMBAL_2     = 6;
        private const int CYMBAL_3     = 7;
        private const int CYMBAL_SHIFT = CYMBAL_1 - TOM_1;

        public static unsafe DrumTrack Create(
            YARGChart chart,
            InstrumentTrack2<FourLaneDrums> instrument,
            in DualTime chartEndTime,
            in InstrumentSelection selection
        )
        {
            var difficultyTrack = instrument[selection.Difficulty];

            var positions = new YargNativeList<DualTime>
            {
                Capacity = difficultyTrack.Notes.Count
            };
            // Alters `Count` to allow directly indexing (and overwriting) groups in the list
            positions.Resize_NoInitialization(difficultyTrack.Notes.Count);

            var groups = new YargNativeList<DrumNoteGroup>
            {
                Capacity = difficultyTrack.Notes.Count
            };
            // Alters `Count` to allow directly indexing (and overwriting) groups in the list
            groups.Resize_NoInitialization(difficultyTrack.Notes.Count);

            var rolls = new YargNativeList<DrumRoll>
            {
                Capacity = difficultyTrack.Notes.Count
            };

            var overdrives = CommonTrackOps.InitHittablePhrases(difficultyTrack.Overdrives);
            var solos = CommonTrackOps.InitHittablePhrases(difficultyTrack.Solos);

            // The actual final number of the notes can differ from the number of notes in the track
            // depending on the difficulty setup and sets of active modifiers
            int noteCount = 0;

            bool noKicks = (selection.Modifiers & Modifier.NoKicks) == Modifier.NoKicks;
            bool isExpertPlus = selection.Difficulty == Difficulty.ExpertPlus;

            for (int noteIndex = 0, overdriveIndex = 0, soloIndex = 0;
                noteIndex < difficultyTrack.Notes.Count;
                noteIndex++)
            {
                var note = difficultyTrack.Notes.Data + noteIndex;
                // This ensures that the result screen doesn't add notes that can never be played
                if (note->Key >= chartEndTime)
                {
                    break;
                }

                var group = new DrumNoteGroup
                {
                    NormalMask = DrumLaneMask.None,
                    AccentMask = DrumLaneMask.None,
                    GhostMask = DrumLaneMask.None,
                };

                var lanes = (DualTime*)&note->Value.Lanes;
                for (int lane = noKicks ? 1 : 0; lane < FourLaneDrums.NUM_LANES; lane++)
                {
                    var fret = lanes[lane];
                    if (!fret.IsActive())
                    {
                        continue;
                    }

                    if (lane == KICK
                        && note->Value.KickState != KickState.Shared
                        && isExpertPlus != (note->Value.KickState == KickState.PlusOnly))
                    {
                        continue;
                    }

                    DrumLaneMask laneMask;
                    switch (lane)
                    {
                        case TOM_1:
                        {
                            if (note->Value.Cymbals.Yellow)
                            {
                                laneMask = DrumLaneMask.Cymbal_1;
                            }
                            else if (selection.Instrument == Instrument.FourLaneDrums
                                || !lanes[TOM_2].IsActive()
                                || note->Value.Cymbals.Blue)
                            {
                                laneMask = DrumLaneMask.Tom_1;
                            }
                            else
                            {
                                laneMask = DrumLaneMask.Snare;
                            }
                            break;
                        }
                        case TOM_2:
                        {
                            if (!note->Value.Cymbals.Blue)
                            {
                                laneMask = selection.Instrument == Instrument.FourLaneDrums
                                    ? DrumLaneMask.Tom_2
                                    : DrumLaneMask.Tom_1;
                            }
                            else
                            {
                                laneMask = selection.Instrument == Instrument.FourLaneDrums
                                    || !lanes[TOM_3].IsActive()
                                    || !note->Value.Cymbals.Green
                                    ? DrumLaneMask.Cymbal_2
                                    : DrumLaneMask.Cymbal_1;
                            }
                            break;
                        }
                        case TOM_3:
                        {
                            if (!note->Value.Cymbals.Green)
                            {
                                laneMask = selection.Instrument == Instrument.FourLaneDrums
                                    ? DrumLaneMask.Tom_3
                                    : DrumLaneMask.Tom_2;
                            }
                            else
                            {
                                laneMask = selection.Instrument == Instrument.FourLaneDrums
                                    ? DrumLaneMask.Cymbal_3
                                    : DrumLaneMask.Cymbal_2;
                            }
                            break;
                        }
                        default:
                        {
                            laneMask = (DrumLaneMask)(1 << lane);
                            break;
                        }
                    }

                    if (fret.Ticks >= chart.Settings.SustainCutoffThreshold)
                    {
                        rolls.Add(new DrumRoll(fret + note->Key, laneMask));
                    }

                    var dynamics = lane switch
                    {
                        1 => note->Value.Dynamics.Snare,
                        2 => note->Value.Dynamics.Yellow,
                        3 => note->Value.Dynamics.Blue,
                        4 => note->Value.Dynamics.Green,
                        _ => DrumDynamics.None,
                    };

                    switch (dynamics)
                    {
                        case DrumDynamics.None:
                        {
                            group.NormalMask |= laneMask;
                            break;
                        }
                        case DrumDynamics.Accent:
                        {
                            group.AccentMask |= laneMask;
                            break;
                        }
                        case DrumDynamics.Ghost:
                        {
                            group.GhostMask |= laneMask;
                            break;
                        }
                        default:
                        {
                            throw new ArgumentOutOfRangeException();
                        }
                    }
                }

                // The actual final number of the notes can differ from the number of notes in the track
                // depending on the difficulty setup and sets of active modifiers that may restrict
                // certain lanes from appearing in the track
                if (group.NormalMask != DrumLaneMask.None
                ||  group.AccentMask != DrumLaneMask.None
                ||  group.GhostMask  != DrumLaneMask.None)
                {
                    continue;
                }

                group.OverdriveIndex = CommonTrackOps.GetHittablePhraseIndex(overdrives, note->Key, ref overdriveIndex);
                group.SoloIndex = CommonTrackOps.GetHittablePhraseIndex(solos, note->Key, ref soloIndex);

                positions[noteCount] = note->Key;
                groups[noteCount] = group;

                ++noteCount;
            }

            positions.Resize_NoInitialization(noteCount);
            groups.Resize_NoInitialization(noteCount);
            positions.TrimExcess();
            groups.TrimExcess();
            rolls.TrimExcess();
            overdrives.TrimExcess();
            solos.TrimExcess();

            return new DrumTrack(5, positions, groups, rolls, overdrives, solos);
        }

        public static unsafe DrumTrack Create(
            YARGChart chart,
            InstrumentTrack2<FiveLaneDrums> instrument,
            in DualTime chartEndTime,
            in InstrumentSelection selection
        )
        {
            var difficultyTrack = instrument[selection.Difficulty];

            var positions = new YargNativeList<DualTime>
            {
                Capacity = difficultyTrack.Notes.Count
            };
            // Alters `Count` to allow directly indexing (and overwriting) groups in the list
            positions.Resize_NoInitialization(difficultyTrack.Notes.Count);

            var groups = new YargNativeList<DrumNoteGroup>
            {
                Capacity = difficultyTrack.Notes.Count
            };
            // Alters `Count` to allow directly indexing (and overwriting) groups in the list
            groups.Resize_NoInitialization(difficultyTrack.Notes.Count);

            var rolls = new YargNativeList<DrumRoll>
            {
                Capacity = difficultyTrack.Notes.Count
            };

            var overdrives = CommonTrackOps.InitHittablePhrases(difficultyTrack.Overdrives);
            var solos = CommonTrackOps.InitHittablePhrases(difficultyTrack.Solos);

            // The actual final number of the notes can differ from the number of notes in the track
            // depending on the difficulty setup and sets of active modifiers
            int noteCount = 0;

            bool noKicks = (selection.Modifiers & Modifier.NoKicks) == Modifier.NoKicks;
            bool isExpertPlus = selection.Difficulty == Difficulty.ExpertPlus;

            for (int noteIndex = 0, overdriveIndex = 0, soloIndex = 0;
                noteIndex < difficultyTrack.Notes.Count;
                noteIndex++)
            {
                var note = difficultyTrack.Notes.Data + noteIndex;
                // This ensures that the result screen doesn't add notes that can never be played
                if (note->Key >= chartEndTime)
                {
                    break;
                }

                var group = new DrumNoteGroup
                {
                    NormalMask = DrumLaneMask.None,
                    AccentMask = DrumLaneMask.None,
                    GhostMask = DrumLaneMask.None,
                };

                var lanes = (DualTime*)&note->Value.Lanes;
                for (int lane = noKicks ? 1 : 0; lane < FourLaneDrums.NUM_LANES; lane++)
                {
                    var fret = lanes[lane];
                    if (!fret.IsActive())
                    {
                        continue;
                    }

                    if (lane == KICK
                        && note->Value.KickState != KickState.Shared
                        && isExpertPlus != (note->Value.KickState == KickState.PlusOnly))
                    {
                        continue;
                    }

                    int shift;
                    if (selection.Instrument == Instrument.FiveLaneDrums)
                    {
                        shift = lane switch
                        {
                            TOM_1    => CYMBAL_1,
                            TOM_2    => TOM_1,
                            TOM_3    => CYMBAL_2,
                            CYMBAL_1 => TOM_2,
                            _        => lane
                        };
                    }
                    else
                    {
                        shift = lane switch
                        {
                            TOM_1    => CYMBAL_1,
                            TOM_3    => CYMBAL_3,
                            CYMBAL_1 => !lanes[TOM_3].IsActive() ? TOM_3 : TOM_2,
                            _        => lane
                        };
                    }

                    var laneMask = (DrumLaneMask)(1 << shift);

                    if (fret.Ticks >= chart.Settings.SustainCutoffThreshold)
                    {
                        rolls.Add(new DrumRoll(fret + note->Key, laneMask));
                    }

                    var dynamics = lane switch
                    {
                        1 => note->Value.Dynamics.Snare,
                        2 => note->Value.Dynamics.Yellow,
                        3 => note->Value.Dynamics.Blue,
                        4 => note->Value.Dynamics.Orange,
                        5 => note->Value.Dynamics.Green,
                        _ => DrumDynamics.None,
                    };

                    switch (dynamics)
                    {
                        case DrumDynamics.None:
                        {
                            group.NormalMask |= laneMask;
                            break;
                        }
                        case DrumDynamics.Accent:
                        {
                            group.AccentMask |= laneMask;
                            break;
                        }
                        case DrumDynamics.Ghost:
                        {
                            group.GhostMask |= laneMask;
                            break;
                        }
                        default:
                        {
                            throw new ArgumentOutOfRangeException();
                        }
                    }
                }

                // The actual final number of the notes can differ from the number of notes in the track
                // depending on the difficulty setup and sets of active modifiers that may restrict
                // certain lanes from appearing in the track
                if (group.NormalMask != DrumLaneMask.None
                ||  group.AccentMask != DrumLaneMask.None
                ||  group.GhostMask  != DrumLaneMask.None)
                {
                    continue;
                }

                group.OverdriveIndex = CommonTrackOps.GetHittablePhraseIndex(overdrives, note->Key, ref overdriveIndex);
                group.SoloIndex = CommonTrackOps.GetHittablePhraseIndex(solos, note->Key, ref soloIndex);

                positions[noteCount] = note->Key;
                groups[noteCount] = group;

                ++noteCount;
            }

            // noteCount may be less than the number of notes in the original track
            positions.Resize_NoInitialization(noteCount);
            groups.Resize_NoInitialization(noteCount);

            positions.TrimExcess();
            groups.TrimExcess();
            rolls.TrimExcess();
            overdrives.TrimExcess();
            solos.TrimExcess();

            return new DrumTrack(6, positions, groups, rolls, overdrives, solos);
        }
    }
}

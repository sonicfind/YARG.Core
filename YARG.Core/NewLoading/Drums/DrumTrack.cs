using System;
using System.Diagnostics;
using YARG.Core.Containers;
using YARG.Core.Game;
using YARG.Core.IO;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public class DrumTrack : IDisposable
    {
        public int                            NumLanes   { get; }
        public TimedCollection<DrumNoteGroup> Notes      { get; }
        public FixedArray<DrumRoll>           Rolls      { get; }
        public FixedArray<HittablePhrase>     Overdrives { get; }
        public FixedArray<HittablePhrase>     Solos      { get; }

        public DrumTrack Clone()
        {
            return new DrumTrack
            (
                NumLanes,
                Notes.Clone(),
                Rolls.Clone(),
                Overdrives.Clone(),
                Solos.Clone()
            );
        }

        public void Dispose()
        {
            Notes.Dispose();
            Rolls.Dispose();
            Overdrives.Dispose();
            Solos.Dispose();
        }

        private DrumTrack(
            int numLanes,
            TimedCollection<DrumNoteGroup> notes,
            FixedArray<DrumRoll>           rolls,
            FixedArray<HittablePhrase>     overdrives,
            FixedArray<HittablePhrase>     solos
        )
        {
            NumLanes = numLanes;
            Notes = notes;
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
            DualTime chartEndTime,
            InstrumentSelection selection
        )
        {
            var difficultyTrack = instrument[selection.Difficulty];

            using var notes = TimedCollection<DrumNoteGroup>.Create(difficultyTrack.Notes.Count);

            using var rolls = FixedArray<DrumRoll>.Alloc(difficultyTrack.Notes.Count);

            using var overdrives = CommonTrackOps.InitHittablePhrases(difficultyTrack.Overdrives);
            using var solos = CommonTrackOps.InitHittablePhrases(difficultyTrack.Solos);

            bool noKicks = (selection.Modifiers & Modifier.NoKicks) == Modifier.NoKicks;
            bool isExpertPlus = selection.Difficulty == Difficulty.ExpertPlus;

            // The actual final number of the notes can differ from the number of notes in the track
            // depending on the difficulty setup and sets of active modifiers
            int noteCount = 0;
            int rollCount = 0;

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

                if (!noKicks)
                {
                    var kick = note->Value.Lanes.Kick;
                    if (kick.IsActive()
                        && (note->Value.KickState == KickState.Shared
                            || isExpertPlus == (note->Value.KickState == KickState.PlusOnly))
                    )
                    {
                        if (kick.Ticks >= chart.Settings.SustainCutoffThreshold)
                        {
                            rolls[rollCount++] = new DrumRoll(note->Key, note->Key + kick, DrumLaneMask.Kick);
                        }
                        group.NormalMask |= DrumLaneMask.Kick;
                    }
                }

                var lanes = (DualTime*)&note->Value.Lanes;
                for (int lane = 1; lane < FourLaneDrums.NUM_LANES; lane++)
                {
                    var fret = lanes[lane];
                    if (!fret.IsActive())
                    {
                        continue;
                    }

                    DrumLaneMask laneMask;
                    switch (lane)
                    {
                        case SNARE:
                        {
                            laneMask = DrumLaneMask.Snare;
                            break;
                        }
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
                            throw new ArgumentOutOfRangeException();
                        }
                    }

                    if (fret.Ticks >= chart.Settings.SustainCutoffThreshold)
                    {
                        rolls[rollCount++] = new DrumRoll(note->Key, note->Key + fret, DrumLaneMask.Kick);
                    }

                    var dynamics = lane switch
                    {
                        1 => note->Value.Dynamics.Snare,
                        2 => note->Value.Dynamics.Yellow,
                        3 => note->Value.Dynamics.Blue,
                        4 => note->Value.Dynamics.Green,
                        _ => throw new ArgumentOutOfRangeException(),
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
                if (group.NormalMask == DrumLaneMask.None
                &&  group.AccentMask == DrumLaneMask.None
                &&  group.GhostMask  == DrumLaneMask.None)
                {
                    continue;
                }

                group.OverdriveIndex = CommonTrackOps.GetHittablePhraseIndex(overdrives, note->Key, ref overdriveIndex);
                group.SoloIndex = CommonTrackOps.GetHittablePhraseIndex(solos, note->Key, ref soloIndex);

                notes.Ticks[noteCount] = note->Key.Ticks;
                notes.Seconds[noteCount] = note->Key.Seconds;
                notes.Elements[noteCount] = group;
                noteCount++;
            }

            notes.Resize(noteCount);
            rolls.Resize(rollCount);

            return new DrumTrack(
                5,
                notes.TransferOwnership(),
                rolls.TransferOwnership(),
                overdrives.TransferOwnership(),
                solos.TransferOwnership()
            );
        }

        public static unsafe DrumTrack Create(
            YARGChart chart,
            InstrumentTrack2<FiveLaneDrums> instrument,
            DualTime chartEndTime,
            InstrumentSelection selection
        )
        {
            var difficultyTrack = instrument[selection.Difficulty];

            using var notes = TimedCollection<DrumNoteGroup>.Create(difficultyTrack.Notes.Count);

            using var rolls = FixedArray<DrumRoll>.Alloc(difficultyTrack.Notes.Count);

            using var overdrives = CommonTrackOps.InitHittablePhrases(difficultyTrack.Overdrives);
            using var solos = CommonTrackOps.InitHittablePhrases(difficultyTrack.Solos);

            bool noKicks = (selection.Modifiers & Modifier.NoKicks) == Modifier.NoKicks;
            bool isExpertPlus = selection.Difficulty == Difficulty.ExpertPlus;

            // The actual final number of the notes can differ from the number of notes in the track
            // depending on the difficulty setup and sets of active modifiers
            int noteCount = 0;
            int rollCount = 0;

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

                if (!noKicks)
                {
                    var kick = note->Value.Lanes.Kick;
                    if (kick.IsActive()
                        && (note->Value.KickState == KickState.Shared
                            || isExpertPlus == (note->Value.KickState == KickState.PlusOnly))
                    )
                    {
                        if (kick.Ticks >= chart.Settings.SustainCutoffThreshold)
                        {
                            rolls[rollCount++] = new DrumRoll(note->Key, note->Key + kick, DrumLaneMask.Kick);
                        }
                        group.NormalMask |= DrumLaneMask.Kick;
                    }
                }

                var lanes = (DualTime*)&note->Value.Lanes;
                for (int lane = 1; lane < FourLaneDrums.NUM_LANES; lane++)
                {
                    var fret = lanes[lane];
                    if (!fret.IsActive())
                    {
                        continue;
                    }

                    DrumLaneMask laneMask;
                    switch (lane)
                    {
                        case SNARE:
                        {
                            laneMask = DrumLaneMask.Snare;
                            break;
                        }
                        case TOM_1:
                        {
                            laneMask = DrumLaneMask.Cymbal_1;
                            break;
                        }
                        case TOM_2:
                        {
                            laneMask = selection.Instrument == Instrument.FiveLaneDrums
                                ? DrumLaneMask.Tom_1
                                : DrumLaneMask.Tom_2;
                            break;
                        }
                        case TOM_3:
                        {
                            laneMask = selection.Instrument == Instrument.FiveLaneDrums
                                ? DrumLaneMask.Cymbal_2
                                : DrumLaneMask.Cymbal_3;
                            break;
                        }
                        case CYMBAL_1:
                        {
                            if (selection.Instrument == Instrument.FiveLaneDrums)
                            {
                                laneMask = DrumLaneMask.Tom_2;
                            }
                            else if (!lanes[TOM_3].IsActive())
                            {
                                laneMask = DrumLaneMask.Tom_3;
                            }
                            else
                            {
                                laneMask = DrumLaneMask.Tom_2;
                            }
                            break;
                        }
                        default:
                        {
                            throw new ArgumentOutOfRangeException();
                        }
                    }

                    if (fret.Ticks >= chart.Settings.SustainCutoffThreshold)
                    {
                        rolls[rollCount++] = new DrumRoll(note->Key, note->Key + fret, DrumLaneMask.Kick);
                    }

                    var dynamics = lane switch
                    {
                        1 => note->Value.Dynamics.Snare,
                        2 => note->Value.Dynamics.Yellow,
                        3 => note->Value.Dynamics.Blue,
                        4 => note->Value.Dynamics.Orange,
                        5 => note->Value.Dynamics.Green,
                        _ => throw new ArgumentOutOfRangeException(),
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
                if (group.NormalMask == DrumLaneMask.None
                &&  group.AccentMask == DrumLaneMask.None
                &&  group.GhostMask  == DrumLaneMask.None)
                {
                    continue;
                }

                group.OverdriveIndex = CommonTrackOps.GetHittablePhraseIndex(overdrives, note->Key, ref overdriveIndex);
                group.SoloIndex = CommonTrackOps.GetHittablePhraseIndex(solos, note->Key, ref soloIndex);

                notes.Ticks[noteCount] = note->Key.Ticks;
                notes.Seconds[noteCount] = note->Key.Seconds;
                notes.Elements[noteCount] = group;
                noteCount++;
            }

            notes.Resize(noteCount);
            rolls.Resize(rollCount);

            return new DrumTrack(
                6,
                notes.TransferOwnership(),
                rolls.TransferOwnership(),
                overdrives.TransferOwnership(),
                solos.TransferOwnership()
            );
        }
    }
}

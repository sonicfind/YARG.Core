using System;
using System.Diagnostics;
using YARG.Core.Game;
using YARG.Core.NewLoading.Players;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading.FourLane
{
    public sealed class FourLanePlayer : InstrumentPlayer
    {
        public enum CymbalState
        {
            Off,
            On,
            NonPro,
        }

        public struct SubNote
        {
            public readonly int Index;
            public readonly CymbalState State;
            public readonly DrumDynamics Dynamics;
            public readonly DualTime EndPosition;
            public HitStatus Status;

            public SubNote(int index, CymbalState state, DrumDynamics dynamics, DualTime endPosition)
            {
                Index = index;
                State = state;
                Dynamics = dynamics;
                EndPosition = endPosition;
                Status = HitStatus.Idle;
            }
        }

        public readonly struct Note
        {
            public readonly DualTime StartPosition;
            public readonly SubNote[] Notes;
            public readonly bool Flam;
            public readonly int OverdriveIndex;
            public readonly int SoloIndex;

            public unsafe Note(in DualTime start, SubNote* notes, int laneCount, bool flam, int overdrive, int solo)
            {
                StartPosition = start;
                Notes = new SubNote[laneCount];
                for (int i = 0; i < laneCount; i++)
                {
                    Notes[i] = notes[i];
                }
                Flam = flam;
                OverdriveIndex = overdrive;
                SoloIndex = solo;
            }
        }

        private readonly Note[] _notes;
        private int _noteIndex = -1;


        private const int NUMLANES = 5;
        private const int BASS = 0;
        private const int SNARE = 1;
        private const int YELLOW = 2;
        private const int BLUE = 3;

        public unsafe FourLanePlayer(InstrumentTrack2<DifficultyTrack2<FourLaneDrums>> track, SyncTrack2 sync, YargProfile profile, long sustainCutoff)
            : base(sync, profile)
        {
            var diff = track[profile.CurrentDifficulty];
            Debug.Assert(diff != null, "This function should only be used with a valid difficulty");
            Debug.Assert(diff.Notes.Count > 0, "This function should only be used when notes are present");

            var overdriveRanges = diff.Phrases.Overdrives.Count > 0 ? diff.Phrases.Overdrives : track.Phrases.Overdrives;
            var soloRanges = diff.Phrases.Soloes.Count > 0 ? diff.Phrases.Soloes : track.Phrases.Soloes;

            var curr = diff.Notes.Data;
            var end = curr + diff.Notes.Count;

            var notes = new Note[diff.Notes.Count];
            _overdrives = new OverdrivePhrase[overdriveRanges.Count];
            _soloes = new SoloPhrase[soloRanges.Count];

            int numNotes = 0;
            int currOverdrive = 0;
            int overdriveNoteCount = 0;
            int currSolo = 0;
            int soloNoteCount = 0;

            bool disableKick = (profile.CurrentModifiers & Modifier.NoKicks) > 0;
            bool isExpertPlus = profile.CurrentDifficulty == Difficulty.ExpertPlus;
            bool isProDrums = profile.CurrentInstrument == Instrument.ProDrums;

            var buffer = stackalloc SubNote[NUMLANES];
            while (curr < end)
            {
                while (currOverdrive < overdriveRanges.Count)
                {
                    ref readonly var ovd = ref overdriveRanges.Data[currOverdrive];
                    if (curr->Key < ovd.Key + ovd.Value)
                    {
                        break;
                    }
                    _overdrives[currOverdrive++] = new OverdrivePhrase(ovd.Key, overdriveNoteCount);
                    overdriveNoteCount = 0;
                }

                while (currSolo < soloRanges.Count)
                {
                    ref readonly var solo = ref soloRanges.Data[currSolo];
                    var soloEnd = solo.Key + solo.Value;
                    if (curr->Key < soloEnd)
                    {
                        break;
                    }
                    _soloes[currSolo++] = new SoloPhrase(solo.Key, soloEnd, soloNoteCount);
                    soloNoteCount = 0;
                }

                int laneCount = 0;
                var lanes = &curr->Value.Kick;
                for (int i = 0; i < NUMLANES; ++i)
                {
                    // We only add active notes under certain conditions:
                    // A. The lane is NOT the kick lane
                    // B. If kick lane:
                    // -- 1. The user did not disable kicks &&
                    // -- 2. The kickstate enables use regardless of difficulty OR simply matches the current difficulty
                    if (lanes[i].IsActive() && (i >= SNARE || (!disableKick && (curr->Value.KickState == KickState.Shared || (isExpertPlus == (curr->Value.KickState == KickState.PlusOnly))))))
                    {
                        int index = !Profile.LeftyFlip || i < SNARE ? i : NUMLANES - i;
                        var state = isProDrums
                            ? i >= YELLOW && (&curr->Value.Cymbal_Yellow)[i - YELLOW] ? CymbalState.On : CymbalState.Off
                            : CymbalState.NonPro;
                        var dynamics = i >= SNARE ? (&curr->Value.Dynamics_Snare)[i - SNARE] : DrumDynamics.None;
                        buffer[laneCount++] = new SubNote(index, state, dynamics, DualTime.Truncate(lanes[i], sustainCutoff) + curr->Key);
                    }
                }

                if (laneCount > 0)
                {
                    int ovdIndex = -1;
                    if (currOverdrive < overdriveRanges.Count && curr->Key >= overdriveRanges.Data[currOverdrive].Key)
                    {
                        overdriveNoteCount++;
                        ovdIndex = currOverdrive;
                    }

                    int soloIndex = -1;
                    if (currSolo < soloRanges.Count && curr->Key >= soloRanges.Data[currSolo].Key)
                    {
                        soloNoteCount++;
                        soloIndex = currSolo;
                    }

                    bool flamActive = curr->Value.IsFlammed && (lanes[BASS].IsActive() ? laneCount == 2 : laneCount == 1);
                    notes[numNotes++] = new Note(curr->Key, buffer, laneCount, flamActive, ovdIndex, soloIndex);
                }
                ++curr;
            }

            while (currOverdrive < overdriveRanges.Count)
            {
                ref readonly var ovd = ref overdriveRanges.Data[currOverdrive];
                _overdrives[currOverdrive] = new OverdrivePhrase(ovd.Key, overdriveNoteCount);
                ++currOverdrive;
                overdriveNoteCount = 0;
            }

            while (currSolo < soloRanges.Count)
            {
                ref readonly var solo = ref soloRanges.Data[currSolo];
                _soloes[currSolo] = new SoloPhrase(solo.Key, solo.Key + solo.Value, soloNoteCount);
                ++currSolo;
                soloNoteCount = 0;
            }
            _notes = notes[..numNotes];
        }

        public unsafe FourLanePlayer(InstrumentTrack2<DifficultyTrack2<FiveLaneDrums>> track, SyncTrack2 sync, YargProfile profile, long sustainCutoff)
            : base(sync, profile)
        {
            const int FIVELANECOUNT = 6;
            const int ORANGE = 4;
            const int GREEN = 5;

            var diff = track[profile.CurrentDifficulty];
            Debug.Assert(diff != null, "This function should only be used with a valid difficulty");
            Debug.Assert(diff.Notes.Count > 0, "This function should only be used when notes are present");

            var overdriveRanges = diff.Phrases.Overdrives.Count > 0 ? diff.Phrases.Overdrives : track.Phrases.Overdrives;
            var soloRanges = diff.Phrases.Soloes.Count > 0 ? diff.Phrases.Soloes : track.Phrases.Soloes;

            var curr = diff.Notes.Data;
            var end = curr + diff.Notes.Count;

            var notes = new Note[diff.Notes.Count];
            _overdrives = new OverdrivePhrase[overdriveRanges.Count];
            _soloes = new SoloPhrase[soloRanges.Count];

            int numNotes = 0;
            int currOverdrive = 0;
            int overdriveNoteCount = 0;
            int currSolo = 0;
            int soloNoteCount = 0;

            bool disableKick = (profile.CurrentModifiers & Modifier.NoKicks) > 0;
            bool isExpertPlus = profile.CurrentDifficulty == Difficulty.ExpertPlus;
            bool isProDrums = profile.CurrentInstrument == Instrument.ProDrums;

            var buffer = stackalloc SubNote[NUMLANES];
            while (curr < end)
            {
                while (currOverdrive < overdriveRanges.Count)
                {
                    ref readonly var ovd = ref overdriveRanges.Data[currOverdrive];
                    if (curr->Key < ovd.Key + ovd.Value)
                    {
                        break;
                    }
                    _overdrives[currOverdrive++] = new OverdrivePhrase(ovd.Key, overdriveNoteCount);
                    overdriveNoteCount = 0;
                }

                while (currSolo < soloRanges.Count)
                {
                    ref readonly var solo = ref soloRanges.Data[currSolo];
                    var soloEnd = solo.Key + solo.Value;
                    if (curr->Key < soloEnd)
                    {
                        break;
                    }
                    _soloes[currSolo++] = new SoloPhrase(solo.Key, soloEnd, soloNoteCount);
                    soloNoteCount = 0;
                }

                int laneCount = 0;
                var lanes = (DualTime*) &curr->Value;
                for (int i = 0; i < FIVELANECOUNT; ++i)
                {
                    // We only add active notes under certain conditions:
                    // A. The lane is NOT the kick lane
                    // B. If kick lane:
                    // -- 1. The user did not disable kicks &&
                    // -- 2. The kickstate enables use regardless of difficulty OR simply matches the current difficulty
                    if (lanes[i].IsActive() && (i >= SNARE || (!disableKick && (curr->Value.KickState == KickState.Shared || (isExpertPlus == (curr->Value.KickState == KickState.PlusOnly))))))
                    {
                        var dynamics = i >= SNARE ? (&curr->Value.Dynamics_Snare)[i - SNARE] : DrumDynamics.None;
                        int index = i switch
                        {
                            // Handles the collision between Green cymbal and Green tom
                            // by making the tom blue
                            GREEN => lanes[ORANGE].IsActive() ? BLUE : ORANGE,
                            _ => i,
                        };

                        if (Profile.LeftyFlip && index >= SNARE)
                        {
                            index = NUMLANES - index;
                        }

                        var state = isProDrums
                            ? i == YELLOW || i == ORANGE ? CymbalState.On : CymbalState.Off
                            : CymbalState.NonPro;
                        buffer[laneCount++] = new SubNote(index, state, dynamics, DualTime.Truncate(lanes[i], sustainCutoff) + curr->Key);
                    }
                }

                if (laneCount > 0)
                {
                    int ovdIndex = -1;
                    if (currOverdrive < overdriveRanges.Count && curr->Key >= overdriveRanges.Data[currOverdrive].Key)
                    {
                        overdriveNoteCount++;
                        ovdIndex = currOverdrive;
                    }

                    int soloIndex = -1;
                    if (currSolo < soloRanges.Count && curr->Key >= soloRanges.Data[currSolo].Key)
                    {
                        soloNoteCount++;
                        soloIndex = currSolo;
                    }
                    bool flamActive = curr->Value.IsFlammed && (lanes[BASS].IsActive() ? laneCount == 2 : laneCount == 1);
                    notes[numNotes++] = new Note(curr->Key, buffer, laneCount, flamActive, ovdIndex, soloIndex);
                }
                ++curr;
            }

            while (currOverdrive < overdriveRanges.Count)
            {
                ref readonly var ovd = ref overdriveRanges.Data[currOverdrive];
                _overdrives[currOverdrive] = new OverdrivePhrase(ovd.Key, overdriveNoteCount);
                ++currOverdrive;
                overdriveNoteCount = 0;
            }

            while (currSolo < soloRanges.Count)
            {
                ref readonly var solo = ref soloRanges.Data[currSolo];
                _soloes[currSolo] = new SoloPhrase(solo.Key, solo.Key + solo.Value, soloNoteCount);
                ++currSolo;
                soloNoteCount = 0;
            }
            _notes = notes[..numNotes];
        }

        public override void Set(in DualTime startTime, in DualTime endTime)
        {
        }
    }
}

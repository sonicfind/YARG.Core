using System.Diagnostics;
using YARG.Core.Containers;
using YARG.Core.Game;
using YARG.Core.IO;
using YARG.Core.NewLoading.Players;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading.Drums
{
    public enum CymbalState
    {
        Off,
        On,
        NonPro,
    }

    public struct FourLaneSubNote
    {
        public readonly int Index;
        public readonly CymbalState State;
        public readonly DrumDynamics Dynamics;
        public readonly DualTime EndPosition;
        public HitStatus Status;

        public FourLaneSubNote(int index, CymbalState state, DrumDynamics dynamics, DualTime endPosition)
        {
            Index = index;
            State = state;
            Dynamics = dynamics;
            EndPosition = endPosition;
            Status = HitStatus.Idle;
        }
    }

    public struct FiveLaneSubNote
    {
        public readonly int Index;
        public readonly DrumDynamics Dynamics;
        public readonly DualTime EndPosition;
        public HitStatus Status;

        public FiveLaneSubNote(int index, DrumDynamics dynamics, DualTime endPosition)
        {
            Index = index;
            Dynamics = dynamics;
            EndPosition = endPosition;
            Status = HitStatus.Idle;
        }
    }

    public readonly struct Note
    {
        public readonly int NoteIndex;
        public readonly int LaneCount;
        public readonly bool Flam;
        public readonly int OverdriveIndex;
        public readonly int SoloIndex;

        public unsafe Note(int noteIndex, int laneCount, bool flam, int overdrive, int solo)
        {
            NoteIndex = noteIndex;
            LaneCount = laneCount;
            Flam = flam;
            OverdriveIndex = overdrive;
            SoloIndex = solo;
        }
    }

    public static class DrumPlayer
    {
        private const int FOURLANECOUNT = 5;
        private const int FIVELANECOUNT = 6;
        private const int BASS = 0;
        private const int SNARE = 1;
        private const int YELLOW = 2;
        private const int BLUE = 3;
        private const int ORANGE = 4;
        private const int GREEN = 5;

        public static unsafe InstrumentPlayer<Note, FourLaneSubNote> LoadFourLane(InstrumentTrack2<FourLaneDrums> track, SyncTrack2 sync, YargProfile profile, long sustainCutoff)
        {
            ref readonly var diff = ref track.Difficulties[profile.CurrentDifficulty];
            Debug.Assert(diff.Notes.Count > 0, "This function should only be used when notes are present");

            var curr = diff.Notes.Data;
            var end = curr + diff.Notes.Count;

            var notes = new YARGNativeSortedList<DualTime, Note>()
            {
                Capacity = diff.Notes.Count,
            };

            var subNotes = new YARGNativeList<FourLaneSubNote>()
            {
                Capacity = diff.Notes.Count * 2,
            };

            var overdrives = FixedArray<OverdrivePhrase>.Alloc(diff.Overdrives.Count);
            var soloes = FixedArray<SoloPhrase>.Alloc(diff.Soloes.Count);

            int currOverdrive = 0;
            int overdriveNoteCount = 0;
            int currSolo = 0;
            int soloNoteCount = 0;

            bool disableKick = (profile.CurrentModifiers & Modifier.NoKicks) > 0;
            bool isExpertPlus = profile.CurrentDifficulty == Difficulty.ExpertPlus;
            bool isProDrums = profile.CurrentInstrument == Instrument.ProDrums;

            var buffer = stackalloc FourLaneSubNote[FOURLANECOUNT];
            while (curr < end)
            {
                while (currOverdrive < overdrives.Length)
                {
                    ref readonly var ovd = ref diff.Overdrives.Data[currOverdrive];
                    if (curr->Key < ovd.Key + ovd.Value)
                    {
                        break;
                    }
                    overdrives[currOverdrive++] = new OverdrivePhrase(ovd.Key, overdriveNoteCount);
                    overdriveNoteCount = 0;
                }

                while (currSolo < soloes.Length)
                {
                    ref readonly var solo = ref diff.Soloes.Data[currSolo];
                    var soloEnd = solo.Key + solo.Value;
                    if (curr->Key < soloEnd)
                    {
                        break;
                    }
                    soloes[currSolo++] = new SoloPhrase(solo.Key, soloEnd, soloNoteCount);
                    soloNoteCount = 0;
                }

                int laneCount = 0;
                var lanes = (DualTime*) &curr->Value;
                for (int i = 0; i < FOURLANECOUNT; ++i)
                {
                    if (lanes[i].IsActive() && (i >= SNARE || (!disableKick && (curr->Value.KickState == KickState.Shared || (isExpertPlus == (curr->Value.KickState == KickState.PlusOnly))))))
                    {
                        int index = !profile.LeftyFlip || i < SNARE ? i : FOURLANECOUNT - i;
                        var state = isProDrums
                            ? i >= YELLOW && (&curr->Value.Cymbal_Yellow)[i - YELLOW] ? CymbalState.On : CymbalState.Off
                            : CymbalState.NonPro;
                        var dynamics = i >= SNARE ? (&curr->Value.Dynamics_Snare)[i - SNARE] : DrumDynamics.None;
                        buffer[laneCount++] = new FourLaneSubNote(index, state, dynamics, DualTime.Truncate(lanes[i], sustainCutoff) + curr->Key);
                    }
                }

                if (laneCount > 0)
                {
                    int ovdIndex = -1;
                    if (currOverdrive < overdrives.Length && curr->Key >= diff.Overdrives.Data[currOverdrive].Key)
                    {
                        overdriveNoteCount++;
                        ovdIndex = currOverdrive;
                    }

                    int soloIndex = -1;
                    if (currSolo < soloes.Length && curr->Key >= diff.Soloes.Data[currSolo].Key)
                    {
                        soloNoteCount++;
                        soloIndex = currSolo;
                    }

                    bool flamActive = curr->Value.IsFlammed && (lanes[BASS].IsActive() ? laneCount == 2 : laneCount == 1);
                    notes.Append(curr->Key, new Note(subNotes.Count, laneCount, flamActive, ovdIndex, soloIndex));
                    subNotes.AddRange(buffer, laneCount);
                }
                ++curr;
            }

            while (currOverdrive < overdrives.Length)
            {
                ref readonly var ovd = ref diff.Overdrives.Data[currOverdrive];
                overdrives[currOverdrive] = new OverdrivePhrase(ovd.Key, overdriveNoteCount);
                ++currOverdrive;
                overdriveNoteCount = 0;
            }

            while (currSolo < soloes.Length)
            {
                ref readonly var solo = ref diff.Soloes.Data[currSolo];
                soloes[currSolo] = new SoloPhrase(solo.Key, solo.Key + solo.Value, soloNoteCount);
                ++currSolo;
                soloNoteCount = 0;
            }
            notes.TrimExcess();
            subNotes.TrimExcess();
            return new InstrumentPlayer<Note, FourLaneSubNote>(in notes, in subNotes, in soloes, in overdrives, sync, profile);
        }

        public static unsafe InstrumentPlayer<Note, FourLaneSubNote> LoadFourLane(InstrumentTrack2<FiveLaneDrums> track, SyncTrack2 sync, YargProfile profile, long sustainCutoff)
        {
            ref readonly var diff = ref track.Difficulties[profile.CurrentDifficulty];
            Debug.Assert(diff.Notes.Count > 0, "This function should only be used when notes are present");

            var curr = diff.Notes.Data;
            var end = curr + diff.Notes.Count;

            var notes = new YARGNativeSortedList<DualTime, Note>()
            {
                Capacity = diff.Notes.Count,
            };

            var subNotes = new YARGNativeList<FourLaneSubNote>()
            {
                Capacity = diff.Notes.Count * 2,
            };

            var overdrives = FixedArray<OverdrivePhrase>.Alloc(diff.Overdrives.Count);
            var soloes = FixedArray<SoloPhrase>.Alloc(diff.Soloes.Count);

            int currOverdrive = 0;
            int overdriveNoteCount = 0;
            int currSolo = 0;
            int soloNoteCount = 0;

            bool disableKick = (profile.CurrentModifiers & Modifier.NoKicks) > 0;
            bool isExpertPlus = profile.CurrentDifficulty == Difficulty.ExpertPlus;
            bool isProDrums = profile.CurrentInstrument == Instrument.ProDrums;

            var buffer = stackalloc FourLaneSubNote[FOURLANECOUNT];
            while (curr < end)
            {
                while (currOverdrive < overdrives.Length)
                {
                    ref readonly var ovd = ref diff.Overdrives.Data[currOverdrive];
                    if (curr->Key < ovd.Key + ovd.Value)
                    {
                        break;
                    }
                    overdrives[currOverdrive++] = new OverdrivePhrase(ovd.Key, overdriveNoteCount);
                    overdriveNoteCount = 0;
                }

                while (currSolo < soloes.Length)
                {
                    ref readonly var solo = ref diff.Soloes.Data[currSolo];
                    var soloEnd = solo.Key + solo.Value;
                    if (curr->Key < soloEnd)
                    {
                        break;
                    }
                    soloes[currSolo++] = new SoloPhrase(solo.Key, soloEnd, soloNoteCount);
                    soloNoteCount = 0;
                }

                int laneCount = 0;
                var lanes = (DualTime*) &curr->Value;
                for (int i = 0; i < FIVELANECOUNT; ++i)
                {
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

                        if (profile.LeftyFlip && index >= SNARE)
                        {
                            index = FOURLANECOUNT - index;
                        }

                        var state = isProDrums
                            ? i == YELLOW || i == ORANGE ? CymbalState.On : CymbalState.Off
                            : CymbalState.NonPro;
                        buffer[laneCount++] = new FourLaneSubNote(index, state, dynamics, DualTime.Truncate(lanes[i], sustainCutoff) + curr->Key);
                    }
                }

                if (laneCount > 0)
                {
                    int ovdIndex = -1;
                    if (currOverdrive < overdrives.Length && curr->Key >= diff.Overdrives.Data[currOverdrive].Key)
                    {
                        overdriveNoteCount++;
                        ovdIndex = currOverdrive;
                    }

                    int soloIndex = -1;
                    if (currSolo < soloes.Length && curr->Key >= diff.Soloes.Data[currSolo].Key)
                    {
                        soloNoteCount++;
                        soloIndex = currSolo;
                    }
                    bool flamActive = curr->Value.IsFlammed && (lanes[BASS].IsActive() ? laneCount == 2 : laneCount == 1);
                    notes.Append(curr->Key, new Note(subNotes.Count, laneCount, flamActive, ovdIndex, soloIndex));
                    subNotes.AddRange(buffer, laneCount);
                }
                ++curr;
            }

            while (currOverdrive < overdrives.Length)
            {
                ref readonly var ovd = ref diff.Overdrives.Data[currOverdrive];
                overdrives[currOverdrive] = new OverdrivePhrase(ovd.Key, overdriveNoteCount);
                ++currOverdrive;
                overdriveNoteCount = 0;
            }

            while (currSolo < soloes.Length)
            {
                ref readonly var solo = ref diff.Soloes.Data[currSolo];
                soloes[currSolo] = new SoloPhrase(solo.Key, solo.Key + solo.Value, soloNoteCount);
                ++currSolo;
                soloNoteCount = 0;
            }
            notes.TrimExcess();
            subNotes.TrimExcess();
            return new InstrumentPlayer<Note, FourLaneSubNote>(in notes, in subNotes, in soloes, in overdrives, sync, profile);
        }

        public static unsafe InstrumentPlayer<Note, FiveLaneSubNote> LoadFiveLane(InstrumentTrack2<FiveLaneDrums> track, SyncTrack2 sync, YargProfile profile, long sustainCutoff)
        {
            ref readonly var diff = ref track.Difficulties[profile.CurrentDifficulty];
            Debug.Assert(diff.Notes.Count > 0, "This function should only be used when notes are present");

            var curr = diff.Notes.Data;
            var end = curr + diff.Notes.Count;

            var notes = new YARGNativeSortedList<DualTime, Note>()
            {
                Capacity = diff.Notes.Count,
            };

            var subNotes = new YARGNativeList<FiveLaneSubNote>()
            {
                Capacity = diff.Notes.Count * 2,
            };

            var overdrives = FixedArray<OverdrivePhrase>.Alloc(diff.Overdrives.Count);
            var soloes = FixedArray<SoloPhrase>.Alloc(diff.Soloes.Count);

            int currOverdrive = 0;
            int overdriveNoteCount = 0;
            int currSolo = 0;
            int soloNoteCount = 0;

            bool disableKick = (profile.CurrentModifiers & Modifier.NoKicks) > 0;
            bool isExpertPlus = profile.CurrentDifficulty == Difficulty.ExpertPlus;
            bool isProDrums = profile.CurrentInstrument == Instrument.ProDrums;

            var buffer = stackalloc FiveLaneSubNote[FIVELANECOUNT];
            while (curr < end)
            {
                while (currOverdrive < overdrives.Length)
                {
                    ref readonly var ovd = ref diff.Overdrives.Data[currOverdrive];
                    if (curr->Key < ovd.Key + ovd.Value)
                    {
                        break;
                    }
                    overdrives[currOverdrive++] = new OverdrivePhrase(ovd.Key, overdriveNoteCount);
                    overdriveNoteCount = 0;
                }

                while (currSolo < soloes.Length)
                {
                    ref readonly var solo = ref diff.Soloes.Data[currSolo];
                    var soloEnd = solo.Key + solo.Value;
                    if (curr->Key < soloEnd)
                    {
                        break;
                    }
                    soloes[currSolo++] = new SoloPhrase(solo.Key, soloEnd, soloNoteCount);
                    soloNoteCount = 0;
                }

                int laneCount = 0;
                var lanes = (DualTime*) &curr->Value;
                for (int i = 0; i < FIVELANECOUNT; ++i)
                {
                    if (lanes[i].IsActive() && (i >= SNARE || (!disableKick && (curr->Value.KickState == KickState.Shared || (isExpertPlus == (curr->Value.KickState == KickState.PlusOnly))))))
                    {
                        int index = !profile.LeftyFlip || i < SNARE ? i : FIVELANECOUNT - i;
                        var dynamics = i >= SNARE ? (&curr->Value.Dynamics_Snare)[i - SNARE] : DrumDynamics.None;
                        buffer[laneCount++] = new FiveLaneSubNote(index, dynamics, DualTime.Truncate(lanes[i], sustainCutoff) + curr->Key);
                    }
                }

                if (laneCount > 0)
                {
                    int ovdIndex = -1;
                    if (currOverdrive < overdrives.Length && curr->Key >= diff.Overdrives.Data[currOverdrive].Key)
                    {
                        overdriveNoteCount++;
                        ovdIndex = currOverdrive;
                    }

                    int soloIndex = -1;
                    if (currSolo < soloes.Length && curr->Key >= diff.Soloes.Data[currSolo].Key)
                    {
                        soloNoteCount++;
                        soloIndex = currSolo;
                    }
                    bool flamActive = curr->Value.IsFlammed && (lanes[BASS].IsActive() ? laneCount == 2 : laneCount == 1);
                    notes.Append(curr->Key, new Note(subNotes.Count, laneCount, flamActive, ovdIndex, soloIndex));
                    subNotes.AddRange(buffer, laneCount);
                }
                ++curr;
            }

            while (currOverdrive < overdrives.Length)
            {
                ref readonly var ovd = ref diff.Overdrives.Data[currOverdrive];
                overdrives[currOverdrive] = new OverdrivePhrase(ovd.Key, overdriveNoteCount);
                ++currOverdrive;
                overdriveNoteCount = 0;
            }

            while (currSolo < soloes.Length)
            {
                ref readonly var solo = ref diff.Soloes.Data[currSolo];
                soloes[currSolo] = new SoloPhrase(solo.Key, solo.Key + solo.Value, soloNoteCount);
                ++currSolo;
                soloNoteCount = 0;
            }
            notes.TrimExcess();
            subNotes.TrimExcess();
            return new InstrumentPlayer<Note, FiveLaneSubNote>(in notes, in subNotes, in soloes, in overdrives, sync, profile);
        }

        public static unsafe InstrumentPlayer<Note, FiveLaneSubNote> LoadFiveLane(InstrumentTrack2<FourLaneDrums> track, SyncTrack2 sync, YargProfile profile, long sustainCutoff)
        {
            const int FOURLANECOUNT = 5;

            ref readonly var diff = ref track.Difficulties[profile.CurrentDifficulty];
            Debug.Assert(diff.Notes.Count > 0, "This function should only be used when notes are present");

            var curr = diff.Notes.Data;
            var end = curr + diff.Notes.Count;

            var notes = new YARGNativeSortedList<DualTime, Note>()
            {
                Capacity = diff.Notes.Count,
            };

            var subNotes = new YARGNativeList<FiveLaneSubNote>()
            {
                Capacity = diff.Notes.Count * 2,
            };

            var overdrives = FixedArray<OverdrivePhrase>.Alloc(diff.Overdrives.Count);
            var soloes = FixedArray<SoloPhrase>.Alloc(diff.Soloes.Count);

            int currOverdrive = 0;
            int overdriveNoteCount = 0;
            int currSolo = 0;
            int soloNoteCount = 0;

            bool disableKick = (profile.CurrentModifiers & Modifier.NoKicks) > 0;
            bool isExpertPlus = profile.CurrentDifficulty == Difficulty.ExpertPlus;
            bool isProDrums = profile.CurrentInstrument == Instrument.ProDrums;

            var buffer = stackalloc FiveLaneSubNote[FIVELANECOUNT];
            while (curr < end)
            {
                while (currOverdrive < overdrives.Length)
                {
                    ref readonly var ovd = ref diff.Overdrives.Data[currOverdrive];
                    if (curr->Key < ovd.Key + ovd.Value)
                    {
                        break;
                    }
                    overdrives[currOverdrive++] = new OverdrivePhrase(ovd.Key, overdriveNoteCount);
                    overdriveNoteCount = 0;
                }

                while (currSolo < soloes.Length)
                {
                    ref readonly var solo = ref diff.Soloes.Data[currSolo];
                    var soloEnd = solo.Key + solo.Value;
                    if (curr->Key < soloEnd)
                    {
                        break;
                    }
                    soloes[currSolo++] = new SoloPhrase(solo.Key, soloEnd, soloNoteCount);
                    soloNoteCount = 0;
                }

                int laneCount = 0;
                var lanes = (DualTime*)&curr->Value;
                for (int i = 0; i < FOURLANECOUNT; ++i)
                {
                    if (lanes[i].IsActive() && (i >= SNARE || (!disableKick && (curr->Value.KickState == KickState.Shared || (isExpertPlus == (curr->Value.KickState == KickState.PlusOnly))))))
                    {
                        var dynamics = i >= SNARE ? DrumDynamics.None : (&curr->Value.Dynamics_Snare)[i - SNARE];
                        int index = i;
                        switch (i)
                        {
                            case YELLOW:
                                if (!curr->Value.Cymbal_Yellow)
                                {
                                    index = lanes[BLUE].IsActive() && !curr->Value.Cymbal_Blue ? SNARE : YELLOW;
                                }
                                break;
                            case BLUE:
                                if (curr->Value.Cymbal_Blue)
                                {
                                    // Fivelane orange == fourlane green
                                    index = lanes[ORANGE].IsActive() && curr->Value.Cymbal_Green ? YELLOW : ORANGE;
                                }
                                break;
                            // Fivelane orange == fourlane green
                            case ORANGE:
                                if (!curr->Value.Cymbal_Green)
                                {
                                    index = GREEN;
                                }
                                break;
                        }

                        if (profile.LeftyFlip && index >= SNARE)
                        {
                            index = FIVELANECOUNT - index;
                        }
                        buffer[laneCount++] = new FiveLaneSubNote(index, dynamics, DualTime.Truncate(lanes[i], sustainCutoff) + curr->Key);
                    }
                }

                if (laneCount > 0)
                {
                    int ovdIndex = -1;
                    if (currOverdrive < overdrives.Length && curr->Key >= diff.Overdrives.Data[currOverdrive].Key)
                    {
                        overdriveNoteCount++;
                        ovdIndex = currOverdrive;
                    }

                    int soloIndex = -1;
                    if (currSolo < soloes.Length && curr->Key >= diff.Soloes.Data[currSolo].Key)
                    {
                        soloNoteCount++;
                        soloIndex = currSolo;
                    }

                    bool flamActive = curr->Value.IsFlammed && (lanes[BASS].IsActive() ? laneCount == 2 : laneCount == 1);
                    notes.Append(curr->Key, new Note(subNotes.Count, laneCount, flamActive, ovdIndex, soloIndex));
                    subNotes.AddRange(buffer, laneCount);
                }
                ++curr;
            }

            while (currOverdrive < overdrives.Length)
            {
                ref readonly var ovd = ref diff.Overdrives.Data[currOverdrive];
                overdrives[currOverdrive] = new OverdrivePhrase(ovd.Key, overdriveNoteCount);
                ++currOverdrive;
                overdriveNoteCount = 0;
            }

            while (currSolo < soloes.Length)
            {
                ref readonly var solo = ref diff.Soloes.Data[currSolo];
                soloes[currSolo] = new SoloPhrase(solo.Key, solo.Key + solo.Value, soloNoteCount);
                ++currSolo;
                soloNoteCount = 0;
            }
            notes.TrimExcess();
            subNotes.TrimExcess();
            return new InstrumentPlayer<Note, FiveLaneSubNote>(in notes, in subNotes, in soloes, in overdrives, sync, profile);
        }
    }
}

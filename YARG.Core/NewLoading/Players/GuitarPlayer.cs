using System.Diagnostics;
using YARG.Core.Containers;
using YARG.Core.Game;
using YARG.Core.NewParsing;
using YARG.Core.Song;

namespace YARG.Core.NewLoading.Guitar
{
    public static class GuitarPlayerLoader
    {
        public struct SubNote
        {
            public readonly int Index;
            public readonly DualTime EndPosition;
            public HitStatus Status;

            public SubNote(int index, DualTime endPosition)
            {
                Index = index;
                EndPosition = endPosition;
                Status = HitStatus.Idle;
            }
        }

        public readonly struct Note
        {
            public readonly DualTime StartPosition;
            public readonly GuitarState State;
            public readonly SubNote[] Notes;
            public readonly int OverdriveIndex;
            public readonly int SoloIndex;

            public unsafe Note(in DualTime start, GuitarState state, SubNote* notes, int laneCount, int overdrive, int solo)
            {
                StartPosition = start;
                State = state;
                Notes = new SubNote[laneCount];
                for (int i = 0; i < laneCount; i++)
                {
                    Notes[i] = notes[i];
                }
                OverdriveIndex = overdrive;
                SoloIndex = solo;
            }
        }

        internal static unsafe (Note[], OverdrivePhrase[], SoloPhrase[]) Load<TNote>(InstrumentTrack2<DifficultyTrack2<TNote>> track, YargProfile profile, in LoaderSettings settings)
            where TNote : unmanaged, IGuitarNote
        {
            var diff = track[profile.CurrentDifficulty];
            Debug.Assert(diff != null, "This function should only be used with a valid difficulty");
            Debug.Assert(diff.Notes.Count > 0, "This function should only be used when notes are present");

            var modifiers = profile.CurrentModifiers;
            var overdriveRanges = diff.Phrases.Overdrives.Count > 0 ? diff.Phrases.Overdrives : track.Phrases.Overdrives;
            var soloRanges = diff.Phrases.Soloes.Count > 0 ? diff.Phrases.Soloes : track.Phrases.Soloes;

            var curr = diff.Notes.Data;
            var end = curr + diff.Notes.Count;

            var notes = new Note[diff.Notes.Count];
            var overdrives = new OverdrivePhrase[overdriveRanges.Count];
            var soloes = new SoloPhrase[soloRanges.Count];

            int numNotes = 0;
            int currOverdrive = 0;
            int overdriveNoteCount = 0;
            int currSolo = 0;
            int soloNoteCount = 0;

            YARGKeyValuePair<DualTime, TNote>* prev = null;
            var buffer = stackalloc SubNote[6];
            while (curr < end)
            {
                while (currOverdrive < overdriveRanges.Count)
                {
                    ref readonly var ovd = ref overdriveRanges.Data[currOverdrive];
                    if (curr->Key < ovd.Key + ovd.Value)
                    {
                        break;
                    }
                    overdrives[currOverdrive++] = new OverdrivePhrase(ovd.Key, overdriveNoteCount);
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
                    soloes[currSolo++] = new SoloPhrase(solo.Key, soloEnd, soloNoteCount);
                    soloNoteCount = 0;
                }

                const int OPEN_NOTE = 0;
                var frets = (DualTime*) &curr->Value;
                int laneCount = 0;
                for (int i = 0; i < curr->Value.NUMLANES; ++i)
                {
                    if (frets[i].IsActive())
                    {
                        int index = !profile.LeftyFlip || i == OPEN_NOTE ? i : curr->Value.NUMLANES - i;
                        buffer[laneCount++] = new SubNote(index, DualTime.Truncate(frets[i], settings.SustainCutoffThreshold) + curr->Key);
                    }
                }

                if (laneCount > 0)
                {
                    var state = ParseGuitarState(curr, prev, curr->Value.State, modifiers, settings.HopoThreshold, settings.AllowHopoAfterChord);
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
                    notes[numNotes++] = new Note(curr->Key, state, buffer, laneCount, ovdIndex, soloIndex);
                }
                prev = curr;
                ++curr;
            }

            while (currOverdrive < overdriveRanges.Count)
            {
                ref readonly var ovd = ref overdriveRanges.Data[currOverdrive];
                overdrives[currOverdrive] = new OverdrivePhrase(ovd.Key, overdriveNoteCount);
                ++currOverdrive;
                overdriveNoteCount = 0;
            }

            while (currSolo < soloRanges.Count)
            {
                ref readonly var solo = ref soloRanges.Data[currSolo];
                soloes[currSolo] = new SoloPhrase(solo.Key, solo.Key + solo.Value, soloNoteCount);
                ++currSolo;
                soloNoteCount = 0;
            }
            return (notes[..numNotes], overdrives, soloes);
        }

        private static unsafe GuitarState ParseGuitarState<TNote>(YARGKeyValuePair<DualTime, TNote>* curr, YARGKeyValuePair<DualTime, TNote>* prev, GuitarState state, Modifier modifiers, long hopoThreshold, bool allowHopoAfterChord)
            where TNote : unmanaged, IGuitarNote
        {
            if ((modifiers & Modifier.AllStrums) > 0)
            {
                return GuitarState.Strum;
            }

            if ((modifiers & Modifier.AllTaps) > 0)
            {
                return GuitarState.Tap;
            }

            if ((modifiers & Modifier.AllHopos) > 0)
            {
                return GuitarState.Hopo;
            }

            if (state == GuitarState.Hopo)
            {
                if ((modifiers & Modifier.HoposToTaps) > 0)
                {
                    return GuitarState.Tap;
                }
            }
            else if (state != GuitarState.Strum && (state != GuitarState.Tap || (modifiers & Modifier.TapsToHopos) > 0))
            {
                var naturalState = prev != null
                    && curr->Value.GetNumActiveLanes() == 1
                    && !Contains((DualTime*) &curr->Value, (DualTime*) &prev->Value, curr->Value.NUMLANES, allowHopoAfterChord)
                    && curr->Key.Ticks <= prev->Key.Ticks + hopoThreshold
                    ? GuitarState.Strum
                    : GuitarState.Hopo;

                return (state != GuitarState.Forced) == (naturalState == GuitarState.Strum)
                    ? GuitarState.Strum
                    : GuitarState.Hopo;
            }
            return state;
        }

        private static unsafe bool Contains(DualTime* currLanes, DualTime* prevLanes, int numlanes, bool allowHopoAfterChord)
        {
            bool isNotTopLane = false;
            for (int i = numlanes - 1; i >= 0; --i)
            {
                if (prevLanes[i].IsActive())
                {
                    if (currLanes[i].IsActive())
                    {
                        // If the data came from a .chart file and if the note that follows a chord
                        // is the top most lane of said chord, we must interpret the note as a hopo.
                        //
                        // Why? Beats me, but it is what it is.
                        return !allowHopoAfterChord || isNotTopLane;
                    }
                    isNotTopLane = true;
                }
            }
            return false;
        }
    }
}

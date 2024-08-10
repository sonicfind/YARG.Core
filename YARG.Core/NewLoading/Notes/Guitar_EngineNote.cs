using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using YARG.Core.Containers;
using YARG.Core.Extensions;
using YARG.Core.Game;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public struct Guitar_EngineNote
    {
        public readonly DualTime StartPosition;
        public readonly GuitarState State;
        private readonly SubNote[] _notes;

        private Guitar_EngineNote(in DualTime start, GuitarState state, ReadOnlySpan<SubNote> notes)
        {
            StartPosition = start;
            State = state;
            _notes = new SubNote[notes.Length];
            for (int i = 0; i < notes.Length; i++)
            {
                _notes[i] = notes[i];
            }
        }

        public static long HopoThreshold = 192;
        public static unsafe List<Guitar_EngineNote> ConvertNotes<TNote>(YARGNativeSortedList<DualTime, TNote> notes, Modifier modifiers, int rngSeed)
            where TNote : unmanaged, IGuitarNote
        {
            Debug.Assert(notes.Count > 0, "This function should only be used when notes are present");
            var curr = notes.Data;
            var end = curr + notes.Count;
            int numLanes = curr[0].Value.NUMLANES;

            var conversion = new List<Guitar_EngineNote>(notes.Count);
            
            YARGKeyValuePair<DualTime, TNote>* prev = null;
            var buffer = stackalloc SubNote[numLanes];
            while (curr < end)
            {
                var noteCount = LoadSubNotes(curr, buffer);
                if (noteCount > 0)
                {
                    var state = ParseGuitarState(curr, prev, curr->Value.State, modifiers);
                    var engineNote = new Guitar_EngineNote(curr->Key, state, new ReadOnlySpan<SubNote>(buffer, noteCount));
                    conversion.Add(engineNote);
                }
                prev = curr;
                ++curr;
            }
            return conversion;
        }

        private static unsafe int LoadSubNotes<TNote>(YARGKeyValuePair<DualTime, TNote>* currNote, SubNote* buffer)
            where TNote : unmanaged, IGuitarNote
        {
            var frets = (DualTime*)&currNote->Value;
            int subIndex = 0;
            for (int i = 0; i < currNote->Value.NUMLANES; ++i)
            {
                if (frets[i].IsActive())
                {
                    buffer[subIndex++] = new SubNote(i, frets[i] + currNote->Key);
                }
            }
            return subIndex;
        }

        private static unsafe GuitarState ParseGuitarState<TGuitar>(YARGKeyValuePair<DualTime, TGuitar>* curr,
                                                                   YARGKeyValuePair<DualTime, TGuitar>* prev,
                                                                   GuitarState state, Modifier modifiers)
            where TGuitar : unmanaged, IGuitarNote
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
                    && !Contains((DualTime*)&curr->Value, (DualTime*) &prev->Value, curr->Value.NUMLANES)
                    && curr->Key.Ticks <= prev->Key.Ticks + HopoThreshold
                    ? GuitarState.Strum
                    : GuitarState.Hopo;

                return (state != GuitarState.Forced) == (naturalState == GuitarState.Strum)
                    ? GuitarState.Strum
                    : GuitarState.Hopo;
            }
            return state;
        }

        private static unsafe bool Contains(DualTime* currLanes, DualTime* prevLanes, int numLanes)
        {
            for (int i = 0; i < numLanes; ++i)
            {
                if (currLanes[i].IsActive())
                {
                    return prevLanes[i].IsActive();
                }
            }
            throw new Exception("Unreachable");
        }
    }
}

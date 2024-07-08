using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Extensions;
using YARG.Core.Game;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public class Guitar_EngineNote
    {
        public readonly DualTime StartPosition;
        private readonly GuitarState _state;
        private readonly SubNote[] _notes;
        private List<DualTime>? _overdrivePoints;

        private Guitar_EngineNote(in DualTime start, GuitarState state, SubNote[] notes)
        {
            StartPosition = start;
            _state = state;
            _notes = notes;
        }

        public static long HopoThreshold = 192;
        public static List<Guitar_EngineNote> ConvertNotes<TFretConfig>(YARGNativeSortedList<DualTime, GuitarNote2<TFretConfig>> notes, in Modifier modifiers, int rngSeed)
            where TFretConfig : unmanaged, IFretConfig
        {
            var conversion = new List<Guitar_EngineNote>(notes.Count);
            var rng = new Random(rngSeed);
            unsafe
            {
                YARGKeyValuePair<DualTime, GuitarNote2<TFretConfig>>* prev = null;
                var curr = notes.Data;
                var end = notes.End;
                while (curr < end)
                {
                    var engineNote = Create(in *curr, prev, modifiers, rng);
                    conversion.Add(engineNote);
                    prev = curr;
                    ++curr;
                }
            }
            return conversion;
        }

        private static unsafe Guitar_EngineNote Create<TFretConfig>(in YARGKeyValuePair<DualTime, GuitarNote2<TFretConfig>> currNote,
                                                                   YARGKeyValuePair<DualTime, GuitarNote2<TFretConfig>>* prevNote,
                                                                   Modifier modifiers,
                                                                   Random rng)
            where TFretConfig : unmanaged, IFretConfig
        {
            var state = currNote.Value.State;
            if ((modifiers & Modifier.AllStrums) > 0)
            {
                state = GuitarState.Strum;
            }
            else if ((modifiers & Modifier.AllTaps) > 0)
            {
                state = GuitarState.Tap;
            }
            else if ((modifiers & Modifier.AllHopos) > 0)
            {
                state = GuitarState.Hopo;
            }
            else if (state == GuitarState.Hopo)
            {
                if ((modifiers & Modifier.HoposToTaps) > 0)
                {
                    state = GuitarState.Tap;
                }
            }
            else if (state != GuitarState.Strum && (state != GuitarState.Tap || (modifiers & Modifier.TapsToHopos) > 0))
            {
                var naturalState = GetNaturalState(in currNote, in prevNote);
                if ((state != GuitarState.Forced) == (naturalState == GuitarState.Strum))
                {
                    state = GuitarState.Strum;
                }
                else
                {
                    state = GuitarState.Hopo;
                }
            }

            int numActive = currNote.Value.GetNumActiveLanes();
            var subNotes = new SubNote[numActive];

            int index = 0;
            if (currNote.Value.Frets[0].IsActive())
            {
                subNotes[0] = new SubNote(0, currNote.Value.Frets[0] + currNote.Key);
                ++index;
            }

            if (index < numActive)
            {
                int[]? shuffling = null;
                if ((modifiers & Modifier.NoteShuffle) > 0)
                {
                    var buf = new int[currNote.Value.Frets.NumColors];
                    for (int i = 0; i < currNote.Value.Frets.NumColors; ++i)
                    {
                        buf[i] = i + 1;
                    }
                    buf.Shuffle(rng);
                    shuffling = buf[..(numActive - index)];
                }

                for (int i = 1; i < currNote.Value.Frets.NumColors + 1; ++i)
                {
                    if (currNote.Value.Frets[i].IsActive())
                    {
                        int subNote = shuffling != null ? shuffling[index - 1] : i;
                        subNotes[index++] = new SubNote(subNote, currNote.Value.Frets[i] + currNote.Key);
                    }
                }
            }
            return new Guitar_EngineNote(currNote.Key, state, subNotes);
        }

        private static unsafe GuitarState GetNaturalState<TFretConfig>(in YARGKeyValuePair<DualTime, GuitarNote2<TFretConfig>> currNote,
                                                                       in YARGKeyValuePair<DualTime, GuitarNote2<TFretConfig>>* prevNote)
            where TFretConfig : unmanaged, IFretConfig
        {
            if (prevNote == null)
            {
                return GuitarState.Strum;
            }

            if (currNote.Value.GetNumActiveLanes() > 1)
            {
                return GuitarState.Strum;
            }

            if (Contains(in currNote.Value, in prevNote->Value))
            {
                return GuitarState.Strum;
            }

            if (prevNote->Key.Ticks + HopoThreshold < currNote.Key.Ticks)
            {
                return GuitarState.Strum;
            }
            return GuitarState.Hopo;
        }

        private static bool Contains<TFretConfig>(in GuitarNote2<TFretConfig> currNote, in GuitarNote2<TFretConfig> prevNote)
            where TFretConfig : unmanaged, IFretConfig
        {
            for (int i = 0; i < currNote.Frets.NumColors + 1; ++i)
            {
                if (currNote.Frets[i].IsActive())
                {
                    return prevNote.Frets[i].IsActive();
                }
            }
            throw new Exception("Unreachable");
        }
    }
}

using System;
using System.Runtime.CompilerServices;

namespace YARG.Core.Parsing.Guitar
{
    public enum GuitarState
    {
        NATURAL,
        FORCED_LEGACY,
        HOPO,
        STRUM,
        TAP
    }

    public struct GuitarNote<TConfig> : INote
        where TConfig : unmanaged, IFretConfig
    {
        public static readonly int NUMLANES;

        static GuitarNote()
        {
            unsafe
            {
                NUMLANES = sizeof(TConfig) / sizeof(TruncatableSustain);
            }
        }

        private TConfig frets;
        public GuitarState State;

        private ref TruncatableSustain GetSustain(int lane)
        {
            if (lane < 0 || NUMLANES <= lane)
                throw new IndexOutOfRangeException();

            unsafe
            {
                fixed (TConfig* ptr = &frets)
                    return ref ((TruncatableSustain*) ptr)[lane];
            }
        }

        public DualTime this[int lane]
        {
            get => GetSustain(lane);
            set
            {
                if (lane < 0 || NUMLANES <= lane)
                    throw new IndexOutOfRangeException();

                unsafe
                {
                    fixed (TConfig* ptr = &frets)
                    {
                        var lanes = (TruncatableSustain*) ptr;
                        lanes[lane] = new TruncatableSustain(value);
                        if (lane == 0)
                        {
                            for (int i = 1; i < NUMLANES; ++i)
                                lanes[lane].Disable();
                        }
                        else
                            lanes[0].Disable();
                    }
                }
            }
        }

        public void Disable(int lane)
        {
            GetSustain(lane).Disable();
        }

        public int GetNumActiveNotes()
        {
            int numActive = 0;
            unsafe
            {
                fixed (TConfig* ptr = &frets)
                {
                    var lanes = (TruncatableSustain*) ptr;
                    for (int i = 0; i < NUMLANES; ++i)
                    {
                        bool active = lanes[i].IsActive();
                        numActive += Unsafe.As<bool, byte>(ref active);
                    }
                }
            }
            return numActive;
        }

        public DualTime GetLongestSustain()
        {
            unsafe
            {
                fixed (TConfig* ptr = &frets)
                {
                    var lanes = (TruncatableSustain*) ptr;
                    var sustain = lanes[0];
                    for (int i = 1; i < NUMLANES; ++i)
                    {
                        var dur = lanes[i];
                        if (dur > sustain)
                            sustain = dur;
                    }
                    return sustain;
                }
            }
        }
    }
}

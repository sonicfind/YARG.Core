using System;
using System.Runtime.CompilerServices;

namespace YARG.Core.Chart.Guitar
{
    public enum ForceStatus
    {
        NATURAL,
        FORCED_LEGACY,
        HOPO,
        STRUM
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
        public ForceStatus Forcing;
        public bool IsTap;
        public void ToggleTap() { IsTap = !IsTap; }

        private unsafe TruncatableSustain* FretPtr
        {
            get
            {
                fixed (TConfig* lanes = &frets)
                    return (TruncatableSustain*)lanes;
            }
        }

        private ref TruncatableSustain GetSustain(int lane)
        {
            if (lane < 0 || NUMLANES <= lane)
                throw new IndexOutOfRangeException();

            unsafe
            {
                return ref FretPtr[lane];
            }
        }

        public long this[int lane]
        {
            get => GetSustain(lane);
            set
            {
                if (lane < 0 || NUMLANES <= lane)
                    throw new IndexOutOfRangeException();

                unsafe
                {
                    var lanes = FretPtr;
                    lanes[lane] = value;
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

        public void Disable(int lane)
        {
            GetSustain(lane).Disable();
        }

        public int GetNumActiveNotes()
        {
            int numActive = 0;
            unsafe
            {
                var lanes = FretPtr;
                for (int i = 0; i < NUMLANES; ++i)
                {
                    bool active = lanes[i].IsActive();
                    numActive += Unsafe.As<bool, byte>(ref active);
                }
            }
            return numActive;
        }

        public long GetLongestSustain()
        {
            unsafe
            {
                var lanes = FretPtr;
                long sustain = lanes[0];
                for (int i = 1; i < NUMLANES; ++i)
                {
                    long dur = lanes[i].Duration;
                    if (dur > sustain)
                        sustain = dur;
                }
                return sustain;
            }
            
        }
    }
}

using System;
using System.Runtime.CompilerServices;

namespace YARG.Core.Parsing.Keys
{
    public struct KeyNote : INote
    {
        public TruncatableSustain Green;
        public TruncatableSustain Red;
        public TruncatableSustain Yellow;
        public TruncatableSustain Blue;
        public TruncatableSustain Orange;

        public ref TruncatableSustain this[int lane]
        {
            get
            {
                unsafe
                {
                    if (0 <= lane &lane < 5)
                    {
                        fixed (TruncatableSustain* lanes = &Green)
                            return ref lanes[lane];
                    }
                    throw new IndexOutOfRangeException();
                }
            }
        }

        public int GetNumActiveNotes()
        {
            bool active = Green.IsActive();
            int numActive = Unsafe.As<bool, byte>(ref active);

            active = Red.IsActive();
            numActive += Unsafe.As<bool, byte>(ref active);

            active = Yellow.IsActive();
            numActive += Unsafe.As<bool, byte>(ref active);

            active = Blue.IsActive();
            numActive += Unsafe.As<bool, byte>(ref active);

            active = Orange.IsActive();
            return numActive + Unsafe.As<bool, byte>(ref active);
        }

        public long GetLongestSustain()
        {
            unsafe
            {
                fixed (TruncatableSustain* lanes = &Green)
                {
                    long sustain = lanes[0];
                    for (int i = 1; i < 5; ++i)
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
}

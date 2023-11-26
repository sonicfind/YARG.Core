using System;
using System.Runtime.CompilerServices;

namespace YARG.Core.Parsing.Keys
{
    public struct KeyNote : INote, IDotChartLoadable
    {
        public DualTime Green;
        public DualTime Red;
        public DualTime Yellow;
        public DualTime Blue;
        public DualTime Orange;

        public ref DualTime this[int lane]
        {
            get
            {
                unsafe
                {
                    if (0 <= lane &lane < 5)
                    {
                        fixed (DualTime* lanes = &Green)
                            return ref lanes[lane];
                    }
                    throw new IndexOutOfRangeException();
                }
            }
        }

        public bool SetFromDotChart(int lane, in DualTime length)
        {
            if (lane >= 5)
                return false;

            unsafe
            {
                fixed (DualTime* lanes = &Green)
                    lanes[lane] = DualTime.Truncate(length);
            }
            return true;
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

        public DualTime GetLongestSustain()
        {
            unsafe
            {
                fixed (DualTime* lanes = &Green)
                {
                    var sustain = lanes[0];
                    for (int i = 1; i < 5; ++i)
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

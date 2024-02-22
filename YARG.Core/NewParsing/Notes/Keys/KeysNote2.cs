using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace YARG.Core.NewParsing
{
    public struct KeysNote2 : IInstrumentNote
    {
        public const int NUM_COLORS = 5;

        public DualTime Green;
        public DualTime Red;
        public DualTime Yellow;
        public DualTime Blue;
        public DualTime Orange;

        public DualTime this[int lane]
        {
            get
            {
                if (lane < 0 || NUM_COLORS <= lane)
                {
                    throw new ArgumentOutOfRangeException(nameof(lane));
                }

                unsafe
                {
                    fixed (void* ptr = &Green)
                    {
                        return ((DualTime*) ptr)[lane];
                    }
                }
            }

            set
            {
                if (lane < 0 || NUM_COLORS <= lane)
                {
                    throw new ArgumentOutOfRangeException(nameof(lane));
                }

                unsafe
                {
                    fixed (void* ptr = &Green)
                    {
                        ((DualTime*) ptr)[lane] = value;
                    }
                }
            }
        }

        public int GetNumActiveLanes()
        {
            unsafe
            {
                fixed (void* ptr = &Green)
                {
                    var lanes = (DualTime*) ptr;
                    int numActive = 0;
                    for (int i = 0; i < NUM_COLORS; ++i)
                    {
                        bool active = lanes[i].IsActive();
                        numActive += Unsafe.As<bool, byte>(ref active);
                    }
                    return numActive;
                }
            }
        }

        public DualTime GetLongestSustain()
        {
            unsafe
            {
                fixed (void* ptr = &Green)
                {
                    var lanes = (DualTime*) ptr;
                    var sustain = lanes[0];
                    for (int i = 0; i < NUM_COLORS; ++i)
                    {
                        if (lanes[i] > sustain)
                        {
                            sustain = lanes[i];
                        }
                    }
                    return sustain;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace YARG.Core.NewParsing
{
    public enum GuitarState
    {
        Natural,
        Forced,
        Hopo,
        Strum,
        Tap
    }

    public struct GuitarNote2<TFrets> : IInstrumentNote
        where TFrets : unmanaged, IFretConfig
    {
        public TFrets Frets;
        public GuitarState State;

        public DualTime this[int lane]
        {
            get
            {
                if (lane < 0 || Frets.NumColors < lane)
                {
                    throw new ArgumentOutOfRangeException(nameof(lane));
                }

                unsafe
                {
                    fixed (void* ptr = &Frets)
                    {
                        return ((DualTime*) ptr)[lane];
                    }
                }
            }

            set
            {
                if (lane < 0 || Frets.NumColors < lane)
                {
                    throw new ArgumentOutOfRangeException(nameof(lane));
                }

                unsafe
                {
                    fixed (void* ptr = &Frets)
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
                fixed (void* ptr = &Frets)
                {
                    var lanes = (DualTime*) ptr;
                    int numActive = 0;
                    for (int i = 0; i <= Frets.NumColors; ++i)
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
                fixed (void* ptr = &Frets)
                {
                    var lanes = (DualTime*) ptr;
                    var sustain = lanes[0];
                    for (int i = 0; i <= Frets.NumColors; ++i)
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

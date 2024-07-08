using System;
using System.Runtime.CompilerServices;

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

        public readonly override string ToString()
        {
            return $"{State} | {Frets}";
        }
    }
}

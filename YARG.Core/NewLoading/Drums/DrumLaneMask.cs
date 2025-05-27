using System;

namespace YARG.Core.NewLoading
{
    [Flags]
    public enum DrumLaneMask : byte
    {
        None     = 0,
        Kick     = 1 << 0,
        Snare    = 1 << 1,
        Tom_1    = 1 << 2,
        Tom_2    = 1 << 3,
        Tom_3    = 1 << 4,
        Cymbal_1 = 1 << 5,
        Cymbal_2 = 1 << 6,
        Cymbal_3 = 1 << 7,
    }
}

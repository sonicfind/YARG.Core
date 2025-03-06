using System;

namespace YARG.Core.NewLoading
{
    [Flags]
    public enum DrumDynamicsMask
    {
        None            = 0,
        Accent_Kick     = 1 <<  0,
        Accent_Snare    = 1 <<  1,
        Accent_Tom_1    = 1 <<  2,
        Accent_Tom_2    = 1 <<  3,
        Accent_Tom_3    = 1 <<  4,
        Accent_Cymbal_1 = 1 <<  5,
        Accent_Cymbal_2 = 1 <<  6,
        Accent_Cymbal_3 = 1 <<  7,
        Ghost_Kick      = 1 <<  8,
        Ghost_Snare     = 1 <<  9,
        Ghost_Tom_1     = 1 << 10,
        Ghost_Tom_2     = 1 << 11,
        Ghost_Tom_3     = 1 << 12,
        Ghost_Cymbal_1  = 1 << 13,
        Ghost_Cymbal_2  = 1 << 14,
        Ghost_Cymbal_3  = 1 << 15,
    }
}
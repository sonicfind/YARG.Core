using System;

namespace YARG.Core.NewLoading.Guitar
{
    [Flags]
    public enum GuitarButtonMask
    {
        None      = 0,
        Fret1     = 1 << 0,
        Fret2     = 1 << 1,
        Fret3     = 1 << 2,
        Fret4     = 1 << 3,
        Fret5     = 1 << 4,
        Fret6     = 1 << 5,
        StrumUp   = 1 << 6,
        StrumDown = 1 << 7,
        Overdrive = 1 << 8,
        Whammy    = 1 << 9,
        FretMask  = Fret1 | Fret2 | Fret3 | Fret4 | Fret5 | Fret6,
        StrumMask = StrumUp | StrumDown,
    }

    public static class GuitarButtonExtensions
    {
        public static bool Has(this GuitarButtonMask mask, GuitarButtonMask flag)
        {
            return (mask & flag) == flag;
        }
    }
}

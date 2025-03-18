using System;
using System.Runtime.CompilerServices;

namespace YARG.Core.NewLoading
{
    [Flags]
    public enum GuitarLaneMask
    {
        None                  = 0,
        Open_DisableAnchoring = 1 << 0,
        Green                 = 1 << 1,
        Red                   = 1 << 2,
        Yellow                = 1 << 3,
        Blue                  = 1 << 4,
        Orange                = 1 << 5,
    }

    public static class GuitarLaneMaskExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Has(this GuitarLaneMask mask, GuitarLaneMask flag)
        {
            return (mask & flag) == flag;
        }
    }
}
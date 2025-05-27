using System;
using System.Runtime.CompilerServices;

namespace YARG.Core.NewLoading.Guitar
{
    public enum GuitarLane : byte
    {
        Open_DisableAnchoring = 0,

        Green                 = 1,
        Red                   = 2,
        Yellow                = 3,
        Blue                  = 4,
        Orange                = 5,

        Black1                = 1,
        Black2                = 2,
        Black3                = 3,
        White1                = 4,
        White2                = 5,
        White3                = 6,
    }

    [Flags]
    public enum GuitarLaneMask : byte
    {
        None                  = 0,

        Open_DisableAnchoring = 1 << GuitarLane.Open_DisableAnchoring,

        Green                 = 1 << GuitarLane.Green ,
        Red                   = 1 << GuitarLane.Red   ,
        Yellow                = 1 << GuitarLane.Yellow,
        Blue                  = 1 << GuitarLane.Blue  ,
        Orange                = 1 << GuitarLane.Orange,

        Black1                = 1 << GuitarLane.Black1,
        Black2                = 1 << GuitarLane.Black2,
        Black3                = 1 << GuitarLane.Black3,
        White1                = 1 << GuitarLane.White1,
        White2                = 1 << GuitarLane.White2,
        White3                = 1 << GuitarLane.White3,
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

using System;
using System.Runtime.CompilerServices;

namespace YARG.Core.NewLoading
{
    public enum GuitarLane
    {
        Open_DisableAnchoring = 0,
        Green                 = 1,
        Black1                = 1,
        Red                   = 2,
        Black2                = 2,
        Yellow                = 3,
        Black3                = 3,
        Blue                  = 4,
        White1                = 4,
        Orange                = 5,
        White2                = 5,
        White3                = 6,
    }

    [Flags]
    public enum GuitarLaneMask
    {
        None                  = 0,
        Open_DisableAnchoring = 1 << GuitarLane.Open_DisableAnchoring,
        Green                 = 1 << GuitarLane.Green ,
        Black1                = 1 << GuitarLane.Black1,
        Red                   = 1 << GuitarLane.Red   ,
        Black2                = 1 << GuitarLane.Black2,
        Yellow                = 1 << GuitarLane.Yellow,
        Black3                = 1 << GuitarLane.Black3,
        Blue                  = 1 << GuitarLane.Blue  ,
        White1                = 1 << GuitarLane.White1,
        Orange                = 1 << GuitarLane.Orange,
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
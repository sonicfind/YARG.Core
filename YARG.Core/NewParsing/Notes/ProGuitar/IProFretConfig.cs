using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public interface IProFretConfig<TConfig>
        where TConfig : unmanaged, IProFretConfig<TConfig>
    {
        private static readonly TConfig CONFIG = default;
        public static int ValidateFret(int fret)
        {
            if (fret < 0 || CONFIG.MAX_FRET < fret)
            {
                throw new ArgumentOutOfRangeException($"Fret value must lie in the range of [0, {CONFIG.MAX_FRET}]");
            }
            return fret;
        }

        public int MAX_FRET { get; }
    }

    public struct ProFret_17 : IProFretConfig<ProFret_17>
    {
        public readonly int MAX_FRET => 17;
    }

    public struct ProFret_22 : IProFretConfig<ProFret_22>
    {
        public readonly int MAX_FRET => 22;
    }
}

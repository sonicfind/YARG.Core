using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public interface IProFretConfig
    {
        public int ValidateFret(int fret);
    }

    public struct ProFret_17 : IProFretConfig
    {
        private const int MAX_FRET = 17;
        public readonly int ValidateFret(int fret)
        {
            if (fret < 0 || MAX_FRET < fret)
            {
                throw new ArgumentOutOfRangeException($"Fret value must lie in the range of [0, {MAX_FRET}]");
            }
            return fret;
        }
    }

    public struct ProFret_22 : IProFretConfig
    {
        private const int MAX_FRET = 22;
        public readonly int ValidateFret(int fret)
        {
            if (fret < 0 || MAX_FRET < fret)
            {
                throw new ArgumentOutOfRangeException($"Fret value must lie in the range of [0, {MAX_FRET}]");
            }
            return fret;
        }
    }
}

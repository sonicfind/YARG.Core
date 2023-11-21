using System;

namespace YARG.Core.Chart.ProGuitar
{
    public interface IProFretConfig
    {
        public void ThrowIfOutOfRange(int fret);
    }

    public struct ProFret_17 : IProFretConfig
    {
        private const int MAX_FRET = 17;
        public void ThrowIfOutOfRange(int fret)
        {
            if (fret < 0 || MAX_FRET < fret)
                throw new ArgumentOutOfRangeException($"Fret value must lie in the range of 0 - {MAX_FRET} (inclusive)");
        }
    }

    public struct ProFret_22 : IProFretConfig
    {
        private const int MAX_FRET = 22;
        public void ThrowIfOutOfRange(int fret)
        {
            if (fret < 0 || MAX_FRET < fret)
                throw new ArgumentOutOfRangeException($"Fret value must lie in the range of 0 - {MAX_FRET} (inclusive)");
        }
    }
}

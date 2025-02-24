using System;
using YARG.Core.Game;

namespace YARG.Core.NewLoading
{
    public struct InstrumentSelection : IEquatable<InstrumentSelection>
    {
        public Instrument Instrument;
        public Difficulty Difficulty;
        public Modifier   Modifiers;

        public readonly override int GetHashCode()
        {
            return Instrument.GetHashCode() ^ Difficulty.GetHashCode() ^ Modifiers.GetHashCode();
        }

        public readonly bool Equals(InstrumentSelection other)
        {
            return Instrument == other.Instrument &&
                Difficulty == other.Difficulty &&
                Modifiers  == other.Modifiers;
        }
    }
}
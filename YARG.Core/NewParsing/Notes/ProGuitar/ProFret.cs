using System;

namespace YARG.Core.NewParsing
{
    public interface IProFret
    {
        int MAX_FRET { get; }
        int Value { get; set; }
        bool Validate(int fret);
    }

    public struct ProFret_17 : IProFret
    {
        public readonly int MAX_FRET => 17;

        private int _value;
        public int Value
        {
            readonly get => _value;
            set
            {
                if (!Validate(value))
                {
                    throw new ArgumentOutOfRangeException($"Fret value must lie in the range of [0, 17]");
                }
                _value = value;
            }
        }

        public readonly bool Validate(int fret)
        {
            return 0 <= fret && fret <= MAX_FRET;
        }
    }

    public struct ProFret_22 : IProFret
    {
        public readonly int MAX_FRET => 22;

        private int _value;
        public int Value
        {
            readonly get => _value;
            set
            {
                if (!Validate(value))
                {
                    throw new ArgumentOutOfRangeException($"Fret value must lie in the range of [0, 22]");
                }
                _value = value;
            }
        }

        public readonly bool Validate(int fret)
        {
            return 0 <= fret && fret <= MAX_FRET;
        }
    }
}

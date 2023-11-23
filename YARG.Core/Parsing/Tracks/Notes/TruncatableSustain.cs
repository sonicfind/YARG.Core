using System;

namespace YARG.Core.Parsing
{
    public struct TruncatableSustain : IEnableable, IEquatable<TruncatableSustain>, IComparable<TruncatableSustain>
    {
        public static long MinDuration = 180;
        // Default init to 0 is desired
        private DualTime _value;

        public TruncatableSustain(DualTime time)
        {
            if (time.ticks < MinDuration)
            {
                time.seconds /= time.ticks;
                time.ticks = 1;
            }

            _value = time;
        }

        public static implicit operator DualTime(in TruncatableSustain sustain) => sustain._value;

        public bool IsActive() { return _value.ticks > 0; }
        public void Disable() { _value = DualTime.Zero; }

        public override string ToString()
        {
            return _value.ToString();
        }

        public int CompareTo(TruncatableSustain other)
        {
            return _value.CompareTo(other._value);
        }

        public bool Equals(TruncatableSustain other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is TruncatableSustain sustain && Equals(sustain);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public static bool operator <(in TruncatableSustain lhs, in TruncatableSustain rhs)
        {
            return lhs._value < rhs._value;
        }

        public static bool operator >(in TruncatableSustain lhs, in TruncatableSustain rhs)
        {
            return lhs._value > rhs._value;
        }

        public static bool operator <=(in TruncatableSustain lhs, in TruncatableSustain rhs)
        {
            return lhs._value <= rhs._value;
        }

        public static bool operator >=(in TruncatableSustain lhs, in TruncatableSustain rhs)
        {
            return lhs._value >= rhs._value;
        }

        public static bool operator ==(in TruncatableSustain lhs, in TruncatableSustain rhs)
        {
            return lhs._value == rhs._value;
        }

        public static bool operator !=(in TruncatableSustain lhs, in TruncatableSustain rhs)
        {
            return lhs._value != rhs._value;
        }
    }
}

using System;

namespace YARG.Core.Parsing
{
    public struct NormalizedDuration : IEquatable<NormalizedDuration>, IComparable<NormalizedDuration>
    {
        private static DualTime BASEDuration = new(1, 0);
        private DualTime _value;

        public NormalizedDuration(in DualTime time)
        {
            _value = time.ticks > 0 ? BASEDuration : time;
        }

        public static implicit operator DualTime(in NormalizedDuration dur) => dur._value;

        public override string ToString()
        {
            return _value.ToString();
        }

        public int CompareTo(NormalizedDuration other)
        {
            return _value.CompareTo(other._value);
        }

        public bool Equals(NormalizedDuration other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is NormalizedDuration sustain && Equals(sustain);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public static bool operator <(in NormalizedDuration lhs, in NormalizedDuration rhs)
        {
            return lhs._value < rhs._value;
        }

        public static bool operator >(in NormalizedDuration lhs, in NormalizedDuration rhs)
        {
            return lhs._value > rhs._value;
        }

        public static bool operator <=(in NormalizedDuration lhs, in NormalizedDuration rhs)
        {
            return lhs._value <= rhs._value;
        }

        public static bool operator >=(in NormalizedDuration lhs, in NormalizedDuration rhs)
        {
            return lhs._value >= rhs._value;
        }

        public static bool operator ==(in NormalizedDuration lhs, in NormalizedDuration rhs)
        {
            return lhs._value == rhs._value;
        }

        public static bool operator !=(in NormalizedDuration lhs, in NormalizedDuration rhs)
        {
            return lhs._value != rhs._value;
        }
    }
}

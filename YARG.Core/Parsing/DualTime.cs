using System;

namespace YARG.Core.Parsing
{
    public struct DualTime : IEquatable<DualTime>, IComparable<DualTime>
    {
        public static readonly DualTime Inactive = new(-1, 0);
        public static readonly DualTime Zero = new(0, 0);

        public long ticks;
        public double seconds;

        public DualTime(long ticks, double seconds)
        {
            this.ticks = ticks;
            this.seconds = seconds;
        }

        public int CompareTo(DualTime other)
        {
            return ticks.CompareTo(other.ticks);
        }

        public bool Equals(DualTime other)
        {
            return ticks.Equals(other.ticks);
        }

        public override bool Equals(object obj)
        {
            return obj is DualTime dual && Equals(dual);
        }

        public override int GetHashCode()
        {
            return ticks.GetHashCode();
        }

        public static bool operator<(in DualTime lhs, in DualTime rhs)
        {
            return lhs.ticks < rhs.ticks;
        }

        public static bool operator>(in DualTime lhs, in DualTime rhs)
        {
            return lhs.ticks > rhs.ticks;
        }

        public static bool operator<=(in DualTime lhs, in DualTime rhs)
        {
            return lhs.ticks <= rhs.ticks;
        }

        public static bool operator>=(in DualTime lhs, in DualTime rhs)
        {
            return lhs.ticks >= rhs.ticks;
        }

        public static bool operator==(in DualTime lhs, in DualTime rhs)
        {
            return lhs.ticks == rhs.ticks;
        }

        public static bool operator!=(in DualTime lhs, in DualTime rhs)
        {
            return lhs.ticks != rhs.ticks;
        }

        public static DualTime operator-(in DualTime lhs, in DualTime rhs)
        {
            return new DualTime(lhs.ticks - rhs.ticks, lhs.seconds - rhs.seconds);
        }

        public static DualTime operator+(in DualTime lhs, in DualTime rhs)
        {
            return new DualTime(lhs.ticks - rhs.ticks, lhs.seconds - rhs.seconds);
        }
    }
}

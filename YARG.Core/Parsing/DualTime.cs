using System;

namespace YARG.Core.Parsing
{
    public struct DualTime : IEquatable<DualTime>, IComparable<DualTime>, IEnableable
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

        public bool IsActive()
        {
            return ticks > 0;
        }

        public void Disable()
        {
            ticks = 0;
            seconds = 0;
        }

        public static long TruncationLimit = 180;
        public static DualTime Truncate(DualTime time)
        {
            if (time.ticks < TruncationLimit)
            {
                time.seconds /= time.ticks;
                time.ticks = 1;
            }
            return time;
        }

        private static readonly DualTime NORMALIZED_TIME = new(1, 0);

        public static DualTime Normalize(DualTime time)
        {
            return time.ticks > 0 ? NORMALIZED_TIME : time;
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

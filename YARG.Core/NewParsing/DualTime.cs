using System;

namespace YARG.Core.NewParsing
{
    public struct DualTime : IEquatable<DualTime>, IComparable<DualTime>,
                             IEquatable<long>, IComparable<long>,
                             IEquatable<double>, IComparable<double>
    {
        public static readonly DualTime Inactive = new()
        {
            Ticks = -1,
            // Seconds value doesn't matter
            Seconds = 0,
        };

        public static readonly DualTime Zero = new()
        {
            Ticks = 0,
            Seconds = 0,
        };

        public long Ticks;
        public double Seconds;

        public readonly bool IsActive()
        {
            return Ticks > 0;
        }

        public readonly int CompareTo(DualTime other)
        {
            return Ticks.CompareTo(other.Ticks);
        }

        public readonly bool Equals(DualTime other)
        {
            return Ticks.Equals(other.Ticks);
        }

        public readonly int CompareTo(long ticks)
        {
            return Ticks.CompareTo(ticks);
        }

        public readonly bool Equals(long ticks)
        {
            return Ticks.Equals(ticks);
        }

        public readonly int CompareTo(double seconds)
        {
            return Seconds.CompareTo(seconds);
        }

        public readonly bool Equals(double seconds)
        {
            return Seconds.Equals(seconds);
        }

        public readonly override bool Equals(object obj)
        {
            return obj is DualTime dual && Equals(dual);
        }

        public readonly override int GetHashCode()
        {
            return Ticks.GetHashCode();
        }

        public readonly override string ToString()
        {
            return $"[{Ticks}, {Seconds}]";
        }

        public static bool operator <(in DualTime lhs, in DualTime rhs)
        {
            return lhs.Ticks < rhs.Ticks;
        }

        public static bool operator >(in DualTime lhs, in DualTime rhs)
        {
            return lhs.Ticks > rhs.Ticks;
        }

        public static bool operator <=(in DualTime lhs, in DualTime rhs)
        {
            return lhs.Ticks <= rhs.Ticks;
        }

        public static bool operator >=(in DualTime lhs, in DualTime rhs)
        {
            return lhs.Ticks >= rhs.Ticks;
        }

        public static bool operator ==(in DualTime lhs, in DualTime rhs)
        {
            return lhs.Ticks == rhs.Ticks;
        }

        public static bool operator !=(in DualTime lhs, in DualTime rhs)
        {
            return lhs.Ticks != rhs.Ticks;
        }

        public static DualTime operator -(in DualTime lhs, in DualTime rhs)
        {
            return new DualTime()
            {
                Ticks = lhs.Ticks - rhs.Ticks,
                Seconds = lhs.Seconds - rhs.Seconds,
            };
        }

        public static DualTime operator +(in DualTime lhs, in DualTime rhs)
        {
            return new DualTime()
            {
                Ticks = lhs.Ticks + rhs.Ticks,
                Seconds = lhs.Seconds + rhs.Seconds,
            };
        }
    }
}

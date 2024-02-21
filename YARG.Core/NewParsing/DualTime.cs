using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public struct DualTime : IEquatable<DualTime>, IComparable<DualTime>
    {
        public static readonly DualTime Inactive = new()
        {
            Ticks = -1,
        };

        public long Ticks;
        public double Seconds;

        public readonly int CompareTo(DualTime other)
        {
            return Ticks.CompareTo(other.Ticks);
        }

        public readonly bool Equals(DualTime other)
        {
            return Ticks.Equals(other.Ticks);
        }

        public readonly override bool Equals(object obj)
        {
            return obj is DualTime dual && Equals(dual);
        }

        public readonly override int GetHashCode()
        {
            return Ticks.GetHashCode();
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using YARG.Core.Chart;
using YARG.Core.Song;

namespace YARG.Core.NewParsing
{
    [DebuggerDisplay("[{Ticks}, {Seconds}]")]
    public struct DualTime : IEquatable<DualTime>, IComparable<DualTime>
    {
        public static readonly DualTime Inactive = new()
        {
            Ticks = -1,
        };

        public long Ticks;
        public double Seconds;

        public readonly bool IsActive()
        {
            return Ticks > 0;
        }

        /// <summary>
        /// Attempts to normalize the instance to 1 tick if the current tick value lies beneath the threshold
        /// </summary>
        /// <param name="time">Time instance to compare against</param>
        /// <param name="threshold">The tick value required to dodge truncation</param>
        public static DualTime Truncate(DualTime time, long threshold)
        {
            if (time.Ticks < threshold)
            {
                time.Seconds /= time.Ticks;
                time.Ticks = 1;
            }
            return time;
        }

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

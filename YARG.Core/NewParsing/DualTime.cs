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

        public readonly bool IsActive()
        {
            return Ticks > 0;
        }

        public static long TruncationLimit = 170;
        /// <summary>
        /// Returns a <see cref="DualTime"/> structure that abides by the currently specified <see cref="TruncationLimit"/>.
        /// <br></br>If the time exceeds the limit, it returns unchanged. Otherwise, it gets proportionally scaled down to a tick value of one.
        /// </summary>
        /// <remarks>If the ticks provided by <see langword="time"/> is zero, the resulting seconds will be <see cref="double.NaN"/>
        /// as there is no tickrate to base off</remarks>
        /// <param name="time">The time to evaluate</param>
        /// <returns>A possibly truncated time structure</returns>
        public static DualTime Truncate(DualTime time)
        {
            if (time.Ticks < TruncationLimit)
            {
                time.Seconds /= time.Ticks;
                time.Ticks = 1;
            }
            return time;
        }

        private static readonly DualTime NORMALIZED_TIME = new()
        {
            Ticks = 1,
            Seconds = double.NaN
        };

        /// <summary>
        /// Normalizes DualTime structures with ticks of zero to ticks of one.
        /// </summary>
        /// <remarks>If the ticks provided by <see langword="time"/> is zero, the resulting seconds will be <see cref="double.NaN"/>
        /// as there is no tickrate to base off</remarks>
        /// <param name="time">The time to evaluate</param>
        /// <returns>A time >= one tick</returns>
        public static DualTime Normalize(DualTime time)
        {
            return time.Ticks > 0 ? NORMALIZED_TIME : time;
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

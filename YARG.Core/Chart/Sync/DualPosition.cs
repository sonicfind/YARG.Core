using System;

namespace YARG.Core.Chart
{
    public readonly struct DualPosition : IEquatable<DualPosition>, IComparable<DualPosition>
    {
        public readonly long ticks;
        public readonly float seconds;

        public DualPosition(long ticks, float seconds)
        {
            this.ticks = ticks;
            this.seconds = seconds;
        }

        public int CompareTo(DualPosition other)
        {
            return ticks.CompareTo(other.ticks);
        }

        public bool Equals(DualPosition other)
        {
            return ticks.Equals(other.ticks);
        }
    }
}

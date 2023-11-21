namespace YARG.Core.Parsing
{
    public struct NormalizedDuration
    {
        private long _duration;
        public long Duration
        {
            get { return _duration; }
            set
            {
                if (value == 0)
                    value = 1;
                _duration = value;
            }
        }

        public NormalizedDuration(long duration)
        {
            _duration = 1;
            Duration = duration;
        }

        public static implicit operator long(NormalizedDuration dur) => dur._duration;
        public static implicit operator NormalizedDuration(long dur) => new(dur);
    }
}

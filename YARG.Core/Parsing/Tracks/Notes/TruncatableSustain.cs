namespace YARG.Core.Parsing
{
    public struct TruncatableSustain : IEnableable
    {
        public static long MinDuration = 180;
        // Default init to 0 is desired
        private long _duration;
        public long Duration
        {
            get { return _duration; }
            set
            {
                if (value < MinDuration)
                    value = 1;
                _duration = value;
            }
        }

        public TruncatableSustain(long duration)
        {
            _duration = 0;
            Duration = duration;
        }

        public static implicit operator long(TruncatableSustain dur) => dur._duration;
        public static implicit operator TruncatableSustain(long dur) => new(dur);

        public bool IsActive() { return _duration > 0; }
        public void Disable() { _duration = 0; }

        public override string ToString()
        {
            return _duration.ToString();
        }
    }
}

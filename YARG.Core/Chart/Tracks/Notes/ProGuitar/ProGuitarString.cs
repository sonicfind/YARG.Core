namespace YARG.Core.Chart.ProGuitar
{
    public enum StringMode
    {
        Normal,
        Bend,
        Muted,
        Tapped,
        Harmonics,
        Pinch_Harmonics
    };

    public struct ProGuitarString<TFretConfig> : IEnableable
        where TFretConfig : IProFretConfig, new()
    {
        public static readonly TFretConfig CONFIG = new();
        static ProGuitarString() { }

        private int _fret;
        public StringMode mode;
        public TruncatableSustain Duration;

        public int Fret
        {
            get => _fret;
            set
            {
                CONFIG.ThrowIfOutOfRange(value);
                _fret = value;
            }
        }

        public bool IsActive()
        {
            return Duration.IsActive();
        }

        public void Disable()
        {
            Duration.Disable();
            _fret = 0;
            mode = StringMode.Normal;
        }
    }
}

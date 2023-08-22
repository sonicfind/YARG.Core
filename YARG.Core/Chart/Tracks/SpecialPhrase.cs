namespace YARG.Core.Chart
{
    public enum SpecialPhraseType
    {
        FaceOff_Player1 = 0,
        FaceOff_Player2 = 1,
        StarPower = 2,
        Solo = 3,
        LyricLine = 4,
        RangeShift = 5,
        HarmonyLine = 6,
        StarPower_Diff = 8,
        BRE = 64,
        Tremolo = 65,
        Trill = 66,
        LyricShift = 67,
    }

    public struct SpecialPhrase_FW
    {
        private NormalizedDuration _duration;
        public long Duration
        {
            get => _duration;
            set => _duration = value;
        }

        public SpecialPhraseType Type { get; set; }
        public int Velocity { get; set; }

        public SpecialPhrase_FW(SpecialPhraseType type, long duration, int velocity = 100)
        {
            Type = type;
            Velocity = velocity;
            _duration = duration;
        }
    }
}

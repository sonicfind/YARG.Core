namespace YARG.Core.Parsing
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

    public struct SpecialPhraseInfo
    {
        private NormalizedDuration _duration;
        public long Duration
        {
            get => _duration;
            set => _duration = value;
        }

        public int Velocity { get; set; }

        public SpecialPhraseInfo(long duration, int velocity = 100)
        {
            Velocity = velocity;
            _duration = duration;
        }
    }
}

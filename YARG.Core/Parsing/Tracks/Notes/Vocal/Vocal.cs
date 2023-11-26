using YARG.Core.Parsing.Pitch;

namespace YARG.Core.Parsing.Vocal
{
    public struct VocalNote_FW
    {
        public struct VocalConfig : IPitchConfig
        {
            public int OCTAVE_MIN() => 2;
            public int OCTAVE_MAX() => 6;
        }

        public string Lyric;
        public Pitch<VocalConfig> Pitch;
        public DualTime Duration;

        public bool IsPlayable() { return Lyric.Length > 0 && (Pitch.Octave >= 2 || Lyric[0] == '#'); }

        public VocalNote_FW(string lyric)
        {
            Lyric = lyric;
            Duration = default;
            Pitch = default;
        }
    }
}

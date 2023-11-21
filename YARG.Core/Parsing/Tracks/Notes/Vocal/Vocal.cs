using YARG.Core.Chart.Pitch;

namespace YARG.Core.Chart.Vocal
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
        public NormalizedDuration Duration;

        public bool IsPlayable() { return Lyric.Length > 0 && (Pitch.Octave >= 2 || Lyric[0] == '#'); }

        public VocalNote_FW(string lyric)
        {
            Lyric = lyric;
            Duration = default;
            Pitch = default;
        }
    }
}

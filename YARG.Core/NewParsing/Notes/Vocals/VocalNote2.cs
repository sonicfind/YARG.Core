using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public struct VocalNote2
    {
        public struct VocalConfig : IPitchConfig
        {
            public readonly int OCTAVE_MIN => 2;
            public readonly int OCTAVE_MAX => 6;
        }

        public string Lyric;
        public Pitch<VocalConfig> Pitch;
        public DualTime Duration;

        public readonly bool IsPlayable() { return Lyric.Length > 0 && (Pitch.Octave >= 2 || Lyric[0] == '#'); }
    }
}

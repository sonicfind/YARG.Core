using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
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
        private DualTime _duration;

        public int Velocity;

        public DualTime Duration
        {
            readonly get => _duration;
            set => _duration = DualTime.Normalize(value);
        }

        public SpecialPhraseInfo(in DualTime duration, int velocity = 100)
        {
            Velocity = velocity;
            _duration = DualTime.Normalize(duration);
        }
    }
}

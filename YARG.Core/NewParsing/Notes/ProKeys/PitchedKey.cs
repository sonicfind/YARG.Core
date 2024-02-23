using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public struct PitchedKey
    {
        public struct ProKeyConfig : IPitchConfig
        {
            public readonly int OCTAVE_MIN => 3;
            public readonly int OCTAVE_MAX => 5;
        }

        public Pitch<ProKeyConfig> Pitch;
        public DualTime Duration;
    }
}

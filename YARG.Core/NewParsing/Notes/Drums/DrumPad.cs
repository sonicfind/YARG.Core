 using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public enum DrumDynamics
    {
        None,
        Accent,
        Ghost
    }

    public struct DrumPad
    {
        public DualTime Duration;
        public DrumDynamics Dynamics;

        public readonly override string ToString()
        {
            return Dynamics != DrumDynamics.None ? $"{Duration.Ticks} - {Dynamics}" : Duration.Ticks.ToString();
        }
    }

    public struct DrumPad_Pro
    {
        public DualTime Duration;
        public DrumDynamics Dynamics;
        public bool CymbalFlag;

        public readonly override string ToString()
        {
            string str = Dynamics != DrumDynamics.None ? $"{Duration.Ticks} - {Dynamics}" : Duration.Ticks.ToString();
            if (CymbalFlag)
            {
                str += " - Cymbal";
            }
            return str;
        }
    }
}

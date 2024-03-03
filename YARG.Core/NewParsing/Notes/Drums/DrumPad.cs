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

        public readonly bool IsActive()
        {
            return Duration.IsActive();
        }

        public override string ToString()
        {
            return Dynamics != DrumDynamics.None ? $"{Duration.Ticks} - {Dynamics}" : Duration.Ticks.ToString();
        }
    }
}

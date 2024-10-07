using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace YARG.Core.NewParsing
{
    public struct FiveLaneDrums : IInstrumentNote, IDotChartLoadable
    {
        public DualTime Kick;
        public DualTime Snare;
        public DualTime Yellow;
        public DualTime Blue;
        public DualTime Orange;
        public DualTime Green;
        public DrumDynamics Dynamics_Snare;
        public DrumDynamics Dynamics_Yellow;
        public DrumDynamics Dynamics_Blue;
        public DrumDynamics Dynamics_Orange;
        public DrumDynamics Dynamics_Green;
        public KickState KickState;
        public bool IsFlammed;

        public readonly int NUMLANES => 6;

        public bool SetFromDotChart(int lane, in DualTime length)
        {
            switch (lane)
            {
                case 0: Kick = length; break;
                case 1: Snare = length; break;
                case 2: Yellow = length; break;
                case 3: Blue = length; break;
                case 4: Orange = length; break;
                case 5: Green = length; break;

                case 32: KickState = KickState.PlusOnly; break;

                case 34: Dynamics_Snare = DrumDynamics.Accent; break;
                case 35: Dynamics_Yellow = DrumDynamics.Accent; break;
                case 36: Dynamics_Blue = DrumDynamics.Accent; break;
                case 37: Dynamics_Orange = DrumDynamics.Accent; break;
                case 38: Dynamics_Green = DrumDynamics.Accent; break;

                case 40: Dynamics_Snare = DrumDynamics.Ghost; break;
                case 41: Dynamics_Yellow = DrumDynamics.Ghost; break;
                case 42: Dynamics_Blue = DrumDynamics.Ghost; break;
                case 43: Dynamics_Orange = DrumDynamics.Ghost; break;
                case 44: Dynamics_Green = DrumDynamics.Ghost; break;
                default:
                    return false;
            }
            return true;
        }

        public readonly int GetNumActiveLanes()
        {
            int numActive = 0;
            bool state = Kick.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Snare.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Yellow.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Blue.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Orange.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Green.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            return numActive;
        }

        public readonly DualTime GetLongestSustain()
        {
            var sustain = Kick;
            if (Snare > sustain)
            {
                sustain = Snare;
            }
            if (Yellow > sustain)
            {
                sustain = Yellow;
            }
            if (Blue > sustain)
            {
                sustain = Blue;
            }
            if (Orange > sustain)
            {
                sustain = Orange;
            }
            if (Green > sustain)
            {
                sustain = Green;
            }
            return sustain;
        }

        public readonly override string ToString()
        {
            StringBuilder builder = new();
            if (Kick.IsActive())
            {
                if (KickState != KickState.PlusOnly)
                {
                    builder.Append($"Bass: {Kick.Ticks} | ");
                }
                else
                {
                    builder.Append($"DoubleBass: {Kick.Ticks} | ");
                }
            }
            if (Snare.IsActive())
            {
                builder.Append($"Snare: {Snare.Ticks}");
                if (Dynamics_Snare != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics_Snare})");
                }
                builder.Append(" | ");
            }
            if (Yellow.IsActive())
            {
                builder.Append($"Yellow: {Yellow.Ticks}");
                if (Dynamics_Yellow != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics_Yellow})");
                }
                builder.Append(" | ");
            }
            if (Blue.IsActive())
            {
                builder.Append($"Blue: {Blue.Ticks}");
                if (Dynamics_Blue != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics_Blue})");
                }
                builder.Append(" | ");
            }
            if (Orange.IsActive())
            {
                builder.Append($"Orange: {Orange.Ticks}");
                if (Dynamics_Orange != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics_Orange})");
                }
                builder.Append(" | ");
            }
            if (Green.IsActive())
            {
                builder.Append($"Green: {Green.Ticks}");
                if (Dynamics_Green != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics_Green})");
                }
                builder.Append(" | ");
            }
            return builder.ToString();
        }
    }
}

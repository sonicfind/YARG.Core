﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using YARG.Core.Chart;

namespace YARG.Core.NewParsing
{
    public struct UnknownLaneDrums : IInstrumentNote, IDotChartLoadable
    {
        public static DrumsType DrumType;

        public bool IsDoubleBass;
        public bool IsFlammed;
        public DualTime Bass;
        public DualTime Snare;
        public DualTime Yellow;
        public DualTime Blue;
        public DualTime Orange;
        public DrumDynamics Dynamics_Snare;
        public DrumDynamics Dynamics_Yellow;
        public DrumDynamics Dynamics_Blue;
        public DrumDynamics Dynamics_Orange;
        public bool Cymbal_Yellow;
        public bool Cymbal_Blue;
        public bool Cymbal_Orange;
        // Placed at the bottom to allowed best blittability with FourLane
        // as that will be the most common conversion
        public DualTime Green;
        public DrumDynamics Dynamics_Green;

        public readonly int NUMLANES => 6;

        public bool SetFromDotChart(int lane, in DualTime length)
        {
            switch (lane)
            {
                case 0: Bass = DualTime.Truncate(length); break;
                case 1: Snare = DualTime.Truncate(length); break;
                case 2: Yellow = DualTime.Truncate(length); break;
                case 3: Blue = DualTime.Truncate(length); break;
                case 4: Orange = DualTime.Truncate(length); break;
                case 5:
                    if ((DrumType & DrumsType.FiveLane) != DrumsType.FiveLane)
                    {
                        return false;
                    }
                    Green = DualTime.Truncate(length);
                    DrumType = DrumsType.FiveLane;
                    break;
                case 32: IsDoubleBass = true; break;

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

                case 66:
                    if ((DrumType & DrumsType.ProDrums) == DrumsType.ProDrums) unsafe
                    {
                        Cymbal_Yellow = true;
                        DrumType = DrumsType.ProDrums;
                    }
                    break;
                case 67:
                    if ((DrumType & DrumsType.ProDrums) == DrumsType.ProDrums) unsafe
                    {
                        Cymbal_Blue = true;
                        DrumType = DrumsType.ProDrums;
                    }
                    break;
                case 68:
                    if ((DrumType & DrumsType.ProDrums) == DrumsType.ProDrums) unsafe
                    {
                        Cymbal_Orange = true;
                        DrumType = DrumsType.ProDrums;
                    }
                    break;
                default:
                    return false;
            }
            return true;
        }

        public readonly int GetNumActiveLanes()
        {
            int numActive = 0;
            bool state = Bass.IsActive();
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
            var sustain = Bass;
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
            if (Bass.IsActive())
            {
                if (!IsDoubleBass)
                {
                    builder.Append($"Bass: {Bass.Ticks} | ");
                }
                else
                {
                    builder.Append($"DoubleBass: {Bass.Ticks} | ");
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
                if (Cymbal_Yellow)
                {
                    builder.Append("(Cymbal)");
                }
                if (Dynamics_Yellow != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics_Yellow})");
                }
                builder.Append(" | ");
            }
            if (Blue.IsActive())
            {
                builder.Append($"Blue: {Blue.Ticks}");
                if (Cymbal_Blue)
                {
                    builder.Append("(Cymbal)");
                }
                if (Dynamics_Blue != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics_Blue})");
                }
                builder.Append(" | ");
            }
            if (Orange.IsActive())
            {
                builder.Append($"Orange: {Orange.Ticks}");
                if (Cymbal_Orange)
                {
                    builder.Append("(Cymbal)");
                }
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

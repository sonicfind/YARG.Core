using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public interface IDrumPadConfig
    {
        public int NumPads { get; }
        public void SetDynamics(int index, DrumDynamics dynamics);
    }

    public struct FourLane : IDrumPadConfig
    {
        public DrumPad Snare;
        public DrumPad Yellow;
        public DrumPad Blue;
        public DrumPad Green;

        public readonly int NumPads => 4;

        public void SetDynamics(int index, DrumDynamics dynamics)
        {
            switch (index)
            {
                case 0: Snare.Dynamics  = dynamics; break;
                case 1: Yellow.Dynamics = dynamics; break;
                case 2: Blue.Dynamics   = dynamics; break;
                case 3: Green.Dynamics  = dynamics; break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new();
            if (Snare.IsActive())
            {
                builder.Append($"Snare: {Snare}|");
            }
            if (Yellow.IsActive())
            {
                builder.Append($"Yellow: {Yellow}|");
            }
            if (Blue.IsActive())
            {
                builder.Append($"Blue: {Blue}|");
            }
            if (Green.IsActive())
            {
                builder.Append($"Green: {Green}|");
            }
            return builder.ToString();
        }
    }

    public struct FiveLane : IDrumPadConfig
    {
        public DrumPad Snare;
        public DrumPad Yellow;
        public DrumPad Blue;
        public DrumPad Orange;
        public DrumPad Green;

        public readonly int NumPads => 5;

        public void SetDynamics(int index, DrumDynamics dynamics)
        {
            switch (index)
            {
                case 0: Snare.Dynamics  = dynamics; break;
                case 1: Yellow.Dynamics = dynamics; break;
                case 2: Blue.Dynamics   = dynamics; break;
                case 3: Orange.Dynamics = dynamics; break;
                case 4: Green.Dynamics  = dynamics; break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new();

            if (Snare.IsActive())
            {
                builder.Append($"Snare: {Snare}|");
            }
            if (Yellow.IsActive())
            {
                builder.Append($"Yellow: {Yellow}|");
            }
            if (Blue.IsActive())
            {
                builder.Append($"Blue: {Blue}|");
            }
            if (Orange.IsActive())
            {
                builder.Append($"Orange: {Orange}|");
            }
            if (Green.IsActive())
            {
                builder.Append($"Green: {Green}|");
            }
            return builder.ToString();
        }
    }
}

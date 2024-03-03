using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public interface IDrumPadConfig
    {
        public int NumPads { get; }
    }

    public struct FourLane : IDrumPadConfig
    {
        public DrumPad Snare;
        public DrumPad Yellow;
        public DrumPad Blue;
        public DrumPad Green;

        public readonly int NumPads => 4;

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

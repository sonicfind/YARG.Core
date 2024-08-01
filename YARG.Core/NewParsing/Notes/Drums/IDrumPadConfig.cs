using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public interface IDrumPadConfig
    {
        public int NumPads { get; }
        public DualTime this[int lane] { get; set; }
    }

    public struct FourLane : IDrumPadConfig
    {
        public DrumPad_Pro Snare;
        public DrumPad_Pro Yellow;
        public DrumPad_Pro Blue;
        public DrumPad_Pro Green;

        public readonly int NumPads => 4;

        public DualTime this[int lane]
        {
            readonly get
            {
                return lane switch
                {
                    0 => Snare.Duration,
                    1 => Yellow.Duration,
                    2 => Blue.Duration,
                    3 => Green.Duration,
                    _ => throw new ArgumentOutOfRangeException(nameof(lane)),
                };
            }
            set
            {
                switch (lane)
                {
                    case 0: Snare.Duration = value; break;
                    case 1: Yellow.Duration = value; break;
                    case 2: Blue.Duration = value; break;
                    case 3: Green.Duration = value; break;
                    default: throw new ArgumentOutOfRangeException(nameof(lane));
                };
            }
        }

        public readonly override string ToString()
        {
            StringBuilder builder = new();
            if (Snare.Duration.IsActive())
            {
                builder.Append($"Snare: {Snare}|");
            }
            if (Yellow.Duration.IsActive())
            {
                builder.Append($"Yellow: {Yellow}|");
            }
            if (Blue.Duration.IsActive())
            {
                builder.Append($"Blue: {Blue}|");
            }
            if (Green.Duration.IsActive())
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

        public DualTime this[int lane]
        {
            readonly get
            {
                return lane switch
                {
                    0 => Snare.Duration,
                    1 => Yellow.Duration,
                    2 => Blue.Duration,
                    3 => Orange.Duration,
                    4 => Green.Duration,
                    _ => throw new ArgumentOutOfRangeException(nameof(lane)),
                };
            }
            set
            {
                switch (lane)
                {
                    case 0: Snare.Duration = value; break;
                    case 1: Yellow.Duration = value; break;
                    case 2: Blue.Duration = value; break;
                    case 3: Orange.Duration = value; break;
                    case 4: Green.Duration = value; break;
                    default: throw new ArgumentOutOfRangeException(nameof(lane));
                };
            }
        }

        public readonly override string ToString()
        {
            StringBuilder builder = new();

            if (Snare.Duration.IsActive())
            {
                builder.Append($"Snare: {Snare}|");
            }
            if (Yellow.Duration.IsActive())
            {
                builder.Append($"Yellow: {Yellow}|");
            }
            if (Blue.Duration.IsActive())
            {
                builder.Append($"Blue: {Blue}|");
            }
            if (Orange.Duration.IsActive())
            {
                builder.Append($"Orange: {Orange}|");
            }
            if (Green.Duration.IsActive())
            {
                builder.Append($"Green: {Green}|");
            }
            return builder.ToString();
        }
    }

    public struct UnknownLane : IDrumPadConfig
    {
        public DrumPad_Pro Snare;
        public DrumPad_Pro Yellow;
        public DrumPad_Pro Blue;
        public DrumPad_Pro Orange;
        public DrumPad_Pro Green;

        public readonly int NumPads => 5;

        public DualTime this[int lane]
        {
            readonly get
            {
                return lane switch
                {
                    0 => Snare.Duration,
                    1 => Yellow.Duration,
                    2 => Blue.Duration,
                    3 => Orange.Duration,
                    4 => Green.Duration,
                    _ => throw new ArgumentOutOfRangeException(nameof(lane)),
                };
            }
            set
            {
                switch (lane)
                {
                    case 0: Snare.Duration = value; break;
                    case 1: Yellow.Duration = value; break;
                    case 2: Blue.Duration = value; break;
                    case 3: Orange.Duration = value; break;
                    case 4: Green.Duration = value; break;
                    default: throw new ArgumentOutOfRangeException(nameof(lane));
                };
            }
        }

        public readonly override string ToString()
        {
            StringBuilder builder = new();

            if (Snare.Duration.IsActive())
            {
                builder.Append($"Snare: {Snare}|");
            }
            if (Yellow.Duration.IsActive())
            {
                builder.Append($"Yellow: {Yellow}|");
            }
            if (Blue.Duration.IsActive())
            {
                builder.Append($"Blue: {Blue}|");
            }
            if (Orange.Duration.IsActive())
            {
                builder.Append($"Orange: {Orange}|");
            }
            if (Green.Duration.IsActive())
            {
                builder.Append($"Green: {Green}|");
            }
            return builder.ToString();
        }
    }
}

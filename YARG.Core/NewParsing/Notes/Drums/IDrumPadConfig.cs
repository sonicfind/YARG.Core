using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public interface IDrumPadConfig<TPads>
        where TPads : unmanaged, IDrumPadConfig<TPads>
    {
        public static readonly int NUM_PADS;
        static IDrumPadConfig()
        {
            TPads d = default;
            NUM_PADS = d.NumPads;
        }

        public int NumPads { get; }
        public DualTime this[int lane] { get; set; }
        public void SetDynamics(int index, DrumDynamics dynamics);
    }

    public struct FourLane : IDrumPadConfig<FourLane>
    {
        public DrumPad Snare;
        public DrumPad Yellow;
        public DrumPad Blue;
        public DrumPad Green;

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
                }
            }
        }

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

    public struct FiveLane : IDrumPadConfig<FiveLane>
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
                }
            }
        }

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

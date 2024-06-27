using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public interface IDrumPadConfig<TPad>
        where TPad : unmanaged, IDrumPad
    {
        public int NumPads { get; }
        public ref TPad this[int lane] { get; }
    }

    public struct FourLane<TPad> : IDrumPadConfig<TPad>
        where TPad : unmanaged, IDrumPad
    {
        public TPad Snare;
        public TPad Yellow;
        public TPad Blue;
        public TPad Green;

        public readonly int NumPads => 4;

        public ref TPad this[int lane]
        {
            get
            {
                unsafe
                {
#pragma warning disable CS9084 // Struct member returns 'this' or other instance members by reference
                    switch (lane)
                    {
                        case 0: return ref Snare;
                        case 1: return ref Yellow;
                        case 2: return ref Blue;
                        case 3: return ref Green;
                        default: throw new ArgumentOutOfRangeException(nameof(lane));
                    }
#pragma warning restore CS9084 // Struct member returns 'this' or other instance members by reference
                }
            }
        }

        public readonly override string ToString()
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

    public struct FiveLane<TPad> : IDrumPadConfig<TPad>
        where TPad : unmanaged, IDrumPad
    {
        public TPad Snare;
        public TPad Yellow;
        public TPad Blue;
        public TPad Orange;
        public TPad Green;

        public readonly int NumPads => 5;

        public ref TPad this[int lane]
        {
            get
            {
                unsafe
                {
#pragma warning disable CS9084 // Struct member returns 'this' or other instance members by reference
                    switch (lane)
                    {
                        case 0: return ref Snare;
                        case 1: return ref Yellow;
                        case 2: return ref Blue;
                        case 3: return ref Orange;
                        case 4: return ref Green;
                        default: throw new ArgumentOutOfRangeException(nameof(lane));
                    }
#pragma warning restore CS9084 // Struct member returns 'this' or other instance members by reference
                }
            }
        }

        public readonly override string ToString()
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

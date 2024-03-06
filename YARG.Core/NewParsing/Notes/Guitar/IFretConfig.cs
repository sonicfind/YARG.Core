using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public interface IFretConfig
    {
        public int NumColors { get; }

        public ref DualTime this[int lane] { get; }
    }

    public struct FiveFret : IFretConfig
    {
        public DualTime Open;
        public DualTime Green;
        public DualTime Red;
        public DualTime Yellow;
        public DualTime Blue;
        public DualTime Orange;

        public readonly int NumColors => 5;

        public ref DualTime this[int lane]
        {
            get
            {
                unsafe
                {
#pragma warning disable CS9084 // Struct member returns 'this' or other instance members by reference
                    switch (lane)
                    {
                        case 0: return ref Open;
                        case 1: return ref Green;
                        case 2: return ref Red;
                        case 3: return ref Yellow;
                        case 4: return ref Blue;
                        case 5: return ref Orange;
                        default: throw new ArgumentOutOfRangeException(nameof(lane));
                    }
#pragma warning restore CS9084 // Struct member returns 'this' or other instance members by reference
                }
            }
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new();
            if (Open.IsActive())
                stringBuilder.Append($"Open: {Open.Ticks} | ");
            if (Green.IsActive())
                stringBuilder.Append($"Green: {Green.Ticks} | ");
            if (Red.IsActive())
                stringBuilder.Append($"Red: {Red.Ticks} | ");
            if (Yellow.IsActive())
                stringBuilder.Append($"Yellow: {Yellow.Ticks} | ");
            if (Blue.IsActive())
                stringBuilder.Append($"Blue: {Blue.Ticks} | ");
            if (Orange.IsActive())
                stringBuilder.Append($"Orange: {Orange.Ticks}");
            return stringBuilder.ToString();
        }
    }

    public struct SixFret : IFretConfig
    {
        public DualTime Open;
        public DualTime Black1;
        public DualTime Black2;
        public DualTime Black3;
        public DualTime White1;
        public DualTime White2;
        public DualTime White3;

        public readonly int NumColors => 6;

        public ref DualTime this[int lane]
        {
            get
            {
                unsafe
                {
#pragma warning disable CS9084 // Struct member returns 'this' or other instance members by reference
                    switch (lane)
                    {
                        case 0: return ref Open;
                        case 1: return ref Black1;
                        case 2: return ref Black2;
                        case 3: return ref Black3;
                        case 4: return ref White1;
                        case 5: return ref White2;
                        case 6: return ref White3;
                        default: throw new ArgumentOutOfRangeException(nameof(lane));
                    }
#pragma warning restore CS9084 // Struct member returns 'this' or other instance members by reference
                }
            }
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new();
            if (Open.IsActive())
                stringBuilder.Append($"Open: {Open.Ticks} | ");
            if (Black1.IsActive())
                stringBuilder.Append($"Black 1: {Black1.Ticks} | ");
            if (Black2.IsActive())
                stringBuilder.Append($"Black 2: {Black2.Ticks} | ");
            if (Black3.IsActive())
                stringBuilder.Append($"Black 3: {Black3.Ticks} | ");
            if (White1.IsActive())
                stringBuilder.Append($"White 1: {White1.Ticks} | ");
            if (White2.IsActive())
                stringBuilder.Append($"White 2: {White2.Ticks} | ");
            if (White3.IsActive())
                stringBuilder.Append($"White 3: {White3.Ticks}");
            return stringBuilder.ToString();
        }
    }
}

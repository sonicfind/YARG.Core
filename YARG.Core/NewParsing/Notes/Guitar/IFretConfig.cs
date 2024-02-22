using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public interface IFretConfig
    {
        public int NumColors { get; }
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
    }
}

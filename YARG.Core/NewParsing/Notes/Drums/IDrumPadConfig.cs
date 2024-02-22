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
    }

    public struct FiveLane : IDrumPadConfig
    {
        public DrumPad Snare;
        public DrumPad Yellow;
        public DrumPad Blue;
        public DrumPad Orange;
        public DrumPad Green;

        public readonly int NumPads => 5;
    }
}

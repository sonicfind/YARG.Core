using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.Parsing.Midi.Drums
{
    public abstract class DrumsMidiDiff
    {
        public bool Flam { get; set; }
        public readonly DualTime[] Notes;

        protected DrumsMidiDiff(int numLanes)
        {
            Notes = new DualTime[numLanes];
            for (int i = 0; i < numLanes; ++i)
                Notes[i] = DualTime.Inactive;
        }
    }

    public class FourLaneDiff : DrumsMidiDiff
    {
        public FourLaneDiff() : base(6) { }
    }

    public class FiveLaneDiff : DrumsMidiDiff
    {
        public FiveLaneDiff() : base(7) { }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing.Midi
{
    public abstract class DrumsMidiDifficulty
    {
        internal bool Flam;
        internal readonly DualTime[] Notes;

        protected DrumsMidiDifficulty(int numLanes)
        {
            Notes = new DualTime[numLanes];
            for (int i = 0; i < numLanes; ++i)
                Notes[i] = DualTime.Inactive;
        }
    }

    public class FourLaneDifficulty : DrumsMidiDifficulty
    {
        public FourLaneDifficulty() : base(6) { }
    }

    public class FiveLaneDifficulty : DrumsMidiDifficulty
    {
        public FiveLaneDifficulty() : base(7) { }
    }
}

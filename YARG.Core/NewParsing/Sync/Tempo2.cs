using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public struct Tempo2
    {
        public const int MICROS_PER_SECOND =  1000000;
        /// <summary>
        /// A factor used to convert to and from BPM and MicrosPerQuarter.<br></br>
        /// There is quite literally no unit of measurement for it.
        /// </summary>
        public const int BPM_FACTOR = 60000000;
        public const int DEFAULT_BPM = 120;
        public const int MICROS_AT_120BPM = BPM_FACTOR / DEFAULT_BPM;

        public static readonly Tempo2 DEFAULT = new()
        {
            MicrosPerQuarter = MICROS_AT_120BPM
        };

        public int MicrosPerQuarter;
        public long Anchor;

        public float BPM
        {
            readonly get { return MicrosPerQuarter != 0 ? (float) BPM_FACTOR / MicrosPerQuarter : 0; }
            set { MicrosPerQuarter = value != 0 ? (int) (BPM_FACTOR / value) : 0; }
        }
    }
}

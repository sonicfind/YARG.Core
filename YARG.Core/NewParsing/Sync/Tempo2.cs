using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public struct Tempo2
    {
        public const int BPM_FACTOR = 60000000;
        public const int DEFAULT_BPM = 120;
        public const int MICROS_AT_120BPM = BPM_FACTOR / DEFAULT_BPM;

        public static readonly Tempo2 DEFAULT = new()
        {
            Micros = MICROS_AT_120BPM
        };

        public int Micros;
        public long Anchor;

        public float BPM
        {
            readonly get { return Micros != 0 ? (float) BPM_FACTOR / Micros : 0; }
            set { Micros = value != 0 ? (int) (BPM_FACTOR / value) : 0; }
        }
    }
}

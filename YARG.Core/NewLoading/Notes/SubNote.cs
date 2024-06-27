using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public struct SubNote
    {
        public readonly int Index;
        public readonly DualTime EndPosition;

        public HitStatus Status;

        public SubNote(int index, DualTime endPosition)
        {
            Index = index;
            EndPosition = endPosition;
            Status = HitStatus.Idle;
        }
    }
}

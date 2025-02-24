﻿using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public struct HittablePhrase
    {
        public DualTime EndTime;
        public long     TotalNotes;

        public HittablePhrase(in DualTime endTime)
        {
            EndTime = endTime;
            TotalNotes = 0;
        }
    }
}

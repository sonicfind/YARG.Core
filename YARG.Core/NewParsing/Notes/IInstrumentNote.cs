using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public interface IInstrumentNote
    {
        public int NUMLANES { get; }
        public int GetNumActiveLanes();
        public DualTime GetLongestSustain();
    }
}

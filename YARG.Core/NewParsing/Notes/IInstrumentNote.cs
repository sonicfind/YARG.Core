using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public interface IInstrumentNote
    {
        public int GetNumActiveLanes();
        public long GetLongestSustain();
    }
}

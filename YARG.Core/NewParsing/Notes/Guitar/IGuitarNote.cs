using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public interface IGuitarNote : IInstrumentNote
    {
        public GuitarState State { get; set; }
    }
}

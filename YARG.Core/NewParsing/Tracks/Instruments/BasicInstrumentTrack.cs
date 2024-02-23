using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public class BasicInstrumentTrack<TNote> : InstrumentTrack2<DifficultyTrack2<TNote>>
        where TNote : unmanaged, IInstrumentNote
    {
    }
}

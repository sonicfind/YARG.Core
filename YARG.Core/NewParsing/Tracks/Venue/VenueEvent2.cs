using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Chart;

namespace YARG.Core.NewParsing
{
    public struct VenueEvent2<TType>
        where TType : unmanaged
    {
        private readonly VenueEventFlags _flags;
        public TType Value;

        public readonly bool IsOptional => (_flags & VenueEventFlags.Optional) != 0;

        public VenueEvent2(TType value, VenueEventFlags flags = VenueEventFlags.None)
        {
            Value = value;
            _flags = flags;
        }
    }
}

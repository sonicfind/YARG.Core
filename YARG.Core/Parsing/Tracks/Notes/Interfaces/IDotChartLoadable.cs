using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.Parsing
{
    internal interface IDotChartLoadable
    {
        bool SetFromDotChart(int lane, in DualTime length);
    }
}

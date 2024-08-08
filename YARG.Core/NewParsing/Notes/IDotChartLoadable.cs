using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public interface IDotChartLoadable
    {
        public bool SetFromDotChart(int lane, in DualTime length);
    }
}

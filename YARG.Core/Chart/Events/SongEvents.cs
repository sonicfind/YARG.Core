using System.Collections.Generic;
using YARG.Core.Chart.FlatDictionary;

namespace YARG.Core.Chart
{
    public class SongEvents
    {
        public readonly TimedFlatDictionary<SongSection> sections = new();
        public readonly TimedFlatDictionary<List<string>> globals = new();
    }
}

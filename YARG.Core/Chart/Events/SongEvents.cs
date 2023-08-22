using System.Collections.Generic;

namespace YARG.Core.Chart
{
    public class SongEvents
    {
        public readonly TimedFlatDictionary<SongSection> sections = new();
        public readonly TimedFlatDictionary<List<string>> globals = new();
    }
}

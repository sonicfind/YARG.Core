using System.Collections.Generic;
using YARG.Core.Chart.FlatDictionary;

namespace YARG.Core.Chart
{
    public class SongEvents
    {
        public readonly TimedManagedFlatDictionary<SongSection> sections = new();
        public readonly TimedManagedFlatDictionary<List<string>> globals = new();

        public void Clear()
        {
            sections.Clear();
            globals.Clear();
        }
    }
}

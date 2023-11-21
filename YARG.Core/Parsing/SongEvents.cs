using System.Collections.Generic;

namespace YARG.Core.Parsing
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

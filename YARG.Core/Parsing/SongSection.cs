namespace YARG.Core.Chart
{
    public class SongSection
    {
        public string Name = string.Empty;
        public static implicit operator string(SongSection section) => section.Name;
        public static implicit operator SongSection(string str) => new(str);

        public SongSection() { }
        public SongSection(string name) { Name = name; }
    }
}

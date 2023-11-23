namespace YARG.Core.Parsing
{
    public interface INote
    {
        public int GetNumActiveNotes();
        public DualTime GetLongestSustain();
    }
}

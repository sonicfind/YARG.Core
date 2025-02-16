namespace YARG.Core.NewParsing
{
    public interface IInstrumentNote
    {
        public int GetNumActiveLanes();
        public DualTime GetLongestSustain();
    }
}

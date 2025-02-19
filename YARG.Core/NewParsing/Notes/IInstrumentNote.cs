namespace YARG.Core.NewParsing
{
    public interface IInstrumentNote
    {
        int GetNumActiveLanes();
        DualTime GetLongestSustain();
    }
}

using System.Diagnostics.Contracts;

namespace YARG.Core.NewParsing
{
    public interface IInstrumentNote
    {
        [Pure]
        int GetNumActiveLanes();
        [Pure]
        DualTime GetLongestSustain();
    }
}

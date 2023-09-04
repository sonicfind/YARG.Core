namespace YARG.Core.Chart.FlatDictionary
{
    public class TimedFlatDictionary<TObj> : FlatDictionary<long, TObj>
        where TObj : new()
    {
    }

    public class TimedNativeFlatDictionary<TObj> : NativeFlatDictionary<long, TObj>
        where TObj : unmanaged
    {
    }
}

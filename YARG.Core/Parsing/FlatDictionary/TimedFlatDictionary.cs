namespace YARG.Core.Parsing
{
    /// <summary>
    /// Subtype of ManagedFlatDictionary that uses "long"s as keys
    /// </summary>
    /// <typeparam name="TObj">The objects to place at each new position</typeparam>
    public class TimedManagedFlatDictionary<TObj> : ManagedFlatDictionary<long, TObj>
        where TObj : new()
    {
    }

    /// <summary>
    /// Subtype of NativeFlatDictionary that uses "long"s as keys
    /// </summary>
    /// <typeparam name="TObj">The unmanaged objects to place at each new position</typeparam>
    public class TimedNativeFlatDictionary<TObj> : NativeFlatDictionary<long, TObj>
        where TObj : unmanaged
    {
    }
}

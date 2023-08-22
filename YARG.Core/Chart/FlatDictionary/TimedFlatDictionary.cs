namespace YARG.Core.Chart
{
    public struct TimedFlatMapNode<T>
    {
        public long position;
        public T obj;
        public static bool operator <(TimedFlatMapNode<T> node, long position) { return node.position < position; }
        public static bool operator >(TimedFlatMapNode<T> node, long position) { return node.position > position; }
    }

    public class TimedFlatDictionary<TObj> : FlatDictionary<long, TObj>
        where TObj : new()
    {
    }
}

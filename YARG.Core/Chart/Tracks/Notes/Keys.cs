namespace YARG.Core.Chart
{
    public unsafe class Keys : Note_FW<TruncatableSustain>
    {
        public Keys() : base(5) { }
        public long this[int lane]
        {
            get { return lanes[lane]; }
            set { lanes[lane] = value; }
        }
    }
}

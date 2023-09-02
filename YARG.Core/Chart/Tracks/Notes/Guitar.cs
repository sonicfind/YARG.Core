namespace YARG.Core.Chart
{
    public enum ForceStatus
    {
        NATURAL,
        FORCED_LEGACY,
        HOPO,
        STRUM
    }

    public abstract unsafe class GuitarNote_FW : Note_FW<TruncatableSustain>
    {
        public ForceStatus Forcing { get; set; }
        public bool IsTap { get; set; }
        public void ToggleTap() { IsTap = !IsTap; }

        protected GuitarNote_FW(int numColors) : base(numColors) { }

        public long this[int lane]
        {
            get => lanes[lane];
            set
            {
                lanes[lane].Duration = value;
                if (lane == 0)
                {
                    for (int i = 1; i < numLanes; ++i)
                        lanes[i].Disable();
                }
                else
                    lanes[0].Disable();
            }
        }

        public void Disable(int lane)
        {
            lanes[lane].Disable();
        }
    }

    public class FiveFret : GuitarNote_FW
    {
        public FiveFret() : base(6) { }
    }

    public class SixFret : GuitarNote_FW
    {
        public SixFret() : base(7) { }
    }
}

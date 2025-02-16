namespace YARG.Core.NewParsing
{
    public enum GuitarState
    {
        Natural,
        Forced,
        Hopo,
        Strum,
        Tap
    }

    public interface IGuitarNote : IInstrumentNote
    {
        public GuitarState State { get; set; }
    }
}

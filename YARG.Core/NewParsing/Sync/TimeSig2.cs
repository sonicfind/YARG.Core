namespace YARG.Core.NewParsing
{
    /// <summary>
    /// Holds all time signature information (abiding by .midi's structure)
    /// </summary>
    public struct TimeSig2
    {
        public static readonly TimeSig2 DEFAULT = new()
        {
            Numerator = 4,
            Denominator = 2,
            Metronome = 24,
            Num32nds = 8
        };

        public byte Numerator;
        public byte Denominator;
        public byte Metronome;
        public byte Num32nds;
    }
}

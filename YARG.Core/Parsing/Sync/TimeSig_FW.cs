namespace YARG.Core.Parsing
{
    public struct TimeSig_FW
    {
        public static readonly TimeSig_FW DEFAULT = new(4, 2, 24, 8);

        public byte Numerator;
        public byte Denominator;
        public byte Metronome;
        public byte Num32nds;

        public TimeSig_FW(byte numerator, byte denominator, byte metronome, byte num32nds)
        {
            Numerator = numerator;
            Denominator = denominator;
            Metronome = metronome;
            Num32nds = num32nds;
        }
    }
}

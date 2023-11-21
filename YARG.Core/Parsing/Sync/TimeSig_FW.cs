namespace YARG.Core.Parsing
{
    public struct TimeSig_FW
    {
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

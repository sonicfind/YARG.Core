namespace YARG.Core.NewParsing.Midi
{
    internal struct Midi_Phrase
    {
        internal readonly SpecialPhraseType[] Types;
        internal DualTime Position;
        internal int Velocity;

        internal Midi_Phrase(params SpecialPhraseType[] types)
        {
            Types = types;
            Position = DualTime.Inactive;
            Velocity = 0;
        }
    }

    internal struct Midi_PhraseMapping
    {
        internal readonly int[] MidiValues;
        internal Midi_Phrase Phrase;

        internal Midi_PhraseMapping(int[] values, params SpecialPhraseType[] types)
        {
            MidiValues = values;
            Phrase = new Midi_Phrase(types);
        }
    }
}

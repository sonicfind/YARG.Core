using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    public abstract class MidiInstrumentLoader<TTrack> : MidiTrackLoader
        where TTrack : Track, new()
    {
        private readonly DualTime[] _BRENotes;

        internal readonly TTrack Track = new();

        protected MidiInstrumentLoader(int numBRElanes)
        {
            _BRENotes = new DualTime[numBRElanes];
            for (int i = 0; i < numBRElanes; ++i)
            {
                _BRENotes[i] = DualTime.Inactive;
            }
        }

        private DualTime _lastOnTick = default;
        internal void NormalizeNoteOnPosition()
        {
            if (Position.Ticks < _lastOnTick.Ticks + 16)
                Position = _lastOnTick;
            else
                _lastOnTick = Position;
        }

        internal bool ParseBRE_ON()
        {
            if (Note.value < 120 || 124 < Note.value)
            {
                return false;
            }

            _BRENotes[Note.value - 120] = Position;
            return true;
        }

        internal bool ParseBRE_Off()
        {
            if (Note.value < 120 || 124 < Note.value)
            {
                return false;
            }

            for (int i = 0; i < _BRENotes.Length - 1; ++i)
            {
                if (_BRENotes[i].Ticks != _BRENotes[i + 1].Ticks)
                {
                    return true;
                }
            }

            Track.SpecialPhrases.TraverseBackwardsUntil(_BRENotes[0])
                                .TryAdd(SpecialPhraseType.BRE, new SpecialPhraseInfo(Position - _BRENotes[0]));

            for (int i = 0; i < _BRENotes.Length; i++)
            {
                _BRENotes[i] = DualTime.Inactive;
            }
            return true;
        }

        internal bool AddPhrase_ON(Midi_PhraseMapping[] mappings)
        {
            return AddPhrase_ON(mappings, Track.SpecialPhrases);
        }

        internal bool AddPhrase_Off(Midi_PhraseMapping[] mappings)
        {
            return AddPhrase_Off(mappings, Track.SpecialPhrases);
        }
    }

    internal class MidiBasicInstrumentLoader
    {
        public const int NOTES_PER_DIFFICULTY = 12;
        public const int DEFAULT_MIN = 60;
        public const int DEFAULT_MAX = 100;

        public static readonly Midi_PhraseMapping SOLO =    new(new[] { 103 }, SpecialPhraseType.Solo);
        public static readonly Midi_PhraseMapping TREMOLO = new(new[] { 126 }, SpecialPhraseType.Tremolo);
        public static readonly Midi_PhraseMapping TRILL =   new(new[] { 127 }, SpecialPhraseType.Trill);

        public static readonly int[] DIFFVALUES = new int[InstrumentTrack2.NUM_DIFFICULTIES * NOTES_PER_DIFFICULTY]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3
        };

        static MidiBasicInstrumentLoader() { }
    }

    public class MidiBasicInstrumentLoader<TNote, TDiffTracker> : MidiInstrumentLoader<BasicInstrumentTrack2<TNote>>
        where TNote : unmanaged, IInstrumentNote
        where TDiffTracker : new()
    {
        internal readonly TDiffTracker[] Difficulties = new TDiffTracker[InstrumentTrack2.NUM_DIFFICULTIES];
        internal readonly Midi_PhraseMapping[] PhraseMappings = new Midi_PhraseMapping[]
        {
            OverdrivePhrase, MidiBasicInstrumentLoader.SOLO, MidiBasicInstrumentLoader.TREMOLO, MidiBasicInstrumentLoader.TRILL,
        };

        internal MidiBasicInstrumentLoader(HashSet<Difficulty>? difficulties, int numBRELanes)
            : base(numBRELanes)
        {
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; i++)
            {
                if (difficulties == null || difficulties.Contains((Difficulty) i))
                {
                    Track[i] = new();
                    Difficulties[i] = new();
                }
            }
        }

        internal bool AddPhrase_ON()
        {
            return AddPhrase_ON(PhraseMappings);
        }

        internal bool AddPhrase_Off()
        {
            return AddPhrase_Off(PhraseMappings);
        }
    }
}

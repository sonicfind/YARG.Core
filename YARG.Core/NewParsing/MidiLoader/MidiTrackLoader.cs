using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    using SortedPhraseList = YARGManagedSortedList<DualTime, Dictionary<SpecialPhraseType, SpecialPhraseInfo>>;

    public abstract class MidiTrackLoader
    {
        internal static readonly byte[] SYSEXTAG = Encoding.ASCII.GetBytes("PS");
        internal static Midi_PhraseMapping OverdrivePhrase = new(new[] { 116 }, SpecialPhraseType.StarPower);

        public static void SetMultiplierNote(int note)
        {
            OverdrivePhrase.MidiValues[0] = note == 103 ? 116 : 103;
        }

        protected DualTime _position = default;
        protected MidiNote _note = default;
        protected MidiEvent _event = MidiEvent.Default;

        protected void Process(YARGMidiTrack midiTrack, SyncTrack2 sync)
        {
            int tempoIndex = 0;
            while (midiTrack.ParseEvent(true, ref _event))
            {
                _position.Ticks = midiTrack.Position;
                _position.Seconds = sync.ConvertToSeconds(midiTrack.Position, ref tempoIndex);
                if (_event.Type == MidiEventType.Note_On)
                {
                    midiTrack.ExtractMidiNote(ref _note);
                    if (_note.velocity > 0)
                    {
                        ParseNote_ON();
                    }
                    else
                    {
                        ParseNote_Off();
                    }
                }
                else if (_event.Type == MidiEventType.Note_Off)
                {
                    midiTrack.ExtractMidiNote(ref _note);
                    ParseNote_Off();
                }
                else if (_event.Type == MidiEventType.SysEx || _event.Type == MidiEventType.SysEx_End)
                {
                    ParseSysEx(midiTrack.ExtractTextOrSysEx(in _event));
                }
                else if (MidiEventType.Text <= _event.Type && _event.Type <= MidiEventType.Text_EnumLimit)
                {
                    ParseText(midiTrack.ExtractTextOrSysEx(in _event));
                }
            }
        }

        protected abstract void ParseNote_ON();
        protected abstract void ParseNote_Off();
        protected virtual void ParseSysEx(ReadOnlySpan<byte> str) { }
        protected virtual void ParseText(ReadOnlySpan<byte> str) { }

        internal bool AddPhrase_ON(Midi_PhraseMapping[] mappings, SortedPhraseList phrases)
        {
            for (int i = 0; i < mappings.Length; ++i)
            {
                ref var map = ref mappings[i];
                foreach (int val in map.MidiValues)
                {
                    if (val == _note.value)
                    {
                        phrases.GetLastOrAppend(_position);
                        map.Phrase.Position = _position;
                        map.Phrase.Velocity = _note.velocity;
                        return true;
                    }
                }
            }
            return false;
        }

        internal bool AddPhrase_Off(Midi_PhraseMapping[] mappings, SortedPhraseList phrases)
        {
            for (int i = 0; i < mappings.Length; ++i)
            {
                ref var map = ref mappings[i];
                foreach (int val in map.MidiValues)
                {
                    if (val == _note.value)
                    {
                        ref var phr = ref map.Phrase;
                        if (phr.Position.Ticks != -1)
                        {
                            foreach (var type in phr.Types)
                            {
                                phrases.TraverseBackwardsUntil(phr.Position)
                                       .TryAdd(type, new SpecialPhraseInfo(_position - phr.Position, phr.Velocity));
                            }
                            phr.Position.Ticks = -1;
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        internal bool AddPhrase_ON(Midi_PhraseMapping[] mappings, SortedPhraseList phrases, SpecialPhraseType phraseToAdd, byte velocity)
        {
            for (int i = 0; i < mappings.Length; ++i)
            {
                ref var phr = ref mappings[i].Phrase;
                if (phr.Types.Contains(phraseToAdd))
                {
                    phrases.GetLastOrAppend(_position);
                    phr.Position = _position;
                    phr.Velocity = velocity;
                    return true;
                }
            }
            return false;
        }

        internal bool AddPhrase_Off(Midi_PhraseMapping[] mappings, SortedPhraseList phrases, SpecialPhraseType phraseToAdd)
        {
            for (int i = 0; i < mappings.Length; ++i)
            {
                ref var phr = ref mappings[i].Phrase;
                if (phr.Types.Contains(phraseToAdd))
                {
                    if (phr.Position.Ticks != -1)
                    {
                        foreach (var type in phr.Types)
                        {
                            phrases.TraverseBackwardsUntil(phr.Position)
                                   .TryAdd(type, new SpecialPhraseInfo(_position - phr.Position, phr.Velocity));
                        }
                        phr.Position.Ticks = -1;
                    }
                    return true;
                }
            }
            return false;
        }
    }
}

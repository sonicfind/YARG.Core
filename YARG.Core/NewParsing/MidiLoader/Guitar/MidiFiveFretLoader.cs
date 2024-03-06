using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    public class MidiFiveFretLoader : MidiBasicInstrumentLoader<GuitarNote2<FiveFret>, FiveFretMidiDifficulty>
    {
        public static BasicInstrumentTrack2<GuitarNote2<FiveFret>> Load(YARGMidiTrack midiTrack, SyncTrack2 sync, HashSet<Difficulty>? difficulties)
        {
            var loader = new MidiFiveFretLoader(difficulties);
            int tempoIndex = 0;
            while (midiTrack.ParseEvent(true))
            {
                loader.Position.Ticks = midiTrack.Position;
                loader.Position.Seconds = sync.ConvertToSeconds(midiTrack.Position, ref tempoIndex);
                if (midiTrack.Type == MidiEventType.Note_On)
                {
                    midiTrack.ExtractMidiNote(ref loader.Note);
                    if (loader.Note.velocity > 0)
                    {
                        loader.ParseNote_ON();
                    }
                    else
                        loader.ParseNote_Off();
                }
                else if (midiTrack.Type == MidiEventType.Note_Off)
                {
                    midiTrack.ExtractMidiNote(ref loader.Note);
                    loader.ParseNote_Off();
                }
                else if (midiTrack.Type == MidiEventType.SysEx || midiTrack.Type == MidiEventType.SysEx_End)
                {
                    loader.ParseSysEx(midiTrack.ExtractTextOrSysEx());
                }
                else if (MidiEventType.Text <= midiTrack.Type && midiTrack.Type <= MidiEventType.Text_EnumLimit)
                {
                    loader.ParseText(midiTrack.ExtractTextOrSysEx());
                }
            }

            loader.Track.TrimExcess();
            return loader.Track;
        }

        private static readonly byte[][] ENHANCED_STRINGS = new byte[][] { Encoding.ASCII.GetBytes("[ENHANCED_OPENS]"), Encoding.ASCII.GetBytes("ENHANCED_OPENS") };

        private readonly int[] _lanes = new int[]
        {
            13, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
            13, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
            13, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
            13, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
        };

        private MidiFiveFretLoader(HashSet<Difficulty>? difficulties)
            : base(difficulties, 5) { }

        private void ParseNote_ON()
        {
            NormalizeNoteOnPosition();
            if (59 <= Note.value && Note.value <= 107)
            {
                ParseLaneColor_ON();
            }
            else if (!AddPhrase_ON())
            {
                ParseBRE_ON();
            }
        }

        private void ParseNote_Off()
        {
            if (59 <= Note.value && Note.value <= 107)
            {
                ParseLaneColor_Off();
            }
            else if (!AddPhrase_Off())
            {
                ParseBRE_Off();
            }
        }

        private void ParseSysEx(ReadOnlySpan<byte> str)
        {
            if (!str.StartsWith(SYSEXTAG))
            {
                return;
            }

            bool enable = str[6] == 1;
            if (enable)
            {
                NormalizeNoteOnPosition();
            }

            if (str[4] == (char) 0xFF)
            {
                switch (str[5])
                {
                    case 1:
                        int status = str[6] == 0 ? 1 : 0;
                        for (int diffIndex = 0; diffIndex < 4; ++diffIndex)
                        {
                            _lanes[12 * diffIndex + 1] = status;
                        }
                        break;
                    case 4:
                        for (int diffIndex = 0; diffIndex < 4; ++diffIndex)
                        {
                            Difficulties[diffIndex]?.ProcessTapSysex_ON(Track[diffIndex]!, Position);
                        }
                        break;
                }
            }
            else
            {
                byte diffIndex = str[4];
                ref var tracker = ref Difficulties[diffIndex];
                if (tracker == null)
                    return;

                switch (str[5])
                {
                    case 1:
                        _lanes[12 * diffIndex + 1] = str[6] == 0 ? 1 : 0;
                        break;
                    case 4:
                        Difficulties[diffIndex]?.ProcessTapSysex_Off(Track[diffIndex]!, Position);
                        break;
                }
            }
        }

        private void ParseText(ReadOnlySpan<byte> str)
        {
            if (_lanes[0] == 13 && (str.SequenceEqual(ENHANCED_STRINGS[0]) || str.SequenceEqual(ENHANCED_STRINGS[1])))
            {
                for (int diff = 0; diff < 4; ++diff)
                {
                    _lanes[12 * diff] = 0;
                }
            }
            else
            {
                Track.Events.GetLastOrAppend(Position).Add(Encoding.UTF8.GetString(str));
            }
        }

        private void ParseLaneColor_ON()
        {
            int noteValue = Note.value - 59;
            int diffIndex = MidiBasicInstrumentLoader.DIFFVALUES[noteValue];
            int lane = _lanes[noteValue];

            var midiDiff = Difficulties[diffIndex];
            if (midiDiff == null && (diffIndex != 3 || lane != 8))
                return;

            ref var diff = ref Track[diffIndex]!;
            switch(lane)
            {
                case < 6:
                    midiDiff!.Notes[lane] = Position;
                    if (diff.Notes.Capacity == 0)
                    {
                        diff.Notes.Capacity = 5000;
                    }

                    unsafe
                    {
                        if (diff.Notes.TryAppend(Position, out var note))
                        {
                            if (midiDiff.SliderNotes)
                                note->State = GuitarState.Tap;
                            else if (midiDiff.HopoOn)
                                note->State = GuitarState.Hopo;
                            else if (midiDiff.HopoOff)
                                note->State = GuitarState.Strum;
                        }
                    }
                    
                    break;
                case 6:
                    midiDiff!.HopoOn = true;
                    unsafe
                    {
                        if (diff.Notes.TryGetLastValue(Position, out var note))
                        {
                            // In official games, Hopo flag has preference over Strum flags
                            // Therefore, the only limiter is Tap
                            if (note->State != GuitarState.Tap)
                                note->State = GuitarState.Hopo;
                        }
                    }
                    
                    break;
                case 7:
                    midiDiff!.HopoOff = true;
                    unsafe
                    {
                        if (diff.Notes.TryGetLastValue(Position, out var note))
                        {
                            // In official games, Tap & Hopo both have preference over strum
                            // Therefore, we only override the Natrual state
                            if (note->State == GuitarState.Natural)
                                note->State = GuitarState.Strum;
                        }
                    }
                    break;
                case 8:
                    if (diffIndex == 3)
                    {
                        AddPhrase_ON(PhraseMappings, Track.SpecialPhrases, SpecialPhraseType.Solo, 100);
                        break;
                    }

                    for (int i = 0; i < 4; ++i)
                        _lanes[12 * i + 8] = 12;

                    var expertPhrases = Track[3]!.SpecialPhrases;
                    for (int i = 0; i < Track.SpecialPhrases.Count;)
                    {
                        var node = Track.SpecialPhrases.ElementAtIndex(i);
                        if (node.Value.Remove(SpecialPhraseType.Solo, out var phrase))
                        {
                            expertPhrases[node.Key].TryAdd(SpecialPhraseType.StarPower_Diff, phrase);
                            if (node.Value.Count == 0)
                            {
                                Track.SpecialPhrases.RemoveAtIndex(i);
                                continue;
                            }
                        }
                        ++i;
                    }

                    AddPhrase_ON(midiDiff!.Phrases, diff.SpecialPhrases, SpecialPhraseType.StarPower_Diff, 100);
                    break;
                case 9:
                    midiDiff!.SliderNotes = true;
                    break;
                case 10:
                    AddPhrase_ON(midiDiff!.Phrases, diff.SpecialPhrases, SpecialPhraseType.FaceOff_Player1, 100);
                    break;
                case 11:
                    AddPhrase_ON(midiDiff!.Phrases, diff.SpecialPhrases, SpecialPhraseType.FaceOff_Player2, 100);
                    break;
                case 12:
                    AddPhrase_ON(midiDiff!.Phrases, diff.SpecialPhrases, SpecialPhraseType.StarPower_Diff, 100);
                    break;

            }
        }

        private void ParseLaneColor_Off()
        {
            int noteValue = Note.value - 59;
            int diffIndex = MidiBasicInstrumentLoader.DIFFVALUES[noteValue];
            int lane = _lanes[noteValue];

            var midiDiff = Difficulties[diffIndex];
            if (midiDiff == null && (diffIndex != 3 || lane != 8))
                return;

            ref var diff = ref Track[diffIndex]!;
            switch (lane)
            {
                case < 6:
                    ref var colorPosition = ref Difficulties[diffIndex].Notes[lane];
                    if (colorPosition.Ticks != -1)
                    {
                        diff.Notes.TraverseBackwardsUntil(colorPosition).Frets[lane] = DualTime.Truncate(Position - colorPosition);
                        colorPosition.Ticks = -1;
                    }
                    break;
                case 6:
                    midiDiff!.HopoOn = false;
                    unsafe
                    {
                        if (diff.Notes.TryGetLastValue(Position, out var note))
                        {
                            if (note->State == GuitarState.Hopo)
                            {
                                note->State = midiDiff!.HopoOff ? GuitarState.Strum : GuitarState.Natural;
                            }
                        }
                    }
                    break;
                case 7:
                    midiDiff!.HopoOff = false;
                    unsafe
                    {
                        if (diff.Notes.TryGetLastValue(Position, out var note))
                        {
                            if (note->State == GuitarState.Strum)
                            {
                                note->State = midiDiff!.HopoOn ? GuitarState.Hopo : GuitarState.Natural;
                            }
                        }
                    }
                    break;
                case 8:
                    AddPhrase_Off(PhraseMappings, Track.SpecialPhrases, SpecialPhraseType.Solo);
                    break;
                case 9:
                    midiDiff!.SliderNotes = false;
                    break;
                case 10:
                    AddPhrase_Off(midiDiff!.Phrases, diff.SpecialPhrases, SpecialPhraseType.FaceOff_Player1);
                    break;
                case 11:
                    AddPhrase_Off(midiDiff!.Phrases, diff.SpecialPhrases, SpecialPhraseType.FaceOff_Player2);
                    break;
                case 12:
                    AddPhrase_Off(midiDiff!.Phrases, diff.SpecialPhrases, SpecialPhraseType.StarPower_Diff);
                    break;

            }
        }
    }
}

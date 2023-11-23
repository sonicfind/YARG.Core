using System;
using System.Text;
using YARG.Core.IO;

namespace YARG.Core.Parsing.Midi
{
    public static class MidiTrackLoader
    {
        public static void SetMultiplierNote(int note)
        {
            OverdrivePhrase = new int[] { note };
        }

        public static int[] OverdrivePhrase = { 116 };
    }

    public abstract class MidiTrackLoader<TTrack>
        where TTrack : Track
    {
        protected static readonly byte[] SYSEXTAG = Encoding.ASCII.GetBytes("PS");
        protected DualTime position;
        protected MidiNote note;
        protected readonly Midi_PhraseList phrases;
        protected readonly TTrack track;

        protected MidiTrackLoader(TTrack track, Midi_PhraseList phrases)
        {
            this.track = track;
            this.phrases = phrases;
        }

        protected TTrack Process(SyncTrack_FW sync, YARGMidiTrack midiTrack)
        {
            int tempoIndex = 0;
            while (midiTrack.ParseEvent(true))
            {
                position.ticks = midiTrack.Position;
                position.seconds = sync.ConvertToSeconds(midiTrack.Position, ref tempoIndex);
                if (midiTrack.Type == MidiEventType.Note_On)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    if (note.velocity > 0)
                    {
                        ParseNote_ON(midiTrack);
                    }
                    else
                        ParseNote_Off(midiTrack);
                }
                else if (midiTrack.Type == MidiEventType.Note_Off)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    ParseNote_Off(midiTrack);
                }
                else if (midiTrack.Type == MidiEventType.SysEx || midiTrack.Type == MidiEventType.SysEx_End)
                    ParseSysEx(midiTrack.ExtractTextOrSysEx());
                else if (midiTrack.Type <= MidiEventType.Text_EnumLimit)
                    ParseText(midiTrack.ExtractTextOrSysEx());
            }

            track.TrimExcess();
            return track;
        }

        protected abstract void ParseNote_ON(YARGMidiTrack midiTrack);

        protected abstract void ParseNote_Off(YARGMidiTrack midiTrack);

        protected virtual void ParseSysEx(ReadOnlySpan<byte> str) { }

        protected virtual void ParseText(ReadOnlySpan<byte> str)
        {
            track.Events.Get_Or_Add_Last(position).Add(Encoding.UTF8.GetString(str));
        }
    }
}

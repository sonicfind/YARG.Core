using System;
using System.Text;
using YARG.Core.IO;

namespace YARG.Core.Song
{
    public abstract class Midi_Preparser
    {
        protected static readonly byte[] SYSEXTAG = Encoding.ASCII.GetBytes("PS");
        protected MidiNote _note;
        protected MidiEvent _event = MidiEvent.Default;

        protected Midi_Preparser() { }

        protected bool Process(YARGMidiTrack track)
        {
            while (track.ParseEvent(false, ref _event))
            {
                switch (_event.Type)
                {
                    case MidiEventType.Note_On:
                        track.ExtractMidiNote(ref _note);
                        if (_note.velocity > 0 ? ParseNote_ON() : ParseNote_Off())
                            return true;
                        break;
                    case MidiEventType.Note_Off:
                        track.ExtractMidiNote(ref _note);
                        if (ParseNote_Off())
                            return true;
                        break;
                    case MidiEventType.SysEx:
                    case MidiEventType.SysEx_End:
                        ParseSysEx(track.ExtractTextOrSysEx(in _event));
                        break;
                    case >= MidiEventType.Text and <= MidiEventType.Text_EnumLimit:
                        ParseText(track.ExtractTextOrSysEx(in _event));
                        break;
                }
            }
            return false;
        }

        protected abstract bool ParseNote_ON();

        protected abstract bool ParseNote_Off();

        protected virtual void ParseSysEx(ReadOnlySpan<byte> str) { }

        protected virtual void ParseText(ReadOnlySpan<byte> str) { }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using YARG.Core.Extensions;
using YARG.Core.Logging;

namespace YARG.Core.IO
{
    public struct YARGMidiFile : IEnumerable<YARGMidiTrack>
    {
        private static readonly uint HEADER_TAG = CharacterCodes.ConvertToInt32('M', 'T', 'h', 'd');
        private static readonly uint TRACK_TAG  = CharacterCodes.ConvertToInt32('M', 'T', 'r', 'k');
        private const int TAG_SIZE = sizeof(uint);

        private readonly ushort _format;
        private readonly ushort _num_tracks;
        private readonly ushort _resolution;

        private readonly FixedArray<byte> _data;
        private long _position;
        private ushort _trackNumber;

        public readonly ushort Format => _format;
        public readonly ushort NumTracks => _num_tracks;
        public readonly ushort Resolution => _resolution;
        public readonly ushort TrackNumber => _trackNumber;

        private const int SIZEOF_HEADER = 6;
        public unsafe YARGMidiFile(in FixedArray<byte> data)
        {
            if (TAG_SIZE > data.Length || *(uint*)data.Ptr != HEADER_TAG)
            {
                throw new Exception("Midi header Tag 'MThd' mismatch");
            }

            const int DATA_OFFSET = TAG_SIZE + sizeof(int);
            if (DATA_OFFSET > data.Length)
            {
                throw new EndOfStreamException("End of stream found within midi header");
            }
            
            int headerSize = (data[TAG_SIZE] << 24) | (data[TAG_SIZE + 1] << 16) | (data[TAG_SIZE + 2] << 8) | data[TAG_SIZE + 3];
            if (headerSize < SIZEOF_HEADER)
            {
                throw new Exception("Midi header length less than minimum");
            }

            _position = DATA_OFFSET + headerSize;
            if (_position > data.Length)
            {
                throw new EndOfStreamException("End of stream found within midi header");
            }

            _format     = (ushort)((data[DATA_OFFSET] << 8)     | data[DATA_OFFSET + 1]);
            _num_tracks = (ushort)((data[DATA_OFFSET + 2] << 8) | data[DATA_OFFSET + 3]);
            _resolution = (ushort)((data[DATA_OFFSET + 4] << 8) | data[DATA_OFFSET + 5]);

            _data = data;
            _trackNumber = 0;
        }

        public unsafe bool LoadNextTrack(out YARGMidiTrack track)
        {
            track = default;
            if (_trackNumber == NumTracks || _position == _data.Length)
            {
                return false;
            }

            ++_trackNumber;
            if (_position + TAG_SIZE > _data.Length || *(uint*)&_data.Ptr[_position] != TRACK_TAG)
            {
                throw new Exception("Midi Track Tag 'MTrk' mismatch");
            }
            _position += TAG_SIZE;

            if (_position + sizeof(int) > _data.Length)
            {
                throw new EndOfStreamException("End of stream found within midi track");
            }

            int length = (_data[_position] << 24) | (_data[_position + 1] << 16) | (_data[_position + 2] << 8) | _data[_position + 3];
            _position += sizeof(int);
            track = new YARGMidiTrack(_data.Ptr + _position, length);
            return true;
        }

        public IEnumerator<YARGMidiTrack> GetEnumerator()
        {
            return new MidiFileEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct MidiFileEnumerator : IEnumerator<YARGMidiTrack>
        {
            private readonly YARGMidiFile file;
            private YARGMidiTrack _current;
            public MidiFileEnumerator(YARGMidiFile file)
            {
                this.file = file;
                _current = default;
            }

            public readonly YARGMidiTrack Current => _current;

            readonly object IEnumerator.Current => _current;

            public bool MoveNext()
            {
                return file.LoadNextTrack(out _current);
            }

            public readonly void Reset()
            {
                throw new NotImplementedException();
            }

            public readonly void Dispose()
            {
            }
        }
    }
}

using System;
using YARG.Core.Chart.FlatDictionary;

namespace YARG.Core.Chart
{
    public class SyncTrack_FW : IDisposable
    {
        private uint _tickrate;
        private TimedNativeFlatDictionary<Tempo_FW>? _tempoMarkers = new();
        private TimedNativeFlatDictionary<TimeSig_FW>? _timeSigs = new();
        private NativeFlatDictionary<DualPosition, BeatlineType>? _beatMap = new();

        public TimedNativeFlatDictionary<Tempo_FW> TempoMarkers => _tempoMarkers!;
        public TimedNativeFlatDictionary<TimeSig_FW> TimeSigs => _timeSigs!;
        public NativeFlatDictionary<DualPosition, BeatlineType> BeatMap => _beatMap!;

        public SyncTrack_FW() { }

        public uint Tickrate
        {
            get { return _tickrate; }
            set { _tickrate = value; }
        }

        internal const int MICROS_PER_SECOND = 1000000;
        public float ConvertToSeconds(long ticks, int startIndex = 0)
        {
            return ConvertToSeconds(ticks, ref startIndex);
        }

        public float ConvertToSeconds(long ticks, ref int startIndex)
        {
            var span = _tempoMarkers!.Span;
            int length = span.Length;
            for (int i = startIndex; i < length; i++)
            {
                if (i + 1 == length || ticks < span[i + 1].position)
                {
                    ref var marker = ref span[i];
                    startIndex = i;
                    return ((marker.obj.Micros * (ticks - marker.position) / (float) _tickrate) + marker.obj.Anchor) / MICROS_PER_SECOND;
                }
            }
            throw new Exception("dafuq");
        }

        public long ConvertToTicks(float seconds, int startIndex = 0)
        {
            return ConvertToTicks(seconds, ref startIndex);
        }

        public long ConvertToTicks(float seconds, ref int startIndex)
        {
            var span = _tempoMarkers!.Span;
            int length = span.Length;
            float micros = seconds * MICROS_PER_SECOND;
            for (int i = startIndex; i < length; i++)
            {
                if (i + 1 == length || micros < span[i + 1].obj.Anchor)
                {
                    ref var marker = ref span[i];
                    startIndex = i;
                    return (long) ((micros - marker.obj.Anchor) * _tickrate / marker.obj.Micros) + marker.position;
                }
            }
            throw new Exception("dafuq");
        }

        public void Dispose()
        {
            _tempoMarkers!.Dispose();
            _tempoMarkers = null;

            _timeSigs!.Dispose();
            _timeSigs = null;

            _beatMap!.Dispose();
            _beatMap = null;
        }
    }
}

using System;
using System.Collections.Generic;
using YARG.Core.Game;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public struct InstrumentSelection : IEquatable<InstrumentSelection>
    {
        public Instrument Instrument;
        public Difficulty Difficulty;
        public Modifier Modifiers;

        public readonly override int GetHashCode()
        {
            return Instrument.GetHashCode() ^ Difficulty.GetHashCode() ^ Modifiers.GetHashCode();
        }

        public readonly bool Equals(InstrumentSelection other)
        {
            return Instrument == other.Instrument &&
                   Difficulty == other.Difficulty &&
                   Modifiers  == other.Modifiers;
        }
    }

    public class TrackCacheCollection : IDisposable
    {
        private readonly YARGChart _chart;
        private readonly DualTime _endTime;
        private readonly Dictionary<InstrumentSelection, GuitarTrackCache> _guitar;

        public TrackCacheCollection(YARGChart chart)
        {
            _chart = chart;
            _endTime = chart.GetEndTime();
            _guitar = new Dictionary<InstrumentSelection, GuitarTrackCache>();
        }

        public GuitarTrackCache GetFiveFretCache(in InstrumentSelection selection)
        {
            if (!_guitar.TryGetValue(selection, out var guitarCache))
            {
                var instrument = selection.Instrument switch
                {
                    Instrument.FiveFretGuitar => _chart.FiveFretGuitar,
                    Instrument.FiveFretBass => _chart.FiveFretBass,
                    Instrument.FiveFretRhythm => _chart.FiveFretRhythm,
                    Instrument.FiveFretCoopGuitar => _chart.FiveFretCoopGuitar,
                    Instrument.Keys => _chart.Keys,
                    _ => throw new InvalidOperationException("Incompatible instrument for guitar")
                };
                _guitar.Add(selection, guitarCache = GuitarTrackCache.Create(_chart, instrument, in _endTime, in selection));
            }
            return new GuitarTrackCache(guitarCache);
        }

        public void Dispose()
        {
            foreach (var guitarCache in _guitar.Values)
            {
                guitarCache.Dispose();
            }
            _guitar.Clear();
        }
    }
}
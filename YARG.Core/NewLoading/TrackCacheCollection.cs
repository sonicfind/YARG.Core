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

        public GuitarTrackCache GetGuitarTrackCache(in InstrumentSelection selection)
        {
            if (!_guitar.TryGetValue(selection, out var guitarCache))
            {
                guitarCache = selection.Instrument switch
                {
                    Instrument.FiveFretGuitar     => GuitarTrackCache.Create(_chart, _chart.FiveFretGuitar,     in _endTime, in selection),
                    Instrument.FiveFretBass       => GuitarTrackCache.Create(_chart, _chart.FiveFretBass,       in _endTime, in selection),
                    Instrument.FiveFretRhythm     => GuitarTrackCache.Create(_chart, _chart.FiveFretRhythm,     in _endTime, in selection),
                    Instrument.FiveFretCoopGuitar => GuitarTrackCache.Create(_chart, _chart.FiveFretCoopGuitar, in _endTime, in selection),
                    Instrument.Keys               => GuitarTrackCache.Create(_chart, _chart.Keys,               in _endTime, in selection),
                    Instrument.SixFretGuitar      => GuitarTrackCache.Create(_chart, _chart.SixFretGuitar,      in _endTime, in selection),
                    Instrument.SixFretBass        => GuitarTrackCache.Create(_chart, _chart.SixFretBass,        in _endTime, in selection),
                    Instrument.SixFretRhythm      => GuitarTrackCache.Create(_chart, _chart.SixFretRhythm,      in _endTime, in selection),
                    Instrument.SixFretCoopGuitar  => GuitarTrackCache.Create(_chart, _chart.SixFretCoopGuitar,  in _endTime, in selection),
                    _ => throw new InvalidOperationException("Incompatible instrument for guitar")
                };
                _guitar.Add(selection, guitarCache);
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
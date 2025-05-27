using System;
using System.Collections.Generic;
using YARG.Core.NewLoading.Guitar;
using YARG.Core.NewParsing;
using YARG.Core.Song;

namespace YARG.Core.NewLoading
{
    public class TrackCacheCollection : IDisposable
    {
        private readonly YARGChart                                    _chart;
        private readonly DualTime                                     _endTime;
        private readonly Dictionary<InstrumentSelection, GuitarTrack> _guitar;
        private readonly Dictionary<InstrumentSelection, DrumTrack>   _drums;

        public TrackCacheCollection(YARGChart chart)
        {
            _chart = chart;
            _endTime = chart.GetEndTime();
            _guitar = new Dictionary<InstrumentSelection, GuitarTrack>();
            _drums = new Dictionary<InstrumentSelection, DrumTrack>();
        }

        public GuitarTrack GetGuitarTrack(in InstrumentSelection selection)
        {
            if (!_guitar.TryGetValue(selection, out var guitarCache))
            {
                guitarCache = selection.Instrument switch
                {
                    Instrument.FiveFretGuitar     => GuitarTrack.Create(_chart, _chart.FiveFretGuitar,     in _endTime, in selection),
                    Instrument.FiveFretBass       => GuitarTrack.Create(_chart, _chart.FiveFretBass,       in _endTime, in selection),
                    Instrument.FiveFretRhythm     => GuitarTrack.Create(_chart, _chart.FiveFretRhythm,     in _endTime, in selection),
                    Instrument.FiveFretCoopGuitar => GuitarTrack.Create(_chart, _chart.FiveFretCoopGuitar, in _endTime, in selection),
                    Instrument.Keys               => GuitarTrack.Create(_chart, _chart.Keys,               in _endTime, in selection),
                    Instrument.SixFretGuitar      => GuitarTrack.Create(_chart, _chart.SixFretGuitar,      in _endTime, in selection),
                    Instrument.SixFretBass        => GuitarTrack.Create(_chart, _chart.SixFretBass,        in _endTime, in selection),
                    Instrument.SixFretRhythm      => GuitarTrack.Create(_chart, _chart.SixFretRhythm,      in _endTime, in selection),
                    Instrument.SixFretCoopGuitar  => GuitarTrack.Create(_chart, _chart.SixFretCoopGuitar,  in _endTime, in selection),
                    _ => throw new InvalidOperationException("Incompatible instrument for guitar")
                };
                _guitar.Add(selection, guitarCache);
            }
            return guitarCache.Clone();
        }

        public DrumTrack GetDrumTrack(in InstrumentSelection selection)
        {
            if (!_drums.TryGetValue(selection, out var drumCache))
            {
                if (selection.Instrument is Instrument.ProDrums or Instrument.FourLaneDrums)
                {
                    drumCache = _chart.Has(Instrument.FourLaneDrums)
                        ? DrumTrack.Create(_chart, _chart.FourLaneDrums, _endTime, selection)
                        : DrumTrack.Create(_chart, _chart.FiveLaneDrums, _endTime, selection);
                }
                else if (selection.Instrument is Instrument.FiveLaneDrums)
                {
                    drumCache = _chart.Has(Instrument.FiveLaneDrums)
                        ? DrumTrack.Create(_chart, _chart.FiveLaneDrums, _endTime, selection)
                        : DrumTrack.Create(_chart, _chart.FourLaneDrums, _endTime, selection);
                }
                else
                {
                    throw new InvalidOperationException("Incompatible instrument for drums");
                }
                _drums.Add(selection, drumCache);
            }
            return drumCache.Clone();
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

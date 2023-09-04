using System;
using System.Collections.Generic;
using YARG.Core.Chart.FlatDictionary;
using YARG.Core.Chart.Pitch;
using YARG.Core.Chart.ProGuitar;

namespace YARG.Core.Chart
{
    public enum ChordPhrase
    {
        Force_Numbering,
        Slash,
        Hide,
        Accidental_Switch
    };

    public class ProGuitarTrack<TProFretConfig> : InstrumentTrack_Base<ProGuitarDifficulty<TProFretConfig>>, IDisposable
        where TProFretConfig : IProFretConfig, new()
    {
        private bool disposedValue = false;
        private TimedNativeFlatDictionary<PitchName> _roots = new();
        private TimedNativeFlatDictionary<HandPosition<TProFretConfig>> _handPositions = new();
        private TimedFlatDictionary<List<ChordPhrase>> _chordPhrases = new();

        public TimedNativeFlatDictionary<PitchName> Roots => _roots;
        public TimedNativeFlatDictionary<HandPosition<TProFretConfig>> HandPositions => _handPositions;
        public TimedFlatDictionary<List<ChordPhrase>> ChordPhrases => _chordPhrases;

        public override bool IsOccupied()
        {
            return !_roots.IsEmpty() || !_handPositions.IsEmpty() || !_chordPhrases.IsEmpty() || base.IsOccupied();
        }

        public override void Clear()
        {
            base.Clear();
            _roots.Clear();
            _handPositions.Clear();
            _chordPhrases.Clear();
        }

        protected virtual void Dispose(bool disposing)
        {
            foreach (var diff in difficulties)
                if (diff != null)
                    diff.Dispose();

            for (int i = 0; i < difficulties.Length; ++i)
                difficulties[i] = null;

            _roots.Dispose();
            _handPositions.Dispose();
        }

        public void Dispose()
        {
            if (!disposedValue)
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
                disposedValue = true;
            }
        }
    }
}

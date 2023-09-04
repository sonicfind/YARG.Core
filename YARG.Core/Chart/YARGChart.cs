using System;
using System.Collections.Generic;
using YARG.Core.Chart.Drums;
using YARG.Core.Chart.Guitar;
using YARG.Core.Chart.Keys;
using YARG.Core.Chart.ProGuitar;
using YARG.Core.Chart.ProKeys;
using YARG.Core.Song;
using YARG.Core.IO.Ini;

namespace YARG.Core.Chart
{
    public class YARGChart : IDisposable
    {
        private bool disposedValue;
        private SyncTrack_FW _sync = new();
        private SongEvents? _events = new();
        private SongMetadata? _metadata = new();
        private Dictionary<string, IniModifier>? _miscellaneous = new();
        public string MidiSequenceName = string.Empty;

        public SyncTrack_FW Sync => _sync;
        public SongEvents Events => _events!;
        public SongMetadata Metadata => _metadata!;
        public Dictionary<string, IniModifier> Miscellaneous => _miscellaneous!;

        public InstrumentTrack_FW<GuitarNote<FiveFret>>? FiveFretGuitar;
        public InstrumentTrack_FW<GuitarNote<FiveFret>>? FiveFretBass;
        public InstrumentTrack_FW<GuitarNote<FiveFret>>? FiveFretRhythm;
        public InstrumentTrack_FW<GuitarNote<FiveFret>>? FiveFretCoopGuitar;
		
        public InstrumentTrack_FW<KeyNote>? Keys;

        public InstrumentTrack_FW<GuitarNote<SixFret>>?   SixFretGuitar;
        public InstrumentTrack_FW<GuitarNote<SixFret>>?   SixFretBass;
        public InstrumentTrack_FW<GuitarNote<SixFret>>?   SixFretRhythm;
        public InstrumentTrack_FW<GuitarNote<SixFret>>?   SixFretCoopGuitar;

        public InstrumentTrack_FW<DrumNote<DrumPad_4, Basic_Drums>>?    FourLaneDrums;
        public InstrumentTrack_FW<DrumNote<DrumPad_4, Pro_Drums>>?      ProDrums;
        public InstrumentTrack_FW<DrumNote<DrumPad_5, Basic_Drums>>?    FiveLaneDrums;

        // public InstrumentTrack_FW<TrueDrums>? TrueDrums;

        public ProGuitarTrack<ProFret_17>? ProGuitar_17Fret;
        public ProGuitarTrack<ProFret_22>? ProGuitar_22Fret;
        public ProGuitarTrack<ProFret_17>? ProBass_17Fret;
        public ProGuitarTrack<ProFret_22>? ProBass_22Fret;

        public InstrumentTrack_FW<ProKeyNote>? ProKeys;

        // public TBDTrack Dj;

        public VocalTrack_FW? LeadVocals;
        public VocalTrack_FW? HarmonyVocals;

        private static void DisposeTrack<Track>(ref Track? track)
            where Track : class, IDisposable
        {
            if (track != null)
            {
                track.Dispose();
                track = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _sync.Dispose();

                    DisposeTrack(ref FiveFretGuitar);
                    DisposeTrack(ref FiveFretBass);
                    DisposeTrack(ref FiveFretRhythm);
                    DisposeTrack(ref FiveFretCoopGuitar);

                    DisposeTrack(ref SixFretGuitar);
                    DisposeTrack(ref SixFretBass);
                    DisposeTrack(ref SixFretRhythm);
                    DisposeTrack(ref SixFretCoopGuitar);

                    DisposeTrack(ref Keys);

                    DisposeTrack(ref FourLaneDrums);
                    DisposeTrack(ref ProDrums);
                    DisposeTrack(ref FiveLaneDrums);

                    //DisposeTrack(ref TrueDrums);

                    DisposeTrack(ref ProGuitar_17Fret);
                    DisposeTrack(ref ProGuitar_22Fret);
                    DisposeTrack(ref ProBass_17Fret);
                    DisposeTrack(ref ProBass_22Fret);

                    DisposeTrack(ref ProKeys);

                    //DisposeTrack(ref FiveFretGuitar);

                    DisposeTrack(ref LeadVocals);
                    DisposeTrack(ref HarmonyVocals);
                }

                _events = null;
                _metadata = null;
                _miscellaneous = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

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
        public readonly SyncTrack_FW Sync = new();
        public readonly SongEvents Events = new();
        public readonly SongMetadata Metadata = new();
        public readonly Dictionary<string, IniModifier> Miscellaneous = new();
        public string MidiSequenceName = string.Empty;

        public InstrumentTrack_FW<GuitarNote<FiveFret>>? FiveFretGuitar;
        public InstrumentTrack_FW<GuitarNote<FiveFret>>? FiveFretBass;
        public InstrumentTrack_FW<GuitarNote<FiveFret>>? FiveFretRhythm;
        public InstrumentTrack_FW<GuitarNote<FiveFret>>? FiveFretCoopGuitar;
		
        public InstrumentTrack_FW<KeyNote>? Keys;

        public InstrumentTrack_FW<GuitarNote<SixFret>>? SixFretGuitar;
        public InstrumentTrack_FW<GuitarNote<SixFret>>? SixFretBass;
        public InstrumentTrack_FW<GuitarNote<SixFret>>? SixFretRhythm;
        public InstrumentTrack_FW<GuitarNote<SixFret>>? SixFretCoopGuitar;

        public InstrumentTrack_FW<DrumNote<DrumPad_4, Basic_Drums>>? FourLaneDrums;
        public InstrumentTrack_FW<DrumNote<DrumPad_4, Pro_Drums>>?   ProDrums;
        public InstrumentTrack_FW<DrumNote<DrumPad_5, Basic_Drums>>? FiveLaneDrums;

        // public InstrumentTrack_FW<TrueDrums>? TrueDrums;

        public ProGuitarTrack<ProFret_17>? ProGuitar_17Fret;
        public ProGuitarTrack<ProFret_22>? ProGuitar_22Fret;
        public ProGuitarTrack<ProFret_17>? ProBass_17Fret;
        public ProGuitarTrack<ProFret_22>? ProBass_22Fret;

        public InstrumentTrack_Base<ProKeysDifficulty>? ProKeys;

        // public TBDTrack DJ;

        public VocalTrack_FW? LeadVocals;
        public VocalTrack_FW? HarmonyVocals;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Sync.Dispose();

                    FiveFretGuitar?.Dispose();
                    FiveFretBass?.Dispose();
                    FiveFretRhythm?.Dispose();
                    FiveFretCoopGuitar?.Dispose();

                    SixFretGuitar?.Dispose();
                    SixFretBass?.Dispose();
                    SixFretRhythm?.Dispose();
                    SixFretCoopGuitar?.Dispose();

                    Keys?.Dispose();

                    FourLaneDrums?.Dispose();
                    ProDrums?.Dispose();
                    FiveLaneDrums?.Dispose();

                    // TrueDrums?.Dispose();

                    ProGuitar_17Fret?.Dispose();
                    ProGuitar_22Fret?.Dispose();
                    ProBass_17Fret?.Dispose();
                    ProBass_22Fret?.Dispose();

                    ProKeys?.Dispose();

                    // DJ?.Dispose();

                    LeadVocals?.Dispose();
                    HarmonyVocals?.Dispose();
                }

                Events.Clear();
                Miscellaneous.Clear();
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

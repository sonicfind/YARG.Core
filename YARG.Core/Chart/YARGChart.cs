using System.Collections.Generic;
using YARG.Core.Song;
using YARG.Core.IO.Ini;

namespace YARG.Core.Chart
{
    public class YARGChart
    {
        public InstrumentTrack_FW<FiveFret>?  FiveFretGuitar;
        public InstrumentTrack_FW<FiveFret>?  FiveFretBass;
        public InstrumentTrack_FW<FiveFret>?  FiveFretRhythm;
        public InstrumentTrack_FW<FiveFret>?  FiveFretCoopGuitar;
        public InstrumentTrack_FW<Keys>?      Keys;

        public InstrumentTrack_FW<SixFret>?   SixFretGuitar;
        public InstrumentTrack_FW<SixFret>?   SixFretBass;
        public InstrumentTrack_FW<SixFret>?   SixFretRhythm;
        public InstrumentTrack_FW<SixFret>?   SixFretCoopGuitar;

        public InstrumentTrack_FW<Drum_4>?    FourLaneDrums;
        public InstrumentTrack_FW<Drum_4Pro>? ProDrums;
        public InstrumentTrack_FW<Drum_5>?    FiveLaneDrums;

        // public InstrumentTrack_FW<TrueDrums>? TrueDrums;

        public ProGuitarTrack<Fret_17>? ProGuitar_17Fret;
        public ProGuitarTrack<Fret_22>? ProGuitar_22Fret;
        public ProGuitarTrack<Fret_17>? ProBass_17Fret;
        public ProGuitarTrack<Fret_22>? ProBass_22Fret;

        public InstrumentTrack_FW<Keys_Pro>? ProKeys;

        // public TBDTrack Dj;

        public VocalTrack_FW? LeadVocals;
        public VocalTrack_FW? HarmonyVocals;

        public string MidiSequenceName = string.Empty;
        public readonly SyncTrack_FW Sync = new();
        public readonly SongEvents   Events = new();
        public readonly SongMetadata Metadata = new();
        public readonly Dictionary<string, IniModifier> Miscellaneous = new();
    }
}

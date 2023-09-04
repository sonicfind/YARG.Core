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
    public class YARGChart
    {
        public InstrumentTrack_FW<GuitarNote<FiveFret>>?  FiveFretGuitar;
        public InstrumentTrack_FW<GuitarNote<FiveFret>>?  FiveFretBass;
        public InstrumentTrack_FW<GuitarNote<FiveFret>>?  FiveFretRhythm;
        public InstrumentTrack_FW<GuitarNote<FiveFret>>?  FiveFretCoopGuitar;
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

        public string MidiSequenceName = string.Empty;
        public readonly SyncTrack_FW Sync = new();
        public readonly SongEvents   Events = new();
        public readonly SongMetadata Metadata = new();
        public readonly Dictionary<string, IniModifier> Miscellaneous = new();
    }
}

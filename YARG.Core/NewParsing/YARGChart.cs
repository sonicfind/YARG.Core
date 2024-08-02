using System;
using System.Collections.Generic;
using YARG.Core.Chart;
using YARG.Core.IO.Ini;
using YARG.Core.Song;

namespace YARG.Core.NewParsing
{
    public partial class YARGChart : IDisposable
    {
        public readonly SyncTrack2 Sync;
        public readonly TextEvents2 Events = new();
        public readonly YARGNativeSortedList<DualTime, BeatlineType> BeatMap = new();
        public readonly Dictionary<string, IniModifier> Miscellaneous = new();

        public SongMetadata Metadata;
        public LoaderSettings Settings;
        public string MidiSequenceName = string.Empty;

        public InstrumentTrack2<DifficultyTrack2<GuitarNote2<FiveFret>>>? FiveFretGuitar;
        public InstrumentTrack2<DifficultyTrack2<GuitarNote2<FiveFret>>>? FiveFretBass;
        public InstrumentTrack2<DifficultyTrack2<GuitarNote2<FiveFret>>>? FiveFretRhythm;
        public InstrumentTrack2<DifficultyTrack2<GuitarNote2<FiveFret>>>? FiveFretCoopGuitar;
        public InstrumentTrack2<DifficultyTrack2<GuitarNote2<FiveFret>>>? Keys;

        public InstrumentTrack2<DifficultyTrack2<GuitarNote2<SixFret>>>? SixFretGuitar;
        public InstrumentTrack2<DifficultyTrack2<GuitarNote2<SixFret>>>? SixFretBass;
        public InstrumentTrack2<DifficultyTrack2<GuitarNote2<SixFret>>>? SixFretRhythm;
        public InstrumentTrack2<DifficultyTrack2<GuitarNote2<SixFret>>>? SixFretCoopGuitar;

        public InstrumentTrack2<DifficultyTrack2<DrumNote2<FourLane>>>? FourLaneDrums;
        public InstrumentTrack2<DifficultyTrack2<DrumNote2<FiveLane>>>? FiveLaneDrums;

        // public InstrumentTrack2<TrueDrums>? TrueDrums;

        public ProGuitarInstrumentTrack<ProFret_17>? ProGuitar_17Fret;
        public ProGuitarInstrumentTrack<ProFret_22>? ProGuitar_22Fret;
        public ProGuitarInstrumentTrack<ProFret_17>? ProBass_17Fret;
        public ProGuitarInstrumentTrack<ProFret_22>? ProBass_22Fret;

        public InstrumentTrack2<ProKeysDifficultyTrack>? ProKeys;

        // public TBDTrack DJ;

        public VocalTrack2? LeadVocals;
        public VocalTrack2? HarmonyVocals;

        public VenueTrack2? Venue;

        public YARGChart()
        {
            Sync = new SyncTrack2(480);
            Metadata = SongMetadata.Default;
            Settings = LoaderSettings.Default;
        }

        public void Dispose()
        {
            Sync.Dispose();
            Events.Clear();
            BeatMap.Dispose();
            Miscellaneous.Clear();

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
    }
}

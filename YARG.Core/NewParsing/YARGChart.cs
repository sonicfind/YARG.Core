using System;
using System.Collections.Generic;
using YARG.Core.Chart;
using YARG.Core.IO.Ini;
using YARG.Core.Song;

namespace YARG.Core.NewParsing
{
    public partial class YARGChart : IDisposable
    {
        private bool disposedValue;
        public readonly SyncTrack2 Sync;
        public readonly TextEvents2 Events = new();
        public readonly YARGNativeSortedList<DualTime, BeatlineType> BeatMap = new();
        public readonly IniModifierCollection? Miscellaneous;

        public SongMetadata Metadata;
        public LoaderSettings Settings;
        public string MidiSequenceName = string.Empty;

        public BasicInstrumentTrack2<GuitarNote2<FiveFret>>? FiveFretGuitar;
        public BasicInstrumentTrack2<GuitarNote2<FiveFret>>? FiveFretBass;
        public BasicInstrumentTrack2<GuitarNote2<FiveFret>>? FiveFretRhythm;
        public BasicInstrumentTrack2<GuitarNote2<FiveFret>>? FiveFretCoopGuitar;
        public BasicInstrumentTrack2<GuitarNote2<FiveFret>>? Keys;

        public BasicInstrumentTrack2<GuitarNote2<SixFret>>? SixFretGuitar;
        public BasicInstrumentTrack2<GuitarNote2<SixFret>>? SixFretBass;
        public BasicInstrumentTrack2<GuitarNote2<SixFret>>? SixFretRhythm;
        public BasicInstrumentTrack2<GuitarNote2<SixFret>>? SixFretCoopGuitar;

        public BasicInstrumentTrack2<DrumNote2<FourLane>>?    FourLaneDrums;
        public BasicInstrumentTrack2<ProDrumNote2<FourLane>>? ProDrums;
        public BasicInstrumentTrack2<DrumNote2<FiveLane>>?    FiveLaneDrums;

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

        public YARGChart(long tickrate = 480)
        {
            Sync = new SyncTrack2(tickrate);
            Metadata = SongMetadata.Default;
            Settings = LoaderSettings.Default;
            Miscellaneous = new();
        }

        private YARGChart(long tickrate, in SongMetadata metadata, in LoaderSettings settings, IniModifierCollection? modifiers)
        {
            Sync = new SyncTrack2(tickrate);
            Metadata = metadata;
            Settings = settings;
            Miscellaneous = modifiers;
        }

        public void Dispose()
        {
            if (!disposedValue)
            {
                Sync.Dispose();
                Events.Dispose();
                BeatMap.Dispose();
                Miscellaneous?.Clear();

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
                disposedValue = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}

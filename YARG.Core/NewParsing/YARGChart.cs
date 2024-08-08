﻿using System;
using System.Collections.Generic;
using YARG.Core.Chart;
using YARG.Core.IO.Ini;
using YARG.Core.Song;

namespace YARG.Core.NewParsing
{
    public partial class YARGChart : IDisposable
    {
        public readonly SyncTrack2 Sync;
        public readonly YARGManagedSortedList<DualTime, NonNullString> Sections = new();
        public readonly YARGManagedSortedList<DualTime, List<string>> Globals = new();
        public readonly YARGNativeSortedList<DualTime, BeatlineType> BeatMap = new();
        public readonly Dictionary<string, IniModifier> Miscellaneous = new();

        public SongMetadata Metadata;
        public LoaderSettings Settings;
        public string MidiSequenceName = string.Empty;

        public InstrumentTrack2<DifficultyTrack2<FiveFretGuitar>>? FiveFretGuitar;
        public InstrumentTrack2<DifficultyTrack2<FiveFretGuitar>>? FiveFretBass;
        public InstrumentTrack2<DifficultyTrack2<FiveFretGuitar>>? FiveFretRhythm;
        public InstrumentTrack2<DifficultyTrack2<FiveFretGuitar>>? FiveFretCoopGuitar;
        public InstrumentTrack2<DifficultyTrack2<FiveFretGuitar>>? Keys;

        public InstrumentTrack2<DifficultyTrack2<SixFretGuitar>>? SixFretGuitar;
        public InstrumentTrack2<DifficultyTrack2<SixFretGuitar>>? SixFretBass;
        public InstrumentTrack2<DifficultyTrack2<SixFretGuitar>>? SixFretRhythm;
        public InstrumentTrack2<DifficultyTrack2<SixFretGuitar>>? SixFretCoopGuitar;

        public InstrumentTrack2<DifficultyTrack2<FourLaneDrums>>? FourLaneDrums;
        public InstrumentTrack2<DifficultyTrack2<FiveLaneDrums>>? FiveLaneDrums;

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

        private void TrimExcessData()
        {
            Sync.TrimExcessData();
            BeatMap.TrimExcess();
            FiveFretGuitar?.TrimExcess();
            FiveFretBass?.TrimExcess();
            FiveFretRhythm?.TrimExcess();
            FiveFretCoopGuitar?.TrimExcess();

            SixFretGuitar?.TrimExcess();
            SixFretBass?.TrimExcess();
            SixFretRhythm?.TrimExcess();
            SixFretCoopGuitar?.TrimExcess();

            Keys?.TrimExcess();

            FourLaneDrums?.TrimExcess();
            FiveLaneDrums?.TrimExcess();

            // TrueDrums?.TrimExcess();

            ProGuitar_17Fret?.TrimExcess();
            ProGuitar_22Fret?.TrimExcess();
            ProBass_17Fret?.TrimExcess();
            ProBass_22Fret?.TrimExcess();

            ProKeys?.TrimExcess();

            // DJ?.Dispose();

            LeadVocals?.TrimExcess();
            HarmonyVocals?.TrimExcess();
        }

        public void Dispose()
        {
            Sync.Dispose();
            Sections.Clear();
            Globals.Clear();
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

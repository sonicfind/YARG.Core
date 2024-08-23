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
        public readonly IniModifierCollection? Miscellaneous;

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

        public DualTime GetEndTime()
        {
            var globals = Globals.Span;
            for (int i = globals.Length - 1; i >= 0; --i)
            {
                foreach (string ev in globals[i].Value)
                {
                    if (ev == "[end]")
                    {
                        return globals[i].Key;
                    }
                }
            }

            static void Test<TTrack>(TTrack? track, ref DualTime lastNoteTime)
                where TTrack : class, ITrack
            {
                if (track != null)
                {
                    var lastTime = track.GetLastNoteTime();
                    if (lastTime > lastNoteTime)
                        lastNoteTime = lastTime;
                }
            }

            DualTime lastNoteTime = default;
            Test(FiveFretGuitar, ref lastNoteTime);
            Test(FiveFretBass, ref lastNoteTime);
            Test(FiveFretRhythm, ref lastNoteTime);
            Test(FiveFretCoopGuitar, ref lastNoteTime);
            Test(SixFretGuitar, ref lastNoteTime);
            Test(SixFretBass, ref lastNoteTime);
            Test(SixFretRhythm, ref lastNoteTime);
            Test(SixFretCoopGuitar, ref lastNoteTime);

            Test(Keys, ref lastNoteTime);

            Test(FourLaneDrums, ref lastNoteTime);
            Test(FiveLaneDrums, ref lastNoteTime);

            // Test(TrueDrums, ref lastNoteTime);

            Test(ProGuitar_17Fret, ref lastNoteTime);
            Test(ProGuitar_22Fret, ref lastNoteTime);
            Test(ProBass_17Fret, ref lastNoteTime);
            Test(ProBass_22Fret, ref lastNoteTime);

            Test(ProKeys, ref lastNoteTime);

            // Test(Dj, ref lastNoteTime);

            Test(LeadVocals, ref lastNoteTime);
            Test(HarmonyVocals, ref lastNoteTime);
            return lastNoteTime;
        }

        public void Dispose()
        {
            Sync.Dispose();
            Sections.Clear();
            Globals.Clear();
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

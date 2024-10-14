using System;
using System.Collections.Generic;
using YARG.Core.Chart;
using YARG.Core.Containers;
using YARG.Core.IO.Ini;
using YARG.Core.Song;

namespace YARG.Core.NewParsing
{
    public partial class YARGChart : IDisposable
    {
        private readonly long _resolution;
        public long Resolution => _resolution;

        public readonly SyncTrack2 Sync;
        public YARGManagedSortedList<DualTime, NonNullString> Sections = YARGManagedSortedList<DualTime, NonNullString>.Default;
        public YARGManagedSortedList<DualTime, List<string>> Globals = YARGManagedSortedList<DualTime, List<string>>.Default;
        public YARGNativeSortedList<DualTime, BeatlineType> BeatMap = YARGNativeSortedList<DualTime, BeatlineType>.Default;
        public readonly Dictionary<string, IniModifier> Miscellaneous = new();

        public SongMetadata Metadata;
        public LoaderSettings Settings;
        public string MidiSequenceName = string.Empty;

        public readonly InstrumentTrack2<FiveFretGuitar> FiveFretGuitar = new();
        public readonly InstrumentTrack2<FiveFretGuitar> FiveFretBass = new();
        public readonly InstrumentTrack2<FiveFretGuitar> FiveFretRhythm = new();
        public readonly InstrumentTrack2<FiveFretGuitar> FiveFretCoopGuitar = new();
        public readonly InstrumentTrack2<FiveFretGuitar> Keys = new();

        public readonly InstrumentTrack2<SixFretGuitar> SixFretGuitar = new();
        public readonly InstrumentTrack2<SixFretGuitar> SixFretBass = new();
        public readonly InstrumentTrack2<SixFretGuitar> SixFretRhythm = new();
        public readonly InstrumentTrack2<SixFretGuitar> SixFretCoopGuitar = new();

        public readonly InstrumentTrack2<FourLaneDrums> FourLaneDrums = new();
        public readonly InstrumentTrack2<FiveLaneDrums> FiveLaneDrums = new();

        // public readonly InstrumentTrack2<EliteDrums> EliteDrums = new();

        public readonly ProGuitarInstrumentTrack<ProFret_17> ProGuitar_17Fret = new();
        public readonly ProGuitarInstrumentTrack<ProFret_22> ProGuitar_22Fret = new();
        public readonly ProGuitarInstrumentTrack<ProFret_17> ProBass_17Fret = new();
        public readonly ProGuitarInstrumentTrack<ProFret_22> ProBass_22Fret = new();

        public readonly ProKeysInstrumentTrack ProKeys = new();

        // public TBDTrack DJ;

        public readonly LeadVocalsTrack LeadVocals = new();
        public readonly HarmonyVocalsTrack HarmonyVocals = new();

        public readonly VenueTrack2 Venue = new();

        /// <summary>
        /// Constructs an empty chart with default metadata and settings
        /// </summary>
        /// <param name="resolution">The tick rate to initialize the sync track to</param>
        public YARGChart(long resolution = 480)
        {
            _resolution = resolution;
            Sync = new SyncTrack2();
            Metadata = SongMetadata.Default;
            Settings = LoaderSettings.Default;
        }

        /// <summary>
        /// Calculates when the chart should end based on the notes and text events contained within it.
        /// </summary>
        /// <remarks>If the "[end]" global text event is present in the chart, the position of that event will be used.</remarks>
        /// <returns>The end time for the chart</returns>
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

            static void Test<TTrack>(TTrack track, ref DualTime lastNoteTime)
                where TTrack : class, ITrack
            {
                var lastTime = track.GetLastNoteTime();
                if (lastTime > lastNoteTime)
                {
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

        /// <summary>
        /// Disposes of all unmanaged data present in any of the chart's tracks or containers
        /// </summary>
        public void Dispose()
        {
            Sync.Dispose();
            Sections.Clear();
            Globals.Clear();
            BeatMap.Dispose();
            Miscellaneous.Clear();

            FiveFretGuitar.Dispose();
            FiveFretBass.Dispose();
            FiveFretRhythm.Dispose();
            FiveFretCoopGuitar.Dispose();

            SixFretGuitar.Dispose();
            SixFretBass.Dispose();
            SixFretRhythm.Dispose();
            SixFretCoopGuitar.Dispose();

            Keys.Dispose();

            FourLaneDrums.Dispose();
            FiveLaneDrums.Dispose();

            // TrueDrums?.Dispose();

            ProGuitar_17Fret.Dispose();
            ProGuitar_22Fret.Dispose();
            ProBass_17Fret.Dispose();
            ProBass_22Fret.Dispose();

            ProKeys.Dispose();

            // DJ?.Dispose();

            LeadVocals.Dispose();
            HarmonyVocals.Dispose();

            Venue.Dispose();
        }
    }
}

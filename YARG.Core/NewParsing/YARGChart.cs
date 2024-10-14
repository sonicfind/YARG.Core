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

            var lastNoteTime = default(DualTime);
            FiveFretGuitar.UpdateLastNoteTime(ref lastNoteTime);
            FiveFretBass.UpdateLastNoteTime(ref lastNoteTime);
            FiveFretRhythm.UpdateLastNoteTime(ref lastNoteTime);
            FiveFretCoopGuitar.UpdateLastNoteTime(ref lastNoteTime);
            SixFretGuitar.UpdateLastNoteTime(ref lastNoteTime);
            SixFretBass.UpdateLastNoteTime(ref lastNoteTime);
            SixFretRhythm.UpdateLastNoteTime(ref lastNoteTime);
            SixFretCoopGuitar.UpdateLastNoteTime(ref lastNoteTime);

            Keys.UpdateLastNoteTime(ref lastNoteTime);

            FourLaneDrums.UpdateLastNoteTime(ref lastNoteTime);
            FiveLaneDrums.UpdateLastNoteTime(ref lastNoteTime);

            // TrueDrums.UpdateLastNoteTime(ref lastNoteTime);

            ProGuitar_17Fret.UpdateLastNoteTime(ref lastNoteTime);
            ProGuitar_22Fret.UpdateLastNoteTime(ref lastNoteTime);
            ProBass_17Fret.UpdateLastNoteTime(ref lastNoteTime);
            ProBass_22Fret.UpdateLastNoteTime(ref lastNoteTime);

            ProKeys.UpdateLastNoteTime(ref lastNoteTime);

            // Dj.UpdateLastNoteTime(ref lastNoteTime);

            LeadVocals.UpdateLastNoteTime(ref lastNoteTime);
            HarmonyVocals.UpdateLastNoteTime(ref lastNoteTime);
            return lastNoteTime;
        }

        /// <summary>
        /// Disposes of all unmanaged data present in any of the chart's tracks or containers
        /// </summary>
        private void _Dispose(bool dispose)
        {
            if (dispose)
            {
                Globals.Dispose();
                Sections.Dispose();
                Miscellaneous.Clear();
            }

            Sync.Dispose();
            BeatMap.Dispose();

            FiveFretGuitar.Dispose(dispose);
            FiveFretBass.Dispose(dispose);
            FiveFretRhythm.Dispose(dispose);
            FiveFretCoopGuitar.Dispose(dispose);

            SixFretGuitar.Dispose(dispose);
            SixFretBass.Dispose(dispose);
            SixFretRhythm.Dispose(dispose);
            SixFretCoopGuitar.Dispose(dispose);

            Keys.Dispose(dispose);

            FourLaneDrums.Dispose(dispose);
            FiveLaneDrums.Dispose(dispose);

            // TrueDrums.Dispose(dispose);

            ProGuitar_17Fret.Dispose(dispose);
            ProGuitar_22Fret.Dispose(dispose);
            ProBass_17Fret.Dispose(dispose);
            ProBass_22Fret.Dispose(dispose);

            ProKeys.Dispose(dispose);

            // DJ.Dispose(dispose);

            LeadVocals.Dispose(dispose);
            HarmonyVocals.Dispose(dispose);

            Venue.Dispose();
        }

        public void Dispose()
        {
            _Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~YARGChart()
        {
            _Dispose(false);
        }
    }
}

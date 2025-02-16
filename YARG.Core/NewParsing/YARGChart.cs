﻿using System;
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

        public SyncTrack2 Sync { get; } = new();
        public YARGManagedSortedList<DualTime, NonNullString> Sections { get; } = new();
        public YARGManagedSortedList<DualTime, List<string>> Globals { get; } = new();
        public YARGNativeSortedList<DualTime, BeatlineType> BeatMap { get; } = new();
        public VenueTrack2 Venue { get; } = new();

        public SongMetadata Metadata;
        public LoaderSettings Settings;
        public string MidiSequenceName = string.Empty;
        public IniModifierCollection? Miscellaneous { get; }

        private InstrumentTrack2<FiveFretGuitar>? _fiveFretGuitar;
        private InstrumentTrack2<FiveFretGuitar>? _fiveFretBass;
        private InstrumentTrack2<FiveFretGuitar>? _fiveFretRhythm;
        private InstrumentTrack2<FiveFretGuitar>? _fiveFretCoopGuitar;
        private InstrumentTrack2<FiveFretGuitar>? _keys;
        
        private InstrumentTrack2<SixFretGuitar>? _sixFretGuitar;
        private InstrumentTrack2<SixFretGuitar>? _sixFretBass;
        private InstrumentTrack2<SixFretGuitar>? _sixFretRhythm;
        private InstrumentTrack2<SixFretGuitar>? _sixFretCoopGuitar;
        
        private InstrumentTrack2<FourLaneDrums>? _fourLaneDrums;
        private InstrumentTrack2<FiveLaneDrums>? _fiveLaneDrums;

        // private InstrumentTrack2<EliteDrums>? _eliteDrums;

        private ProGuitarInstrumentTrack<ProFret_17>? _proGuitar_17Fret;
        private ProGuitarInstrumentTrack<ProFret_22>? _proGuitar_22Fret;
        private ProGuitarInstrumentTrack<ProFret_17>? _proBass_17Fret;
        private ProGuitarInstrumentTrack<ProFret_22>? _proBass_22Fret;

        private ProKeysInstrumentTrack? _proKeys;

        private VocalsTrack2? _leadVocals;
        private VocalsTrack2? _harmonyVocals;

        public InstrumentTrack2<FiveFretGuitar> FiveFretGuitar
        {
            get => _fiveFretGuitar ??= new();
            private set => _fiveFretGuitar = value;
        }

        public InstrumentTrack2<FiveFretGuitar> FiveFretBass
        {
            get => _fiveFretBass ??= new();
            private set => _fiveFretBass = value;
        }

        public InstrumentTrack2<FiveFretGuitar> FiveFretRhythm
        {
            get => _fiveFretRhythm ??= new();
            private set => _fiveFretRhythm = value;
        }

        public InstrumentTrack2<FiveFretGuitar> FiveFretCoopGuitar
        {
            get => _fiveFretCoopGuitar ??= new();
            private set => _fiveFretCoopGuitar = value;
        }

        public InstrumentTrack2<FiveFretGuitar> Keys
        {
            get => _keys ??= new();
            private set => _keys = value;
        }

        public InstrumentTrack2<SixFretGuitar> SixFretGuitar
        {
            get => _sixFretGuitar ??= new();
            private set => _sixFretGuitar = value;
        }

        public InstrumentTrack2<SixFretGuitar> SixFretBass
        {
            get => _sixFretBass ??= new();
            private set => _sixFretBass = value;
        }

        public InstrumentTrack2<SixFretGuitar> SixFretRhythm
        {
            get => _sixFretRhythm ??= new();
            private set => _sixFretRhythm = value;
        }

        public InstrumentTrack2<SixFretGuitar> SixFretCoopGuitar
        {
            get => _sixFretCoopGuitar ??= new();
            private set => _sixFretCoopGuitar = value;
        }

        public InstrumentTrack2<FourLaneDrums> FourLaneDrums
        {
            get => _fourLaneDrums ??= new();
            private set => _fourLaneDrums = value;
        }

        public InstrumentTrack2<FiveLaneDrums> FiveLaneDrums
        {
            get => _fiveLaneDrums ??= new();
            private set => _fiveLaneDrums = value;
        }

        // public readonly InstrumentTrack2<EliteDrums> EliteDrums
        // {
        //    get => _eliteDrums ??= new();
        //    private set => _eliteDrums = value;
        // }

        public ProGuitarInstrumentTrack<ProFret_17> ProGuitar_17Fret
        {
            get => _proGuitar_17Fret ??= new();
            private set => _proGuitar_17Fret = value;
        }

        public ProGuitarInstrumentTrack<ProFret_22> ProGuitar_22Fret
        {
            get => _proGuitar_22Fret ??= new();
            private set => _proGuitar_22Fret = value;
        }

        public ProGuitarInstrumentTrack<ProFret_17> ProBass_17Fret
        {
            get => _proBass_17Fret ??= new();
            private set => _proBass_17Fret = value;
        }

        public ProGuitarInstrumentTrack<ProFret_22> ProBass_22Fret
        {
            get => _proBass_22Fret ??= new();
            private set => _proBass_22Fret = value;
        }

        public ProKeysInstrumentTrack ProKeys
        {
            get => _proKeys ??= new();
            private set => _proKeys = value;
        }

        public VocalsTrack2 LeadVocals
        {
            get => _leadVocals ??= new(1);
            private set => _leadVocals = value;
        }

        public VocalsTrack2 HarmonyVocals
        {
            get => _harmonyVocals ??= new(3);
            private set => _harmonyVocals = value;
        }

        /// <summary>
        /// Constructs an empty chart with default metadata and settings
        /// </summary>
        /// <param name="resolution">The tick rate to initialize the sync track to</param>
        public YARGChart(long resolution = 480)
        {
            _resolution = resolution;
            Metadata = SongMetadata.Default;
            Settings = LoaderSettings.Default;
            Miscellaneous = new();
        }

        private YARGChart(long resolution, in SongMetadata metadata, in LoaderSettings settings, IniModifierCollection? modifiers)
        {
            _resolution = resolution;
            Metadata = metadata;
            Settings = settings;
            Miscellaneous = modifiers;
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
            _fiveFretGuitar?.UpdateLastNoteTime(ref lastNoteTime);
            _fiveFretBass?.UpdateLastNoteTime(ref lastNoteTime);
            _fiveFretRhythm?.UpdateLastNoteTime(ref lastNoteTime);
            _fiveFretCoopGuitar?.UpdateLastNoteTime(ref lastNoteTime);
            _keys?.UpdateLastNoteTime(ref lastNoteTime);

            _sixFretGuitar?.UpdateLastNoteTime(ref lastNoteTime);
            _sixFretBass?.UpdateLastNoteTime(ref lastNoteTime);
            _sixFretRhythm?.UpdateLastNoteTime(ref lastNoteTime);
            _sixFretCoopGuitar?.UpdateLastNoteTime(ref lastNoteTime);

            _fourLaneDrums?.UpdateLastNoteTime(ref lastNoteTime);
            _fiveLaneDrums?.UpdateLastNoteTime(ref lastNoteTime);

            // _eliteDrums?.UpdateLastNoteTime(ref lastNoteTime);

            _proGuitar_17Fret?.UpdateLastNoteTime(ref lastNoteTime);
            _proGuitar_22Fret?.UpdateLastNoteTime(ref lastNoteTime);
            _proBass_17Fret?.UpdateLastNoteTime(ref lastNoteTime);
            _proBass_22Fret?.UpdateLastNoteTime(ref lastNoteTime);

            _proKeys?.UpdateLastNoteTime(ref lastNoteTime);

            _leadVocals?.UpdateLastNoteTime(ref lastNoteTime);
            _harmonyVocals?.UpdateLastNoteTime(ref lastNoteTime);
            return lastNoteTime;
        }

        /// <summary>
        /// Disposes of all unmanaged data present in any of the chart's tracks or containers
        /// </summary>
        public void Dispose()
        {
            Globals.Dispose();
            Sections.Dispose();
            Miscellaneous?.Clear();
            Sync.Dispose();
            BeatMap.Dispose();
            Venue.Dispose();

            _fiveFretGuitar?.Dispose();
            _fiveFretBass?.Dispose();
            _fiveFretRhythm?.Dispose();
            _fiveFretCoopGuitar?.Dispose();
            _keys?.Dispose();

            _sixFretGuitar?.Dispose();
            _sixFretBass?.Dispose();
            _sixFretRhythm?.Dispose();
            _sixFretCoopGuitar?.Dispose();

            _fourLaneDrums?.Dispose();
            _fiveLaneDrums?.Dispose();

            // _eliteDrums?.Dispose();

            _proGuitar_17Fret?.Dispose();
            _proGuitar_22Fret?.Dispose();
            _proBass_17Fret?.Dispose();
            _proBass_22Fret?.Dispose();

            _proKeys?.Dispose();

            _leadVocals?.Dispose();
            _harmonyVocals?.Dispose();
        }
    }
}

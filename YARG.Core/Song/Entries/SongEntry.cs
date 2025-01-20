using System;
using System.IO;
using YARG.Core.Chart;
using YARG.Core.Extensions;
using YARG.Core.Song.Cache;

namespace YARG.Core.Song
{
    public enum ScanResult
    {
        Success,
        DirectoryError,
        DuplicateFilesFound,
        IniEntryCorruption,
        IniNotDownloaded,
        ChartNotDownloaded,
        NoName,
        NoNotes,
        DTAError,
        MoggError,
        UnsupportedEncryption,
        MissingCONMidi,
        PossibleCorruption,
        FailedSngLoad,

        InvalidResolution,
        InvalidResolution_Update,
        InvalidResolution_Upgrade,

        NoAudio,
        PathTooLong,
        MultipleMidiTrackNames,
        MultipleMidiTrackNames_Update,
        MultipleMidiTrackNames_Upgrade,

        LooseChart_Warning,
    }

    /// <summary>
    /// The type of chart file to read.
    /// </summary>
    public enum ChartFormat
    {
        Mid,
        Midi,
        Chart,
    };

    public enum EntryType
    {
        Ini,
        Sng,
        ExCON,
        CON,
    }

    public struct LoaderSettings
    {
        public static readonly LoaderSettings Default = new()
        {
            HopoThreshold = -1,
            SustainCutoffThreshold = -1,
            OverdiveMidiNote = 116
        };

        public long HopoThreshold;
        public long SustainCutoffThreshold;
        public int OverdiveMidiNote;
    }

    /// <summary>
    /// The metadata for a song.
    /// </summary>
    /// <remarks>
    /// This class is intended to hold all metadata for all songs, whether it be displayed in the song list or used for
    /// parsing/loading of the song.
    /// <br/>
    /// Display/common metadata should be added directly to this class. Metadata only used in a specific file type
    /// should not be handled through inheritance, make a separate class for that data instead and add it as a field to
    /// this one.
    /// <br/>
    /// Instances of this class should not be created directly (except for things like a chart editor), instead they
    /// should be created through static methods which parse in a metadata file of a specific type and return an
    /// instance.
    /// </remarks>
    [Serializable]
    public abstract partial class SongEntry
    {
        protected static readonly string[] BACKGROUND_FILENAMES =
        {
            "bg", "background", "video"
        };

        protected static readonly string[] VIDEO_EXTENSIONS =
        {
            ".mp4", ".mov", ".webm",
        };

        protected static readonly string[] IMAGE_EXTENSIONS =
        {
            ".png", ".jpg", ".jpeg", ".tga", ".bmp", ".psd", ".gif", ".pic"
        };

        protected static readonly string YARGROUND_EXTENSION = ".yarground";
        protected static readonly string YARGROUND_FULLNAME = "bg.yarground";
        protected static readonly Random BACKROUND_RNG = new();

        public const int MINIMUM_YEAR_DIGITS = 4;

        private string _nameSearchable = string.Empty;
        private string _artistSearchable = string.Empty;
        private string _albumSearchable = string.Empty;
        private string _genreSearchable = string.Empty;
        private string _charterSearchable = string.Empty;
        private string _sourceSearchable = string.Empty;
        private string _playlistSearchable = string.Empty;

        protected SongMetadata _metadata = SongMetadata.Default;
        protected AvailableParts _parts = AvailableParts.Default;
        protected HashWrapper _hash = default;
        protected LoaderSettings _settings = LoaderSettings.Default;
        protected string _parsedYear = string.Empty;
        protected int _yearAsNumber = int.MaxValue;

        public abstract EntryType SubType { get; }
        public abstract string SortBasedLocation { get; }
        public abstract string ActualLocation { get; }
        public abstract DateTime LastWriteTime { get; }

        public string Name => _metadata.Name;
        public string Artist => _metadata.Artist;
        public string Album => _metadata.Album;
        public string Genre => _metadata.Genre;
        public string Charter => _metadata.Charter;
        public string Source => _metadata.Source;
        public string Playlist => _metadata.Playlist;

        public string NameSearchable => _nameSearchable;
        public string ArtistSearchable => _artistSearchable;
        public string AlbumSearchable => _albumSearchable;
        public string GenreSearchable => _genreSearchable;
        public string CharterSearchable => _charterSearchable;
        public string SourceSearchable => _sourceSearchable;
        public string PlaylistSearchable => _playlistSearchable;

        public string UnmodifiedYear => _metadata.Year;
        public string ParsedYear => _parsedYear;
        public int YearAsNumber => _yearAsNumber;

        public bool IsMaster => _metadata.IsMaster;
        public bool VideoLoop => _metadata.VideoLoop;

        public int AlbumTrack => _metadata.AlbumTrack;

        public int PlaylistTrack => _metadata.PlaylistTrack;

        public string LoadingPhrase => _metadata.LoadingPhrase;
        
        public string CreditWrittenBy => _metadata.CreditWrittenBy;
        
        public string CreditPerformedBy => _metadata.CreditPerformedBy;
        
        public string CreditCourtesyOf => _metadata.CreditCourtesyOf;
        
        public string CreditAlbumCover => _metadata.CreditAlbumCover;
        
        public string CreditLicense => _metadata.CreditLicense;

        public long SongLengthMilliseconds => _metadata.SongLength;

        public long SongOffsetMilliseconds => _metadata.SongOffset;

        public long PreviewStartMilliseconds => _metadata.Preview.Start;

        public long PreviewEndMilliseconds => _metadata.Preview.End;

        public long VideoStartTimeMilliseconds => _metadata.Video.Start;

        public long VideoEndTimeMilliseconds => _metadata.Video.End;

        public double SongLengthSeconds => SongLengthMilliseconds / SongMetadata.MILLISECOND_FACTOR;

        public double SongOffsetSeconds => SongOffsetMilliseconds / SongMetadata.MILLISECOND_FACTOR;

        public double PreviewStartSeconds => PreviewStartMilliseconds / SongMetadata.MILLISECOND_FACTOR;

        public double PreviewEndSeconds => PreviewEndMilliseconds / SongMetadata.MILLISECOND_FACTOR;

        public double VideoStartTimeSeconds => VideoStartTimeMilliseconds / SongMetadata.MILLISECOND_FACTOR;

        public double VideoEndTimeSeconds => VideoEndTimeMilliseconds >= 0 ? VideoEndTimeMilliseconds / SongMetadata.MILLISECOND_FACTOR : -1;

        public int VocalsCount
        {
            get
            {
                if (_parts.HarmonyVocals[2])
                {
                    return 3;
                }

                if (_parts.HarmonyVocals[1])
                {
                    return 2;
                }
                return _parts.HarmonyVocals[0] || _parts.LeadVocals[0] ? 1 : 0;
            }
        }

        public sbyte BandDifficulty => _parts.BandDifficulty.Intensity;

        public override string ToString() { return Artist + " | " + Name; }

        public PartValues this[Instrument instrument]
        {
            get
            {
                return instrument switch
                {
                    Instrument.FiveFretGuitar => _parts.FiveFretGuitar,
                    Instrument.FiveFretBass => _parts.FiveFretBass,
                    Instrument.FiveFretRhythm => _parts.FiveFretRhythm,
                    Instrument.FiveFretCoopGuitar => _parts.FiveFretCoopGuitar,
                    Instrument.Keys => _parts.Keys,

                    Instrument.SixFretGuitar => _parts.SixFretGuitar,
                    Instrument.SixFretBass => _parts.SixFretBass,
                    Instrument.SixFretRhythm => _parts.SixFretRhythm,
                    Instrument.SixFretCoopGuitar => _parts.SixFretCoopGuitar,

                    Instrument.FourLaneDrums => _parts.FourLaneDrums,
                    Instrument.FiveLaneDrums => _parts.FiveLaneDrums,
                    Instrument.ProDrums => _parts.ProDrums,

                    Instrument.EliteDrums => _parts.EliteDrums,

                    Instrument.ProGuitar_17Fret => _parts.ProGuitar_17Fret,
                    Instrument.ProGuitar_22Fret => _parts.ProGuitar_22Fret,
                    Instrument.ProBass_17Fret => _parts.ProBass_17Fret,
                    Instrument.ProBass_22Fret => _parts.ProBass_22Fret,

                    Instrument.ProKeys => _parts.ProKeys,

                    // Instrument.Dj => DJ,

                    Instrument.Vocals => _parts.LeadVocals,
                    Instrument.Harmony => _parts.HarmonyVocals,
                    Instrument.Band => _parts.BandDifficulty,

                    _ => throw new NotImplementedException($"Unhandled instrument {instrument}!")
                };
            }
        }

        public bool HasInstrument(Instrument instrument)
        {
            return instrument switch
            {
                Instrument.FiveFretGuitar => _parts.FiveFretGuitar.IsActive(),
                Instrument.FiveFretBass => _parts.FiveFretBass.IsActive(),
                Instrument.FiveFretRhythm => _parts.FiveFretRhythm.IsActive(),
                Instrument.FiveFretCoopGuitar => _parts.FiveFretCoopGuitar.IsActive(),
                Instrument.Keys => _parts.Keys.IsActive(),

                Instrument.SixFretGuitar => _parts.SixFretGuitar.IsActive(),
                Instrument.SixFretBass => _parts.SixFretBass.IsActive(),
                Instrument.SixFretRhythm => _parts.SixFretRhythm.IsActive(),
                Instrument.SixFretCoopGuitar => _parts.SixFretCoopGuitar.IsActive(),

                Instrument.FourLaneDrums => _parts.FourLaneDrums.IsActive(),
                Instrument.FiveLaneDrums => _parts.FiveLaneDrums.IsActive(),
                Instrument.ProDrums => _parts.ProDrums.IsActive(),

                Instrument.EliteDrums => _parts.EliteDrums.IsActive(),

                Instrument.ProGuitar_17Fret => _parts.ProGuitar_17Fret.IsActive(),
                Instrument.ProGuitar_22Fret => _parts.ProGuitar_22Fret.IsActive(),
                Instrument.ProBass_17Fret => _parts.ProBass_17Fret.IsActive(),
                Instrument.ProBass_22Fret => _parts.ProBass_22Fret.IsActive(),

                Instrument.ProKeys => _parts.ProKeys.IsActive(),

                Instrument.Vocals => _parts.LeadVocals.IsActive(),
                Instrument.Harmony => _parts.HarmonyVocals.IsActive(),
                Instrument.Band => _parts.BandDifficulty.IsActive(),

                _ => false
            };
        }

        protected SongEntry() { }

        protected void Deserialize(UnmanagedMemoryStream stream, CategoryCacheStrings strings)
        {
            _hash = HashWrapper.Deserialize(stream);
            unsafe
            {
                _parts = *(AvailableParts*) stream.PositionPointer;
                stream.Position += sizeof(AvailableParts);
            }

            _metadata.Name =     strings.titles   [stream.Read<int>(Endianness.Little)];
            _metadata.Artist =   strings.artists  [stream.Read<int>(Endianness.Little)];
            _metadata.Album =    strings.albums   [stream.Read<int>(Endianness.Little)];
            _metadata.Genre =    strings.genres   [stream.Read<int>(Endianness.Little)];
            _metadata.Year =     strings.years    [stream.Read<int>(Endianness.Little)];
            _metadata.Charter =  strings.charters [stream.Read<int>(Endianness.Little)];
            _metadata.Playlist = strings.playlists[stream.Read<int>(Endianness.Little)];
            _metadata.Source =   strings.sources  [stream.Read<int>(Endianness.Little)];

            _metadata.IsMaster =  stream.ReadBoolean();
            _metadata.VideoLoop = stream.ReadBoolean();

            _metadata.AlbumTrack =    stream.Read<int>(Endianness.Little);
            _metadata.PlaylistTrack = stream.Read<int>(Endianness.Little);

            _metadata.SongLength = stream.Read<long>(Endianness.Little);
            _metadata.SongOffset = stream.Read<long>(Endianness.Little);
            _metadata.SongRating = stream.Read<uint>(Endianness.Little);

            _metadata.Preview.Start = stream.Read<long>(Endianness.Little);
            _metadata.Preview.End   = stream.Read<long>(Endianness.Little);

            _metadata.Video.Start = stream.Read<long>(Endianness.Little);
            _metadata.Video.End = stream.Read<long>(Endianness.Little);

            _metadata.LoadingPhrase = stream.ReadString();

            _metadata.CreditWrittenBy = stream.ReadString();
            _metadata.CreditPerformedBy = stream.ReadString();
            _metadata.CreditCourtesyOf = stream.ReadString();
            _metadata.CreditAlbumCover = stream.ReadString();
            _metadata.CreditLicense = stream.ReadString();

            _settings.HopoThreshold = stream.Read<long>(Endianness.Little);
            _settings.SustainCutoffThreshold = stream.Read<long>(Endianness.Little);
            _settings.OverdiveMidiNote = stream.Read<int>(Endianness.Little);

            SetSearchStrings();
        }

        protected void SetSearchStrings()
        {
            _nameSearchable = SortString.ConvertToSearchable(_metadata.Name);
            _artistSearchable = SortString.ConvertToSearchable(_metadata.Artist);
            _albumSearchable = SortString.ConvertToSearchable(_metadata.Album);
            _genreSearchable = SortString.ConvertToSearchable(_metadata.Genre);
            _charterSearchable = SortString.ConvertToSearchable(_metadata.Charter);
            _sourceSearchable = SortString.ConvertToSearchable(_metadata.Source);
            _playlistSearchable = SortString.ConvertToSearchable(_metadata.Playlist);
        }

        public virtual void Serialize(MemoryStream stream, CacheWriteIndices node)
        {
            _hash.Serialize(stream);
            unsafe
            {
                fixed (AvailableParts* ptr = &_parts)
                {
                    stream.Write(new Span<byte>(ptr, sizeof(AvailableParts)));
                }
            }

            stream.Write(node.title,    Endianness.Little);
            stream.Write(node.artist,   Endianness.Little);
            stream.Write(node.album,    Endianness.Little);
            stream.Write(node.genre,    Endianness.Little);
            stream.Write(node.year,     Endianness.Little);
            stream.Write(node.charter,  Endianness.Little);
            stream.Write(node.playlist, Endianness.Little);
            stream.Write(node.source,   Endianness.Little);

            stream.Write(_metadata.IsMaster);
            stream.Write(_metadata.VideoLoop);

            stream.Write(_metadata.AlbumTrack,    Endianness.Little);
            stream.Write(_metadata.PlaylistTrack, Endianness.Little);

            stream.Write(_metadata.SongLength, Endianness.Little);
            stream.Write(_metadata.SongOffset, Endianness.Little);
            stream.Write(_metadata.SongRating, Endianness.Little);

            stream.Write(_metadata.Preview.Start, Endianness.Little);
            stream.Write(_metadata.Preview.End,   Endianness.Little);

            stream.Write(_metadata.Video.Start, Endianness.Little);
            stream.Write(_metadata.Video.End,   Endianness.Little);

            stream.Write(_metadata.LoadingPhrase);

            stream.Write(_metadata.CreditWrittenBy);
            stream.Write(_metadata.CreditPerformedBy);
            stream.Write(_metadata.CreditCourtesyOf);
            stream.Write(_metadata.CreditAlbumCover);
            stream.Write(_metadata.CreditLicense);

            stream.Write(_settings.HopoThreshold, Endianness.Little);
            stream.Write(_settings.SustainCutoffThreshold, Endianness.Little);
            stream.Write(_settings.OverdiveMidiNote, Endianness.Little);
        }
    }
}
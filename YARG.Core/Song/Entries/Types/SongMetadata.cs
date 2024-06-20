using System.IO;
using System.Text.RegularExpressions;
using YARG.Core.Chart;
using YARG.Core.IO;
using YARG.Core.IO.Ini;
using YARG.Core.Song.Cache;

namespace YARG.Core.Song
{
    public struct SongMetadata
    {
        public const double MILLISECOND_FACTOR = 1000.0;
        public static readonly SortString DEFAULT_NAME = "Unknown Name";
        public static readonly SortString DEFAULT_ARTIST = "Unknown Artist";
        public static readonly SortString DEFAULT_ALBUM = "Unknown Album";
        public static readonly SortString DEFAULT_GENRE = "Unknown Genre";
        public static readonly SortString DEFAULT_CHARTER = "Unknown Charter";
        public static readonly SortString DEFAULT_SOURCE = "Unknown Source";
        public const string DEFAULT_YEAR = "####";

        public static readonly SongMetadata Default = new()
        {
            Name = SortString.Empty,
            Artist = DEFAULT_ARTIST,
            Album = DEFAULT_ALBUM,
            Genre = DEFAULT_GENRE,
            Charter = DEFAULT_CHARTER,
            Source = DEFAULT_SOURCE,
            Playlist = SortString.Empty,
            IsMaster = true,
            AlbumTrack = 0,
            PlaylistTrack = 0,
            LoadingPhrase = string.Empty,
            Year = DEFAULT_YEAR,
            SongLength = 0,
            SongOffset = 0,
            PreviewStart = -1,
            PreviewEnd = -1,
            VideoStartTime = 0,
            VideoEndTime = -1,
        };

        public SortString Name;
        public SortString Artist;
        public SortString Album;
        public SortString Genre;
        public SortString Charter;
        public SortString Source;
        public SortString Playlist;

        public string Year;

        public ulong SongLength;
        public long SongOffset;
        public uint SongRating;  // 1 = FF; 2 = SR; 3 = M; 4 = NR

        public long PreviewStart;
        public long PreviewEnd;

        public long VideoStartTime;
        public long VideoEndTime;

        public bool IsMaster;

        public int AlbumTrack;
        public int PlaylistTrack;

        public string LoadingPhrase;

        public SongMetadata(IniSection modifiers, string defaultPlaylist)
        {
            modifiers.TryGet("name", out Name, DEFAULT_NAME);
            modifiers.TryGet("artist", out Artist, DEFAULT_ARTIST);
            modifiers.TryGet("album", out Album, DEFAULT_ALBUM);
            modifiers.TryGet("genre", out Genre, DEFAULT_GENRE);

            if (!modifiers.TryGet("year", out Year))
            {
                if (modifiers.TryGet("Year", out Year))
                {
                    if (Year.StartsWith(", "))
                    {
                        Year = Year[2..];
                    }
                    else if (Year.StartsWith(','))
                    {
                        Year = Year[1..];
                    }
                }
                else
                {
                    Year = DEFAULT_YEAR;
                }
            }

            if (!modifiers.TryGet("charter", out Charter, DEFAULT_CHARTER))
            {
                modifiers.TryGet("frets", out Charter, DEFAULT_CHARTER);
            }

            modifiers.TryGet("icon", out Source, DEFAULT_SOURCE);
            modifiers.TryGet("playlist", out Playlist, defaultPlaylist);

            modifiers.TryGet("loading_phrase", out LoadingPhrase);

            if (!modifiers.TryGet("playlist_track", out PlaylistTrack))
            {
                PlaylistTrack = -1;
            }

            if (!modifiers.TryGet("album_track", out AlbumTrack))
            {
                AlbumTrack = -1;
            }

            modifiers.TryGet("song_length", out SongLength);
            modifiers.TryGet("rating", out SongRating);

            modifiers.TryGet("video_start_time", out VideoStartTime);
            if (!modifiers.TryGet("video_end_time", out VideoEndTime))
            {
                VideoEndTime = -1;
            }

            if (!modifiers.TryGet("preview", out PreviewStart, out PreviewEnd))
            {
                if (!modifiers.TryGet("preview_start_time", out PreviewStart))
                {
                    // Capitlization = from .chart
                    if (modifiers.TryGet("PreviewStart", out double previewStartSeconds))
                    {
                        PreviewStart = (long) (previewStartSeconds * MILLISECOND_FACTOR);
                    }
                    else
                    {
                        PreviewStart = -1;
                    }
                }

                if (!modifiers.TryGet("preview_end_time", out PreviewEnd))
                {
                    // Capitlization = from .chart
                    if (modifiers.TryGet("PreviewEnd", out double previewEndSeconds))
                    {
                        PreviewEnd = (long) (previewEndSeconds * MILLISECOND_FACTOR);
                    }
                    else
                    {
                        PreviewEnd = -1;
                    }
                }
            }

            if (!modifiers.TryGet("delay", out SongOffset) || SongOffset == 0)
            {
                if (modifiers.TryGet("Offset", out double songOffsetSeconds))
                {
                    SongOffset = (long) (songOffsetSeconds * MILLISECOND_FACTOR);
                }
            }

            IsMaster = !modifiers.TryGet("tags", out string tag) || tag.ToLower() != "cover";
        }
    }
}

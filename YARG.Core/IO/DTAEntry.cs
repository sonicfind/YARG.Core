using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YARG.Core.Extensions;
using YARG.Core.Logging;
using YARG.Core.Song;

namespace YARG.Core.IO
{
    public class DTAEntry
    {
        public string? Name;
        public string? Artist;
        public string? Album;
        public string? Genre;
        public string? Charter;
        public string? Source;
        public string? Playlist;
        public int? YearAsNumber;

        public ulong? SongLength;
        public uint? SongRating;  // 1 = FF; 2 = SR; 3 = M; 4 = NR

        public long? PreviewStart;
        public long? PreviewEnd;
        public bool? IsMaster;

        public int? AlbumTrack;

        public string? SongID;
        public uint? AnimTempo;
        public string? DrumBank;
        public string? VocalPercussionBank;
        public uint? VocalSongScrollSpeed;
        public bool? VocalGender; //true for male, false for female
        //public bool HasAlbumArt;
        //public bool IsFake;
        public uint? VocalTonicNote;
        public bool? SongTonality; // 0 = major, 1 = minor
        public int? TuningOffsetCents;
        public uint? VenueVersion;

        public string[]? Soloes;
        public string[]? VideoVenues;

        public int[]? RealGuitarTuning;
        public int[]? RealBassTuning;

        public RBAudio<int>? Indices;
        public int[]? CrowdChannels;

        public string? Location;
        public float[]? Pans;
        public float[]? Volumes;
        public float[]? Cores;
        public long? HopoThreshold;
        public bool AlternatePath;
        public bool DiscUpdate;
        public Encoding Encoding;

        public RBCONDifficulties Difficulties = RBCONDifficulties.Default;

        private DTAEntry(Encoding encoding)
        {
            Encoding = encoding;
        }

        private void LoadData(string nodename, ref YARGTextContainer<byte> container)
        {
            // The container encoding may differ from the internal encoding
            // for this specifc DTA. This will usually only occur when utilizing
            // CON update or upgrade dtas, NOT direct song dtas.
            //
            // If it does happen with direct song dtas, then the packer didn't check for duplicate node names.
            // Therefore, it's not our problem.
            var containerEncoding = container.Encoding;
            container.Encoding = Encoding;
            while (StartNode(ref container))
            {
                string name = GetNameOfNode(ref container, false);
                switch (name)
                {
                    case "name": Name = ExtractText(ref container); break;
                    case "artist": Artist = ExtractText(ref container); break;
                    case "master": IsMaster = ExtractBoolean_FlippedDefault(ref container); break;
                    case "context": /*Context = container.Read<uint>();*/ break;
                    case "song":
                        while (StartNode(ref container))
                        {
                            string descriptor = GetNameOfNode(ref container, false);
                            switch (descriptor)
                            {
                                case "name": Location = ExtractText(ref container); break;
                                case "tracks":
                                    {
                                        var indices = RBAudio<int>.Empty;
                                        while (StartNode(ref container))
                                        {
                                            while (StartNode(ref container))
                                            {
                                                switch (GetNameOfNode(ref container, false))
                                                {
                                                    case "drum"  : indices.Drums = ExtractArray_Int(ref container); break;
                                                    case "bass"  : indices.Bass = ExtractArray_Int(ref container); break;
                                                    case "guitar": indices.Guitar = ExtractArray_Int(ref container); break;
                                                    case "keys"  : indices.Keys = ExtractArray_Int(ref container); break;
                                                    case "vocals": indices.Vocals = ExtractArray_Int(ref container); break;
                                                }
                                                EndNode(ref container);
                                            }
                                            EndNode(ref container);
                                        }
                                        Indices = indices;
                                        break;
                                    }
                                case "crowd_channels": CrowdChannels = ExtractArray_Int(ref container); break;
                                //case "vocal_parts": VocalParts = container.Read<ushort>(); break;
                                case "pans": Pans = ExtractArray_Float(ref container); break;
                                case "vols": Volumes = ExtractArray_Float(ref container); break;
                                case "cores": Cores = ExtractArray_Float(ref container); break;
                                case "hopo_threshold": HopoThreshold = ExtractInt64(ref container); break;
                            }
                            EndNode(ref container);
                        }
                        break;
                    case "song_vocals": while (StartNode(ref container)) EndNode(ref container); break;
                    case "song_scroll_speed": VocalSongScrollSpeed = ExtractUInt32(ref container); break;
                    case "tuning_offset_cents": TuningOffsetCents = ExtractInt32(ref container); break;
                    case "bank": VocalPercussionBank = ExtractText(ref container); break;
                    case "anim_tempo":
                        {
                            string val = ExtractText(ref container);
                            AnimTempo = val switch
                            {
                                "kTempoSlow" => 16,
                                "kTempoMedium" => 32,
                                "kTempoFast" => 64,
                                _ => uint.Parse(val)
                            };
                            break;
                        }
                    case "preview":
                        PreviewStart = ExtractInt64(ref container);
                        PreviewEnd = ExtractInt64(ref container);
                        break;
                    case "rank":
                        while (StartNode(ref container))
                        {
                            string descriptor = GetNameOfNode(ref container, false);
                            int diff = ExtractInt32(ref container);
                            switch (descriptor)
                            {
                                case "drum":
                                case "drums" : Difficulties.FourLaneDrums = (short) diff; break;

                                case "guitar": Difficulties.FiveFretGuitar = (short) diff; break;
                                case "bass"  : Difficulties.FiveFretBass = (short) diff; break;
                                case "vocals": Difficulties.LeadVocals = (short) diff; break;
                                case "keys"  : Difficulties.Keys = (short) diff; break;

                                case "realGuitar" :
                                case "real_guitar": Difficulties.ProGuitar = (short) diff; break;

                                case "realBass" :
                                case "real_bass": Difficulties.ProBass = (short) diff; break;

                                case "realKeys" :
                                case "real_keys": Difficulties.ProKeys = (short) diff; break;

                                case "realDrums" :
                                case "real_drums": Difficulties.ProDrums = (short) diff; break;

                                case "harmVocals":
                                case "vocal_harm": Difficulties.HarmonyVocals = (short) diff; break;

                                case "band": Difficulties.Band = (short) diff; break;
                            }
                            EndNode(ref container);
                        }
                        break;
                    case "solo": Soloes = ExtractArray_String(ref container); break;
                    case "genre": Genre = ExtractText(ref container); break;
                    case "decade": /*Decade = container.ExtractText();*/ break;
                    case "vocal_gender": VocalGender = ExtractText(ref container) == "male"; break;
                    case "format": /*Format = container.Read<uint>();*/ break;
                    case "version": VenueVersion = ExtractUInt32(ref container); break;
                    case "fake": /*IsFake = container.ExtractText();*/ break;
                    case "downloaded": /*Downloaded = container.ExtractText();*/ break;
                    case "game_origin":
                        {
                            string str = ExtractText(ref container);
                            if ((str == "ugc" || str == "ugc_plus"))
                            {
                                if (!nodename.StartsWith("UGC_"))
                                    Source = "customs";
                            }
                            else if (str == "#ifdef")
                            {
                                string conditional = ExtractText(ref container);
                                if (conditional == "CUSTOMSOURCE")
                                {
                                    Source = ExtractText(ref container);
                                }
                                else
                                {
                                    Source = "customs";
                                }
                            }
                            else
                            {
                                Source = str;
                            }

                            //// if the source is any official RB game or its DLC, charter = Harmonix
                            //if (SongSources.GetSource(str).Type == SongSources.SourceType.RB)
                            //{
                            //    _charter = "Harmonix";
                            //}

                            //// if the source is meant for usage in TBRB, it's a master track
                            //// TODO: NEVER assume localized version contains "Beatles"
                            //if (SongSources.SourceToGameName(str).Contains("Beatles")) _isMaster = true;
                            break;
                        }
                    case "song_id": SongID = ExtractText(ref container); break;
                    case "rating": SongRating = ExtractUInt32(ref container); break;
                    case "short_version": /*ShortVersion = container.Read<uint>();*/ break;
                    case "album_art": /*HasAlbumArt = container.ExtractBoolean();*/ break;
                    case "year_released":
                    case "year_recorded": YearAsNumber = ExtractInt32(ref container); break;
                    case "album_name": Album = ExtractText(ref container); break;
                    case "album_track_number": AlbumTrack = ExtractInt32(ref container); break;
                    case "pack_name": Playlist = ExtractText(ref container); break;
                    case "base_points": /*BasePoints = container.Read<uint>();*/ break;
                    case "band_fail_cue": /*BandFailCue = container.ExtractText();*/ break;
                    case "drum_bank": DrumBank = ExtractText(ref container); break;
                    case "song_length": SongLength = ExtractUInt64(ref container); break;
                    case "sub_genre": /*Subgenre = container.ExtractText();*/ break;
                    case "author": Charter = ExtractText(ref container); break;
                    case "guide_pitch_volume": /*GuidePitchVolume = container.ReadFloat();*/ break;
                    case "encoding":
                        Encoding = ExtractText(ref container).ToLower() switch
                        {
                            "latin1" => YARGTextReader.Latin1,
                            "utf-8" or
                            "utf8" => Encoding.UTF8,
                            _ => container.Encoding
                        };

                        var currEncoding = container.Encoding;
                        if (currEncoding != Encoding)
                        {
                            string Convert(string str)
                            {
                                byte[] bytes = currEncoding.GetBytes(str);
                                return Encoding.GetString(bytes);
                            }

                            if (Name != null)
                                Name = Convert(Name);
                            if (Artist != null)
                                Artist = Convert(Artist);
                            if (Album != null)
                                Album = Convert(Album);
                            if (Genre != null)
                                Genre = Convert(Genre);
                            if (Charter != null)
                                Charter = Convert(Charter);
                            if (Source != null)
                                Source = Convert(Source);

                            if (Playlist != null)
                                Playlist = Convert(Playlist);
                            container.Encoding = Encoding;
                        }

                        break;
                    case "vocal_tonic_note": VocalTonicNote = ExtractUInt32(ref container); break;
                    case "song_tonality": SongTonality = ExtractBoolean(ref container); break;
                    case "alternate_path": AlternatePath = ExtractBoolean(ref container); break;
                    case "real_guitar_tuning": RealGuitarTuning = ExtractArray_Int(ref container); break;
                    case "real_bass_tuning": RealBassTuning = ExtractArray_Int(ref container); break;
                    case "video_venues": VideoVenues = ExtractArray_String(ref container); break;
                    case "extra_authoring":
                        {
                            StringBuilder authors = new();
                            foreach (string str in ExtractArray_String(ref container))
                            {
                                if (str == "disc_update")
                                {
                                    DiscUpdate = true;
                                }
                                else if (authors.Length == 0 && Charter == SongMetadata.DEFAULT_CHARTER)
                                {
                                    authors.Append(str);
                                }
                                else
                                {
                                    if (authors.Length == 0)
                                        authors.Append(Charter);
                                    authors.Append(", " + str);
                                }
                            }

                            if (authors.Length == 0)
                                authors.Append(Charter);

                            Charter = authors.ToString();
                        }
                        break;
                }
                EndNode(ref container);
            }
            container.Encoding = containerEncoding;
        }

        public static Dictionary<string, DTAEntry> LoadEntries(string filename)
        {
            using var data = FixedArray<byte>.Load(filename);
            var entries = LoadEntries(data);
            if (entries == null)
            {
                YargLogger.LogError($"Error while loading {filename}");
                entries = new Dictionary<string, DTAEntry>();
            }
            return entries;
        }

        public static Dictionary<string, DTAEntry> LoadEntries(FileStream stream, CONFileListing listing)
        {
            using var data = listing.LoadAllBytes(stream);
            var entries = LoadEntries(data);
            if (entries == null)
            {
                YargLogger.LogError($"Error while loading {listing.Filename}");
                entries = new Dictionary<string, DTAEntry>();
            }
            return entries;
        }

        private static unsafe Dictionary<string, DTAEntry>? LoadEntries(in FixedArray<byte> data)
        {
            if ((data[0] == 0xFF && data[1] == 0xFE) || (data[0] == 0xFE && data[1] == 0xFF))
            {
                YargLogger.LogError("UTF-16 & UTF-32 are not supported for .dta files");
                return null;
            }

            var container = new YARGTextContainer<byte>(in data, YARGTextReader.Latin1);
            if (data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
            {
                container.Position += 3;
                container.Encoding = Encoding.UTF8;
            }

            var entries = new Dictionary<string, DTAEntry>();
            try
            {
                while (StartNode(ref container))
                {
                    string name = GetNameOfNode(ref container, true);
                    if (!entries.TryGetValue(name, out var entry))
                    {
                        entries.Add(name, entry = new DTAEntry(container.Encoding));
                    }
                    entry.LoadData(name, ref container);
                    EndNode(ref container);
                }
            }
            catch (Exception e)
            {
                YargLogger.LogException(e);
                return null;
            }
            return entries;
        }

        private static unsafe char SkipWhitespace(ref YARGTextContainer<byte> container)
        {
            while (container.Position < container.End)
            {
                char ch = (char) *container.Position;
                if (ch > 32 && ch != ';')
                {
                    return ch;
                }

                ++container.Position;
                if (ch > 32)
                {
                    // In comment
                    while (container.Position < container.End)
                    {
                        if (*container.Position++ == '\n')
                        {
                            break;
                        }
                    }
                }
            }
            return (char) 0;
        }

        private static unsafe string GetNameOfNode(ref YARGTextContainer<byte> container, bool allowNonAlphetical)
        {
            char ch = (char) container.CurrentValue;
            if (ch == '(')
            {
                return string.Empty;
            }

            bool hasApostrophe = ch == '\'';
            if (hasApostrophe)
            {
                ++container.Position;
                ch = (char) container.CurrentValue;
            }

            var start = container.Position;
            var end = container.Position;
            while (true)
            {
                if (ch == '\'')
                {
                    if (!hasApostrophe)
                    {
                        throw new Exception("Invalid name format");
                    }
                    container.Position = end + 1;
                    break;
                }

                if (ch <= 32)
                {
                    if (!hasApostrophe)
                    {
                        container.Position = end + 1;
                        break;
                    }
                }
                else if (!allowNonAlphetical && !ch.IsAsciiLetter() && ch != '_')
                {
                    container.Position = end;
                    break;
                }

                ++end;
                if (end >= container.End)
                {
                    container.Position = end;
                    break;
                }
                ch = (char) *end;
            }

            SkipWhitespace(ref container);
            return Encoding.UTF8.GetString(start, (int) (end - start));
        }

        private enum TextScopeState
        {
            None,
            Squirlies,
            Quotes,
            Apostrophes
        }

        private static unsafe string ExtractText(ref YARGTextContainer<byte> container)
        {
            char ch = (char) container.CurrentValue;
            var state = ch switch
            {
                '{' => TextScopeState.Squirlies,
                '\"' => TextScopeState.Quotes,
                '\'' => TextScopeState.Apostrophes,
                _ => TextScopeState.None
            };

            if (state != TextScopeState.None)
            {
                ++container.Position;
                ch = (char) container.CurrentValue;
            }

            var start = container.Position;
            // Loop til the end of the text is found
            while (true)
            {
                if (ch == '{')
                    throw new Exception("Text error - no { braces allowed");

                if (ch == '}')
                {
                    if (state == TextScopeState.Squirlies)
                        break;
                    throw new Exception("Text error - no \'}\' allowed");
                }
                else if (ch == '\"')
                {
                    if (state == TextScopeState.Quotes)
                        break;
                    if (state != TextScopeState.Squirlies)
                        throw new Exception("Text error - no quotes allowed");
                }
                else if (ch == '\'')
                {
                    if (state == TextScopeState.Apostrophes)
                        break;
                    if (state == TextScopeState.None)
                        throw new Exception("Text error - no apostrophes allowed");
                }
                else if (ch <= 32 || ch == ')')
                {
                    if (state == TextScopeState.None)
                        break;
                }
                ++container.Position;
                ch = (char) container.CurrentValue;
            }

            string txt = container.Encoding.GetString(start, (int) (container.Position - start)).Replace("\\q", "\"");
            if (ch != ')')
            {
                ++container.Position;

            }
            SkipWhitespace(ref container);
            return txt;
        }

        private static int[] ExtractArray_Int(ref YARGTextContainer<byte> container)
        {
            bool doEnd = StartNode(ref container);
            List<int> values = new();
            while (container.CurrentValue != ')')
            {
                values.Add(ExtractInt32(ref container));
            }

            if (doEnd)
            {
                EndNode(ref container);
            }
            return values.ToArray();
        }

        private static float[] ExtractArray_Float(ref YARGTextContainer<byte> container)
        {
            bool doEnd = StartNode(ref container);
            List<float> values = new();
            while (container.CurrentValue != ')')
            {
                values.Add(ExtractFloat(ref container));
            }

            if (doEnd)
            {
                EndNode(ref container);
            }
            return values.ToArray();
        }

        private static string[] ExtractArray_String(ref YARGTextContainer<byte> container)
        {
            bool doEnd = StartNode(ref container);
            List<string> strings = new();
            while (container.CurrentValue != ')')
            {
                strings.Add(ExtractText(ref container));
            }

            if (doEnd)
            {
                EndNode(ref container);
            }
            return strings.ToArray();
        }

        private static bool StartNode(ref YARGTextContainer<byte> container)
        {
            if (container.IsAtEnd() || !container.IsCurrentCharacter('('))
            {
                return false;
            }

            unsafe
            {
                ++container.Position;
            }
            SkipWhitespace(ref container);
            return true;
        }

        private static unsafe void EndNode(ref YARGTextContainer<byte> container)
        {
            int scopeLevel = 0;
            bool inApostropes = false;
            bool inQuotes = false;
            bool inComment = false;
            while (container.Position < container.End && scopeLevel >= 0)
            {
                char curr = (char) *container.Position;
                ++container.Position;
                if (inComment)
                {
                    if (curr == '\n')
                    {
                        inComment = false;
                    }
                }
                else if (curr == '\"')
                {
                    if (inApostropes)
                    {
                        throw new Exception("Ah hell nah wtf");
                    }
                    inQuotes = !inQuotes;
                }
                else if (!inQuotes)
                {
                    if (!inApostropes)
                    {
                        switch (curr)
                        {
                            case '(': ++scopeLevel; break;
                            case ')': --scopeLevel; break;
                            case '\'': inApostropes = true; break;
                            case ';': inComment = true; break;
                        }
                    }
                    else if (curr == '\'')
                    {
                        inApostropes = false;
                    }
                }
            }
            SkipWhitespace(ref container);
        }

        /// <summary>
        /// Extracts a boolean and skips the following whitespace
        /// </summary>
        /// <remarks>Throws if no value could be parsed</remarks>
        /// <returns>The boolean or `false` on failed extraction</returns>
        private static bool ExtractBoolean(ref YARGTextContainer<byte> container)
        {
            bool result = YARGTextReader.ExtractBoolean(in container);
            SkipWhitespace(ref container);
            return result;
        }

        /// <summary>
        /// Extracts a boolean and skips the following whitespace
        /// </summary>
        /// <remarks>Throws if no value could be parsed</remarks>
        /// <returns>The boolean or `true` on failed extraction</returns>
        private static unsafe bool ExtractBoolean_FlippedDefault(ref YARGTextContainer<byte> container)
        {
            bool result = container.Position >= container.End || (char) *container.Position switch
            {
                '0' => false,
                '1' => true,
                _ => container.Position + 5 > container.End ||
                    char.ToLowerInvariant((char) container.Position[0]) != 'f' ||
                    char.ToLowerInvariant((char) container.Position[1]) != 'a' ||
                    char.ToLowerInvariant((char) container.Position[2]) != 'l' ||
                    char.ToLowerInvariant((char) container.Position[3]) != 's' ||
                    char.ToLowerInvariant((char) container.Position[4]) != 'e',
            };
            SkipWhitespace(ref container);
            return result;
        }

        /// <summary>
        /// Extracts a short and skips the following whitespace
        /// </summary>
        /// <remarks>Throws if no value could be parsed</remarks>
        /// <returns>The short</returns>
        private static short ExtractInt16(ref YARGTextContainer<byte> container)
        {
            if (!YARGTextReader.TryExtractInt16(ref container, out short value))
            {
                throw new Exception("Data for Int16 not present");
            }
            SkipWhitespace(ref container);
            return value;
        }

        /// <summary>
        /// Extracts a ushort and skips the following whitespace
        /// </summary>
        /// <remarks>Throws if no value could be parsed</remarks>
        /// <returns>The ushort</returns>
        private static ushort ExtractUInt16(ref YARGTextContainer<byte> container)
        {
            if (!YARGTextReader.TryExtractUInt16(ref container, out ushort value))
            {
                throw new Exception("Data for UInt16 not present");
            }
            SkipWhitespace(ref container);
            return value;
        }

        /// <summary>
        /// Extracts a int and skips the following whitespace
        /// </summary>
        /// <remarks>Throws if no value could be parsed</remarks>
        /// <returns>The int</returns>
        private static int ExtractInt32(ref YARGTextContainer<byte> container)
        {
            if (!YARGTextReader.TryExtractInt32(ref container, out int value))
            {
                throw new Exception("Data for Int32 not present");
            }
            SkipWhitespace(ref container);
            return value;
        }

        /// <summary>
        /// Extracts a uint and skips the following whitespace
        /// </summary>
        /// <remarks>Throws if no value could be parsed</remarks>
        /// <returns>The uint</returns>
        private static uint ExtractUInt32(ref YARGTextContainer<byte> container)
        {
            if (!YARGTextReader.TryExtractUInt32(ref container, out uint value))
            {
                throw new Exception("Data for UInt32 not present");
            }
            SkipWhitespace(ref container);
            return value;
        }

        /// <summary>
        /// Extracts a long and skips the following whitespace
        /// </summary>
        /// <remarks>Throws if no value could be parsed</remarks>
        /// <returns>The long</returns>
        private static long ExtractInt64(ref YARGTextContainer<byte> container)
        {
            if (!YARGTextReader.TryExtractInt64(ref container, out long value))
            {
                throw new Exception("Data for Int64 not present");
            }
            SkipWhitespace(ref container);
            return value;
        }

        /// <summary>
        /// Extracts a ulong and skips the following whitespace
        /// </summary>
        /// <remarks>Throws if no value could be parsed</remarks>
        /// <returns>The ulong</returns>
        private static ulong ExtractUInt64(ref YARGTextContainer<byte> container)
        {
            if (!YARGTextReader.TryExtractUInt64(ref container, out ulong value))
            {
                throw new Exception("Data for UInt64 not present");
            }
            SkipWhitespace(ref container);
            return value;
        }

        /// <summary>
        /// Extracts a float and skips the following whitespace
        /// </summary>
        /// <remarks>Throws if no value could be parsed</remarks>
        /// <returns>The float</returns>
        private static float ExtractFloat(ref YARGTextContainer<byte> container)
        {
            if (!YARGTextReader.TryExtractFloat(ref container, out float value))
            {
                throw new Exception("Data for float not present");
            }
            SkipWhitespace(ref container);
            return value;
        }
    }
}

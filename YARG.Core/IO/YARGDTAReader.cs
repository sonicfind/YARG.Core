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
        public static DTAEntry[] LoadEntries(FileInfo dta)
        {
            using var data = MemoryMappedArray.Load(dta);
            var entries = LoadEntries(data);
            if (entries == null)
            {
                YargLogger.LogError($"Error while loading {dta.FullName}");
                entries = Array.Empty<DTAEntry>();
            }
            return entries;
        }

        public static DTAEntry[] LoadEntries(FileStream stream, CONFileListing? listing)
        {
            if (listing == null)
            {
                return Array.Empty<DTAEntry>();
            }

            using var data = listing.LoadAllBytes(stream);
            var entries = LoadEntries(data);
            if (entries == null)
            {
                YargLogger.LogError($"Error while loading {listing.Filename}");
                entries = Array.Empty<DTAEntry>();
            }
            return entries;
        }

        private static unsafe DTAEntry[]? LoadEntries(FixedArray<byte> data)
        {
            if ((data[0] == 0xFF && data[1] == 0xFE) || (data[0] == 0xFE && data[1] == 0xFF))
            {
                YargLogger.LogError("UTF-16 & UTF-32 are not supported for .dta files");
                return null;
            }

            var container = data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF
                ? new YARGTextContainer<byte>(data.Ptr + 3, data.Ptr + data.Length, Encoding.UTF8)
                : new YARGTextContainer<byte>(data.Ptr, data.Ptr + data.Length, YARGTextReader.Latin1);

            var entries = new List<DTAEntry>();
            try
            {
                while (YARGDTAReader.StartNode(ref container))
                {
                    string name = YARGDTAReader.GetNameOfNode(ref container, true);
                    var entry = new DTAEntry(name, container);
                    entries.Add(entry);
                    YARGDTAReader.EndNode(ref container);
                }
            }
            catch (Exception e)
            {
                YargLogger.LogException(e);
                return null;
            }
            return entries.ToArray();
        }

        public readonly string NodeName;
        public readonly string? Name;
        public readonly string? Artist;
        public readonly string? Album;
        public readonly string? Genre;
        public readonly string? Charter;
        public readonly string? Source;
        public readonly string? Playlist;
        public readonly string? Year;
        public readonly int YearAsNumber;

        public readonly ulong SongLength;
        public readonly uint SongRating;  // 1 = FF; 2 = SR; 3 = M; 4 = NR

        public readonly long PreviewStart;
        public readonly long PreviewEnd;
        public readonly bool IsMaster;

        public readonly int AlbumTrack;

        public readonly string? SongID;
        public readonly uint AnimTempo;
        public readonly string? DrumBank;
        public readonly string? VocalPercussionBank;
        public readonly uint VocalSongScrollSpeed;
        public readonly bool VocalGender; //true for male, false for female
        //public bool HasAlbumArt;
        //public bool IsFake;
        public readonly uint VocalTonicNote;
        public readonly bool SongTonality; // 0 = major, 1 = minor
        public readonly int TuningOffsetCents;
        public readonly uint VenueVersion;

        public readonly string[]? Soloes;
        public readonly string[]? VideoVenues;

        public readonly int[]? RealGuitarTuning;
        public readonly int[]? RealBassTuning;

        public readonly RBAudio<int> Indices;
        public readonly RBAudio<float> Panning;

        public readonly string? Location;
        public readonly float[]? Pans;
        public readonly float[]? Volumes;
        public readonly float[]? Cores;
        public readonly long HopoThreshold;
        public readonly bool AlternatePath;
        public readonly bool DiscUpdate;
        public readonly Encoding Encoding;

        public readonly RBCONDifficulties RBDifficulties;
        public readonly AvailableParts Parts;

        public DTAEntry(string nodename, YARGTextContainer<byte> container)
        {
            NodeName = nodename;
            Encoding = container.Encoding;
            while (YARGDTAReader.StartNode(ref container))
            {
                string name = YARGDTAReader.GetNameOfNode(ref container, false);
                switch (name)
                {
                    case "name": Name = YARGDTAReader.ExtractText(ref container); break;
                    case "artist": Artist = YARGDTAReader.ExtractText(ref container); break;
                    case "master": IsMaster = YARGDTAReader.ExtractBoolean_FlippedDefault(ref container); break;
                    case "context": /*Context = container.Read<uint>();*/ break;
                    case "song":
                        while (YARGDTAReader.StartNode(ref container))
                        {
                            string descriptor = YARGDTAReader.GetNameOfNode(ref container, false);
                            switch (descriptor)
                            {
                                case "name": Location = YARGDTAReader.ExtractText(ref container); break;
                                case "tracks":
                                    while (YARGDTAReader.StartNode(ref container))
                                    {
                                        while (YARGDTAReader.StartNode(ref container))
                                        {
                                            switch (YARGDTAReader.GetNameOfNode(ref container, false))
                                            {
                                                case "drum"  : Indices.Drums = YARGDTAReader.ExtractArray_Int(ref container); break;
                                                case "bass"  : Indices.Bass = YARGDTAReader.ExtractArray_Int(ref container); break;
                                                case "guitar": Indices.Guitar = YARGDTAReader.ExtractArray_Int(ref container); break;
                                                case "keys"  : Indices.Keys = YARGDTAReader.ExtractArray_Int(ref container); break;
                                                case "vocals": Indices.Vocals = YARGDTAReader.ExtractArray_Int(ref container); break;
                                            }
                                            YARGDTAReader.EndNode(ref container);
                                        }
                                        YARGDTAReader.EndNode(ref container);
                                    }
                                    break;
                                case "crowd_channels": Indices.Crowd = YARGDTAReader.ExtractArray_Int(ref container); break;
                                //case "vocal_parts": VocalParts = container.Read<ushort>(); break;
                                case "pans": Pans = YARGDTAReader.ExtractArray_Float(ref container); break;
                                case "vols": Volumes = YARGDTAReader.ExtractArray_Float(ref container); break;
                                case "cores": Cores = YARGDTAReader.ExtractArray_Float(ref container); break;
                                case "hopo_threshold": HopoThreshold = YARGDTAReader.ExtractInt64(ref container); break;
                            }
                            YARGDTAReader.EndNode(ref container);
                        }
                        break;
                    case "song_vocals": while (YARGDTAReader.StartNode(ref container)) YARGDTAReader.EndNode(ref container); break;
                    case "song_scroll_speed": VocalSongScrollSpeed = YARGDTAReader.ExtractUInt32(ref container); break;
                    case "tuning_offset_cents": TuningOffsetCents = YARGDTAReader.ExtractInt32(ref container); break;
                    case "bank": VocalPercussionBank = YARGDTAReader.ExtractText(ref container); break;
                    case "anim_tempo":
                        {
                            string val = YARGDTAReader.ExtractText(ref container);
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
                        PreviewStart = YARGDTAReader.ExtractInt64(ref container);
                        PreviewEnd = YARGDTAReader.ExtractInt64(ref container);
                        break;
                    case "rank":
                        while (YARGDTAReader.StartNode(ref container))
                        {
                            string descriptor = YARGDTAReader.GetNameOfNode(ref container, false);
                            int diff = YARGDTAReader.ExtractInt32(ref container);
                            switch (descriptor)
                            {
                                case "drum":
                                case "drums":
                                    RBDifficulties.FourLaneDrums = (short) diff;
                                    SetRank(ref Parts.FourLaneDrums.Intensity, diff, DrumDiffMap);
                                    if (Parts.ProDrums.Intensity == -1)
                                    {
                                        Parts.ProDrums.Intensity = Parts.FourLaneDrums.Intensity;
                                    }
                                    break;
                                case "guitar":
                                    RBDifficulties.FiveFretGuitar = (short) diff;
                                    SetRank(ref Parts.FiveFretGuitar.Intensity, diff, GuitarDiffMap);
                                    if (Parts.ProGuitar_17Fret.Intensity == -1)
                                    {
                                        Parts.ProGuitar_22Fret.Intensity = Parts.ProGuitar_17Fret.Intensity = Parts.FiveFretGuitar.Intensity;
                                    }
                                    break;
                                case "bass":
                                    RBDifficulties.FiveFretBass = (short) diff;
                                    SetRank(ref Parts.FiveFretBass.Intensity, diff, BassDiffMap);
                                    if (Parts.ProBass_17Fret.Intensity == -1)
                                    {
                                        Parts.ProBass_22Fret.Intensity = Parts.ProBass_17Fret.Intensity = Parts.FiveFretBass.Intensity;
                                    }
                                    break;
                                case "vocals":
                                    RBDifficulties.LeadVocals = (short) diff;
                                    SetRank(ref Parts.LeadVocals.Intensity, diff, VocalsDiffMap);
                                    if (Parts.HarmonyVocals.Intensity == -1)
                                    {
                                        Parts.HarmonyVocals.Intensity = Parts.LeadVocals.Intensity;
                                    }
                                    break;
                                case "keys":
                                    RBDifficulties.Keys = (short) diff;
                                    SetRank(ref Parts.Keys.Intensity, diff, KeysDiffMap);
                                    if (Parts.ProKeys.Intensity == -1)
                                    {
                                        Parts.ProKeys.Intensity = Parts.Keys.Intensity;
                                    }
                                    break;
                                case "realGuitar":
                                case "real_guitar":
                                    RBDifficulties.ProGuitar = (short) diff;
                                    SetRank(ref Parts.ProGuitar_17Fret.Intensity, diff, RealGuitarDiffMap);
                                    Parts.ProGuitar_22Fret.Intensity = Parts.ProGuitar_17Fret.Intensity;
                                    if (Parts.FiveFretGuitar.Intensity == -1)
                                    {
                                        Parts.FiveFretGuitar.Intensity = Parts.ProGuitar_17Fret.Intensity;
                                    }
                                    break;
                                case "realBass":
                                case "real_bass":
                                    RBDifficulties.ProBass = (short) diff;
                                    SetRank(ref Parts.ProBass_17Fret.Intensity, diff, RealBassDiffMap);
                                    Parts.ProBass_22Fret.Intensity = Parts.ProBass_17Fret.Intensity;
                                    if (Parts.FiveFretBass.Intensity == -1)
                                    {
                                        Parts.FiveFretBass.Intensity = Parts.ProBass_17Fret.Intensity;
                                    }
                                    break;
                                case "realKeys":
                                case "real_keys":
                                    RBDifficulties.ProKeys = (short) diff;
                                    SetRank(ref Parts.ProKeys.Intensity, diff, RealKeysDiffMap);
                                    if (Parts.Keys.Intensity == -1)
                                    {
                                        Parts.Keys.Intensity = Parts.ProKeys.Intensity;
                                    }
                                    break;
                                case "realDrums":
                                case "real_drums":
                                    RBDifficulties.ProDrums = (short) diff;
                                    SetRank(ref Parts.ProDrums.Intensity, diff, RealDrumsDiffMap);
                                    if (Parts.FourLaneDrums.Intensity == -1)
                                    {
                                        Parts.FourLaneDrums.Intensity = Parts.ProDrums.Intensity;
                                    }
                                    break;
                                case "harmVocals":
                                case "vocal_harm":
                                    RBDifficulties.HarmonyVocals = (short) diff;
                                    SetRank(ref Parts.HarmonyVocals.Intensity, diff, HarmonyDiffMap);
                                    if (Parts.LeadVocals.Intensity == -1)
                                    {
                                        Parts.LeadVocals.Intensity = Parts.HarmonyVocals.Intensity;
                                    }
                                    break;
                                case "band":
                                    RBDifficulties.Band = (short) diff;
                                    SetRank(ref Parts.BandDifficulty.Intensity, diff, BandDiffMap);
                                    Parts.BandDifficulty.SubTracks = 1;
                                    break;
                            }
                            YARGDTAReader.EndNode(ref container);
                        }
                        break;
                    case "solo": Soloes = YARGDTAReader.ExtractArray_String(ref container); break;
                    case "genre": Genre = YARGDTAReader.ExtractText(ref container); break;
                    case "decade": /*Decade = container.ExtractText();*/ break;
                    case "vocal_gender": VocalGender = YARGDTAReader.ExtractText(ref container) == "male"; break;
                    case "format": /*Format = container.Read<uint>();*/ break;
                    case "version": VenueVersion = YARGDTAReader.ExtractUInt32(ref container); break;
                    case "fake": /*IsFake = container.ExtractText();*/ break;
                    case "downloaded": /*Downloaded = container.ExtractText();*/ break;
                    case "game_origin":
                        {
                            string str = YARGDTAReader.ExtractText(ref container);
                            if ((str == "ugc" || str == "ugc_plus"))
                            {
                                if (!nodename.StartsWith("UGC_"))
                                    Source = "customs";
                            }
                            else if (str == "#ifdef")
                            {
                                string conditional = YARGDTAReader.ExtractText(ref container);
                                if (conditional == "CUSTOMSOURCE")
                                {
                                    Source = YARGDTAReader.ExtractText(ref container);
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
                    case "song_id": SongID = YARGDTAReader.ExtractText(ref container); break;
                    case "rating": SongRating = YARGDTAReader.ExtractUInt32(ref container); break;
                    case "short_version": /*ShortVersion = container.Read<uint>();*/ break;
                    case "album_art": /*HasAlbumArt = container.ExtractBoolean();*/ break;
                    case "year_released":
                    case "year_recorded":
                        YearAsNumber = YARGDTAReader.ExtractInt32(ref container);
                        Year = YearAsNumber.ToString();
                        break;
                    case "album_name": Album = YARGDTAReader.ExtractText(ref container); break;
                    case "album_track_number": AlbumTrack = YARGDTAReader.ExtractInt32(ref container); break;
                    case "pack_name": Playlist = YARGDTAReader.ExtractText(ref container); break;
                    case "base_points": /*BasePoints = container.Read<uint>();*/ break;
                    case "band_fail_cue": /*BandFailCue = container.ExtractText();*/ break;
                    case "drum_bank": DrumBank = YARGDTAReader.ExtractText(ref container); break;
                    case "song_length": SongLength = YARGDTAReader.ExtractUInt64(ref container); break;
                    case "sub_genre": /*Subgenre = container.ExtractText();*/ break;
                    case "author": Charter = YARGDTAReader.ExtractText(ref container); break;
                    case "guide_pitch_volume": /*GuidePitchVolume = container.ReadFloat();*/ break;
                    case "encoding":
                        Encoding = YARGDTAReader.ExtractText(ref container).ToLower() switch
                        {
                            "latin1" => YARGTextReader.Latin1,
                            "utf-8" or
                            "utf8" => Encoding.UTF8,
                            _ => container.Encoding
                        };

                        if (container.Encoding != Encoding)
                        {
                            string Convert(string str)
                            {
                                byte[] bytes = container.Encoding.GetBytes(str);
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
                    case "vocal_tonic_note": VocalTonicNote = YARGDTAReader.ExtractUInt32(ref container); break;
                    case "song_tonality": SongTonality = YARGDTAReader.ExtractBoolean(ref container); break;
                    case "alternate_path": AlternatePath = YARGDTAReader.ExtractBoolean(ref container); break;
                    case "real_guitar_tuning": RealGuitarTuning = YARGDTAReader.ExtractArray_Int(ref container); break;
                    case "real_bass_tuning": RealBassTuning = YARGDTAReader.ExtractArray_Int(ref container); break;
                    case "video_venues": VideoVenues = YARGDTAReader.ExtractArray_String(ref container); break;
                    case "extra_authoring":
                        {
                            StringBuilder authors = new();
                            foreach (string str in YARGDTAReader.ExtractArray_String(ref container))
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
                YARGDTAReader.EndNode(ref container);
            }
        }

        private static readonly int[] BandDiffMap = { 163, 215, 243, 267, 292, 345 };
        private static readonly int[] GuitarDiffMap = { 139, 176, 221, 267, 333, 409 };
        private static readonly int[] BassDiffMap = { 135, 181, 228, 293, 364, 436 };
        private static readonly int[] DrumDiffMap = { 124, 151, 178, 242, 345, 448 };
        private static readonly int[] KeysDiffMap = { 153, 211, 269, 327, 385, 443 };
        private static readonly int[] VocalsDiffMap = { 132, 175, 218, 279, 353, 427 };
        private static readonly int[] RealGuitarDiffMap = { 150, 205, 264, 323, 382, 442 };
        private static readonly int[] RealBassDiffMap = { 150, 208, 267, 325, 384, 442 };
        private static readonly int[] RealDrumsDiffMap = { 124, 151, 178, 242, 345, 448 };
        private static readonly int[] RealKeysDiffMap = { 153, 211, 269, 327, 385, 443 };
        private static readonly int[] HarmonyDiffMap = { 132, 175, 218, 279, 353, 427 };

        private static void SetRank(ref sbyte intensity, int rank, int[] values)
        {
            sbyte i = 0;
            while (i < 6 && values[i] <= rank)
                ++i;
            intensity = i;
        }
    }

    public unsafe static class YARGDTAReader
    {
        public static bool TryCreate(in FixedArray<byte> data, out YARGTextContainer<byte> container)
        {
            if ((data[0] == 0xFF && data[1] == 0xFE) || (data[0] == 0xFE && data[1] == 0xFF))
            {
                YargLogger.LogError("UTF-16 & UTF-32 are not supported for .dta files");
                container = default;
                return false;
            }

            container = new YARGTextContainer<byte>(in data, YARGTextReader.Latin1);
            if (data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
            {
                container.Position += 3;
                container.Encoding = Encoding.UTF8;
            }
            return true;
        }

        public static char SkipWhitespace(ref YARGTextContainer<byte> container)
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

        public static string GetNameOfNode(ref YARGTextContainer<byte> container, bool allowNonAlphetical)
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

        public static string ExtractText(ref YARGTextContainer<byte> container)
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

        public static int[] ExtractArray_Int(ref YARGTextContainer<byte> container)
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

        public static float[] ExtractArray_Float(ref YARGTextContainer<byte> container)
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

        public static string[] ExtractArray_String(ref YARGTextContainer<byte> container)
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

        public static bool StartNode(ref YARGTextContainer<byte> container)
        {
            if (container.IsAtEnd() || !container.IsCurrentCharacter('('))
            {
                return false;
            }

            ++container.Position;
            SkipWhitespace(ref container);
            return true;
        }

        public static void EndNode(ref YARGTextContainer<byte> container)
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
        public static bool ExtractBoolean(ref YARGTextContainer<byte> container)
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
        public static bool ExtractBoolean_FlippedDefault(ref YARGTextContainer<byte> container)
        {
            bool result = container.Position >= container.End || (char)*container.Position switch
            {
                '0' => false,
                '1' => true,
                _ => container.Position + 5 > container.End ||
                    char.ToLowerInvariant((char)container.Position[0]) != 'f' ||
                    char.ToLowerInvariant((char)container.Position[1]) != 'a' ||
                    char.ToLowerInvariant((char)container.Position[2]) != 'l' ||
                    char.ToLowerInvariant((char)container.Position[3]) != 's' ||
                    char.ToLowerInvariant((char)container.Position[4]) != 'e',
            };
            SkipWhitespace(ref container);
            return result;
        }

        /// <summary>
        /// Extracts a short and skips the following whitespace
        /// </summary>
        /// <remarks>Throws if no value could be parsed</remarks>
        /// <returns>The short</returns>
        public static short ExtractInt16(ref YARGTextContainer<byte> container)
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
        public static ushort ExtractUInt16(ref YARGTextContainer<byte> container)
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
        public static int ExtractInt32(ref YARGTextContainer<byte> container)
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
        public static uint ExtractUInt32(ref YARGTextContainer<byte> container)
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
        public static long ExtractInt64(ref YARGTextContainer<byte> container)
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
        public static ulong ExtractUInt64(ref YARGTextContainer<byte> container)
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
        public static float ExtractFloat(ref YARGTextContainer<byte> container)
        {
            if (!YARGTextReader.TryExtractFloat(ref container, out float value))
            {
                throw new Exception("Data for float not present");
            }
            SkipWhitespace(ref container);
            return value;
        }

        /// <summary>
        /// Extracts a double and skips the following whitespace
        /// </summary>
        /// <remarks>Throws if no value could be parsed</remarks>
        /// <returns>The double</returns>
        public static double ExtractDouble(ref YARGTextContainer<byte> container)
        {
            if (!YARGTextReader.TryExtractDouble(ref container, out double value))
            {
                throw new Exception("Data for double not present");
            }
            SkipWhitespace(ref container);
            return value;
        }
    };
}

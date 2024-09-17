using System;
using System.Collections.Generic;
using System.IO;
using YARG.Core.Extensions;
using YARG.Core.IO;

namespace YARG.Core.Song.Cache
{
    public sealed class UpdateGroup : IModificationGroup
    {
        public readonly DirectoryInfo Directory;
        public readonly DateTime DTALastWrite;
        public readonly Dictionary<string, SongUpdate> Updates = new();

        public UpdateGroup(in FileCollection collection, FileInfo dta, Dictionary<string, Dictionary<string, FileInfo>> mapping)
        {
            Directory = collection.Directory;
            DTALastWrite = dta.LastWriteTime;

            Updates = new Dictionary<string, SongUpdate>();
            foreach (var (name, entry) in DTAEntry.LoadEntries(dta.FullName))
            {
                AbridgedFileInfo? midi = null;
                AbridgedFileInfo? mogg = null;
                AbridgedFileInfo? milo = null;
                AbridgedFileInfo? image = null;

                string subname = name.ToLowerInvariant();
                if (mapping.TryGetValue(subname, out var files))
                {
                    if (files.TryGetValue(subname + "_update.mid", out var file))
                    {
                        midi = new AbridgedFileInfo(file, false);
                    }
                    if (files.TryGetValue(subname + "_update.mogg", out file))
                    {
                        mogg = new AbridgedFileInfo(file, false);
                    }
                    if (files.TryGetValue(subname + ".milo_xbox", out file))
                    {
                        milo = new AbridgedFileInfo(file, false);
                    }
                    if (files.TryGetValue(subname + "_keep.png_xbox", out file))
                    {
                        image = new AbridgedFileInfo(file, false);
                    }
                }

                Updates.Add(name, new SongUpdate(in DTALastWrite, midi, mogg, milo, image, entry));
            }
        }

        public ReadOnlyMemory<byte> SerializeModifications()
        {
            using MemoryStream ms = new();
            using BinaryWriter writer = new(ms);

            writer.Write(Directory.FullName);
            writer.Write(DTALastWrite.ToBinary());
            writer.Write(Updates.Count);
            foreach (var (name, update) in Updates)
            {
                writer.Write(name);
                update.Serialize(writer);
            }
            return new ReadOnlyMemory<byte>(ms.GetBuffer(), 0, (int)ms.Length);
        }
    }

    public class SongUpdate
    {
        public readonly DateTime UpdateTime;
        public readonly AbridgedFileInfo? Midi;
        public readonly AbridgedFileInfo? Mogg;
        public readonly AbridgedFileInfo? Milo;
        public readonly AbridgedFileInfo? Image;
        public readonly DTAEntry Entry;

        internal SongUpdate(in DateTime time, AbridgedFileInfo? midi, AbridgedFileInfo? mogg, AbridgedFileInfo? milo, AbridgedFileInfo? image, DTAEntry entry)
        {
            UpdateTime = time;
            Midi = midi;
            Mogg = mogg;
            Milo = milo;
            Image = image;
            Entry = entry;
        }

        public void Serialize(BinaryWriter writer)
        {
            WriteInfo(Midi, writer);
            WriteInfo(Mogg, writer);
            WriteInfo(Milo, writer);
            WriteInfo(Image, writer);

            static void WriteInfo(in AbridgedFileInfo? info, BinaryWriter writer)
            {
                if (info != null)
                {
                    writer.Write(true);
                    writer.Write(info.Value.LastUpdatedTime.ToBinary());
                }
                else
                {
                    writer.Write(false);
                }
            }
        }

        public bool Validate(UnmanagedMemoryStream stream)
        {
            if (!CheckInfo(in Midi, stream))
            {
                SkipInfo(stream);
                SkipInfo(stream);
                SkipInfo(stream);
                return false;
            }

            if (!CheckInfo(in Mogg, stream))
            {
                SkipInfo(stream);
                SkipInfo(stream);
                return false;
            }

            if (!CheckInfo(in Milo, stream))
            {
                SkipInfo(stream);
                return false;
            }
            return CheckInfo(in Image, stream);

            static bool CheckInfo(in AbridgedFileInfo? info, UnmanagedMemoryStream stream)
            {
                if (stream.ReadBoolean())
                {
                    var lastWrite = DateTime.FromBinary(stream.Read<long>(Endianness.Little));
                    if (info == null || info.Value.LastUpdatedTime != lastWrite)
                    {
                        return false;
                    }
                }
                else if (info != null)
                {
                    return false;
                }
                return true;
            }
        }

        public static SongUpdate Union(SongUpdate up1, SongUpdate up2)
        {
            SongUpdate newest, oldest;
            if (up1.UpdateTime < up2.UpdateTime)
            {
                newest = up2;
                oldest = up1;
            }
            else
            {
                newest = up1;
                oldest = up2;
            }

            // Overrides old data with newer data where applicable
            if (newest.Entry.Name != null) { oldest.Entry.Name = newest.Entry.Name; }
            if (newest.Entry.Artist != null) { oldest.Entry.Artist = newest.Entry.Artist; }
            if (newest.Entry.Charter != null) { oldest.Entry.Charter = newest.Entry.Charter; }
            if (newest.Entry.Genre != null) { oldest.Entry.Genre = newest.Entry.Genre; }
            if (newest.Entry.YearAsNumber != null) { oldest.Entry.YearAsNumber = newest.Entry.YearAsNumber; }
            if (newest.Entry.Source != null) { oldest.Entry.Source = newest.Entry.Source; }
            if (newest.Entry.Playlist != null) { oldest.Entry.Playlist = newest.Entry.Playlist; }
            if (newest.Entry.SongLength != null) { oldest.Entry.SongLength = newest.Entry.SongLength; }
            if (newest.Entry.IsMaster != null) { oldest.Entry.IsMaster = newest.Entry.IsMaster; }
            if (newest.Entry.AlbumTrack != null) { oldest.Entry.AlbumTrack = newest.Entry.AlbumTrack; }
            if (newest.Entry.PreviewStart != null)
            {
                oldest.Entry.PreviewStart = newest.Entry.PreviewStart;
                oldest.Entry.PreviewEnd = newest.Entry.PreviewEnd!;
            }
            if (newest.Entry.HopoThreshold != null) { oldest.Entry.HopoThreshold = newest.Entry.HopoThreshold; }
            if (newest.Entry.SongRating != null) { oldest.Entry.SongRating = newest.Entry.SongRating; }
            if (newest.Entry.VocalPercussionBank != null) { oldest.Entry.VocalPercussionBank = newest.Entry.VocalPercussionBank; }
            if (newest.Entry.VocalGender != null) { oldest.Entry.VocalGender = newest.Entry.VocalGender; }
            if (newest.Entry.VocalSongScrollSpeed != null) { oldest.Entry.VocalSongScrollSpeed = newest.Entry.VocalSongScrollSpeed; }
            if (newest.Entry.VocalTonicNote != null) { oldest.Entry.VocalTonicNote = newest.Entry.VocalTonicNote; }
            if (newest.Entry.VideoVenues != null) { oldest.Entry.VideoVenues = newest.Entry.VideoVenues; }
            if (newest.Entry.DrumBank != null) { oldest.Entry.DrumBank = newest.Entry.DrumBank; }
            if (newest.Entry.SongID != null) { oldest.Entry.SongID = newest.Entry.SongID; }
            if (newest.Entry.SongTonality != null) { oldest.Entry.SongTonality = newest.Entry.SongTonality; }
            if (newest.Entry.Soloes != null) { oldest.Entry.Soloes = newest.Entry.Soloes; }
            if (newest.Entry.AnimTempo != null) { oldest.Entry.AnimTempo = newest.Entry.AnimTempo; }
            if (newest.Entry.TuningOffsetCents != null) { oldest.Entry.TuningOffsetCents = newest.Entry.TuningOffsetCents; }
            if (newest.Entry.RealGuitarTuning != null) { oldest.Entry.RealGuitarTuning = newest.Entry.RealGuitarTuning; }
            if (newest.Entry.RealBassTuning != null) { oldest.Entry.RealBassTuning = newest.Entry.RealBassTuning; }

            if (newest.Entry.Cores != null) { oldest.Entry.Cores = newest.Entry.Cores; }
            if (newest.Entry.Volumes != null) { oldest.Entry.Volumes = newest.Entry.Volumes; }
            if (newest.Entry.Pans != null) { oldest.Entry.Pans = newest.Entry.Pans; }

            if (newest.Entry.Location != null) { oldest.Entry.Location = newest.Entry.Location; }

            if (newest.Entry.Indices != null) { oldest.Entry.Indices = newest.Entry.Indices; }

            if (newest.Entry.CrowdChannels != null) { oldest.Entry.CrowdChannels = newest.Entry.CrowdChannels; }

            if (newest.Entry.Difficulties.Band >= 0) oldest.Entry.Difficulties.Band = newest.Entry.Difficulties.Band;
            if (newest.Entry.Difficulties.FiveFretGuitar >= 0) oldest.Entry.Difficulties.FiveFretGuitar = newest.Entry.Difficulties.FiveFretGuitar;
            if (newest.Entry.Difficulties.FiveFretBass >= 0) oldest.Entry.Difficulties.FiveFretBass = newest.Entry.Difficulties.FiveFretBass;
            if (newest.Entry.Difficulties.FiveFretRhythm >= 0) oldest.Entry.Difficulties.FiveFretRhythm = newest.Entry.Difficulties.FiveFretRhythm;
            if (newest.Entry.Difficulties.FiveFretCoop >= 0) oldest.Entry.Difficulties.FiveFretCoop = newest.Entry.Difficulties.FiveFretCoop;
            if (newest.Entry.Difficulties.Keys >= 0) oldest.Entry.Difficulties.Keys = newest.Entry.Difficulties.Keys;
            if (newest.Entry.Difficulties.FourLaneDrums >= 0) oldest.Entry.Difficulties.FourLaneDrums = newest.Entry.Difficulties.FourLaneDrums;
            if (newest.Entry.Difficulties.ProDrums >= 0) oldest.Entry.Difficulties.ProDrums = newest.Entry.Difficulties.ProDrums;
            if (newest.Entry.Difficulties.ProGuitar >= 0) oldest.Entry.Difficulties.ProGuitar = newest.Entry.Difficulties.ProGuitar;
            if (newest.Entry.Difficulties.ProBass >= 0) oldest.Entry.Difficulties.ProBass = newest.Entry.Difficulties.ProBass;
            if (newest.Entry.Difficulties.ProKeys >= 0) oldest.Entry.Difficulties.ProKeys = newest.Entry.Difficulties.ProKeys;
            if (newest.Entry.Difficulties.LeadVocals >= 0) oldest.Entry.Difficulties.LeadVocals = newest.Entry.Difficulties.LeadVocals;
            if (newest.Entry.Difficulties.HarmonyVocals >= 0) oldest.Entry.Difficulties.HarmonyVocals = newest.Entry.Difficulties.HarmonyVocals;

            AbridgedFileInfo? midi = null;
            AbridgedFileInfo? image = null;
            AbridgedFileInfo? mogg;
            AbridgedFileInfo? milo;
            // Unlike the above, we only ever want to use the newest data infos,
            // regardless of the last write dateTime for the dtas
            //
            // That being said, we'll still give preference to the newest instanced dta
            if (oldest.Entry.DiscUpdate)
            {
                midi = oldest.Midi == null || (newest.Midi != null && oldest.Midi.Value.LastUpdatedTime <= newest.Midi.Value.LastUpdatedTime)
                    ? newest.Midi
                    : oldest.Midi;
            }

            if (oldest.Entry.AlternatePath)
            {
                image = oldest.Image == null || (newest.Image != null && oldest.Image.Value.LastUpdatedTime <= newest.Image.Value.LastUpdatedTime)
                    ? newest.Image
                    : oldest.Image;
            }

            mogg = oldest.Mogg == null || (newest.Mogg != null && oldest.Mogg.Value.LastUpdatedTime <= newest.Mogg.Value.LastUpdatedTime)
                   ? newest.Mogg
                   : oldest.Mogg;

            milo = oldest.Milo == null || (newest.Milo != null && oldest.Milo.Value.LastUpdatedTime <= newest.Milo.Value.LastUpdatedTime)
                   ? newest.Milo
                   : oldest.Milo;
            return new SongUpdate(newest.UpdateTime, midi, mogg, milo, image, oldest.Entry);
        }

        public static void SkipRead(UnmanagedMemoryStream stream)
        {
            SkipInfo(stream);
            SkipInfo(stream);
            SkipInfo(stream);
            SkipInfo(stream);
        }

        private static void SkipInfo(UnmanagedMemoryStream stream)
        {
            if (stream.ReadBoolean())
            {
                stream.Position += CacheHandler.SIZEOF_DATETIME;
            }
        }
    }
}

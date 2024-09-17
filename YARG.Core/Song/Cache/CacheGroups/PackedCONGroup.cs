using System;
using System.Collections.Generic;
using System.IO;
using YARG.Core.IO;

namespace YARG.Core.Song.Cache
{
    public sealed class PackedCONGroup : CONGroup<PackedRBCONEntry>, IUpgradeGroup<PackedRBProUpgrade>
    {
        public readonly AbridgedFileInfo Info;
        public readonly CONFile ConFile;
        public readonly CONFileListing? SongDTA;
        public readonly CONFileListing? UpgradeDta;

        /// <summary>
        /// A reference to a pre-loaded pre-"using"ed filestream for the CON file.<br></br>
        /// For use with scanning new entries.
        /// </summary>
        public FileStream Stream;

        public override string Location => Info.FullName;
        public override Dictionary<string, DTAEntry> DTAEntries { get; }
        public Dictionary<string, (DTAEntry Entry, PackedRBProUpgrade? Upgrade)> Upgrades { get; }

        public PackedCONGroup(CONFile conFile, AbridgedFileInfo info, string defaultPlaylist)
            : base(defaultPlaylist)
        {
            const string SONGSFILEPATH = "songs/songs.dta";
            const string UPGRADESFILEPATH = "songs_upgrades/upgrades.dta";

            Info = info;
            ConFile = conFile;
            Stream = null!;

            using var stream = File.OpenRead(info.FullName);
            DTAEntries = conFile.TryGetListing(SONGSFILEPATH, out SongDTA)
                ? DTAEntry.LoadEntries(stream, SongDTA)
                : new Dictionary<string, DTAEntry>();

            Upgrades = new Dictionary<string, (DTAEntry Entry, PackedRBProUpgrade? Upgrade)>();
            if (conFile.TryGetListing(UPGRADESFILEPATH, out UpgradeDta))
            {
                foreach (var (name, entry) in DTAEntry.LoadEntries(stream, UpgradeDta))
                {
                    var upgrade = default(PackedRBProUpgrade);
                    if (conFile.TryGetListing(name, out var upgradeListing))
                    {
                        upgrade = new PackedRBProUpgrade(upgradeListing, upgradeListing.LastWrite);
                    }
                    Upgrades.Add(name, (entry, upgrade));
                }
            }
        }

        public override ReadOnlyMemory<byte> SerializeEntries(Dictionary<SongEntry, CategoryCacheWriteNode> nodes)
        {
            using MemoryStream ms = new();
            using BinaryWriter writer = new(ms);

            writer.Write(Location);
            writer.Write(SongDTA!.LastWrite.ToBinary());
            Serialize(writer, ref nodes);
            return new ReadOnlyMemory<byte>(ms.GetBuffer(), 0, (int)ms.Length);
        }

        public ReadOnlyMemory<byte> SerializeModifications()
        {
            using MemoryStream ms = new();
            using BinaryWriter writer = new(ms);

            writer.Write(Location);
            writer.Write(Info.LastUpdatedTime.ToBinary());
            writer.Write(UpgradeDta!.LastWrite.ToBinary());
            writer.Write(Upgrades.Count);
            foreach (var upgrade in Upgrades)
            {
                writer.Write(upgrade.Key);
                if (upgrade.Value.Upgrade != null)
                {
                    writer.Write(true);
                    writer.Write(upgrade.Value.Upgrade.LastUpdatedTime.ToBinary());
                }
                else
                {
                    writer.Write(false);
                }
            }
            return new ReadOnlyMemory<byte>(ms.GetBuffer(), 0, (int)ms.Length);
        }
    }
}

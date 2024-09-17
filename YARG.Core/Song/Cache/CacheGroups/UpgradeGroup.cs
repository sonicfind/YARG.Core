using System;
using System.Collections.Generic;
using System.IO;
using YARG.Core.IO;

namespace YARG.Core.Song.Cache
{
    public interface IUpgradeGroup<TUpgrade> : IModificationGroup
        where TUpgrade : RBProUpgrade
    {
        public Dictionary<string, (DTAEntry Entry, TUpgrade? Upgrade)> Upgrades { get; }
    }

    public sealed class UpgradeGroup : IUpgradeGroup<UnpackedRBProUpgrade>
    {
        private readonly string _directory;
        private readonly DateTime _dtaLastUpdate;

        public Dictionary<string, (DTAEntry Entry, UnpackedRBProUpgrade? Upgrade)> Upgrades { get; }

        public UpgradeGroup(in FileCollection collection, FileInfo dta)
        {
            _directory = collection.Directory.FullName;
            _dtaLastUpdate = dta.LastWriteTime;

            Upgrades = new Dictionary<string, (DTAEntry Entry, UnpackedRBProUpgrade? Upgrade)>();
            foreach (var (name, entry) in DTAEntry.LoadEntries(dta.FullName))
            {
                var upgrade = default(UnpackedRBProUpgrade);
                if (collection.Subfiles.TryGetValue($"{name.ToLower()}_plus.mid", out var info))
                {
                    var abridged = new AbridgedFileInfo(info, false);
                    upgrade = new UnpackedRBProUpgrade(abridged);
                }
                Upgrades.Add(name, (entry, upgrade));
            }
        }

        public ReadOnlyMemory<byte> SerializeModifications()
        {
            using MemoryStream ms = new();
            using BinaryWriter writer = new(ms);

            writer.Write(_directory);
            writer.Write(_dtaLastUpdate.ToBinary());
            writer.Write(Upgrades.Count);
            foreach (var upgrade in Upgrades)
            {
                writer.Write(upgrade.Key);
                if (upgrade.Value.Upgrade != null)
                {
                    writer.Write(true);
                    upgrade.Value.Upgrade.WriteToCache(writer);
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

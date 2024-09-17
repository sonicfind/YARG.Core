using System;
using System.Collections.Generic;
using System.IO;
using YARG.Core.IO;

namespace YARG.Core.Song.Cache
{
    public sealed class UnpackedCONGroup : CONGroup<UnpackedRBCONEntry>
    {
        public readonly AbridgedFileInfo DTAInfo;

        public override string Location { get; }
        public override Dictionary<string, DTAEntry> DTAEntries { get; }

        public UnpackedCONGroup(string directory, FileInfo dta, string defaultPlaylist)
            : base(defaultPlaylist)
        {
            Location = directory;
            DTAInfo = new AbridgedFileInfo(dta);
            DTAEntries = DTAEntry.LoadEntries(dta.FullName);
        }

        public override ReadOnlyMemory<byte> SerializeEntries(Dictionary<SongEntry, CategoryCacheWriteNode> nodes)
        {
            using MemoryStream ms = new();
            using BinaryWriter writer = new(ms);

            writer.Write(Location);
            writer.Write(DTAInfo.LastUpdatedTime.ToBinary());
            Serialize(writer, ref nodes);
            return new ReadOnlyMemory<byte>(ms.GetBuffer(), 0, (int)ms.Length);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using YARG.Core.IO;

namespace YARG.Core.Song.Cache
{
    public abstract class CONGroup<TEntry> : ICacheGroup<RBCONEntry>
        where TEntry : RBCONEntry
    {
        public readonly Dictionary<string, TEntry> SongEntries = new();
        public int Count => SongEntries.Count;

        public readonly string DefaultPlaylist;

        public abstract string Location { get; }
        public abstract Dictionary<string, DTAEntry> DTAEntries { get; }

        protected CONGroup(string defaultPlaylist)
        {
            DefaultPlaylist = defaultPlaylist;
        }

        public abstract ReadOnlyMemory<byte> SerializeEntries(Dictionary<SongEntry, CategoryCacheWriteNode> nodes);

        public bool TryRemoveEntry(SongEntry entryToRemove)
        {
            // No locking as the post-scan removal sequence
            // cannot be parallelized
            foreach (var entry in SongEntries)
            {
                if (ReferenceEquals(entry.Value, entryToRemove))
                {
                    SongEntries.Remove(entry.Key);
                    return true;
                }
            }
            return false;
        }

        protected void Serialize(BinaryWriter writer, ref Dictionary<SongEntry, CategoryCacheWriteNode> nodes)
        {
            using var ms = new MemoryStream();
            using var entryWriter = new BinaryWriter(ms);
            writer.Write(SongEntries.Count);
            foreach (var entry in SongEntries)
            {
                ms.SetLength(0);

                writer.Write(entry.Key);
                entry.Value.Serialize(entryWriter, nodes[entry.Value]);

                writer.Write((int) ms.Length);
                writer.Write(ms.GetBuffer(), 0, (int)ms.Length);
            }
        }
    }
}

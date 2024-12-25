using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace YARG.Core.Song.Cache
{
    public readonly struct FileCollection
    {
        public readonly DirectoryInfo Directory;
        public readonly Dictionary<string, FileSystemInfo> Entries;
        public readonly bool ContainedDupes;

        internal static bool TryCollect(string directory, out FileCollection collection)
        {
            var info = new DirectoryInfo(directory);
            if (!info.Exists)
            {
                collection = default;
                return false;
            }
            collection = new FileCollection(info);
            return true;
        }

        internal FileCollection(DirectoryInfo directory)
        {
            Directory = directory;
            Entries = new Dictionary<string, FileSystemInfo>(StringComparer.InvariantCultureIgnoreCase);
            var dupes = new HashSet<string>();

            foreach (var entry in directory.EnumerateFileSystemInfos())
            {
                if (!Entries.TryAdd(entry.Name, entry))
                {
                    dupes.Add(entry.Name);
                }
            }

            // Removes any sort of ambiguity from duplicates
            ContainedDupes = dupes.Count > 0;
            foreach (var dupe in dupes)
            {
                Entries.Remove(dupe);
            }
        }

        public bool FindFile(string name, out FileInfo file)
        {
            if (Entries.TryGetValue(name, out var entry) && entry is FileInfo result)
            {
                file = result;
                return true;
            }
            file = null!;
            return false;
        }

        public bool FindDirectory(string name, out DirectoryInfo directory)
        {
            if (Entries.TryGetValue(name, out var entry) && entry is DirectoryInfo result)
            {
                directory = result;
                return true;
            }
            directory = null!;
            return false;
        }

        public bool ContainsDirectory()
        {
            foreach (var entry in Entries)
            {
                if (entry.Value.Attributes == FileAttributes.Directory)
                {
                    return true;
                }
            }
            return false;
        }

        public bool ContainsAudio()
        {
            foreach (var entry in Entries)
            {
                if (IniAudio.IsAudioFile(entry.Key))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using YARG.Core.Extensions;
using YARG.Core.IO.Ini;

namespace YARG.Core.IO
{

    /// <summary>
    /// <see href="https://github.com/mdsitton/SngFileFormat">Documentation of SNG file type</see>
    /// </summary>
    public class SngFile : IDisposable, IEnumerable<KeyValuePair<string, SngFileListing>>
    {
        public readonly uint Version;
        public readonly SngMask Mask;
        public readonly IniSection Metadata;
        private readonly Dictionary<string, SngFileListing> listings;

        private SngFile(uint version, byte[] mask, IniSection metadata, Dictionary<string, SngFileListing> listings)
        {
            Version = version;
            Mask = new SngMask(mask);
            Metadata = metadata;
            this.listings = listings;
        }

        public SngFileListing this[string key] => listings[key];
        public bool ContainsKey(string key) => listings.ContainsKey(key);
        public bool TryGetValue(string key, out SngFileListing listing) => listings.TryGetValue(key, out listing);

        IEnumerator<KeyValuePair<string, SngFileListing>> IEnumerable<KeyValuePair<string, SngFileListing>>.GetEnumerator()
        {
            return listings.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return listings.GetEnumerator();
        }

        public void Dispose()
        {
            Mask.Dispose();
        }


        private const int XORMASK_SIZE = 16;
        private const int BYTES_64BIT = 8;
        private const int BYTES_32BIT = 4;
        private const int BYTES_24BIT = 3;
        private const int BYTES_16BIT = 2;
        private static readonly byte[] SNGPKG = { (byte)'S', (byte) 'N', (byte) 'G', (byte)'P', (byte)'K', (byte)'G' };

        public static SngFile? TryLoadFile(string filename)
        {
            using var stream = InitStream_Internal(filename);
            if (stream == null)
                return null;

            {
                Span<byte> tag = stackalloc byte[SNGPKG.Length];
                if (stream.Read(tag) != tag.Length)
                    return null;

                if (!tag.SequenceEqual(SNGPKG))
                    return null;
            }

            try
            {
                uint version = stream.ReadLE<uint>();
                var xorMask = stream.ReadBytes(XORMASK_SIZE);
                var metadata = ReadMetadata(stream);
                var listings = ReadListings(stream);
                return new SngFile(version, xorMask, metadata, listings);
            }
            catch (Exception ex)
            {
                YargTrace.LogException(ex, $"Error loading {filename}");
                return null;
            }
        }

        private static FileStream? InitStream_Internal(string filename)
        {
            try
            {
                return new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception ex)
            {
                YargTrace.LogException(ex, $"Error loading {filename}");
                return null;
            }
        }

        private static IniSection ReadMetadata(FileStream stream)
        {
            Dictionary<string, List<IniModifier>> modifiers = new();
            ulong sectionLength = stream.ReadLE<ulong>() - sizeof(ulong);
            ulong numPairs = stream.ReadLE<ulong>();

            var validNodes = SongIniHandler.SONG_INI_DICTIONARY["[song]"];
            using var buffer = DisposableArray<byte>.Create(stream, (int)sectionLength);
            using YARGTextContainer<byte> text = new(buffer);
            for (ulong i = 0; i < numPairs; i++)
            {
                unsafe
                {
                    int strLength = ParseStrLength(text);
                    var key = Encoding.UTF8.GetString(text.Position, strLength);
                    text.Position = text.Next;

                    strLength = ParseStrLength(text);
                    if (validNodes.TryGetValue(key, out var node))
                    {
                        var mod = node.CreateSngModifier(text);
                        if (modifiers.TryGetValue(node.outputName, out var list))
                            list.Add(mod);
                        else
                            modifiers.Add(node.outputName, new() { mod });
                    }
                    text.Position = text.Next;
                }
            }
            return new IniSection(modifiers);
        }

        private static Dictionary<string, SngFileListing> ReadListings(FileStream stream)
        {
            ulong length = stream.ReadLE<ulong>() - sizeof(ulong);
            ulong numListings = stream.ReadLE<ulong>();

            Dictionary<string, SngFileListing> listings = new((int)numListings);

            using var reader = new YARGBinaryReader(stream, (long)length);
            for (ulong i = 0; i < numListings; i++)
            {
                var strLen = reader.ReadByte();
                string filename = Encoding.UTF8.GetString(reader.ReadSpan(strLen));
                int idx = filename.LastIndexOf('/');
                if (idx != -1)
                    filename = filename[idx..];
                listings.Add(filename.ToLower(), new SngFileListing(reader));
            }
            return listings;
        }
        
        private static int ParseStrLength(YARGTextContainer<byte> container)
        {
            unsafe
            {
                var pos = container.Position;
                container.Position += sizeof(int);
                if (container.Position <= container.End)
                {
                    int length = Unsafe.AsRef<int>(pos);
                    container.Next = container.Position + length;
                    if (container.Next <= container.End)
                        return length;
                }
                throw new EndOfStreamException();
            }
        }
    }
}

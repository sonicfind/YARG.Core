using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using YARG.Core.Extensions;

namespace YARG.Core.IO
{
    public class SngFileListing
    {
        public readonly long Position;
        public readonly long Length;

        public SngFileListing(YARGBinaryReader reader)
        {
            Length = reader.Read<long>();
            Position = reader.Read<long>();
        }

        /// <summary>
        /// Returns a array of bytes that represents the untangled data of a SNG subfile
        /// </summary>
        /// <param name="filename">The path to SNG file to load</param>
        /// <param name="mask">The decryption keys to use</param>
        /// <returns>The untangled file data in bytes</returns>
        public DisposableArray<byte> LoadAllBytes(string filename, SngMask mask)
        {
            return SngFileStream.LoadFile(filename, Length, Position, mask.Clone());
        }

        /// <summary>
        /// Returns a stream that handles the untangling of the listing's data within a given SNG
        /// </summary>
        /// <param name="filename">The path to SNG file to load</param>
        /// <param name="mask">The decryption keys to use</param>
        /// <returns>A stream that points to the subfile within a CON</returns>
        public SngFileStream CreateStream(string filename, SngMask mask)
        {
            return new SngFileStream(filename, Length, Position, mask.Clone());
        }
    }
}

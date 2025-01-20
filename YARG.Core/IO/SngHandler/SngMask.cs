using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace YARG.Core.IO
{
    /// <summary>
    /// Handles the buffer of decryption keys, while also providing easy access
    /// to SIMD vector operations through pointers and fixed array behavior.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 32)]
    public static class SngMask
    {
        public const int MASK_SIZE = 256;
        public static readonly int NUM_VECTORS = MASK_SIZE / Vector<byte>.Count;
        // For vectorization, we want to copy the bytes of the original mask in a repeated manner.
        // Outside testing shows that loops thaat span 16 iterations at once provide the greatest performance.
        // For that, the mask needs to extend past the normal bounds to decrypt a block of that size.
        public static readonly int SNG_MASK_MULTIPLIER = 1 + (Vector<byte>.Count / 16);

        public static FixedArray<byte> LoadMask(Stream stream)
        {
            const int MASKLENGTH = 16;
            Span<byte> keys = stackalloc byte[MASKLENGTH];
            if (stream.Read(keys) < keys.Length)
            {
                throw new EndOfStreamException("Unable to read SNG mask");
            }

            var mask = FixedArray<byte>.Alloc(MASK_SIZE * SNG_MASK_MULTIPLIER);
            for (int i = 0; i < SngMask.MASK_SIZE; ++i)
            {
                mask[i] = (byte) (keys[i % MASKLENGTH] ^ i);
            }

            for (int offset = MASK_SIZE; offset < mask.Length; offset += MASK_SIZE)
            {
                unsafe
                {
                    Unsafe.CopyBlock(mask.Ptr + offset, mask.Ptr, MASK_SIZE);
                }
            }
            return mask;
        }
    }
}

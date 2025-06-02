using System;
using System.Diagnostics;
using YARG.Core.IO;
using YARG.Core.NewLoading.Guitar;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    [DebuggerDisplay("Length: {Length}")]
    public class TimedCollection<T> : IDisposable
        where T : unmanaged
    {
        public static TimedCollection<T> Create(int count)
        {
            return new TimedCollection<T>
            {
                Ticks = FixedArray<long>.Alloc(count),
                Seconds = FixedArray<double>.Alloc(count),
                Elements = FixedArray<T>.Alloc(count),
            };
        }

        public FixedArray<long>   Ticks    { get; private set; } = null!;
        public FixedArray<double> Seconds  { get; private set; } = null!;
        public FixedArray<T>      Elements { get; private set; } = null!;

        public int Length => Elements.Length;

        public ref T this[int index] => ref Elements[index];

        private TimedCollection() { }

        public TimedCollection<T> Clone()
        {
            return new TimedCollection<T>
            {
                Ticks = Ticks.Clone(),
                Seconds = Seconds.Clone(),
                Elements = Elements.Clone(),
            };
        }

        public TimedCollection<T> TransferOwnership()
        {
            return new TimedCollection<T>
            {
                Ticks = Ticks.TransferOwnership(),
                Seconds = Seconds.TransferOwnership(),
                Elements = Elements.TransferOwnership(),
            };
        }

        public void Resize(int count)
        {
            Ticks.Resize(count);
            Seconds.Resize(count);
            Elements.Resize(count);
        }

        public void ReplaceFrom(TimedCollection<T> source, int replaceIndex, int sourceIndex)
        {
            ReplaceFrom(source, replaceIndex, Length - replaceIndex, sourceIndex, source.Length - sourceIndex);
        }

        public unsafe void ReplaceFrom(TimedCollection<T> source, int replaceIndex, int replaceCount, int sourceIndex, int sourceCount)
        {
            // Have to create a new buffer to combine the two if the size changes
            if (sourceCount != replaceCount)
            {
                int newSize = Elements.Length + sourceCount - replaceCount;

                using var newTicks = FixedArray<long>.Alloc(newSize);
                using var newSeconds = FixedArray<double>.Alloc(newSize);
                using var newElements = FixedArray<T>.Alloc(newSize);

                // Copy the pre-update data
                int positionBytes = replaceIndex * sizeof(long); // Same size for double
                int elementBytes = replaceIndex * sizeof(T);

                Buffer.MemoryCopy(Ticks.Ptr, newTicks.Ptr, positionBytes, positionBytes);
                Buffer.MemoryCopy(Seconds.Ptr, newSeconds.Ptr, positionBytes, positionBytes);
                Buffer.MemoryCopy(Elements.Ptr, newElements.Ptr, elementBytes, elementBytes);

                // Copy the post-update data
                int leftoverIndex = replaceIndex + replaceCount;
                int leftoverCount = Elements.Length - leftoverIndex;

                positionBytes = leftoverCount * sizeof(long); // Same size for double
                elementBytes = leftoverCount * sizeof(T);

                int copyIndex = replaceIndex + sourceCount;

                Buffer.MemoryCopy(Ticks.Ptr + leftoverIndex, newTicks.Ptr + copyIndex, positionBytes, positionBytes);
                Buffer.MemoryCopy(Seconds.Ptr + leftoverIndex, newSeconds.Ptr + copyIndex, positionBytes, positionBytes);
                Buffer.MemoryCopy(Elements.Ptr + leftoverIndex, newElements.Ptr + copyIndex, elementBytes, elementBytes);

                // Replace the arrays
                Ticks.Dispose();
                Seconds.Dispose();
                Elements.Dispose();

                Ticks = newTicks.TransferOwnership();
                Seconds = newSeconds.TransferOwnership();
                Elements = newElements.TransferOwnership();
            }

            int positionCopyBytes = sourceCount * sizeof(long); // Same size for double
            int elementCopyBytes = sourceCount * sizeof(T);

            Buffer.MemoryCopy(
                source.Ticks.Ptr + sourceIndex,
                Ticks.Ptr + replaceIndex,
                positionCopyBytes,
                positionCopyBytes
            );

            Buffer.MemoryCopy(
                source.Seconds.Ptr + sourceIndex,
                Seconds.Ptr + replaceIndex,
                positionCopyBytes,
                positionCopyBytes
            );

            Buffer.MemoryCopy(
                source.Elements.Ptr + sourceIndex,
                Elements.Ptr + replaceIndex,
                elementCopyBytes,
                elementCopyBytes
            );
        }

        public int GetBestPositionIndex(double seconds, int lo = 0, int hi = int.MaxValue)
        {
            if (lo < 0)
            {
                lo = 0;
            }

            if (hi > Seconds.Length)
            {
                hi = Seconds.Length;
            }
            hi--;

            while (lo <= hi)
            {
                int curr = (hi + lo) >> 1;
                int order = Seconds[curr].CompareTo(seconds);
                if (order == 0)
                {
                    return curr;
                }

                if (order < 0)
                {
                    lo = curr + 1;
                }
                else
                {
                    hi = curr - 1;
                }
            }
            return lo;
        }

        public void Dispose()
        {
            Ticks.Dispose();
            Seconds.Dispose();
            Elements.Dispose();
        }
    }

    public static class FixedArrayReplacer
    {
        public static unsafe void ReplaceFrom<T>(
            ref FixedArray<T> original,
            FixedArray<T> source,
            int originalIndex, int originalCount,
            int sourceIndex, int sourceCount
        )
            where T : unmanaged
        {
            // Have to create a new buffer to combine the two if the size changes
            if (sourceCount != originalCount)
            {
                using var newElements = FixedArray<T>.Alloc(original.Length + sourceCount - originalCount);

                // Copy the pre-update data
                int elementBytes = originalIndex * sizeof(T);

                Buffer.MemoryCopy(original.Ptr, newElements.Ptr, elementBytes, elementBytes);

                // Copy the post-update data
                int leftoverIndex = originalIndex + originalCount;

                elementBytes = (original.Length - leftoverIndex) * sizeof(T);

                Buffer.MemoryCopy(original.Ptr + leftoverIndex, newElements.Ptr + (originalIndex + sourceCount), elementBytes, elementBytes);

                // Replace the array
                original.Dispose();
                original = newElements.TransferOwnership();
            }

            int elementCopyBytes = sourceCount * sizeof(T);

            Buffer.MemoryCopy(source.Ptr + sourceIndex, original.Ptr + originalIndex, elementCopyBytes, elementCopyBytes);
        }
    }
}
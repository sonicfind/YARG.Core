using System;

namespace YARG.Core.NewParsing
{
    public interface ITrack : IDisposable
    {
        /// <summary>
        /// Returns whether the track contains no data
        /// </summary>
        /// <returns>Whether the track is empty</returns>
        bool IsEmpty();

        /// <summary>
        /// Clears all data
        /// </summary>
        void Clear();

        /// <summary>
        /// Shrinks unmanaged data buffers to solely cover actual data
        /// </summary>
        void TrimExcess();
    }
}

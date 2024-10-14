using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public interface ITrack : IDisposable
    {
        /// <summary>
        /// Returns whether the track contains no data
        /// </summary>
        /// <returns>Whether the track is empty</returns>
        public bool IsEmpty();

        /// <summary>
        /// Clears all data
        /// </summary>
        public void Clear();

        /// <summary>
        /// Shrinks unmanaged data buffers to solely cover actual data
        /// </summary>
        public void TrimExcess();

        /// <summary>
        /// Returns the end time for any notes present
        /// </summary>
        /// <returns>The tick and seconds position of the end time</returns>
        public void UpdateLastNoteTime(ref DualTime LastNoteTime);

        public void Dispose(bool dispose);
    }
}

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
    }
}

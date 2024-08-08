using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public interface ITrack : IDisposable
    {
        public bool IsEmpty();
        public void Clear();
        public void TrimExcess();
        public DualTime GetLastNoteTime();
    }
}

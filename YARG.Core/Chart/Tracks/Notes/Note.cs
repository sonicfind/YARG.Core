using System;
using System.Runtime.InteropServices;

namespace YARG.Core.Chart
{
    public abstract unsafe class Note_FW<NoteType> : INote
        where NoteType : unmanaged, IEnableable
    {
        protected readonly NoteType* lanes;
        protected readonly int numLanes;

        protected ref NoteType Get(int index)
        {
            if (index >= numLanes)
                throw new ArgumentOutOfRangeException("index");
            return ref lanes[index];
        }

        protected Note_FW(int numLanes)
        {
            lanes = (NoteType*) Marshal.AllocHGlobal(numLanes * sizeof(NoteType));
            this.numLanes = numLanes;
        }

        ~Note_FW()
        {
            Marshal.FreeHGlobal((IntPtr)lanes);
        }

        public virtual int GetNumActive()
        {
            int num = 0;
            for (int i = 0; i < numLanes; ++i)
                if (lanes[i].IsActive())
                    ++num;
            return num;
        }

        public virtual bool HasActiveNotes()
        {
            for (int i = 0; i < numLanes; ++i)
                if (lanes[i].IsActive())
                    return true;
            return false;
        }

        public virtual long GetLongestSustain()
        {
            long sustain = 0;
            for (int i = 0; i < numLanes; ++i)
            {
                long end = lanes[i].Duration;
                if (end > sustain)
                    sustain = end;
            }
            return sustain;
        }
    }
}

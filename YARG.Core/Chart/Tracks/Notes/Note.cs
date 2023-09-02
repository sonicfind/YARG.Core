namespace YARG.Core.Chart
{
    public abstract unsafe class Note_FW<NoteType> : INote
        where NoteType : unmanaged, IEnableable
    {
        protected readonly NoteType[] lanes;
        protected readonly int numLanes;

        protected Note_FW(int numLanes)
        {
            lanes = new NoteType[numLanes];
            this.numLanes = numLanes;
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

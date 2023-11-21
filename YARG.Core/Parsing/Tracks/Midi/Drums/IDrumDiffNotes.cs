using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.Parsing.Midi.Drums
{
    public interface IDrumDiffNotes
    {
        public ref long this[int index] { get; }
    }

    public struct FourLaneDiff : IDrumDiffNotes
    {
        private unsafe fixed long notes[6];

        public unsafe ref long this[int index] => ref notes[index];
    }

    public struct FiveLaneDiff : IDrumDiffNotes
    {
        private unsafe fixed long notes[7];
        public unsafe ref long this[int index] => ref notes[index];
    }

    public class DrumsMidiDiff<TConfig>
        where TConfig : unmanaged, IDrumDiffNotes
    {
        public bool Flam { get; set; }
        public readonly TConfig notes;

        public DrumsMidiDiff()
        {
            unsafe
            {
                int numnotes = sizeof(TConfig) / sizeof(long);
                for (int i = 0; i < numnotes; i++)
                    notes[i] = -1;
            }
        }
    }
}

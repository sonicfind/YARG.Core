using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Chart;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    public class MidiProDrumsLoader : MidiProDrumsLoader_Base<FourLane<DrumPad_Pro>, FourLaneDifficulty>
    {
        public static BasicInstrumentTrack2<DrumNote2<FourLane<DrumPad_Pro>, DrumPad_Pro>> Load(YARGMidiTrack midiTrack, SyncTrack2 sync, HashSet<Difficulty>? difficulties)
        {
            var loader = new MidiProDrumsLoader(difficulties);
            return loader.Process(midiTrack, sync);
        }

        private MidiProDrumsLoader(HashSet<Difficulty>? difficulties)
            : base(difficulties, DrumsType.ProDrums) { }
    }
}

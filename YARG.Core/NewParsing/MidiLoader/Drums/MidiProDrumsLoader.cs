using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Chart;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    public static class MidiProDrumsLoader
    {
        public static BasicInstrumentTrack2<ProDrumNote2<FourLane>> Load(YARGMidiTrack midiTrack, SyncTrack2 sync, HashSet<Difficulty>? difficulties)
        {
            var loader = new MidiProDrumsLoader_Base<FourLane, FourLaneDifficulty>(difficulties, DrumsType.ProDrums);
            return loader.Process(midiTrack, sync);
        }
    }
}

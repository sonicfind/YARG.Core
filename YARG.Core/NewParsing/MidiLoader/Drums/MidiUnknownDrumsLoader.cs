using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Chart;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    public class MidiUnkownDrumsLoader : MidiProDrumsLoader_Base<FiveLane<DrumPad_Pro>, FiveLaneDifficulty>
    {
        public static BasicInstrumentTrack2<DrumNote2<FiveLane<DrumPad_Pro>, DrumPad_Pro>> Load(YARGMidiTrack midiTrack, SyncTrack2 sync, HashSet<Difficulty>? difficulties, ref DrumsType type)
        {
            var loader = new MidiUnkownDrumsLoader(difficulties, type);
            var track = loader.Process(midiTrack, sync);
            type = loader._type switch
            {
                DrumsType.Unknown => DrumsType.FourLane,
                DrumsType.UnknownPro => DrumsType.ProDrums,
                _ => loader._type
            };
            return track;
        }

        private MidiUnkownDrumsLoader(HashSet<Difficulty>? difficulties, DrumsType type)
            : base(difficulties, type) { }
    }
}

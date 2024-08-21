using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Game;
using YARG.Core.NewLoading.Players;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading.Guitar
{
    public class SixFretPlayer : InstrumentPlayer<SixFretGuitar, GuitarPlayerLoader.Note>
    {
        private readonly GuitarParams _settings;
        private readonly InstrumentTrack2<DifficultyTrack2<FiveFretGuitar>>? _fiveFretTrack;

        public SixFretPlayer(in GuitarParams settings
                            , InstrumentTrack2<DifficultyTrack2<SixFretGuitar>>? sixFret
                            , InstrumentTrack2<DifficultyTrack2<FiveFretGuitar>>? fiveFret
                            , SyncTrack2 sync, YargProfile profile)
            : base(sixFret, sync, profile)
        {
            _fiveFretTrack = fiveFret;
            _settings = settings;
        }

        public override unsafe void Set(in DualTime startTime, in DualTime endTime)
        {
            (_notes, _overdrives, _soloes) = _track != null
                ? GuitarPlayerLoader.Load(_track, Profile, in _settings, in startTime, in endTime)
                : GuitarPlayerLoader.Load(_fiveFretTrack!, Profile, in _settings, in startTime, in endTime);
        }
    }
}

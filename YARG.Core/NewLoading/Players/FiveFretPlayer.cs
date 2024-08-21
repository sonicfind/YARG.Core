using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using YARG.Core.Game;
using YARG.Core.NewLoading.Players;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading.Guitar
{
    public class FiveFretPlayer : InstrumentPlayer<FiveFretGuitar, GuitarPlayerLoader.Note>
    {
        private readonly GuitarParams _settings;
        public FiveFretPlayer(in GuitarParams settings, InstrumentTrack2<DifficultyTrack2<FiveFretGuitar>> track, SyncTrack2 sync, YargProfile profile)
            : base(track, sync, profile)
        {
            _settings = settings;
        }

        public override unsafe void Set(in DualTime startTime, in DualTime endTime)
        {
            (_notes, _overdrives, _soloes) = GuitarPlayerLoader.Load(_track!, Profile, in _settings, in startTime, in endTime);
        }
    }
}

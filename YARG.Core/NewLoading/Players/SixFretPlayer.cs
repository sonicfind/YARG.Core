using YARG.Core.Game;
using YARG.Core.NewLoading.Players;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading.Guitar
{
    public class SixFretPlayer : InstrumentPlayer
    {
        private readonly GuitarPlayerLoader.Note[] _notes;
        private int _noteIndex = -1;

        public SixFretPlayer(InstrumentTrack2<DifficultyTrack2<SixFretGuitar>> track, long hopoThreshold, bool allowHopoAfterChord, SyncTrack2 sync, YargProfile profile)
            : base(sync, profile)
        {
            (_notes, _overdrives, _soloes) = GuitarPlayerLoader.Load(track, Profile, hopoThreshold, allowHopoAfterChord);
        }

        public SixFretPlayer(InstrumentTrack2<DifficultyTrack2<FiveFretGuitar>> track, long hopoThreshold, bool allowHopoAfterChord, SyncTrack2 sync, YargProfile profile)
            : base(sync, profile)
        {
            (_notes, _overdrives, _soloes) = GuitarPlayerLoader.Load(track, Profile, hopoThreshold, allowHopoAfterChord);
        }

        public override unsafe void Set(in DualTime startTime, in DualTime endTime)
        {
        }
    }
}

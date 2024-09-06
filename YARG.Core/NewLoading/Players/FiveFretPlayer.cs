using YARG.Core.Game;
using YARG.Core.NewLoading.Players;
using YARG.Core.NewParsing;
using YARG.Core.Song;

namespace YARG.Core.NewLoading.Guitar
{
    public class FiveFretPlayer : InstrumentPlayer
    {
        private readonly GuitarPlayerLoader.Note[] _notes;
        private int _noteIndex = -1;

        public FiveFretPlayer(InstrumentTrack2<DifficultyTrack2<FiveFretGuitar>> track, in LoaderSettings settings, SyncTrack2 sync, YargProfile profile)
        : base(sync, profile)
        {
            (_notes, _overdrives, _soloes) = GuitarPlayerLoader.Load(track, Profile, in settings);
        }

        public override unsafe void Set(in DualTime startTime, in DualTime endTime)
        {
        }
    }
}

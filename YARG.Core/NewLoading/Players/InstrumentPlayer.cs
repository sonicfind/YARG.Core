using YARG.Core.Game;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading.Players
{
    public abstract class InstrumentPlayer : BasePlayer
    {
        protected SoloPhrase[] _soloes;
        protected int _soloIndex;

        protected InstrumentPlayer(SyncTrack2 sync, YargProfile profile)
            : base(sync, profile)
        {
            _soloes = null!;
        }
    }
}

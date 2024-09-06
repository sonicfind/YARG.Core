using System;
using YARG.Core.Game;
using YARG.Core.IO;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public abstract class BasePlayer : IDisposable
    {
        protected readonly FixedArray<OverdrivePhrase> _overdrives;
        protected int _overdriveIndex = 0;

        public readonly YargProfile Profile;
        public readonly SyncTrack2 Sync;
        public string Name;

        protected BasePlayer(FixedArray<OverdrivePhrase> overdrives, SyncTrack2 sync, YargProfile profile)
        {
            Sync = sync;
            Profile = profile;
            Name = profile.Name;
            _overdrives = overdrives;
        }

        public virtual void Dispose()
        {
            _overdrives.Dispose();
        }
    }
}

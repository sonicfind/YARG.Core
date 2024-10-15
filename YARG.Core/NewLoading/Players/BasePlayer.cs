using System;
using YARG.Core.Game;
using YARG.Core.IO;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public abstract class BasePlayer : IDisposable
    {
        protected readonly FixedArray<OverdrivePhrase> _overdrives;

        public readonly YargProfile Profile;
        public readonly SyncTrack2 Sync;
        public string Name;

        public virtual long NativeMemoryUsage => _overdrives.ByteCount;

        protected BasePlayer(in FixedArray<OverdrivePhrase> overdrives, SyncTrack2 sync, YargProfile profile)
        {
            Sync = sync;
            Profile = profile;
            Name = profile.Name;
            _overdrives = overdrives;
        }

        protected virtual void _Dispose()
        {
            _overdrives.Dispose();
        }

        public void Dispose()
        {
            _Dispose();
            GC.SuppressFinalize(this);
        }

        ~BasePlayer()
        {
            _Dispose();
        }
    }
}

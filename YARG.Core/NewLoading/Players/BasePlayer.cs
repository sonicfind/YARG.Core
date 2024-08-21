using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Game;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public abstract class BasePlayer
    {
        protected OverdrivePhrase[] _overdrives;
        protected int _overdriveIndex;

        public readonly YargProfile Profile;
        public readonly SyncTrack2 Sync;
        public string Name;

        protected BasePlayer(SyncTrack2 sync, YargProfile profile)
        {
            Sync = sync;
            Profile = profile;
            Name = profile.Name;
            _overdrives = null!;
        }

        public abstract void Set(in DualTime startTime, in DualTime endTime);
    }
}

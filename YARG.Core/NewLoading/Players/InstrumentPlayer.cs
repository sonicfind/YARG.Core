using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Game;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading.Players
{
    public abstract class InstrumentPlayer<TNote, TEngineNote> : BasePlayer
        where TNote : unmanaged, IInstrumentNote
        where TEngineNote : struct
    {
        protected readonly InstrumentTrack2<DifficultyTrack2<TNote>>? _track;

        protected TEngineNote[] _notes;
        protected SoloPhrase[] _soloes;
        protected int _noteIndex;
        protected int _soloIndex;

        protected InstrumentPlayer(InstrumentTrack2<DifficultyTrack2<TNote>>? track, SyncTrack2 sync, YargProfile profile)
            : base(sync, profile)
        {
            _track = track;
            _notes = null!;
            _soloes = null!;
        }
    }
}

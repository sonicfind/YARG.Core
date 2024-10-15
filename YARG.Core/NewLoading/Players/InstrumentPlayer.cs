using YARG.Core.Containers;
using YARG.Core.Game;
using YARG.Core.IO;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading.Players
{
    public sealed class InstrumentPlayer<TNote, TSubNote> : BasePlayer
        where TNote : unmanaged
        where TSubNote : unmanaged
    {
        private readonly YARGNativeList<TSubNote> _subNoteBuffer;
        private readonly YARGNativeSortedList<DualTime, TNote> _notes;
        private readonly FixedArray<SoloPhrase> _soloes;

        public override long NativeMemoryUsage =>
            base.NativeMemoryUsage
            + _subNoteBuffer.MemoryUsage
            + _notes.MemoryUsage
            + _soloes.ByteCount;

        public InstrumentPlayer(in YARGNativeSortedList<DualTime, TNote> notes, in YARGNativeList<TSubNote> subNotes, in FixedArray<SoloPhrase> soloes, in FixedArray<OverdrivePhrase> overdrives, SyncTrack2 sync, YargProfile profile)
            : base(in overdrives, sync, profile)
        {
            _notes = notes;
            _subNoteBuffer = subNotes;
            _soloes = soloes;
        }

        protected override void _Dispose()
        {
            _subNoteBuffer.Dispose();
            _notes.Dispose();
            _soloes.Dispose();
            base._Dispose();
        }
    }
}

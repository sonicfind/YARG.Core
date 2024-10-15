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
        public readonly YARGNativeList<TSubNote> SubNoteBuffer;
        public readonly YARGNativeSortedList<DualTime, TNote> Notes;
        public readonly FixedArray<SoloPhrase> Soloes;

        public override long NativeMemoryUsage =>
            base.NativeMemoryUsage
            + SubNoteBuffer.MemoryUsage
            + Notes.MemoryUsage
            + Soloes.ByteCount;

        public InstrumentPlayer(in YARGNativeSortedList<DualTime, TNote> notes, in YARGNativeList<TSubNote> subNotes, in FixedArray<SoloPhrase> soloes, in FixedArray<OverdrivePhrase> overdrives, SyncTrack2 sync, YargProfile profile)
            : base(in overdrives, sync, profile)
        {
            Notes = notes;
            SubNoteBuffer = subNotes;
            Soloes = soloes;
        }

        protected override void _Dispose()
        {
            SubNoteBuffer.Dispose();
            Notes.Dispose();
            Soloes.Dispose();
            base._Dispose();
        }
    }
}

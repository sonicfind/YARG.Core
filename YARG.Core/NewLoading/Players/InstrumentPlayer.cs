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
        private int _noteIndex = 0;

        private readonly FixedArray<SoloPhrase> _soloes;
        private int _soloIndex = 0;

        public InstrumentPlayer(YARGNativeSortedList<DualTime, TNote> notes, YARGNativeList<TSubNote> subNotes, FixedArray<SoloPhrase> soloes, FixedArray<OverdrivePhrase> overdrives, SyncTrack2 sync, YargProfile profile)
            : base(overdrives, sync, profile)
        {
            _notes = notes;
            _subNoteBuffer = subNotes;
            _soloes = soloes;
        }

        public override void Dispose()
        {
            _subNoteBuffer.Dispose();
            _notes.Dispose();
            _soloes.Dispose();
            base.Dispose();
        }
    }
}

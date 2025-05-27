using YARG.Core.Containers;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading.Guitar
{
    public class GuitarBattleEngine : GuitarEngine
    {
        /// <summary>
        /// The length of time (in seconds) for any activated battle mode event
        /// </summary>
        public const double LENGTH_OF_EVENT = 10;

        private readonly YargNativeList<GuitarNoteGroup> _doubleNoteGroups;
        private readonly YargNativeList<GuitarSustain>   _doubleNoteSustains;

        public GuitarBattleEngine(
            GuitarTrack track,
            in NewHitWindow hitWindow,
            double strumWindow,
            BeatTracker beatTracker,
            OverdriveStyle style
        ) : base(track, hitWindow, strumWindow, beatTracker, style)
        {
            _doubleNoteGroups = new YargNativeList<GuitarNoteGroup>(track.NoteGroups);
            _doubleNoteSustains = new YargNativeList<GuitarSustain>(track.Sustains);
            // If we don't run this over the entire track beforehand, there could be instances where doubled-up notes
            // conflict with non-doubled-up notes.
            GuitarTrack.ApplyDoubleNotes(track.NotePositions, _doubleNoteGroups, _doubleNoteSustains, track.NumLanes);
        }

        public void ApplyDoubleNotes(in DualTime time)
        {
            double timeStart = time.Seconds + TRANSFORMATION_SPACING;

            int noteIndex = NoteIndex;
            while (noteIndex < Track.NoteGroups.Count && Track.NotePositions[noteIndex].Seconds < timeStart)
            {
                ++noteIndex;
            }

            if (noteIndex == Track.NoteGroups.Count)
            {
                return;
            }

            double timeEnd = timeStart + LENGTH_OF_EVENT;

            int sustainIndex = Track.NoteGroups[noteIndex].SustainIndex;
            while (noteIndex < Track.NoteGroups.Count && Track.NotePositions[noteIndex].Seconds < timeEnd)
            {
                ref var noteGroup = ref Track.NoteGroups[noteIndex];
                ref readonly var doubleNoteGroup = ref _doubleNoteGroups[noteIndex];
                noteGroup.LaneMask = doubleNoteGroup.LaneMask;
                noteGroup.LaneCount = doubleNoteGroup.LaneCount;
            }

            int sustainEnd = noteIndex < Track.NoteGroups.Count
                ? Track.NoteGroups[noteIndex].SustainIndex
                : Track.Sustains.Count;

            while (sustainIndex < sustainEnd)
            {
                ref var sustain = ref Track.Sustains[sustainIndex];
                ref readonly var doubleSustain = ref _doubleNoteSustains[sustainIndex];
                sustain.LaneMask = doubleSustain.LaneMask;
                sustain.LaneCount = doubleSustain.LaneCount;
                sustainIndex++;
            }
        }
    }
}

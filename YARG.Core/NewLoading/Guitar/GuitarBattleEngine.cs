using System.Diagnostics;
using YARG.Core.Containers;
using YARG.Core.IO;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading.Guitar
{
    public class GuitarBattleDifficultyUpBuffer
    {
        public TimedCollection<GuitarNoteGroup> Notes           { get; }
        public TimedCollection<GuitarSustain>   Sustains        { get; }
        public FixedArray<GuitarNoteGroup>      DoubledNotes    { get; }
        public FixedArray<GuitarSustain>        DoubledSustains { get; }

        private GuitarBattleDifficultyUpBuffer(TimedCollection<GuitarNoteGroup> notes, TimedCollection<GuitarSustain> sustains, int numLanes)
        {
            Notes = notes;
            Sustains = sustains;

            DoubledNotes = notes.Elements.Clone();
            DoubledSustains = sustains.Elements.Clone();

            GuitarTrack.ApplyDoubleNotes(
                notes.Ticks,
                DoubledNotes,
                sustains.Ticks,
                DoubledSustains,
                numLanes
            );
        }
    }

    public class GuitarBattleEngine : GuitarEngine
    {
        /// <summary>
        /// The length of time (in seconds) for any activated battle mode event
        /// </summary>
        public const double LENGTH_OF_EVENT = 10;

        private readonly GuitarBattleDifficultyUpBuffer?   _difficultyUpBuffer;
        private          FixedArray<GuitarNoteGroup>       _doubledNotes;
        private          FixedArray<GuitarSustain>         _doubledSustains;
        private          double                            _endOfDoubleNotes;

        public GuitarBattleEngine(
            GuitarTrack track,
            in NewHitWindow hitWindow,
            double strumWindow,
            BeatTracker beatTracker,
            OverdriveStyle style
        ) : base(track, hitWindow, strumWindow, beatTracker, style)
        {
            _doubledNotes = track.Notes.Elements.Clone();
            _doubledSustains = track.Sustains.Elements.Clone();

            GuitarTrack.ApplyDoubleNotes(
                track.Notes.Ticks,
                _doubledNotes,
                track.Sustains.Ticks,
                _doubledSustains,
                track.NumLanes
            );
        }

        public void ApplyDoubleNotes(double timeOfActivation)
        {
            double timeStart = timeOfActivation + TRANSFORMATION_SPACING;
            double timeEnd = timeStart + LENGTH_OF_EVENT;

            for (int noteIndex = NoteIndex; noteIndex < Track.Notes.Length; noteIndex++)
            {
                double position = Track.Notes.Seconds[noteIndex];
                if (position < timeStart)
                {
                    continue;
                }

                if (position >= timeEnd)
                {
                    break;
                }

                ref var noteGroup = ref Track.Notes.Elements[noteIndex];
                ref readonly var doubleNoteGroup = ref _doubledNotes[noteIndex];

                noteGroup.LaneMask = doubleNoteGroup.LaneMask;
                noteGroup.LaneCount = doubleNoteGroup.LaneCount;

                for (int sustainOffset = 0; sustainOffset < noteGroup.SustainCount; sustainOffset++)
                {
                    int sustainIndex = noteGroup.SustainIndex + sustainOffset;
                    Track.Sustains.Elements[sustainIndex] = _doubledSustains[sustainIndex];
                }
            }

            _endOfDoubleNotes = timeEnd;
        }

        public void ApplyDifficultyUp(double timeOfActivation)
        {
            Debug.Assert(_difficultyUpBuffer != null, "Cannot use difficulty up on expert");

            double timeStart = timeOfActivation + TRANSFORMATION_SPACING;
            double timeEnd = timeStart + LENGTH_OF_EVENT;

            // Gather the ranges needed to perform the copies

            int destinationGroupIndex = NoteIndex;
            while (destinationGroupIndex < Track.Notes.Length && timeStart < Track.Notes.Seconds[destinationGroupIndex])
            {
                destinationGroupIndex++;
            }

            int destinationGroupEndIndex = destinationGroupIndex;
            while (destinationGroupEndIndex < Track.Notes.Length && Track.Notes.Seconds[destinationGroupEndIndex] < timeEnd)
            {
                destinationGroupEndIndex++;
            }

            int sourceGroupIndex = _difficultyUpBuffer.Notes.GetBestPositionIndex(timeStart);
            int sourceGroupEndIndex = sourceGroupIndex;
            while (sourceGroupEndIndex < _difficultyUpBuffer.Notes.Length && _difficultyUpBuffer.Notes.Seconds[sourceGroupEndIndex] < timeEnd)
            {
                sourceGroupEndIndex++;
            }

            int destinationSustainIndex = destinationGroupIndex < Track.Notes.Length
                ? Track.Notes[destinationGroupIndex].SustainIndex
                : Track.Sustains.Length;

            int destinationSustainEndIndex = destinationGroupEndIndex < Track.Notes.Length
                ? Track.Notes[destinationGroupEndIndex].SustainIndex
                : Track.Sustains.Length;

            int sourceSustainIndex = sourceGroupIndex < _difficultyUpBuffer.Notes.Length
                ? _difficultyUpBuffer.Notes[sourceGroupIndex].SustainIndex
                : _difficultyUpBuffer.Sustains.Length;

            int sourceSustainEndIndex = sourceGroupEndIndex < _difficultyUpBuffer.Notes.Length
                ? _difficultyUpBuffer.Notes[sourceGroupEndIndex].SustainIndex
                : _difficultyUpBuffer.Sustains.Length;

            // Actually perform the difficulty swaps (which will override previously set double note effects)
            int destinationGroupCount = destinationGroupEndIndex - destinationGroupIndex;
            int sourceGroupCount = sourceGroupEndIndex - sourceGroupIndex;
            Track.Notes.ReplaceFrom(
                _difficultyUpBuffer.Notes,
                destinationGroupIndex,
                destinationGroupCount,
                sourceGroupIndex,
                sourceGroupCount
            );

            int destinationSustainCount = destinationSustainEndIndex - destinationSustainIndex;
            int sourceSustainCount = sourceSustainEndIndex - sourceSustainIndex;
            Track.Sustains.ReplaceFrom(
                _difficultyUpBuffer.Sustains,
                destinationSustainIndex,
                destinationSustainCount,
                sourceSustainIndex,
                sourceSustainCount
            );

            FixedArrayReplacer.ReplaceFrom(
                ref _doubledNotes,
                _difficultyUpBuffer.DoubledNotes,
                destinationGroupIndex,
                destinationGroupCount,
                sourceGroupIndex,
                sourceGroupCount
            );

            FixedArrayReplacer.ReplaceFrom(
                ref _doubledSustains,
                _difficultyUpBuffer.DoubledSustains,
                destinationSustainIndex,
                destinationSustainCount,
                sourceSustainIndex,
                sourceSustainCount
            );

            if (destinationGroupIndex == Track.Notes.Length)
            {
                return;
            }

            // We need to cap any active sustains to before the first note of the range
            // so that we don't run the risk of any conflicts

            long   firstNoteTicks   = Track.Notes.Ticks[destinationGroupIndex];
            double firstNoteSeconds = Track.Notes.Seconds[destinationGroupIndex];

            for (int activeIndex = 0; activeIndex < ActiveSustains.Count; ++activeIndex)
            {
                int sustainIndex = ActiveSustains[activeIndex].SustainIndex;
                if (Track.Sustains.Ticks[sustainIndex] > firstNoteTicks)
                {
                    Track.Sustains.Ticks[sustainIndex] = firstNoteTicks;
                    Track.Sustains.Seconds[sustainIndex] = firstNoteSeconds;
                }
            }

            // Double notes may be active, so we need to ensure we replace the notes still within its range with
            // their doubled versions
            for (int noteIndex = destinationGroupIndex;
                noteIndex < Track.Notes.Length && Track.Notes.Seconds[noteIndex] < _endOfDoubleNotes;
                noteIndex++)
            {
                ref var noteGroup = ref Track.Notes.Elements[noteIndex];
                ref readonly var doubleNoteGroup = ref _doubledNotes[noteIndex];

                noteGroup.LaneMask = doubleNoteGroup.LaneMask;
                noteGroup.LaneCount = doubleNoteGroup.LaneCount;

                for (int sustainOffset = 0; sustainOffset < noteGroup.SustainCount; sustainOffset++)
                {
                    int sustainIndex = noteGroup.SustainIndex + sustainOffset;
                    Track.Sustains.Elements[sustainIndex] = _doubledSustains[sustainIndex];
                }
            }

            // Have to make sure any "overdrive" phrase that bleeds into the segment holds the correct total count of notes
            int overdriveNoteIndex = destinationGroupIndex;
            if (Track.Notes[overdriveNoteIndex].OverdriveIndex >= 0)
            {
                ref var overdrive = ref Track.Overdrives[Track.Notes[overdriveNoteIndex].OverdriveIndex];
                if (overdrive.IsActive())
                {
                    overdrive.TotalNotes = 0;
                    for (int noteIndex = overdriveNoteIndex - 1;
                        noteIndex >= 0 && overdrive.StartTime.Ticks <= Track.Notes.Ticks[noteIndex];
                        --noteIndex)
                    {
                        overdrive.TotalNotes++;
                    }

                    while (overdriveNoteIndex < Track.Notes.Length && Track.Notes.Ticks[overdriveNoteIndex] < overdrive.EndTime.Ticks)
                    {
                        overdrive.TotalNotes++;
                        overdriveNoteIndex++;
                    }
                }
            }

            // Then, we need to correct any phrases that begin within the segment
            int outsideEventIndex = destinationGroupIndex + sourceGroupCount;
            while (overdriveNoteIndex < outsideEventIndex)
            {
                int overdriveIndex = Track.Notes[overdriveNoteIndex].OverdriveIndex;
                overdriveNoteIndex++;

                if (overdriveIndex == -1)
                {
                    continue;
                }

                ref var overdrive = ref Track.Overdrives[overdriveIndex];
                overdrive.TotalNotes = 1;
                while (overdriveNoteIndex < Track.Notes.Length && Track.Notes.Ticks[overdriveNoteIndex] < overdrive.EndTime.Ticks)
                {
                    overdrive.TotalNotes++;
                    overdriveNoteIndex++;
                }
            }

            if (outsideEventIndex == Track.Notes.Length)
            {
                return;
            }

            // Finally, we cap any sustains that extend past notes outside the effected segment
            // to dodge any potential note conflicts
            long   outsideNoteTicks   = Track.Notes.Ticks[outsideEventIndex];
            double outsideNoteSeconds = Track.Notes.Seconds[outsideEventIndex];

            int outsideSustainIndex = destinationSustainIndex + sourceSustainCount;
            for (int sustainIndex = destinationSustainIndex;
                sustainIndex < outsideSustainIndex;
                sustainIndex++)
            {
                if (Track.Sustains.Ticks[sustainIndex] > outsideNoteTicks)
                {
                    Track.Sustains.Ticks[sustainIndex] = outsideNoteTicks;
                    Track.Sustains.Seconds[sustainIndex] = outsideNoteSeconds;
                }
            }
        }
    }
}

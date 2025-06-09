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

        public static GuitarBattleDifficultyUpBuffer Create(GuitarTrack diffUpTrack)
        {
            var doubleNotes = diffUpTrack.Notes.Elements.Clone();
            var doubleSustains = diffUpTrack.Sustains.Elements.Clone();

            GuitarTrack.ApplyDoubleNotes(
                diffUpTrack.Notes.Ticks,
                doubleNotes,
                diffUpTrack.Sustains.Ticks,
                doubleSustains,
                diffUpTrack.NumLanes
            );

            return new GuitarBattleDifficultyUpBuffer(
                diffUpTrack.Notes,
                diffUpTrack.Sustains,
                doubleNotes,
                doubleSustains
            );
        }

        private GuitarBattleDifficultyUpBuffer(
            TimedCollection<GuitarNoteGroup> notes,
            TimedCollection<GuitarSustain> sustains,
            FixedArray<GuitarNoteGroup> doubledNotes,
            FixedArray<GuitarSustain>  doubledSustains
        )
        {
            Notes = notes;
            Sustains = sustains;
            DoubledNotes = doubledNotes;
            DoubledSustains = doubledSustains;
        }
    }

    public class GuitarBattleEngine
    {
        private enum StrumState
        {
            Inactive,
            Waiting,
            Taken,
            Overstrum,
        }

        /// <summary>
        /// The length of time (in seconds) for any activated battle mode event
        /// </summary>
        public const double LENGTH_OF_EVENT = 10;

        /// <summary>
        /// The length of time (in seconds) to use when applying transformation on a track
        /// </summary>
        public const double TRANSFORMATION_SPACING = 1.5;

        private readonly GuitarBattleDifficultyUpBuffer?   _difficultyUpBuffer;
        private          FixedArray<GuitarNoteGroup>       _doubledNotes;
        private          FixedArray<GuitarSustain>         _doubledSustains;
        private          double                            _endOfDoubleNotes;

        private GuitarButtonMask _buttonMask = GuitarButtonMask.None;
        private StrumState       _strumState = StrumState.Inactive;
        private double           _strumWindow;

        public GuitarTrack                 Track          { get; private set; }
        public int                         NoteIndex      { get; private set; }
        public long                        Health         { get; private set; } = (long) int.MaxValue + 1;
        public double                      CurrentTime    { get; private set; } = 0;
        public YargNativeList<int>         ActiveSustains { get; }              = new();
        public YargNativeList<DeadSustain> DeadSustains   { get; }              = new();

        public GuitarBattleEngine(
            GuitarTrack track,
            in NewHitWindow hitWindow,
            double strumWindow
        )
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

        public void UpdateInput(double updateTime, GuitarButtonMask buttonMask)
        {
            UpdateTime(updateTime);

            bool newStrum = IsNewStrum(buttonMask, _buttonMask);
            bool newFretting = (buttonMask & GuitarButtonMask.FretMask) != (_buttonMask & GuitarButtonMask.FretMask);
            _buttonMask = buttonMask;

            if (newStrum || newFretting)
            {
                // Gets the set of valid inputs currently used for holding active sustains
                var sustainInputMask = ValidateSustainInputs(updateTime, buttonMask);

                int noteIndex = NoteIndex;
                while (noteIndex < Track.Notes.Length)
                {
                    double notePosition = Track.Notes.Seconds[noteIndex];
                    double noteDistance = double.MaxValue;
                    if (noteIndex > 0)
                    {
                        noteDistance = notePosition - Track.Notes.Seconds[noteIndex - 1];
                    }

                    double frontEnd = HitWindow.CalculateFrontEnd(noteDistance);
                    // No reason to process notes that still haven't entered the window
                    if (updateTime < notePosition - frontEnd)
                    {
                        break;
                    }

                    ref var note = ref Track.Notes[noteIndex];
                    // Checks if the set of inputs counts towards hitting the current note
                    if (TestInputsAgainstNote(updateTime, in note, newStrum, (int) buttonMask, (int) sustainInputMask))
                    {
                        AddHit(updateTime, notePosition, ref note, scaledMultiplier);

                        // We declare any notes skipped as missed and therefore need to invalidate any
                        // attached overdrive phrases and declare all attached sustains as dead
                        while (NoteIndex < noteIndex)
                        {
                            ref readonly var skippedNote = ref Track.Notes[NoteIndex];
                            if (skippedNote.OverdriveIndex != -1)
                            {
                                Track.Overdrives[skippedNote.OverdriveIndex].Disable();
                            }

                            double skippedPosition = Track.Notes.Seconds[NoteIndex];

                            for (int sustainOffset = 0; sustainOffset < skippedNote.SustainCount; sustainOffset++)
                            {
                                int sustainIndex = skippedNote.SustainIndex + sustainOffset;
                                DeadSustains.Add(
                                    new DeadSustain
                                    (
                                        skippedPosition,
                                        Track.Sustains.Seconds[sustainIndex],
                                        Track.Sustains[sustainIndex].LaneMask
                                    )
                                );
                            }
                        }
                        NoteIndex++;

                        // Don't want to activate the below conditions
                        newStrum = false;
                    }

                    // Our focus is only on the current note
                    if (Combo > 0)
                    {
                        break;
                    }

                    noteIndex++;
                }

                // Processing explicit over-strums
                bool withinRange = IsWithinInputRange(updateTime);
                if (!withinRange || (newStrum && _strumState == StrumState.Overstrum))
                {
                    // However, only while inputs fall within the range of a note or sustain should
                    // we account for the over-strum
                    if (withinRange)
                    {
                        ApplyOverStrum(updateTime, scaledMultiplier);
                    }

                    _strumState = StrumState.Inactive;
                    _strumWindow = updateTime;
                }
                else if (newStrum)
                {
                    _strumState = StrumState.Overstrum;
                    _strumWindow = updateTime + _strumLeniency;
                }
            }

            if (buttonMask.Has(GuitarButtonMask.Whammy))
            {
                StartWhammy(updateTime);
            }
            else if (updateTime < _whammyEnd)
            {
                UpdateWhammy(updateTime);
            }
        }

        public void UpdateTime(double time)
        {
            bool hadOverStrum = ProcessNoteDrops(time);

            // We don't need to check whether the inputs match the sustains, as at this stage, it is
            // given that sustains reside either in  the "held" state (match the current set of inputs) or the
            // "drop leniency" state
            for (int activeIndex = 0; activeIndex < ActiveSustains.Count;)
            {
                ref var activeTracker = ref ActiveSustains[activeIndex];

                var sustainEndTime = Track.Sustains.Seconds[activeTracker];

                if (hadOverStrum && _strumWindow < sustainEndTime)
                {
                    DeadSustains.Add(new DeadSustain(_strumWindow, sustainEndTime, Track.Sustains[activeTracker].LaneMask));
                    ActiveSustains.RemoveAt(activeIndex);
                }
                else if (time >= sustainEndTime)
                {
                    ActiveSustains.RemoveAt(activeIndex);
                }
                else
                {
                    activeIndex++;
                }
            }

            CurrentTime = time;
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

            foreach (var sustain in ActiveSustains)
            {
                if (Track.Sustains.Ticks[sustain] > firstNoteTicks)
                {
                    Track.Sustains.Ticks[sustain] = firstNoteTicks;
                    Track.Sustains.Seconds[sustain] = firstNoteSeconds;
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

        private bool ProcessNoteDrops(double updateTime)
        {
            bool hasOverStrum = false;
            if (_strumWindow <= updateTime)
            {
                hasOverStrum = _strumState == StrumState.Overstrum;
                _strumState = StrumState.Inactive;
            }

            while (NoteIndex < Track.Notes.Length)
            {
                double position = Track.Notes.Seconds[NoteIndex];

                double noteDistance = double.MaxValue;
                if (NoteIndex + 1 < Track.Notes.Length)
                {
                    noteDistance = Track.Notes.Seconds[NoteIndex + 1] - position;
                }

                double backEnd = HitWindow.CalculateBackEnd(noteDistance);
                double backTime = position + backEnd;

                if ((hasOverStrum || backTime <= updateTime) && Combo > 0)
                {
                    Combo = 0;
                }


                // An over-strum will only affect an overdrive phrase if it happens in between two notes
                // that share the same phrase.
                //
                // Or, internally, for over-strums on note offsets [1, n-1] of a phrase
                int overdriveIndex = Track.Notes[NoteIndex].OverdriveIndex;
                if (overdriveIndex >= 0)
                {
                    ref var overdrive = ref Track.Overdrives[overdriveIndex];
                    if (backTime <= updateTime || (hasOverStrum && overdrive.HitCount > 0))
                    {
                        overdrive.Disable();
                    }
                }

                if (updateTime < backTime)
                {
                    break;
                }

                NoteIndex++;
            }

            return hasOverStrum;
        }

        private void ApplyOverStrum(double updateTime, long multiplier)
        {
            foreach (var tracker in ActiveSustains)
            {
                DeadSustains.Add(
                    new DeadSustain
                    (
                        updateTime,
                        Track.Sustains.Seconds[tracker],
                        Track.Sustains[tracker].LaneMask
                    )
                );
            }
            for (int i = 0; i < ActiveSustains.Count; ++i)
            {
                ref var tracker = ref ActiveSustains[i];

            }
            ActiveSustains.Clear();
            Combo = 0;
        }
    }
}

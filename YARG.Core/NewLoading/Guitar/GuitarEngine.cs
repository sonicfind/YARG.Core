using System;
using YARG.Core.Containers;

namespace YARG.Core.NewLoading.Guitar
{
    public class GuitarEngine : BaseEngine
    {
        private enum StrumState
        {
            Inactive,
            Waiting,
            Taken,
            Overstrum,
        }

        private struct SustainChange
        {
            public enum ChangeAction
            {
                MultiplierChange,
                OverdriveDrop,
                Overstrum,
            }

            public readonly ChangeAction Action;
            public readonly double       Time;
            public          long         Multiplier;

            public SustainChange(ChangeAction action, double time, long multiplier)
            {
                Action = action;
                Time = time;
                Multiplier = multiplier;
            }
        }

        private const double STRUM_LENIENCY_FOR_TESTING       = .060;
        private const double STRUM_SMALL_LENIENCY_FOR_TESTING = .030;
        private const long   POINTS_PER_FRET                  = 50;
        private const double WHAMMY_TIME                      = .5;

        private readonly double _strumLeniency      = STRUM_LENIENCY_FOR_TESTING;
        private readonly double _strumSmallLeniency = STRUM_SMALL_LENIENCY_FOR_TESTING;

        private GuitarButtonMask _buttonMask = GuitarButtonMask.None;
        private StrumState       _strumState = StrumState.Inactive;
        private double           _strumWindow;
        private double           _whammyEnd;

        public GuitarTrack                   Track            { get; }
        public int                           NoteIndex        { get; private set; }
        public NewHitWindow                  HitWindow        { get; }
        public BeatTracker                   BeatTracker      { get; }
        public OverdriveTracker              OverdriveTracker { get; }
        public YargNativeList<ActiveSustain> ActiveSustains   { get; } = new();
        public YargNativeList<DeadSustain>   DeadSustains     { get; } = new();

        public GuitarEngine(
            GuitarTrack track,
            NewHitWindow hitWindow,
            double strumWindow,
            BeatTracker beatTracker,
            OverdriveStyle style
        )
        {
            Track = track;
            HitWindow = hitWindow;
            _strumWindow = strumWindow;
            BeatTracker = beatTracker;
            OverdriveTracker = new OverdriveTracker(style);
        }

        public void UpdateInput(double updateTime, GuitarButtonMask buttonMask)
        {
            var overdriveInfo = UpdateTime(updateTime);

            bool newStrum = IsNewStrum(buttonMask, _buttonMask);
            bool newFretting = (buttonMask & GuitarButtonMask.FretMask) != (_buttonMask & GuitarButtonMask.FretMask);
            _buttonMask = buttonMask;

            long scaledMultiplier = Multiplier;
            if (overdriveInfo.State == OverdriveState.Active)
            {
                scaledMultiplier *= 2;
            }

            if (newStrum || newFretting)
            {
                // Gets the set of valid inputs currently used for holding active sustains
                var sustainInputMask = ValidateSustainInputs(updateTime, buttonMask, scaledMultiplier);

                int noteIndex = NoteIndex;
                while (noteIndex < Track.NoteGroups.Count)
                {
                    double notePosition = Track.NotePositions[noteIndex].Seconds;
                    double noteDistance = double.MaxValue;
                    if (noteIndex > 0)
                    {
                        noteDistance = notePosition - Track.NotePositions[noteIndex - 1].Seconds;
                    }

                    double frontEnd = HitWindow.CalculateFrontEnd(noteDistance);
                    // No reason to process notes that still haven't entered the window
                    if (updateTime < notePosition - frontEnd)
                    {
                        break;
                    }

                    ref var note = ref Track.NoteGroups[noteIndex];
                    // Checks if the set of inputs counts towards hitting the current note
                    if (TestInputsAgainstNote(updateTime, in note, newStrum, (int) buttonMask, (int) sustainInputMask))
                    {
                        AddHit(updateTime, notePosition, ref note, scaledMultiplier);

                        // We declare any notes skipped as missed and therefore need to invalidate any
                        // attached overdrive phrases
                        while (NoteIndex < noteIndex)
                        {
                            int overdriveIndex = Track.NoteGroups[NoteIndex++].OverdriveIndex;
                            if (overdriveIndex != -1)
                            {
                                Track.Overdrives[overdriveIndex].Disable();
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
                        ApplyOverstrum(updateTime, scaledMultiplier);
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

        public override OverdriveInfo UpdateTime(double time)
        {
            Span<SustainChange> changes = stackalloc SustainChange[4];
            int changeCount = ProcessNoteDrops(time, changes);
            changeCount = FinalizeSustainChanges(changes, changeCount);

            var currentOverdrive = OverdriveTracker.Update(time);

            long scaledMultiplier = Multiplier;
            if (currentOverdrive.State == OverdriveState.Active)
            {
                scaledMultiplier *= 2;
            }

            // We don't need to check whether the inputs match the sustains, as at this stage, it is
            // given that sustains reside either in  the "held" state (match the current set of inputs) or the
            // "drop leniency" state
            for (int activeIndex = 0; activeIndex < ActiveSustains.Count;)
            {
                ref var activeTracker = ref ActiveSustains[activeIndex];
                ref readonly var sustain = ref Track.Sustains[activeTracker.SustainIndex];

                // For each change, we need to finalize the added score and update the base position
                // of the tracker.
                //
                // We achieve better score consistency with this pattern instead of haggling with
                // time-by-time additions that lead to repeated loss of precision
                int changeIndex = 0;
                while (changeIndex < changeCount)
                {
                    ref readonly var change = ref changes[changeIndex];
                    if (sustain.EndTime.Seconds < change.Time || change.Action == SustainChange.ChangeAction.Overstrum)
                    {
                        break;
                    }

                    Score += CalculateSustainScore(ref activeTracker, sustain.LaneCount, change.Time, change.Multiplier);
                    changeIndex++;
                }

                if (changeIndex == changeCount && time < sustain.EndTime.Seconds)
                {
                    activeIndex++;
                    continue;
                }

                double endTime = sustain.EndTime.Seconds;
                long finalMultiplier = scaledMultiplier;

                if (changeIndex < changeCount)
                {
                    ref readonly var change = ref changes[changeIndex];
                    if (change.Time < sustain.EndTime.Seconds)
                    {
                        DeadSustains.Add(new DeadSustain(change.Time, sustain.EndTime.Seconds, sustain.LaneMask));
                        endTime = change.Time;
                    }
                    finalMultiplier = change.Multiplier;
                }

                Score += CalculateSustainScore(ref activeTracker, sustain.LaneCount, endTime, finalMultiplier);

                ActiveSustains.RemoveAt(activeIndex);
            }

            CurrentTime = time;
            return currentOverdrive;
        }

        public override HittablePhrase GetCurrentOverdrive()
        {
            while (OverdriveIndex < Track.Overdrives.Count && Track.Overdrives[OverdriveIndex].EndTime.Seconds >= CurrentTime)
            {
                ++OverdriveIndex;
            }

            return OverdriveIndex < Track.Overdrives.Count
                ? Track.Overdrives[OverdriveIndex]
                : HittablePhrase.Unhittable;
        }

        public override void Dispose()
        {
            Track.Dispose();
            ActiveSustains.Dispose();
            DeadSustains.Dispose();
        }

        public void SwapTrack(GuitarTrack newTrack, double swapPosition)
        {
            double sourceStartLimit = swapPosition;

            // Check active sustains for endpoints that extend past the set swap position
            //
            // We don't want to have the new stuff overlap the valid old stuff, do we?
            for (int i = 0; i < ActiveSustains.Count; i++)
            {
                ref var tracker = ref ActiveSustains[i];
                // always disable current overdrives
                tracker.OverdriveIndex = -1;

                var sustainEnd = Track.Sustains[tracker.SustainIndex].EndTime.Seconds;
                if (sourceStartLimit < sustainEnd)
                {
                    sourceStartLimit = sustainEnd;
                }
            }

            int noteDestinationIndex = NoteIndex;
            while (noteDestinationIndex < Track.NoteGroups.Count)
            {
                if (Track.NotePositions[noteDestinationIndex].Seconds >= swapPosition)
                {
                    break;
                }

                ref var noteGroup = ref Track.NoteGroups[noteDestinationIndex];
                // always disable current overdrives & solos as they no longer match
                noteGroup.OverdriveIndex = -1;
                noteGroup.SoloIndex = -1;

                for (int sustainOffset = 0; sustainOffset < noteGroup.SustainCount; sustainOffset++)
                {
                    var sustainEnd = Track.Sustains[noteGroup.SustainIndex + sustainOffset].EndTime.Seconds;
                    if (sourceStartLimit < sustainEnd)
                    {
                        sourceStartLimit = sustainEnd;
                    }
                }
                ++noteDestinationIndex;
            }

            // The visuals are unimportant to the engine itself, so we can clamp them however we want
            for (int i = 0; i < DeadSustains.Count; ++i)
            {
                ref var deadSustain = ref DeadSustains[i];
                if (deadSustain.EndTime > sourceStartLimit)
                {
                    deadSustain.EndTime = sourceStartLimit;
                }
            }

            int sustainDestinationIndex = noteDestinationIndex < Track.NoteGroups.Count
                ? Track.NoteGroups[noteDestinationIndex].SustainIndex
                : Track.Sustains.Count;

            Track.NotePositions.Resize_NoInitialization(noteDestinationIndex);
            Track.NoteGroups.Resize_NoInitialization(noteDestinationIndex);

            Track.Sustains.Resize_NoInitialization(sustainDestinationIndex);

            unsafe
            {
                int noteSourceIndex = newTrack.NotePositions.GetBestPositionIndex(sourceStartLimit + TRANSFORMATION_SPACING);

                Track.NotePositions.AddRange(newTrack.NotePositions.Data + noteSourceIndex, newTrack.NotePositions.Count - noteSourceIndex);
                Track.NoteGroups.AddRange(newTrack.NoteGroups.Data + noteSourceIndex, newTrack.NoteGroups.Count - noteSourceIndex);

                if (noteSourceIndex < newTrack.NoteGroups.Count)
                {
                    int sustainSourceIndex = newTrack.NoteGroups[noteSourceIndex].SustainIndex;
                    Track.Sustains.AddRange
                    (
                        newTrack.Sustains.Data + sustainSourceIndex,
                        newTrack.Sustains.Count - sustainSourceIndex
                    );
                }
            }

            // Fixup all the sustain indices
            while (noteDestinationIndex < Track.NoteGroups.Count)
            {
                ref var group = ref Track.NoteGroups[noteDestinationIndex];
                group.SustainIndex = sustainDestinationIndex;
                sustainDestinationIndex += group.SustainCount;
            }

            Track.Solos.CopyFrom(newTrack.Solos);
            Track.Overdrives.CopyFrom(newTrack.Overdrives);

            NoteIndex = noteDestinationIndex;
        }

        private int ProcessNoteDrops(double updateTime, Span<SustainChange> changes)
        {
            int changeCount = 0;

            bool hasOverStrum = false;
            if (_strumWindow <= updateTime)
            {
                if (_strumState == StrumState.Overstrum)
                {
                    changes[0] = new SustainChange(SustainChange.ChangeAction.Overstrum, _strumWindow, Multiplier);
                    changeCount = 1;
                    hasOverStrum = true;
                }
                _strumState = StrumState.Inactive;
            }

            while (NoteIndex < Track.NoteGroups.Count)
            {
                double position = Track.NotePositions[NoteIndex].Seconds;
                double noteDistance = double.MaxValue;
                if (NoteIndex + 1 < Track.NoteGroups.Count)
                {
                    noteDistance = Track.NotePositions[NoteIndex + 1].Seconds - position;
                }

                double backEnd = HitWindow.CalculateBackEnd(noteDistance);
                double backTime = position + backEnd;

                ref readonly var note = ref Track.NoteGroups[NoteIndex];
                if ((hasOverStrum || backTime <= updateTime) && Combo > 0)
                {
                    if (Multiplier > 1)
                    {
                        if (backTime <= updateTime)
                        {
                            if (!hasOverStrum)
                            {
                                changes[0] = new SustainChange
                                (
                                    SustainChange.ChangeAction.MultiplierChange,
                                    backTime,
                                    Multiplier
                                );
                                changeCount = 1;
                            }
                            else if (backTime < _strumWindow)
                            {
                                changes[1] = changes[0];
                                changes[0] = new SustainChange
                                (
                                    SustainChange.ChangeAction.MultiplierChange,
                                    backTime,
                                    Multiplier
                                );
                                changes[1].Multiplier = 1;
                                changeCount = 2;
                            }
                            // There's no point in adding a node if the multiplier drop happens after the over-strum,
                            // as we would've already dropped every active sustain by that point
                        }
                        Multiplier = 1;
                    }

                    Combo = 0;
                }

                // An over-strum will only affect an overdrive phrase if it happens in between two notes
                // that share the same phrase.
                //
                // Or, internally, for over-strums on note offsets [1, n-1] of a phrase
                if (note.OverdriveIndex >= 0)
                {
                    ref var overdrive = ref Track.Overdrives[note.OverdriveIndex];
                    if (backTime <= updateTime || (hasOverStrum && overdrive.HitCount > 0))
                    {
                        if (overdrive.HitCount > 0 &&
                            backTime <= updateTime &&
                            (!hasOverStrum || backTime < _strumWindow))
                        {
                            int index = 0;
                            while (index < changeCount && changes[index].Time <= backTime)
                            {
                                index++;
                            }

                            for (int shift = changeCount; index < shift; shift--)
                            {
                                changes[shift] = changes[shift - 1];
                            }

                            changes[index] = new SustainChange
                            (
                                SustainChange.ChangeAction.OverdriveDrop,
                                backTime,
                                // The multiplier is unimportant for overdrive drops
                                -1
                            );
                            changeCount++;
                        }
                        overdrive.Disable();
                    }
                }

                if (updateTime < backTime)
                {
                    break;
                }

                NoteIndex++;
            }

            return changeCount;
        }

        private int FinalizeSustainChanges(Span<SustainChange> changes, int changeCount)
        {
            for (int i = 0; i < changeCount;)
            {
                ref var change = ref changes[i];
                if (change.Action == SustainChange.ChangeAction.OverdriveDrop)
                {
                    UpdateWhammy(change.Time);

                    for (int j = i; j + 1 < changeCount; ++j)
                    {
                        changes[j] = changes[j + 1];
                    }

                    changeCount--;
                    continue;
                }

                var localOverdrive = OverdriveTracker.Update(change.Time);
                if (localOverdrive.State == OverdriveState.Active)
                {
                    change.Multiplier *= 2;
                }
                else if (localOverdrive.State == OverdriveState.Completed)
                {
                    for (int shift = changeCount; i < shift; shift--)
                    {
                        changes[shift] = changes[shift - 1];
                    }

                    changes[i++] = new SustainChange
                    (
                        SustainChange.ChangeAction.MultiplierChange,
                        localOverdrive.Time,
                        change.Multiplier * 2
                    );
                }
                i++;
            }

            return changeCount;
        }

        private long CalculateSustainScore(
            ref ActiveSustain activeTracker,
            int numLanes,
            double endTime,
            long multiplier
        )
        {
            if (endTime <= activeTracker.BasePosition)
            {
                return 0;
            }

            double beatCount = 0;
            while (activeTracker.BaseBeatIndex + 1 < BeatTracker.Count)
            {
                double current = BeatTracker[activeTracker.BaseBeatIndex].Position;
                double next = BeatTracker[activeTracker.BaseBeatIndex + 1].Position;
                if (endTime < next)
                {
                    if (activeTracker.BaseBeatIndex + 1 < BeatTracker.Count && activeTracker.BasePosition < endTime)
                    {
                        beatCount += (endTime - activeTracker.BasePosition) / (next - current);
                        activeTracker.BasePosition = endTime;
                    }
                    break;
                }

                beatCount += (next - activeTracker.BasePosition) / (next - current);
                activeTracker.BasePosition = next;
                activeTracker.BaseBeatIndex++;
            }
            return (long)Math.Round(multiplier * POINTS_PER_BEAT * beatCount * numLanes);
        }

        private GuitarButtonMask ValidateSustainInputs(double updateTime, GuitarButtonMask buttonMask, long multiplier)
        {
            var sustainedInputs = GuitarButtonMask.None;
            for (int i = 0; i < ActiveSustains.Count; i++)
            {
                ref var activeTracker = ref ActiveSustains[i];
                ref readonly var sustain = ref Track.Sustains[activeTracker.SustainIndex];

                // Shift removes the anchor bit to line up with the input mask
                int laneMask = (int) sustain.LaneMask >> 1;

                if (TestSustainInput(in sustain, laneMask, (int)buttonMask))
                {
                    sustainedInputs |= (GuitarButtonMask) laneMask;
                    i++;
                }
                else
                {
                    Score += CalculateSustainScore(ref activeTracker, sustain.LaneCount, updateTime, multiplier);

                    DeadSustains.Add(new DeadSustain(updateTime, sustain.EndTime.Seconds, sustain.LaneMask));

                    ActiveSustains.RemoveAt(i);
                }
            }
            return sustainedInputs;
        }

        private static bool TestSustainInput(in GuitarSustain sustain, int laneMask, int inputMask)
        {
            // Note: inactive leniency also means that the sustain remains by itself.
            // No extended sustains before it, no disjoints in its chord, and no notes within its duration
            if (sustain.HasFretLeniency)
            {
                inputMask &= laneMask;
            }
            // have to test `sustain.LaneMask` instead of laneMask as we've already removed laneMask's open bit
            else if (sustain.LaneCount < 2 && !sustain.LaneMask.Has(GuitarLaneMask.Open_DisableAnchoring))
            {
                // Remove the anchor-able bits
                while ((laneMask & 1) == 0)
                {
                    laneMask >>= 1;
                    inputMask >>= 1;
                }
            }
            return inputMask == laneMask;
        }

        private static bool IsNewStrum(GuitarButtonMask current, GuitarButtonMask previous)
        {
            var currStrum = current & GuitarButtonMask.StrumMask;
            var previousStrum = previous & GuitarButtonMask.StrumMask;
            return currStrum > previousStrum
                || (current == GuitarButtonMask.StrumUp && previousStrum == GuitarButtonMask.StrumDown);
        }

        private bool TestInputsAgainstNote(double updateTime, in GuitarNoteGroup note, bool newStrum, int inputMask, int sustainInputMask)
        {
            // Shift removes the anchor bit to line up with the input mask
            int laneMask = (int)note.LaneMask >> 1;

            // We have to account for inputs that sustains may have already siphoned
            int inputMaskAlternate = inputMask ^ (inputMask & sustainInputMask);

            if (note.LaneCount < 2 && !note.LaneMask.Has(GuitarLaneMask.Open_DisableAnchoring))
            {
                // Remove the anchor-able bits
                while ((laneMask & 1) == 0)
                {
                    laneMask >>= 1;
                    inputMask >>= 1;
                    inputMaskAlternate >>= 1;
                }
            }

            if (laneMask != inputMask && laneMask != inputMaskAlternate)
            {
                return false;
            }

            // Requires strumming
            if (note.Style == GuitarNoteStyle.Strum || (note.Style == GuitarNoteStyle.Hopo && Combo == 0))
            {
                if (!newStrum && _strumState is not StrumState.Taken and not StrumState.Overstrum)
                {
                    return false;
                }

                if (_strumState != StrumState.Waiting)
                {
                    _strumState = StrumState.Inactive;
                    _strumWindow = updateTime;
                }
                else
                {
                    _strumWindow = updateTime + _strumLeniency;
                }
            }
            else if (newStrum)
            {
                _strumWindow = updateTime + _strumLeniency;

                // If the over-strum state is set, then a new strum would exceed normal conditions
                // and thus would continue that same status.
                //
                // Anything else abides to the one-note-one-strum rule.
                if (_strumState != StrumState.Overstrum)
                {
                    _strumState = StrumState.Taken;
                }
            }
            // Allows a strum input within the window to potentially attribute to the current hopo/tap
            else if (_strumState != StrumState.Overstrum)
            {
                _strumWindow = updateTime + _strumSmallLeniency;
                _strumState = StrumState.Waiting;
            }
            else
            {
                _strumState = StrumState.Taken;
            }
            return true;
        }

        private void AddHit(double updateTime, double notePosition, ref GuitarNoteGroup note, long scaledMultiplier)
        {
            Score += POINTS_PER_FRET * note.LaneCount * scaledMultiplier;
            Combo++;

            if (Combo is 10 or 20 or 30)
            {
                // We need to finalize the current state of sustains to the total score
                // with the current multiplier
                for (int i = 0; i < ActiveSustains.Count; ++i)
                {
                    ref var tracker = ref ActiveSustains[i];
                    Score += CalculateSustainScore(
                        ref tracker,
                        Track.Sustains[tracker.SustainIndex].LaneCount,
                        updateTime,
                        scaledMultiplier
                    );
                }
                Multiplier++;
            }

            // This makes it easier to track the hit count after the fact
            // AND better matches the visual representation of the note
            note.LaneMask = GuitarLaneMask.None;

            int beatIndex = BeatTracker.Index;
            while (beatIndex > 0 && notePosition < BeatTracker[beatIndex].Position)
            {
                --beatIndex;
            }

            while (beatIndex + 1 < BeatTracker.Count && notePosition >= BeatTracker[beatIndex + 1].Position)
            {
                ++beatIndex;
            }

            for (int i = 0; i < note.SustainCount; ++i)
            {
                ActiveSustains.Add(new ActiveSustain(beatIndex, notePosition, note.SustainIndex + i, note.OverdriveIndex));
            }

            if (note.SoloIndex != -1)
            {
                Track.Solos[note.SoloIndex].AddHit();
            }

            if (note.OverdriveIndex != -1 && Track.Overdrives[note.OverdriveIndex].AddHit())
            {
                OverdriveTracker.AddOverdrive(updateTime, OverdriveTracker.OVERDRIVE_PER_PHRASE, BeatTracker);
            }
        }

        private bool IsWithinInputRange(double updateTime)
        {
            if (!ActiveSustains.IsEmpty())
            {
                return true;
            }

            if (NoteIndex < Track.NotePositions.Count &&
                updateTime + _strumLeniency + HitWindow.FrontMax >= Track.NotePositions[NoteIndex].Seconds)
            {
                return true;
            }

            if (NoteIndex > 0 &&
                updateTime < Track.NotePositions[NoteIndex - 1].Seconds  + _strumLeniency + HitWindow.BackMax)
            {
                return true;
            }
            return false;
        }

        private void StartWhammy(double updateTime)
        {
            double? whammyEnd = null;
            for (int i = 0; i < ActiveSustains.Count; i++)
            {
                ref var tracker = ref ActiveSustains[i];
                tracker.WhammyStart = updateTime;

                if (tracker.OverdriveIndex == -1 || Track.Overdrives[tracker.OverdriveIndex].HitCount == -1)
                {
                    continue;
                }

                var sustainEnd = Track.Sustains[tracker.SustainIndex].EndTime.Seconds;
                if (!whammyEnd.HasValue || whammyEnd < sustainEnd)
                {
                    whammyEnd = sustainEnd;
                }
            }

            if (whammyEnd.HasValue)
            {
                if (whammyEnd > updateTime + WHAMMY_TIME)
                {
                    whammyEnd = updateTime + WHAMMY_TIME;
                }

                _whammyEnd = whammyEnd.Value;
                OverdriveTracker.StartGains(updateTime, whammyEnd.Value, BeatTracker);
            }
        }

        private void UpdateWhammy(double time)
        {
            double? whammyEnd = null;
            for (int i = 0; i < ActiveSustains.Count; ++i)
            {
                ref var tracker = ref ActiveSustains[i];
                if (tracker.OverdriveIndex == -1 ||
                    Track.Overdrives[tracker.OverdriveIndex].HitCount == -1 ||
                    !tracker.WhammyStart.HasValue)
                {
                    continue;
                }


                double end = tracker.WhammyStart.Value + WHAMMY_TIME;
                double sustainEnd = Track.Sustains[tracker.SustainIndex].EndTime.Seconds;
                if (end > sustainEnd)
                {
                    end = sustainEnd;
                }

                if (end > time && (!whammyEnd.HasValue || end > whammyEnd.Value))
                {
                    whammyEnd = end;
                }
            }

            if (!whammyEnd.HasValue)
            {
                OverdriveTracker.StopGains(time, BeatTracker);
            }
            else if (whammyEnd.Value < _whammyEnd)
            {
                _whammyEnd = whammyEnd.Value;
                OverdriveTracker.StartGains(time, whammyEnd.Value, BeatTracker);
            }
        }

        private void ApplyOverstrum(double updateTime, long multiplier)
        {
            for (int i = 0; i < ActiveSustains.Count; ++i)
            {
                ref var tracker = ref ActiveSustains[i];
                ref readonly var sustain = ref Track.Sustains[tracker.SustainIndex];

                // No need to check against the end time of the sustain because we would've already removed
                // any sustains that ended before this input time in the UpdateTime() method
                Score += CalculateSustainScore(ref tracker, sustain.LaneCount, updateTime, multiplier);

                DeadSustains.Add(
                    new DeadSustain
                    (
                        updateTime,
                        sustain.EndTime.Seconds,
                        sustain.LaneMask
                    )
                );
            }
            ActiveSustains.Clear();
            OverdriveTracker.StopGains(updateTime, BeatTracker);

            Multiplier = 1;
            Combo = 0;
        }
    }
}

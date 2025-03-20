using System;
using System.Diagnostics;
using YARG.Core.Containers;
using YARG.Core.Engine;
using YARG.Core.Game;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public class GuitarEngine : IDisposable
    {
        private enum StrumState
        {
            Inactive,
            Pending_Overstrum,
            Taken,
        }

        private readonly NewHitWindow                   _hitWindow;
        private readonly DualTime                       _strumLeniency;
        private readonly YargNativeList<SustainTracker> _activeSustains = new();

        private GuitarTrack?     _track        = null;
        private EngineStats      _stats        = EngineStats.Zero;
        private long             _sustainIndex = 0;
        private GuitarButtonMask _buttonMask   = GuitarButtonMask.None;
        private DualTime         _strumWindow  = DualTime.Zero;
        private StrumState       _strumState   = StrumState.Inactive;


        public GuitarEngine(in NewHitWindow hitWindow, in DualTime strumWindow, Modifier modifiers)
        {
            _hitWindow = hitWindow;
            _strumWindow = strumWindow;
        }

        public void SetTrack(in GuitarTrack track)
        {
            _track?.Dispose();
            _track = track;
            _stats.NoteIndex = 0;
            _stats.SoloIndex = 0;
        }

        public void SwapTrack(in GuitarTrack track, in DualTime swapPosition)
        {
            Debug.Assert(track != null);
            long noteReplacementIndex = _stats.NoteIndex;
            long sustainReplacementIndex = _sustainIndex;
            double latestEndPoint = swapPosition.Seconds;
            for (int i = 0; i < _activeSustains.Count; ++i)
            {
                ref readonly var activeSustain = ref _activeSustains[i];
                ref var sustain = ref _track!.Sustains[activeSustain.SustainIndex];
                sustain.OverdriveIndex = -1;
                if (latestEndPoint < sustain.EndTime.Seconds)
                {
                    latestEndPoint = sustain.EndTime.Seconds;
                }
            }

            while (noteReplacementIndex < _track!.NoteGroups.Count)
            {
                ref var noteGroup = ref _track.NoteGroups[noteReplacementIndex];
                if (noteGroup.Position >= swapPosition)
                {
                    break;
                }

                noteGroup.OverdriveIndex = -1;
                noteGroup.SoloIndex = -1;
                for (int sustainOffset = 0; sustainOffset < noteGroup.SustainCount; sustainOffset++)
                {
                    ref var sustain = ref _track.Sustains[sustainReplacementIndex++];
                    sustain.OverdriveIndex = -1;
                    if (latestEndPoint < sustain.EndTime.Seconds)
                    {
                        latestEndPoint = sustain.EndTime.Seconds;
                    }
                }
                ++noteReplacementIndex;
            }

            latestEndPoint += 1;
            long noteCopyIndex = 0;
            long sustainCopyIndex = 0;
            while (noteCopyIndex < track.NoteGroups.Count)
            {
                ref readonly var noteGroup = ref track.NoteGroups[noteCopyIndex];
                if (noteGroup.Position.Seconds >= latestEndPoint)
                {
                    break;
                }
                sustainCopyIndex += noteGroup.SustainCount;
                ++noteCopyIndex;
            }

            _track.NoteGroups.Resize_NoInitialization(noteReplacementIndex);
            _track.Sustains.Resize_NoInitialization(sustainReplacementIndex);
            unsafe
            {
                _track.NoteGroups.AddRange(track.NoteGroups.Data + noteCopyIndex, track.NoteGroups.Count - noteCopyIndex);
                _track.Sustains.AddRange(track.Sustains.Data + sustainCopyIndex, track.Sustains.Count - sustainCopyIndex);
            }

            _track!.Solos.CopyFrom(track.Solos);
            _track.Overdrives.CopyFrom(track.Overdrives);
            // We need to invalidate an overdrive phrase if the starting note of the copy
            // does not allow the player to hit all the notes required
            if (noteReplacementIndex < _track.NoteGroups.Count)
            {
                ref readonly var noteGroup = ref _track.NoteGroups[noteCopyIndex];
                if (noteGroup.OverdriveIndex >= 0)
                {
                    long overdriveNoteIndex = noteCopyIndex + 1;
                    long overdriveNoteCount = 1;
                    while (overdriveNoteIndex < _track.NoteGroups.Count &&
                        _track.NoteGroups[overdriveNoteIndex].OverdriveIndex == noteGroup.OverdriveIndex)
                    {
                        overdriveNoteCount++;
                        overdriveNoteIndex++;
                    }

                    ref var overdrive = ref _track.Overdrives[noteGroup.OverdriveIndex];
                    if (overdrive.TotalNotes != overdriveNoteCount)
                    {
                        overdrive.HitCount = -1;
                    }
                }
            }

            _stats.NoteIndex = noteReplacementIndex;
            _sustainIndex = sustainReplacementIndex;
        }

        public UpdateResult UpdateTime(in DualTime time, long resolution)
        {
            if (_track == null)
            {
                throw new InvalidOperationException("Track has not been set.");
            }

            _stats.CurrentTime = time;
            for (int i = 0; i < _activeSustains.Count;)
            {
                ref var tracker = ref _activeSustains[i];
                ref readonly var sustain = ref _track.Sustains[tracker.SustainIndex];
                // The else clause can only occur for a sustain maximally once
                if (time < sustain.EndTime)
                {
                    i++;
                }
                else
                {
                    long length = sustain.EndTime.Ticks - tracker.BasePosition.Ticks;
                    long sustainScoreUnscaled =
                        _stats.Multiplier * EngineStats.POINTS_PER_BEAT * length * sustain.LaneCount;
                    _stats.Score += (long)Math.Ceiling((double)sustainScoreUnscaled / resolution);
                    _activeSustains.RemoveAt(i);
                }
            }

            var result = UpdateResult.OK;
            while (_stats.NoteIndex < _track.NoteGroups.Count)
            {
                ref readonly var note = ref _track.NoteGroups[_stats.NoteIndex];
                double noteDistance = double.MaxValue;
                if (_stats.NoteIndex + 1 < _track.NoteGroups.Count)
                {
                    noteDistance = _track.NoteGroups[_stats.NoteIndex + 1].Position.Seconds - note.Position.Seconds;
                }

                double backEnd = _hitWindow.CalculateBackEnd(noteDistance);
                if (time.Seconds < note.Position.Seconds + backEnd)
                {
                    break;
                }

                result = UpdateResult.Drop;
                if (note.OverdriveIndex >= 0)
                {
                    _track.Overdrives[note.OverdriveIndex].HitCount = -1;
                }
                _stats.NoteIndex++;
            }

            if (result == UpdateResult.Drop)
            {
                if (_stats.Multiplier >= 2)
                {
                    result = UpdateResult.MultiplierDrop;
                    //  Marks a new segment of multiplier score gain
                    for (int i = 0; i < _activeSustains.Count; ++i)
                    {
                        ref var tracker = ref _activeSustains[i];
                        // Accounts for squeezing
                        if (time.Ticks <= tracker.BasePosition.Ticks)
                        {
                            continue;
                        }

                        ref readonly var sustain = ref _track.Sustains[tracker.SustainIndex];
                        long length = time.Ticks - tracker.BasePosition.Ticks;
                        long sustainScoreUnscaled =
                            _stats.Multiplier * EngineStats.POINTS_PER_BEAT * length * sustain.LaneCount;
                        _stats.Score += (long) Math.Ceiling((double) sustainScoreUnscaled / resolution);
                        tracker.BasePosition = time;
                    }
                    _stats.Multiplier = 1;
                }
                _stats.Combo = 0;
            }
            return result;
        }

        public UpdateResult UpdateInput(in DualTime time, long resolution, GuitarButtonMask buttonMask)
        {
            var result = UpdateTime(time, resolution);
            bool newStrum = IsNewStrum(buttonMask, _buttonMask);
            bool newFretting = (buttonMask & GuitarButtonMask.FretMask) != (_buttonMask & GuitarButtonMask.FretMask);

            if (newStrum || newFretting)
            {
                long noteIndex = _stats.NoteIndex;
                while (noteIndex < _track!.NoteGroups.Count)
                {
                    ref var note = ref _track.NoteGroups[noteIndex];
                    double noteDistance = double.MaxValue;
                    if (noteIndex > 0)
                    {
                        noteDistance = note.Position.Seconds - _track.NoteGroups[noteIndex - 1].Position.Seconds;
                    }

                    double frontEnd = _hitWindow.CalculateFrontEnd(noteDistance);
                    if (time.Seconds < note.Position.Seconds - frontEnd)
                    {
                        break;
                    }

                    // The UpdateTime() call ensures that the current note lies within the window
                    // if the break above isn't caught

                    int laneMask = (int)note.LaneMask;
                    int inputMask = (int)buttonMask;
                    if (note.LaneCount < 2 && !note.LaneMask.Has(GuitarLaneMask.Open_DisableAnchoring))
                    {
                        // Have to skip the first bit for the shift after the loop
                        // to behave properly
                        while ((laneMask & 2) == 0)
                        {
                            laneMask >>= 1;
                            inputMask >>= 1;
                        }
                    }

                    // Removes the anchor bit (or whatever zero bit was put in its place)
                    laneMask >>= 1;

                    bool skipRemainder = _stats.Combo > 0;
                    bool strumRequired = note.GuitarState == GuitarState.Strum || (note.GuitarState == GuitarState.Hopo && _stats.Combo == 0);
                    if (!strumRequired || newStrum || note.Position < _strumWindow)
                    {
                        // Note hit
                        if (laneMask == inputMask)
                        {
                            if (strumRequired)
                            {
                                if (note.Position < _strumWindow && newStrum && _strumState == StrumState.Pending_Overstrum)
                                {
                                    _strumWindow = time + _strumWindow;
                                }
                                _strumWindow = note.Position < _strumWindow && newStrum
                                    ? time + _strumWindow
                                    : DualTime.Zero;

                            }
                        }
                    }
                }
            }

            bool newWhammy = buttonMask.Has(GuitarButtonMask.Whammy);
            if (newWhammy)
            {
                for (long i = 0; i < _activeSustains.Count; i++)
                {
                    ref readonly var sustain = ref _track!.Sustains[_activeSustains[i].SustainIndex];
                    if (sustain.OverdriveIndex >= 0 && _track.Overdrives[sustain.OverdriveIndex].HitCount >= 0)
                    {
                        // Queue node changes in overdriveTracker
                        break;
                    }
                }
            }
            return result;
        }

        public void Dispose()
        {
            _track?.Dispose();
            _activeSustains.Dispose();
        }

        private static bool IsNewStrum(GuitarButtonMask current, GuitarButtonMask previous)
        {
            var currStrum = current & GuitarButtonMask.StrumMask;
            var previousStrum = previous & GuitarButtonMask.StrumMask;
            return currStrum > previousStrum
                || (current == GuitarButtonMask.StrumUp && previousStrum == GuitarButtonMask.StrumDown);
        }
    }
}
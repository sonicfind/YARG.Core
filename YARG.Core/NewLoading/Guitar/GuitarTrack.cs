using System;
using System.Collections.Generic;
using YARG.Core.Containers;
using YARG.Core.Game;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public class GuitarTrack : IDisposable
    {
        public int                             NumLanes   { get; }
        public YargNativeList<GuitarNoteGroup> NoteGroups { get; }
        public YargNativeList<GuitarSustain>   Sustains   { get; }
        public YargNativeList<OverdrivePhrase> Overdrives { get; }
        public YargNativeList<SoloPhrase>      Solos      { get; }

        public GuitarTrack Clone()
        {
            return new GuitarTrack
            (
                NumLanes,
                new YargNativeList<GuitarNoteGroup>(NoteGroups),
                new YargNativeList<GuitarSustain>(Sustains),
                new YargNativeList<OverdrivePhrase>(Overdrives),
                new YargNativeList<SoloPhrase>(Solos)
            );
        }

        public void Dispose()
        {
            NoteGroups.Dispose();
            Sustains.Dispose();
            Overdrives.Dispose();
            Solos.Dispose();
        }

        private GuitarTrack(
            int numLanes,
            YargNativeList<GuitarNoteGroup> noteGroups,
            YargNativeList<GuitarSustain> sustains,
            YargNativeList<OverdrivePhrase> overdrives,
            YargNativeList<SoloPhrase> solos
        )
        {
            NumLanes = numLanes;
            NoteGroups = noteGroups;
            Sustains = sustains;
            Overdrives = overdrives;
            Solos = solos;
        }

        private static readonly Dictionary<GuitarLaneMask, GuitarDoubleNote> _doubleNotesFiveFret = new()
        {
            {GuitarLaneMask.Green,                          new GuitarDoubleNote((int)GuitarLane.Red,    (int)GuitarLane.Green) },
            {GuitarLaneMask.Red,                            new GuitarDoubleNote((int)GuitarLane.Yellow, (int)GuitarLane.Red) },
            {GuitarLaneMask.Yellow,                         new GuitarDoubleNote((int)GuitarLane.Blue,   (int)GuitarLane.Yellow) },
            {GuitarLaneMask.Blue,                           new GuitarDoubleNote((int)GuitarLane.Orange, (int)GuitarLane.Blue) },
            {GuitarLaneMask.Orange,                         new GuitarDoubleNote((int)GuitarLane.Yellow, (int)GuitarLane.Orange) },
            {GuitarLaneMask.Green  | GuitarLaneMask.Red,    new GuitarDoubleNote((int)GuitarLane.Yellow, (int)GuitarLane.Red) },
            {GuitarLaneMask.Green  | GuitarLaneMask.Yellow, new GuitarDoubleNote((int)GuitarLane.Blue,   (int)GuitarLane.Yellow) },
            {GuitarLaneMask.Green  | GuitarLaneMask.Blue,   new GuitarDoubleNote((int)GuitarLane.Orange, (int)GuitarLane.Blue) },
            {GuitarLaneMask.Green  | GuitarLaneMask.Orange, new GuitarDoubleNote((int)GuitarLane.Yellow, (int)GuitarLane.Orange) },
            {GuitarLaneMask.Red    | GuitarLaneMask.Yellow, new GuitarDoubleNote((int)GuitarLane.Blue,   (int)GuitarLane.Yellow) },
            {GuitarLaneMask.Red    | GuitarLaneMask.Blue,   new GuitarDoubleNote((int)GuitarLane.Orange, (int)GuitarLane.Blue) },
            {GuitarLaneMask.Red    | GuitarLaneMask.Orange, new GuitarDoubleNote((int)GuitarLane.Green,  (int)GuitarLane.Red) },
            {GuitarLaneMask.Yellow | GuitarLaneMask.Blue,   new GuitarDoubleNote((int)GuitarLane.Orange, (int)GuitarLane.Blue) },
            {GuitarLaneMask.Yellow | GuitarLaneMask.Orange, new GuitarDoubleNote((int)GuitarLane.Red,    (int)GuitarLane.Yellow) },
            {GuitarLaneMask.Blue   | GuitarLaneMask.Orange, new GuitarDoubleNote((int)GuitarLane.Yellow, (int)GuitarLane.Blue) },
        };

        private static readonly Dictionary<GuitarLaneMask, GuitarDoubleNote> _doubleNotesSixFret = new()
        {
            {GuitarLaneMask.Black1,                         new GuitarDoubleNote((int)GuitarLane.Black2, (int)GuitarLane.Black1) },
            {GuitarLaneMask.Black2,                         new GuitarDoubleNote((int)GuitarLane.Black3, (int)GuitarLane.Black2) },
            {GuitarLaneMask.Black3,                         new GuitarDoubleNote((int)GuitarLane.White1, (int)GuitarLane.Black3) },
            {GuitarLaneMask.White1,                         new GuitarDoubleNote((int)GuitarLane.White2, (int)GuitarLane.White1) },
            {GuitarLaneMask.White2,                         new GuitarDoubleNote((int)GuitarLane.White3, (int)GuitarLane.White2) },
            {GuitarLaneMask.White3,                         new GuitarDoubleNote((int)GuitarLane.Black3, (int)GuitarLane.White3) },
            {GuitarLaneMask.Black1 | GuitarLaneMask.Black2, new GuitarDoubleNote((int)GuitarLane.Black3, (int)GuitarLane.Black2) },
            {GuitarLaneMask.Black1 | GuitarLaneMask.Black3, new GuitarDoubleNote((int)GuitarLane.White1, (int)GuitarLane.Black3) },
            {GuitarLaneMask.Black1 | GuitarLaneMask.White1, new GuitarDoubleNote((int)GuitarLane.White2, (int)GuitarLane.White1) },
            {GuitarLaneMask.Black1 | GuitarLaneMask.White2, new GuitarDoubleNote((int)GuitarLane.White3, (int)GuitarLane.White2) },
            {GuitarLaneMask.Black1 | GuitarLaneMask.White3, new GuitarDoubleNote((int)GuitarLane.Black3, (int)GuitarLane.White3) },
            {GuitarLaneMask.Black2 | GuitarLaneMask.Black3, new GuitarDoubleNote((int)GuitarLane.White1, (int)GuitarLane.Black3) },
            {GuitarLaneMask.Black2 | GuitarLaneMask.White1, new GuitarDoubleNote((int)GuitarLane.White2, (int)GuitarLane.White1) },
            {GuitarLaneMask.Black2 | GuitarLaneMask.White2, new GuitarDoubleNote((int)GuitarLane.White3, (int)GuitarLane.White2) },
            {GuitarLaneMask.Black2 | GuitarLaneMask.White3, new GuitarDoubleNote((int)GuitarLane.Black1, (int)GuitarLane.White3) },
            {GuitarLaneMask.Black3 | GuitarLaneMask.White1, new GuitarDoubleNote((int)GuitarLane.White2, (int)GuitarLane.White1) },
            {GuitarLaneMask.Black3 | GuitarLaneMask.White2, new GuitarDoubleNote((int)GuitarLane.White3, (int)GuitarLane.White2) },
            {GuitarLaneMask.Black3 | GuitarLaneMask.White3, new GuitarDoubleNote((int)GuitarLane.Black2, (int)GuitarLane.White3) },
            {GuitarLaneMask.White1 | GuitarLaneMask.White2, new GuitarDoubleNote((int)GuitarLane.White3, (int)GuitarLane.White2) },
            {GuitarLaneMask.White1 | GuitarLaneMask.White3, new GuitarDoubleNote((int)GuitarLane.Black3, (int)GuitarLane.White3) },
            {GuitarLaneMask.White2 | GuitarLaneMask.White3, new GuitarDoubleNote((int)GuitarLane.White1, (int)GuitarLane.White2) },
        };

        public static unsafe GuitarTrack Create<TConfig>(
            YARGChart chart,
            InstrumentTrack2<GuitarNote<TConfig>> instrumentTrack,
            in DualTime chartEndTime,
            in InstrumentSelection selection
        )
            where TConfig : unmanaged, IGuitarConfig<TConfig>
        {
            var difficultyTrack = instrumentTrack[selection.Difficulty];
            var track = new GuitarTrack
            (
                IGuitarConfig<TConfig>.MAX_LANES,
                new YargNativeList<GuitarNoteGroup>
                {
                    Capacity = difficultyTrack.Notes.Count
                },
                new YargNativeList<GuitarSustain>
                {
                    Capacity = difficultyTrack.Notes.Count
                },
                new YargNativeList<OverdrivePhrase>
                {
                    Capacity = difficultyTrack.Overdrives.Count
                },
                new YargNativeList<SoloPhrase>
                {
                    Capacity = difficultyTrack.Solos.Count
                }
            );

            for (int i = 0; i < difficultyTrack.Overdrives.Count; i++)
            {
                track.Overdrives.Add(new OverdrivePhrase());
            }

            foreach (var solo in difficultyTrack.Solos)
            {
                track.Solos.Add(new SoloPhrase(solo.Key, solo.Key + solo.Value));
            }

            long overdriveIndex = 0;
            long soloIndex = 0;

            // We have to validate note sustain data to ensure that we can run engine logic without issue.
            var priorNoteEnd = DualTime.Zero;
            for (long noteIndex = 0; noteIndex < difficultyTrack.Notes.Count; noteIndex++)
            {
                var note = difficultyTrack.Notes.Data + noteIndex;
                // This ensures that the result screen doesn't add notes that can never be played
                if (note->Key >= chartEndTime)
                {
                    break;
                }

                var group = new GuitarNoteGroup(
                    in note->Key,
                    CommonTrackCacheOps.GetOverdrivePhraseIndex(difficultyTrack.Overdrives, track.Overdrives, in note->Key, ref overdriveIndex),
                    CommonTrackCacheOps.GetSoloPhraseIndex(track.Solos, in note->Key, ref soloIndex)
                );

                long sustainIndex = track.Sustains.Count;
                var frets = (DualTime*)&note->Value.Lanes;

                bool sustainFretLeniency = note->Key < priorNoteEnd;
                if (!sustainFretLeniency && noteIndex + 1 < difficultyTrack.Notes.Count)
                {
                    sustainFretLeniency = IsDisjointOrExtended(
                        in note->Key,
                        in difficultyTrack.Notes[noteIndex + 1].Key,
                        frets,
                        IGuitarConfig<TConfig>.MAX_LANES);
                }

                for (int lane = 0; lane < IGuitarConfig<TConfig>.MAX_LANES; lane++)
                {
                    var fret = frets[lane];
                    if (!fret.IsActive())
                    {
                        continue;
                    }

                    int laneMask = 1 << lane;
                    var length = DualTime.Truncate(fret, chart.Settings.SustainCutoffThreshold);
                    // Anything greater than 1 signals that the sustain length satisfied the threshold
                    if (length.Ticks > 1)
                    {
                        long target = sustainIndex;
                        var endTime = length + note->Key;
                        while (target < track.Sustains.Count && track.Sustains[target].EndTime != endTime)
                        {
                            target++;
                        }

                        if (target == track.Sustains.Count)
                        {
                            track.Sustains.Add(
                                new GuitarSustain
                                (
                                    in endTime,
                                    sustainFretLeniency,
                                    group.OverdriveIndex
                                )
                            );

                            if (priorNoteEnd < endTime)
                            {
                                priorNoteEnd = endTime;
                            }
                        }

                        ref var sustain = ref track.Sustains[target];
                        sustain.LaneMask |= (GuitarLaneMask)laneMask;
                        sustain.LaneCount++;
                    }
                    group.LaneMask |= (GuitarLaneMask)laneMask;
                    group.LaneCount++;
                }

                group.SustainCount = track.Sustains.Count - sustainIndex;
                if (selection.Modifiers.Has(Modifier.AllStrums))
                {
                    group.GuitarState = GuitarState.Strum;
                }
                else if (selection.Modifiers.Has(Modifier.AllTaps))
                {
                    group.GuitarState = GuitarState.Tap;
                }
                else if (selection.Modifiers.Has(Modifier.AllHopos))
                {
                    group.GuitarState = GuitarState.Hopo;
                }
                else if (note->Value.State == GuitarState.Tap)
                {
                    // Going by YARG, the presence of this flag turns *all* taps to hopos, regardless
                    // of other factors (like notes before it)
                    group.GuitarState = !selection.Modifiers.Has(Modifier.TapsToHopos)
                        ? GuitarState.Tap : GuitarState.Hopo;
                }
                else
                {
                    switch (note->Value.State)
                    {
                        case GuitarState.Natural:
                        case GuitarState.Forced:
                        {
                            var naturalState = GuitarState.Strum;
                            if (group.LaneCount == 1 && track.NoteGroups.Count > 0)
                            {
                                ref readonly var previous = ref track.NoteGroups[track.NoteGroups.Count - 1];
                                if ((previous.LaneMask & group.LaneMask) == 0 &&
                                    note->Key.Ticks - previous.Position.Ticks <= chart.Settings.HopoThreshold)
                                {
                                    naturalState = GuitarState.Hopo;
                                }
                            }

                            // Natural + Strum = Strum
                            // Natural + Hopo  = Hopo
                            // Forced  + Strum = Hopo
                            // Forced  + Hopo  = Strum
                            // Think of it like xor, where "Strum" is 0
                            group.GuitarState = (group.GuitarState == GuitarState.Natural) == (naturalState == GuitarState.Strum)
                                ? GuitarState.Strum : GuitarState.Hopo;
                            break;
                        }
                        case GuitarState.Hopo:
                        case GuitarState.Strum:
                        {
                            group.GuitarState = note->Value.State;
                            break;
                        }
                        default:
                        {
                            throw new ArgumentOutOfRangeException();
                        }
                    }

                    if (group.GuitarState == GuitarState.Hopo && selection.Modifiers.Has(Modifier.HoposToTaps))
                    {
                        group.GuitarState = GuitarState.Tap;
                    }
                }

                track.NoteGroups.Add(in group);
            }

            if (selection.Modifiers.Has(Modifier.DoubleNotes))
            {
                ApplyDoubleNotes(track.NoteGroups, track.Sustains, IGuitarConfig<TConfig>.MAX_LANES);
            }

            if (selection.Modifiers.Has(Modifier.NoteShuffle))
            {
                ApplyRandomNotes(track.NoteGroups, track.Sustains, IGuitarConfig<TConfig>.MAX_LANES,
                    difficultyTrack.GetHashCode());
            }

            track.Sustains.TrimExcess();
            track.Overdrives.TrimExcess();
            track.Solos.TrimExcess();
            return track;
        }

        public static unsafe void ApplyDoubleNotes(
            YargNativeList<GuitarNoteGroup> groups,
            YargNativeList<GuitarSustain> sustains,
            int numLanes)
        {
            // NumLanes includes open note
            Span<long> noteTrackers = stackalloc long[numLanes];
            var mapping = numLanes == 6 ? _doubleNotesFiveFret : _doubleNotesSixFret;
            var sustainPtr = sustains.Data;
            for (long noteIndex = 0; noteIndex < groups.Count; noteIndex++)
            {
                ref var noteGroup = ref groups[noteIndex];

                // For the tracker to successfully lock out certain double note options,
                // we have to apply the end ticks for the sustains that already existed.
                for (int sustainOffset = 0; sustainOffset < noteGroup.SustainCount; sustainOffset++)
                {
                    ref readonly var sustain = ref sustainPtr[sustainOffset];
                    for (int lane = 1; lane < numLanes; lane++)
                    {
                        if (noteTrackers[lane] < sustain.EndTime.Ticks &&
                            sustain.LaneMask.Has((GuitarLaneMask)(1 << lane)))
                        {
                            noteTrackers[lane] = sustain.EndTime.Ticks;
                        }
                    }
                }

                // Open note is irrelevant in these operations
                if (mapping.TryGetValue(noteGroup.LaneMask & ~GuitarLaneMask.Open_DisableAnchoring, out var doubleNote))
                {
                    long sustainOffset2 = 0;
                    while (sustainOffset2 < noteGroup.SustainCount && !sustainPtr[sustainOffset2].LaneMask.Has(doubleNote.MaskQuery))
                    {
                        sustainOffset2++;
                    }

                    if (noteTrackers[doubleNote.LaneAddition] <= noteGroup.Position.Ticks)
                    {
                        if (sustainOffset2 == noteGroup.SustainCount ||
                            TryAddLaneToSustain(groups, ref sustainPtr[sustainOffset2], doubleNote.MaskAddition, noteIndex + 1,
                                ref noteTrackers[doubleNote.LaneAddition]))
                        {
                            noteGroup.LaneMask |= doubleNote.MaskAddition;
                            noteGroup.LaneCount++;
                            continue;
                        }
                    }

                    int lower = doubleNote.LaneQuery - 1;
                    int upper = doubleNote.LaneQuery + 1;
                    while (lower > 0 || upper < numLanes)
                    {
                        if (lower > 0)
                        {
                            var maskToAdd = (GuitarLaneMask) (1 << lower);
                            if (!noteGroup.LaneMask.Has(maskToAdd) && noteTrackers[lower] <= noteGroup.Position.Ticks)
                            {
                                if (sustainOffset2 == noteGroup.SustainCount ||
                                    TryAddLaneToSustain(groups, ref sustainPtr[sustainOffset2], maskToAdd, noteIndex + 1,
                                        ref noteTrackers[lower]))
                                {
                                    noteGroup.LaneMask |= maskToAdd;
                                    noteGroup.LaneCount++;
                                    break;
                                }
                            }
                            --lower;
                        }

                        if (upper < numLanes)
                        {
                            var maskToAdd = (GuitarLaneMask) (1 << upper);
                            if (!noteGroup.LaneMask.Has(maskToAdd) && noteTrackers[upper] <= noteGroup.Position.Ticks)
                            {
                                if (sustainOffset2 == noteGroup.SustainCount ||
                                    TryAddLaneToSustain(groups, ref sustainPtr[sustainOffset2], maskToAdd, noteIndex + 1,
                                        ref noteTrackers[upper]))
                                {
                                    noteGroup.LaneMask |= maskToAdd;
                                    noteGroup.LaneCount++;
                                    break;
                                }
                            }
                            ++upper;
                        }
                    }
                }
                sustainPtr += noteGroup.SustainCount;
            }
        }

        private static unsafe void ApplyRandomNotes(
            YargNativeList<GuitarNoteGroup> groups,
            YargNativeList<GuitarSustain> sustains,
            int numLanes,
            int rngSeed)
        {
            Span<GuitarLaneMask> masks = stackalloc GuitarLaneMask[numLanes];
            for (int i = 0; i < numLanes; i++)
            {
                masks[i] = (GuitarLaneMask) (1 << i);
            }

            var random = new Random(rngSeed);
            Span<int> laneMappingBuffer = stackalloc int[numLanes];
            Span<int> laneMapping       = stackalloc int[numLanes];
            for (int i = 0; i < numLanes; i++)
            {
                laneMappingBuffer[i] = i;
            }

            Span<long> laneEndTrackers = stackalloc long[numLanes];
            laneEndTrackers.Clear();
            var sustainPtr = sustains.Data;
            for (long noteIndex = 0; noteIndex < groups.Count; noteIndex++)
            {
                laneMappingBuffer.CopyTo(laneMapping);
                // Shuffle the lane map
                for (int lane = 1; lane < numLanes; lane++)
                {
                    int index = random.Next(lane, numLanes);
                    if (index != lane)
                    {
                        (laneMapping[index], laneMapping[lane]) = (laneMapping[lane], laneMapping[index]);
                    }
                }

                ref var noteGroup = ref groups[noteIndex];
                // For extended sustains to behave properly, the lane needs to stay the same for all the notes
                // for the length of the sustain
                for (int lane = 1; lane < numLanes; lane++)
                {
                    if (laneEndTrackers[lane] > noteGroup.Position.Ticks)
                    {
                        int search = 1;
                        while (laneMapping[search] != laneMappingBuffer[lane])
                        {
                            ++search;
                        }

                        if (search != lane)
                        {
                            (laneMapping[lane], laneMapping[search]) = (laneMapping[search], laneMapping[lane]);
                        }
                    }
                }

                var originalMask = noteGroup.LaneMask;
                noteGroup.LaneMask &= GuitarLaneMask.Open_DisableAnchoring;
                for (int lane = 1; lane < numLanes; lane++)
                {
                    if (originalMask.Has(masks[lane]))
                    {
                        noteGroup.LaneMask |= (GuitarLaneMask) (1 << laneMapping[lane]);
                    }
                }

                for (int sustainOffset = 0; sustainOffset < noteGroup.SustainCount; sustainOffset++)
                {
                    ref var sustain = ref sustainPtr[sustainOffset];
                    originalMask = sustain.LaneMask;
                    sustain.LaneMask &= GuitarLaneMask.Open_DisableAnchoring;
                    for (int lane = 1; lane < numLanes; lane++)
                    {
                        if (originalMask.Has(masks[lane]))
                        {
                            sustain.LaneMask |= (GuitarLaneMask) (1 << laneMapping[lane]);
                            laneEndTrackers[lane] = sustain.EndTime.Ticks;
                        }
                    }
                }
                sustainPtr += noteGroup.SustainCount;
                laneMapping.CopyTo(laneMappingBuffer);
            }
        }

        private static unsafe bool IsDisjointOrExtended(
            in DualTime currNote,
            in DualTime nextNote,
            DualTime* frets,
            int numLanes
        )
        {
            long fretIndex = 0;
            while (fretIndex < numLanes && !frets[fretIndex].IsActive())
            {
                fretIndex++;
            }

            if (fretIndex < numLanes)
            {
                ref readonly var firstFret = ref frets[fretIndex++];
                if (currNote + firstFret > nextNote)
                {
                    // Extended
                    return true;
                }

                while (fretIndex < numLanes)
                {
                    ref readonly var currFret = ref frets[fretIndex++];
                    if (currFret.IsActive() && currFret != firstFret)
                    {
                        // Disjoint
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool TryAddLaneToSustain(
            YargNativeList<GuitarNoteGroup> groups,
            ref GuitarSustain sustain,
            GuitarLaneMask laneToAdd,
            long groupIndex,
            ref long endTick)
        {
            while (groupIndex < groups.Count)
            {
                ref readonly var noteGroup = ref groups[groupIndex++];
                if (sustain.EndTime <= noteGroup.Position)
                {
                    break;
                }

                if (noteGroup.LaneMask.Has(laneToAdd))
                {
                    return false;
                }
            }

            sustain.LaneMask |= laneToAdd;
            sustain.LaneCount++;
            endTick = sustain.EndTime.Ticks;
            return true;
        }
    }
}
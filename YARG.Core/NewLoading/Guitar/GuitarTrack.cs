using System;
using System.Collections.Generic;
using System.Diagnostics;
using YARG.Core.Containers;
using YARG.Core.Game;
using YARG.Core.NewParsing;
using YARG.Core.Song;

namespace YARG.Core.NewLoading.Guitar
{
    public class GuitarTrack : IDisposable
    {
        public int                             NumLanes      { get; }
        public YargNativeList<DualTime>        NotePositions { get; }
        public YargNativeList<GuitarNoteGroup> NoteGroups    { get; }
        public YargNativeList<GuitarSustain>   Sustains      { get; }
        public YargNativeList<HittablePhrase>  Overdrives    { get; }
        public YargNativeList<HittablePhrase>  Solos         { get; }

        public GuitarTrack Clone()
        {
            return new GuitarTrack
            (
                NumLanes,
                new YargNativeList<DualTime>(NotePositions),
                new YargNativeList<GuitarNoteGroup>(NoteGroups),
                new YargNativeList<GuitarSustain>(Sustains),
                new YargNativeList<HittablePhrase>(Overdrives),
                new YargNativeList<HittablePhrase>(Solos)
            );
        }

        public void Dispose()
        {
            NotePositions.Dispose();
            NoteGroups.Dispose();
            Sustains.Dispose();
            Overdrives.Dispose();
            Solos.Dispose();
        }

        private GuitarTrack(
            int numLanes,
            YargNativeList<DualTime>        notePositions,
            YargNativeList<GuitarNoteGroup> noteGroups,
            YargNativeList<GuitarSustain>   sustains,
            YargNativeList<HittablePhrase>  overdrives,
            YargNativeList<HittablePhrase>  solos
        )
        {
            NumLanes = numLanes;
            NotePositions = notePositions;
            NoteGroups = noteGroups;
            Sustains = sustains;
            Overdrives = overdrives;
            Solos = solos;
        }

        public static unsafe GuitarTrack Create<TConfig>(
            YARGChart chart,
            InstrumentTrack2<GuitarNote<TConfig>> instrumentTrack,
            in DualTime chartEndTime,
            in InstrumentSelection selection)
            where TConfig : unmanaged, IGuitarConfig<TConfig>
        {
            var difficultyTrack = instrumentTrack[selection.Difficulty];

            var positions = new YargNativeList<DualTime>
            {
                Capacity = difficultyTrack.Notes.Count
            };
            // Alters `Count` to allow directly indexing (and overwriting) groups in the list
            positions.Resize_NoInitialization(difficultyTrack.Notes.Count);

            var groups = new YargNativeList<GuitarNoteGroup>
            {
                Capacity = difficultyTrack.Notes.Count
            };
            // Alters `Count` to allow directly indexing (and overwriting) groups in the list
            groups.Resize_NoInitialization(difficultyTrack.Notes.Count);

            var overdrives = CommonTrackOps.InitHittablePhrases(difficultyTrack.Overdrives);
            var solos = CommonTrackOps.InitHittablePhrases(difficultyTrack.Solos);

            var sustains = new YargNativeList<GuitarSustain>
            {
                Capacity = difficultyTrack.Notes.Count
            };

            // All indices will start at 1 instead of zero as we don't care
            // about shuffling open notes.
            //
            // Can't really shuffle the representation of "zero frets".
            //
            // We'll still stack allocate the mappings and lane trackers with the full
            // lane count to make using the indexers easier.
            Span<GuitarLaneMask> laneMappingBuffer = stackalloc GuitarLaneMask[IGuitarConfig<TConfig>.MAX_LANES];
            Span<GuitarLaneMask> laneMapping       = stackalloc GuitarLaneMask[IGuitarConfig<TConfig>.MAX_LANES];
            Span<long>           laneEndTrackers   = stackalloc long[IGuitarConfig<TConfig>.MAX_LANES];

            var rand = default(Random);
            if (selection.Modifiers.Has(Modifier.NoteShuffle))
            {
                rand = new Random(difficultyTrack.GetHashCode());
                for (int i = 0; i < IGuitarConfig<TConfig>.MAX_LANES; i++)
                {
                    laneMappingBuffer[i] = (GuitarLaneMask)(1 << i);
                }
                laneEndTrackers.Clear();
            }
            else
            {
                for (int i = 0; i < IGuitarConfig<TConfig>.MAX_LANES; i++)
                {
                    laneMapping[i] = (GuitarLaneMask)(1 << i);
                }
            }

            // We have to validate note sustain data to ensure that we can run engine logic without issue.
            var priorNoteEnd = DualTime.Zero;

            for (int noteIndex = 0, overdriveIndex = 0, soloIndex = 0;
                noteIndex < difficultyTrack.Notes.Count;
                noteIndex++)
            {
                var note = difficultyTrack.Notes.Data + noteIndex;
                // This ensures that the result screen doesn't add notes that can never be played
                if (note->Key >= chartEndTime)
                {
                    positions.Resize_NoInitialization(noteIndex);
                    groups.Resize_NoInitialization(noteIndex);
                    positions.TrimExcess();
                    groups.TrimExcess();
                    break;
                }

                var group = new GuitarNoteGroup
                {
                    LaneMask = GuitarLaneMask.None,
                    LaneCount = 0,
                    SustainIndex = sustains.Count,
                    SustainCount = 0,
                    OverdriveIndex = CommonTrackOps.GetHittablePhraseIndex(overdrives, note->Key, ref overdriveIndex),
                    SoloIndex = CommonTrackOps.GetHittablePhraseIndex(solos, note->Key, ref soloIndex),
                };

                if (rand != null)
                {
                    laneMappingBuffer.CopyTo(laneMapping);
                    for (int lane = 1; lane < IGuitarConfig<TConfig>.MAX_LANES; lane++)
                    {
                        if (laneEndTrackers[lane] > note->Key.Ticks)
                        {
                            continue;
                        }

                        int index = rand.Next(1, IGuitarConfig<TConfig>.MAX_LANES);
                        if (index != lane && laneEndTrackers[index] <= note->Key.Ticks)
                        {
                            (laneMapping[index], laneMapping[lane]) = (laneMapping[lane], laneMapping[index]);
                        }
                    }
                }

                bool overlapsPreviousNote = note->Key < priorNoteEnd;

                var frets = (DualTime*)&note->Value.Lanes;
                for (int lane = 0; lane < IGuitarConfig<TConfig>.MAX_LANES; lane++)
                {
                    var fret = frets[lane];
                    if (!fret.IsActive())
                    {
                        continue;
                    }

                    // Anything greater than 1 signals that the sustain length satisfied the threshold
                    if (fret.Ticks >= chart.Settings.SustainCutoffThreshold)
                    {
                        var endTime = note->Key + fret;
                        laneEndTrackers[lane] = endTime.Ticks;

                        int target = group.SustainIndex;
                        while (target < sustains.Count && sustains[target].EndTime != endTime)
                        {
                            target++;
                        }

                        if (target == sustains.Count)
                        {
                            sustains.Add(new GuitarSustain(in endTime));

                            if (priorNoteEnd < endTime)
                            {
                                priorNoteEnd = endTime;
                            }

                            group.SustainCount++;
                        }

                        ref var sustain = ref sustains[target];
                        sustain.LaneMask |= laneMapping[lane];
                        sustain.LaneCount++;
                    }
                    group.LaneMask |= laneMapping[lane];
                    group.LaneCount++;
                }

                // We give sustains fret-leniency in specific circumstances:
                // 1.     If some previous note had a sustain that overlapped with the current one [Extended]
                // 2 & 3. If the current note contains frets with differing sustain lengths        [Disjointed]
                // 4.     If the current note extends past the note that follows it                [Extended]
                if (overlapsPreviousNote ||
                    group.SustainCount > 1 ||
                    (group.SustainCount == 1 && sustains[^1].LaneCount != group.LaneCount) ||
                    (noteIndex + 1 < difficultyTrack.Notes.Count && difficultyTrack.Notes[noteIndex + 1].Key < priorNoteEnd))
                {
                    for (int i = 0; i < group.SustainCount; i++)
                    {
                        sustains[group.SustainIndex + i].HasFretLeniency = true;
                    }
                }

                if (rand != null)
                {
                    laneMapping.CopyTo(laneMappingBuffer);
                }

                Debug.Assert(group.LaneCount > 0, "Parser failed to attribute lanes to the note");

                if (selection.Modifiers.Has(Modifier.AllStrums))
                {
                    group.Style = GuitarNoteStyle.Strum;
                }
                else if (selection.Modifiers.Has(Modifier.AllTaps))
                {
                    group.Style = GuitarNoteStyle.Tap;
                }
                else if (selection.Modifiers.Has(Modifier.AllHopos))
                {
                    group.Style = GuitarNoteStyle.Hopo;
                }
                else switch (note->Value.State)
                {
                    case GuitarState.Natural:
                    case GuitarState.Forced:
                    {
                        var naturalState = GuitarState.Strum;
                        if (group.LaneCount == 1 && groups.Count > 0)
                        {
                            var previousTickPosition = positions[noteIndex - 1].Ticks;
                            var previousLanes = groups[noteIndex - 1].LaneMask;

                            if ((previousLanes & group.LaneMask) == 0 &&
                                note->Key.Ticks - previousTickPosition <= chart.Settings.HopoThreshold)
                            {
                                naturalState = GuitarState.Hopo;
                            }
                        }

                        // Natural + Strum = Strum
                        // Natural + Hopo  = Hopo
                        // Forced  + Strum = Hopo
                        // Forced  + Hopo  = Strum
                        // Think of it like xor, where "Strum" is 0
                        if ((note->Value.State == GuitarState.Natural) == (naturalState == GuitarState.Strum))
                        {
                            group.Style = GuitarNoteStyle.Strum;
                            break;
                        }
                        goto case GuitarState.Hopo;
                    }
                    case GuitarState.Hopo:
                    {
                        group.Style = !selection.Modifiers.Has(Modifier.HoposToTaps) ?  GuitarNoteStyle.Hopo : GuitarNoteStyle.Tap;
                        break;
                    }
                    case GuitarState.Tap:
                    {
                        // Going by YARG, the presence of this flag turns *all* taps to hopos, regardless
                        // of other factors (like notes before it)
                        group.Style = !selection.Modifiers.Has(Modifier.TapsToHopos) ? GuitarNoteStyle.Tap : GuitarNoteStyle.Hopo;
                        break;
                    }
                    case GuitarState.Strum:
                    {
                        group.Style = GuitarNoteStyle.Strum;
                        break;
                    }
                    default:
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                }

                // Hopos occupying the same lanes as a prior note are incorrect behavior,
                // so we'll forcibly override them with strums
                if (group.Style == GuitarNoteStyle.Hopo &&
                    noteIndex > 0 && groups[noteIndex - 1].LaneMask == group.LaneMask)
                {
                    group.Style = GuitarNoteStyle.Strum;
                }

                groups[noteIndex] = group;
                positions[noteIndex] = note->Key;
            }

            sustains.TrimExcess();
            overdrives.TrimExcess();
            solos.TrimExcess();

            if (selection.Modifiers.Has(Modifier.DoubleNotes))
            {
                ApplyDoubleNotes(positions, groups, sustains, IGuitarConfig<TConfig>.MAX_LANES);
            }

            return new GuitarTrack(IGuitarConfig<TConfig>.MAX_LANES, positions, groups, sustains, overdrives, solos);
        }

        private static readonly GuitarDoubleNote[] _doubleNotesFiveFret = new GuitarDoubleNote[byte.MaxValue + 1];
        private static readonly GuitarDoubleNote[] _doubleNotesSixFret = new GuitarDoubleNote[byte.MaxValue + 1];

        static GuitarTrack()
        {
            _doubleNotesFiveFret[(int) GuitarLaneMask.Green                          ] = new GuitarDoubleNote((int)GuitarLane.Red,    (int)GuitarLane.Green);
            _doubleNotesFiveFret[(int) GuitarLaneMask.Red                            ] = new GuitarDoubleNote((int)GuitarLane.Yellow, (int)GuitarLane.Red);
            _doubleNotesFiveFret[(int) GuitarLaneMask.Yellow                         ] = new GuitarDoubleNote((int)GuitarLane.Blue,   (int)GuitarLane.Yellow);
            _doubleNotesFiveFret[(int) GuitarLaneMask.Blue                           ] = new GuitarDoubleNote((int)GuitarLane.Orange, (int)GuitarLane.Blue);
            _doubleNotesFiveFret[(int) GuitarLaneMask.Orange                         ] = new GuitarDoubleNote((int)GuitarLane.Yellow, (int)GuitarLane.Orange);
            _doubleNotesFiveFret[(int)(GuitarLaneMask.Green  | GuitarLaneMask.Red)   ] = new GuitarDoubleNote((int)GuitarLane.Yellow, (int)GuitarLane.Red);
            _doubleNotesFiveFret[(int)(GuitarLaneMask.Green  | GuitarLaneMask.Yellow)] = new GuitarDoubleNote((int)GuitarLane.Blue,   (int)GuitarLane.Yellow);
            _doubleNotesFiveFret[(int)(GuitarLaneMask.Green  | GuitarLaneMask.Blue)  ] = new GuitarDoubleNote((int)GuitarLane.Orange, (int)GuitarLane.Blue);
            _doubleNotesFiveFret[(int)(GuitarLaneMask.Green  | GuitarLaneMask.Orange)] = new GuitarDoubleNote((int)GuitarLane.Yellow, (int)GuitarLane.Orange);
            _doubleNotesFiveFret[(int)(GuitarLaneMask.Red    | GuitarLaneMask.Yellow)] = new GuitarDoubleNote((int)GuitarLane.Blue,   (int)GuitarLane.Yellow);
            _doubleNotesFiveFret[(int)(GuitarLaneMask.Red    | GuitarLaneMask.Blue)  ] = new GuitarDoubleNote((int)GuitarLane.Orange, (int)GuitarLane.Blue);
            _doubleNotesFiveFret[(int)(GuitarLaneMask.Red    | GuitarLaneMask.Orange)] = new GuitarDoubleNote((int)GuitarLane.Green,  (int)GuitarLane.Red);
            _doubleNotesFiveFret[(int)(GuitarLaneMask.Yellow | GuitarLaneMask.Blue)  ] = new GuitarDoubleNote((int)GuitarLane.Orange, (int)GuitarLane.Blue);
            _doubleNotesFiveFret[(int)(GuitarLaneMask.Yellow | GuitarLaneMask.Orange)] = new GuitarDoubleNote((int)GuitarLane.Red,    (int)GuitarLane.Yellow);
            _doubleNotesFiveFret[(int)(GuitarLaneMask.Blue   | GuitarLaneMask.Orange)] = new GuitarDoubleNote((int)GuitarLane.Yellow, (int)GuitarLane.Blue);

            _doubleNotesSixFret[(int) GuitarLaneMask.Black1                         ] = new GuitarDoubleNote((int)GuitarLane.Black2, (int)GuitarLane.Black1);
            _doubleNotesSixFret[(int) GuitarLaneMask.Black2                         ] = new GuitarDoubleNote((int)GuitarLane.Black3, (int)GuitarLane.Black2);
            _doubleNotesSixFret[(int) GuitarLaneMask.Black3                         ] = new GuitarDoubleNote((int)GuitarLane.White1, (int)GuitarLane.Black3);
            _doubleNotesSixFret[(int) GuitarLaneMask.White1                         ] = new GuitarDoubleNote((int)GuitarLane.White2, (int)GuitarLane.White1);
            _doubleNotesSixFret[(int) GuitarLaneMask.White2                         ] = new GuitarDoubleNote((int)GuitarLane.White3, (int)GuitarLane.White2);
            _doubleNotesSixFret[(int) GuitarLaneMask.White3                         ] = new GuitarDoubleNote((int)GuitarLane.Black3, (int)GuitarLane.White3);
            _doubleNotesSixFret[(int)(GuitarLaneMask.Black1 | GuitarLaneMask.Black2)] = new GuitarDoubleNote((int)GuitarLane.Black3, (int)GuitarLane.Black2);
            _doubleNotesSixFret[(int)(GuitarLaneMask.Black1 | GuitarLaneMask.Black3)] = new GuitarDoubleNote((int)GuitarLane.White1, (int)GuitarLane.Black3);
            _doubleNotesSixFret[(int)(GuitarLaneMask.Black1 | GuitarLaneMask.White1)] = new GuitarDoubleNote((int)GuitarLane.White2, (int)GuitarLane.White1);
            _doubleNotesSixFret[(int)(GuitarLaneMask.Black1 | GuitarLaneMask.White2)] = new GuitarDoubleNote((int)GuitarLane.White3, (int)GuitarLane.White2);
            _doubleNotesSixFret[(int)(GuitarLaneMask.Black1 | GuitarLaneMask.White3)] = new GuitarDoubleNote((int)GuitarLane.Black3, (int)GuitarLane.White3);
            _doubleNotesSixFret[(int)(GuitarLaneMask.Black2 | GuitarLaneMask.Black3)] = new GuitarDoubleNote((int)GuitarLane.White1, (int)GuitarLane.Black3);
            _doubleNotesSixFret[(int)(GuitarLaneMask.Black2 | GuitarLaneMask.White1)] = new GuitarDoubleNote((int)GuitarLane.White2, (int)GuitarLane.White1);
            _doubleNotesSixFret[(int)(GuitarLaneMask.Black2 | GuitarLaneMask.White2)] = new GuitarDoubleNote((int)GuitarLane.White3, (int)GuitarLane.White2);
            _doubleNotesSixFret[(int)(GuitarLaneMask.Black2 | GuitarLaneMask.White3)] = new GuitarDoubleNote((int)GuitarLane.Black1, (int)GuitarLane.White3);
            _doubleNotesSixFret[(int)(GuitarLaneMask.Black3 | GuitarLaneMask.White1)] = new GuitarDoubleNote((int)GuitarLane.White2, (int)GuitarLane.White1);
            _doubleNotesSixFret[(int)(GuitarLaneMask.Black3 | GuitarLaneMask.White2)] = new GuitarDoubleNote((int)GuitarLane.White3, (int)GuitarLane.White2);
            _doubleNotesSixFret[(int)(GuitarLaneMask.Black3 | GuitarLaneMask.White3)] = new GuitarDoubleNote((int)GuitarLane.Black2, (int)GuitarLane.White3);
            _doubleNotesSixFret[(int)(GuitarLaneMask.White1 | GuitarLaneMask.White2)] = new GuitarDoubleNote((int)GuitarLane.White3, (int)GuitarLane.White2);
            _doubleNotesSixFret[(int)(GuitarLaneMask.White1 | GuitarLaneMask.White3)] = new GuitarDoubleNote((int)GuitarLane.Black3, (int)GuitarLane.White3);
            _doubleNotesSixFret[(int)(GuitarLaneMask.White2 | GuitarLaneMask.White3)] = new GuitarDoubleNote((int)GuitarLane.White1, (int)GuitarLane.White2);
        }

        /// <remarks>
        /// Double Notes can be applied in the middle of gameplay. For simplicity, callers will pass the number of lanes
        /// as a parameter instead of the generic... for now at least. We'll see if that changes later.
        /// </remarks>
        public static unsafe void ApplyDoubleNotes(
            YargNativeList<DualTime> positions,
            YargNativeList<GuitarNoteGroup> groups,
            YargNativeList<GuitarSustain> sustains,
            int numLanes
            )
        {
            // NumLanes includes open note
            //
            // We can't capture Span<long> in local functions, unfortunately, so pointer it is
            var noteTrackers = stackalloc long[numLanes];

            var mapping = numLanes == 6 ? _doubleNotesFiveFret : _doubleNotesSixFret;
            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                ref var noteGroup = ref groups[groupIndex];
                var groupSustains = sustains.Data + noteGroup.SustainIndex;

                // We ignore open note state for these operations
                var doubleNote = mapping[(int)(noteGroup.LaneMask & ~GuitarLaneMask.Open_DisableAnchoring)];
                if (doubleNote.LaneAddition != 0)
                {
                    long tickPosition = positions[groupIndex].Ticks;
                    var maskQuery = (GuitarLaneMask) (1 << doubleNote.LaneQuery);

                    // We only care about the one sustain that contains the lane being queried.
                    // Everything else we can disregard.
                    int sustainOffset = 0;
                    while (sustainOffset < noteGroup.SustainCount && !groupSustains[sustainOffset].LaneMask.Has(maskQuery))
                    {
                        sustainOffset++;
                    }

                    if (noteTrackers[doubleNote.LaneAddition] > tickPosition ||
                        !TryAddMask(
                            ref noteGroup,
                            sustainOffset,
                            (GuitarLaneMask) (1 << doubleNote.LaneAddition),
                            groupIndex,
                            doubleNote.LaneAddition)
                        )
                    {
                        // We split the queries in both directions in an attempt to apply double notes in a
                        // "closest adjacency" manner. Whichever reacts first gets the note.
                        int lower = doubleNote.LaneQuery - 1;
                        int upper = doubleNote.LaneQuery + 1;
                        while (lower > 0 || upper < numLanes)
                        {
                            if (lower > 0)
                            {
                                var maskToAdd = (GuitarLaneMask) (1 << lower);
                                if (!noteGroup.LaneMask.Has(maskToAdd) && noteTrackers[lower] <= tickPosition)
                                {
                                    if (TryAddMask(ref noteGroup, sustainOffset, maskToAdd, groupIndex, lower))
                                    {
                                        break;
                                    }
                                }
                                --lower;
                            }

                            if (upper < numLanes)
                            {
                                var maskToAdd = (GuitarLaneMask) (1 << upper);
                                if (!noteGroup.LaneMask.Has(maskToAdd) && noteTrackers[upper] <= tickPosition)
                                {
                                    if (TryAddMask(ref noteGroup, sustainOffset, maskToAdd, groupIndex, upper))
                                    {
                                        break;
                                    }
                                }
                                ++upper;
                            }
                        }
                    }
                }

                // For the tracker to successfully lock out certain double note options,
                // we have to apply the end ticks for the sustains that already existed.
                for (int sustainOffset = 0; sustainOffset < noteGroup.SustainCount; sustainOffset++)
                {
                    ref readonly var sustain = ref sustains[noteGroup.SustainIndex + sustainOffset];
                    for (int lane = 1; lane < numLanes; lane++)
                    {
                        if (noteTrackers[lane] < sustain.EndTime.Ticks &&
                            sustain.LaneMask.Has((GuitarLaneMask)(1 << lane)))
                        {
                            noteTrackers[lane] = sustain.EndTime.Ticks;
                        }
                    }
                }
            }

            return;

            bool TryAddMask(ref GuitarNoteGroup noteGroup, int sustainOffset, GuitarLaneMask maskToAdd, int groupIndex, int laneAddition)
            {
                if (sustainOffset != noteGroup.SustainCount)
                {
                    ref var sustain = ref sustains[noteGroup.SustainIndex + sustainOffset];
                    while (groupIndex < groups.Count)
                    {
                        if (sustain.EndTime <= positions[groupIndex])
                        {
                            break;
                        }

                        if (groups[groupIndex].LaneMask.Has(maskToAdd))
                        {
                            return false;
                        }

                        ++groupIndex;
                    }

                    sustain.LaneMask |= maskToAdd;
                    sustain.LaneCount++;
                    noteTrackers[laneAddition] = sustain.EndTime.Ticks;
                }

                noteGroup.LaneMask |= maskToAdd;
                noteGroup.LaneCount++;
                return true;
            }
        }
    }
}

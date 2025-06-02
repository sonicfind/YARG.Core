using System;
using System.Collections.Generic;
using System.Diagnostics;
using YARG.Core.Containers;
using YARG.Core.Game;
using YARG.Core.IO;
using YARG.Core.NewParsing;
using YARG.Core.Song;

namespace YARG.Core.NewLoading.Guitar
{
    public class GuitarTrack : IDisposable
    {
        public int                              NumLanes   { get; }
        public TimedCollection<GuitarNoteGroup> Notes      { get; }
        public TimedCollection<GuitarSustain>   Sustains   { get; }
        public FixedArray<HittablePhrase>       Overdrives { get; }
        public FixedArray<HittablePhrase>       Solos      { get; }

        public GuitarTrack Clone()
        {
            return new GuitarTrack
            (
                NumLanes,
                Notes.Clone(),
                Sustains.Clone(),
                Overdrives.Clone(),
                Solos.Clone()
            );
        }

        public void Dispose()
        {
            Notes.Dispose();
            Sustains.Dispose();
            Overdrives.Dispose();
            Solos.Dispose();
        }

        private GuitarTrack(
            int numLanes,
            TimedCollection<GuitarNoteGroup> notes,
            TimedCollection<GuitarSustain>   sustains,
            FixedArray<HittablePhrase>  overdrives,
            FixedArray<HittablePhrase>  solos
        )
        {
            NumLanes = numLanes;
            Notes = notes;
            Sustains = sustains;
            Overdrives = overdrives;
            Solos = solos;
        }

        public static unsafe GuitarTrack Create<TConfig>(
            YARGChart chart,
            InstrumentTrack2<GuitarNote<TConfig>> instrumentTrack,
            DualTime chartEndTime,
            InstrumentSelection selection)
            where TConfig : unmanaged, IGuitarConfig<TConfig>
        {
            var difficultyTrack = instrumentTrack[selection.Difficulty];

            using var notes = TimedCollection<GuitarNoteGroup>.Create(difficultyTrack.Notes.Count);
            using var sustains = TimedCollection<GuitarSustain>.Create(difficultyTrack.Notes.Count);

            using var overdrives = CommonTrackOps.InitHittablePhrases(difficultyTrack.Overdrives);
            using var solos = CommonTrackOps.InitHittablePhrases(difficultyTrack.Solos);

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
            long priorNoteEnd = 0;

            int sustainCount = 0;

            for (int noteIndex = 0, overdriveIndex = 0, soloIndex = 0;
                noteIndex < difficultyTrack.Notes.Count;
                noteIndex++)
            {
                var note = difficultyTrack.Notes.Data + noteIndex;
                // This ensures that the result screen doesn't add notes that can never be played
                if (note->Key >= chartEndTime)
                {
                    notes.Resize(noteIndex);
                    break;
                }

                var group = new GuitarNoteGroup
                {
                    LaneMask = GuitarLaneMask.None,
                    LaneCount = 0,
                    SustainIndex = sustainCount,
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

                bool overlapsPreviousNote = note->Key.Ticks < priorNoteEnd;

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
                        while (target < sustainCount && sustains.Ticks[target] != endTime.Ticks)
                        {
                            target++;
                        }

                        if (target == sustains.Length)
                        {
                            sustains.Resize(sustains.Length * 2);
                        }

                        ref var sustain = ref sustains[target];
                        if (target == sustainCount)
                        {
                            if (priorNoteEnd < endTime.Ticks)
                            {
                                priorNoteEnd = endTime.Ticks;
                            }

                            sustains.Ticks[target] = endTime.Ticks;
                            sustains.Seconds[target] = endTime.Seconds;

                            // Ensures the sustain doesn't start with garbage data
                            sustain.LaneMask = GuitarLaneMask.None;
                            sustain.LaneCount = 0;

                            group.SustainCount++;
                            sustainCount++;
                        }

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
                    (group.SustainCount == 1 && sustains[sustainCount - 1].LaneCount != group.LaneCount) ||
                    (noteIndex + 1 < difficultyTrack.Notes.Count && difficultyTrack.Notes[noteIndex + 1].Key.Ticks < priorNoteEnd))
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
                        if (group.LaneCount == 1 && noteIndex > 0)
                        {
                            var previousTickPosition = notes.Ticks[noteIndex - 1];
                            var previousLanes = notes[noteIndex - 1].LaneMask;

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
                    noteIndex > 0 && notes[noteIndex - 1].LaneMask == group.LaneMask)
                {
                    group.Style = GuitarNoteStyle.Strum;
                }


                notes.Ticks[noteIndex] = note->Key.Ticks;
                notes.Seconds[noteIndex] = note->Key.Seconds;
                notes.Elements[noteIndex] = group;
            }

            sustains.Resize(sustainCount);

            if (selection.Modifiers.Has(Modifier.DoubleNotes))
            {
                ApplyDoubleNotes(
                    notes.Ticks,
                    notes.Elements,
                    sustains.Ticks,
                    sustains.Elements,
                    IGuitarConfig<TConfig>.MAX_LANES
                );
            }

            return new GuitarTrack(
                IGuitarConfig<TConfig>.MAX_LANES,
                notes.TransferOwnership(),
                sustains.TransferOwnership(),
                overdrives.TransferOwnership(),
                solos.TransferOwnership()
            );
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
        /// <br></br>
        /// Also, all the parameters will stay separated
        /// </remarks>
        public static unsafe void ApplyDoubleNotes(
            FixedArray<long> noteTickPositions,
            FixedArray<GuitarNoteGroup> noteGroups,
            FixedArray<long> sustainEndTickPositions,
            FixedArray<GuitarSustain> sustains,
            int numLanes
            )
        {
            // NumLanes includes open note
            //
            // We can't capture Span<long> in local functions, unfortunately, so pointer it is
            var noteTrackers = stackalloc long[numLanes];

            var mapping = numLanes == 6 ? _doubleNotesFiveFret : _doubleNotesSixFret;
            for (int groupIndex = 0; groupIndex < noteGroups.Length; groupIndex++)
            {
                ref var noteGroup = ref noteGroups[groupIndex];

                // We ignore open note state for these operations
                var doubleNote = mapping[(int)(noteGroup.LaneMask & ~GuitarLaneMask.Open_DisableAnchoring)];
                if (doubleNote.LaneAddition != 0)
                {
                    long tickPosition = noteTickPositions[groupIndex];
                    var maskQuery = (GuitarLaneMask) (1 << doubleNote.LaneQuery);

                    // We only care about the one sustain that contains the lane being queried.
                    // Everything else we can disregard.
                    int sustainOffset = 0;
                    while (sustainOffset < noteGroup.SustainCount
                        && !sustains[noteGroup.SustainIndex + sustainOffset].LaneMask.Has(maskQuery))
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
                        // "> 0" as we don't include open note for doubling
                        while (lower > 0 || upper < numLanes)
                        {
                            if (lower > 0)
                            {
                                var maskToAdd = (GuitarLaneMask) (1 << lower);
                                if (!noteGroup.LaneMask.Has(maskToAdd)
                                    && noteTrackers[lower] <= tickPosition
                                    && TryAddMask(ref noteGroup, sustainOffset, maskToAdd, groupIndex, lower))
                                {
                                    break;
                                }
                                --lower;
                            }

                            if (upper < numLanes)
                            {
                                var maskToAdd = (GuitarLaneMask) (1 << upper);
                                if (!noteGroup.LaneMask.Has(maskToAdd)
                                    && noteTrackers[upper] <= tickPosition
                                    && TryAddMask(ref noteGroup, sustainOffset, maskToAdd, groupIndex, upper))
                                {
                                    break;
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
                    int sustainIndex = noteGroup.SustainIndex + sustainOffset;

                    var lanes = sustains[sustainIndex].LaneMask;
                    var endTicks = sustainEndTickPositions[sustainIndex];

                    for (int lane = 1; lane < numLanes; lane++)
                    {
                        if (noteTrackers[lane] < endTicks && lanes.Has((GuitarLaneMask)(1 << lane)))
                        {
                            noteTrackers[lane] = endTicks;
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
                    var endTime = sustainEndTickPositions[noteGroup.SustainIndex + sustainOffset];

                    while (groupIndex < noteGroups.Length)
                    {
                        if (endTime <= noteTickPositions[groupIndex])
                        {
                            break;
                        }

                        if (noteGroups[groupIndex].LaneMask.Has(maskToAdd))
                        {
                            return false;
                        }

                        ++groupIndex;
                    }

                    sustain.LaneMask |= maskToAdd;
                    sustain.LaneCount++;
                    noteTrackers[laneAddition] = endTime;
                }

                noteGroup.LaneMask |= maskToAdd;
                noteGroup.LaneCount++;
                return true;
            }
        }
    }
}

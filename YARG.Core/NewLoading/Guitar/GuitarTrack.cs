using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using YARG.Core.Chart;
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

        public long TryAddLaneToGroup(long groupIndex, long sustainIndex, GuitarLaneMask originalMask, int lane)
        {
            ref var noteGroup = ref NoteGroups[groupIndex];
            var mask = (GuitarLaneMask)(1 << lane);
            long sustainOffset = 0;
            while (sustainOffset < noteGroup.SustainCount)
            {
                ref readonly var sustain = ref Sustains[sustainIndex + sustainOffset];
                if (!sustain.LaneMask.HasFlag(originalMask))
                {
                    ++sustainOffset;
                    continue;
                }

                if (IsLaneOccupied(groupIndex + 1, in sustain.EndTime, mask))
                {
                    return -1;
                }
                break;
            }

            noteGroup.LaneMask |= mask;
            if (sustainOffset == noteGroup.SustainCount)
            {
                return noteGroup.Position.Ticks + 1;
            }

            ref var sustainToModify = ref Sustains[sustainIndex + sustainOffset];
            sustainToModify.LaneMask |= mask;
            return sustainToModify.EndTime.Ticks;
        }

        public bool IsLaneOccupied(long startIndex, in DualTime endTime, GuitarLaneMask mask)
        {
            while (startIndex < NoteGroups.Count)
            {
                ref readonly var futureNote = ref NoteGroups[startIndex++];
                if (endTime <= futureNote.Position)
                {
                    break;
                }

                if (futureNote.LaneMask.HasFlag(mask))
                {
                    return true;
                }
            }
            return false;
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

        private enum SustainLeniency
        {
            Normal,
            Extended,
            Disjoint
        }

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

                var nextNotePosition = noteIndex + 1 < difficultyTrack.Notes.Count
                    ? difficultyTrack.Notes[noteIndex + 1].Key
                    : DualTime.Max;

                var sustainLeniency = CheckSustainLeniency<TConfig>(in note->Key, in nextNotePosition, frets);
                for (int lane = 0; lane < IGuitarConfig<TConfig>.MAX_LANES; lane++)
                {
                    var fret = frets[lane];
                    // We have to account for bad charting where the current note has an active lane
                    // even though the prior note sustains through it with the same lane
                    if (!fret.IsActive())
                    {
                        continue;
                    }

                    int laneMask = 1 << lane;
                    var length = DualTime.Truncate(fret, chart.Settings.SustainCutoffThreshold);
                    // Anything greater than 1 signals that the sustain length satisfied the threshold
                    if (length.Ticks > 1)
                    {
                        if (sustainLeniency == SustainLeniency.Disjoint || sustainIndex == track.Sustains.Count)
                        {
                            var endTime = length + note->Key;
                            track.Sustains.Add(
                                new GuitarSustain
                                (
                                    in endTime,
                                    group.OverdriveIndex,
                                    sustainLeniency != SustainLeniency.Normal
                                )
                            );

                            if (priorNoteEnd < endTime)
                            {
                                priorNoteEnd = endTime;
                            }
                        }

                        ref var sustain = ref track.Sustains[track.Sustains.Count - 1];
                        sustain.LaneMask |= (GuitarLaneMask)laneMask;
                        sustain.LaneCount++;
                    }
                    group.LaneMask |= (GuitarLaneMask)laneMask;
                    group.LaneCount++;
                }

                group.SustainCount = (int)(track.Sustains.Count - sustainIndex);
                if (selection.Modifiers.HasFlag(Modifier.AllStrums))
                {
                    group.GuitarState = GuitarState.Strum;
                }
                else if (selection.Modifiers.HasFlag(Modifier.AllTaps))
                {
                    group.GuitarState = GuitarState.Tap;
                }
                else if (selection.Modifiers.HasFlag(Modifier.AllHopos))
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

            if (selection.Modifiers.Has(Modifier.NoteShuffle))
            {
                int rngSeed = difficultyTrack.GetHashCode();
                Span<int> laneMappingBuffer = stackalloc int[IGuitarConfig<TConfig>.MAX_LANES];
                Span<int> laneMapping       = stackalloc int[IGuitarConfig<TConfig>.MAX_LANES];
                for (int i = 0; i < IGuitarConfig<TConfig>.MAX_LANES; i++)
                {
                    laneMappingBuffer[i] = i;
                }

                Span<long> laneEndTrackers  = stackalloc long[IGuitarConfig<TConfig>.MAX_LANES];
                laneEndTrackers.Clear();
                for (long noteIndex = 0, sustainIndex = 0; noteIndex < track.NoteGroups.Count; noteIndex++)
                {

                    ref var noteGroup = ref track.NoteGroups[noteIndex];

                }
            }

            track.Sustains.TrimExcess();
            track.Overdrives.TrimExcess();
            track.Solos.TrimExcess();
            return track;
        }

        private static unsafe SustainLeniency CheckSustainLeniency<TConfig>(
            in DualTime currNote,
            in DualTime nextNote,
            DualTime* frets
        )
            where TConfig : unmanaged, IGuitarConfig<TConfig>
        {
            var leniency = SustainLeniency.Normal;
            long length = 0;
            for (int lane = 0; lane < IGuitarConfig<TConfig>.MAX_LANES; lane++)
            {
                ref readonly var fret = ref frets[lane];
                if (!fret.IsActive() || fret.Ticks == length)
                {
                    continue;
                }

                if (length > 0)
                {
                    leniency = SustainLeniency.Disjoint;
                    break;
                }

                if (currNote + fret > nextNote)
                {
                    leniency = SustainLeniency.Extended;
                }
                length = fret.Ticks;
            }
            return leniency;
        }
    }
}
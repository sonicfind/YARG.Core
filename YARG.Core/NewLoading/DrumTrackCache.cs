using System;
using System.Diagnostics;
using YARG.Core.Containers;
using YARG.Core.Game;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    [Flags]
    public enum DrumNoteMask
    {
        Kick = 1 << 0,
        Snare = 1 << 1,
        Tom_1 = 1 << 2,
        Tom_2 = 1 << 3,
        Tom_3 = 1 << 4,
        Cymbal_1 = 1 << 5,
        Cymbal_2 = 1 << 6,
        Cymbal_3 = 1 << 7,
    }

    [Flags]
    public enum DrumDynamicsMask
    {
        Accent_Kick     = 1 <<  0,
        Accent_Snare    = 1 <<  1,
        Accent_Tom_1    = 1 <<  2,
        Accent_Tom_2    = 1 <<  3,
        Accent_Tom_3    = 1 <<  4,
        Accent_Cymbal_1 = 1 <<  5,
        Accent_Cymbal_2 = 1 <<  6,
        Accent_Cymbal_3 = 1 <<  7,
        Ghost_Kick      = 1 <<  8,
        Ghost_Snare     = 1 <<  9,
        Ghost_Tom_1     = 1 << 10,
        Ghost_Tom_2     = 1 << 11,
        Ghost_Tom_3     = 1 << 12,
        Ghost_Cymbal_1  = 1 << 13,
        Ghost_Cymbal_2  = 1 << 14,
        Ghost_Cymbal_3  = 1 << 15,
    }

    public struct DrumNoteGroup
    {
        public DrumNoteMask     NoteMask;
        public DrumDynamicsMask DynamicsMask;
        public long             RollIndex;
        public long             OverdriveIndex;
        public long             SoloIndex;
    }

    public class DrumTrackCache : IDisposable
    {
        public YargNativeSortedList<DualTime, DrumNoteGroup>  NoteGroups { get; }
        public YargNativeList<Sustain>                        Rolls      { get; }
        public YargNativeSortedList<DualTime, HittablePhrase> Overdrives { get; }
        public YargNativeSortedList<DualTime, HittablePhrase> Solos      { get; }

        public DrumTrackCache(DrumTrackCache source)
        {
            NoteGroups = new YargNativeSortedList<DualTime, DrumNoteGroup> (source.NoteGroups);
            Overdrives = new YargNativeSortedList<DualTime, HittablePhrase>(source.Overdrives);
            Solos      = new YargNativeSortedList<DualTime, HittablePhrase>(source.Solos);
            Rolls      = new YargNativeList<Sustain>(source.Rolls);
        }

        private DrumTrackCache()
        {
            NoteGroups = new YargNativeSortedList<DualTime, DrumNoteGroup>();
            Overdrives = new YargNativeSortedList<DualTime, HittablePhrase>();
            Solos      = new YargNativeSortedList<DualTime, HittablePhrase>();
            Rolls      = new YargNativeList<Sustain>();
        }

        public void Dispose()
        {
            NoteGroups.Dispose();
            Rolls.Dispose();
            Overdrives.Dispose();
            Solos.Dispose();
        }

        private const int KICK         = 0;
        private const int SNARE        = 1;
        private const int TOM_1        = 2;
        private const int TOM_2        = 3;
        private const int TOM_3        = 4;
        private const int CYMBAL_1     = 5;
        private const int CYMBAL_2     = 6;
        private const int CYMBAL_3     = 7;
        private const int CYMBAL_SHIFT = CYMBAL_1 - TOM_1;
        private const int DYNAMICS_SHIFT = 8;

        public static unsafe DrumTrackCache Create(
            YARGChart chart,
            InstrumentTrack2<FourLaneDrums> instrument,
            in DualTime chartEndTime,
            in InstrumentSelection selection
        )
        {
            var track = instrument[selection.Difficulty];
            var cache = new DrumTrackCache();
            cache.NoteGroups.Capacity = track.Notes.Count;
            cache.Rolls.Capacity      = track.Notes.Count;
            cache.Overdrives.Capacity = track.Overdrives.Count;
            cache.Solos.Capacity      = track.Solos.Count;

            long overdriveIndex = 0;
            long soloIndex = 0;

            // We have to validate note sustain data to ensure that we can run engine logic without issue.
            var landEndings = stackalloc long[FourLaneDrums.NUM_LANES];
            for (long noteIndex = 0; noteIndex < track.Notes.Count; noteIndex++)
            {
                var note = track.Notes.Data + noteIndex;
                // This ensures that the result screen doesn't add notes that can never be played
                if (note->Key >= chartEndTime)
                {
                    break;
                }

                var group = new DrumNoteGroup
                {
                    RollIndex = -1,
                    OverdriveIndex = CommonTrackCacheOps.GetPhraseIndex(track.Overdrives, cache.Overdrives, in note->Key, ref overdriveIndex),
                    SoloIndex = CommonTrackCacheOps.GetPhraseIndex(track.Solos, cache.Solos, in note->Key, ref soloIndex),
                };

                var lanes = (DualTime*)&note->Value.Lanes;
                for (int lane = 0; lane < FourLaneDrums.NUM_LANES; lane++)
                {
                    var fret = lanes[lane];
                    if (!fret.IsActive() ||
                       (selection.Difficulty != Difficulty.ExpertPlus && lane == KICK && note->Value.KickState == KickState.PlusOnly))
                    {
                        continue;
                    }

                    int shift = lane;
                    if (lane >= TOM_1)
                    {
                        if (selection.Instrument != Instrument.FiveLaneDrums)
                        {
                            if (((bool*) &note->Value.Cymbals)[lane - TOM_1])
                            {
                                shift += CYMBAL_SHIFT;
                            }
                        }
                        else switch (lane)
                        {
                            case TOM_1:
                            {
                                if (note->Value.Cymbals.Yellow)
                                {
                                    shift = CYMBAL_1;
                                }
                                else if (lanes[TOM_2].IsActive() && !note->Value.Cymbals.Blue)
                                {
                                    shift = SNARE;
                                }
                                break;
                            }
                            case TOM_2:
                            {
                                if (!note->Value.Cymbals.Blue)
                                {
                                    shift = TOM_1;
                                }
                                else if (!lanes[TOM_3].IsActive() || !note->Value.Cymbals.Green)
                                {
                                    shift = CYMBAL_2;
                                }
                                else
                                {
                                    shift = CYMBAL_1;
                                }
                                break;
                            }
                            case TOM_3:
                            {
                                shift = note->Value.Cymbals.Green ? CYMBAL_2 : TOM_2;
                                break;
                            }
                        }
                    }

                    int laneMask = 1 << shift;
                    var sustain = DualTime.Truncate(fret, chart.Settings.SustainCutoffThreshold);
                    // Anything greater than 1 signals that the sustain length satisfied the threshold.
                    // However, you can't do a drum roll on two drums... in-game anyway.
                    if (sustain.Ticks > 1 && group.RollIndex == -1)
                    {
                        group.RollIndex = cache.Rolls.Count;
                        cache.Rolls.Add(new Sustain(sustain + note->Key) { NoteMask = laneMask });
                    }
                    group.NoteMask |= (DrumNoteMask)laneMask;

                    if (lane >= SNARE)
                    {
                        switch (((DrumDynamics*)&note->Value.Dynamics)[lane - SNARE])
                        {
                            case DrumDynamics.Accent: group.DynamicsMask |= (DrumDynamicsMask)laneMask; break;
                            case DrumDynamics.Ghost:  group.DynamicsMask |= (DrumDynamicsMask)(laneMask << DYNAMICS_SHIFT); break;
                        }
                    }
                }
                cache.NoteGroups.Add(note->Key, in group);
            }

            cache.Rolls.TrimExcess();
            cache.Overdrives.TrimExcess();
            cache.Solos.TrimExcess();
            return cache;
        }

        public static unsafe DrumTrackCache Create(
            YARGChart chart,
            InstrumentTrack2<FiveLaneDrums> instrument,
            in DualTime chartEndTime,
            in InstrumentSelection selection
        )
        {
            var track = instrument[selection.Difficulty];
            var cache = new DrumTrackCache();
            cache.NoteGroups.Capacity = track.Notes.Count;
            cache.Rolls.Capacity      = track.Notes.Count;
            cache.Overdrives.Capacity = track.Overdrives.Count;
            cache.Solos.Capacity      = track.Solos.Count;

            long overdriveIndex = 0;
            long soloIndex = 0;

            // We have to validate note sustain data to ensure that we can run engine logic without issue.
            var landEndings = stackalloc long[FourLaneDrums.NUM_LANES];
            for (long noteIndex = 0; noteIndex < track.Notes.Count; noteIndex++)
            {
                var note = track.Notes.Data + noteIndex;
                // This ensures that the result screen doesn't add notes that can never be played
                if (note->Key >= chartEndTime)
                {
                    break;
                }

                var group = new DrumNoteGroup
                {
                    RollIndex = -1,
                    OverdriveIndex = CommonTrackCacheOps.GetPhraseIndex(track.Overdrives, cache.Overdrives, in note->Key, ref overdriveIndex),
                    SoloIndex = CommonTrackCacheOps.GetPhraseIndex(track.Solos, cache.Solos, in note->Key, ref soloIndex),
                };

                var lanes = (DualTime*)&note->Value.Lanes;
                for (int lane = 0; lane < FourLaneDrums.NUM_LANES; lane++)
                {
                    var fret = lanes[lane];
                    if (!fret.IsActive() ||
                       (selection.Difficulty != Difficulty.ExpertPlus && lane == KICK && note->Value.KickState == KickState.PlusOnly))
                    {
                        continue;
                    }

                    int shift;
                    if (selection.Instrument == Instrument.FiveLaneDrums)
                    {
                        shift = lane switch
                        {
                            TOM_1    => CYMBAL_1,
                            TOM_2    => TOM_1,
                            TOM_3    => CYMBAL_2,
                            CYMBAL_1 => TOM_2,
                            _        => lane
                        };
                    }
                    else
                    {
                        shift = lane switch
                        {
                            TOM_1    => CYMBAL_1,
                            TOM_3    => CYMBAL_3,
                            CYMBAL_1 => !lanes[TOM_3].IsActive() ? TOM_3 : TOM_2,
                            _        => lane
                        };
                    }

                    int laneMask = 1 << shift;
                    var sustain = DualTime.Truncate(fret, chart.Settings.SustainCutoffThreshold);
                    // Anything greater than 1 signals that the sustain length satisfied the threshold.
                    // However, you can't do a drum roll on two drums... in-game anyway.
                    if (sustain.Ticks > 1 && group.RollIndex == -1)
                    {
                        group.RollIndex = cache.Rolls.Count;
                        cache.Rolls.Add(new Sustain(sustain + note->Key) { NoteMask = laneMask });
                    }
                    group.NoteMask |= (DrumNoteMask)laneMask;

                    if (lane >= SNARE)
                    {
                        switch (((DrumDynamics*)&note->Value.Dynamics)[lane - SNARE])
                        {
                            case DrumDynamics.Accent: group.DynamicsMask |= (DrumDynamicsMask)laneMask; break;
                            case DrumDynamics.Ghost:  group.DynamicsMask |= (DrumDynamicsMask)(laneMask << DYNAMICS_SHIFT); break;
                        }
                    }
                }
                cache.NoteGroups.Add(note->Key, in group);
            }

            cache.Rolls.TrimExcess();
            cache.Overdrives.TrimExcess();
            cache.Solos.TrimExcess();
            return cache;
        }
    }
}
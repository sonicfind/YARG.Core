using System;
using System.Diagnostics;
using YARG.Core.Containers;
using YARG.Core.Game;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public class DrumTrackCache : IDisposable
    {
        public YargNativeList<DrumNoteGroup>   NoteGroups { get; }
        public YargNativeList<DrumRoll>        Rolls      { get; }
        public YargNativeList<OverdrivePhrase> Overdrives { get; }
        public YargNativeList<SoloPhrase>      Solos      { get; }

        public DrumTrackCache Clone()
        {
            return new DrumTrackCache(this);
        }

        public void Dispose()
        {
            NoteGroups.Dispose();
            Rolls.Dispose();
            Overdrives.Dispose();
            Solos.Dispose();
        }

        private DrumTrackCache(long numNotes, long numOverdrives, long numSolos)
        {
            NoteGroups = new YargNativeList<DrumNoteGroup>
            {
                Capacity = numNotes
            };
            Rolls = new YargNativeList<DrumRoll>
            {
                Capacity = numNotes
            };
            Overdrives = new YargNativeList<OverdrivePhrase>
            {
                Capacity = numOverdrives
            };
            Solos = new YargNativeList<SoloPhrase>
            {
                Capacity = numSolos
            };
        }

        private DrumTrackCache(DrumTrackCache source)
        {
            NoteGroups = new YargNativeList<DrumNoteGroup>  (source.NoteGroups);
            Rolls      = new YargNativeList<DrumRoll>       (source.Rolls);
            Overdrives = new YargNativeList<OverdrivePhrase>(source.Overdrives);
            Solos      = new YargNativeList<SoloPhrase>     (source.Solos);
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
            var cache = new DrumTrackCache(track.Notes.Count, track.Overdrives.Count, track.Solos.Count);

            for (int i = 0; i < track.Notes.Count; i++)
            {
                cache.Overdrives.Add(new OverdrivePhrase());
            }

            foreach (var solo in track.Solos)
            {
                cache.Solos.Add(new SoloPhrase(solo.Key, solo.Key + solo.Value));
            }

            long overdriveIndex = 0;
            long soloIndex = 0;

            for (long noteIndex = 0; noteIndex < track.Notes.Count; noteIndex++)
            {
                var note = track.Notes.Data + noteIndex;
                // This ensures that the result screen doesn't add notes that can never be played
                if (note->Key >= chartEndTime)
                {
                    break;
                }

                var group = new DrumNoteGroup(
                    in note->Key,
                    CommonTrackCacheOps.GetOverdrivePhraseIndex(track.Overdrives, cache.Overdrives, in note->Key, ref overdriveIndex),
                    CommonTrackCacheOps.GetSoloPhraseIndex(cache.Solos, in note->Key, ref soloIndex)
                );

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
                    var length = DualTime.Truncate(fret, chart.Settings.SustainCutoffThreshold);
                    // Anything greater than 1 signals that the sustain length satisfied the threshold.
                    if (length.Ticks > 1)
                    {
                        cache.Rolls.Add(new DrumRoll(length + note->Key, laneMask));
                    }
                    group.LaneMask |= (DrumLaneMask)laneMask;

                    if (lane >= SNARE)
                    {
                        switch (((DrumDynamics*)&note->Value.Dynamics)[lane - SNARE])
                        {
                            case DrumDynamics.Accent: group.DynamicsMask |= (DrumDynamicsMask)laneMask; break;
                            case DrumDynamics.Ghost:  group.DynamicsMask |= (DrumDynamicsMask)(laneMask << DYNAMICS_SHIFT); break;
                        }
                    }
                }
                cache.NoteGroups.Add(in group);
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
            var cache = new DrumTrackCache(track.Notes.Count, track.Overdrives.Count, track.Solos.Count);

            for (int i = 0; i < track.Notes.Count; i++)
            {
                cache.Overdrives.Add(new OverdrivePhrase());
            }

            foreach (var solo in track.Solos)
            {
                cache.Solos.Add(new SoloPhrase(solo.Key, solo.Key + solo.Value));
            }

            long overdriveIndex = 0;
            long soloIndex = 0;

            for (long noteIndex = 0; noteIndex < track.Notes.Count; noteIndex++)
            {
                var note = track.Notes.Data + noteIndex;
                // This ensures that the result screen doesn't add notes that can never be played
                if (note->Key >= chartEndTime)
                {
                    break;
                }

                var group = new DrumNoteGroup(
                    in note->Key,
                    CommonTrackCacheOps.GetOverdrivePhraseIndex(track.Overdrives, cache.Overdrives, in note->Key, ref overdriveIndex),
                    CommonTrackCacheOps.GetSoloPhraseIndex(cache.Solos, in note->Key, ref soloIndex)
                );

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
                    var length = DualTime.Truncate(fret, chart.Settings.SustainCutoffThreshold);
                    // Anything greater than 1 signals that the sustain length satisfied the threshold.
                    if (length.Ticks > 1)
                    {
                        cache.Rolls.Add(new DrumRoll(length + note->Key, laneMask));
                    }
                    group.LaneMask |= (DrumLaneMask)laneMask;

                    if (lane >= SNARE)
                    {
                        switch (((DrumDynamics*)&note->Value.Dynamics)[lane - SNARE])
                        {
                            case DrumDynamics.Accent: group.DynamicsMask |= (DrumDynamicsMask)laneMask; break;
                            case DrumDynamics.Ghost:  group.DynamicsMask |= (DrumDynamicsMask)(laneMask << DYNAMICS_SHIFT); break;
                        }
                    }
                }
                cache.NoteGroups.Add(in group);
            }

            cache.Rolls.TrimExcess();
            cache.Overdrives.TrimExcess();
            cache.Solos.TrimExcess();
            return cache;
        }
    }
}
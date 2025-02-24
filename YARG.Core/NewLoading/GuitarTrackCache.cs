using System;
using YARG.Core.Containers;
using YARG.Core.Game;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public class GuitarTrackCache : IDisposable
    {
        public struct Sustain
        {
            public readonly DualTime EndTime;
            public          uint     LaneMask;

            public Sustain(in DualTime endTime)
            {
                EndTime = endTime;
                LaneMask = 0;
            }
        }

        public struct GuitarNoteGroup
        {
            public uint        LaneMask;
            public GuitarState State;
            public long        SustainIndex;
            public long        SustainCount;
            public long        OverdriveIndex;
            public long        SoloIndex;
        }

        public YargNativeSortedList<DualTime, GuitarNoteGroup> NoteGroups { get; }
        public YargNativeSortedList<DualTime, HittablePhrase>  Overdrives { get; }
        public YargNativeSortedList<DualTime, HittablePhrase>  Solos { get; }
        public YargNativeList<Sustain> Sustains { get; }

        public GuitarTrackCache(GuitarTrackCache source)
        {
            NoteGroups = new YargNativeSortedList<DualTime, GuitarNoteGroup>(source.NoteGroups);
            Overdrives = new YargNativeSortedList<DualTime, HittablePhrase>(source.Overdrives);
            Solos = new YargNativeSortedList<DualTime, HittablePhrase>(source.Solos);
            Sustains = new YargNativeList<Sustain>(source.Sustains);
        }

        private GuitarTrackCache()
        {
            NoteGroups = new YargNativeSortedList<DualTime, GuitarNoteGroup>();
            Overdrives = new YargNativeSortedList<DualTime, HittablePhrase>();
            Solos = new YargNativeSortedList<DualTime, HittablePhrase>();
            Sustains = new YargNativeList<Sustain>();
        }

        public void Dispose()
        {
            NoteGroups.Dispose();
            Sustains.Dispose();
            Overdrives.Dispose();
            Solos.Dispose();
        }

        public static GuitarTrackCache Create(YARGChart chart, InstrumentTrack2<GuitarNote<FiveFret>> instrument, in DualTime chartEndTime, in InstrumentSelection selection)
        {
            var track = instrument[selection.Difficulty];
            var cache = new GuitarTrackCache();
            cache.NoteGroups.Capacity = track.Notes.Count;
            cache.Sustains.Capacity   = track.Notes.Count;
            cache.Overdrives.Capacity = track.Overdrives.Count;
            cache.Solos.Capacity      = track.Solos.Count;

            const uint NUM_LANES = 6;
            const uint OPEN_NOTE = 0;
            bool useLeftyFlip = selection.Modifiers.Has(Modifier.LeftyFlip);
            long overdriveIndex = 0;
            long soloIndex = 0;
            for (long i = 0; i < track.Notes.Count; i++)
            {
                unsafe
                {
                    var note = track.Notes.Data + i;
                    var group = new GuitarNoteGroup
                    {
                        SustainIndex = cache.Sustains.Count,
                        OverdriveIndex = -1,
                        SoloIndex = -1
                    };

                    while (overdriveIndex < track.Overdrives.Count)
                    {
                        ref var overdrive = ref track.Overdrives[overdriveIndex];
                        var phraseEndTime = overdrive.Key + overdrive.Value;
                        if (note->Key < phraseEndTime)
                        {
                            if (note->Key >= overdrive.Key)
                            {
                                group.OverdriveIndex = overdriveIndex;
                                if (cache.Overdrives.GetLastOrAdd(overdrive.Key, out var phrase))
                                {
                                    phrase->EndTime = phraseEndTime;
                                }
                                phrase->TotalNotes++;
                            }
                            break;
                        }
                        overdriveIndex++;
                    }

                    while (soloIndex < track.Overdrives.Count)
                    {
                        ref var solo = ref track.Solos[soloIndex];
                        var phraseEndTime = solo.Key + solo.Value;
                        if (note->Key < solo.Key + solo.Value)
                        {
                            if (note->Key >= solo.Key)
                            {
                                group.SoloIndex = soloIndex;
                                if (cache.Overdrives.GetLastOrAdd(solo.Key, out var phrase))
                                {
                                    phrase->EndTime = phraseEndTime;
                                }
                                phrase->TotalNotes++;
                            }
                            break;
                        }
                        soloIndex++;
                    }

                    for (uint index = 0; index < NUM_LANES; index++)
                    {
                        var fret = (&note->Value.Lanes.Open)[index];
                        if (!fret.IsActive())
                        {
                            continue;
                        }

                        uint lane = !useLeftyFlip || index == OPEN_NOTE ? index : NUM_LANES - index;
                        var sustain = DualTime.Truncate(fret, chart.Settings.SustainCutoffThreshold);
                        if (sustain.Ticks > 1)
                        {
                            var laneEndTime = sustain + note->Key;
                            long location = group.SustainIndex;
                            while (location < cache.Sustains.Count && cache.Sustains[location].EndTime != laneEndTime)
                            {
                                ++location;
                            }

                            if (location == cache.Sustains.Count)
                            {
                                cache.Sustains.Add(new Sustain(laneEndTime));
                                group.SustainCount++;
                            }
                            cache.Sustains[location].LaneMask |= lane;
                        }
                        group.LaneMask |= lane;
                    }

                    group.State = ParseGuitarState(
                        selection.Modifiers,
                        note->Value.State,
                        group.LaneMask,
                        cache.NoteGroups,
                        note->Key.Ticks,
                        chart.Settings.HopoThreshold
                    );

                    cache.NoteGroups.Add(note->Key, in group);
                }
            }

            cache.Sustains.TrimExcess();
            cache.Overdrives.TrimExcess();
            cache.Solos.TrimExcess();
            return cache;
        }

        private static GuitarState ParseGuitarState(
            Modifier modifiers,
            GuitarState state,
            uint laneMask,
            YargNativeSortedList<DualTime, GuitarNoteGroup> groups,
            long tickPosition,
            long hopoThreshold
        )
        {
            if (modifiers.Has(Modifier.AllStrums))
            {
                return GuitarState.Strum;
            }

            if (modifiers.Has(Modifier.AllTaps))
            {
                return GuitarState.Tap;
            }

            if (modifiers.Has(Modifier.AllHopos))
            {
                return GuitarState.Hopo;
            }

            if (state == GuitarState.Tap)
            {
                return !modifiers.Has(Modifier.TapsToHopos) ? GuitarState.Tap : GuitarState.Hopo;
            }

            if (state == GuitarState.Natural || state == GuitarState.Forced)
            {
                var naturalState = GuitarState.Strum;
                if (!IsChord(laneMask) && groups.Count > 0)
                {
                    ref readonly var previous = ref groups[groups.Count - 1];
                    if ((previous.Value.LaneMask & laneMask) == 0 && tickPosition - previous.Key.Ticks <= hopoThreshold)
                    {
                        naturalState = GuitarState.Hopo;
                    }
                }

                // Nat    + Strum = Strum
                // Nat    + Hopo  = Hopo
                // Forced + Strum = Hopo
                // Forced + Hopo  = Strum
                // Think of it like xor
                state = (state == GuitarState.Natural) == (naturalState == GuitarState.Strum)
                    ? GuitarState.Strum : GuitarState.Hopo;
            }

            if (state == GuitarState.Hopo)
            {
                return !modifiers.Has(Modifier.HoposToTaps) ? GuitarState.Hopo : GuitarState.Tap;
            }
            return state;
        }

        private static bool IsChord(uint laneMask)
        {
            while (laneMask > 0)
            {
                if ((laneMask & 1) > 0)
                {
                    return laneMask > 1;
                }
                laneMask >>= 1;
            }
            return false;
        }
    }
}
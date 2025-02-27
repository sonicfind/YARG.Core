using System;
using System.Diagnostics;
using YARG.Core.Containers;
using YARG.Core.Game;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    [Flags]
    public enum GuitarNoteMask
    {
        Open_DisableAnchoring = 1 << 0,
        Green                 = 1 << 1,
        Red                   = 1 << 2,
        Yellow                = 1 << 3,
        Blue                  = 1 << 4,
        Orange                = 1 << 5,
    }

    public struct GuitarNoteGroup
    {
        public GuitarNoteMask NoteMask;
        public GuitarState    State;
        public long           SustainIndex;
        public long           SustainCount;
        public long           OverdriveIndex;
        public long           SoloIndex;
    }

    public class GuitarTrackCache : IDisposable
    {
        public YargNativeSortedList<DualTime, GuitarNoteGroup> NoteGroups { get; }
        public YargNativeList<Sustain>                         Sustains   { get; }
        public YargNativeSortedList<DualTime, HittablePhrase>  Overdrives { get; }
        public YargNativeSortedList<DualTime, HittablePhrase>  Solos      { get; }

        public GuitarTrackCache(GuitarTrackCache source)
        {
            NoteGroups = new YargNativeSortedList<DualTime, GuitarNoteGroup>(source.NoteGroups);
            Overdrives = new YargNativeSortedList<DualTime, HittablePhrase>(source.Overdrives);
            Solos      = new YargNativeSortedList<DualTime, HittablePhrase>(source.Solos);
            Sustains   = new YargNativeList<Sustain>(source.Sustains);
        }

        private GuitarTrackCache()
        {
            NoteGroups = new YargNativeSortedList<DualTime, GuitarNoteGroup>();
            Overdrives = new YargNativeSortedList<DualTime, HittablePhrase>();
            Solos      = new YargNativeSortedList<DualTime, HittablePhrase>();
            Sustains   = new YargNativeList<Sustain>();
        }

        public void Dispose()
        {
            NoteGroups.Dispose();
            Sustains.Dispose();
            Overdrives.Dispose();
            Solos.Dispose();
        }

        public static unsafe GuitarTrackCache Create<TConfig>(
            YARGChart chart,
            InstrumentTrack2<GuitarNote<TConfig>> instrument,
            in DualTime chartEndTime,
            in InstrumentSelection selection
        )
            where TConfig : unmanaged, IGuitarConfig<TConfig>
        {
            var track = instrument[selection.Difficulty];
            var cache = new GuitarTrackCache();
            cache.NoteGroups.Capacity = track.Notes.Count;
            cache.Sustains.Capacity   = track.Notes.Count;
            cache.Overdrives.Capacity = track.Overdrives.Count;
            cache.Solos.Capacity      = track.Solos.Count;

            long overdriveIndex = 0;
            long soloIndex = 0;

            // We have to validate note sustain data to ensure that we can run engine logic without issue.
            var landEndings = stackalloc long[IGuitarConfig<TConfig>.MAX_LANES];
            for (long noteIndex = 0; noteIndex < track.Notes.Count; noteIndex++)
            {
                var note = track.Notes.Data + noteIndex;
                // This ensures that the result screen doesn't add notes that can never be played
                if (note->Key >= chartEndTime)
                {
                    break;
                }

                var group = new GuitarNoteGroup
                {
                    SustainIndex = cache.Sustains.Count,
                    OverdriveIndex = CommonTrackCacheOps.GetPhraseIndex(track.Overdrives, cache.Overdrives, in note->Key, ref overdriveIndex),
                    SoloIndex = CommonTrackCacheOps.GetPhraseIndex(track.Solos, cache.Solos, in note->Key, ref soloIndex),
                };

                var frets = (DualTime*)&note->Value.Lanes;
                for (int lane = 0; lane < IGuitarConfig<TConfig>.MAX_LANES; lane++)
                {
                    var fret = frets[lane];
                    // We have to account for bad charting where the current note has an active lane
                    // even though the prior note sustains through it with the same lane
                    if (!fret.IsActive() || note->Key.Ticks < landEndings[lane])
                    {
                        continue;
                    }

                    int laneMask = 1 << lane;
                    var sustain = DualTime.Truncate(fret, chart.Settings.SustainCutoffThreshold);
                    // Anything greater than 1 signals that the sustain length satisfied the threshold
                    if (sustain.Ticks > 1)
                    {
                        long location = group.SustainIndex;
                        var laneEndTime = sustain + note->Key;
                        // To keep parity with other games that support disjointed sustains, we need to group together
                        // lanes that extend to the same end time point. In doing so, if/when a player drops one of the
                        // frets in the group, both notes will be dropped together (See Clone Hero for an example).
                        while (location < cache.Sustains.Count && cache.Sustains[location].EndTime != laneEndTime)
                        {
                            ++location;
                        }

                        if (location == cache.Sustains.Count)
                        {
                            cache.Sustains.Add(new Sustain(laneEndTime));
                        }
                        cache.Sustains[location].NoteMask |= laneMask;
                    }
                    group.NoteMask |= (GuitarNoteMask)laneMask;
                }

                group.SustainCount = cache.Sustains.Count - group.SustainIndex;
                group.State = ParseGuitarState(
                    selection.Modifiers,
                    note->Value.State,
                    group.NoteMask,
                    cache.NoteGroups,
                    note->Key.Ticks,
                    chart.Settings.HopoThreshold
                );

                cache.NoteGroups.Add(note->Key, in group);
            }

            cache.Sustains.TrimExcess();
            cache.Overdrives.TrimExcess();
            cache.Solos.TrimExcess();
            return cache;
        }

        private static GuitarState ParseGuitarState(
            Modifier modifiers,
            GuitarState state,
            GuitarNoteMask noteMask,
            YargNativeSortedList<DualTime, GuitarNoteGroup> groups,
            long tickPosition,
            long hopoThreshold
        )
        {
            // First three are pretty straightforward
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
                // Going by YARG, the presence of this flag turns *all* taps to hopos, regardless
                // of other factors (like notes before it)
                return !modifiers.Has(Modifier.TapsToHopos) ? GuitarState.Tap : GuitarState.Hopo;
            }

            if (state is GuitarState.Natural or GuitarState.Forced)
            {
                var naturalState = GuitarState.Strum;
                if (!IsChord((uint)noteMask) && groups.Count > 0)
                {
                    ref readonly var previous = ref groups[groups.Count - 1];
                    if ((previous.Value.NoteMask & noteMask) == 0 && tickPosition - previous.Key.Ticks <= hopoThreshold)
                    {
                        naturalState = GuitarState.Hopo;
                    }
                }

                // Natural + Strum = Strum
                // Natural + Hopo  = Hopo
                // Forced  + Strum = Hopo
                // Forced  + Hopo  = Strum
                // Think of it like xor, where "Strum" is 0
                state = (state == GuitarState.Natural) == (naturalState == GuitarState.Strum)
                    ? GuitarState.Strum : GuitarState.Hopo;
            }

            if (state == GuitarState.Hopo)
            {
                return !modifiers.Has(Modifier.HoposToTaps) ? GuitarState.Hopo : GuitarState.Tap;
            }
            return state;
        }

        private static bool IsChord(uint noteMask)
        {
            Debug.Assert(noteMask > 0, "At least one lane must be specified");
            // We know that at least one lane is active, so this loop will always terminate.
            // Once we find the first active bit, we simply need to test whether it's the *sole* bit
            while ((noteMask & 1) == 0)
            {
                noteMask >>= 1;
            }
            // Anything more than 1 signals that at least one additional bit exists in the mask
            // meaning more than one lane
            return noteMask > 1;
        }
    }
}
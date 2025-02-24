using System;
using YARG.Core.Containers;
using YARG.Core.Game;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public struct GuitarNoteGroup
    {
        public uint        LaneMask;
        public GuitarState State;
        public long        SustainIndex;
        public long        SustainCount;
        public long        OverdriveIndex;
        public long        SoloIndex;
    }

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

        private const uint OPEN_NOTE = 0;
        public static GuitarTrackCache Create<TConfig>(YARGChart chart, InstrumentTrack2<GuitarNote<TConfig>> instrument, in DualTime chartEndTime, in InstrumentSelection selection)
            where TConfig : unmanaged, IGuitarConfig<TConfig>
        {
            var track = instrument[selection.Difficulty];
            var cache = new GuitarTrackCache();
            cache.NoteGroups.Capacity = track.Notes.Count;
            cache.Sustains.Capacity   = track.Notes.Count;
            cache.Overdrives.Capacity = track.Overdrives.Count;
            cache.Solos.Capacity      = track.Solos.Count;

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
                        OverdriveIndex = GetPhraseIndex(track.Overdrives, cache.Overdrives, in note->Key, ref overdriveIndex),
                        SoloIndex = GetPhraseIndex(track.Solos, cache.Solos, in note->Key, ref soloIndex),
                    };

                    var frets = (DualTime*)&note->Value.Lanes;
                    for (uint index = 0; index < IGuitarConfig<TConfig>.MAX_LANES; index++)
                    {
                        var fret = frets[index];
                        if (!fret.IsActive())
                        {
                            continue;
                        }

                        uint lane = !useLeftyFlip || index == OPEN_NOTE ? index : IGuitarConfig<TConfig>.MAX_LANES - index;
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

        private static long GetPhraseIndex(
            YargNativeSortedList<DualTime, DualTime> trackPhrases,
            YargNativeSortedList<DualTime, HittablePhrase> cachePhrases,
            in DualTime position,
            ref long phraseIndex
        )
        {
            while (phraseIndex < trackPhrases.Count)
            {
                ref readonly var overdrive = ref trackPhrases[phraseIndex];
                var phraseEndTime = overdrive.Key + overdrive.Value;
                if (position < phraseEndTime)
                {
                    if (position >= overdrive.Key)
                    {
                        unsafe
                        {
                            if (cachePhrases.GetLastOrAdd(overdrive.Key, out var phrase))
                            {
                                phrase->EndTime = phraseEndTime;
                            }
                            phrase->TotalNotes++;
                        }
                        return phraseIndex;
                    }
                    break;
                }
                phraseIndex++;
            }
            return -1;
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

            if (state is GuitarState.Natural or GuitarState.Forced)
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
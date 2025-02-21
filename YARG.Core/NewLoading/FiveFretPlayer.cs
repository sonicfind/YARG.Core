using System;
using YARG.Core.Containers;
using YARG.Core.Game;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public class FiveFretPlayer : YargPlayer
    {
        /// <summary>
        /// Total number of possible lanes (including open note)
        /// </summary>
        public const int NUM_LANES = 6;
        public const int OPEN_NOTE = 0;
        public const int GREEN_NOTE = 1;
        public const int RED_NOTE = 2;
        public const int YELLOW_NOTE = 3;
        public const int BLUE_NOTE = 4;
        public const int ORANGE_NOTE = 5;

        public struct GuitarNoteGroup
        {
            public GuitarState State;
            public long        NoteIndex;
            public long        NoteCount;
        }

        private readonly YargNativeList<BasicNote>                       _notes;
        private readonly YargNativeSortedList<DualTime, GuitarNoteGroup> _noteGroups;

        private FiveFretPlayer(YargNativeList<BasicNote> notes, YargNativeSortedList<DualTime, GuitarNoteGroup> noteGroups)
        {
            _notes = notes;
            _noteGroups = noteGroups;
        }

        public override void Dispose()
        {
            _notes.Dispose();
            _noteGroups.Dispose();
        }

        public static FiveFretPlayer Create(YARGChart chart, InstrumentTrack2<GuitarNote<FiveFret>> instrument, in DualTime chartEndTime, in InstrumentSelection selection)
        {
            var track = instrument[selection.Difficulty];
            var notes = new YargNativeList<BasicNote>()
            {
                // Potentially dealing with chords (usually with two notes each).
                // A factor of two should suffice for the majority of songs.
                Capacity = track.Notes.Count * 2,
            };

            var groups = new YargNativeSortedList<DualTime, GuitarNoteGroup>()
            {
                Capacity = track.Notes.Count,
            };

            bool useLeftyFlip = selection.Modifiers.Has(Modifier.LeftyFlip);
            for (int i = 0; i < track.Notes.Count; i++)
            {
                unsafe
                {
                    var note = track.Notes.Data + i;
                    var group = groups.Add(note->Key);
                    group->NoteIndex = notes.Count;

                    for (int lane = 0; lane < NUM_LANES; lane++)
                    {
                        var fret = (&note->Value.Lanes.Open)[lane];
                        if (!fret.IsActive())
                        {
                            continue;
                        }

                        int index = !useLeftyFlip || i == OPEN_NOTE ? i : NUM_LANES - i;
                        var laneEndTime = DualTime.Truncate(fret, chart.Settings.SustainCutoffThreshold) + note->Key;

                        long location = group->NoteIndex;
                        while (location < notes.Count && notes[location].EndTime != laneEndTime)
                        {
                            ++location;
                        }

                        if (location == notes.Count)
                        {
                            notes.Add(new BasicNote(laneEndTime));
                            group->NoteCount++;
                        }
                        notes[location].AddLane(index);
                    }
                    group->State = note->Value.State;
                }
            }
            return new FiveFretPlayer(notes, groups);
        }
    }
}
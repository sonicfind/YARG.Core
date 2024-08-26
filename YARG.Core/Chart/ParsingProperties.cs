using System;
using YARG.Core.IO.Ini;

namespace YARG.Core.Chart
{
    /// <summary>
    /// The type of drums contained in the chart.
    /// </summary>
    public enum DrumsType
    {
        FourLane,
        ProDrums,
        FiveLane,
        Unknown,
        Unknown_Four,
        UnknownPro,
    }

    /// <summary>
    /// Settings used when parsing charts.
    /// </summary>
    public struct ParseSettings
    {
        /// <summary>
        /// The default settings to use for parsing.
        /// </summary>
        public static readonly ParseSettings Default = new()
        {
            DrumsType = DrumsType.Unknown,

            HopoThreshold = SETTING_DEFAULT,
            HopoFreq_FoF = SETTING_DEFAULT,
            EighthNoteHopo = false,
            ChordHopoCancellation = false,

            SustainCutoffThreshold = SETTING_DEFAULT,
            NoteSnapThreshold = 0,

            StarPowerNote = SETTING_DEFAULT,
        };

        /// <summary>
        /// The value used to indicate a setting should be overwritten with the
        /// appropriate default value for the chart being parsed.
        /// </summary>
        public const int SETTING_DEFAULT = -1;

        /// <summary>
        /// The drums mode to parse the drums track as.
        /// </summary>
        public DrumsType DrumsType;

        /// <summary>
        /// The tick distance between notes to use as the HOPO threshold.
        /// </summary>
        /// <remarks>
        /// Uses the <c>hopo_threshold</c> tag from song.ini files.<br/>
        /// Defaults to a 1/12th note.
        /// </remarks>
        public long HopoThreshold;

        /// <summary>
        /// The FoF HOPO threshold setting number to use.
        /// </summary>
        /// <remarks>
        /// Uses the <c>hopofreq</c> tag from song.ini files.<br/>
        /// 0 -> 1/24th note, 1 -> 1/16th note, 2 -> 1/12th note, 3 -> 1/8th note, 4 -> 1/6th note, 5 -> 1/4th note.
        /// </remarks>
        public int HopoFreq_FoF;

        /// <summary>
        /// Set the HOPO threshold to a 1/8th note instead of a 1/12th note.
        /// </summary>
        /// <remarks>
        /// Uses the <c>eighthnote_hopo</c> tag from song.ini files.
        /// </remarks>
        public bool EighthNoteHopo;

        /// <summary>
        /// Skip marking single notes after chords as HOPOs
        /// if the single note shares a fret with the chord.
        /// </summary>
        public bool ChordHopoCancellation;

        /// <summary>
        /// The tick threshold to use for sustain cutoffs.
        /// </summary>
        /// <remarks>
        /// Uses the <c>sustain_cutoff_threshold</c> tag from song.ini files.<br/>
        /// Defaults to a 1/12th note in .mid, and 0 in .chart.
        /// </remarks>
        public long SustainCutoffThreshold;

        /// <summary>
        /// The tick threshold to use for snapping together single notes into chords.
        /// </summary>
        /// <remarks>
        /// Defaults to 10 in CON files, and 0 in other charts.
        /// </remarks>
        public long NoteSnapThreshold;

        /// <summary>
        /// The MIDI note to use for Star Power phrases in .mid charts.
        /// </summary>
        /// <remarks>
        /// Uses the <c>multiplier_note</c> and <c>star_power_note</c> tags from song.ini files.<br/>
        /// Defaults to 116.
        /// </remarks>
        public int StarPowerNote;

        public ParseSettings(IniSection modifiers, DrumsType drums)
        {
            if (!modifiers.TryGet("hopo_frequency", out HopoThreshold))
            {
                HopoThreshold = SETTING_DEFAULT;
            }

            if (!modifiers.TryGet("hopofreq", out HopoFreq_FoF))
            {
                HopoFreq_FoF = SETTING_DEFAULT;
            }

            modifiers.TryGet("eighthnote_hopo", out EighthNoteHopo);

            if (!modifiers.TryGet("sustain_cutoff_threshold", out SustainCutoffThreshold))
            {
                SustainCutoffThreshold = SETTING_DEFAULT;
            }

            if (!modifiers.TryGet("multiplier_note", out StarPowerNote))
            {
                StarPowerNote = SETTING_DEFAULT;
            }
            DrumsType = drums;
            ChordHopoCancellation = false;
            NoteSnapThreshold = 0;
        }

        /// <summary>
        /// Calculates the HOPO threshold to use from the various HOPO settings.
        /// </summary>
        public readonly float GetHopoThreshold(float resolution)
        {
            // Prefer in this order:
            // 1. hopo_threshold
            // 2. eighthnote_hopo
            // 3. hopofreq

            if (HopoThreshold > 0)
            {
                return HopoThreshold;
            }

            if (EighthNoteHopo)
            {
                return resolution / 2;
            }

            if (HopoFreq_FoF >= 0)
            {
                int denominator = HopoFreq_FoF switch
                {
                    0 => 24,
                    1 => 16,
                    2 => 12,
                    3 => 8,
                    4 => 6,
                    5 => 4,
                    _ => throw new NotImplementedException($"Unhandled hopofreq value {HopoFreq_FoF}!")
                };
                return (resolution * 4) / denominator;
            }
            return resolution / 3;
        }

        public readonly long GetHopoThreshold(long resolution)
        {
            // Prefer in this order:
            // 1. hopo_threshold
            // 2. eighthnote_hopo
            // 3. hopofreq

            if (HopoThreshold > 0)
            {
                return HopoThreshold;
            }

            if (EighthNoteHopo)
            {
                return resolution / 2;
            }

            if (HopoFreq_FoF >= 0)
            {
                int denominator = HopoFreq_FoF switch
                {
                    0 => 12,
                    1 => 8,
                    2 => 6,
                    3 => 4,
                    4 => 3,
                    5 => 2,
                    _ => throw new NotImplementedException($"Unhandled hopofreq value {HopoFreq_FoF}!")
                };
                return (resolution * 2) / denominator;
            }
            return resolution / 3;
        }
    }
}
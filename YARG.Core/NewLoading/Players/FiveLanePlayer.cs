using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using YARG.Core.Game;
using YARG.Core.NewLoading.Guitar;
using YARG.Core.NewLoading.Players;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading.FiveLane
{
    

    public sealed class FiveLanePlayer : InstrumentPlayer<FiveLaneDrums, FiveLanePlayer.Note>
    {
        public struct SubNote
        {
            public readonly int Index;
            public readonly DrumDynamics Dynamics;
            public readonly DualTime EndPosition;
            public HitStatus Status;

            public SubNote(int index, DrumDynamics dynamics, DualTime endPosition)
            {
                Index = index;
                Dynamics = dynamics;
                EndPosition = endPosition;
                Status = HitStatus.Idle;
            }
        }

        public readonly struct Note
        {
            public readonly DualTime StartPosition;
            public readonly SubNote[] Notes;
            public readonly bool Flam;
            public readonly int OverdriveIndex;
            public readonly int SoloIndex;

            public unsafe Note(in DualTime start, SubNote* notes, int laneCount, bool flam, int overdrive, int solo)
            {
                StartPosition = start;
                Notes = new SubNote[laneCount];
                for (int i = 0; i < laneCount; i++)
                {
                    Notes[i] = notes[i];
                }
                Flam = flam;
                OverdriveIndex = overdrive;
                SoloIndex = solo;
            }
        }

        private readonly InstrumentTrack2<DifficultyTrack2<FourLaneDrums>>? _fourlaneTrack;

        public FiveLanePlayer(YARGChart chart, YargProfile profile)
            : base(chart.FiveLaneDrums, chart.Sync, profile)
        {
            _fourlaneTrack = chart.FourLaneDrums;
            _notes = null!;
            _overdrives = null!;
            _soloes = null!;
        }

        public override void Set(in DualTime startTime, in DualTime endTime)
        {
            if (_track != null)
            {
                LoadFiveLane(in startTime, in endTime);
            }
            else
            {
                LoadFourLane(in startTime, in endTime);
            }
        }

        private unsafe void LoadFiveLane(in DualTime startTime, in DualTime endTime)
        {
            const int NUMLANES = 6;
            const int BASS = 0;
            const int SNARE = 1;

            var diff = _track![Profile.CurrentDifficulty];
            Debug.Assert(diff != null, "This function should only be used with a valid difficulty");
            Debug.Assert(diff.Notes.Count > 0, "This function should only be used when notes are present");

            var overdriveRanges = diff.Phrases.Overdrives.Count > 0 ? diff.Phrases.Overdrives : _track.Phrases.Overdrives;
            var soloRanges = diff.Phrases.Soloes.Count > 0 ? diff.Phrases.Soloes : _track.Phrases.Soloes;

            var curr = diff.Notes.Data;
            var end = curr + diff.Notes.Count;

            var notes = new Note[diff.Notes.Count];
            _overdrives = new OverdrivePhrase[overdriveRanges.Count];
            _soloes = new SoloPhrase[soloRanges.Count];

            int numNotes = 0;
            int currOverdrive = 0;
            int overdriveNoteCount = 0;
            int currSolo = 0;
            int soloNoteCount = 0;

            bool disableKick = (Profile.CurrentModifiers & Modifier.NoKicks) > 0;
            bool isExpertPlus = Profile.CurrentDifficulty == Difficulty.ExpertPlus;
            bool isProDrums = Profile.CurrentInstrument == Instrument.ProDrums;

            var buffer = stackalloc SubNote[NUMLANES];
            while (curr < end)
            {
                if (curr->Key < startTime)
                {
                    ++curr;
                    continue;
                }

                while (currOverdrive < overdriveRanges.Count)
                {
                    ref readonly var ovd = ref overdriveRanges.Data[currOverdrive];
                    if (curr->Key < ovd.Key + ovd.Value)
                    {
                        break;
                    }
                    _overdrives[currOverdrive++] = new OverdrivePhrase(ovd.Key, overdriveNoteCount);
                    overdriveNoteCount = 0;
                }

                while (currSolo < soloRanges.Count)
                {
                    ref readonly var solo = ref soloRanges.Data[currSolo];
                    var soloEnd = solo.Key + solo.Value;
                    if (curr->Key < soloEnd)
                    {
                        break;
                    }
                    _soloes[currSolo++] = new SoloPhrase(solo.Key, soloEnd, soloNoteCount);
                    soloNoteCount = 0;
                }

                int laneCount = 0;
                var lanes = (DualTime*) &curr->Value;
                for (int i = 0; i < NUMLANES; ++i)
                {
                    if (lanes[i].IsActive() && (i >= SNARE || (!disableKick && (curr->Value.KickState == KickState.Shared || (isExpertPlus == (curr->Value.KickState == KickState.PlusOnly))))))
                    {
                        int index = !Profile.LeftyFlip || i < SNARE ? i : NUMLANES - i;
                        var dynamics = i >= SNARE ? (&curr->Value.Dynamics_Snare)[i - SNARE] : DrumDynamics.None;
                        buffer[laneCount++] = new SubNote(index, dynamics, lanes[i] + curr->Key);
                    }
                }

                if (laneCount > 0)
                {
                    int ovdIndex = -1;
                    if (currOverdrive < overdriveRanges.Count && curr->Key >= overdriveRanges.Data[currOverdrive].Key)
                    {
                        overdriveNoteCount++;
                        ovdIndex = currOverdrive;
                    }

                    int soloIndex = -1;
                    if (currSolo < soloRanges.Count && curr->Key >= soloRanges.Data[currSolo].Key)
                    {
                        soloNoteCount++;
                        soloIndex = currSolo;
                    }
                    bool flamActive = curr->Value.IsFlammed && (lanes[BASS].IsActive() ? laneCount == 2 : laneCount == 1);
                    notes[numNotes++] = new Note(curr->Key, buffer, laneCount, flamActive, ovdIndex, soloIndex);
                }
                ++curr;
            }

            while (currOverdrive < overdriveRanges.Count)
            {
                ref readonly var ovd = ref overdriveRanges.Data[currOverdrive];
                _overdrives[currOverdrive] = new OverdrivePhrase(ovd.Key, overdriveNoteCount);
                ++currOverdrive;
                overdriveNoteCount = 0;
            }

            while (currSolo < soloRanges.Count)
            {
                ref readonly var solo = ref soloRanges.Data[currSolo];
                _soloes[currSolo] = new SoloPhrase(solo.Key, solo.Key + solo.Value, soloNoteCount);
                ++currSolo;
                soloNoteCount = 0;
            }
            _notes = notes[..numNotes];
        }

        private unsafe void LoadFourLane(in DualTime startTime, in DualTime endTime)
        {
            const int NUMLANES = 6;
            const int FOURLANECOUNT = 5;
            const int BASS = 0;
            const int SNARE = 1;
            const int YELLOW = 2;
            const int BLUE = 3;
            const int ORANGE = 4;
            const int GREEN = 5;

            var diff = _fourlaneTrack![Profile.CurrentDifficulty];
            Debug.Assert(diff != null, "This function should only be used with a valid difficulty");
            Debug.Assert(diff.Notes.Count > 0, "This function should only be used when notes are present");

            var overdriveRanges = diff.Phrases.Overdrives.Count > 0 ? diff.Phrases.Overdrives : _fourlaneTrack.Phrases.Overdrives;
            var soloRanges = diff.Phrases.Soloes.Count > 0 ? diff.Phrases.Soloes : _fourlaneTrack.Phrases.Soloes;

            var curr = diff.Notes.Data;
            var end = curr + diff.Notes.Count;

            var notes = new Note[diff.Notes.Count];
            _overdrives = new OverdrivePhrase[overdriveRanges.Count];
            _soloes = new SoloPhrase[soloRanges.Count];

            int numNotes = 0;
            int currOverdrive = 0;
            int overdriveNoteCount = 0;
            int currSolo = 0;
            int soloNoteCount = 0;

            bool disableKick = (Profile.CurrentModifiers & Modifier.NoKicks) > 0;
            bool isExpertPlus = Profile.CurrentDifficulty == Difficulty.ExpertPlus;
            bool isProDrums = Profile.CurrentInstrument == Instrument.ProDrums;

            var buffer = stackalloc SubNote[NUMLANES];
            while (curr < end)
            {
                if (curr->Key < startTime)
                {
                    ++curr;
                    continue;
                }

                while (currOverdrive < overdriveRanges.Count)
                {
                    ref readonly var ovd = ref overdriveRanges.Data[currOverdrive];
                    if (curr->Key < ovd.Key + ovd.Value)
                    {
                        break;
                    }
                    _overdrives[currOverdrive++] = new OverdrivePhrase(ovd.Key, overdriveNoteCount);
                    overdriveNoteCount = 0;
                }

                while (currSolo < soloRanges.Count)
                {
                    ref readonly var solo = ref soloRanges.Data[currSolo];
                    var soloEnd = solo.Key + solo.Value;
                    if (curr->Key < soloEnd)
                    {
                        break;
                    }
                    _soloes[currSolo++] = new SoloPhrase(solo.Key, soloEnd, soloNoteCount);
                    soloNoteCount = 0;
                }

                int laneCount = 0;
                var lanes = &curr->Value.Kick;
                for (int i = 0; i < FOURLANECOUNT; ++i)
                {
                    if (lanes[i].IsActive() && (i >= SNARE || (!disableKick && (curr->Value.KickState == KickState.Shared || (isExpertPlus == (curr->Value.KickState == KickState.PlusOnly))))))
                    {
                        var dynamics = i >= SNARE ? DrumDynamics.None : (&curr->Value.Dynamics_Snare)[i - SNARE];
                        int index = i;
                        switch (i)
                        {
                            case YELLOW:
                                if (!curr->Value.Cymbal_Yellow)
                                {
                                    index = lanes[BLUE].IsActive() && !curr->Value.Cymbal_Blue ? SNARE : YELLOW;
                                }
                                break;
                            case BLUE:
                                if (curr->Value.Cymbal_Blue)
                                {
                                    // Fivelane orange == fourlane green
                                    index = lanes[ORANGE].IsActive() && curr->Value.Cymbal_Green ? YELLOW : ORANGE;
                                }
                                break;
                            // Fivelane orange == fourlane green
                            case ORANGE:
                                if (!curr->Value.Cymbal_Green)
                                {
                                    index = GREEN;
                                }
                                break;
                        }

                        if (Profile.LeftyFlip && index >= SNARE)
                        {
                            index = NUMLANES - index;
                        }
                        buffer[laneCount++] = new SubNote(index, dynamics, lanes[i] + curr->Key);
                    }
                }

                if (laneCount > 0)
                {
                    int ovdIndex = -1;
                    if (currOverdrive < overdriveRanges.Count && curr->Key >= overdriveRanges.Data[currOverdrive].Key)
                    {
                        overdriveNoteCount++;
                        ovdIndex = currOverdrive;
                    }

                    int soloIndex = -1;
                    if (currSolo < soloRanges.Count && curr->Key >= soloRanges.Data[currSolo].Key)
                    {
                        soloNoteCount++;
                        soloIndex = currSolo;
                    }

                    bool flamActive = curr->Value.IsFlammed && (lanes[BASS].IsActive() ? laneCount == 2 : laneCount == 1);
                    notes[numNotes++] = new Note(curr->Key, buffer, laneCount, flamActive, ovdIndex, soloIndex);
                }
                ++curr;
            }

            while (currOverdrive < overdriveRanges.Count)
            {
                ref readonly var ovd = ref overdriveRanges.Data[currOverdrive];
                _overdrives[currOverdrive] = new OverdrivePhrase(ovd.Key, overdriveNoteCount);
                ++currOverdrive;
                overdriveNoteCount = 0;
            }

            while (currSolo < soloRanges.Count)
            {
                ref readonly var solo = ref soloRanges.Data[currSolo];
                _soloes[currSolo] = new SoloPhrase(solo.Key, solo.Key + solo.Value, soloNoteCount);
                ++currSolo;
                soloNoteCount = 0;
            }
            _notes = notes[..numNotes];
        }
    }
}

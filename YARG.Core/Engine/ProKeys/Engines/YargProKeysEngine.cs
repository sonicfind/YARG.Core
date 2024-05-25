﻿using YARG.Core.Chart;
using YARG.Core.Input;
using YARG.Core.Logging;

namespace YARG.Core.Engine.ProKeys.Engines
{
    public class YargProKeysEngine : ProKeysEngine
    {
        public YargProKeysEngine(InstrumentDifficulty<ProKeysNote> chart, SyncTrack syncTrack,
            ProKeysEngineParameters engineParameters, bool isBot) : base(chart, syncTrack, engineParameters, isBot)
        {
        }

        protected override void MutateStateWithInput(GameInput gameInput)
        {
            var action = gameInput.GetAction<ProKeysAction>();

            if (action is ProKeysAction.StarPower)
            {
                // TODO
            }
            else if (action is ProKeysAction.TouchEffects)
            {
                // TODO
            }
            else if (gameInput.Button)
            {
                State.KeyHit = (int) action;
                State.KeyMask |= 1 << (int) action;
            }
            else if (!gameInput.Button)
            {
                State.KeyReleased = (int) action;
                State.KeyMask &= ~(1 << (int) action);
            }
        }

        protected override void UpdateHitLogic(double time)
        {
            UpdateStarPower();

            // Update bot (will return if not enabled)
            UpdateBot(time);

            // Quit early if there are no notes left
            if (State.NoteIndex >= Notes.Count)
            {
                State.KeyHit = null;
                State.KeyReleased = null;
                return;
            }

            CheckForNoteHit();
        }

        protected override void CheckForNoteHit()
        {
            var parentNote = Notes[State.NoteIndex];

            // Miss out the back end
            if (!IsNoteInWindow(parentNote, out bool missed))
            {
                if (missed)
                {
                    // If one of the notes in the chord was missed out the back end,
                    // that means all of them would miss.
                    foreach (var missedNote in parentNote.ChordEnumerator())
                    {
                        MissNote(missedNote);
                    }
                }
            }
            else
            {
                // Hit note
                if (CanNoteBeHit(parentNote))
                {
                    YargLogger.LogDebug("Can hit whole note");
                    foreach (var childNote in parentNote.ChordEnumerator())
                    {
                        HitNote(childNote);
                    }

                    State.KeyHit = null;
                }
                else
                {
                    // Note cannot be hit in full, try to use chord staggering logic

                    // Note is a chord and chord staggering was active and is now expired
                    if (parentNote.IsChord)
                    {
                        if (State.ChordStaggerTimer.IsActive)
                        {
                            if (State.ChordStaggerTimer.IsExpired(State.CurrentTime))
                            {
                                YargLogger.LogFormatDebug("Ending chord staggering at {0}", State.CurrentTime);
                                foreach (var note in parentNote.ChordEnumerator())
                                {
                                    // This key in the chord was held by the time chord staggering ended, so it can be hit
                                    if ((State.KeyMask & note.NoteMask) == note.DisjointMask)
                                    {
                                        HitNote(note);
                                        YargLogger.LogFormatDebug("Hit staggered note {0} in chord", note.Key);
                                    }
                                    else
                                    {
                                        YargLogger.LogFormatDebug("Missing note {0} due to chord staggering", note.Key);
                                        MissNote(note);
                                    }
                                }

                                State.ChordStaggerTimer.Disable();
                            }
                        }
                        else
                        {
                            foreach (var note in parentNote.ChordEnumerator())
                            {
                                if ((State.KeyMask & note.NoteMask) == note.DisjointMask)
                                {
                                    YargLogger.LogFormatDebug("Starting chord staggering at {0}", State.CurrentTime);
                                    StartTimer(ref State.ChordStaggerTimer, State.CurrentTime);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // If no note was hit but the user hit a key, then over hit
            if (State.KeyHit != null)
            {
                Overhit();
                State.KeyHit = null;
            }
        }

        protected override bool CanNoteBeHit(ProKeysNote note)
        {
            return (State.KeyMask & note.NoteMask) == note.NoteMask;
        }

        protected override void UpdateBot(double time)
        {
            if (!IsBot || State.NoteIndex >= Notes.Count)
            {
                return;
            }

            var note = Notes[State.NoteIndex];

            if (time < note.Time)
            {
                return;
            }

            foreach (var chordNote in note.ChordEnumerator())
            {
                State.KeyHit = chordNote.Key;
                CheckForNoteHit();
            }
        }
    }
}
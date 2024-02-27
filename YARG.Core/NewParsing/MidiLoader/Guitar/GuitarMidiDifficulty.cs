namespace YARG.Core.NewParsing.Midi
{
    public struct GuitarMidiDifficulty
    {
        internal bool SliderNotes;
        internal bool HopoOn;
        internal bool HopoOff;

        internal readonly void ModifyNote<TFretConfig>(ref GuitarNote2<TFretConfig> note)
            where TFretConfig : unmanaged, IFretConfig
        {
            if (SliderNotes)
            {
                note.State = GuitarState.Tap;
            }
            else if (note.State == GuitarState.Tap)
            {
                if (HopoOn)
                    note.State = GuitarState.Hopo;
                else if (HopoOff)
                    note.State = GuitarState.Strum;
                else
                    note.State = GuitarState.Natural;
            }
        }
    }
}

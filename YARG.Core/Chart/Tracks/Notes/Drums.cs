namespace YARG.Core.Chart
{
    public enum DrumDynamics
    {
        None,
        Accent,
        Ghost
    }

    public struct DrumPad : IEnableable
    {
        private TruncatableSustain _duration;
        public long Duration
        {
            get { return _duration; }
            set { _duration = value; }
        }
        public DrumDynamics Dynamics { get; set; }
        public bool IsActive()
        {
            return _duration.IsActive();
        }
        public void Disable()
        {
            _duration.Disable();
            Dynamics = DrumDynamics.None;
        }
    }

    public abstract unsafe class DrumNote_FW : Note_FW<DrumPad>
    {
        protected TruncatableSustain _bass;
        protected TruncatableSustain _doubleBass;
        public ref DrumPad GetPad(int index) => ref lanes[index];

        public long Bass
        {
            get { return _bass; }
            set
            {
                _bass = value;
                _doubleBass.Disable();
            }
        }
        public long DoubleBass
        {
            get { return _doubleBass; }
            set
            {
                _doubleBass = value;
                _bass.Disable();
            }
        }
        public bool IsFlammed { get; set; }

        public long this[int lane]
        {
            get
            {
                if (lane == 0)
                    return _bass;
                if (lane == 1)
                    return _doubleBass;
                return lanes[lane - 2].Duration;
            }
            set
            {
                if (lane == 0)
                {
                    _bass = value;
                    _doubleBass.Disable();
                }
                else if (lane == 1)
                {
                    _doubleBass = value;
                    _bass.Disable();
                }
                else
                    lanes[lane - 2].Duration = value;
            }
        }

        protected DrumNote_FW(int numPads) : base(numPads) { }

        protected DrumNote_FW(int numPads, DrumNote_FW other) : this(numPads)
        {
            _bass = other._bass;
            _doubleBass = other._doubleBass;
            for (int i = 0; i < numPads; ++i)
                lanes[i] = other.lanes[i];
            IsFlammed = other.IsFlammed;
        }

        public override bool HasActiveNotes()
        {
            return HasActiveNotes() || _bass.IsActive() || _doubleBass.IsActive();
        }
    }

    public class Drum_4 : DrumNote_FW
    {
        public Drum_4() : base(4) { }
        public Drum_4(Drum_Unknown drum) : base(4, drum) { }
    }

    public class Drum_5 : DrumNote_FW
    {
        public Drum_5() : base(5) { }
        public Drum_5(Drum_Unknown drum) : base(5, drum) { }
    }

    public class Drum_4Pro : Drum_4
    {
        public readonly bool[] cymbals;
        public Drum_4Pro() : base()
        {
            cymbals = new bool[3];
        }

        public Drum_4Pro(Drum_Unknown drum) : base(drum)
        {
            cymbals = drum.cymbals;
        }
    }

    public class Drum_Unknown : DrumNote_FW
    {
        public readonly bool[] cymbals = new bool[3];
        public Drum_Unknown() : base(5) { }
    }
}

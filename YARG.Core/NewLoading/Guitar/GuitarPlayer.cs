using YARG.Core.Containers;

namespace YARG.Core.NewLoading.Guitar
{
    public struct InputState<TInput>
        where TInput : unmanaged
    {
        public static readonly InputState<TInput> Default = new()
        {
            State = default,
            Time = 0,
        };

        public TInput State { get; private set; }
        public double Time  { get; private set; }

        public void Update(TInput state, double time)
        {
            State = state;
            Time = time;
        }
    }

    public class GuitarPlayer
    {
        public InputState<bool>   Fret1      { get; private set; } = InputState<bool>.Default;
        public InputState<bool>   Fret2      { get; private set; } = InputState<bool>.Default;
        public InputState<bool>   Fret3      { get; private set; } = InputState<bool>.Default;
        public InputState<bool>   Fret4      { get; private set; } = InputState<bool>.Default;
        public InputState<bool>   Fret5      { get; private set; } = InputState<bool>.Default;
        public InputState<bool>   Fret6      { get; private set; } = InputState<bool>.Default;
        public InputState<bool>   StrumUp    { get; private set; } = InputState<bool>.Default;
        public InputState<bool>   StrumDown  { get; private set; } = InputState<bool>.Default;
        public InputState<bool>   Overdrive  { get; private set; } = InputState<bool>.Default;
        public InputState<double> WhammyAxis { get; private set; } = InputState<double>.Default;

        private readonly YargNativeList<GuitarButtonMask> _currentScope = new();
        private readonly YargNativeList<GuitarButtonMask> _buffer = new();
    }
}

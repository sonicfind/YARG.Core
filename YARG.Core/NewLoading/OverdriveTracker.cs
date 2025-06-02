using YARG.Core.Chart;
using YARG.Core.Containers;
using YARG.Core.Logging;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public enum OverdriveStyle
    {
        GH,
        RB
    }

    public enum OverdriveState
    {
        Inactive,
        Active,
        Completed
    }

    public struct OverdriveInfo
    {
        public static readonly OverdriveInfo Default = new()
        {
            State = OverdriveState.Inactive,
            Amount = 0,
            Time = 0,
        };

        public OverdriveState State;
        public double         Amount;
        public double         Time;
    }

    public class OverdriveTracker
    {
        public const double MAX_OVERDRIVE        = 480.0;
        public const double ACTIVATION_THRESHOLD = MAX_OVERDRIVE / 2;
        public const double GAIN_PER_BEAT        = MAX_OVERDRIVE / 30;
        public const double DRAIN_PER_BEAT       = -MAX_OVERDRIVE / 32;
        public const double DRAIN_PER_MEASURE    = -MAX_OVERDRIVE / 8;
        public const double OVERDRIVE_PER_PHRASE = MAX_OVERDRIVE / 4;

        private readonly OverdriveStyle _style;

        private readonly YargNativeList<(double EndTime, double ChangePerSecond)> _gains = new();
        private readonly YargNativeList<(double Time, double Amount)>             _route = new();
        private          double                                                   _baseOverdrive;
        private          double                                                   _currentOverdrive;
        private          double                                                   _timeTracker;
        private          bool                                                     _isActive;

        public OverdriveTracker(OverdriveStyle style)
        {
            _style = style;
        }

        public void Reset(double time)
        {
            _gains.Clear();
            _route.Clear();
            _isActive = false;
            _baseOverdrive = 0;
            _timeTracker = time;
        }

        public OverdriveInfo Update(double time)
        {
            YargLogger.Assert(_timeTracker <= time,
                "Backwards time progression encountered! A reset is needed before this operation!");
            _timeTracker = time;

            var info = new OverdriveInfo
            {
                State = _isActive ? OverdriveState.Active : OverdriveState.Inactive
            };

            while (_route.Count >= 2 && _route[1].Time <= time)
            {
                _route.RemoveAt(0);
                _baseOverdrive = _route[0].Amount;
                if (_route[0].Amount > 0)
                {
                    continue;
                }

                if (_isActive)
                {
                    info.State = OverdriveState.Completed;
                    info.Time = _route[0].Time;
                }
                _isActive = false;
            }

            info.Amount = _baseOverdrive;
            if (_route.Count >= 2)
            {
                var timeDiff = _route[1].Time - _route[0].Time;
                double overdriveDiff = _route[1].Amount - _route[0].Amount;

                var timeDisplacement = time - _route[0].Time;
                info.Amount += overdriveDiff * timeDisplacement / timeDiff;
            }

            _currentOverdrive = info.Amount;
            return info;
        }

        public void AddOverdrive(double time, double overdrive, BeatTracker tracker)
        {
            _currentOverdrive += overdrive;
            if (_currentOverdrive > MAX_OVERDRIVE)
            {
                _currentOverdrive = MAX_OVERDRIVE;
            }
            _baseOverdrive = _currentOverdrive;
            if (!_route.IsEmpty() && !tracker.IsComplete())
            {
                while (_gains.Count > 0 && _gains[0].EndTime <= time)
                {
                    _gains.RemoveAt(0);
                }
                BuildRoute(time, tracker);
            }
        }

        public void StartGains(double time, double endTime, BeatTracker tracker)
        {
            _baseOverdrive = _currentOverdrive;
            _gains.Clear();

            if (tracker.IsComplete())
            {
                return;
            }

            int startBeatIndex = tracker.Index;
            while (tracker[startBeatIndex].Beat == BeatlineType.Weak && startBeatIndex > 0)
            {
                --startBeatIndex;
            }

            for (int currBeatIndex = startBeatIndex; currBeatIndex + 1 < tracker.Count;)
            {
                int nextBeatIndex = currBeatIndex + 1;
                while (tracker[nextBeatIndex].Beat == BeatlineType.Weak && nextBeatIndex + 1 < tracker.Count)
                {
                    ++nextBeatIndex;
                }

                var nextBeatTime = tracker[nextBeatIndex].Position;
                var beatDiffSeconds = nextBeatTime - tracker[currBeatIndex].Position;
                if (nextBeatTime >= endTime)
                {
                    _gains.Add(
                        (
                            endTime,
                            GAIN_PER_BEAT / beatDiffSeconds
                        )
                    );
                    break;
                }

                _gains.Add(
                    (
                        nextBeatTime,
                        GAIN_PER_BEAT / beatDiffSeconds
                    )
                );
                currBeatIndex = nextBeatIndex;
            }

            BuildRoute(time, tracker);
        }

        public void StopGains(double time, BeatTracker tracker)
        {
            _baseOverdrive = _currentOverdrive;
            _gains.Clear();

            if (_isActive && !tracker.IsComplete())
            {
                BuildRoute(time, tracker);
            }
        }

        public bool ActivateOverdrive(double time, BeatTracker tracker)
        {
            if (_isActive || _currentOverdrive < ACTIVATION_THRESHOLD || tracker.IsComplete())
            {
                return false;
            }

            _baseOverdrive = _currentOverdrive;
            _isActive = true;
            while (_gains.Count > 0 && _gains[0].EndTime <= time)
            {
                _gains.RemoveAt(0);
            }

            BuildRoute(time, tracker);
            return true;
        }

        private void BuildRoute(double time, BeatTracker tracker)
        {
            int gainIndex = 0;
            _route.Clear();
            _route.Add((time, _baseOverdrive));

            // Drains (w/ potential whammy gains)
            if (_isActive)
            {
                int startBeat = tracker.Index;

                if (_style == OverdriveStyle.RB)
                {
                    while (tracker[startBeat].Beat == BeatlineType.Weak && startBeat > 0)
                    {
                        --startBeat;
                    }
                }
                else
                {
                    while (tracker[startBeat].Beat != BeatlineType.Measure && startBeat > 0)
                    {
                        --startBeat;
                    }
                }

                // Validates the initial "+1" access
                while (startBeat + 1 < tracker.Count)
                {
                    int nextBeat = startBeat + 1;
                    double drainPerSecond;
                    if (_style == OverdriveStyle.RB)
                    {
                        drainPerSecond = DRAIN_PER_BEAT;
                        // RB drains based on the beat track
                        while (tracker[nextBeat].Beat == BeatlineType.Weak && nextBeat < tracker.Count - 1)
                        {
                            ++nextBeat;
                        }
                    }
                    else
                    {
                        drainPerSecond = DRAIN_PER_MEASURE;
                        // GH drains on a per-measure basis
                        while (tracker[nextBeat].Beat != BeatlineType.Measure && nextBeat < tracker.Count - 1)
                        {
                            ++nextBeat;
                        }
                    }

                    var nextDrainTime = tracker[nextBeat].Position;
                    drainPerSecond /= nextDrainTime - tracker[startBeat].Position;

                    // If either of these return false, we still have some overdrive left to work through
                    if (AddRouteDrainsAndGains(ref gainIndex, ref drainPerSecond, nextDrainTime) ||
                        AddRouteDrain(drainPerSecond, nextDrainTime))
                    {
                        break;
                    }

                    startBeat = nextBeat;
                }
            }

            while (gainIndex < _gains.Count)
            {
                ref readonly var prevNode = ref _route[_route.Count - 1];
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (prevNode.Amount == MAX_OVERDRIVE)
                {
                    break;
                }

                ref readonly var gain = ref _gains[gainIndex];

                var timeDiff = gain.EndTime - prevNode.Time;
                double overdriveChange = gain.ChangePerSecond * timeDiff;
                double nextAmount = prevNode.Amount + overdriveChange;

                if (nextAmount <= MAX_OVERDRIVE)
                {
                    _route.Add((gain.EndTime, nextAmount));
                }
                else
                {
                    double changeToOne = MAX_OVERDRIVE - prevNode.Amount;
                    double secondsToOne = changeToOne / gain.ChangePerSecond;
                    _route.Add((prevNode.Time + secondsToOne, MAX_OVERDRIVE));
                }
                ++gainIndex;
            }
        }

        private bool AddRouteDrainsAndGains(ref int gainIndex, ref double changePerSecond, double drainEnd)
        {
            double initialDrainRate = changePerSecond;
            while (gainIndex < _gains.Count)
            {
                ref readonly var gain = ref _gains[gainIndex];
                changePerSecond += gain.ChangePerSecond;

                if (gain.EndTime >= drainEnd)
                {
                    break;
                }

                if (AddRouteDrain(changePerSecond, gain.EndTime))
                {
                    return true;
                }
                ++gainIndex;
                changePerSecond = initialDrainRate;
            }
            return false;
        }

        private bool AddRouteDrain(double changePerSecond, double endTime)
        {
            ref readonly var prevNode = ref _route[_route.Count - 1];

            var timeDiff = endTime - prevNode.Time;
            double overdriveChange = changePerSecond * timeDiff;
            double nextAmount = prevNode.Amount + overdriveChange;
            if (nextAmount < 0)
            {
                // Negative amount because the change is negative
                double secondsToZero = -prevNode.Amount / changePerSecond;
                _route.Add((prevNode.Time + secondsToZero, 0));
                return true;
            }

            if (nextAmount > MAX_OVERDRIVE)
            {
                nextAmount = MAX_OVERDRIVE;
                if (prevNode.Amount < MAX_OVERDRIVE)
                {
                    double changeToOne = MAX_OVERDRIVE - prevNode.Amount;
                    double secondsToOne = changeToOne / changePerSecond;
                    _route.Add((prevNode.Time + secondsToOne, MAX_OVERDRIVE));
                }
            }
            _route.Add((endTime, nextAmount));
            return false;
        }
    }
}

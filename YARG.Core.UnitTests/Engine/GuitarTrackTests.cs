using NUnit.Framework;
using YARG.Core.Game;
using YARG.Core.NewLoading;
using YARG.Core.NewLoading.Guitar;
using YARG.Core.NewParsing;
using YARG.Core.Song;

namespace YARG.Core.UnitTests.Engine;

public class GuitarTrackTests
{
    private static YARGChart _chart;
    private static DualTime  _endtime;
    private static GuitarTrack _baseTrack;

    [SetUp]
    public static void Setup()
    {
        string workingDirectory = Environment.CurrentDirectory;
        string projectDirectory = Directory.GetParent(workingDirectory)!.Parent!.Parent!.FullName;
        string chartDirectory = Path.Combine(projectDirectory, "Engine", "Test Charts");
        string chartPath = Path.Combine(chartDirectory, "crashtest.mid");
        _chart = YARGChart.LoadMidi_Single(chartPath, null);
        _endtime = _chart.GetEndTime();

        //var selection = new InstrumentSelection
        //{
        //    Instrument = Instrument.FiveFretGuitar,
        //    Difficulty = Difficulty.Expert,
        //    Modifiers = Modifier.None
        //};

        //_baseTrack = GuitarTrack.Create(_chart, _chart.FiveFretGuitar, in _endtime, selection);
    }

    [TearDown]
    public static void Cleanup()
    {
        _chart.Dispose();
        //_baseTrack.Dispose();
    }

    [Test]
    public void TrackModifierTests()
    {
        var selection = new InstrumentSelection
        {
            Instrument = Instrument.FiveFretGuitar,
            Difficulty = Difficulty.Expert,
            Modifiers = Modifier.None
        };

        using var baseTrack = GuitarTrack.Create(_chart, _chart.FiveFretGuitar, _endtime, selection);
        //var selection = new InstrumentSelection
        //{
        //    Instrument = Instrument.FiveFretGuitar,
        //    Difficulty = Difficulty.Expert,
        //    Modifiers = Modifier.DoubleNotes
        //};

        //using var doubleNotes = GuitarTrack.Create(_chart, _chart.FiveFretGuitar, in _endtime, selection);

        //selection.Modifiers = Modifier.NoteShuffle;
        //using var noteShuffle = GuitarTrack.Create(_chart, _chart.FiveFretGuitar, in _endtime, selection);

        //selection.Modifiers = Modifier.DoubleNotes | Modifier.NoteShuffle;
        //using var combo = GuitarTrack.Create(_chart, _chart.FiveFretGuitar, in _endtime, selection);
    }
}
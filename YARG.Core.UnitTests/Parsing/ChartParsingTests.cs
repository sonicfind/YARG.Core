using MoonscraperChartEditor.Song.IO;
using NUnit.Framework;
using YARG.Core.Chart;

namespace YARG.Core.UnitTests.Parsing
{
    public class ChartParsingTests
    {
        private string? chartsDirectory;
        private ParseSettings settings = ParseSettings.Default;

        [SetUp]
        public void Setup()
        {
            // This will get the current WORKING directory (i.e. \bin\Debug)
            string workingDirectory = Environment.CurrentDirectory;

            // This will get the current PROJECT directory
            string projectDirectory = Directory.GetParent(workingDirectory)!.Parent!.Parent!.FullName;

            chartsDirectory = Path.Combine(projectDirectory, "Parsing", "Test Charts");
            settings.StarPowerNote = 116;
        }

        [TestCase("test.chart")]
        public void ParseChartFile_New(string notesFile)
        {
            YargTrace.AddListener(new YargDebugTraceListener());

            Assert.DoesNotThrow(() =>
            {
                string chartPath = Path.Combine(chartsDirectory!, notesFile);
                var chart = DotChartLoader.Load(chartPath, settings, null, true);
            });
        }

        [TestCase("test.chart")]
        public void ParseChartFile(string notesFile)
        {
            YargTrace.AddListener(new YargDebugTraceListener());

            Assert.DoesNotThrow(() =>
            {
                string chartPath = Path.Combine(chartsDirectory!, notesFile);
                var song = ChartReader.ReadFromFile(settings, chartPath);
            });
        }

        [TestCase("test.mid")]
        public void ParseMidiFile_New(string notesFile)
        {
            YargTrace.AddListener(new YargDebugTraceListener());

            Assert.DoesNotThrow(() =>
            {
                string chartPath = Path.Combine(chartsDirectory!, notesFile);
                var chart = DotMidiLoader.LoadFull(chartPath, settings, null);
            });
        }

        [TestCase("test.mid")]
        public void ParseMidiFile(string notesFile)
        {
            YargTrace.AddListener(new YargDebugTraceListener());

            Assert.DoesNotThrow(() =>
            {
                string chartPath = Path.Combine(chartsDirectory!, notesFile);
                var song = MidReader.ReadMidi(settings, chartPath);
            });
        }
    }
}
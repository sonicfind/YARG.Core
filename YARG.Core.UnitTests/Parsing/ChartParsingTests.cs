using MoonscraperChartEditor.Song.IO;
using NUnit.Framework;
using YARG.Core.Chart;
using YARG.Core.Parsing;

namespace YARG.Core.UnitTests.Parsing
{
    public class ChartParsingTests
    {
        private string? chartsDirectory;

        [SetUp]
        public void Setup()
        {
            // This will get the current WORKING directory (i.e. \bin\Debug)
            string workingDirectory = Environment.CurrentDirectory;

            // This will get the current PROJECT directory
            string projectDirectory = Directory.GetParent(workingDirectory)!.Parent!.Parent!.FullName;

            chartsDirectory = Path.Combine(projectDirectory, "Parsing", "Test Charts");
        }

        [TestCase("test.chart")]
        public void ParseChartFile_New(string notesFile)
        {
            YargTrace.AddListener(new YargDebugTraceListener());
            Assert.DoesNotThrow(() =>
            {
                string chartPath = Path.Combine(chartsDirectory!, notesFile);
                using var chart = DotChartLoader.Load(chartPath, ParseSettings.Default, null, true);
            });
        }

        [TestCase("test.chart")]
        public void ParseChartFile(string notesFile)
        {
            YargTrace.AddListener(new YargDebugTraceListener());

            Assert.DoesNotThrow(() =>
            {
                string chartPath = Path.Combine(chartsDirectory!, notesFile);
                var song = ChartReader.ReadFromFile(ParseSettings.Default, chartPath);
            });
        }

        [TestCase("test.mid")]
        public void ParseMidiFile_New(string notesFile)
        {
            YargTrace.AddListener(new YargDebugTraceListener());
            Assert.DoesNotThrow(() =>
            {
                string chartPath = Path.Combine(chartsDirectory!, notesFile);
                using var chart = DotMidiLoader.LoadFull(chartPath, ParseSettings.Default, null);
            });
        }

        [TestCase("test.mid")]
        public void ParseMidiFile(string notesFile)
        {
            YargTrace.AddListener(new YargDebugTraceListener());

            Assert.DoesNotThrow(() =>
            {
                string chartPath = Path.Combine(chartsDirectory!, notesFile);
                var song = MidReader.ReadMidi(ParseSettings.Default, chartPath);
            });
        }
    }
}
using MoonscraperChartEditor.Song;
using MoonscraperChartEditor.Song.IO;
using NUnit.Framework;
using YARG.Core.Chart;
using YARG.Core.IO.Disposables;
using YARG.Core.Logging;
using YARG.Core.NewParsing;

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
        public void ParseChartFile(string notesFile)
        {
            YargLogger.AddLogListener(new DebugYargLogListener());

            Assert.DoesNotThrow(() =>
            {
                string chartPath = Path.Combine(chartsDirectory!, notesFile);
                var song = ChartReader.ReadFromFile(chartPath);
            });
        }

        [TestCase("test.chart")]
        [TestCase("test.mid")]
        public void ParseChartFile_Full(string notesFile)
        {
            YargLogger.AddLogListener(new DebugYargLogListener());

            Assert.DoesNotThrow(() =>
            {
                string chartPath = Path.Combine(chartsDirectory!, notesFile);
                var song = SongChart.FromFile(ParseSettings.Default, chartPath);
            });
        }

        [TestCase("test.chart")]
        public void ParseChartFile_New(string notesFile)
        {
            YargLogger.AddLogListener(new DebugYargLogListener());
            Assert.DoesNotThrow(() =>
            {
                string chartPath = Path.Combine(chartsDirectory!, notesFile);
                var info = new FileInfo(chartPath);
                using var file = MemoryMappedArray.Load(info);
                using var chart = YARGDotChartLoader.Load(file, Song.SongMetadata.Default, ParseSettings.Default, null);
            });
        }

        [TestCase("test.mid")]
        public void ParseMidiFile(string notesFile)
        {
            YargLogger.AddLogListener(new DebugYargLogListener());

            Assert.DoesNotThrow(() =>
            {
                string chartPath = Path.Combine(chartsDirectory!, notesFile);
                var song = MidReader.ReadMidi(chartPath);
            });
        }

        [TestCase("test.mid")]
        public void ParseMidiFile_New(string notesFile)
        {
            YargLogger.AddLogListener(new DebugYargLogListener());
            Assert.DoesNotThrow(() =>
            {
                string chartPath = Path.Combine(chartsDirectory!, notesFile);
                using var chart = DotMidiLoader.LoadSingle(chartPath, Song.SongMetadata.Default, ParseSettings.Default, null);
            });
        }
    }
}
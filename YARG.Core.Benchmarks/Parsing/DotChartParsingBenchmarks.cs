using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Engines;
using YARG.Core.Chart;
using YARG.Core.IO;
using YARG.Core.NewParsing;
using YARG.Core.Song;

namespace YARG.Core.Benchmarks
{
    // [SimpleJob(RunStrategy.ColdStart, targetCount: 25, invocationCount: 1)]
    //[NativeMemoryProfiler] // Enable LOCALLY ONLY if on Windows
    [MemoryDiagnoser]
    public class DotChartParsingBenchmarks
    {
        private static string chartPath;
        private readonly HashSet<Instrument> guitarOnly = new()
        {
            { Instrument.FiveFretGuitar },
        };

        [GlobalSetup]
        public static void Initialize()
        {
            chartPath = Environment.GetEnvironmentVariable(Program.CHART_PATH_VAR);
            if (!File.Exists(chartPath))
            {
                throw new FileNotFoundException(chartPath);
            }
        }

        [Benchmark]
        public void SongParsing_New()
        {
            using var chart = YARGDotChartLoader.Load(chartPath, null);
        }

        [Benchmark]
        public void SongParsing_New_GuitarOnly()
        {
            using var chart = YARGDotChartLoader.Load(chartPath, guitarOnly);
        }

        [Benchmark]
        public void SongParsing()
        {
            MoonSongLoader.LoadDotChart(ParseSettings.Default_Chart, File.ReadAllText(chartPath));
        }

        [Benchmark]
        public SongChart FullChartParsing()
        {
            return SongChart.FromDotChart(in ParseSettings.Default_Chart, File.ReadAllText(chartPath));
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Engines;
using YARG.Core.Chart;
using YARG.Core.IO.Disposables;
using YARG.Core.NewParsing;

namespace YARG.Core.Benchmarks
{
    // [SimpleJob(RunStrategy.ColdStart, targetCount: 25, invocationCount: 1)]
    [NativeMemoryProfiler] // Enable LOCALLY ONLY if on Windows
    [MemoryDiagnoser]
    public class DotChartParsingBenchmarks
    {
        private static FileInfo chartInfo;
        private ParseSettings settings = ParseSettings.Default;
        private readonly Dictionary<Instrument, HashSet<Difficulty>> expertGuitarOnly = new()
        {
            { Instrument.FiveFretGuitar, new() { Difficulty.Expert } },
        };

        [GlobalSetup]
        public static void Initialize()
        {
            chartInfo = new FileInfo(Environment.GetEnvironmentVariable(Program.CHART_PATH_VAR));
            if (!chartInfo.Exists)
            {
                throw new FileNotFoundException(chartInfo.FullName);
            }
        }

        [Benchmark]
        public void SongParsing_New()
        {
            using var chart = YARGDotChartLoader.Load(chartInfo, null);
        }

        [Benchmark]
        public void SongParsing_New_GuitarOnly()
        {
            using var chart = YARGDotChartLoader.Load(chartInfo, expertGuitarOnly);
        }

        [Benchmark]
        public void SongParsing()
        {
            MoonSongLoader.LoadDotChart(settings, File.ReadAllText(chartInfo.FullName));
        }

        [Benchmark]
        public SongChart FullChartParsing()
        {
            return SongChart.FromDotChart(settings, File.ReadAllText(chartInfo.FullName));
        }
    }
}

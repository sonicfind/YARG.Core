using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs; // Enable LOCALLY ONLY if on Windows
using YARG.Core.Chart;
using YARG.Core.IO;

namespace YARG.Core.Benchmarks
{
    // [SimpleJob(RunStrategy.ColdStart, targetCount: 25, invocationCount: 1)]
    [NativeMemoryProfiler] // Enable LOCALLY ONLY if on Windows
    [MemoryDiagnoser]
    public class DotChartParsingBenchmarks
    {
        private static string ChartPath;
        private ParseSettings settings = ParseSettings.Default;
		private readonly Dictionary<NoteTracks_Chart, HashSet<Difficulty>> guitarOnly = new()
        {
            { NoteTracks_Chart.Single, new() { Difficulty.Expert } },
        };
		
        [GlobalSetup]
        public static void Initialize()
        {
            ChartPath = Environment.GetEnvironmentVariable(Program.CHART_PATH_VAR);
        }

        [Benchmark]
        public void SongParsing_New()
        {
            using var chart = DotChartLoader.Load(ChartPath, null, null, true);
        }

        [Benchmark]
        public void SongParsing_New_GuitarOnly()
        {
            using var chart = DotChartLoader.Load(ChartPath, null, guitarOnly, true);
        }

        [Benchmark]
        public void SongParsing()
        {
            MoonSongLoader.LoadDotChart(settings, File.ReadAllText(ChartPath));
        }

        [Benchmark]
        public SongChart FullChartParsing()
        {
            return SongChart.FromDotChart(settings, File.ReadAllText(ChartPath));
        }
    }
}

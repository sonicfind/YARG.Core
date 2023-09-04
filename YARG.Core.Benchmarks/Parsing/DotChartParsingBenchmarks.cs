using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using YARG.Core.Chart;
using YARG.Core.IO;

namespace YARG.Core.Benchmarks
{
    // [SimpleJob(RunStrategy.ColdStart, targetCount: 25, invocationCount: 1)]
    // [NativeMemoryProfiler] // 1. Windows Only, 2. Need to figure out why it's not showing the native allocations
    [MemoryDiagnoser]
    public class DotChartParsingBenchmarks
    {
        private static string ChartPath;
        private ParseSettings settings = ParseSettings.Default;
		
        [GlobalSetup]
        public void Initialize()
        {
            ChartPath = Environment.GetEnvironmentVariable(Program.CHART_PATH_VAR);
        }

        [Benchmark]
        public static void SongParsing_New()
        {
            using var chart = DotChartLoader.Load(ChartPath, null, null, true);
        }

        [Benchmark]
        public static void SongParsing_New_GuitarOnly()
        {
            Dictionary<NoteTracks_Chart, HashSet<Difficulty>> instruments = new()
            {
                { NoteTracks_Chart.Single, new() { Difficulty.Expert } },
            };
            using var chart = DotChartLoader.Load(ChartPath, null, instruments, true);
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

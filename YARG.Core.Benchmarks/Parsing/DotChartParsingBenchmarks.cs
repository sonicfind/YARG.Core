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
        private readonly HashSet<Instrument> guitarOnly = new()
        {
            { Instrument.FiveFretGuitar },
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
            using var file = MemoryMappedArray.Load(chartInfo);
            using var chart = YARGDotChartLoader.Load(file, Song.SongMetadata.Default, ParseSettings.Default, null);
        }

        [Benchmark]
        public void SongParsing_New_GuitarOnly()
        {
            using var file = MemoryMappedArray.Load(chartInfo);
            using var chart = YARGDotChartLoader.Load(file, Song.SongMetadata.Default, ParseSettings.Default, guitarOnly);
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

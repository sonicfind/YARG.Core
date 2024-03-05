using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Engines;
using Melanchall.DryWetMidi.Core;
using YARG.Core.Chart;
using YARG.Core.IO;
using YARG.Core.NewParsing;

namespace YARG.Core.Benchmarks
{
    // [SimpleJob(RunStrategy.ColdStart, targetCount: 25, invocationCount: 1)]
    [NativeMemoryProfiler] // Enable LOCALLY ONLY if on Windows
    [MemoryDiagnoser]
    public class MidiParsingBenchmarks
    {
        private static FileInfo chartInfo;
        private static ParseSettings settings = ParseSettings.Default;
        private static readonly HashSet<MidiTrackType> guitarOnly = new()
        {
            { MidiTrackType.Guitar_5 },
            { MidiTrackType.Vocals }
        };

        [GlobalSetup]
        public static void Initialize()
        {
            chartInfo = new FileInfo(Environment.GetEnvironmentVariable(Program.CHART_PATH_VAR));
            if (!chartInfo.Exists)
            {
                throw new FileNotFoundException(chartInfo.FullName);
            }
            settings.StarPowerNote = 116;
        }

        [Benchmark]
        public void SongLoading_New()
        {
            using var chart = DotMidiLoader.LoadSingle(chartInfo, null);
        }

        [Benchmark]
        public void SongLoading_New_GuitarOnly()
        {
            using var chart = DotMidiLoader.LoadSingle(chartInfo, guitarOnly);
        }

        [Benchmark]
        public void SongLoading()
        {
            MoonSongLoader.LoadSong(settings, chartInfo.FullName);
        }

        [Benchmark]
        public SongChart FullChartLoading()
        {
            return SongChart.FromMidi(settings, MidiFile.Read(chartInfo.FullName));
        }
    }
}

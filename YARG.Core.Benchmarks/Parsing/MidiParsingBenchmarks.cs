using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs; // Enable LOCALLY ONLY if on Windows
using Melanchall.DryWetMidi.Core;
using YARG.Core.Chart;
using YARG.Core.IO;

namespace YARG.Core.Benchmarks
{
    // [SimpleJob(RunStrategy.ColdStart, targetCount: 25, invocationCount: 1)]
    [NativeMemoryProfiler] // Enable LOCALLY ONLY if on Windows
    [MemoryDiagnoser]
    public class MidiParsingBenchmarks
    {
        private static string ChartPath;
        private static ParseSettings settings = ParseSettings.Default;
        private static readonly Dictionary<MidiTrackType, HashSet<Difficulty>> guitarOnly = new()
        {
            { MidiTrackType.Guitar_5, new() { Difficulty.Expert } },
            { MidiTrackType.Vocals, null }
        };

        [GlobalSetup]
        public static void Initialize()
        {
            ChartPath = Environment.GetEnvironmentVariable(Program.CHART_PATH_VAR);
            settings.StarPowerNote = 116;
        }

        [Benchmark]
        public void SongLoading_New()
        {
            using var chart = DotMidiLoader.LoadFull(ChartPath, settings, null);
        }

        [Benchmark]
        public void SongLoading_New_GuitarOnly()
        {
            using var chart = DotMidiLoader.LoadFull(ChartPath, settings, guitarOnly);
        }

        [Benchmark]
        public void SongLoading()
        {
            MoonSongLoader.LoadSong(settings, ChartPath);
        }

        [Benchmark]
        public SongChart FullChartLoading()
        {
            return SongChart.FromMidi(settings, MidiFile.Read(ChartPath));
        }
    }
}

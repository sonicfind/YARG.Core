using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Melanchall.DryWetMidi.Core;
using YARG.Core.Chart;
using YARG.Core.IO;

namespace YARG.Core.Benchmarks
{
    // [SimpleJob(RunStrategy.ColdStart, targetCount: 25, invocationCount: 1)]
    // [NativeMemoryProfiler] // 1. Windows Only, 2. Need to figure out why it's not showing the native allocations
    [MemoryDiagnoser]
    public class MidiParsingBenchmarks
    {
        private static string ChartPath;
        private ParseSettings settings = ParseSettings.Default;
        private MidiFile midi;

        [GlobalSetup]
        public void Initialize()
        {
            ChartPath = Environment.GetEnvironmentVariable(Program.CHART_PATH_VAR);
            midi = MidiFile.Read(ChartPath);
        }

        [Benchmark]
        public void SongLoading_New()
        {
            using var chart = DotMidiLoader.LoadFull(ChartPath, settings, null);
        }

        [Benchmark]
        public void SongLoading_New_GuitarOnly()
        {
            Dictionary<MidiTrackType, HashSet<Difficulty>> instruments = new()
            {
                { MidiTrackType.Guitar_5, new() { Difficulty.Expert } },
                { MidiTrackType.Vocals, null }
            };
            using var chart = DotMidiLoader.LoadFull(ChartPath, settings, instruments);
        }

        [Benchmark]
        public void SongLoading()
        {
            MoonSongLoader.LoadSong(settings, ChartPath);
        }

        [Benchmark]
        public SongChart FullChartLoading()
        {
            return SongChart.FromMidi(settings, midi);
        }
    }
}

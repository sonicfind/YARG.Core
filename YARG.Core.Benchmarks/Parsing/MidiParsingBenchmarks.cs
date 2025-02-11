using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Engines;
using Melanchall.DryWetMidi.Core;
using YARG.Core.Chart;
using YARG.Core.IO;
using YARG.Core.NewParsing;
using YARG.Core.Song;

namespace YARG.Core.Benchmarks
{
    // [SimpleJob(RunStrategy.ColdStart, targetCount: 25, invocationCount: 1)]
    //[NativeMemoryProfiler] // Enable LOCALLY ONLY if on Windows
    [MemoryDiagnoser]
    public class MidiParsingBenchmarks
    {
        private static string ChartPath;
        private static ParseSettings settings = ParseSettings.Default;
        private static readonly HashSet<MidiTrackType> guitarOnly = new()
        {
            { MidiTrackType.Guitar_5 },
            { MidiTrackType.Vocals }
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
            using var chart = DotMidiLoader.LoadSingle(ChartPath, null);
        }

        [Benchmark]
        public void SongLoading_New_GuitarOnly()
        {
            using var chart = DotMidiLoader.LoadSingle(ChartPath, guitarOnly);
        }

        [Benchmark]
        public void SongLoading()
        {
            MoonSongLoader.LoadSong(ParseSettings.Default_Midi, ChartPath);
        }

        [Benchmark]
        public SongChart FullChartLoading()
        {
            return SongChart.FromMidi(in ParseSettings.Default_Midi, MidiFile.Read(ChartPath));
        }
    }
}

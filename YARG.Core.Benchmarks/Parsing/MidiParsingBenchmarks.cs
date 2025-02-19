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
using YARG.Core.Song;

namespace YARG.Core.Benchmarks
{
    // [SimpleJob(RunStrategy.ColdStart, targetCount: 25, invocationCount: 1)]
    //[NativeMemoryProfiler] // Enable LOCALLY ONLY if on Windows
    [MemoryDiagnoser]
    public class MidiParsingBenchmarks
    {
        private static string chartPath;
        private static ParseSettings settings = ParseSettings.Default;
        private static readonly HashSet<MidiTrackType> guitarOnly = new()
        {
            { MidiTrackType.Guitar_5 },
            { MidiTrackType.Vocals }
        };

        [GlobalSetup]
        public static void Initialize()
        {
            chartPath = Environment.GetEnvironmentVariable(Program.CHART_PATH_VAR);
            if (!File.Exists(chartPath))
            {
                throw new FileNotFoundException(chartPath);
            }
            settings.StarPowerNote = 116;
        }

        [Benchmark]
        public void YARGChartLoading()
        {
            using var chart = YARGChart.LoadMidi_Single(chartPath, null);
        }

        [Benchmark]
        public void YARGChartLoading_GuitarOnly()
        {
            using var chart = YARGChart.LoadMidi_Single(chartPath, guitarOnly);
        }

        [Benchmark]
        public void MoonSongParsing()
        {
            MoonSongLoader.LoadSong(ParseSettings.Default_Midi, chartPath);
        }

        [Benchmark]
        public SongChart SongChartLoading()
        {
            return SongChart.FromMidi(in ParseSettings.Default_Midi, MidiFile.Read(chartPath));
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using YARG.Core.Audio;
using YARG.Core.Chart;
using YARG.Core.IO;
using YARG.Core.IO.Disposables;
using YARG.Core.Logging;
using YARG.Core.NewParsing;
using YARG.Core.Venue;

namespace YARG.Core.Song
{
    public class BackgroundResult : IDisposable
    {
        public readonly BackgroundType Type;
        public readonly Stream? Stream;
        public readonly YARGImage? Image;

        public BackgroundResult(BackgroundType type, Stream stream)
        {
            Type = type;
            Stream = stream;
        }

        public BackgroundResult(YARGImage? image)
        {
            Type = BackgroundType.Image;
            Image = image;
        }

        public void Dispose()
        {
            Stream?.Dispose();
            Image?.Dispose();
        }
    }

    public abstract partial class SongEntry
    {
        public abstract SongChart? LoadChart();
        public abstract YARGChart? LoadChart_New(Dictionary<Instrument, HashSet<Difficulty>> activeInstruments);
        public abstract StemMixer? LoadAudio(float speed, double volume, params SongStem[] ignoreStems);
        public abstract StemMixer? LoadPreviewAudio(float speed);
        public abstract YARGImage? LoadAlbumData();
        public abstract BackgroundResult? LoadBackground(BackgroundType options);
        public abstract FixedArray<byte>? LoadMiloData();

        protected static Dictionary<MidiTrackType, HashSet<Difficulty>?> ConvertToMidiTrackDict(Dictionary<Instrument, HashSet<Difficulty>> activeInstruments)
        {
            var tracks = new Dictionary<MidiTrackType, HashSet<Difficulty>?>();
            foreach (var instrument in activeInstruments)
            {
                switch (instrument.Key)
                {
                    case Instrument.FiveFretGuitar:     tracks.Add(MidiTrackType.Guitar_5, instrument.Value); break;
                    case Instrument.FiveFretBass:       tracks.Add(MidiTrackType.Bass_5,   instrument.Value); break;
                    case Instrument.FiveFretRhythm:     tracks.Add(MidiTrackType.Rhythm_5, instrument.Value); break;
                    case Instrument.FiveFretCoopGuitar: tracks.Add(MidiTrackType.Coop_5,   instrument.Value); break;

                    case Instrument.Keys: tracks.Add(MidiTrackType.Keys, instrument.Value); break;

                    case Instrument.SixFretGuitar:     tracks.Add(MidiTrackType.Guitar_6, instrument.Value); break;
                    case Instrument.SixFretBass:       tracks.Add(MidiTrackType.Bass_6,   instrument.Value); break;
                    case Instrument.SixFretRhythm:     tracks.Add(MidiTrackType.Rhythm_6, instrument.Value); break;
                    case Instrument.SixFretCoopGuitar: tracks.Add(MidiTrackType.Coop_6,   instrument.Value); break;

                    case Instrument.FourLaneDrums:
                    case Instrument.ProDrums:
                    case Instrument.FiveLaneDrums:
                        if (!tracks.TryGetValue(MidiTrackType.Drums, out var drums))
                        {
                            tracks.Add(MidiTrackType.Drums, drums = new HashSet<Difficulty>());
                        }
                        drums!.UnionWith(instrument.Value);
                        break;
                    case Instrument.ProGuitar_17Fret: tracks.Add(MidiTrackType.Pro_Guitar_17, instrument.Value); break;
                    case Instrument.ProGuitar_22Fret: tracks.Add(MidiTrackType.Pro_Guitar_22, instrument.Value); break;
                    case Instrument.ProBass_17Fret:   tracks.Add(MidiTrackType.Pro_Bass_17,   instrument.Value); break;
                    case Instrument.ProBass_22Fret:   tracks.Add(MidiTrackType.Pro_Bass_22,   instrument.Value); break;

                    case Instrument.ProKeys:
                        foreach (var difficulty in instrument.Value)
                        {
                            tracks.Add(difficulty switch
                            {
                                Difficulty.Easy =>   MidiTrackType.Pro_Keys_E,
                                Difficulty.Medium => MidiTrackType.Pro_Keys_M,
                                Difficulty.Hard =>   MidiTrackType.Pro_Keys_H,
                                Difficulty.Expert => MidiTrackType.Pro_Keys_X,
                                _ => throw new NotImplementedException()
                            }, null);
                        }
                        break;
                    case Instrument.Vocals: tracks.Add(MidiTrackType.Vocals, null); break;
                    case Instrument.Harmony:
                        if (!tracks.ContainsKey(MidiTrackType.Harm1))
                        {
                            tracks.Add(MidiTrackType.Harm1, null);
                            tracks.Add(MidiTrackType.Harm2, null);
                            tracks.Add(MidiTrackType.Harm3, null);
                        }
                        break;
                }
            }
            return tracks;
        }
    }
}

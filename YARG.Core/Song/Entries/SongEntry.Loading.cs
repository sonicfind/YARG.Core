using System;
using System.Collections.Generic;
using System.IO;
using YARG.Core.Audio;
using YARG.Core.Chart;
using YARG.Core.IO;
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
        public abstract YARGChart? LoadChart_New(HashSet<Instrument> activeInstruments);
        public abstract StemMixer? LoadAudio(float speed, double volume, params SongStem[] ignoreStems);
        public abstract StemMixer? LoadPreviewAudio(float speed);
        public abstract YARGImage? LoadAlbumData();
        public abstract BackgroundResult? LoadBackground(BackgroundType options);
        public abstract FixedArray<byte> LoadMiloData();

        protected static HashSet<MidiTrackType> ConvertToMidiTracks(HashSet<Instrument> activeInstruments)
        {
            var tracks = new HashSet<MidiTrackType>();
            foreach (var instrument in activeInstruments)
            {
                switch (instrument)
                {
                    case Instrument.FiveFretGuitar:     tracks.Add(MidiTrackType.Guitar_5); break;
                    case Instrument.FiveFretBass:       tracks.Add(MidiTrackType.Bass_5); break;
                    case Instrument.FiveFretRhythm:     tracks.Add(MidiTrackType.Rhythm_5); break;
                    case Instrument.FiveFretCoopGuitar: tracks.Add(MidiTrackType.Coop_5); break;

                    case Instrument.Keys: tracks.Add(MidiTrackType.Keys); break;

                    case Instrument.SixFretGuitar:     tracks.Add(MidiTrackType.Guitar_6); break;
                    case Instrument.SixFretBass:       tracks.Add(MidiTrackType.Bass_6); break;
                    case Instrument.SixFretRhythm:     tracks.Add(MidiTrackType.Rhythm_6); break;
                    case Instrument.SixFretCoopGuitar: tracks.Add(MidiTrackType.Coop_6); break;

                    case Instrument.FourLaneDrums:
                    case Instrument.ProDrums:
                    case Instrument.FiveLaneDrums:
                        tracks.Add(MidiTrackType.Drums);
                        break;
                    case Instrument.ProGuitar_17Fret: tracks.Add(MidiTrackType.Pro_Guitar_17); break;
                    case Instrument.ProGuitar_22Fret: tracks.Add(MidiTrackType.Pro_Guitar_22); break;
                    case Instrument.ProBass_17Fret:   tracks.Add(MidiTrackType.Pro_Bass_17); break;
                    case Instrument.ProBass_22Fret:   tracks.Add(MidiTrackType.Pro_Bass_22); break;

                    case Instrument.ProKeys:
                        tracks.Add(MidiTrackType.Pro_Keys_E);
                        tracks.Add(MidiTrackType.Pro_Keys_M);
                        tracks.Add(MidiTrackType.Pro_Keys_H);
                        tracks.Add(MidiTrackType.Pro_Keys_X);
                        break;
                    case Instrument.Vocals: tracks.Add(MidiTrackType.Vocals); break;
                    case Instrument.Harmony:
                        tracks.Add(MidiTrackType.Harm1);
                        tracks.Add(MidiTrackType.Harm2);
                        tracks.Add(MidiTrackType.Harm3);
                        break;
                }
            }
            return tracks;
        }
    }
}

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using YARG.Core.Game;
using YARG.Core.IO;
using YARG.Core.Logging;
using YARG.Core.Song;
using YARG.Core.Utility;

namespace YARG.Core.Replays
{
    public class Replay
    {
        public static readonly EightCC REPLAY_MAGIC_HEADER = new('Y', 'A', 'R', 'G', 'P', 'L', 'A', 'Y');
        public const int REPLAY_VERSION = 5;
        public const int ENGINE_VERSION = 0;

        public string      SongName;
        public string      ArtistName;
        public string      CharterName;
        public int         BandScore;
        public StarAmount  BandStars;
        public double      ReplayLength;
        public DateTime    Date;
        public HashWrapper SongChecksum;

        public ReplayPresetContainer ReplayPresetContainer;

        public readonly string[] PlayerNames;
        public readonly ReplayFrame[] Frames;

        public int PlayerCount => PlayerNames.Length;

        public Replay(SongEntry song, double length, string[] players, ReplayFrame[] frames, ReplayPresetContainer presets, int score, StarAmount stars)
        {
            SongName = song.Name;
            ArtistName = song.Artist;
            CharterName = song.Charter;
            SongChecksum = song.Hash;
            Date = DateTime.Now;
            ReplayLength = length;
            PlayerNames = players;
            Frames = frames;
            ReplayPresetContainer = presets;
            BandScore = score;
            BandStars = stars;
        }

        public Replay(BinaryReader reader, int version)
        {
            SongName = reader.ReadString();
            ArtistName = reader.ReadString();
            CharterName = reader.ReadString();
            BandScore = reader.ReadInt32();
            BandStars = (StarAmount) reader.ReadByte();
            ReplayLength = reader.ReadDouble();
            Date = DateTime.FromBinary(reader.ReadInt64());
            SongChecksum = HashWrapper.Deserialize(reader);

            // TODO: Find a way to skip this step when analyzing replays
            ReplayPresetContainer = new ReplayPresetContainer(reader);

            int count = reader.ReadInt32();
            PlayerNames = new string[count];
            for (int i = 0; i < count; i++)
            {
                PlayerNames[i] = reader.ReadString();
            }

            Frames = new ReplayFrame[count];
            for (int i = 0; i < count; i++)
            {
                Frames[i] = new ReplayFrame(reader, version);
            }
        }

        public ReplayFile? Serialize(string path)
        {
            try
            {
                using var stream = File.OpenWrite(path);
                using var writer = new BinaryWriter(stream);

                var data = ConvertToMemory();
                var hash = HashWrapper.Hash(data);

                REPLAY_MAGIC_HEADER.Serialize(writer);
                writer.Write(REPLAY_VERSION);
                writer.Write(ENGINE_VERSION);
                hash.Serialize(writer);
                writer.Write(data);
                return new ReplayFile(this, REPLAY_VERSION, ENGINE_VERSION, hash);
            }
            catch (Exception e)
            {
                YargLogger.LogException(e, "Failed to save replay to file");
                return null;
            }
            
        }

        private ReadOnlySpan<byte> ConvertToMemory()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(SongName);
            writer.Write(ArtistName);
            writer.Write(CharterName);
            writer.Write(BandScore);
            writer.Write((byte) BandStars);
            writer.Write(ReplayLength);
            writer.Write(Date.ToBinary());

            SongChecksum.Serialize(writer);

            ReplayPresetContainer.Serialize(writer);

            writer.Write(PlayerCount);
            for (int i = 0; i < PlayerCount; i++)
            {
                writer.Write(PlayerNames[i]);
            }

            for (int i = 0; i < PlayerCount; i++)
            {
                Frames[i].Serialize(writer);
            }
            return new ReadOnlySpan<byte>(stream.GetBuffer(), 0, (int) stream.Length);
        }
    }
}
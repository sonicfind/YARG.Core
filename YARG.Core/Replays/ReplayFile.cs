using System;
using System.IO;
using System.Linq;
using YARG.Core.IO;
using YARG.Core.Logging;
using YARG.Core.Song;
using YARG.Core.Utility;

namespace YARG.Core.Replays
{
    public enum ReplayReadResult
    {
        Valid,
        NotAReplay,
        InvalidVersion,
        Corrupted,
    }

    public class ReplayFile
    {
        // Some versions may be invalidated (such as significant format changes)
        private static readonly int[] InvalidVersions =
        {
            0, 1, 2, 3
        };

        public static ReplayReadResult TryLoad(string path, out ReplayFile? file)
        {
            using var stream = File.OpenRead(path);
            using var reader = new BinaryReader(stream);

            try
            {
                if (!Replay.REPLAY_MAGIC_HEADER.Matches(reader.BaseStream))
                {
                    file = null;
                    return ReplayReadResult.NotAReplay;
                }

                int replayVersion = reader.ReadInt32();
                if (InvalidVersions.Contains(replayVersion) || replayVersion > Replay.REPLAY_VERSION)
                {
                    file = null;
                    return ReplayReadResult.InvalidVersion;
                }

                int engineVersion = reader.ReadInt32();
                var hash = HashWrapper.Deserialize(reader);
                var replay = new Replay(reader, replayVersion);
                file = new ReplayFile(replay, replayVersion, engineVersion, hash);
                return ReplayReadResult.Valid;
            }
            catch (Exception ex)
            {
                YargLogger.LogException(ex, "Failed to read replay file");
                file = null;
                return ReplayReadResult.Corrupted;
            }
        }

        public readonly Replay Replay;
        public readonly int ReplayVersion;
        public readonly int EngineVersion;
        public readonly HashWrapper Hash;

        public ReplayFile(Replay replay, int replayVersion, int engineVersion, HashWrapper hash)
        {
            Replay = replay;
            ReplayVersion = replayVersion;
            EngineVersion = engineVersion;
            Hash = hash;
        }
    }
}
using System.Diagnostics.CodeAnalysis;
using System.IO;
using YARG.Core.Game;
using YARG.Core.Utility;

namespace YARG.Core.Replays
{
    public struct ReplayPlayerInfo
    {
        public int         PlayerId;
        public int         ColorProfileId;
        public YargProfile Profile;

        public ReplayPlayerInfo(BinaryReader reader)
        {
            PlayerId = reader.ReadInt32();
            ColorProfileId = reader.ReadInt32();
            Profile = new YargProfile(reader);
        }

        public readonly void Serialize(BinaryWriter writer)
        {
            writer.Write(PlayerId);
            writer.Write(ColorProfileId);

            Profile.Serialize(writer);
        }
    }
}
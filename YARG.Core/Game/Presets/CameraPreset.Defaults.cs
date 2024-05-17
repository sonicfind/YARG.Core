using System;
using System.Collections.Generic;
using System.Linq;

namespace YARG.Core.Game
{
    public partial struct CameraPreset
    {
        public static readonly CameraPreset Default = new("Default")
        {
            FieldOfView = 55f,
            PositionY = 2.66f,
            PositionZ = 1.14f,
            Rotation = 24.12f,
            FadeLength = 1.25f,
            CurveFactor = 0.5f
        };

        public static readonly CameraPreset CircularDefault = new("Circular")
        {
            FieldOfView = 60f,
            PositionY = 2.39f,
            PositionZ = 1.54f,
            Rotation = 24.12f,
            FadeLength = 1.25f,
            CurveFactor = 0f,
        };

        public static readonly List<CameraPreset> Defaults = new()
        {
            Default,
            CircularDefault,
            new CameraPreset("High FOV")
            {
                FieldOfView = 60f,
                PositionY   = 2.66f,
                PositionZ   = 1.27f,
                Rotation    = 24.12f,
                FadeLength  = 1.25f,
                CurveFactor = 0.5f,
            },
            new CameraPreset("The Band 1")
            {
                FieldOfView = 47.84f,
                PositionY   = 2.32f,
                PositionZ   = 1.35f,
                Rotation    = 26f,
                FadeLength  = 1.25f,
                CurveFactor = 0.5f,
            },
            new CameraPreset("The Band 2")
            {
                FieldOfView = 44.97f,
                PositionY   = 2.72f,
                PositionZ   = 0.72f,
                Rotation    = 24.12f,
                FadeLength  = 1.25f,
                CurveFactor = 0.5f,
            },
            new CameraPreset("The Band 3")
            {
                FieldOfView = 57.29f,
                PositionY   = 2.22f,
                PositionZ   = 1.61f,
                Rotation    = 23.65f,
                FadeLength  = 1.25f,
                CurveFactor = 0.5f,
            },
            new CameraPreset("The Band 4")
            {
                FieldOfView = 62.16f,
                PositionY   = 2.56f,
                PositionZ   = 1.20f,
                Rotation    = 19.43f,
                FadeLength  = 1.25f,
                CurveFactor = 0.5f,
            },
            new CameraPreset("Hero 2")
            {
                FieldOfView = 58.15f,
                PositionY   = 1.82f,
                PositionZ   = 1.26f,
                Rotation    = 12.40f,
                FadeLength  = 1.5f,
                CurveFactor = 0f,
            },
            new CameraPreset("Hero 3")
            {
                FieldOfView = 52.71f,
                PositionY   = 2.17f,
                PositionZ   = 0.97f,
                Rotation    = 15.21f,
                FadeLength  = 1.5f,
                CurveFactor = 0f,
            },
            new CameraPreset("Hero Traveling the World")
            {
                FieldOfView  = 53.85f,
                PositionY    = 1.97f,
                PositionZ    = 1.31f,
                Rotation     = 16.62f,
                FadeLength   = 1.5f,
                CurveFactor  = 0f,
            },
            new CameraPreset("Hero Live")
            {
                FieldOfView = 62.16f,
                PositionY   = 2.40f,
                PositionZ   = 1.42f,
                Rotation    = 21.31f,
                FadeLength  = 1.25f,
                CurveFactor = 0f,
            },
            new CameraPreset("Clone")
            {
                FieldOfView = 55f,
                PositionY   = 2.07f,
                PositionZ   = 1.27f,
                Rotation    = 17.09f,
                FadeLength  = 1.5f,
                CurveFactor = 0f,
            }
        };

        private static readonly HashSet<Guid> _defaultIDs;

        static CameraPreset()
        {
            _defaultIDs = new();
            foreach (var def in Defaults)
            {
                _defaultIDs.Add(def.Id);
            }
        }

        public static bool IsDefault(in CameraPreset profile)
        {
            return _defaultIDs.Contains(profile.Id);
        }
    }
}
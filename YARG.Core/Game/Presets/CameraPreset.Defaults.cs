using System;
using System.Collections.Generic;
using System.Linq;

namespace YARG.Core.Game
{
    public partial struct CameraPreset
    {
        public static readonly PresetContainer<CameraPreset> Default = new("Default", new CameraPreset()
        {
            FieldOfView = 55f,
            PositionY = 2.66f,
            PositionZ = 1.14f,
            Rotation = 24.12f,
            FadeLength = 1.25f,
            CurveFactor = 0.5f
        });

        public static readonly PresetContainer<CameraPreset> CircularDefault = new("Circular", new CameraPreset()
        {
            FieldOfView = 60f,
            PositionY = 2.39f,
            PositionZ = 1.54f,
            Rotation = 24.12f,
            FadeLength = 1.25f,
            CurveFactor = 0f,
        });

        public static readonly PresetContainer<CameraPreset>[] Defaults =
        {
            Default,
            CircularDefault,
            new("High FOV", new CameraPreset()
            {
                FieldOfView = 60f,
                PositionY   = 2.66f,
                PositionZ   = 1.27f,
                Rotation    = 24.12f,
                FadeLength  = 1.25f,
                CurveFactor = 0.5f,
            }),
            new("The Band 1", new CameraPreset()
            {
                FieldOfView = 47.84f,
                PositionY   = 2.32f,
                PositionZ   = 1.35f,
                Rotation    = 26f,
                FadeLength  = 1.25f,
                CurveFactor = 0.5f,
            }),
            new("The Band 2", new CameraPreset()
            {
                FieldOfView = 44.97f,
                PositionY   = 2.72f,
                PositionZ   = 0.72f,
                Rotation    = 24.12f,
                FadeLength  = 1.25f,
                CurveFactor = 0.5f,
            }),
            new("The Band 3", new CameraPreset()
            {
                FieldOfView = 57.29f,
                PositionY   = 2.22f,
                PositionZ   = 1.61f,
                Rotation    = 23.65f,
                FadeLength  = 1.25f,
                CurveFactor = 0.5f,
            }),
            new("The Band 4", new CameraPreset()
            {
                FieldOfView = 62.16f,
                PositionY   = 2.56f,
                PositionZ   = 1.20f,
                Rotation    = 19.43f,
                FadeLength  = 1.25f,
                CurveFactor = 0.5f,
            }),
            new("Hero 2", new CameraPreset()
            {
                FieldOfView = 58.15f,
                PositionY   = 1.82f,
                PositionZ   = 1.26f,
                Rotation    = 12.40f,
                FadeLength  = 1.5f,
                CurveFactor = 0f,
            }),
            new("Hero 3", new CameraPreset()
            {
                FieldOfView = 52.71f,
                PositionY   = 2.17f,
                PositionZ   = 0.97f,
                Rotation    = 15.21f,
                FadeLength  = 1.5f,
                CurveFactor = 0f,
            }),
            new("Hero Traveling the World", new CameraPreset()
            {
                FieldOfView  = 53.85f,
                PositionY    = 1.97f,
                PositionZ    = 1.31f,
                Rotation     = 16.62f,
                FadeLength   = 1.5f,
                CurveFactor  = 0f,
            }),
            new("Hero Live", new CameraPreset()
            {
                FieldOfView = 62.16f,
                PositionY   = 2.40f,
                PositionZ   = 1.42f,
                Rotation    = 21.31f,
                FadeLength  = 1.25f,
                CurveFactor = 0f,
            }),
            new("Clone", new CameraPreset()
            {
                FieldOfView = 55f,
                PositionY   = 2.07f,
                PositionZ   = 1.27f,
                Rotation    = 17.09f,
                FadeLength  = 1.5f,
                CurveFactor = 0f,
            })
        };

        public static bool IsDefault(in PresetContainer<CameraPreset> camera)
        {
            foreach (var def in Defaults)
            {
                if (def.Id == camera.Id)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
using UnityEngine;

namespace Game.OutGame.Lobby
{
    public readonly struct ChapterBgTheme
    {
        public readonly Color TopColor;
        public readonly Color BottomColor;
        public readonly Color ParticleColor;
        public readonly float ParticleSpeed;
        public readonly int   ParticleCount;
        public readonly float ParticleSize;
        public readonly string PathResourceKey;
        public readonly float  PathScrollSpeed;
        public readonly Color  PathColor;
        public readonly float  PathWidth;

        public ChapterBgTheme(Color top, Color bottom, Color particle, float speed, int count, float size,
            string pathResourceKey, float pathScrollSpeed, Color pathColor, float pathWidth)
        {
            TopColor        = top;
            BottomColor     = bottom;
            ParticleColor   = particle;
            ParticleSpeed   = speed;
            ParticleCount   = count;
            ParticleSize    = size;
            PathResourceKey = pathResourceKey;
            PathScrollSpeed = pathScrollSpeed;
            PathColor       = pathColor;
            PathWidth       = pathWidth;
        }

        // bg_theme_id → visual theme config
        public static ChapterBgTheme Get(int themeId) => themeId switch
        {
            1 => new ChapterBgTheme(                               // Grassland
                top:      new Color(0.42f, 0.71f, 0.83f, 1f),     // #6BB5D4 sky blue
                bottom:   new Color(0.24f, 0.48f, 0.21f, 1f),     // #3D7A35 forest green
                particle: new Color(0.98f, 0.94f, 0.40f, 0.85f),  // firefly yellow
                speed: 35f, count: 14, size: 7f,
                pathResourceKey: "path_chapter_1",
                pathScrollSpeed: 0.15f,
                pathColor: new Color(0.65f, 0.95f, 0.55f, 1f),
                pathWidth: 64f),
            2 => new ChapterBgTheme(                               // Ocean
                top:      new Color(0.10f, 0.22f, 0.37f, 1f),     // #1A385E deep navy
                bottom:   new Color(0.18f, 0.55f, 0.48f, 1f),     // #2E8C7A teal
                particle: new Color(0.72f, 0.94f, 1.00f, 0.65f),  // cyan bubble
                speed: 45f, count: 16, size: 9f,
                pathResourceKey: "path_chapter_2",
                pathScrollSpeed: 0.3f,
                pathColor: new Color(0.4f, 0.88f, 0.98f, 1f),
                pathWidth: 64f),
            _ => new ChapterBgTheme(
                top:      Color.grey,
                bottom:   Color.black,
                particle: Color.white,
                speed: 50f, count: 10, size: 8f,
                pathResourceKey: "",
                pathScrollSpeed: 0.2f,
                pathColor: Color.white,
                pathWidth: 12f)
        };
    }
}

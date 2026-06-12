using UnityEngine;

namespace Game.OutGame.Lobby
{
    public enum ParticleDir { Upward, Downward, Horizontal }

    public readonly struct ChapterBgTheme
    {
        public readonly Color       TopColor;
        public readonly Color       BottomColor;
        public readonly Color       ParticleColor;
        public readonly float       ParticleSpeed;
        public readonly int         ParticleCount;
        public readonly float       ParticleSize;
        public readonly string      PathResourceKey;
        public readonly float       PathScrollSpeed;
        public readonly Color       PathColor;
        public readonly float       PathWidth;
        public readonly ParticleDir ParticleDir;

        public ChapterBgTheme(Color top, Color bottom, Color particle, float speed, int count, float size,
            string pathResourceKey, float pathScrollSpeed, Color pathColor, float pathWidth,
            ParticleDir particleDir = ParticleDir.Upward)
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
            ParticleDir     = particleDir;
        }

        public static ChapterBgTheme Get(int themeId) => themeId switch
        {
            1 => new ChapterBgTheme(                               // Grassland
                top:      new Color(0.42f, 0.71f, 0.83f, 1f),
                bottom:   new Color(0.24f, 0.48f, 0.21f, 1f),
                particle: new Color(0.98f, 0.94f, 0.40f, 0.85f),
                speed: 35f, count: 14, size: 7f,
                pathResourceKey: "path_chapter_1",
                pathScrollSpeed: 0.15f,
                pathColor: new Color(0.65f, 0.95f, 0.55f, 1f),
                pathWidth: 64f,
                particleDir: ParticleDir.Upward),
            2 => new ChapterBgTheme(                               // Ocean (shallow beach/sea)
                top:      new Color(0.96f, 0.87f, 0.70f, 1f),     // #F5DEB3 sandy beach
                bottom:   new Color(0.23f, 0.75f, 0.81f, 1f),     // #3BBFCE shallow turquoise
                particle: new Color(0.72f, 0.94f, 1.00f, 0.60f),
                speed: 40f, count: 14, size: 8f,
                pathResourceKey: "path_chapter_2",
                pathScrollSpeed: 0.3f,
                pathColor: new Color(0.4f, 0.88f, 0.98f, 1f),
                pathWidth: 64f,
                particleDir: ParticleDir.Upward),
            3 => new ChapterBgTheme(                               // Forest
                top:      new Color(0.29f, 0.50f, 0.19f, 1f),     // #4A8030 bright canopy
                bottom:   new Color(0.11f, 0.20f, 0.06f, 1f),     // #1B3210 dark mossy floor
                particle: new Color(0.80f, 0.78f, 0.22f, 0.80f),  // autumn leaf yellow (base; per-leaf colors vary)
                speed: 22f, count: 16, size: 10f,
                pathResourceKey: "path_chapter_3",
                pathScrollSpeed: 0.12f,
                pathColor: new Color(0.30f, 0.55f, 0.18f, 1f),
                pathWidth: 64f,
                particleDir: ParticleDir.Downward),
            4 => new ChapterBgTheme(                               // Desert
                top:      new Color(0.91f, 0.63f, 0.31f, 1f),     // #E8A050 hot hazy sky
                bottom:   new Color(0.78f, 0.48f, 0.17f, 1f),     // #C87A2A deep sand
                particle: new Color(0.91f, 0.75f, 0.44f, 0.40f),  // sandy dust
                speed: 55f, count: 22, size: 5f,
                pathResourceKey: "path_chapter_4",
                pathScrollSpeed: 0.20f,
                pathColor: new Color(0.92f, 0.72f, 0.35f, 1f),
                pathWidth: 64f,
                particleDir: ParticleDir.Horizontal),
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

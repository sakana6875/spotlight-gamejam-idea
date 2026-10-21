using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 给 Boss 预警和占位树共用的纯色方块。圆预警用同一张贴图裁成圆。
    /// </summary>
    public static class Demo2BossTelegraph
    {
        private const int TextureSize = 64;

        private static Sprite _square;
        private static Sprite _circle;

        /// <summary>
        /// 1 世界单位的白色方块。
        /// </summary>
        public static Sprite Square => _square != null ? _square : (_square = Create(false));

        /// <summary>
        /// 直径 1 世界单位的白色圆。
        /// </summary>
        public static Sprite Circle => _circle != null ? _circle : (_circle = Create(true));

        private static Sprite Create(bool circle)
        {
            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            texture.name = circle ? "Demo2BossCircle" : "Demo2BossSquare";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            float radius = (TextureSize - 1) * 0.5f;
            Color32[] pixels = new Color32[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    bool inside = true;
                    if (circle)
                    {
                        float dx = x - radius;
                        float dy = y - radius;
                        inside = dx * dx + dy * dy <= radius * radius;
                    }

                    pixels[y * TextureSize + x] = inside
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(255, 255, 255, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f),
                TextureSize);
        }
    }
}

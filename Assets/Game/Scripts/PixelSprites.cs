using UnityEngine;

namespace PhoneGame
{
    /// <summary>Tiny pixel-art sprites made in code (no image files needed): heart, broken heart, eye.</summary>
    public static class PixelSprites
    {
        static readonly string[] HeartRows =
        {
            " XX   XX ",
            "XXXX XXXX",
            "XXXXXXXXX",
            "XXXXXXXXX",
            " XXXXXXX ",
            "  XXXXX  ",
            "   XXX   ",
            "    X    ",
        };

        // Column of the crack in each row (top to bottom) - a zig-zag down the middle.
        static readonly int[] Crack = { 4, 5, 4, 3, 4, 5, 4, 4 };

        static readonly string[] EyeRows =
        {
            "     XXXXX     ",
            "   XX     XX   ",
            " XX   XXX   XX ",
            "X    XXXXX    X",
            " XX   XXX   XX ",
            "   XX     XX   ",
            "     XXXXX     ",
        };

        static Sprite heart, broken, eye;

        public static Sprite Heart { get { if (heart == null) heart = FromRows(HeartRows, false); return heart; } }
        public static Sprite BrokenHeart { get { if (broken == null) broken = FromRows(HeartRows, true); return broken; } }
        public static Sprite Eye { get { if (eye == null) eye = FromRows(EyeRows, false); return eye; } }

        static Sprite FromRows(string[] rows, bool cracked)
        {
            int h = rows.Length;
            int w = rows[0].Length + (cracked ? 2 : 0);
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color32 clear = new Color32(0, 0, 0, 0), white = new Color32(255, 255, 255, 255);
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) tex.SetPixel(x, y, clear);

            for (int r = 0; r < h; r++)
            {
                string row = rows[r];
                int py = h - 1 - r;   // texture rows go bottom-up
                for (int c = 0; c < row.Length; c++)
                {
                    if (row[c] != 'X') continue;
                    if (!cracked) { tex.SetPixel(c, py, white); continue; }
                    int crack = Crack[Mathf.Min(r, Crack.Length - 1)];
                    if (c == crack) continue;                 // the crack itself
                    int px = c < crack ? c : c + 2;           // halves pushed apart
                    tex.SetPixel(px, py, white);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 10f);
        }
    }
}

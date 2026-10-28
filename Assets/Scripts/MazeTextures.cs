using System.Collections.Generic;
using UnityEngine;

namespace WoodenMaze
{
    /// <summary>
    /// Procedural textures, so the project needs no image assets:
    /// a walnut wood grain, and the dark maze floor with the white route line,
    /// hole numbers and START / FINISH painted on it.
    /// </summary>
    public static class MazeTextures
    {
        public static Texture2D CreateWood(int size, int seed, Color light, Color dark)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "Procedural Wood",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4
            };
            var px = new Color32[size * size];
            float ox = (seed % 1000) * 1.37f + 10f;
            float oy = (seed % 777) * 0.91f + 20f;
            for (int y = 0; y < size; y++)
            {
                float v = y / (float)size;
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    // Grain runs along u. Warp the rings with low-frequency noise.
                    float warp = Mathf.PerlinNoise(ox + u * 2.5f, oy + v * 6f) * 5f;
                    float rings = Mathf.Sin((v * 14f + warp) * Mathf.PI * 2f) * 0.5f + 0.5f;
                    rings = rings * rings * rings;
                    float fibres = Mathf.PerlinNoise(ox + u * 30f, oy + v * 260f);
                    float t = Mathf.Clamp01(rings * 0.55f + fibres * 0.35f + Hash01(x, y) * 0.1f);
                    px[y * size + x] = Color.Lerp(light, dark, t);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return tex;
        }

        public static Texture2D CreateFloor(MazeLayout m, int pixelsPerCell, float holeRadiusCells, float lineWidthCells)
        {
            int ppc = pixelsPerCell;
            int w = m.Width * ppc, h = m.Height * ppc;
            var px = new Color32[w * h];
            var mask = new float[w * h]; // where the white paint goes

            // Base: dark grey hardboard with a little mottling.
            float o1 = (m.Seed % 997) * 0.731f + 3f, o2 = (m.Seed % 883) * 0.517f + 7f;
            var dark = new Color(0.16f, 0.15f, 0.14f);
            var light = new Color(0.29f, 0.275f, 0.26f);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float n = Mathf.PerlinNoise(o1 + x * 0.012f, o2 + y * 0.012f) * 0.55f +
                          Mathf.PerlinNoise(o2 + x * 0.09f, o1 + y * 0.09f) * 0.25f +
                          Hash01(x, y) * 0.2f;
                px[y * w + x] = Color.Lerp(dark, light, n);
            }

            // Worn dark rim around each hole.
            float rPx = holeRadiusCells * ppc;
            const float rim = 7f;
            foreach (var c in m.Holes)
            {
                float cx = (c.x + 0.5f) * ppc, cy = (c.y + 0.5f) * ppc;
                int r0 = Mathf.CeilToInt(rPx + rim);
                for (int y = (int)cy - r0; y <= (int)cy + r0; y++)
                for (int x = (int)cx - r0; x <= (int)cx + r0; x++)
                {
                    if (x < 0 || y < 0 || x >= w || y >= h) continue;
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (d >= rPx + rim) continue;
                    float k = d < rPx ? 0.1f : Mathf.Lerp(0.55f, 1f, (d - rPx) / rim);
                    px[y * w + x] = Scale(px[y * w + x], k);
                }
            }

            
            
            float lineR = lineWidthCells * ppc * 0.5f;
            var pts = m.SmoothPath(3);
            if(m.ShowPath){
                RenderPath(pts, mask, lineR, ppc, w, h);
            // The white route line.
                for (int i = 0; i < pts.Count - 1; i++){
                    Vector2 a = pts[i] * ppc, b = pts[i + 1] * ppc;
                    int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / 0.75f));
                    for (int s = 0; s <= steps; s++)
                        StampDisc(mask, w, h, Vector2.Lerp(a, b, s / (float)steps), lineR);
                }
            }
            // Start and finish rings.
            Vector2 start = new Vector2(m.Start.x + 0.5f, m.Start.y + 0.5f) * ppc;
            Vector2 goal = new Vector2(m.Goal.x + 0.5f, m.Goal.y + 0.5f) * ppc;
            Ring(mask, w, h, start, 0.36f * ppc, lineR * 0.8f);
            Ring(mask, w, h, goal, 0.38f * ppc, lineR * 0.8f);
            Ring(mask, w, h, goal, 0.26f * ppc, lineR * 0.8f);

            // Hole numbers in the top-left corner of each hole's cell.
            int margin = Mathf.RoundToInt(ppc * 0.085f);
            int digitScale = Mathf.Max(1, Mathf.RoundToInt(ppc / 53f));
            for (int i = 0; i < m.Holes.Count; i++)
            {
                var c = m.Holes[i];
                DrawText(mask, w, h, (i + 1).ToString(), c.x * ppc + margin, (c.y + 1) * ppc - 1 - margin, digitScale);
            }

            // START / FINISH, on the side of the cell the route does not leave through.
            int wordScale = Mathf.Max(1, Mathf.RoundToInt(ppc / 80f));
            if (m.Path.Count > 1)
            {
                LabelCell(mask, w, h, "START", m.Start, m.Path[1] - m.Start, ppc, margin, wordScale);
                LabelCell(mask, w, h, "FINISH", m.Goal, m.Path[m.Path.Count - 2] - m.Goal, ppc, margin, wordScale);
            }

            var paint = new Color(0.93f, 0.92f, 0.86f);
            for (int i = 0; i < px.Length; i++)
            {
                float a = mask[i];
                if (a <= 0f) continue;
                px[i] = Color.Lerp(px[i], paint, Mathf.Clamp01(a));
            }

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true)
            {
                name = "Maze Floor " + m.Seed,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 8
            };
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return tex;
        }

        private static void RenderPath(List<Vector2> points, float[] mask, float lineR, int ppc, int w, int h){
             for (int i = 0; i < points.Count - 1; i++){
                    Vector2 a = points[i] * ppc, b = points[i + 1] * ppc;
                    int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / 0.75f));
                    for (int s = 0; s <= steps; s++)
                        StampDisc(mask, w, h, Vector2.Lerp(a, b, s / (float)steps), lineR);
                }
        }
        static void LabelCell(float[] mask, int w, int h, string text, Vector2Int cell, Vector2Int exitDir,
            int ppc, int margin, int scale)
        {
            int tw = TextWidth(text, scale);
            int th = 7 * scale;
            int left = cell.x * ppc + (ppc - tw) / 2;
            // Route leaves downwards -> write in the top part of the cell, otherwise the bottom part.
            int top = exitDir.y < 0
                ? (cell.y + 1) * ppc - 1 - margin
                : cell.y * ppc + margin + th - 1;
            DrawText(mask, w, h, text, left, top, scale);
        }

        static void StampDisc(float[] mask, int w, int h, Vector2 c, float r)
        {
            int x0 = Mathf.FloorToInt(c.x - r - 1), x1 = Mathf.CeilToInt(c.x + r + 1);
            int y0 = Mathf.FloorToInt(c.y - r - 1), y1 = Mathf.CeilToInt(c.y + r + 1);
            for (int y = y0; y <= y1; y++)
            {
                if (y < 0 || y >= h) continue;
                for (int x = x0; x <= x1; x++)
                {
                    if (x < 0 || x >= w) continue;
                    float dx = x + 0.5f - c.x, dy = y + 0.5f - c.y;
                    float a = Mathf.Clamp01(r + 0.5f - Mathf.Sqrt(dx * dx + dy * dy));
                    int i = y * w + x;
                    if (a > mask[i]) mask[i] = a;
                }
            }
        }

        static void Ring(float[] mask, int w, int h, Vector2 c, float radius, float halfWidth)
        {
            int r0 = Mathf.CeilToInt(radius + halfWidth + 1);
            for (int y = (int)c.y - r0; y <= (int)c.y + r0; y++)
            {
                if (y < 0 || y >= h) continue;
                for (int x = (int)c.x - r0; x <= (int)c.x + r0; x++)
                {
                    if (x < 0 || x >= w) continue;
                    float dx = x + 0.5f - c.x, dy = y + 0.5f - c.y;
                    float d = Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - radius);
                    float a = Mathf.Clamp01(halfWidth + 0.5f - d);
                    int i = y * w + x;
                    if (a > mask[i]) mask[i] = a;
                }
            }
        }

        static int TextWidth(string text, int scale) => text.Length * 6 * scale - scale;

        /// <summary>Draws text with a 5x7 pixel font. (left, top) is the top-left pixel; rows go down.</summary>
        static void DrawText(float[] mask, int w, int h, string text, int left, int top, int scale)
        {
            for (int ci = 0; ci < text.Length; ci++)
            {
                if (!Glyphs.TryGetValue(text[ci], out var rows)) continue;
                int gx = left + ci * 6 * scale;
                for (int r = 0; r < 7; r++)
                for (int col = 0; col < 5; col++)
                {
                    if (rows[r][col] != '#') continue;
                    for (int sy = 0; sy < scale; sy++)
                    for (int sx = 0; sx < scale; sx++)
                    {
                        int x = gx + col * scale + sx;
                        int y = top - r * scale - sy;
                        if (x < 0 || y < 0 || x >= w || y >= h) continue;
                        mask[y * w + x] = 1f;
                    }
                }
            }
        }

        static Color32 Scale(Color32 c, float k) =>
            new Color32((byte)(c.r * k), (byte)(c.g * k), (byte)(c.b * k), c.a);

        static float Hash01(int x, int y)
        {
            unchecked
            {
                uint hsh = (uint)(x * 374761393 + y * 668265263);
                hsh = (hsh ^ (hsh >> 13)) * 1274126177u;
                hsh ^= hsh >> 16;
                return (hsh & 0xFFFFFF) / 16777215f;
            }
        }

        static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
        {
            { '0', new[] { ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###." } },
            { '1', new[] { "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###." } },
            { '2', new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####" } },
            { '3', new[] { "#####", "...#.", "..#..", "...#.", "....#", "#...#", ".###." } },
            { '4', new[] { "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#." } },
            { '5', new[] { "#####", "#....", "####.", "....#", "....#", "#...#", ".###." } },
            { '6', new[] { "..##.", ".#...", "#....", "####.", "#...#", "#...#", ".###." } },
            { '7', new[] { "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..." } },
            { '8', new[] { ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###." } },
            { '9', new[] { ".###.", "#...#", "#...#", ".####", "....#", "...#.", ".##.." } },
            { 'S', new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." } },
            { 'T', new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." } },
            { 'A', new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" } },
            { 'R', new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" } },
            { 'F', new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." } },
            { 'I', new[] { ".###.", "..#..", "..#..", "..#..", "..#..", "..#..", ".###." } },
            { 'N', new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#" } },
            { 'H', new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" } },
        };
    }
}

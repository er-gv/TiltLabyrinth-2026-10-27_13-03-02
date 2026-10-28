using System.Collections.Generic;
using UnityEngine;

namespace WoodenMaze
{
    /// <summary>
    /// Procedural layout of a classic tilting labyrinth: a grid of cells with thin
    /// walls on the cell borders, a winding route from START to FINISH, and holes
    /// placed next to that route. Same seed = same maze.
    /// Cell (0,0) is the near-left corner (south-west); y grows away from the player.
    /// </summary>
    public sealed class MazeLayout
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int Seed;

        /// <summary>Wall on the east side of cell [x,y]. Only x &lt; Width-1 is used.</summary>
        public readonly bool[,] WallEast;
        /// <summary>Wall on the north side of cell [x,y]. Only y &lt; Height-1 is used.</summary>
        public readonly bool[,] WallNorth;
        public readonly bool[,] IsHole;

        public Vector2Int Start;
        public Vector2Int Goal;

        /// <summary>The route the white line follows, from Start to Goal.</summary>
        public readonly List<Vector2Int> Path = new List<Vector2Int>();
        /// <summary>Holes in the order they are numbered on the board (index 0 = hole "1").</summary>
        public readonly List<Vector2Int> Holes = new List<Vector2Int>();

        static readonly Vector2Int[] Dirs =
        {
            new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0)
        };

        MazeLayout(int width, int height, int seed)
        {
            Width = width;
            Height = height;
            Seed = seed;
            WallEast = new bool[width, height];
            WallNorth = new bool[width, height];
            IsHole = new bool[width, height];
        }

        public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < Width && c.y < Height;

        /// <summary>True when the two neighbouring cells are not separated by a wall.</summary>
        public bool IsOpen(Vector2Int a, Vector2Int b)
        {
            if (b.y == a.y && b.x == a.x + 1) return !WallEast[a.x, a.y];
            if (b.y == a.y && b.x == a.x - 1) return !WallEast[b.x, b.y];
            if (b.x == a.x && b.y == a.y + 1) return !WallNorth[a.x, a.y];
            if (b.x == a.x && b.y == a.y - 1) return !WallNorth[b.x, b.y];
            return false;
        }

        void SetWall(Vector2Int a, Vector2Int b, bool wall)
        {
            if (b.y == a.y && b.x == a.x + 1) WallEast[a.x, a.y] = wall;
            else if (b.y == a.y && b.x == a.x - 1) WallEast[b.x, b.y] = wall;
            else if (b.x == a.x && b.y == a.y + 1) WallNorth[a.x, a.y] = wall;
            else if (b.x == a.x && b.y == a.y - 1) WallNorth[b.x, b.y] = wall;
        }

        /// <param name="wallRemoval">0..1, share of extra walls knocked out so the board looks like
        /// the real game (sparse sticks) instead of a dense paper maze.</param>
        /// <param name="holeDensity">0..1, share of off-route cells that become holes.</param>
        public static MazeLayout Generate(int width, int height, int seed, float wallRemoval, float holeDensity)
        {
            width = Mathf.Clamp(width, 4, 40);
            height = Mathf.Clamp(height, 4, 40);
            var m = new MazeLayout(width, height, seed);
            var rng = new System.Random(seed);

            m.Start = new Vector2Int(0, height - 1);
            m.Goal = new Vector2Int(width - 1, 0);

            // 1-2. Carve a few perfect mazes (recursive backtracker) and keep the one whose
            //      START-to-FINISH route is longest, so the white line winds across the board.
            bool[,] bestEast = null, bestNorth = null;
            int bestLength = -1;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                m.Carve(rng);
                m.FindPath();
                if (m.Path.Count <= bestLength) continue;
                bestLength = m.Path.Count;
                bestEast = (bool[,])m.WallEast.Clone();
                bestNorth = (bool[,])m.WallNorth.Clone();
            }
            System.Array.Copy(bestEast, m.WallEast, bestEast.Length);
            System.Array.Copy(bestNorth, m.WallNorth, bestNorth.Length);

            // 3. The route: far-left corner to near-right corner.
            m.FindPath();

            var onPath = new bool[width, height];
            foreach (var c in m.Path) onPath[c.x, c.y] = true;

            // 4. Open the board up. Never remove a wall between two route cells, which would
            //    create a shortcut that skips part of the white line.
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                if (x < width - 1 && m.WallEast[x, y] && !(onPath[x, y] && onPath[x + 1, y]) &&
                    rng.NextDouble() < wallRemoval)
                    m.WallEast[x, y] = false;
                if (y < height - 1 && m.WallNorth[x, y] && !(onPath[x, y] && onPath[x, y + 1]) &&
                    rng.NextDouble() < wallRemoval)
                    m.WallNorth[x, y] = false;
            }

            m.PlaceHoles(rng, onPath, holeDensity);
            return m;
        }

        void Carve(System.Random rng)
        {
            // Start with every border closed.
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                WallEast[x, y] = x < Width - 1;
                WallNorth[x, y] = y < Height - 1;
            }

            var visited = new bool[Width, Height];
            var stack = new Stack<Vector2Int>();
            var first = new Vector2Int(rng.Next(Width), rng.Next(Height));
            visited[first.x, first.y] = true;
            stack.Push(first);
            var options = new List<Vector2Int>(4);
            while (stack.Count > 0)
            {
                var c = stack.Peek();
                options.Clear();
                foreach (var d in Dirs)
                {
                    var n = c + d;
                    if (InBounds(n) && !visited[n.x, n.y]) options.Add(n);
                }
                if (options.Count == 0) { stack.Pop(); continue; }
                var next = options[rng.Next(options.Count)];
                SetWall(c, next, false);
                visited[next.x, next.y] = true;
                stack.Push(next);
            }
        }

        void FindPath()
        {
            Path.Clear();
            var prev = new Vector2Int?[Width, Height];
            var seen = new bool[Width, Height];
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(Start);
            seen[Start.x, Start.y] = true;
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                if (c == Goal) break;
                foreach (var d in Dirs)
                {
                    var n = c + d;
                    if (!InBounds(n) || seen[n.x, n.y] || !IsOpen(c, n)) continue;
                    seen[n.x, n.y] = true;
                    prev[n.x, n.y] = c;
                    queue.Enqueue(n);
                }
            }

            var cur = Goal;
            Path.Add(cur);
            while (cur != Start)
            {
                var p = prev[cur.x, cur.y];
                if (!p.HasValue) break; // cannot happen in a perfect maze
                cur = p.Value;
                Path.Add(cur);
            }
            Path.Reverse();
        }

        void PlaceHoles(System.Random rng, bool[,] onPath, float density)
        {
            var candidates = new List<KeyValuePair<Vector2Int, double>>();
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                var c = new Vector2Int(x, y);
                if (onPath[x, y]) continue;
                if (Chebyshev(c, Start) <= 1 || Chebyshev(c, Goal) <= 1) continue;

                // Holes right beside the route (with no wall in between) are the real traps.
                double score = 0;
                foreach (var d in Dirs)
                {
                    var n = c + d;
                    if (!InBounds(n) || !onPath[n.x, n.y]) continue;
                    score += IsOpen(c, n) ? 3.0 : 1.0;
                }
                candidates.Add(new KeyValuePair<Vector2Int, double>(c, score + rng.NextDouble() * 2.5));
            }

            candidates.Sort((a, b) => b.Value.CompareTo(a.Value));
            int count = Mathf.RoundToInt(Mathf.Clamp01(density) * candidates.Count);

            var chosen = new List<Vector2Int>();
            for (int i = 0; i < count; i++)
            {
                var c = candidates[i].Key;
                IsHole[c.x, c.y] = true;
                chosen.Add(c);
            }

            // Number holes in the order you meet them along the route, like the real board.
            var order = new List<KeyValuePair<Vector2Int, float>>();
            foreach (var h in chosen)
            {
                int best = 0;
                float bestDist = float.MaxValue;
                for (int i = 0; i < Path.Count; i++)
                {
                    float dist = (Path[i] - h).sqrMagnitude;
                    if (dist < bestDist) { bestDist = dist; best = i; }
                }
                order.Add(new KeyValuePair<Vector2Int, float>(h, best + bestDist * 0.01f));
            }
            order.Sort((a, b) => a.Value.CompareTo(b.Value));
            Holes.Clear();
            foreach (var o in order) Holes.Add(o.Key);
        }

        static int Chebyshev(Vector2Int a, Vector2Int b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

        /// <summary>The route as a smooth curve, in cell units (cell centre = x + 0.5).</summary>
        public List<Vector2> SmoothPath(int iterations)
        {
            var pts = new List<Vector2>(Path.Count);
            foreach (var c in Path) pts.Add(new Vector2(c.x + 0.5f, c.y + 0.5f));
            for (int it = 0; it < iterations && pts.Count >= 3; it++)
            {
                var next = new List<Vector2>(pts.Count * 2) { pts[0] };
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    next.Add(Vector2.Lerp(pts[i], pts[i + 1], 0.25f));
                    next.Add(Vector2.Lerp(pts[i], pts[i + 1], 0.75f));
                }
                next.Add(pts[pts.Count - 1]);
                pts = next;
            }
            return pts;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace WoodenMaze
{
    /// <summary>
    /// Builds the maze floor as one mesh with real round holes cut through it,
    /// so the marble physically drops through them.
    /// Sub-mesh 0 = painted top surface, sub-mesh 1 = hole insides, underside and edges.
    /// Local space: top surface at y = 0, board centred on the origin.
    /// </summary>
    public static class MazeMeshes
    {
        public static Mesh BuildFloor(MazeLayout m, float cell, float thickness, float holeRadius, int segments = 32)
        {
            segments = Mathf.Max(8, segments / 4 * 4); // multiple of 4 so the tile corners are hit exactly
            float boardW = m.Width * cell, boardH = m.Height * cell;
            float x0 = -boardW * 0.5f, z0 = -boardH * 0.5f;

            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var top = new List<int>();
            var other = new List<int>();

            void Quad(List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n)
            {
                int i = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
                for (int k = 0; k < 4; k++) normals.Add(n);
                uvs.Add(UV(a)); uvs.Add(UV(b)); uvs.Add(UV(c)); uvs.Add(UV(d));
                // Unity treats clockwise triangles as front-facing; pick the winding that faces n.
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), n) >= 0f)
                {
                    tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
                    tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
                }
                else
                {
                    tris.Add(i); tris.Add(i + 2); tris.Add(i + 1);
                    tris.Add(i); tris.Add(i + 3); tris.Add(i + 2);
                }
            }

            Vector2 UV(Vector3 p) => new Vector2((p.x - x0) / boardW, (p.z - z0) / boardH);

            float half = cell * 0.5f;
            var up = Vector3.up;
            var down = Vector3.down;
            var below = new Vector3(0f, -thickness, 0f);

            for (int cy = 0; cy < m.Height; cy++)
            for (int cx = 0; cx < m.Width; cx++)
            {
                var c = new Vector3(x0 + (cx + 0.5f) * cell, 0f, z0 + (cy + 0.5f) * cell);
                if (!m.IsHole[cx, cy])
                {
                    Vector3 a = c + new Vector3(-half, 0, -half), b = c + new Vector3(-half, 0, half),
                        d = c + new Vector3(half, 0, half), e = c + new Vector3(half, 0, -half);
                    Quad(top, a, b, d, e, up);
                    Quad(other, a + below, b + below, d + below, e + below, down);
                    continue;
                }

                // Square tile with a round hole: a ring of quads from the circle out to the square edge.
                for (int i = 0; i < segments; i++)
                {
                    float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                    Vector3 d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0));
                    Vector3 d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                    Vector3 p0 = c + d0 * holeRadius, p1 = c + d1 * holeRadius;
                    Vector3 q0 = c + d0 * (half / Mathf.Max(Mathf.Abs(d0.x), Mathf.Abs(d0.z)));
                    Vector3 q1 = c + d1 * (half / Mathf.Max(Mathf.Abs(d1.x), Mathf.Abs(d1.z)));

                    Quad(top, p0, q0, q1, p1, up);
                    Quad(other, p0 + below, q0 + below, q1 + below, p1 + below, down);

                    // Inside wall of the hole, facing the hole's axis.
                    Vector3 inward = -(d0 + d1).normalized;
                    Quad(other, p0, p1, p1 + below, p0 + below, inward);
                }
            }

            // Outer edges of the board.
            Vector3 sw = new Vector3(x0, 0, z0), nw = new Vector3(x0, 0, z0 + boardH);
            Vector3 ne = new Vector3(x0 + boardW, 0, z0 + boardH), se = new Vector3(x0 + boardW, 0, z0);
            Quad(other, nw, ne, ne + below, nw + below, Vector3.forward);
            Quad(other, se, sw, sw + below, se + below, Vector3.back);
            Quad(other, ne, se, se + below, ne + below, Vector3.right);
            Quad(other, sw, nw, nw + below, sw + below, Vector3.left);

            var mesh = new Mesh { name = "Maze Floor" };
            if (verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(top, 0);
            mesh.SetTriangles(other, 1);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}

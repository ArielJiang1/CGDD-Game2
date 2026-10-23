using System.Collections.Generic;
using UnityEngine;

namespace Blockout
{
    /// <summary>
    /// Builds simple low-poly meshes out of boxes.
    /// UVs are written in METRES (box-projected), so the 1 m grid texture keeps a constant
    /// world scale on every module no matter its size - the book's "world-aligned grid texture"
    /// advice, without needing a triplanar shader (works in Built-in, URP and HDRP).
    /// </summary>
    public class BlockoutMeshBuilder
    {
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector3> norms = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<int> tris = new List<int>();

        /// <summary>Every box added, in local space - used to create matching BoxColliders.</summary>
        public readonly List<Bounds> Boxes = new List<Bounds>();

        public BlockoutMeshBuilder Box(float ax, float ay, float az, float bx, float by, float bz)
        {
            float x0 = Mathf.Min(ax, bx), x1 = Mathf.Max(ax, bx);
            float y0 = Mathf.Min(ay, by), y1 = Mathf.Max(ay, by);
            float z0 = Mathf.Min(az, bz), z1 = Mathf.Max(az, bz);

            Bounds b = new Bounds();
            b.SetMinMax(new Vector3(x0, y0, z0), new Vector3(x1, y1, z1));
            Boxes.Add(b);

            // Each quad is listed clockwise as seen from outside (Unity front face).
            Quad(new Vector3(x0, y1, z1), new Vector3(x1, y1, z1), new Vector3(x1, y1, z0), new Vector3(x0, y1, z0), Vector3.up, 0, 2);
            Quad(new Vector3(x0, y0, z0), new Vector3(x1, y0, z0), new Vector3(x1, y0, z1), new Vector3(x0, y0, z1), Vector3.down, 0, 2);
            Quad(new Vector3(x0, y1, z0), new Vector3(x1, y1, z0), new Vector3(x1, y0, z0), new Vector3(x0, y0, z0), Vector3.back, 0, 1);
            Quad(new Vector3(x0, y0, z1), new Vector3(x1, y0, z1), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1), Vector3.forward, 0, 1);
            Quad(new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), new Vector3(x1, y0, z1), new Vector3(x1, y0, z0), Vector3.right, 2, 1);
            Quad(new Vector3(x0, y0, z0), new Vector3(x0, y0, z1), new Vector3(x0, y1, z1), new Vector3(x0, y1, z0), Vector3.left, 2, 1);
            return this;
        }

        /// <summary>Flat pie-slice lying on the XZ plane, pointing along +Z. Used for vision cones.</summary>
        public BlockoutMeshBuilder Fan(float angleDeg, float range, int segments, float y)
        {
            int start = verts.Count;
            verts.Add(new Vector3(0f, y, 0f)); norms.Add(Vector3.up); uvs.Add(Vector2.zero);
            float half = angleDeg * 0.5f;
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.Deg2Rad * Mathf.Lerp(-half, half, i / (float)segments);
                Vector3 p = new Vector3(Mathf.Sin(a) * range, y, Mathf.Cos(a) * range);
                verts.Add(p); norms.Add(Vector3.up); uvs.Add(new Vector2(p.x, p.z));
            }
            for (int i = 0; i < segments; i++)
            {
                tris.Add(start); tris.Add(start + 1 + i); tris.Add(start + 2 + i);
            }
            return this;
        }

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, int u, int v)
        {
            int i = verts.Count;
            Vector3[] q = { a, b, c, d };
            for (int k = 0; k < 4; k++)
            {
                verts.Add(q[k]);
                norms.Add(normal);
                uvs.Add(new Vector2(q[k][u], q[k][v]));
            }
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }

        public Mesh ToMesh(string name)
        {
            Mesh m = new Mesh();
            m.name = name;
            m.SetVertices(verts);
            m.SetNormals(norms);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }
    }
}

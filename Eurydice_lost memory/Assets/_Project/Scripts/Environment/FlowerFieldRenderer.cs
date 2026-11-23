using System.Collections.Generic;
using UnityEngine;

namespace Eurydice.Gameplay.Environment
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class FlowerFieldRenderer : MonoBehaviour
    {
        [SerializeField] private Material material;
        [SerializeField] private Vector2 fieldSize = new(18f, 18f);
        [SerializeField, Min(1)] private int flowerCount = 360;
        [SerializeField] private int seed = 1337;

        private Mesh _mesh;

        public void Configure(Material flowerMaterial, Vector2 size, int count, int randomSeed)
        {
            material = flowerMaterial;
            fieldSize = size;
            flowerCount = count;
            seed = randomSeed;
            Rebuild();
        }

        private void OnEnable() => Rebuild();

        private void OnValidate()
        {
            if (isActiveAndEnabled) Rebuild();
        }

        private void Rebuild()
        {
            MeshFilter filter = GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (filter == null || meshRenderer == null) return;

            if (_mesh != null)
            {
                if (Application.isPlaying) Destroy(_mesh); else DestroyImmediate(_mesh);
            }

            _mesh = BuildMesh();
            _mesh.name = "Procedural Flower Field";
            filter.sharedMesh = _mesh;
            meshRenderer.sharedMaterial = material;
        }

        private Mesh BuildMesh()
        {
            var vertices = new List<Vector3>(flowerCount * 96);
            var colors = new List<Color>(flowerCount * 96);
            var uvs = new List<Vector2>(flowerCount * 96);
            var triangles = new List<int>(flowerCount * 180);
            var random = new System.Random(seed);
            Color[] blossoms =
            {
                new(1f, 0.32f, 0.52f, 1f),
                new(1f, 0.48f, 0.62f, 1f),
                new(1f, 0.77f, 0.23f, 1f),
                new(1f, 0.90f, 0.38f, 1f)
            };

            for (int i = 0; i < flowerCount; i++)
            {
                float x = ((float)random.NextDouble() - 0.5f) * fieldSize.x;
                float z = ((float)random.NextDouble() - 0.5f) * fieldSize.y;
                if (Mathf.Abs(x) < 1.35f && z > -7f) x += Mathf.Sign(x == 0f ? 1f : x) * 1.7f;

                float height = Mathf.Lerp(0.24f, 0.56f, (float)random.NextDouble());
                float yaw = (float)random.NextDouble() * 180f;
                float scale = Mathf.Lerp(0.7f, 1.2f, (float)random.NextDouble());
                Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
                Vector3 root = new(x, 0f, z);
                Color stem = new(0.2f, 0.58f, 0.22f, 1f);
                Color blossom = blossoms[random.Next(blossoms.Length)];

                AddVerticalCard(vertices, colors, uvs, triangles, root, rotation, 0.022f * scale, height, stem);
                Vector3 flowerCenter = root + Vector3.up * height;
                AddFlowerHead(vertices, colors, uvs, triangles, flowerCenter, rotation, 0.06f * scale, blossom);
            }

            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddVerticalCard(List<Vector3> vertices, List<Color> colors, List<Vector2> uvs,
            List<int> triangles, Vector3 root, Quaternion rotation, float width, float height, Color color)
        {
            int start = vertices.Count;
            Vector3 right = rotation * Vector3.right * (width * 0.5f);
            vertices.Add(root - right);
            vertices.Add(root + right);
            vertices.Add(root + right + Vector3.up * height);
            vertices.Add(root - right + Vector3.up * height);
            colors.Add(new Color(color.r, color.g, color.b, 0f));
            colors.Add(new Color(color.r, color.g, color.b, 0f));
            colors.Add(new Color(color.r, color.g, color.b, 1f));
            colors.Add(new Color(color.r, color.g, color.b, 1f));
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(0f, 1f));
            triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
            triangles.Add(start); triangles.Add(start + 3); triangles.Add(start + 2);
        }

        private static void AddFlowerHead(List<Vector3> vertices, List<Color> colors, List<Vector2> uvs,
            List<int> triangles, Vector3 center, Quaternion rotation, float petalRadius, Color petalColor)
        {
            Vector3 right = rotation * Vector3.right;
            Vector3 forward = rotation * Vector3.forward;
            for (int petal = 0; petal < 5; petal++)
            {
                float angle = petal * Mathf.PI * 2f / 5f + Mathf.PI * 0.5f;
                Vector3 offset = (right * Mathf.Cos(angle) + forward * Mathf.Sin(angle)) * (petalRadius * 0.72f);
                AddOctahedron(vertices, colors, uvs, triangles, center + offset,
                    right, Vector3.up, forward, petalRadius, petalColor);
            }

            AddOctahedron(vertices, colors, uvs, triangles, center + Vector3.up * (petalRadius * 0.45f),
                right, Vector3.up, forward, petalRadius * 0.48f,
                new Color(1f, 0.82f, 0.18f, 1f));
        }

        private static void AddOctahedron(List<Vector3> vertices, List<Color> colors, List<Vector2> uvs,
            List<int> triangles, Vector3 center, Vector3 right, Vector3 up, Vector3 forward,
            float radius, Color color)
        {
            int start = vertices.Count;
            vertices.Add(center + up * radius);
            vertices.Add(center - up * radius * 0.72f);
            vertices.Add(center + right * radius);
            vertices.Add(center + forward * radius);
            vertices.Add(center - right * radius);
            vertices.Add(center - forward * radius);
            for (int i = 0; i < 6; i++) colors.Add(new Color(color.r, color.g, color.b, 1f));
            uvs.Add(new Vector2(0.5f, 1f));
            uvs.Add(new Vector2(0.5f, 0f));
            uvs.Add(new Vector2(1f, 0.5f));
            uvs.Add(new Vector2(0.75f, 0.5f));
            uvs.Add(new Vector2(0f, 0.5f));
            uvs.Add(new Vector2(0.25f, 0.5f));

            int[] ring = { 2, 3, 4, 5 };
            for (int i = 0; i < 4; i++)
            {
                int next = (i + 1) % 4;
                triangles.Add(start); triangles.Add(start + ring[next]); triangles.Add(start + ring[i]);
                triangles.Add(start + 1); triangles.Add(start + ring[i]); triangles.Add(start + ring[next]);
            }
        }
    }
}

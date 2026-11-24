using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eurydice.Gameplay.Environment
{
    [Flags]
    public enum MossFaces { None = 0, Top = 1, Bottom = 2, Left = 4, Right = 8, Front = 16, Back = 32 }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class MossGroundRenderer : MonoBehaviour
    {
        private const string PreviewName = "Moss Surface Preview";

        [Header("Target Faces")]
        [SerializeField] private MossFaces faces = MossFaces.Top;
        [SerializeField, Min(0f)] private float surfaceOffset = 0.025f;
        [Header("Short Grass")]
        [SerializeField, Range(0f, 40f)] private float bladesPerSquareMeter = 7f;
        [SerializeField, Range(0, 20000)] private int maximumBladeCount = 3000;
        [SerializeField, Range(0.01f, 0.25f)] private float bladeHeight = 0.075f;
        [SerializeField] private int seed = 7281;
        [Header("Appearance")]
        [SerializeField] private Color mossDark = new(0.055f, 0.22f, 0.035f, 1f);
        [SerializeField] private Color mossLight = new(0.34f, 0.58f, 0.12f, 1f);
        [SerializeField, Range(0f, 0.1f)] private float windStrength = 0.025f;

        private GameObject _preview;
        private Mesh _mesh;
        private Material _material;

        private readonly struct FaceSurface
        {
            public readonly Vector3 Center, Normal, Tangent, Bitangent;
            public readonly float HalfWidth, HalfHeight;

            public FaceSurface(Vector3 center, Vector3 normal, Vector3 tangent, float halfWidth, float halfHeight)
            {
                Center = center;
                Normal = normal;
                Tangent = tangent;
                Bitangent = Vector3.Cross(normal, tangent).normalized;
                HalfWidth = halfWidth;
                HalfHeight = halfHeight;
            }
        }

        private void OnEnable() => Rebuild();
        private void OnValidate() { if (isActiveAndEnabled) Rebuild(); }

        [ContextMenu("Rebuild Moss On Selected Faces")]
        public void Rebuild()
        {
            MeshFilter source = GetComponent<MeshFilter>();
            if (source == null || source.sharedMesh == null) { ClearPreview(); return; }
            Shader shader = Shader.Find("Eurydice/URP Moss Ground Sway");
            if (shader == null) return;

            Bounds sourceBounds = source.sharedMesh.bounds;
            EnsurePreview();
            EnsureMaterial(shader);
            DestroyObject(_mesh);
            _mesh = BuildMesh(sourceBounds);
            _mesh.name = "Object Moss Surface Mesh";
            _mesh.hideFlags = HideFlags.DontSave;
            _preview.GetComponent<MeshFilter>().sharedMesh = _mesh;
            MeshRenderer renderer = _preview.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        private void EnsurePreview()
        {
            if (_preview != null) return;
            Transform existing = transform.Find(PreviewName);
            if (existing != null) _preview = existing.gameObject;
            else
            {
                _preview = new GameObject(PreviewName);
                _preview.transform.SetParent(transform, false);
            }
            _preview.hideFlags = HideFlags.DontSave;
            if (_preview.GetComponent<MeshFilter>() == null) _preview.AddComponent<MeshFilter>();
            if (_preview.GetComponent<MeshRenderer>() == null) _preview.AddComponent<MeshRenderer>();
        }

        private void EnsureMaterial(Shader shader)
        {
            if (_material == null)
                _material = new Material(shader) { name = "Object Moss Preview", hideFlags = HideFlags.DontSave };
            _material.SetColor("_MossDark", mossDark);
            _material.SetColor("_MossLight", mossLight);
            _material.SetFloat("_NoiseScale", 0.65f);
            _material.SetFloat("_WindStrength", windStrength);
            _material.SetFloat("_WindSpeed", 1.1f);
        }

        private Mesh BuildMesh(Bounds bounds)
        {
            List<FaceSurface> selected = CollectSelectedFaces(bounds);
            var vertices = new List<Vector3>(); var colors = new List<Color>();
            var uvs = new List<Vector2>(); var triangles = new List<int>();
            var random = new System.Random(seed); int remainingBlades = maximumBladeCount;

            foreach (FaceSurface surface in selected)
            {
                AddSurfaceQuad(vertices, colors, uvs, triangles, surface);
                float normalScale = DirectionScale(surface.Normal);
                float tangentScale = DirectionScale(surface.Tangent);
                float bitangentScale = DirectionScale(surface.Bitangent);
                float area = surface.HalfWidth * 2f * tangentScale
                    * surface.HalfHeight * 2f * bitangentScale;
                int bladeCount = Mathf.Min(remainingBlades, Mathf.RoundToInt(area * bladesPerSquareMeter));
                remainingBlades -= bladeCount;
                for (int i = 0; i < bladeCount; i++)
                {
                    float x = Mathf.Lerp(-surface.HalfWidth, surface.HalfWidth, (float)random.NextDouble());
                    float y = Mathf.Lerp(-surface.HalfHeight, surface.HalfHeight, (float)random.NextDouble());
                    Vector3 root = surface.Center + surface.Normal * ((surfaceOffset + 0.002f) / normalScale)
                        + surface.Tangent * x + surface.Bitangent * y;
                    float worldHeight = bladeHeight * Mathf.Lerp(0.55f, 1.35f, (float)random.NextDouble());
                    float angle = (float)random.NextDouble() * Mathf.PI;
                    Vector3 side = (surface.Tangent * Mathf.Cos(angle) + surface.Bitangent * Mathf.Sin(angle)).normalized;
                    float height = worldHeight / normalScale;
                    float worldWidth = worldHeight * Mathf.Lerp(0.07f, 0.13f, (float)random.NextDouble());
                    float width = worldWidth / DirectionScale(side);
                    AddBlade(vertices, colors, uvs, triangles, root, side, surface.Normal, width, height);
                }
                if (remainingBlades <= 0) break;
            }

            Mesh mesh = new() { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private List<FaceSurface> CollectSelectedFaces(Bounds bounds)
        {
            var result = new List<FaceSurface>(); Vector3 c = bounds.center; Vector3 e = bounds.extents;
            if (faces.HasFlag(MossFaces.Top)) result.Add(new FaceSurface(c + Vector3.up * e.y, Vector3.up, Vector3.right, e.x, e.z));
            if (faces.HasFlag(MossFaces.Bottom)) result.Add(new FaceSurface(c + Vector3.down * e.y, Vector3.down, Vector3.right, e.x, e.z));
            if (faces.HasFlag(MossFaces.Left)) result.Add(new FaceSurface(c + Vector3.left * e.x, Vector3.left, Vector3.up, e.y, e.z));
            if (faces.HasFlag(MossFaces.Right)) result.Add(new FaceSurface(c + Vector3.right * e.x, Vector3.right, Vector3.up, e.y, e.z));
            if (faces.HasFlag(MossFaces.Front)) result.Add(new FaceSurface(c + Vector3.forward * e.z, Vector3.forward, Vector3.right, e.x, e.y));
            if (faces.HasFlag(MossFaces.Back)) result.Add(new FaceSurface(c + Vector3.back * e.z, Vector3.back, Vector3.right, e.x, e.y));
            return result;
        }

        private void AddSurfaceQuad(List<Vector3> vertices, List<Color> colors, List<Vector2> uvs,
            List<int> triangles, FaceSurface surface)
        {
            int start = vertices.Count;
            Vector3 center = surface.Center + surface.Normal * (surfaceOffset / DirectionScale(surface.Normal));
            Vector3 u = surface.Tangent * surface.HalfWidth; Vector3 v = surface.Bitangent * surface.HalfHeight;
            vertices.Add(center - u - v); vertices.Add(center + u - v);
            vertices.Add(center + u + v); vertices.Add(center - u + v);
            Color moss = new(mossDark.r, mossDark.g, mossDark.b, 0f);
            for (int i = 0; i < 4; i++) colors.Add(moss);
            uvs.Add(Vector2.zero); uvs.Add(Vector2.right); uvs.Add(Vector2.one); uvs.Add(Vector2.up);
            triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
            triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
        }

        private float DirectionScale(Vector3 localDirection)
        {
            return Mathf.Max(transform.TransformVector(localDirection.normalized).magnitude, 0.0001f);
        }

        private static void AddBlade(List<Vector3> vertices, List<Color> colors, List<Vector2> uvs,
            List<int> triangles, Vector3 root, Vector3 sideDirection, Vector3 normal, float width, float height)
        {
            int start = vertices.Count; Vector3 side = sideDirection * width; Vector3 tip = normal * height;
            vertices.Add(root - side); vertices.Add(root + side);
            vertices.Add(root + side * 0.2f + tip); vertices.Add(root - side * 0.2f + tip);
            Color baseColor = new(0.18f, 0.40f, 0.08f, 0f); Color tipColor = new(0.48f, 0.72f, 0.22f, 1f);
            colors.Add(baseColor); colors.Add(baseColor); colors.Add(tipColor); colors.Add(tipColor);
            uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(1f, 0f)); uvs.Add(new Vector2(1f, 1f)); uvs.Add(new Vector2(0f, 1f));
            triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
            triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
        }

        private void ClearPreview()
        {
            DestroyObject(_mesh); _mesh = null;
            if (_preview != null) DestroyObject(_preview);
            _preview = null;
        }

        private void OnDestroy() { ClearPreview(); DestroyObject(_material); _material = null; }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target); else DestroyImmediate(target);
        }
    }
}

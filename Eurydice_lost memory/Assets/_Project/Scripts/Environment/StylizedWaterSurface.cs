using System.Collections.Generic;
using Eurydice.Gameplay.Player;
using UnityEngine;

namespace Eurydice.Gameplay.Environment
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class StylizedWaterSurface : MonoBehaviour
    {
        private const int RippleCount = 4;

        [Header("Color")]
        [SerializeField] private Color deepColor = new(0.015f, 0.10f, 0.28f, 0.88f);
        [SerializeField] private Color shallowColor = new(0.02f, 0.58f, 0.78f, 0.72f);
        [SerializeField] private Color horizonColor = new(0.12f, 0.38f, 0.68f, 1f);
        [SerializeField] private Color foamColor = new(0.82f, 0.96f, 1f, 1f);
        [SerializeField, Range(0.05f, 1f)] private float transparency = 0.82f;

        [Header("Depth And Foam")]
        [SerializeField, Range(0.1f, 12f)] private float depthFade = 3.2f;
        [SerializeField, Range(0.02f, 2f)] private float foamWidth = 0.32f;
        [SerializeField, Range(0f, 1f)] private float crestFoam = 0.24f;

        [Header("Surface Waves")]
        [SerializeField, Range(0f, 1f)] private float waveHeight = 0.075f;
        [SerializeField, Range(0.05f, 3f)] private float waveScale = 0.68f;
        [SerializeField, Range(0f, 4f)] private float waveSpeed = 0.58f;
        [SerializeField, Range(0f, 2f)] private float normalStrength = 0.82f;
        [SerializeField, Range(8f, 160f)] private float sparkleSize = 72f;
        [SerializeField, Range(0f, 4f)] private float sparkleStrength = 1.35f;
        [SerializeField, Range(0f, 1f)] private float reflectionStrength = 0.38f;
        [SerializeField, Range(0f, 0.08f)] private float refractionStrength = 0.022f;

        [Header("Circular Ripples")]
        [SerializeField, Range(0.1f, 3f)] private float interactionDepth = 0.65f;
        [SerializeField, Range(0.3f, 2f)] private float rippleSpacing = 0.75f;
        [SerializeField, Range(0.5f, 4f)] private float rippleLifetime = 1.65f;
        [SerializeField, Range(0.5f, 8f)] private float rippleRadius = 2.8f;
        [SerializeField, Range(0.02f, 0.5f)] private float rippleWidth = 0.14f;
        [SerializeField, Range(0f, 0.2f)] private float rippleHeight = 0.045f;

        [Header("Mesh Quality")]
        [SerializeField, Range(8, 128)] private int subdivisions = 64;

        private struct RippleState
        {
            public Vector2 Origin;
            public float Age;
            public float Strength;
            public float Angle;
            public float Aspect;
            public float NoiseSeed;
            public bool Active;
        }

        private readonly RippleState[] _ripples = new RippleState[RippleCount];
        private readonly Vector4[] _rippleShaderData = new Vector4[RippleCount];
        private readonly Vector4[] _rippleShapeData = new Vector4[RippleCount];
        private Material _material;
        private Mesh _waterMesh;
        private EurydicePlayerController _player;
        private Vector3 _lastPlayerPosition;
        private Vector2 _lastRipplePosition;
        private int _nextRipple;
        private bool _hasPlayerPosition;
        private bool _wasInWater;

        private void OnEnable()
        {
            Apply();
            FindPlayer();
        }

        private void OnValidate()
        {
            if (isActiveAndEnabled) Apply();
        }

        private void Update()
        {
            if (!Application.isPlaying || _material == null) return;
            if (_player == null) FindPlayer();
            if (_player == null) { AdvanceRipples(); return; }

            Vector3 position = _player.transform.position;
            if (!_hasPlayerPosition)
            {
                _lastPlayerPosition = position;
                _lastRipplePosition = new Vector2(position.x, position.z);
                _hasPlayerPosition = true;
            }

            Vector3 velocity = (position - _lastPlayerPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
            velocity.y = 0f;
            if (velocity.magnitude > 12f) velocity = Vector3.zero;
            float verticalDistance = Mathf.Abs(position.y - transform.position.y);
            bool inWater = verticalDistance < interactionDepth;
            Vector2 currentPosition = new(position.x, position.z);
            bool movedFarEnough = Vector2.Distance(currentPosition, _lastRipplePosition) >= rippleSpacing;

            if (inWater && (!_wasInWater || movedFarEnough))
            {
                float strength = Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(velocity.magnitude / 4f));
                SpawnRipple(currentPosition, strength);
                _lastRipplePosition = currentPosition;
            }

            _wasInWater = inWater;
            _lastPlayerPosition = position;
            AdvanceRipples();
        }

        private void SpawnRipple(Vector2 origin, float strength)
        {
            float randomSeed = Mathf.Repeat(Time.time * 1.618f + _nextRipple * 2.417f, 10f);
            _ripples[_nextRipple] = new RippleState
            {
                Origin = origin,
                Age = 0f,
                Strength = strength,
                Angle = randomSeed * 1.73f,
                Aspect = Mathf.Lerp(0.86f, 1.14f, Mathf.Repeat(randomSeed * 0.731f, 1f)),
                NoiseSeed = randomSeed * 3.17f,
                Active = true
            };
            _nextRipple = (_nextRipple + 1) % RippleCount;
        }

        private void AdvanceRipples()
        {
            for (int i = 0; i < RippleCount; i++)
            {
                RippleState ripple = _ripples[i];
                if (ripple.Active)
                {
                    ripple.Age += Time.deltaTime / Mathf.Max(rippleLifetime, 0.01f);
                    if (ripple.Age >= 1f) ripple.Active = false;
                    _ripples[i] = ripple;
                }

                _rippleShaderData[i] = ripple.Active
                    ? new Vector4(ripple.Origin.x, ripple.Origin.y, ripple.Age, ripple.Strength)
                    : Vector4.zero;
                _rippleShapeData[i] = ripple.Active
                    ? new Vector4(ripple.Angle, ripple.Aspect, ripple.NoiseSeed, 0f)
                    : new Vector4(0f, 1f, 0f, 0f);
            }
            _material.SetVectorArray("_RippleData", _rippleShaderData);
            _material.SetVectorArray("_RippleShape", _rippleShapeData);
        }

        private void FindPlayer()
        {
            _player = FindFirstObjectByType<EurydicePlayerController>();
            _hasPlayerPosition = false;
            _wasInWater = false;
        }

        [ContextMenu("Refresh Stylized Water")]
        public void Apply()
        {
            Shader shader = Shader.Find("Eurydice/URP Stylized Water");
            if (shader == null) return;
            EnsureMesh();
            if (_material == null)
                _material = new Material(shader) { name = "Stylized Water Preview", hideFlags = HideFlags.DontSave };

            _material.SetColor("_DeepColor", deepColor);
            _material.SetColor("_ShallowColor", shallowColor);
            _material.SetColor("_HorizonColor", horizonColor);
            _material.SetColor("_FoamColor", foamColor);
            _material.SetFloat("_Transparency", transparency);
            _material.SetFloat("_DepthFade", depthFade);
            _material.SetFloat("_FoamWidth", foamWidth);
            _material.SetFloat("_CrestFoam", crestFoam);
            _material.SetFloat("_WaveHeight", waveHeight);
            _material.SetFloat("_WaveScale", waveScale);
            _material.SetFloat("_WaveSpeed", waveSpeed);
            _material.SetFloat("_NormalStrength", normalStrength);
            _material.SetFloat("_SparkleSize", sparkleSize);
            _material.SetFloat("_SparkleStrength", sparkleStrength);
            _material.SetFloat("_ReflectionStrength", reflectionStrength);
            _material.SetFloat("_RefractionStrength", refractionStrength);
            _material.SetFloat("_RippleRadius", rippleRadius);
            _material.SetFloat("_RippleWidth", rippleWidth);
            _material.SetFloat("_RippleHeight", rippleHeight);
            _material.SetVectorArray("_RippleData", _rippleShaderData);
            _material.SetVectorArray("_RippleShape", _rippleShapeData);
            GetComponent<MeshRenderer>().sharedMaterial = _material;
        }

        private void EnsureMesh()
        {
            DestroyObject(_waterMesh);
            int resolution = Mathf.Max(8, subdivisions);
            var vertices = new List<Vector3>((resolution + 1) * (resolution + 1));
            var uvs = new List<Vector2>((resolution + 1) * (resolution + 1));
            var triangles = new List<int>(resolution * resolution * 6);
            for (int z = 0; z <= resolution; z++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    float u = x / (float)resolution;
                    float v = z / (float)resolution;
                    vertices.Add(new Vector3((u - 0.5f) * 10f, 0f, (v - 0.5f) * 10f));
                    uvs.Add(new Vector2(u, v));
                }
            }
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int i = z * (resolution + 1) + x;
                    triangles.Add(i); triangles.Add(i + resolution + 1); triangles.Add(i + 1);
                    triangles.Add(i + 1); triangles.Add(i + resolution + 1); triangles.Add(i + resolution + 2);
                }
            }
            _waterMesh = new Mesh { name = "Stylized Water Grid", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32, hideFlags = HideFlags.DontSave };
            _waterMesh.SetVertices(vertices);
            _waterMesh.SetUVs(0, uvs);
            _waterMesh.SetTriangles(triangles, 0);
            _waterMesh.RecalculateNormals();
            _waterMesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = _waterMesh;
        }

        private void OnDestroy()
        {
            DestroyObject(_waterMesh);
            DestroyObject(_material);
        }

        private static void DestroyObject(Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target); else DestroyImmediate(target);
        }
    }
}

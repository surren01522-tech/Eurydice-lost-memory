using UnityEngine;

namespace Eurydice.Gameplay.Environment
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class StylizedPortalSurface : MonoBehaviour
    {
        [Header("Portal Interior")]
        [Tooltip("Static Texture2D or a live destination-camera RenderTexture.")]
        [SerializeField] private Texture portalTexture;
        [SerializeField] private Vector2 textureTiling = Vector2.one;
        [SerializeField] private Vector2 textureOffset;

        [Header("Color And Glow")]
        [SerializeField] private Color tintColor = new(0.45f, 0.9f, 0.5f, 1f);
        [SerializeField] private Color edgeColor = new(0.75f, 1f, 0.6f, 1f);
        [SerializeField, Range(0f, 1f)] private float opacity = 0.78f;
        [SerializeField, Range(0f, 8f)] private float emission = 2.2f;

        [Header("Motion")]
        [SerializeField, Range(0f, 0.15f)] private float distortion = 0.035f;
        [SerializeField, Range(-5f, 5f)] private float flowSpeed = 0.45f;
        [SerializeField, Range(0.01f, 0.4f)] private float edgeWidth = 0.13f;

        private Material _material;

        private void OnEnable() => Apply();
        private void OnValidate() { if (isActiveAndEnabled) Apply(); }

        [ContextMenu("Refresh Portal Surface")]
        public void Apply()
        {
            Shader shader = Shader.Find("Eurydice/URP Stylized Portal");
            if (shader == null) return;
            if (_material == null)
                _material = new Material(shader) { name = "Stylized Portal Preview", hideFlags = HideFlags.DontSave };

            _material.SetTexture("_PortalTexture", portalTexture != null ? portalTexture : Texture2D.whiteTexture);
            _material.SetTextureScale("_PortalTexture", textureTiling);
            _material.SetTextureOffset("_PortalTexture", textureOffset);
            _material.SetColor("_TintColor", tintColor);
            _material.SetColor("_EdgeColor", edgeColor);
            _material.SetFloat("_Opacity", opacity);
            _material.SetFloat("_Emission", emission);
            _material.SetFloat("_Distortion", distortion);
            _material.SetFloat("_FlowSpeed", flowSpeed);
            _material.SetFloat("_EdgeWidth", edgeWidth);
            GetComponent<MeshRenderer>().sharedMaterial = _material;
        }

        private void OnDestroy()
        {
            if (_material == null) return;
            if (Application.isPlaying) Destroy(_material); else DestroyImmediate(_material);
        }
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eurydice.Gameplay.Environment
{
    [DisallowMultipleComponent]
    public sealed class StageSkybox : MonoBehaviour
    {
        private enum SkyboxNumber
        {
            [InspectorName("00 — Scene Default / Custom")] SceneDefaultOrCustom = 0,
            [InspectorName("01 — Midday")] Midday = 1,
            [InspectorName("02 — Evening")] Evening = 2,
            [InspectorName("03 — Midnight")] Midnight = 3,
            [InspectorName("04 — Sunset")] Sunset = 4,
            [InspectorName("05 — Daybreak")] Daybreak = 5
        }

        [SerializeField] private SkyboxNumber skyboxNumber;
        [Tooltip("Optional custom material for option 00.")]
        [SerializeField] private Material skyboxMaterial;

        [Header("Farland Skies Materials")]
        [SerializeField] private Material midday;
        [SerializeField] private Material evening;
        [SerializeField] private Material midnight;
        [SerializeField] private Material sunset;
        [SerializeField] private Material daybreak;

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            ApplyToActiveScene();
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        private void OnActiveSceneChanged(Scene previous, Scene current)
        {
            ApplyToActiveScene();
        }

        private void ApplyToActiveScene()
        {
            if (gameObject.scene != SceneManager.GetActiveScene())
                return;

            Material selected = skyboxNumber switch
            {
                SkyboxNumber.Midday => midday,
                SkyboxNumber.Evening => evening,
                SkyboxNumber.Midnight => midnight,
                SkyboxNumber.Sunset => sunset,
                SkyboxNumber.Daybreak => daybreak,
                _ => skyboxMaterial
            };
            if (selected == null || RenderSettings.skybox == selected)
                return;

            RenderSettings.skybox = selected;
            DynamicGI.UpdateEnvironment();
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Eurydice.Gameplay.Progression
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class PortalTravel : MonoBehaviour
    {
        [SerializeField] private string destinationScene;
        [SerializeField] private string destinationArrival;
        [SerializeField, Min(0f)] private float fadeSeconds = 0.25f;

        private static bool transitionInProgress;

        private void Reset() => GetComponent<BoxCollider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerInput>();
            if (player == null || transitionInProgress || !player.inputIsActive)
                return;

            if (!Application.CanStreamedLevelBeLoaded(destinationScene))
            {
                Debug.LogError($"Portal scene is missing from the build scene list: {destinationScene}", this);
                return;
            }

            TravelAsync(player, destinationScene, destinationArrival, fadeSeconds);
        }

        private static async void TravelAsync(PlayerInput player, string scenePath, string arrivalId, float seconds)
        {
            transitionInProgress = true;
            GameObject overlay = null;
            try
            {
                player.DeactivateInput();
                overlay = new GameObject("Portal Transition", typeof(Canvas), typeof(CanvasGroup));
                DontDestroyOnLoad(overlay);
                Canvas canvas = overlay.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = short.MaxValue;
                CanvasGroup group = overlay.GetComponent<CanvasGroup>();

                var backdrop = new GameObject("Black Backdrop", typeof(RectTransform), typeof(Image));
                backdrop.transform.SetParent(overlay.transform, false);
                RectTransform rect = (RectTransform)backdrop.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                backdrop.GetComponent<Image>().color = Color.black;

                await FadeAsync(group, 0f, 1f, seconds);
                await SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);

                Scene scene = SceneManager.GetActiveScene();
                player = null;
                foreach (var candidate in FindObjectsByType<PlayerInput>(FindObjectsSortMode.None))
                {
                    if (candidate.gameObject.scene != scene)
                        continue;
                    player = candidate;
                    break;
                }

                if (player != null)
                {
                    player.DeactivateInput();
                    PlacePlayer(player, scene, arrivalId);
                }

                await FadeAsync(group, 1f, 0f, seconds);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                if (overlay != null)
                    Destroy(overlay);
                if (player != null)
                    player.ActivateInput();
                transitionInProgress = false;
            }
        }

        private static void PlacePlayer(PlayerInput player, Scene scene, string arrivalId)
        {
            foreach (var point in FindObjectsByType<PortalArrival>(FindObjectsSortMode.None))
            {
                if (point.gameObject.scene != scene || point.ArrivalId != arrivalId)
                    continue;

                var controller = player.GetComponent<CharacterController>();
                bool restoreController = controller != null && controller.enabled;
                if (restoreController)
                    controller.enabled = false;
                player.transform.SetPositionAndRotation(point.transform.position, point.transform.rotation);
                if (restoreController)
                    controller.enabled = true;
                return;
            }

            Debug.LogWarning($"No portal arrival '{arrivalId}' in {scene.name}; using the scene's spawn.");
        }

        private static async Awaitable FadeAsync(CanvasGroup group, float from, float to, float seconds)
        {
            group.alpha = from;
            for (float elapsed = 0f; elapsed < seconds;)
            {
                await Awaitable.NextFrameAsync();
                if (group == null)
                    return;
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, elapsed / seconds);
            }
            group.alpha = to;
        }
    }
}

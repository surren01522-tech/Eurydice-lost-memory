using Eurydice.Gameplay.Player;
using UnityEngine;
using UnityEngine.Events;

namespace Eurydice.Gameplay.Progression
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class MemoryFragment : MonoBehaviour
    {
        [SerializeField] private ChapterProgress progress;
        [SerializeField] private UnityEvent onCollected;
        [SerializeField, Min(0f)] private float collectAnimationDuration = 1f;
        [SerializeField] private string collectAnimationState = "MemoryPickup";

        private bool _collected;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            EurydicePlayerController player = other.GetComponentInParent<EurydicePlayerController>();
            if (_collected || player == null)
            {
                return;
            }

            _collected = true;
            progress?.CollectMemoryFragment();
            onCollected?.Invoke();
            player.PlayInteraction(collectAnimationState, collectAnimationDuration);
            gameObject.SetActive(false);
        }
    }
}

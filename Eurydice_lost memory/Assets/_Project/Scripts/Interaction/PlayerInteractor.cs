using Eurydice.Gameplay.Player;
using UnityEngine;

namespace Eurydice.Gameplay.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EurydiceInputReader))]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float radius = 2f;
        [SerializeField] private LayerMask interactionLayers = ~0;
        [SerializeField, Min(0f)] private float interactionAnimationDuration = 0.65f;

        private readonly Collider[] _hits = new Collider[16];
        private EurydiceInputReader _input;
        private EurydicePlayerController _controller;

        private void Awake()
        {
            _input = GetComponent<EurydiceInputReader>();
            _controller = GetComponent<EurydicePlayerController>();
        }

        private void Update()
        {
            if (!_input.InteractPressedThisFrame)
            {
                return;
            }

            IInteractable closest = FindClosestInteractable();
            if (closest == null)
            {
                return;
            }

            closest.Interact();
            float duration = closest.InteractionAnimationDuration > 0f
                ? closest.InteractionAnimationDuration
                : interactionAnimationDuration;
            _controller?.PlayInteraction(closest.InteractionAnimationState, duration);
        }

        private IInteractable FindClosestInteractable()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, _hits, interactionLayers, QueryTriggerInteraction.Collide);
            IInteractable closest = null;
            float closestDistance = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                IInteractable candidate = FindInteractable(_hits[i]);
                if (candidate == null || !candidate.CanInteract)
                {
                    continue;
                }

                float distance = (_hits[i].ClosestPoint(transform.position) - transform.position).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = candidate;
                }
            }

            return closest;
        }

        private static IInteractable FindInteractable(Collider target)
        {
            MonoBehaviour[] behaviours = target.GetComponentsInParent<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IInteractable interactable)
                {
                    return interactable;
                }
            }

            return null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}

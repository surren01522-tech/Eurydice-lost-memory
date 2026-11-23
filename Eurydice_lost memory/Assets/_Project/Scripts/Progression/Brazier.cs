using Eurydice.Gameplay.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace Eurydice.Gameplay.Progression
{
    [DisallowMultipleComponent]
    public sealed class Brazier : MonoBehaviour, IInteractable
    {
        [SerializeField] private ChapterProgress progress;
        [SerializeField] private GameObject unlitVisual;
        [SerializeField] private GameObject litVisual;
        [SerializeField] private UnityEvent onLit;
        [SerializeField] private string interactionAnimationState = "LightBrazier";
        [SerializeField, Min(0f)] private float interactionAnimationDuration = 1.2f;

        public bool IsLit { get; private set; }
        public bool CanInteract => !IsLit;
        public string InteractionAnimationState => interactionAnimationState;
        public float InteractionAnimationDuration => interactionAnimationDuration;

        private void Awake()
        {
            ApplyVisualState();
        }

        public void Interact()
        {
            if (IsLit)
            {
                return;
            }

            IsLit = true;
            ApplyVisualState();
            progress?.LightBrazier();
            onLit?.Invoke();
        }

        private void ApplyVisualState()
        {
            if (unlitVisual != null) unlitVisual.SetActive(!IsLit);
            if (litVisual != null) litVisual.SetActive(IsLit);
        }
    }
}

using UnityEngine;
using UnityEngine.Events;

namespace Eurydice.Gameplay.Progression
{
    [DisallowMultipleComponent]
    public sealed class PortalUnlocker : MonoBehaviour
    {
        [SerializeField] private ChapterProgress progress;
        [SerializeField] private GameObject portalObject;
        [SerializeField] private UnityEvent onPortalUnlocked;

        private void OnEnable()
        {
            if (progress != null)
            {
                progress.ChapterCompleted += UnlockPortal;
                if (progress.IsComplete) UnlockPortal();
            }
        }

        private void Start()
        {
            if (portalObject != null && (progress == null || !progress.IsComplete))
            {
                portalObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (progress != null)
            {
                progress.ChapterCompleted -= UnlockPortal;
            }
        }

        private void UnlockPortal()
        {
            if (portalObject != null) portalObject.SetActive(true);
            onPortalUnlocked?.Invoke();
        }
    }
}

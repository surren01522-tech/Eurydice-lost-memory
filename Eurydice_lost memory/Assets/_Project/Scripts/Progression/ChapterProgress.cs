using System;
using UnityEngine;
using UnityEngine.Events;

namespace Eurydice.Gameplay.Progression
{
    [DisallowMultipleComponent]
    public sealed class ChapterProgress : MonoBehaviour
    {
        [SerializeField, Min(0)] private int requiredMemoryFragments;
        [SerializeField, Min(0)] private int requiredBraziers;
        [SerializeField] private UnityEvent onChapterCompleted;

        public int CollectedMemoryFragments { get; private set; }
        public int LitBraziers { get; private set; }
        public bool IsComplete { get; private set; }

        public event Action ProgressChanged;
        public event Action ChapterCompleted;

        public void CollectMemoryFragment()
        {
            CollectedMemoryFragments++;
            EvaluateProgress();
        }

        public void LightBrazier()
        {
            LitBraziers++;
            EvaluateProgress();
        }

        private void EvaluateProgress()
        {
            ProgressChanged?.Invoke();
            if (IsComplete || CollectedMemoryFragments < requiredMemoryFragments || LitBraziers < requiredBraziers)
            {
                return;
            }

            IsComplete = true;
            ChapterCompleted?.Invoke();
            onChapterCompleted?.Invoke();
        }
    }
}

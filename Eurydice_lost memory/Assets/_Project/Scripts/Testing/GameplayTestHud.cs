using Eurydice.Gameplay.Player;
using Eurydice.Gameplay.Progression;
using UnityEngine;

namespace Eurydice.Gameplay.Testing
{
    public sealed class GameplayTestHud : MonoBehaviour
    {
        [SerializeField] private ChapterProgress progress;
        [SerializeField] private EurydicePlayerController player;

        public void Initialize(ChapterProgress chapterProgress, EurydicePlayerController playerController)
        {
            progress = chapterProgress;
            player = playerController;
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(16f, 16f, 440f, 145f), "Eurydice Gameplay Test");
            GUI.Label(new Rect(32f, 48f, 410f, 22f), "WASD: Move   Space: Jump / Hold in air to Glide   E: Light brazier");
            GUI.Label(new Rect(32f, 74f, 410f, 22f), "Collect 3 blue memory fragments and light 2 braziers.");

            if (progress != null)
            {
                GUI.Label(new Rect(32f, 100f, 410f, 22f), $"Fragments: {progress.CollectedMemoryFragments}/3   Braziers: {progress.LitBraziers}/2");
                GUI.Label(new Rect(32f, 126f, 410f, 22f), progress.IsComplete ? "Portal unlocked!" : $"Player state: {player?.State}");
            }
        }
    }
}

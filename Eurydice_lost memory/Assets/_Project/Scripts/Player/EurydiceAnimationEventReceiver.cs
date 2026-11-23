using UnityEngine;

namespace Eurydice.Gameplay.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class EurydiceAnimationEventReceiver : MonoBehaviour
    {
        [Header("Footstep Audio")]
        [SerializeField] private AudioClip landingAudioClip;
        [SerializeField] private AudioClip[] footstepAudioClips = System.Array.Empty<AudioClip>();
        [SerializeField, Range(0f, 1f)] private float footstepAudioVolume = 0.5f;

        private CharacterController _characterController;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
        }

        // Animation Event receiver used by the existing Starter Assets clips.
        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight <= 0.5f || footstepAudioClips.Length == 0)
            {
                return;
            }

            AudioClip clip = footstepAudioClips[Random.Range(0, footstepAudioClips.Length)];
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, AudioPosition(), footstepAudioVolume);
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && landingAudioClip != null)
            {
                AudioSource.PlayClipAtPoint(landingAudioClip, AudioPosition(), footstepAudioVolume);
            }
        }

        private Vector3 AudioPosition()
        {
            return transform.TransformPoint(_characterController.center);
        }
    }
}

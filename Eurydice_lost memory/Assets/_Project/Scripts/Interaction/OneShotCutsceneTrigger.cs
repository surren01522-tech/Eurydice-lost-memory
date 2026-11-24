using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Eurydice.Gameplay.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class OneShotCutsceneTrigger : MonoBehaviour
    {
        [Header("Sequence Identity")]
        [Min(1)] [SerializeField] private int sequenceNumber = 1;
        [SerializeField] private bool rememberAfterRestart = true;
        [SerializeField] private bool removeAfterWatching = true;

        [Header("Activation")]
        [SerializeField] private Key activationKey = Key.F2;
        [SerializeField] private string prompt = "F2  연출 보기";

        [Header("Short Camera Cut")]
        [SerializeField] private Camera sequenceCamera;
        [Min(0.1f)] [SerializeField] private float duration = 3f;
        [SerializeField] private bool lockPlayerInput = true;

        [Header("Sequence Events")]
        [SerializeField] private UnityEvent onSequenceStarted;
        [SerializeField] private UnityEvent onSequenceFinished;

        private const string SavePrefix = "Eurydice.Sequence.Watched.";
        private PlayerInput playerInput;
        private bool playerInside;
        private bool playing;

        private string SaveKey => SavePrefix + sequenceNumber;

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;

            if (sequenceCamera != null)
            {
                sequenceCamera.enabled = false;

                AudioListener extraListener = sequenceCamera.GetComponent<AudioListener>();
                if (extraListener != null)
                {
                    extraListener.enabled = false;
                }
            }

            if (rememberAfterRestart && PlayerPrefs.GetInt(SaveKey, 0) == 1)
            {
                gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!playerInside || playing || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current[activationKey].wasPressedThisFrame)
            {
                StartCoroutine(PlaySequence());
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerInput input = other.GetComponentInParent<PlayerInput>();
            if (input == null)
            {
                return;
            }

            playerInput = input;
            playerInside = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (playerInput != null && other.GetComponentInParent<PlayerInput>() == playerInput)
            {
                playerInside = false;
            }
        }

        private IEnumerator PlaySequence()
        {
            playing = true;

            if (lockPlayerInput && playerInput != null)
            {
                playerInput.DeactivateInput();
            }

            if (sequenceCamera != null)
            {
                sequenceCamera.enabled = true;
            }

            onSequenceStarted?.Invoke();
            yield return new WaitForSecondsRealtime(duration);
            onSequenceFinished?.Invoke();

            if (sequenceCamera != null)
            {
                sequenceCamera.enabled = false;
            }

            if (lockPlayerInput && playerInput != null)
            {
                playerInput.ActivateInput();
            }

            if (rememberAfterRestart)
            {
                PlayerPrefs.SetInt(SaveKey, 1);
                PlayerPrefs.Save();
            }

            playing = false;
            playerInside = false;

            if (removeAfterWatching)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (!playing)
            {
                return;
            }

            if (sequenceCamera != null)
            {
                sequenceCamera.enabled = false;
            }

            if (lockPlayerInput && playerInput != null)
            {
                playerInput.ActivateInput();
            }
        }

        private void OnGUI()
        {
            if (!playerInside || playing || string.IsNullOrWhiteSpace(prompt))
            {
                return;
            }

            const float width = 230f;
            const float height = 42f;
            Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.74f, width, height);
            GUI.Box(rect, $"{prompt}  ·  #{sequenceNumber:00}");
        }

        [ContextMenu("Forget This Sequence")]
        private void ForgetThisSequence()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            gameObject.SetActive(true);
        }
    }
}

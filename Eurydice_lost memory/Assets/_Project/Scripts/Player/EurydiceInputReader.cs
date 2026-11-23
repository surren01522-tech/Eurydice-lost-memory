using UnityEngine;
using UnityEngine.InputSystem;

namespace Eurydice.Gameplay.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class EurydiceInputReader : MonoBehaviour
    {
        private PlayerInput _playerInput;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private InputAction _interactAction;

        public Vector2 Move => _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        public Vector2 Look => _lookAction?.ReadValue<Vector2>() ?? Vector2.zero;
        public bool JumpPressedThisFrame => _jumpAction?.WasPressedThisFrame() ?? false;
        public bool JumpHeld => _jumpAction?.IsPressed() ?? false;
        public bool SprintHeld => _sprintAction?.IsPressed() ?? false;
        public bool InteractPressedThisFrame => _interactAction?.WasPressedThisFrame() ?? false;

        private void OnEnable()
        {
            SetCursorLocked(true);
        }

        private void OnDisable()
        {
            SetCursorLocked(false);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && isActiveAndEnabled)
            {
                SetCursorLocked(true);
            }
        }

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _moveAction = FindRequiredAction("Move");
            _lookAction = FindRequiredAction("Look");
            _jumpAction = FindRequiredAction("Jump");
            _sprintAction = FindRequiredAction("Sprint");
            _interactAction = FindRequiredAction("Interact");
        }

        private InputAction FindRequiredAction(string actionName)
        {
            InputAction action = _playerInput.actions?.FindAction(actionName, false);
            if (action == null)
            {
                Debug.LogError($"Input action '{actionName}' is missing on {name}.", this);
            }

            return action;
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}

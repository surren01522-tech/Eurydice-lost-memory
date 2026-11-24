using UnityEngine;

namespace Eurydice.Gameplay.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController), typeof(EurydiceInputReader))]
    public sealed class EurydicePlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private Animator animator;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float walkSpeed = 2f;
        [SerializeField, Min(0f)] private float runSpeed = 5.335f;
        [SerializeField, Min(0f)] private float acceleration = 16f;
        [SerializeField, Range(0.01f, 0.5f)] private float rotationSmoothTime = 0.12f;

        [Header("Air")]
        [SerializeField, Min(0f)] private float jumpHeight = 1.4f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float glideGravity = -3.5f;
        [SerializeField, Min(0f)] private float glideForwardSpeed = 4.5f;
        [SerializeField, Min(0f)] private float glideStartDelay = 0.15f;
        [SerializeField] private float terminalVelocity = -35f;

        [Header("Ground Check")]
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private float groundedOffset = -0.14f;
        [SerializeField, Min(0.01f)] private float groundedRadius = 0.28f;

        [Header("Grass Interaction")]
        [SerializeField, Min(0.1f)] private float grassInteractionRadius = 1.5f;
        [SerializeField, Range(0f, 0.6f)] private float grassBendStrength = 0.28f;
        [SerializeField, Range(0f, 0.2f)] private float grassRippleStrength = 0.055f;
        [SerializeField, Range(0f, 30f)] private float grassRippleSpeed = 14f;

        [Header("Camera")]
        [SerializeField] private bool rotateCameraTarget = true;
        [SerializeField, Range(0.01f, 2f)] private float lookSensitivity = 0.2f;
        [SerializeField] private float topClamp = 70f;
        [SerializeField] private float bottomClamp = -30f;

        [Header("Cross Fade States")]
        [SerializeField] private string idleState = "Idle Walk Run Blend";
        [SerializeField] private string moveState = "Idle Walk Run Blend";
        [SerializeField] private string jumpState = "JumpStart";
        [SerializeField] private string fallState = "InAir";
        [SerializeField] private string glideState = "Fly";
        [SerializeField] private string interactState = "";
        [SerializeField, Min(0f)] private float crossFadeDuration = 0.12f;

        private CharacterController _characterController;
        private EurydiceInputReader _input;
        private float _currentSpeed;
        private float _verticalVelocity;
        private float _rotationVelocity;
        private float _airTime;
        private float _cameraYaw;
        private float _cameraPitch;
        private bool _interactionLocked;
        private PlayerState _state;
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int MotionSpeedId = Animator.StringToHash("MotionSpeed");
        private static readonly int GrassInteractorPositionId = Shader.PropertyToID("_GrassInteractorPosition");
        private static readonly int GrassInteractionParamsId = Shader.PropertyToID("_GrassInteractionParams");

        public PlayerState State => _state;
        public bool IsGrounded { get; private set; }

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _input = GetComponent<EurydiceInputReader>();
            animator ??= GetComponentInChildren<Animator>();
            cameraTransform ??= Camera.main != null ? Camera.main.transform : null;

            if (cameraTarget != null)
            {
                Vector3 angles = cameraTarget.rotation.eulerAngles;
                _cameraYaw = angles.y;
                _cameraPitch = angles.x;
            }
        }

        private void Start()
        {
            ChangeState(PlayerState.Idle, true);
        }

        private void Update()
        {
            CheckGrounded();
            if (_interactionLocked)
            {
                ApplyGravityAndMove(Vector3.zero);
                UpdateGrassInteraction();
                return;
            }

            Vector3 moveDirection = CalculateMoveDirection(_input.Move);
            UpdateRotation(moveDirection);
            UpdateVerticalMovement();
            UpdateMovementState(moveDirection);
            ApplyGravityAndMove(moveDirection);
            UpdateGrassInteraction();
        }

        private void UpdateGrassInteraction()
        {
            float movement = Mathf.Clamp01(_currentSpeed / Mathf.Max(walkSpeed, 0.01f));
            float groundedStrength = IsGrounded ? Mathf.Lerp(0.12f, 1f, movement) : 0.08f;
            Shader.SetGlobalVector(GrassInteractorPositionId, transform.position);
            Shader.SetGlobalVector(GrassInteractionParamsId, new Vector4(
                grassInteractionRadius,
                grassBendStrength * groundedStrength,
                grassRippleStrength * groundedStrength,
                grassRippleSpeed));
        }

        private void OnDisable()
        {
            Shader.SetGlobalVector(GrassInteractionParamsId, Vector4.zero);
        }

        private void LateUpdate()
        {
            if (!rotateCameraTarget || cameraTarget == null)
            {
                return;
            }

            Vector2 look = _input.Look;
            if (look.sqrMagnitude > 0.001f)
            {
                _cameraYaw += look.x * lookSensitivity;
                _cameraPitch -= look.y * lookSensitivity;
            }

            _cameraPitch = Mathf.Clamp(_cameraPitch, bottomClamp, topClamp);
            cameraTarget.rotation = Quaternion.Euler(_cameraPitch, _cameraYaw, 0f);
        }

        public void PlayInteraction(float lockDuration)
        {
            PlayInteraction(interactState, lockDuration);
        }

        public void PlayInteraction(string stateName, float lockDuration)
        {
            if (!isActiveAndEnabled || _interactionLocked)
            {
                return;
            }

            StartCoroutine(InteractionRoutine(stateName, lockDuration));
        }

        private System.Collections.IEnumerator InteractionRoutine(string stateName, float duration)
        {
            _interactionLocked = true;
            _currentSpeed = 0f;
            _state = PlayerState.Interact;
            if (animator != null && !string.IsNullOrWhiteSpace(stateName))
            {
                animator.CrossFadeInFixedTime(stateName, crossFadeDuration);
            }
            yield return new WaitForSeconds(Mathf.Max(0f, duration));
            _interactionLocked = false;
            ChangeState(IsGrounded ? PlayerState.Idle : PlayerState.Fall, true);
        }

        private void CheckGrounded()
        {
            Vector3 center = transform.position + Vector3.up * groundedOffset;
            IsGrounded = Physics.CheckSphere(center, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);

            if (IsGrounded)
            {
                _airTime = 0f;
                if (_verticalVelocity < 0f)
                {
                    _verticalVelocity = -2f;
                }
            }
            else
            {
                _airTime += Time.deltaTime;
            }
        }

        private Vector3 CalculateMoveDirection(Vector2 input)
        {
            if (input.sqrMagnitude < 0.001f)
            {
                return Vector3.zero;
            }

            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            return (forward.normalized * input.y + right.normalized * input.x).normalized;
        }

        private void UpdateRotation(Vector3 moveDirection)
        {
            if (moveDirection.sqrMagnitude < 0.001f)
            {
                return;
            }

            float targetRotation = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref _rotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        }

        private void UpdateVerticalMovement()
        {
            if (IsGrounded && _input.JumpPressedThisFrame)
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                ChangeState(PlayerState.Jump);
                return;
            }

            bool canGlide = !IsGrounded && _verticalVelocity <= 0f && _airTime >= glideStartDelay && _input.JumpHeld;
            float activeGravity = canGlide ? glideGravity : gravity;
            _verticalVelocity = Mathf.Max(_verticalVelocity + activeGravity * Time.deltaTime, terminalVelocity);
        }

        private void UpdateMovementState(Vector3 moveDirection)
        {
            if (!IsGrounded)
            {
                if (_verticalVelocity > 0f)
                {
                    ChangeState(PlayerState.Jump);
                }
                else if (_input.JumpHeld && _airTime >= glideStartDelay)
                {
                    ChangeState(PlayerState.Glide);
                }
                else
                {
                    ChangeState(PlayerState.Fall);
                }

                return;
            }

            ChangeState(moveDirection.sqrMagnitude > 0.001f ? PlayerState.Move : PlayerState.Idle);
        }

        private void ApplyGravityAndMove(Vector3 moveDirection)
        {
            float targetSpeed = moveDirection.sqrMagnitude > 0.001f
                ? (_input.SprintHeld ? runSpeed : walkSpeed)
                : 0f;
            if (_state == PlayerState.Glide)
            {
                targetSpeed = Mathf.Max(targetSpeed, glideForwardSpeed);
                if (moveDirection.sqrMagnitude < 0.001f)
                {
                    moveDirection = transform.forward;
                }
            }

            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, acceleration * Time.deltaTime);
            if (animator != null)
            {
                animator.SetFloat(SpeedId, _currentSpeed, 0.1f, Time.deltaTime);
                animator.SetFloat(MotionSpeedId, targetSpeed > 0f ? 1f : 0f);
            }
            Vector3 velocity = moveDirection * _currentSpeed + Vector3.up * _verticalVelocity;
            _characterController.Move(velocity * Time.deltaTime);
        }

        private void ChangeState(PlayerState nextState, bool force = false)
        {
            if (!force && _state == nextState)
            {
                return;
            }

            _state = nextState;
            if (animator == null)
            {
                return;
            }

            string stateName = nextState switch
            {
                PlayerState.Idle => idleState,
                PlayerState.Move => moveState,
                PlayerState.Jump => jumpState,
                PlayerState.Fall => fallState,
                PlayerState.Glide => glideState,
                PlayerState.Interact => interactState,
                _ => idleState
            };

            if (!string.IsNullOrWhiteSpace(stateName))
            {
                animator.CrossFadeInFixedTime(stateName, crossFadeDuration);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * groundedOffset, groundedRadius);
        }
    }
}

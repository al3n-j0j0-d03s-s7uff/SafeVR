using UnityEngine;
using UnityEngine.InputSystem;

namespace SafeVR.Player
{
    /// <summary>
    /// Smooth First Person Controller for Desktop and Mobile.
    /// Desktop: uses Unity Input System actions (Keyboard/Mouse, Gamepad).
    /// Mobile: uses direct input from MobileHUD via public API.
    /// Can be swapped or extended with XR Origin for VR and AR platforms.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DesktopFPSController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 4.0f;
        [SerializeField] private float sprintSpeed = 7.0f;
        [SerializeField] private float gravity = -18.0f;
        [SerializeField] private float jumpHeight = 1.0f;

        [Header("Look Settings")]
        [SerializeField] private Transform playerCamera;
        [SerializeField] private float mouseSensitivity = 1.8f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;

        [Header("Mobile Look Settings")]
        [SerializeField] private float mobileLookSensitivity = 2.5f;

        [Header("Cursor & Control State")]
        [SerializeField] private bool lockCursorOnStart = true;

        [Header("Input System")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";

        public bool CanMove { get; set; } = true;
        public bool CanLook { get; set; } = true;

        /// <summary>Current look pitch in degrees. Used by PlayerViewSwitcher to sync third-person boom.</summary>
        public float CurrentPitch => cameraPitch;

        /// <summary>Sets look pitch (clamped). Used by PlayerViewSwitcher when returning to first-person.</summary>
        public void SetPitch(float pitch)
        {
            cameraPitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        private CharacterController characterController;
        private float verticalVelocity = 0f;
        private float cameraPitch = 0f;

        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction sprintAction;
        private InputAction jumpAction;

        // Mobile input state (blended with desktop — never latches out desktop input)
        private Vector2 mobileMoveInput = Vector2.zero;
        private bool mobileSprintHeld = false;
        private bool mobileJumpPressed = false;
        private bool mobileInteractPressed = false;
        private bool mobileInteractHeld = false;
        private bool mobileViewTogglePressed = false;
        private float mobileLookAccumulatorX = 0f;
        private float mobileLookAccumulatorY = 0f;

        /// <summary>True while the mobile SPRINT button (or desktop Sprint) is held.</summary>
        public bool IsSprinting => mobileSprintHeld || (sprintAction != null && sprintAction.enabled && sprintAction.IsPressed());
        /// <summary>True while the mobile USE button is held (extinguisher spray).</summary>
        public bool MobileInteractHeld => mobileInteractHeld;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (playerCamera == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null) playerCamera = cam.transform;
            }

            SetupInputActions();
        }

        private void SetupInputActions()
        {
            if (inputActions == null)
            {
                inputActions = Resources.Load<InputActionAsset>("InputSystem_Actions");
            }

            if (inputActions != null)
            {
                var actionMap = inputActions.FindActionMap(actionMapName);
                if (actionMap != null)
                {
                    moveAction = actionMap.FindAction("Move");
                    lookAction = actionMap.FindAction("Look");
                    sprintAction = actionMap.FindAction("Sprint");
                    jumpAction = actionMap.FindAction("Jump");

                    moveAction?.Enable();
                    lookAction?.Enable();
                    sprintAction?.Enable();
                    jumpAction?.Enable();
                }
            }
        }

        private void OnEnable()
        {
            moveAction?.Enable();
            lookAction?.Enable();
            sprintAction?.Enable();
            jumpAction?.Enable();
        }

        private void OnDisable()
        {
            moveAction?.Disable();
            lookAction?.Disable();
            sprintAction?.Disable();
            jumpAction?.Disable();
        }

        // ===== Mobile Input API =====
        /// <summary>Call from MobileHUD to set movement input (-1 to 1 range).</summary>
        public void SetMobileMoveInput(Vector2 input)
        {
            mobileMoveInput = Vector2.ClampMagnitude(input, 1f);
        }

        /// <summary>Call from MobileHUD to add look delta in screen pixels (camera rotation).</summary>
        public void AddMobileLookInput(Vector2 deltaPixels)
        {
            mobileLookAccumulatorX += deltaPixels.x;
            mobileLookAccumulatorY += deltaPixels.y;
        }

        /// <summary>Call from MobileHUD when sprint button is pressed/released.</summary>
        public void SetMobileSprint(bool held)
        {
            mobileSprintHeld = held;
        }

        /// <summary>Call from MobileHUD when jump button is pressed.</summary>
        public void PressMobileJump()
        {
            mobileJumpPressed = true;
        }

        /// <summary>Call from MobileHUD when interact button is pressed.</summary>
        public void PressMobileInteract()
        {
            mobileInteractPressed = true;
        }

        /// <summary>Call from MobileHUD when interact button is held (for extinguisher).</summary>
        public void SetMobileInteractHold(bool held)
        {
            mobileInteractHeld = held;
        }

        /// <summary>Call from MobileHUD when view toggle button is pressed.</summary>
        public void PressMobileViewToggle()
        {
            mobileViewTogglePressed = true;
        }

        private void Start()
        {
            if (lockCursorOnStart && !Application.isMobilePlatform)
            {
                SetCursorLocked(true);
            }
        }

        private void Update()
        {
            HandleCursorToggle();

            // On mobile there is no cursor lock — look runs whenever allowed.
            bool lookAllowed = CanLook && (Application.isMobilePlatform || Cursor.lockState == CursorLockMode.Locked);
#if UNITY_EDITOR
            // In-editor mobile-HUD testing still uses mouse look when cursor is locked.
            if (CanLook && !Application.isMobilePlatform && Cursor.lockState == CursorLockMode.Locked) lookAllowed = true;
#endif
            if (lookAllowed)
            {
                HandleLook();
            }

            if (CanMove)
            {
                HandleMovement();
            }
        }

        public void SetCursorLocked(bool locked)
        {
            if (Application.isMobilePlatform) return; // No cursor lock on mobile

            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void HandleCursorToggle()
        {
            if (Application.isMobilePlatform) return; // No escape key on mobile

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                bool currentlyLocked = Cursor.lockState == CursorLockMode.Locked;
                SetCursorLocked(!currentlyLocked);
            }
        }

        private void HandleLook()
        {
            Vector2 lookDelta = Vector2.zero;

            // Desktop mouse/gamepad look (Input System). Touch bindings were removed
            // from the Player map so touch drags never arrive here — only MobileHUD does.
            if (lookAction != null && lookAction.enabled)
            {
                Vector2 raw = lookAction.ReadValue<Vector2>();
                float sensitivity = mouseSensitivity * 0.1f;
                if (Mouse.current != null && !Mouse.current.delta.IsPressed())
                {
                    // Gamepad stick deflections arrive here too; scale them down.
                    sensitivity = mouseSensitivity * 0.05f;
                }
                lookDelta += raw * sensitivity;
            }

            // Mobile touch look (accumulated pixels from MobileHUD, scaled once here).
            if (mobileLookAccumulatorX != 0f || mobileLookAccumulatorY != 0f)
            {
                lookDelta.x += mobileLookAccumulatorX * mobileLookSensitivity * 0.1f;
                lookDelta.y += mobileLookAccumulatorY * mobileLookSensitivity * 0.1f;
                mobileLookAccumulatorX = 0f;
                mobileLookAccumulatorY = 0f;
            }

            // Horizontal rotation (player body)
            transform.Rotate(Vector3.up * lookDelta.x);

            // Vertical rotation (camera pitch)
            if (playerCamera != null)
            {
                cameraPitch -= lookDelta.y;
                cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);
                playerCamera.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            // Blend desktop + mobile so both control schemes always work.
            Vector2 moveInput = Vector2.zero;
            if (moveAction != null && moveAction.enabled)
            {
                moveInput += moveAction.ReadValue<Vector2>();
            }
            moveInput += mobileMoveInput;
            if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();

            bool isSprinting = mobileSprintHeld;
            if (sprintAction != null && sprintAction.enabled && sprintAction.IsPressed())
            {
                isSprinting = true;
            }

            bool jumpPressed = mobileJumpPressed;
            if (jumpAction != null && jumpAction.enabled && jumpAction.WasPressedThisFrame())
            {
                jumpPressed = true;
            }
            mobileJumpPressed = false; // Consume one-frame input
            mobileInteractPressed = false; // Consumed by MobileHUD -> Interactor path
            mobileViewTogglePressed = false; // Consumed by MobileHUD -> PlayerViewSwitcher path

            Vector3 moveDirection = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
            float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

            // Grounding & gravity
            if (characterController.isGrounded)
            {
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = -2f; // Slight downward push to maintain contact
                }

                if (jumpPressed)
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            Vector3 finalVelocity = moveDirection * currentSpeed + Vector3.up * verticalVelocity;
            characterController.Move(finalVelocity * Time.deltaTime);
        }
    }
}

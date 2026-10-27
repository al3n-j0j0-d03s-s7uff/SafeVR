using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SafeVR.Interaction
{
    /// <summary>
    /// Interactor component that performs raycasting to detect and trigger IInteractable objects.
    /// Can be attached to the Main Camera (for Desktop FPS and VR gaze) or directly to VR hand controller anchors.
    /// Uses Unity Input System for cross-platform input (Keyboard/Mouse, Gamepad, Touch).
    /// </summary>
    public class Interactor : MonoBehaviour
    {
        [Header("Raycast Settings")]
        [Tooltip("Origin transform for the interaction ray. If null, uses this gameObject's transform.")]
        [SerializeField] private Transform rayOrigin;
        
        [Tooltip("Maximum interaction distance in meters.")]
        [SerializeField] private float maxReachDistance = 3.0f;
        
        [Tooltip("Physics layers to check for interactables.")]
        [SerializeField] private LayerMask interactableLayers = ~0;

        [Header("Input System")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private bool listenForInputActions = true;

        public IInteractable CurrentInteractable { get; private set; }
        public RaycastHit LastHit { get; private set; }

        public event Action<IInteractable> OnHoverChanged;
        public event Action<IInteractable> OnInteracted;

        private IHoldInteractable currentHold;
        private bool isHolding;
        private bool mobileHoldPressed; // Driven by MobileHUD USE button (Android spray)

        private InputAction interactAction;
        private InputAction attackAction; // For hold interactions (LMB on desktop)

        private void Awake()
        {
            if (rayOrigin == null)
            {
                Camera cam = GetComponent<Camera>();
                if (cam == null) cam = Camera.main;
                rayOrigin = cam != null ? cam.transform : transform;
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
                    interactAction = actionMap.FindAction("Interact");
                    attackAction = actionMap.FindAction("Attack"); // Used for hold (LMB/touch press)

                    interactAction?.Enable();
                    attackAction?.Enable();
                }
            }
        }

        private void OnEnable()
        {
            interactAction?.Enable();
            attackAction?.Enable();
        }

        private void OnDisable()
        {
            interactAction?.Disable();
            attackAction?.Disable();

            if (CurrentInteractable != null)
            {
                CurrentInteractable.OnHoverExit(this);
                CurrentInteractable = null;
                OnHoverChanged?.Invoke(null);
            }
        }

        private void Update()
        {
            PerformRaycast();
            HandleHoldInput();

            if (listenForInputActions && CheckInteractInput())
            {
                TriggerInteraction();
            }
        }

        /// <summary>Mobile USE button drives hold directly (Android spray).</summary>
        public void SetMobileHold(bool held)
        {
            mobileHoldPressed = held;
            if (held)
            {
                // Start hold immediately if aiming at a hold target (pin pull is
                // handled separately by TriggerInteraction on button down).
                if (!isHolding && CurrentInteractable is IHoldInteractable h && h.IsInteractable)
                {
                    currentHold = h;
                    isHolding = true;
                    currentHold.OnHoldStart(this);
                }
            }
            else
            {
                EndHold();
            }
        }

        private void HandleHoldInput()
        {
            if (!listenForInputActions) return;

            bool holdPressed = CheckHoldIsPressed() || mobileHoldPressed;
            bool holdDownThisFrame = CheckHoldDownThisFrame();
            // Mobile hold edge is handled in SetMobileHold; desktop edge below.
            bool holdUpThisFrame = CheckHoldUpThisFrame();

            // If currently holding a hold-interactable
            if (isHolding && currentHold != null)
            {
                bool stillAimingAtSame = CurrentInteractable == (IInteractable)currentHold;
                if (!holdPressed || !stillAimingAtSame)
                {
                    // Cancel or complete depends on progress - let interactable decide, here we cancel
                    currentHold.OnHoldCancel(this);
                    isHolding = false;
                    currentHold = null;
                    return;
                }
                currentHold.OnHoldTick(this, Time.deltaTime);
                if (currentHold.CurrentHoldProgress >= 1f)
                {
                    currentHold.OnHoldEnd(this, true);
                    isHolding = false;
                    currentHold = null;
                }
                return;
            }

            // Start hold if aiming at hold-interactable and player holds
            if (holdDownThisFrame && CurrentInteractable is IHoldInteractable holdTarget && holdTarget.IsInteractable)
            {
                currentHold = holdTarget;
                isHolding = true;
                currentHold.OnHoldStart(this);
            }
        }

        private void PerformRaycast()
        {
            Transform origin = rayOrigin != null ? rayOrigin : transform;
            Ray ray = new Ray(origin.position, origin.forward);
            IInteractable foundInteractable = null;

            if (Physics.Raycast(ray, out RaycastHit hit, maxReachDistance, interactableLayers, QueryTriggerInteraction.Collide))
            {
                LastHit = hit;
                foundInteractable = hit.collider.GetComponentInParent<IInteractable>();
                if (foundInteractable != null && !foundInteractable.IsInteractable)
                {
                    foundInteractable = null;
                }
            }

            if (foundInteractable != CurrentInteractable)
            {
                // Exit previous
                if (CurrentInteractable != null)
                {
                    CurrentInteractable.OnHoverExit(this);
                }

                CurrentInteractable = foundInteractable;

                // Enter new
                if (CurrentInteractable != null)
                {
                    CurrentInteractable.OnHoverEnter(this);
                }

                OnHoverChanged?.Invoke(CurrentInteractable);
            }
        }

        /// <summary>
        /// Manually triggers interaction on the currently hovered interactable.
        /// Suitable for calling from VR Hand controllers, AR gestures, or custom input scripts.
        /// </summary>
        public void TriggerInteraction()
        {
            if (CurrentInteractable != null && CurrentInteractable.IsInteractable)
            {
                CurrentInteractable.OnInteract(this);
                OnInteracted?.Invoke(CurrentInteractable);
            }
        }

        private bool CheckInteractInput()
        {
            if (interactAction != null && interactAction.enabled)
            {
                return interactAction.WasPressedThisFrame();
            }
            return false;
        }

        private bool CheckHoldIsPressed()
        {
            // Use Attack action for hold (LMB on desktop, touch press on mobile)
            if (attackAction != null && attackAction.enabled)
            {
                return attackAction.IsPressed();
            }
            return false;
        }

        private bool CheckHoldDownThisFrame()
        {
            if (attackAction != null && attackAction.enabled)
            {
                return attackAction.WasPressedThisFrame();
            }
            return false;
        }

        private bool CheckHoldUpThisFrame()
        {
            if (attackAction != null && attackAction.enabled)
            {
                return attackAction.WasReleasedThisFrame();
            }
            return false;
        }

        /// <summary>VR / external callers can drive hold directly</summary>
        public void StartHold()
        {
            if (CurrentInteractable is IHoldInteractable h && h.IsInteractable && !isHolding)
            {
                currentHold = h;
                isHolding = true;
                currentHold.OnHoldStart(this);
            }
        }

        public void EndHold()
        {
            if (isHolding && currentHold != null)
            {
                currentHold.OnHoldCancel(this);
                isHolding = false;
                currentHold = null;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin = rayOrigin != null ? rayOrigin : transform;
            Gizmos.color = CurrentInteractable != null ? Color.green : Color.cyan;
            Gizmos.DrawRay(origin.position, origin.forward * maxReachDistance);
        }
    }
}

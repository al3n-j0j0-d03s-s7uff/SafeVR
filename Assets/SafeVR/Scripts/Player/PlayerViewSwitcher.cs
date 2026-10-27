using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace SafeVR.Player
{
    /// <summary>
    /// First-person / third-person camera modes for the shared SafeVR player.
    /// Only repositions the existing PlayerCamera (same Camera, same Interactor ray),
    /// so interaction works identically in both modes (aimed at screen center).
    /// Input: ToggleView action from Input System (V key on desktop, button on mobile).
    /// VR-ready: VR rigs can disable or replace this component without touching movement or interaction code.
    /// </summary>
    public class PlayerViewSwitcher : MonoBehaviour
    {
        public enum ViewMode { FirstPerson, ThirdPerson }

        [Header("Third Person")]
        [SerializeField] private float boomDistance = 3.2f;
        [SerializeField] private float boomHeight = 0.55f;
        [SerializeField] private float smoothTime = 0.08f;
        [SerializeField] private float minBoomPitch = -60f;
        [SerializeField] private float maxBoomPitch = 60f;

        [Header("Body")]
        [Tooltip("Visible humanoid body root. Hidden in first-person, shown in third-person.")]
        [SerializeField] private GameObject bodyRoot;

        [Header("UI")]
        [Tooltip("Small label showing the active view. Auto-created if left empty.")]
        [SerializeField] private TMP_Text viewLabel;

        [Header("Input System")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";

        public ViewMode CurrentMode { get; private set; } = ViewMode.FirstPerson;
        public event Action<ViewMode> OnViewChanged;

        private DesktopFPSController fpsController;
        private Transform playerCamera;
        private Vector3 firstPersonPos;
        private Quaternion firstPersonRot;
        private Vector3 smoothVelocity;
        private float boomPitch;
        private Renderer[] bodyRenderers = Array.Empty<Renderer>();

        private InputAction toggleViewAction;
        private InputAction lookAction;

        private void Awake()
        {
            // Guard against duplicate players/cameras if a scene is ever merged twice.
            var all = FindObjectsByType<PlayerViewSwitcher>(FindObjectsSortMode.None);
            if (all.Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            fpsController = GetComponent<DesktopFPSController>();
            if (fpsController == null) fpsController = GetComponentInParent<DesktopFPSController>();

            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) playerCamera = cam.transform;

            if (bodyRoot == null)
            {
                Transform found = transform.Find("CharacterBody");
                if (found != null) bodyRoot = found.gameObject;
            }
            if (bodyRoot != null) bodyRenderers = bodyRoot.GetComponentsInChildren<Renderer>(true);

            if (playerCamera != null)
            {
                firstPersonPos = playerCamera.localPosition;
                firstPersonRot = playerCamera.localRotation;
            }
            if (fpsController != null) boomPitch = fpsController.CurrentPitch;

            SetupInputActions();

            ApplyBodyVisibility();
            UpdateLabel();
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
                    toggleViewAction = actionMap.FindAction("ToggleView");
                    lookAction = actionMap.FindAction("Look");

                    toggleViewAction?.Enable();
                    lookAction?.Enable();
                }
            }
        }

        private void OnEnable()
        {
            toggleViewAction?.Enable();
            lookAction?.Enable();
        }

        private void OnDisable()
        {
            toggleViewAction?.Disable();
            lookAction?.Disable();
        }

        private void Update()
        {
            if (CheckTogglePressed()) ToggleView();
            if (CurrentMode == ViewMode.ThirdPerson) HandleThirdPersonPitch();
        }

        private void LateUpdate()
        {
            if (playerCamera == null) return;
            if (CurrentMode == ViewMode.ThirdPerson) UpdateThirdPersonCamera();
        }

        public void ToggleView()
        {
            SetView(CurrentMode == ViewMode.FirstPerson ? ViewMode.ThirdPerson : ViewMode.FirstPerson);
        }

        public void SetView(ViewMode mode)
        {
            if (CurrentMode == mode) return;
            if (mode == ViewMode.ThirdPerson)
            {
                if (fpsController != null) boomPitch = fpsController.CurrentPitch;
                smoothVelocity = Vector3.zero;
            }
            else
            {
                if (fpsController != null) fpsController.SetPitch(boomPitch);
                if (playerCamera != null)
                {
                    playerCamera.localPosition = firstPersonPos;
                    playerCamera.localRotation = firstPersonRot;
                }
            }
            CurrentMode = mode;
            ApplyBodyVisibility();
            UpdateLabel();
            OnViewChanged?.Invoke(mode);
        }

        private void HandleThirdPersonPitch()
        {
            Vector2 lookDelta = Vector2.zero;

            if (lookAction != null && lookAction.enabled)
            {
                lookDelta = lookAction.ReadValue<Vector2>();
            }

            // Scale for third person
            float sensitivity = 0.18f;
            if (Mouse.current != null && Mouse.current.delta.IsPressed())
            {
                sensitivity = 0.18f;
            }
            else
            {
                sensitivity = 0.1f;
            }

            lookDelta *= sensitivity;
            boomPitch = Mathf.Clamp(boomPitch - lookDelta.y, minBoomPitch, maxBoomPitch);
        }

        private void UpdateThirdPersonCamera()
        {
            Vector3 pivot = transform.position + Vector3.up * 1.55f;
            Quaternion yawRot = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            Vector3 lookDir = Quaternion.Euler(boomPitch, transform.eulerAngles.y, 0f) * Vector3.forward;

            Vector3 desired = pivot - lookDir * boomDistance + Vector3.up * boomHeight;
            float dist = boomDistance;

            // Camera collision: shrink boom on world hits, ignoring the player's own body.
            RaycastHit[] hits = Physics.RaycastAll(pivot, (desired - pivot).normalized,
                boomDistance + 0.3f, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            foreach (var hit in hits)
            {
                if (hit.transform == null || hit.transform.IsChildOf(transform)) continue;
                if (hit.distance < nearest) nearest = hit.distance;
            }
            if (nearest < float.MaxValue) dist = Mathf.Max(0.4f, nearest - 0.3f);

            Vector3 target = pivot - lookDir * dist + Vector3.up * boomHeight * (dist / boomDistance);
            target.y = Mathf.Max(target.y, 0.25f);
            playerCamera.position = Vector3.SmoothDamp(playerCamera.position, target, ref smoothVelocity, smoothTime);
            playerCamera.rotation = Quaternion.LookRotation((pivot + lookDir * 4f - playerCamera.position).normalized, Vector3.up);
        }

        private void ApplyBodyVisibility()
        {
            bool show = CurrentMode == ViewMode.ThirdPerson;
            foreach (var r in bodyRenderers)
                if (r != null) r.enabled = show;
        }

        private bool CheckTogglePressed()
        {
            if (toggleViewAction != null && toggleViewAction.enabled)
            {
                return toggleViewAction.WasPressedThisFrame();
            }
            return false;
        }

        private void UpdateLabel()
        {
            if (viewLabel == null) viewLabel = CreateFallbackLabel();
            if (viewLabel != null)
            {
                string toggleText = Application.isMobilePlatform ? "[VIEW]" : "[V]";
                viewLabel.text = CurrentMode == ViewMode.FirstPerson
                    ? $"First Person  •  {toggleText} Third Person"
                    : $"Third Person  •  {toggleText} First Person";
            }
        }

        private TMP_Text CreateFallbackLabel()
        {
            var canvasGO = new GameObject("ViewModeCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var labelGO = new GameObject("ViewModeLabel", typeof(RectTransform));
            labelGO.transform.SetParent(canvasGO.transform, false);
            var rt = labelGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(24f, 24f);
            rt.sizeDelta = new Vector2(420f, 30f);
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 16;
            tmp.color = new Color(0.75f, 0.8f, 0.85f);
            tmp.alignment = TextAlignmentOptions.BottomLeft;
            return tmp;
        }
    }
}

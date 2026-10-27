using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using SafeVR.UI;
using SafeVR.Interaction;

namespace SafeVR.Player
{
    /// <summary>
    /// Mobile touch HUD with custom joystick, touch look area, and UI buttons.
    /// Directly drives DesktopFPSController and Interactor without Input System OnScreen controls.
    /// Only active on mobile platforms (Android/iOS) in gameplay scenes (Tutorial, TrainingRoom, Assessment).
    /// Desktop input (WASD/mouse/E/Shift/Space/V/Tab) is preserved — this HUD only ADDS touch input.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class MobileHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerViewSwitcher viewSwitcher;

        [Header("Layout Settings")]
        [SerializeField] private float joystickSize = 200f;
        [SerializeField] private float buttonSize = 110f;
        [SerializeField] private float margin = 28f;

        [Header("Visual Settings")]
        [SerializeField] private Color joystickBgColor = new Color(0.05f, 0.08f, 0.12f, 0.55f);
        [SerializeField] private Color joystickKnobColor = new Color(0.2f, 0.85f, 0.95f, 0.9f);
        [SerializeField] private Color buttonColor = new Color(0.08f, 0.35f, 0.5f, 0.85f);
        [SerializeField] private Color buttonPressedColor = new Color(0.2f, 0.85f, 0.95f, 1f);
        [SerializeField] private Color interactColor = new Color(0.08f, 0.55f, 0.32f, 0.9f);

        [Header("Mobile Sensitivity")]
        [SerializeField] private float moveSensitivity = 1f;

        private Canvas canvas;
        private CanvasScaler canvasScaler;
        private RectTransform canvasRect;
        private RectTransform safeContainer;
        private bool isMobilePlatform;
        private bool hudBuilt;

        // Joystick components
        private RectTransform joystickBackground;
        private RectTransform joystickHandle;
        private Image joystickBgImage;
        private int joystickPointerId = -1;
        private Vector2 joystickOutput = Vector2.zero;

        // Look area — MUST stay behind all buttons (first sibling)
        private RectTransform lookArea;
        private Vector2 lookLastPosition;
        private int lookPointerId = -1;

        // Buttons (last siblings = on top = receive touches first)
        private Button interactButton;
        private Button sprintButton;
        private Button jumpButton;
        private Button viewToggleButton;
        private Button pauseButton;

        // Runtime references (resolved lazily — prefab Awake order is not guaranteed)
        private DesktopFPSController fpsController;
        private Interactor interactor;
        private ScenarioPauseMenu pauseMenu;

        // True while the HUD yields touches to a modal (pause / retry / inspection /
        // completion). The MobileHUD canvas sorts above the scene canvas, so its
        // invisible LookArea must stop raycasting or modal buttons can't be tapped.
        private bool controlsYielded;

        private static readonly string[] GameplayScenes =
        {
            "Tutorial",
            "TrainingRoom",
            "Assessment"
        };

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            canvasScaler = GetComponent<CanvasScaler>();
            canvasRect = GetComponent<RectTransform>();

            // Configure canvas: overlay, above all scene UI, on UI layer.
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            gameObject.layer = 5; // UI
            // Overlay canvases ignore the camera, but set explicitly for safety.
            canvas.worldCamera = null;
            canvas.planeDistance = 100f;

            if (canvasScaler == null) canvasScaler = gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();
            // GraphicRaycaster on an Overlay canvas must not block scene raycasts.
            var raycaster = GetComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = true;
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

            EnsureEventSystem();

            isMobilePlatform = Application.isMobilePlatform;

#if UNITY_EDITOR
            isMobilePlatform = isMobilePlatform || MobileHUDDebug.ForceMobileHUDInEditor;
#endif

            // Scene-aware activation: Tutorial / TrainingRoom / Assessment only.
            if (!isMobilePlatform || !IsGameplayScene())
            {
                gameObject.SetActive(false);
                return;
            }

            ResolveReferences();
            BuildHUD();
            hudBuilt = true;
        }

        private void Start()
        {
            // Late resolve: Player prefab children may awake before scene UI.
            if (hudBuilt && (fpsController == null || interactor == null))
                ResolveReferences();
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            // Keep across scene loads so Android builds never lose touch input.
            Object.DontDestroyOnLoad(es);
        }

        private void ResolveReferences()
        {
            if (fpsController == null)
            {
                fpsController = GetComponentInParent<DesktopFPSController>();
                if (fpsController == null) fpsController = Object.FindFirstObjectByType<DesktopFPSController>();
            }
            if (interactor == null)
            {
                interactor = GetComponentInParent<Interactor>();
                if (interactor == null) interactor = GetComponentInChildren<Interactor>();
                if (interactor == null) interactor = Object.FindFirstObjectByType<Interactor>();
            }
            if (viewSwitcher == null)
            {
                viewSwitcher = GetComponentInParent<PlayerViewSwitcher>();
                if (viewSwitcher == null) viewSwitcher = Object.FindFirstObjectByType<PlayerViewSwitcher>();
            }
            if (pauseMenu == null)
            {
                pauseMenu = Object.FindFirstObjectByType<ScenarioPauseMenu>();
            }
        }

        private bool IsGameplayScene()
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach (var sceneName in GameplayScenes)
            {
                if (activeScene.name == sceneName) return true;
            }
            return false;
        }

        private void OnEnable()
        {
            if (isMobilePlatform && !IsGameplayScene())
            {
                gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            // Recover refs if scene reloaded around us (DontDestroyOnLoad EventSystem case).
            if (hudBuilt && (fpsController == null || interactor == null || pauseMenu == null))
                ResolveReferences();
            UpdateModalYield();
        }

        /// <summary>
        /// Yields all HUD touches to scene modals (pause / retry confirm / inspection /
        /// completion). Uses only existing state — never touches core gameplay systems.
        /// Desktop is unaffected (this component is disabled off mobile).
        /// </summary>
        private void UpdateModalYield()
        {
            if (!hudBuilt) return;
            bool blocked = false;
            if (pauseMenu != null && (pauseMenu.IsPaused || pauseMenu.IsConfirmOpen))
            {
                blocked = true;
            }
            else if (fpsController != null && (!fpsController.CanMove || !fpsController.CanLook))
            {
                // Any gameplay modal (inspection / completion / report) holds movement
                // and look off — its buttons live under this canvas and need touches.
                blocked = true;
            }
            if (blocked == controlsYielded) return;
            controlsYielded = blocked;
            SetControlsYielded(blocked);
        }

        private void SetControlsYielded(bool yielded)
        {
            // Release in-flight touches so movement never sticks through a modal.
            joystickPointerId = -1;
            lookPointerId = -1;
            joystickOutput = Vector2.zero;
            if (joystickHandle != null) joystickHandle.anchoredPosition = Vector2.zero;
            fpsController?.SetMobileMoveInput(Vector2.zero);
            fpsController?.SetMobileSprint(false);
            fpsController?.SetMobileInteractHold(false);
            interactor?.SetMobileHold(false);

            // LookArea is invisible but covers the right side of the screen — it must
            // stop raycasting or pause/retry/menu buttons underneath can't be tapped.
            if (lookArea != null)
            {
                var lookImg = lookArea.GetComponent<Image>();
                if (lookImg != null) lookImg.raycastTarget = !yielded;
            }
            if (joystickBackground != null)
            {
                var joyImg = joystickBackground.GetComponent<Image>();
                if (joyImg != null) joyImg.raycastTarget = !yielded;
                joystickBackground.gameObject.SetActive(!yielded);
            }
            // Hide action buttons; PAUSE stays visible so the user can always resume.
            if (interactButton != null) interactButton.gameObject.SetActive(!yielded);
            if (sprintButton != null) sprintButton.gameObject.SetActive(!yielded);
            if (jumpButton != null) jumpButton.gameObject.SetActive(!yielded);
            if (viewToggleButton != null) viewToggleButton.gameObject.SetActive(!yielded);
        }

        private void BuildHUD()
        {
            // Safe-area container: canvas itself stays full-screen (Overlay canvases
            // ignore anchors), children are inset from notches / gesture bars.
            safeContainer = new GameObject("SafeArea", typeof(RectTransform)).GetComponent<RectTransform>();
            safeContainer.SetParent(canvas.transform, false);
            safeContainer.anchorMin = Vector2.zero;
            safeContainer.anchorMax = Vector2.one;
            safeContainer.pivot = new Vector2(0.5f, 0.5f);
            safeContainer.offsetMin = Vector2.zero;
            safeContainer.offsetMax = Vector2.zero;
            ApplySafeAreaInsets();

            // Order matters: LookArea FIRST (behind), joystick next, buttons LAST (on top).
            CreateLookArea();
            CreateJoystick();
            CreateButtons();

            lookArea.SetAsFirstSibling();
            safeContainer.SetAsLastSibling();
        }

        private void ApplySafeAreaInsets()
        {
            // Inset the safe container so controls clear notches / rounded corners.
            Rect sa = Screen.safeArea;
            if (sa.width <= 0f || sa.height <= 0f) return;
            Vector2 min = sa.position;
            Vector2 max = sa.position + sa.size;
            float left = min.x;
            float bottom = min.y;
            float right = Screen.width - max.x;
            float top = Screen.height - max.y;
            // Canvas-space pixels match screen pixels for Overlay at scale factor 1;
            // CanvasScaler handles the rest via anchors, so apply raw insets.
            safeContainer.offsetMin = new Vector2(left, bottom);
            safeContainer.offsetMax = new Vector2(-right, -top);
        }

        private void CreateJoystick()
        {
            // Background (static base, bottom-left inside safe area)
            var bgGo = new GameObject("MoveJoystickBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.transform.SetParent(safeContainer, false);
            bgGo.layer = 5;
            joystickBackground = bgGo.GetComponent<RectTransform>();
            joystickBackground.sizeDelta = new Vector2(joystickSize, joystickSize);
            joystickBackground.anchorMin = Vector2.zero;
            joystickBackground.anchorMax = Vector2.zero;
            joystickBackground.pivot = new Vector2(0f, 0f);
            joystickBackground.anchoredPosition = new Vector2(margin, margin);

            joystickBgImage = bgGo.GetComponent<Image>();
            joystickBgImage.color = joystickBgColor;
            // No sprite assigned: Simple type renders a solid rounded-ish disc reliably
            // on device (Sliced with a null sprite can vanish on some Android GPUs).
            joystickBgImage.type = Image.Type.Simple;
            joystickBgImage.raycastTarget = true;

            var trigger = bgGo.AddComponent<EventTrigger>();
            AddEventTrigger(trigger, EventTriggerType.PointerDown, OnJoystickDown);
            AddEventTrigger(trigger, EventTriggerType.Drag, OnJoystickDrag);
            AddEventTrigger(trigger, EventTriggerType.PointerUp, OnJoystickUp);
            AddEventTrigger(trigger, EventTriggerType.PointerExit, OnJoystickUp);

            // Handle (knob)
            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            handleGo.transform.SetParent(bgGo.transform, false);
            handleGo.layer = 5;
            joystickHandle = handleGo.GetComponent<RectTransform>();
            joystickHandle.sizeDelta = new Vector2(joystickSize * 0.55f, joystickSize * 0.55f);
            joystickHandle.anchorMin = Vector2.one * 0.5f;
            joystickHandle.anchorMax = Vector2.one * 0.5f;
            joystickHandle.pivot = Vector2.one * 0.5f;
            joystickHandle.anchoredPosition = Vector2.zero;

            var handleImage = handleGo.GetComponent<Image>();
            handleImage.color = joystickKnobColor;
            handleImage.type = Image.Type.Simple;
            handleImage.raycastTarget = false;
        }

        private void CreateLookArea()
        {
            var go = new GameObject("LookArea", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            // NOTE: parented to the canvas (not the safe container) so it fills the
            // whole right half of the screen edge-to-edge for responsive look drags.
            go.transform.SetParent(canvas.transform, false);
            go.layer = 5;
            lookArea = go.GetComponent<RectTransform>();
            lookArea.anchorMin = new Vector2(0.35f, 0f);
            lookArea.anchorMax = Vector2.one;
            lookArea.pivot = new Vector2(0.5f, 0.5f);
            lookArea.offsetMin = Vector2.zero;
            lookArea.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f); // Invisible but touchable
            image.raycastTarget = true;

            var trigger = go.AddComponent<EventTrigger>();
            AddEventTrigger(trigger, EventTriggerType.PointerDown, OnLookDown);
            AddEventTrigger(trigger, EventTriggerType.Drag, OnLookDrag);
            AddEventTrigger(trigger, EventTriggerType.PointerUp, OnLookUp);
        }

        private void CreateButtons()
        {
            float buttonY = margin;
            float buttonX = margin;

            float interactBtnSize = buttonSize * 1.25f;
            float actionBtnSize = buttonSize;
            float smallBtnSize = buttonSize * 0.9f;

            // ===== RIGHT SIDE BUTTONS (bottom-right cluster, inside safe area) =====
            interactButton = CreateButton("InteractButton", "USE",
                new Vector2(-buttonX - interactBtnSize, buttonY),
                AnchorPresets.BottomRight, interactBtnSize, interactColor);
            interactButton.transform.SetAsLastSibling();
            var interactTrigger = interactButton.gameObject.AddComponent<EventTrigger>();
            AddEventTrigger(interactTrigger, EventTriggerType.PointerDown, OnInteractDown);
            AddEventTrigger(interactTrigger, EventTriggerType.PointerUp, OnInteractUp);
            AddEventTrigger(interactTrigger, EventTriggerType.PointerExit, OnInteractUp);

            sprintButton = CreateButton("SprintButton", "SPRINT",
                new Vector2(-buttonX - actionBtnSize, buttonY + interactBtnSize + 16f),
                AnchorPresets.BottomRight, actionBtnSize, buttonColor);
            sprintButton.transform.SetAsLastSibling();
            var sprintTrigger = sprintButton.gameObject.AddComponent<EventTrigger>();
            AddEventTrigger(sprintTrigger, EventTriggerType.PointerDown, OnSprintDown);
            AddEventTrigger(sprintTrigger, EventTriggerType.PointerUp, OnSprintUp);
            AddEventTrigger(sprintTrigger, EventTriggerType.PointerExit, OnSprintUp);

            jumpButton = CreateButton("JumpButton", "JUMP",
                new Vector2(-buttonX - interactBtnSize - actionBtnSize - 20f, buttonY),
                AnchorPresets.BottomRight, actionBtnSize, buttonColor);
            jumpButton.transform.SetAsLastSibling();
            var jumpTrigger = jumpButton.gameObject.AddComponent<EventTrigger>();
            AddEventTrigger(jumpTrigger, EventTriggerType.PointerDown, OnJumpDown);

            // ===== TOP RIGHT: VIEW toggle =====
            viewToggleButton = CreateButton("ViewToggleButton", "VIEW",
                new Vector2(-buttonX - smallBtnSize, -buttonY - smallBtnSize),
                AnchorPresets.TopRight, smallBtnSize, buttonColor);
            viewToggleButton.transform.SetAsLastSibling();
            var viewTrigger = viewToggleButton.gameObject.AddComponent<EventTrigger>();
            AddEventTrigger(viewTrigger, EventTriggerType.PointerDown, OnViewDown);

            // ===== TOP LEFT: PAUSE =====
            pauseButton = CreateButton("PauseButton", "II",
                new Vector2(buttonX + smallBtnSize, -buttonY - smallBtnSize),
                AnchorPresets.TopLeft, smallBtnSize, buttonColor);
            pauseButton.transform.SetAsLastSibling();
            var pauseTrigger = pauseButton.gameObject.AddComponent<EventTrigger>();
            AddEventTrigger(pauseTrigger, EventTriggerType.PointerDown, OnPauseDown);
        }

        private Button CreateButton(string name, string label, Vector2 anchoredPos, AnchorPresets anchor, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(safeContainer, false);
            go.layer = 5;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);
            SetAnchors(rt, anchor);
            rt.anchoredPosition = anchoredPos;

            var bgImage = go.GetComponent<Image>();
            bgImage.color = color;
            bgImage.type = Image.Type.Simple; // null-sprite safe on Android
            bgImage.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = buttonPressedColor;
            colors.selectedColor = Color.white;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.05f; // instant feedback
            button.colors = colors;
            // Keep Button.onClick empty — EventTrigger gives instant down/up response.

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            labelGo.layer = 5;
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            var tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = size > 110f ? 26 : 20;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            return button;
        }

        // ===== Joystick Events (math in joystick-local space — no canvas mixing) =====
        private void OnJoystickDown(BaseEventData eventData)
        {
            if (eventData is PointerEventData ped && joystickPointerId == -1)
            {
                joystickPointerId = ped.pointerId;
                UpdateJoystick(ped);
            }
        }

        private void OnJoystickDrag(BaseEventData eventData)
        {
            if (eventData is PointerEventData ped && ped.pointerId == joystickPointerId)
            {
                UpdateJoystick(ped);
            }
        }

        private void OnJoystickUp(BaseEventData eventData)
        {
            // PointerExit arrives as BaseEventData without pointerId — always release
            // when we have an active stick touch and this is up/exit.
            bool release = false;
            if (eventData is PointerEventData ped) release = (ped.pointerId == joystickPointerId);
            else release = (joystickPointerId != -1);
            if (release && joystickPointerId != -1)
            {
                joystickPointerId = -1;
                joystickOutput = Vector2.zero;
                if (joystickHandle != null) joystickHandle.anchoredPosition = Vector2.zero;
                if (fpsController == null) ResolveReferences();
                fpsController?.SetMobileMoveInput(Vector2.zero);
            }
        }

        private void UpdateJoystick(PointerEventData ped)
        {
            if (joystickBackground == null) return;
            // Convert the touch to joystick-local coordinates (origin = bg center).
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                joystickBackground, ped.position, null, out Vector2 local))
            {
                // Overlay canvas: worldCamera must be null.
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    joystickBackground, ped.position, canvas.worldCamera, out local);
            }

            float maxRadius = (joystickBackground.rect.width * 0.5f);
            if (maxRadius <= 0.01f) maxRadius = joystickSize * 0.5f;
            Vector2 offset = local;
            // joystickBackground pivot is bottom-left; recenter to bg middle.
            offset -= joystickBackground.rect.center;

            if (offset.magnitude > maxRadius)
                offset = offset.normalized * maxRadius;

            if (joystickHandle != null) joystickHandle.anchoredPosition = offset;

            // Deadzone so resting thumbs don't drift.
            Vector2 normalized = offset / maxRadius;
            if (normalized.magnitude < 0.12f) normalized = Vector2.zero;
            else normalized = Vector2.ClampMagnitude(normalized, 1f);

            // Screen up = forward: joystick Y maps directly to move Y.
            joystickOutput = normalized * moveSensitivity;
            if (fpsController == null) ResolveReferences();
            fpsController?.SetMobileMoveInput(joystickOutput);
        }

        // ===== Look Area Events (raw pixels — DesktopFPSController scales once) =====
        private void OnLookDown(BaseEventData eventData)
        {
            if (eventData is PointerEventData ped && lookPointerId == -1)
            {
                lookPointerId = ped.pointerId;
                lookLastPosition = ped.position;
            }
        }

        private void OnLookDrag(BaseEventData eventData)
        {
            if (eventData is PointerEventData ped && ped.pointerId == lookPointerId)
            {
                Vector2 delta = ped.position - lookLastPosition;
                lookLastPosition = ped.position;
                if (fpsController == null) ResolveReferences();
                // Pass RAW pixels; DesktopFPSController applies mobileLookSensitivity.
                fpsController?.AddMobileLookInput(delta);
            }
        }

        private void OnLookUp(BaseEventData eventData)
        {
            if (eventData is PointerEventData ped && ped.pointerId == lookPointerId)
            {
                lookPointerId = -1;
            }
        }

        // ===== Button Events (instant — drive existing gameplay functions) =====
        private void OnInteractDown(BaseEventData eventData)
        {
            if (fpsController == null || interactor == null) ResolveReferences();
            // Pin pull / pickup / door / panel: same call desktop E makes.
            fpsController?.PressMobileInteract();
            interactor?.TriggerInteraction();
            // Spray / hold: same path as desktop LMB hold.
            fpsController?.SetMobileInteractHold(true);
            interactor?.SetMobileHold(true);
            FlashButton(interactButton);
        }

        private void OnInteractUp(BaseEventData eventData)
        {
            fpsController?.SetMobileInteractHold(false);
            interactor?.SetMobileHold(false);
        }

        private void OnSprintDown(BaseEventData eventData)
        {
            if (fpsController == null) ResolveReferences();
            fpsController?.SetMobileSprint(true);
            FlashButton(sprintButton);
        }

        private void OnSprintUp(BaseEventData eventData)
        {
            fpsController?.SetMobileSprint(false);
        }

        private void OnJumpDown(BaseEventData eventData)
        {
            if (fpsController == null) ResolveReferences();
            // Same function desktop Space triggers.
            fpsController?.PressMobileJump();
            FlashButton(jumpButton);
        }

        private void OnViewDown(BaseEventData eventData)
        {
            if (fpsController == null || viewSwitcher == null) ResolveReferences();
            // Same function desktop V triggers.
            fpsController?.PressMobileViewToggle();
            if (viewSwitcher != null) viewSwitcher.ToggleView();
            else fpsController?.GetComponent<PlayerViewSwitcher>()?.ToggleView();
            FlashButton(viewToggleButton);
        }

        private void OnPauseDown(BaseEventData eventData)
        {
            FlashButton(pauseButton);
            // Tutorial + TrainingRoom + Assessment share ScenarioPauseMenu (Tab equivalent).
            var pause = Object.FindFirstObjectByType<ScenarioPauseMenu>();
            if (pause != null)
            {
                if (pause.IsPaused) pause.Resume();
                else pause.ShowPause();
                return;
            }
            // Legacy training UI fallback.
            var trainingRoomUI = TrainingRoomUI.Instance;
            if (trainingRoomUI != null)
            {
                trainingRoomUI.TogglePauseModal();
            }
        }

        private void FlashButton(Button btn)
        {
            if (btn != null && btn.targetGraphic != null)
                btn.targetGraphic.CrossFadeColor(buttonPressedColor, 0.05f, true, true);
        }

        private void SetAnchors(RectTransform rt, AnchorPresets preset)
        {
            switch (preset)
            {
                case AnchorPresets.BottomLeft:
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.zero;
                    rt.pivot = new Vector2(0f, 0f);
                    break;
                case AnchorPresets.BottomRight:
                    // Bottom-right corner: anchors (1,0), NOT (1,1) — (1,1) is the
                    // TOP-right and pushes buttons off-screen above the display.
                    rt.anchorMin = new Vector2(1f, 0f);
                    rt.anchorMax = new Vector2(1f, 0f);
                    rt.pivot = new Vector2(1f, 0f);
                    break;
                case AnchorPresets.TopLeft:
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    break;
                case AnchorPresets.TopRight:
                    rt.anchorMin = Vector2.one;
                    rt.anchorMax = Vector2.one;
                    rt.pivot = Vector2.one;
                    break;
            }
        }

        private void AddEventTrigger(EventTrigger trigger, EventTriggerType type, System.Action<BaseEventData> callback)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener((data) => callback(data));
            trigger.triggers.Add(entry);
        }

        private enum AnchorPresets
        {
            BottomLeft,
            BottomRight,
            TopLeft,
            TopRight
        }

        public void SetActiveMobileHUD(bool active)
        {
            gameObject.SetActive(active && isMobilePlatform && IsGameplayScene());
        }
    }
}

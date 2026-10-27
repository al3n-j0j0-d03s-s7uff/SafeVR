using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SafeVR.Core;
using SafeVR.Interaction;
using SafeVR.Player;
using SafeVR.Scenario;
using SafeVR.Scenario.Equipment;

namespace SafeVR.Tutorial
{
    /// <summary>
    /// Standalone step-by-step trainer for the Tutorial scene. Tracks real player actions
    /// (movement, sprint, view switch, interact, door, extinguisher, fire, navigation) using
    /// the same components as TrainingRoom/Assessment. Completion is only granted after ALL
    /// required steps finish. Also carries its own exit-pad trigger (OnTriggerEnter).
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        public const string PracticePanelName = "PracticePanel";

        [Header("Refs (wired by builder)")]
        [SerializeField] private TMP_Text stepCounterText;
        [SerializeField] private TMP_Text stepTitleText;
        [SerializeField] private TMP_Text stepDescText;
        [SerializeField] private GameObject finishPanel;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private DoorController practiceDoor;
        [SerializeField] private FireController practiceFire;

        private DesktopFPSController fps;
        private PlayerViewSwitcher viewSwitcher;
        private Interactor interactor;
        private HeldExtinguisher heldExtinguisher;

        private Vector3 startPos;
        private Vector3 lastPos;
        private float sprintDistance;
        private bool thirdPersonSeen;

        private readonly string[] stepTitles =
        {
            "Move",
            "Sprint",
            "Switch Camera View",
            "Interact",
            "Open the Door",
            "Equip the Extinguisher",
            "Pull Pin & Extinguish the Practice Fire",
            "Navigate to the Exit Pad"
        };

        private readonly string[] stepDescsDesktop =
        {
            "Walk with [WASD] / arrows — move a few meters.",
            "Hold [Left Shift] while moving to sprint.",
            "Press [V] to switch to Third Person view.",
            "Aim at the Practice Panel and press [E] / LMB.",
            "Aim at the practice door and press [E] to open it.",
            "Aim at the extinguisher station and press [E] to pick it up.",
            "Aim at the fire base, press [E] to pull the pin, then HOLD [E] / LMB and sweep side-to-side until it is out.",
            "Walk onto the glowing green exit pad."
        };

        private readonly string[] stepDescsMobile =
        {
            "Drag the left joystick to move — move a few meters.",
            "Hold the SPRINT button while moving to sprint.",
            "Tap the VIEW button to switch to Third Person view.",
            "Aim at the Practice Panel and tap INTERACT.",
            "Aim at the practice door and tap INTERACT to open it.",
            "Aim at the extinguisher station and tap INTERACT to pick it up.",
            "Aim at the fire base, tap INTERACT to pull the pin, then HOLD INTERACT and sweep side-to-side until it is out.",
            "Walk onto the glowing green exit pad."
        };

        private int currentStep;
        private bool finished;
        public int CurrentStep => currentStep;
        public int TotalSteps => stepTitles.Length;
        public bool IsFinished => finished;

        private void Awake()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                fps = player.GetComponent<DesktopFPSController>();
                viewSwitcher = player.GetComponent<PlayerViewSwitcher>();
                interactor = player.GetComponentInChildren<Interactor>();
            }
            if (practiceDoor == null) practiceDoor = FindFirstObjectByType<DoorController>();
            if (practiceFire == null) practiceFire = FindFirstObjectByType<FireController>();
            var heldAll = FindObjectsByType<HeldExtinguisher>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (heldAll.Length > 0) heldExtinguisher = heldAll[0];

            if (viewSwitcher != null) viewSwitcher.OnViewChanged += HandleViewChanged;
            if (interactor != null) interactor.OnInteracted += HandleInteracted;
            if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
            if (menuButton != null) menuButton.onClick.AddListener(OnMenu);
            if (finishPanel != null) finishPanel.SetActive(false);
        }

        private void Start()
        {
            if (fps != null) startPos = fps.transform.position;
            lastPos = startPos;
            practiceFire?.Ignite();
            UpdateObjectiveUI();
        }

        private void OnDestroy()
        {
            if (viewSwitcher != null) viewSwitcher.OnViewChanged -= HandleViewChanged;
            if (interactor != null) interactor.OnInteracted -= HandleInteracted;
        }

        private void Update()
        {
            if (finished || fps == null) return;

            Vector3 pos = fps.transform.position;
            Vector3 flat = new Vector3(pos.x - lastPos.x, 0f, pos.z - lastPos.z);
            float frameDist = flat.magnitude;
            float speed = frameDist / Mathf.Max(Time.deltaTime, 0.0001f);

            switch (currentStep)
            {
                case 0: // Move
                    if (FlatDistance(startPos, pos) > 3f) CompleteStep();
                    break;
                case 1: // Sprint
                    if (IsSprintHeld() && speed > 3f)
                    {
                        sprintDistance += frameDist;
                        if (sprintDistance >= 2f) CompleteStep();
                    }
                    break;
                case 2: // Switch view
                    if (thirdPersonSeen) CompleteStep();
                    break;
                case 4: // Open door
                    if (practiceDoor != null && practiceDoor.IsOpen) CompleteStep();
                    break;
                case 5: // Equip extinguisher
                    if (heldExtinguisher != null && heldExtinguisher.IsActive) CompleteStep();
                    break;
                case 6: // Extinguish fire
                    if (practiceFire != null && practiceFire.IsExtinguished) CompleteStep();
                    break;
                case 7: // Navigate to exit pad — distance fallback so completion
                    // never depends solely on physics trigger delivery.
                    // Manager lives on the TutorialExit object, so its position is the pad.
                    if (FlatDistance(transform.position, pos) < 1.4f) CompleteStep();
                    break;
            }

            lastPos = pos;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (finished || currentStep != 7) return;
            bool isPlayer = other.CompareTag("Player")
                || other.GetComponentInParent<DesktopFPSController>() != null
                || other.GetComponent<DesktopFPSController>() != null;
            if (isPlayer) CompleteStep();
        }

        private void HandleViewChanged(PlayerViewSwitcher.ViewMode mode)
        {
            if (mode == PlayerViewSwitcher.ViewMode.ThirdPerson) thirdPersonSeen = true;
        }

        private void HandleInteracted(SafeVR.Interaction.IInteractable interactable)
        {
            if (finished || currentStep != 3 || interactable == null) return;
            var mb = interactable as MonoBehaviour;
            if (mb != null && mb.gameObject.name == PracticePanelName) CompleteStep();
        }

        private void CompleteStep()
        {
            SoundEffectManager.Instance?.PlayInteract();
            currentStep++;
            if (currentStep >= TotalSteps)
            {
                FinishTutorial();
                return;
            }
            UpdateObjectiveUI();
        }

        private void FinishTutorial()
        {
            finished = true;
            TrainingProgress.TutorialCompleted = true;
            SoundEffectManager.Instance?.PlaySuccess();
            if (finishPanel != null) finishPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (fps != null) { fps.CanMove = false; fps.CanLook = false; }
            UpdateObjectiveUI();
        }

        private void UpdateObjectiveUI()
        {
            int shown = Mathf.Min(currentStep + 1, TotalSteps);
            if (stepCounterText != null) stepCounterText.text = finished ? "Tutorial Complete!" : $"Step {shown} / {TotalSteps}";
            if (stepTitleText != null) stepTitleText.text = finished ? "Well done!" : stepTitles[Mathf.Min(currentStep, TotalSteps - 1)];
            if (stepDescText != null) stepDescText.text = finished
                ? "All steps completed. Continue to TrainingRoom or return to MainMenu."
                : GetStepDescription(currentStep);
        }

        private string GetStepDescription(int stepIndex)
        {
            bool isMobile = Application.isMobilePlatform;
            return isMobile ? stepDescsMobile[stepIndex] : stepDescsDesktop[stepIndex];
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private bool IsSprintHeld()
        {
            // Check via Input System Sprint action (works for both desktop and mobile)
            var fpsController = FindFirstObjectByType<DesktopFPSController>();
            if (fpsController != null)
            {
                // Check if moving faster than walk speed
                Vector3 pos = fpsController.transform.position;
                Vector3 flat = new Vector3(pos.x - lastPos.x, 0f, pos.z - lastPos.z);
                float frameDist = flat.magnitude;
                float currentSpeed = frameDist / Mathf.Max(Time.deltaTime, 0.0001f);
                return currentSpeed > 4.5f; // sprintSpeed is 7, walkSpeed is 4
            }
#if ENABLE_INPUT_SYSTEM
            return UnityEngine.InputSystem.Keyboard.current != null
                && UnityEngine.InputSystem.Keyboard.current.leftShiftKey.isPressed;
#else
            return Input.GetKey(KeyCode.LeftShift);
#endif
        }

        private void OnContinue()
        {
            SoundEffectManager.Instance?.PlayClick();
            SceneFlowManager.Instance?.LoadTrainingRoom();
        }

        private void OnMenu()
        {
            SoundEffectManager.Instance?.PlayClick();
            SceneFlowManager.Instance?.LoadMainMenu();
        }
    }
}

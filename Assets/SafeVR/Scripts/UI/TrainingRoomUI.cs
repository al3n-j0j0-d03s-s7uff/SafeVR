using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SafeVR.Core;
using SafeVR.Training;

namespace SafeVR.UI
{
    /// <summary>
    /// Manages the Training Room HUD, objective checklist, inspection popups, and completion state.
    /// </summary>
    public class TrainingRoomUI : MonoBehaviour
    {
        public static TrainingRoomUI Instance { get; private set; }

        [Header("Objective Checklist")]
        [SerializeField] private TMP_Text objective1Text;
        [SerializeField] private TMP_Text objective2Text;
        [SerializeField] private TMP_Text objective3Text;

        [Header("Equipment Inspection Modal")]
        [SerializeField] private GameObject inspectionModal;
        [SerializeField] private TMP_Text inspectionTitleText;
        [SerializeField] private TMP_Text inspectionBodyText;
        [SerializeField] private Button closeInspectionButton;

        [Header("Completion Modal")]
        [SerializeField] private GameObject completionModal;
        [SerializeField] private Button proceedToAssessmentButton;
        [SerializeField] private Button returnToMenuButton;

        [Header("Pause / Menu Modal")]
        [SerializeField] private GameObject pauseModal;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseQuitToMenuButton;

        private Player.DesktopFPSController playerController;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            playerController = FindFirstObjectByType<Player.DesktopFPSController>();
        }

        private void Start()
        {
            if (inspectionModal != null) inspectionModal.SetActive(false);
            if (completionModal != null) completionModal.SetActive(false);
            if (pauseModal != null) pauseModal.SetActive(false);

            if (closeInspectionButton != null)
                closeInspectionButton.onClick.AddListener(CloseInspectionModal);

            if (proceedToAssessmentButton != null)
                proceedToAssessmentButton.onClick.AddListener(OnProceedToAssessmentClicked);

            if (returnToMenuButton != null)
                returnToMenuButton.onClick.AddListener(OnReturnToMenuClicked);

            if (resumeButton != null)
                resumeButton.onClick.AddListener(ClosePauseModal);

            if (pauseQuitToMenuButton != null)
                pauseQuitToMenuButton.onClick.AddListener(OnReturnToMenuClicked);

            if (TrainingManager.Instance != null)
            {
                TrainingManager.Instance.OnObjectiveUpdated += HandleObjectiveUpdated;
            }

            UpdateChecklistVisuals();
        }

        private void Update()
        {
            // Escape to toggle pause modal when no other modal is open
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame)
            {
                TogglePauseModal();
            }
#else
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                TogglePauseModal();
            }
#endif
        }

        private void HandleObjectiveUpdated(TrainingObjective obj)
        {
            UpdateChecklistVisuals();
        }

        public void UpdateChecklistVisuals()
        {
            if (TrainingManager.Instance == null) return;
            var list = TrainingManager.Instance.GetObjectives();

            if (list.Count > 0 && objective1Text != null)
                objective1Text.text = FormatObjective(list[0]);
            if (list.Count > 1 && objective2Text != null)
                objective2Text.text = FormatObjective(list[1]);
            if (list.Count > 2 && objective3Text != null)
                objective3Text.text = FormatObjective(list[2]);
        }

        private string FormatObjective(TrainingObjective obj)
        {
            if (obj.isCompleted)
            {
                return $"<color=#00FF88>✓ [DONE]</color> <s>{obj.title}</s>";
            }
            return $"<color=#FFFFFF>○ [TODO]</color> {obj.title}";
        }

        public void ShowEquipmentInspection(string title, string body)
        {
            if (inspectionModal == null) return;

            if (inspectionTitleText != null) inspectionTitleText.text = title;
            if (inspectionBodyText != null) inspectionBodyText.text = body;

            inspectionModal.SetActive(true);

            if (playerController != null)
            {
                playerController.CanLook = false;
                playerController.CanMove = false;
                playerController.SetCursorLocked(false);
            }
        }

        public void CloseInspectionModal()
        {
            SoundEffectManager.Instance?.PlayClick();
            if (inspectionModal != null) inspectionModal.SetActive(false);

            if (playerController != null && (completionModal == null || !completionModal.activeSelf))
            {
                playerController.CanLook = true;
                playerController.CanMove = true;
                playerController.SetCursorLocked(true);
            }
        }

        public void ShowTrainingCompleted()
        {
            if (completionModal == null) return;
            completionModal.SetActive(true);

            if (playerController != null)
            {
                playerController.CanLook = false;
                playerController.CanMove = false;
                playerController.SetCursorLocked(false);
            }
        }

public void TogglePauseModal()
        {
            if (pauseModal == null) return;
            bool targetState = !pauseModal.activeSelf;
            pauseModal.SetActive(targetState);

            if (playerController != null)
            {
                playerController.CanLook = !targetState;
                playerController.CanMove = !targetState;
                playerController.SetCursorLocked(!targetState);
            }
        }

        public void ClosePauseModal()
        {
            SoundEffectManager.Instance?.PlayClick();
            if (pauseModal != null) pauseModal.SetActive(false);

            if (playerController != null)
            {
                playerController.CanLook = true;
                playerController.CanMove = true;
                playerController.SetCursorLocked(true);
            }
        }

        private void OnProceedToAssessmentClicked()
        {
            SoundEffectManager.Instance?.PlayClick();
            SceneFlowManager.Instance?.LoadAssessment();
        }

        private void OnReturnToMenuClicked()
        {
            SoundEffectManager.Instance?.PlayClick();
            SceneFlowManager.Instance?.LoadMainMenu();
        }

        private void OnDestroy()
        {
            if (TrainingManager.Instance != null)
            {
                TrainingManager.Instance.OnObjectiveUpdated -= HandleObjectiveUpdated;
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using SafeVR.Core;

namespace SafeVR.UI
{
    /// <summary>
    /// UI controller for the MainMenu scene.
    /// Manages Start Training, Assessment, and Quit buttons with audio feedback.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Menu Buttons")]
        [SerializeField] private Button startTrainingButton;
        [SerializeField] private Button tutorialButton;
        [SerializeField] private Button assessmentButton;
        [SerializeField] private Button quitButton;

        private void Awake()
        {
            // Ensure SceneFlowManager and SoundEffectManager exist
            if (FindFirstObjectByType<SceneFlowManager>() == null)
            {
                new GameObject("SceneFlowManager", typeof(SceneFlowManager));
            }
            if (FindFirstObjectByType<SoundEffectManager>() == null)
            {
                new GameObject("SoundEffectManager", typeof(SoundEffectManager));
            }
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (startTrainingButton != null)
            {
                startTrainingButton.onClick.AddListener(OnStartTrainingClicked);
            }

            if (tutorialButton != null)
            {
                tutorialButton.onClick.AddListener(OnTutorialClicked);
            }

            if (assessmentButton != null)
            {
                assessmentButton.onClick.AddListener(OnAssessmentClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitClicked);
            }
        }

        private void OnStartTrainingClicked()
        {
            SoundEffectManager.Instance?.PlayClick();
            SceneFlowManager.Instance?.LoadTrainingRoom();
        }

        private void OnTutorialClicked()
        {
            SoundEffectManager.Instance?.PlayClick();
            SceneFlowManager.Instance?.LoadTutorial();
        }

        private void OnAssessmentClicked()
        {
            SoundEffectManager.Instance?.PlayClick();
            SceneFlowManager.Instance?.LoadAssessment();
        }

        private void OnQuitClicked()
        {
            SoundEffectManager.Instance?.PlayClick();
            SceneFlowManager.Instance?.QuitGame();
        }
    }
}

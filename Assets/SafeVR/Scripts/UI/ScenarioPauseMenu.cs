using UnityEngine;
using UnityEngine.UI;
using SafeVR.Core;
using SafeVR.Scenario;

namespace SafeVR.UI
{
    /// <summary>
    /// Phase 4 — generic in-game pause + retry menu for scenario scenes.
    /// PAUSED offers Resume / Retry Scenario / Main Menu; retry asks for
    /// confirmation (Restart / Cancel) and then restarts ONLY the current scene
    /// through <see cref="SceneFlowManager"/> (all runtime state is instance
    /// state, so reload resets fire, power, extinguisher, doors, timer, metrics —
    /// while AttemptHistory and completed results are preserved).
    /// Shortcuts: Tab = pause/resume, R = retry, Esc = resume/cancel,
    /// Enter = confirm retry. Shortcuts act only in their valid states so
    /// normal WASD / E / LMB gameplay input is never affected. Public methods
    /// are VR-ready (XR UI buttons can call them directly).
    /// </summary>
    public class ScenarioPauseMenu : MonoBehaviour
    {
        [Header("Pause")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;

        [Header("Retry Confirm")]
        [SerializeField] private GameObject confirmPanel;
        [SerializeField] private Button confirmRestartButton;
        [SerializeField] private Button cancelButton;

        private ScenarioManagerBase scenario;
        private Player.DesktopFPSController fps;
        private bool paused;
        private bool confirmOpen;
        private bool runEnded;

        public bool IsPaused => paused;
        public bool IsConfirmOpen => confirmOpen;

        private void Awake()
        {
            scenario = FindFirstObjectByType<ScenarioManagerBase>();
            fps = FindFirstObjectByType<Player.DesktopFPSController>();
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (retryButton != null) retryButton.onClick.AddListener(OpenRetryConfirm);
            if (menuButton != null) menuButton.onClick.AddListener(GoToMenu);
            if (confirmRestartButton != null) confirmRestartButton.onClick.AddListener(ConfirmRetry);
            if (cancelButton != null) cancelButton.onClick.AddListener(CancelRetry);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (confirmPanel != null) confirmPanel.SetActive(false);
            if (scenario != null) scenario.OnScenarioEnded += HandleRunEnded;
        }

        private void OnDestroy()
        {
            if (scenario != null) scenario.OnScenarioEnded -= HandleRunEnded;
        }

        private void HandleRunEnded(ScenarioMetrics m)
        {
            runEnded = true;
            if (pausePanel != null) pausePanel.SetActive(false);
            if (confirmPanel != null) confirmPanel.SetActive(false);
            paused = false;
            confirmOpen = false;
        }

        private void Update()
        {
            if (runEnded) return;
            if (WasPressedTab())
            {
                if (confirmOpen) return; // Tab never dismisses the confirm dialog
                if (paused) Resume(); else ShowPause();
                return;
            }
            if (WasPressedRetry())
            {
                if (!paused && !confirmOpen) { ShowPause(); OpenRetryConfirm(); }
                else if (paused && !confirmOpen) OpenRetryConfirm();
                return;
            }
            if (WasPressedConfirm() && confirmOpen)
            {
                ConfirmRetry();
                return;
            }
            if (WasPressedEscape())
            {
                if (confirmOpen) CancelRetry();
                else if (paused) Resume();
                // When playing, Esc is left to DesktopFPSController (cursor toggle).
            }
        }

        public void ShowPause()
        {
            if (runEnded || paused) return;
            paused = true;
            Time.timeScale = 0f;
            if (pausePanel != null) pausePanel.SetActive(true);
            if (fps != null) { fps.CanMove = false; fps.CanLook = false; fps.SetCursorLocked(false); }
            else { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            SoundEffectManager.Instance?.PlayClick();
        }

        public void Resume()
        {
            if (!paused) return;
            paused = false;
            confirmOpen = false;
            Time.timeScale = 1f;
            if (pausePanel != null) pausePanel.SetActive(false);
            if (confirmPanel != null) confirmPanel.SetActive(false);
            if (fps != null) { fps.CanMove = true; fps.CanLook = true; fps.SetCursorLocked(true); }
            SoundEffectManager.Instance?.PlayClick();
        }

        public void OpenRetryConfirm()
        {
            if (runEnded || confirmOpen) return;
            if (!paused) ShowPause();
            confirmOpen = true;
            if (confirmPanel != null) confirmPanel.SetActive(true);
            SoundEffectManager.Instance?.PlayClick();
        }

        public void CancelRetry()
        {
            if (!confirmOpen) return;
            confirmOpen = false;
            if (confirmPanel != null) confirmPanel.SetActive(false);
            SoundEffectManager.Instance?.PlayClick();
        }

        /// <summary>Fresh attempt of the current scenario only. History is preserved.</summary>
        public void ConfirmRetry()
        {
            if (!confirmOpen) return;
            SoundEffectManager.Instance?.PlayClick();
            Time.timeScale = 1f;
            ScenarioResetHelper.ResetMetrics();
            SceneFlowManager.Instance?.ReloadCurrentScene();
        }

        public void GoToMenu()
        {
            SoundEffectManager.Instance?.PlayClick();
            Time.timeScale = 1f;
            SceneFlowManager.Instance?.LoadMainMenu();
        }

        private static bool WasPressedTab()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.tabKey.wasPressedThisFrame) return true;
#endif
            return Input.GetKeyDown(KeyCode.Tab);
        }

        private static bool WasPressedRetry()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) return true;
#endif
            return Input.GetKeyDown(KeyCode.R);
        }

        private static bool WasPressedEscape()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
#endif
            return Input.GetKeyDown(KeyCode.Escape);
        }

        private static bool WasPressedConfirm()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.enterKey.wasPressedThisFrame) return true;
            if (kb != null && kb.numpadEnterKey.wasPressedThisFrame) return true;
#endif
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
        }
    }
}

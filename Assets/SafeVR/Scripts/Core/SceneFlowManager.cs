using UnityEngine;
using UnityEngine.SceneManagement;

namespace SafeVR.Core
{
    /// <summary>
    /// Central manager for scene transitions and application lifecycle.
    /// Works with standard scene names: MainMenu, Tutorial, TrainingRoom, Assessment, Results.
    /// </summary>
    public class SceneFlowManager : MonoBehaviour
    {
        public static SceneFlowManager Instance { get; private set; }

        public const string SCENE_MAIN_MENU = "MainMenu";
        public const string SCENE_TUTORIAL = "Tutorial";
        public const string SCENE_TRAINING_ROOM = "TrainingRoom";
        public const string SCENE_ASSESSMENT = "Assessment";
        public const string SCENE_RESULTS = "Results";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(SCENE_MAIN_MENU);
        }

        public void LoadTutorial()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(SCENE_TUTORIAL);
        }

        public void LoadTrainingRoom()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SCENE_TRAINING_ROOM);
        }

        public void LoadAssessment()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(SCENE_ASSESSMENT);
        }

        public void LoadResults()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(SCENE_RESULTS);
        }

        public void ReloadCurrentScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

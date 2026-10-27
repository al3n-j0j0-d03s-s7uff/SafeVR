using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SafeVR.Core;
using SafeVR.Assessment;

namespace SafeVR.UI
{
    /// <summary>
    /// Manages the Assessment interactive cards, option buttons, results review, and scene navigation.
    /// </summary>
    public class AssessmentUI : MonoBehaviour
    {
        [Header("Question Card View")]
        [SerializeField] private GameObject questionCardPanel;
        [SerializeField] private TMP_Text categoryText;
        [SerializeField] private TMP_Text questionCounterText;
        [SerializeField] private TMP_Text questionBodyText;
        [SerializeField] private Button[] optionButtons;
        [SerializeField] private TMP_Text[] optionTexts;

        [Header("Results View")]
        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private TMP_Text resultBadgeText;
        [SerializeField] private TMP_Text scoreSummaryText;
        [SerializeField] private TMP_Text feedbackDetailsText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button returnToMenuButton;

        private void Awake()
        {
            // Ensure SceneFlowManager exists
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

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);

            if (returnToMenuButton != null)
                returnToMenuButton.onClick.AddListener(OnReturnToMenuClicked);

            // Bind option buttons
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int index = i;
                if (optionButtons[i] != null)
                {
                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
                }
            }

            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.OnQuestionChanged += HandleQuestionChanged;
                AssessmentManager.Instance.OnAssessmentFinished += HandleAssessmentFinished;
            }

            if (resultsPanel != null) resultsPanel.SetActive(false);
            if (questionCardPanel != null) questionCardPanel.SetActive(true);
        }

        private void HandleQuestionChanged(AssessmentQuestion q, int currentNum, int total)
        {
            if (questionCardPanel != null) questionCardPanel.SetActive(true);
            if (resultsPanel != null) resultsPanel.SetActive(false);

            if (categoryText != null) categoryText.text = q.category;
            if (questionCounterText != null) questionCounterText.text = $"Task {currentNum} of {total}";
            if (questionBodyText != null) questionBodyText.text = q.questionText;

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (i < q.options.Length)
                {
                    optionButtons[i].gameObject.SetActive(true);
                    if (optionTexts[i] != null)
                    {
                        optionTexts[i].text = $"<b>{((char)('A' + i))}</b>. {q.options[i]}";
                    }
                }
                else
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnOptionSelected(int optionIndex)
        {
            AssessmentManager.Instance?.SubmitAnswer(optionIndex);
        }

        private void HandleAssessmentFinished(AssessmentResult result)
        {
            if (questionCardPanel != null) questionCardPanel.SetActive(false);
            if (resultsPanel != null) resultsPanel.SetActive(true);

            if (result.isPassed)
            {
                if (resultBadgeText != null)
                {
                    resultBadgeText.text = "<color=#00FF88>★ ASSESSMENT PASSED ★</color>";
                }
                if (scoreSummaryText != null)
                {
                    scoreSummaryText.text = $"Score: <b>{result.correctAnswers} / {result.totalQuestions} ({result.scorePercentage:F0}%)</b>\nStatus: Certified Safe Operator";
                }
            }
            else
            {
                if (resultBadgeText != null)
                {
                    resultBadgeText.text = "<color=#FF4444>⚠ ASSESSMENT FAILED ⚠</color>";
                }
                if (scoreSummaryText != null)
                {
                    scoreSummaryText.text = $"Score: <b>{result.correctAnswers} / {result.totalQuestions} ({result.scorePercentage:F0}%)</b>\nStatus: Review Required (Passing grade is 66%)";
                }
            }

            // Build detailed feedback review
            if (feedbackDetailsText != null && AssessmentManager.Instance != null)
            {
                var questions = AssessmentManager.Instance.GetQuestions();
                string review = "<b>Performance Review:</b>\n";
                for (int i = 0; i < questions.Count; i++)
                {
                    bool correct = i < result.answerResults.Count && result.answerResults[i];
                    string mark = correct ? "<color=#00FF88>[CORRECT]</color>" : "<color=#FF4444>[INCORRECT]</color>";
                    review += $"\n{mark} <b>Task {i + 1}:</b> {questions[i].category}\n<i>{questions[i].rationale}</i>\n";
                }
                feedbackDetailsText.text = review;
            }
        }

        private void OnRestartClicked()
        {
            SoundEffectManager.Instance?.PlayClick();
            if (resultsPanel != null) resultsPanel.SetActive(false);
            if (questionCardPanel != null) questionCardPanel.SetActive(true);
            AssessmentManager.Instance?.StartAssessment();
        }

        private void OnReturnToMenuClicked()
        {
            SoundEffectManager.Instance?.PlayClick();
            SceneFlowManager.Instance?.LoadMainMenu();
        }

        private void OnDestroy()
        {
            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.OnQuestionChanged -= HandleQuestionChanged;
                AssessmentManager.Instance.OnAssessmentFinished -= HandleAssessmentFinished;
            }
        }
    }
}

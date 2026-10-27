using System;
using System.Collections.Generic;
using UnityEngine;
using SafeVR.Core;

namespace SafeVR.Assessment
{
    public struct AssessmentResult
    {
        public int totalQuestions;
        public int correctAnswers;
        public float scorePercentage;
        public bool isPassed;
        public List<bool> answerResults;
    }

    /// <summary>
    /// Controls the 3-part interactive safety assessment, computes scores, and determines Pass/Fail.
    /// </summary>
    public class AssessmentManager : MonoBehaviour
    {
        public static AssessmentManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float passThresholdPercentage = 66.0f;

        private List<AssessmentQuestion> questions = new List<AssessmentQuestion>();
        private int currentQuestionIndex = 0;
        private List<int> userSelectedAnswers = new List<int>();

        public event Action<AssessmentQuestion, int, int> OnQuestionChanged;
        public event Action<AssessmentResult> OnAssessmentFinished;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadDefaultQuestions();
        }

        private void Start()
        {
            StartAssessment();
        }

        private void LoadDefaultQuestions()
        {
            questions.Clear();

            questions.Add(new AssessmentQuestion(
                "q1",
                "Fire Safety & Equipment Classification",
                "Which fire extinguisher class is specifically designated and safe to use on energized electrical equipment fires?",
                new string[] {
                    "Class A (Ordinary combustibles: wood, paper, cloth)",
                    "Class B (Flammable liquids: grease, gasoline, oil)",
                    "Class C (Energized electrical equipment)",
                    "Class D (Combustible metals: magnesium, titanium)"
                },
                2,
                "Class C extinguishers use non-conductive agents (such as CO2 or dry chemical) to extinguish fires involving active electrical equipment safely without shock hazard."
            ));

            questions.Add(new AssessmentQuestion(
                "q2",
                "Operating Procedures - P.A.S.S.",
                "In the standardized P.A.S.S. protocol for operating a fire extinguisher, what do the four steps stand for?",
                new string[] {
                    "Push, Aim, Squeeze, Stand",
                    "Pull (pin), Aim (base), Squeeze (lever), Sweep (side-to-side)",
                    "Point (fire), Alert (team), Spray (fog), Stop",
                    "Pull (alarm), Actuate (valve), Secure (exit), Step back"
                },
                1,
                "P.A.S.S. stands for: Pull the safety pin, Aim low at the base of the fire, Squeeze the handle lever, and Sweep from side to side."
            ));

            questions.Add(new AssessmentQuestion(
                "q3",
                "Immediate Hazard Mitigation",
                "Upon identifying an active workplace hazard (e.g. sparking electrical panel or chemical leak), what is the first immediate priority?",
                new string[] {
                    "Attempt to rinse the sparking wires with water",
                    "Isolate the power source / alert personnel in the area and sound the alarm",
                    "Ignore it and wait for maintenance to notice during their shift",
                    "Document the scene with a smartphone photo before warning anyone"
                },
                1,
                "Immediate containment via emergency cutoff and warning nearby personnel protects lives and prevents fire propagation before secondary mitigation."
            ));
        }

        public void StartAssessment()
        {
            currentQuestionIndex = 0;
            userSelectedAnswers.Clear();
            DisplayCurrentQuestion();
        }

        public void SubmitAnswer(int optionIndex)
        {
            SoundEffectManager.Instance?.PlayClick();
            userSelectedAnswers.Add(optionIndex);

            currentQuestionIndex++;
            if (currentQuestionIndex < questions.Count)
            {
                DisplayCurrentQuestion();
            }
            else
            {
                CalculateAndFinish();
            }
        }

        private void DisplayCurrentQuestion()
        {
            if (currentQuestionIndex < questions.Count)
            {
                OnQuestionChanged?.Invoke(questions[currentQuestionIndex], currentQuestionIndex + 1, questions.Count);
            }
        }

        private void CalculateAndFinish()
        {
            int correctCount = 0;
            List<bool> answerResults = new List<bool>();

            for (int i = 0; i < questions.Count; i++)
            {
                bool correct = (i < userSelectedAnswers.Count) && (userSelectedAnswers[i] == questions[i].correctOptionIndex);
                if (correct) correctCount++;
                answerResults.Add(correct);
            }

            float percentage = ((float)correctCount / questions.Count) * 100f;
            bool passed = percentage >= passThresholdPercentage;

            // Save to session progress
            TrainingProgress.LastAssessmentScore = correctCount;
            TrainingProgress.LastAssessmentTotal = questions.Count;
            TrainingProgress.LastAssessmentPassed = passed;

            if (passed)
            {
                SoundEffectManager.Instance?.PlaySuccess();
            }
            else
            {
                SoundEffectManager.Instance?.PlayFailure();
            }

            AssessmentResult result = new AssessmentResult
            {
                totalQuestions = questions.Count,
                correctAnswers = correctCount,
                scorePercentage = percentage,
                isPassed = passed,
                answerResults = answerResults
            };

            OnAssessmentFinished?.Invoke(result);
        }

        public IReadOnlyList<AssessmentQuestion> GetQuestions() => questions;
    }
}

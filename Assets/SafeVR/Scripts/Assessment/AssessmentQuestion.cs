using System;

namespace SafeVR.Assessment
{
    [Serializable]
    public class AssessmentQuestion
    {
        public string questionId;
        public string category;
        public string questionText;
        public string[] options;
        public int correctOptionIndex;
        public string rationale;

        public AssessmentQuestion(string id, string category, string text, string[] options, int correctIndex, string rationale)
        {
            this.questionId = id;
            this.category = category;
            this.questionText = text;
            this.options = options;
            this.correctOptionIndex = correctIndex;
            this.rationale = rationale;
        }
    }
}

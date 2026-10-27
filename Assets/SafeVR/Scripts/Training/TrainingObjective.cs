using System;

namespace SafeVR.Training
{
    [Serializable]
    public class TrainingObjective
    {
        public string id;
        public string title;
        public string description;
        public bool isCompleted;

        public TrainingObjective(string id, string title, string description)
        {
            this.id = id;
            this.title = title;
            this.description = description;
            this.isCompleted = false;
        }
    }
}

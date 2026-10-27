using System;
using System.Collections.Generic;
using UnityEngine;
using SafeVR.Scenario;

namespace SafeVR.Core
{
    /// <summary>
    /// Phase 4 — lightweight local attempt history (PlayerPrefs, no external deps).
    /// Keeps the last <see cref="MaxKept"/> completed attempts per scenario key and
    /// exposes current / previous / best / improvement for the Results UI.
    /// History is append-only across retries: a retry starts a fresh attempt and
    /// never deletes previously completed results.
    /// </summary>
    public static class AttemptHistory
    {
        public const int MaxKept = 10;

        [Serializable]
        public class AttemptRecord
        {
            public string date = "";
            public string scenario = "";
            public int score;
            public string grade = "";
            public float time;
            public int unsafeCount;
        }

        [Serializable]
        private class AttemptList
        {
            public List<AttemptRecord> items = new List<AttemptRecord>();
        }

        private static string KeyFor(string scenarioKey) => $"SafeVR_History_{scenarioKey}";

        public static string KeyForMetrics(ScenarioMetrics m)
        {
            if (m == null) return "Unknown";
            return m.scenarioType.ToString();
        }

        public static void Record(string scenarioKey, ScenarioMetrics m)
        {
            if (m == null || string.IsNullOrEmpty(scenarioKey)) return;
            var list = LoadRaw(scenarioKey);
            list.Add(new AttemptRecord
            {
                date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                scenario = scenarioKey,
                score = (int)m.SafetyScore,
                grade = m.Grade,
                time = m.elapsedTime,
                unsafeCount = m.unsafeActions
            });
            while (list.Count > MaxKept) list.RemoveAt(0);
            SaveRaw(scenarioKey, list);
        }

        public static List<AttemptRecord> GetHistory(string scenarioKey)
        {
            return LoadRaw(scenarioKey ?? "");
        }

        public static AttemptRecord GetLast(string scenarioKey)
        {
            var list = LoadRaw(scenarioKey ?? "");
            return list.Count > 0 ? list[list.Count - 1] : null;
        }

        public static AttemptRecord GetPrevious(string scenarioKey)
        {
            var list = LoadRaw(scenarioKey ?? "");
            return list.Count > 1 ? list[list.Count - 2] : null;
        }

        public static AttemptRecord GetBest(string scenarioKey)
        {
            AttemptRecord best = null;
            foreach (var r in LoadRaw(scenarioKey ?? ""))
                if (best == null || r.score > best.score) best = r;
            return best;
        }

        /// <summary>Current minus previous score; null on first attempt.</summary>
        public static int? GetImprovement(string scenarioKey)
        {
            var last = GetLast(scenarioKey);
            var prev = GetPrevious(scenarioKey);
            if (last == null || prev == null) return null;
            return last.score - prev.score;
        }

        private static List<AttemptRecord> LoadRaw(string scenarioKey)
        {
            if (string.IsNullOrEmpty(scenarioKey)) return new List<AttemptRecord>();
            try
            {
                string json = PlayerPrefs.GetString(KeyFor(scenarioKey), "");
                if (string.IsNullOrEmpty(json)) return new List<AttemptRecord>();
                var wrapper = JsonUtility.FromJson<AttemptList>(json);
                return wrapper?.items ?? new List<AttemptRecord>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SafeVR] AttemptHistory load failed for {scenarioKey}: {e.Message}");
                return new List<AttemptRecord>();
            }
        }

        private static void SaveRaw(string scenarioKey, List<AttemptRecord> list)
        {
            try
            {
                var wrapper = new AttemptList { items = list };
                PlayerPrefs.SetString(KeyFor(scenarioKey), JsonUtility.ToJson(wrapper));
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SafeVR] AttemptHistory save failed for {scenarioKey}: {e.Message}");
            }
        }
    }
}

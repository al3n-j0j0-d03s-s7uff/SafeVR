using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SafeVR.Scenario;

namespace SafeVR.Core
{
    /// <summary>
    /// Phase 4 — builds the performance-review sections from actual scenario data
    /// (ScenarioMetrics + action log). No fabricated events: the timeline parses
    /// real log timestamps, objectives read real metric flags, and unsafe reviews
    /// explain real logged mistakes. Shared by PerformanceReportUI and ResultsUI.
    /// </summary>
    public static class PerformanceReview
    {
        public struct ObjectiveItem
        {
            public string label;
            public bool done;
        }

        public struct TimelineItem
        {
            public string time;
            public float seconds;
            public string text;
            public bool isUnsafe;
        }

        public struct UnsafeReviewItem
        {
            public string time;
            public string action;
            public string reason;
            public string recommendation;
        }

        private struct UnsafeRule
        {
            public string[] keywords;
            public string reason;
            public string recommendation;
        }

        // Keyword → explanation table. First match wins; matching is case-insensitive.
        private static readonly UnsafeRule[] UnsafeRules = new UnsafeRule[]
        {
            new UnsafeRule {
                keywords = new[] { "without isolating power", "isolate", "energized" },
                reason = "Electrical equipment was still energized.",
                recommendation = "Isolate the electrical supply before attempting suppression." },
            new UnsafeRule {
                keywords = new[] { "wrong extinguisher", "water on electrical", "water conducts" },
                reason = "Water conducts electricity and is the wrong agent for an electrical fire.",
                recommendation = "Review extinguisher selection for different fire types — use CO2 (Class C) on electrical fires." },
            new UnsafeRule {
                keywords = new[] { "without inspecting" },
                reason = "The extinguisher label was not checked before selection.",
                recommendation = "Always inspect the extinguisher label and class before selecting." },
            new UnsafeRule {
                keywords = new[] { "safety pin", "without removing the pin" },
                reason = "The extinguisher cannot discharge with the safety pin in place.",
                recommendation = "Follow P.A.S.S.: pull the pin only once aimed at the fire, then squeeze." },
            new UnsafeRule {
                keywords = new[] { "empty extinguisher", "extinguisher empty", "emptied" },
                reason = "The unit had no charge left, so continued attempts could do nothing.",
                recommendation = "Watch the charge gauge and make each discharge count — restart for a fresh unit." },
            new UnsafeRule {
                keywords = new[] { "wasting", "aim", "sweep" },
                reason = "Discharge was not landing on the base of the fire.",
                recommendation = "Improve aim and sweep technique: aim low at the base and sweep side-to-side." },
            new UnsafeRule {
                keywords = new[] { "blocked exit" },
                reason = "The primary exit was obstructed and could not be used.",
                recommendation = "Improve evacuation response and route recognition — always identify an alternative route." },
            new UnsafeRule {
                keywords = new[] { "before fire was controlled", "without controlling fire", "evacuat" },
                reason = "Evacuation began before the emergency was under control.",
                recommendation = "Control the fire first, then evacuate through the safe route without delay." },
            new UnsafeRule {
                keywords = new[] { "before emergency", "false evacuation" },
                reason = "Action was taken before any emergency existed.",
                recommendation = "Stay observant but keep equipment mounted until the alarm confirms the emergency." },
            new UnsafeRule {
                keywords = new[] { "too long", "time expired", "time exceeded" },
                reason = "Response took too long and the hazard escalated.",
                recommendation = "Respond faster — hazards escalate quickly once the alarm sounds." },
        };

        public static string FormatTime(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int m = (int)(seconds / 60f);
            int s = (int)(seconds % 60f);
            return $"{m:00}:{s:00}";
        }

        /// <summary>Objective checklist derived from real metric flags + log.</summary>
        public static List<ObjectiveItem> BuildObjectives(ScenarioMetrics m)
        {
            var items = new List<ObjectiveItem>();
            if (m == null) return items;
            if (m.scenarioType == ScenarioType.ElectricalFire)
            {
                items.Add(new ObjectiveItem { label = "Power isolated", done = m.powerIsolated });
                items.Add(new ObjectiveItem { label = "Correct extinguisher selected", done = m.extinguisherUsedCorrectly || HasLog(m, "Equipped correct extinguisher") });
                items.Add(new ObjectiveItem { label = "Extinguisher prepared (pin removed)", done = m.pinRemoved });
                items.Add(new ObjectiveItem { label = "Fire suppressed", done = m.extinguisherUsedCorrectly });
                items.Add(new ObjectiveItem { label = "Exit opened", done = m.exitOpened });
                items.Add(new ObjectiveItem { label = "Safe zone reached", done = m.evacuatedSafely });
            }
            else
            {
                items.Add(new ObjectiveItem { label = "Hazard identified", done = m.hazardIdentified });
                items.Add(new ObjectiveItem { label = "Alarm activated", done = m.alarmActivated });
                items.Add(new ObjectiveItem { label = "Alternative route found", done = m.correctExitUsed });
                items.Add(new ObjectiveItem { label = "Exit opened", done = m.exitOpened });
                items.Add(new ObjectiveItem { label = "Safe zone reached", done = m.evacuatedSafely });
            }
            return items;
        }

        /// <summary>Chronological timeline parsed from real log timestamps (mm:ss).</summary>
        public static List<TimelineItem> BuildTimeline(List<string> actionLog)
        {
            var items = new List<TimelineItem>();
            items.Add(new TimelineItem { time = "00:00", seconds = 0f, text = "Scenario started", isUnsafe = false });
            if (actionLog == null) return items;
            var rx = new Regex(@"^\[(.)\]\[([\d.]+)s\]\s?(.*)$");
            foreach (var entry in actionLog)
            {
                if (string.IsNullOrEmpty(entry)) continue;
                var match = rx.Match(entry);
                if (!match.Success) continue;
                float sec;
                if (!float.TryParse(match.Groups[2].Value, out sec)) sec = 0f;
                items.Add(new TimelineItem
                {
                    time = FormatTime(sec),
                    seconds = sec,
                    text = match.Groups[3].Value,
                    isUnsafe = match.Groups[1].Value == "!"
                });
            }
            return items;
        }

        /// <summary>Per-mistake review (time, action, why unsafe, correct behavior).</summary>
        public static List<UnsafeReviewItem> BuildUnsafeReview(List<string> actionLog)
        {
            var items = new List<UnsafeReviewItem>();
            if (actionLog == null) return items;
            var rx = new Regex(@"^\[!\]\[([\d.]+)s\]\s?(.*)$");
            foreach (var entry in actionLog)
            {
                if (string.IsNullOrEmpty(entry)) continue;
                var match = rx.Match(entry);
                if (!match.Success) continue;
                float sec;
                if (!float.TryParse(match.Groups[1].Value, out sec)) sec = 0f;
                string action = match.Groups[2].Value;
                string lower = action.ToLowerInvariant();
                string reason = "This action reduced the safety margin for the run.";
                string recommendation = "Review the correct procedure and avoid repeating this action.";
                foreach (var rule in UnsafeRules)
                {
                    bool hit = false;
                    foreach (var kw in rule.keywords)
                    {
                        if (lower.Contains(kw.ToLowerInvariant())) { hit = true; break; }
                    }
                    if (hit) { reason = rule.reason; recommendation = rule.recommendation; break; }
                }
                items.Add(new UnsafeReviewItem
                {
                    time = FormatTime(sec),
                    action = action,
                    reason = reason,
                    recommendation = recommendation
                });
            }
            return items;
        }

        private static bool HasLog(ScenarioMetrics m, string fragment)
        {
            if (m.actionLog == null) return false;
            foreach (var e in m.actionLog)
                if (!string.IsNullOrEmpty(e) && e.Contains(fragment)) return true;
            return false;
        }
    }
}

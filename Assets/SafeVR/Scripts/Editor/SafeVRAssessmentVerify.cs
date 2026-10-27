using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SafeVR.Scenario;
using SafeVR.Scenario.Equipment;

namespace SafeVR.Editor
{
    public static class SafeVRAssessmentVerify
    {
        public static void VerifyAssessment()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Assessment.unity", OpenSceneMode.Single);
            int fails = 0;
            void Check(bool cond, string name)
            {
                Debug.Log((cond ? "[PASS] " : "[FAIL] ") + name);
                if (!cond) fails++;
            }

            Check(FindIncludingInactive("DoorHinge") != null, "Assessment DoorHinge exists");
            Check(FindIncludingInactive("DoorPanel") != null, "Assessment DoorPanel exists");
            Check(FindIncludingInactive("DoorFrame") != null, "Assessment DoorFrame exists");
            Check(FindIncludingInactive("EvacuationPathway") != null, "Assessment EvacuationPathway exists");
            Check(FindIncludingInactive("PathFloor") != null, "Assessment PathFloor exists");
            Check(FindIncludingInactive("SafeArea") != null, "Assessment SafeArea exists");
            Check(FindIncludingInactive("SafeZoneTrigger") != null, "Assessment SafeZoneTrigger exists");
            Check(FindIncludingInactive("Wall_South_Lintel") != null, "Assessment south doorway lintel exists (wall has opening)");
            Check(GameObject.Find("Wall_South") == null, "Old solid Wall_South replaced by split segments");
            Check(FindIncludingInactive("AltDoor") == null, "Old stationary AltDoor removed");

            var door = Object.FindFirstObjectByType<DoorController>();
            Check(door != null, "Assessment DoorController exists (reused TrainingRoom system)");
            if (door != null)
            {
                var dso = new SerializedObject(door);
                Check(dso.FindProperty("hinge").objectReferenceValue != null, "DoorController.hinge wired");
                Check(dso.FindProperty("evacScenario").objectReferenceValue != null, "DoorController.evacScenario wired");
            }

            // AlternativeExit root must NOT still carry EvacuationExit (would double-notify / confuse flow)
            var altGO = GameObject.Find("AlternativeExit");
            Check(altGO != null, "AlternativeExit exists");
            if (altGO != null)
                Check(altGO.GetComponent<EvacuationExit>() == null, "AlternativeExit has no EvacuationExit (hinged DoorController owns interaction)");

            // Blocked exit + assessment objectives intact
            Check(FindIncludingInactive("BlockedPrimaryExit") != null, "BlockedPrimaryExit intact (scenario preserved)");
            Check(FindIncludingInactive("InspectBlockedHazard") != null, "InspectBlockedHazard intact (objectives preserved)");

            // Primary door hazard activation point (visible Inspector option)
            Check(FindIncludingInactive("PrimaryDoor") != null, "Assessment PrimaryDoor exists (interactable)");
            var hz = Object.FindFirstObjectByType<PrimaryDoorHazardActivator>();
            Check(hz != null, "PrimaryDoorHazardActivator exists");
            if (hz != null)
            {
                var hso = new SerializedObject(hz);
                Check(hso.FindProperty("primaryDoorReference").objectReferenceValue != null, "Activator.primaryDoorReference wired");
                Check(hso.FindProperty("alternateDoorReference").objectReferenceValue != null, "Activator.alternateDoorReference wired");
                Check(hso.FindProperty("safeZoneReference").objectReferenceValue != null, "Activator.safeZoneReference wired");
                Check(hso.FindProperty("alarmSource").objectReferenceValue != null, "Activator.alarmSource wired");
            }
            string hzSrc = System.IO.File.ReadAllText("Assets/SafeVR/Scripts/Scenario/Equipment/PrimaryDoorHazardActivator.cs");
            Check(hzSrc.Contains("Activate Primary Door Blocked Hazard"), "Activator prompt offers hazard activation");
            Check(hzSrc.Contains("ActivatePrimaryDoorHazard"), "Activator drives manager activation (no auto timer, no completion)");
            string mgrSrc2 = System.IO.File.ReadAllText("Assets/SafeVR/Scripts/Scenario/EvacuationScenarioManager.cs");
            Check(mgrSrc2.Contains("public void ActivatePrimaryDoorHazard"), "Manager has ActivatePrimaryDoorHazard()");
            Check(!mgrSrc2.Contains("ElapsedTime >= blockRevealDelay"), "No auto emergency timer (player-driven activation)");
            string hudSrc = System.IO.File.ReadAllText("Assets/SafeVR/Scripts/UI/ScenarioHUD.cs");
            Check(hudSrc.Contains("Primary door blocked. Use the alternate door."), "Objective updates to alternate-door instruction after activation");

            // AlarmBeacon wired to manager (same AlarmController as TrainingRoom)
            Check(FindIncludingInactive("AlarmBeacon") != null, "Assessment AlarmBeacon exists (reused TrainingRoom system)");
            var alarm = Object.FindFirstObjectByType<AlarmController>();
            Check(alarm != null, "Assessment AlarmController exists");
            var mgrCheck = Object.FindFirstObjectByType<EvacuationScenarioManager>();
            Check(mgrCheck != null, "EvacuationScenarioManager exists");
            if (mgrCheck != null)
            {
                var mso = new SerializedObject(mgrCheck);
                Check(mso.FindProperty("alarmController").objectReferenceValue != null, "Manager.alarmController wired");
                Check(mso.FindProperty("blockedExitVisual").objectReferenceValue != null, "Manager.blockedExitVisual wired");
                Check(mso.FindProperty("safeZoneTrigger").objectReferenceValue != null, "Manager.safeZoneTrigger wired");
            }

            // Alarm must fire automatically at emergency onset (HazardDetected), not on player click
            string mgrSrc = System.IO.File.ReadAllText("Assets/SafeVR/Scripts/Scenario/EvacuationScenarioManager.cs");
            int hzIdx = mgrSrc.IndexOf("case ScenarioState.HazardDetected:");
            int emIdx = mgrSrc.IndexOf("case ScenarioState.EmergencyActive:", hzIdx);
            string hzBlock = hzIdx >= 0 && emIdx > hzIdx ? mgrSrc.Substring(hzIdx, emIdx - hzIdx) : "";
            Check(hzBlock.Contains("alarmController") && hzBlock.Contains("SetActive(true)"), "Alarm activates at HazardDetected (emergency onset, no click needed)");
            // Completion logic: door must not complete; safe zone must complete
            int evacIdx = mgrSrc.IndexOf("case ScenarioState.Evacuating:");
            int nextCase = mgrSrc.IndexOf("case ScenarioState.Completed:", evacIdx);
            string evacBlock = evacIdx >= 0 && nextCase > evacIdx ? mgrSrc.Substring(evacIdx, nextCase - evacIdx) : "";
            Check(!evacBlock.Contains("CompleteScenario"), "Evacuating state does NOT complete (door open alone never completes)");
            Check(mgrSrc.Contains("NotifyReachedSafeZone") && mgrSrc.Substring(mgrSrc.IndexOf("public void NotifyReachedSafeZone")).Contains("CompleteScenario"), "NotifyReachedSafeZone completes (safe zone completes)");
            string doorSrc = System.IO.File.ReadAllText("Assets/SafeVR/Scripts/Scenario/Equipment/DoorController.cs");
            int oi = doorSrc.IndexOf("public override void OnInteract");
            string interactBlock = oi >= 0 ? doorSrc.Substring(oi, System.Math.Min(800, doorSrc.Length - oi)) : "";
            Check(interactBlock.Contains("NotifyAlternativeRouteFound") || interactBlock.Contains("NotifyDoorOpened"), "DoorController routes open to scenario (no direct CompleteScenario)");
            Check(!interactBlock.Contains("CompleteScenario"), "DoorController never calls CompleteScenario directly");
            Check(doorSrc.Contains("IsEvacLocked"), "DoorController has evac blocked-lock (door stays shut until response complete)");

            Debug.Log(fails == 0 ? "[SafeVRAssessmentVerify] ALL CHECKS PASSED" : $"[SafeVRAssessmentVerify] {fails} CHECKS FAILED");
            if (fails > 0) EditorApplication.Exit(1);
        }

        private static GameObject FindIncludingInactive(string name)
        {
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in all)
                if (t.name == name) return t.gameObject;
            return null;
        }
    }
}

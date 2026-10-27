using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SafeVR.Scenario;
using SafeVR.Scenario.Equipment;

namespace SafeVR.Editor
{
    /// <summary>
    /// Phase 2 verification — Extinguisher Inspection and Selection.
    /// Batchmode-safe (edit-mode): checks scene wiring + code gates.
    /// Live walk-through (isolate → select → suppress → evacuate) requires Play mode.
    /// </summary>
    public static class SafeVRPhase2Verify
    {
        public static void Verify()
        {
            int fails = 0;
            void Check(bool cond, string name)
            {
                Debug.Log((cond ? "[PASS] " : "[FAIL] ") + name);
                if (!cond) fails++;
            }

            // ---- TrainingRoom: two typed stations ----
            EditorSceneManager.OpenScene("Assets/Scenes/TrainingRoom.unity", OpenSceneMode.Single);
            var pickups = Object.FindObjectsByType<FireExtinguisherPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Check(pickups.Length == 2, $"TrainingRoom: exactly 2 extinguisher stations (found {pickups.Length})");
            bool hasCO2 = false, hasWater = false;
            foreach (var p in pickups)
            {
                var so = new SerializedObject(p);
                var t = (ExtinguisherType)so.FindProperty("extinguisherType").enumValueIndex;
                if (t == ExtinguisherType.CO2) hasCO2 = true;
                if (t == ExtinguisherType.Water) hasWater = true;
                Check(so.FindProperty("pickupVisualRoot").objectReferenceValue != null, $"TrainingRoom: {p.gameObject.name} pickupVisualRoot wired");
                Check(so.FindProperty("weaponHold").objectReferenceValue != null, $"TrainingRoom: {p.gameObject.name} weaponHold wired");
                Check(so.FindProperty("heldExtinguisher").objectReferenceValue != null, $"TrainingRoom: {p.gameObject.name} heldExtinguisher wired");
                Check(p.GetComponent<Collider>() != null, $"TrainingRoom: {p.gameObject.name} has collider (inspectable)");
            }
            Check(hasCO2, "TrainingRoom: CO2 station present");
            Check(hasWater, "TrainingRoom: Water station present");
            Check(FindIncludingInactive("FireExtinguisher_CO2") != null, "TrainingRoom: FireExtinguisher_CO2 object exists");
            Check(FindIncludingInactive("FireExtinguisher_Water") != null, "TrainingRoom: FireExtinguisher_Water object exists");
            int labelCount = 0;
            foreach (var t in Object.FindObjectsByType<TMPro.TextMeshPro>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.gameObject.name == "TypeLabel") labelCount++;
            Check(labelCount == 2, $"TrainingRoom: 2 floating type labels (found {labelCount})");
            Check(GameObject.Find("FireExtinguisher_Station") == null, "TrainingRoom: old untyped station removed");

            // ---- Scenario manager: correct-type enforcement ----
            var scenario = Object.FindFirstObjectByType<ElectricalFireScenarioManager>();
            Check(scenario != null, "TrainingRoom: ElectricalFireScenarioManager exists");
            if (scenario != null)
            {
                var sso = new SerializedObject(scenario);
                Check((ExtinguisherType)sso.FindProperty("requiredExtinguisher").enumValueIndex == ExtinguisherType.CO2,
                    "TrainingRoom: required extinguisher is CO2");
                Check(scenario.RequiredExtinguisher == ExtinguisherType.CO2, "TrainingRoom: RequiredExtinguisher property is CO2");
            }
            var mgrType = typeof(ElectricalFireScenarioManager);
            Check(mgrType.GetMethod("NotifyExtinguisherInspected") != null, "Manager has NotifyExtinguisherInspected(type)");
            Check(mgrType.GetMethod("NotifyExtinguisherEquipped", new System.Type[] { typeof(ExtinguisherType) }) != null,
                "Manager has NotifyExtinguisherEquipped(ExtinguisherType)");
            string mgrSrc = System.IO.File.ReadAllText("Assets/SafeVR/Scripts/Scenario/ElectricalFireScenarioManager.cs");
            Check(mgrSrc.Contains("Selected wrong extinguisher"), "Manager logs unsafe action on wrong selection");
            Check(mgrSrc.Contains("powerIsolated && correctExtinguisherEquipped"), "Manager metrics require power + correct type");
            Check(mgrSrc.Contains("IsFireSuppressed()"), "Manager: fire never extinguished by power-off alone (IsFireSuppressed guard)");
            Check(mgrSrc.Contains("EnsureDelayedFireStart()"), "Manager: fire still ignites after early power isolation");

            // ---- Discharge: power + type gating ----
            var discharge = Object.FindFirstObjectByType<FireDischargeInteractable>();
            Check(discharge != null, "TrainingRoom: FireDischargeInteractable exists");
            string disSrc = System.IO.File.ReadAllText("Assets/SafeVR/Scripts/Scenario/Equipment/FireDischargeInteractable.cs");
            Check(disSrc.Contains("IsCorrectExtinguisherEquipped"), "Discharge checks correct extinguisher");
            Check(disSrc.Contains("powerOk && typeOk"), "Discharge effective only when power isolated AND type correct");
            Check(disSrc.Contains("Discharged wrong extinguisher"), "Discharge logs unsafe action for wrong type");
            string pickSrc = System.IO.File.ReadAllText("Assets/SafeVR/Scripts/Scenario/Equipment/FireExtinguisherPickup.cs");
            Check(pickSrc.Contains("Press [E] again to equip"), "Pickup enforces inspect-before-select (two-step)");

            // ---- Tutorial untouched (single practice station, still works) ----
            EditorSceneManager.OpenScene("Assets/Scenes/Tutorial.unity", OpenSceneMode.Single);
            var tutPickups = Object.FindObjectsByType<FireExtinguisherPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Check(tutPickups.Length == 1, $"Tutorial: exactly 1 practice extinguisher (found {tutPickups.Length})");
            Check(Object.FindFirstObjectByType<ElectricalFireScenarioManager>() == null, "Tutorial: no scenario manager (practice spray always effective)");
            Check(Object.FindFirstObjectByType<Tutorial.TutorialManager>() != null, "Tutorial: TutorialManager intact");

            // ---- Assessment + Results untouched ----
            EditorSceneManager.OpenScene("Assets/Scenes/Assessment.unity", OpenSceneMode.Single);
            Check(Object.FindFirstObjectByType<EvacuationScenarioManager>() != null, "Assessment: EvacuationScenarioManager intact");
            Check(Object.FindObjectsByType<FireExtinguisherPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0,
                "Assessment: no extinguisher stations (untouched)");
            EditorSceneManager.OpenScene("Assets/Scenes/Results.unity", OpenSceneMode.Single);
            Check(Object.FindFirstObjectByType<SafeVR.UI.ResultsUI>() != null, "Results: ResultsUI intact");

            // ---- Build flow intact ----
            var buildScenes = EditorBuildSettings.scenes;
            Check(buildScenes.Length == 5, $"Build Settings: 5 scenes (found {buildScenes.Length})");

            Debug.Log(fails == 0 ? "[SafeVRPhase2Verify] ALL CHECKS PASSED" : $"[SafeVRPhase2Verify] {fails} CHECKS FAILED");
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

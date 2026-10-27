using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SafeVR.Scenario;
using SafeVR.Scenario.Equipment;

namespace SafeVR.Editor
{
    /// <summary>
    /// Phase 3 verification — Realistic Fire Extinguisher Mechanics.
    /// Batchmode-safe (edit-mode): checks scene wiring, builder visuals, HUD refs,
    /// public VR-ready API surface, and code gates. Live walk-through
    /// (isolate → equip → pin → aim → hold+sweep → evacuate) requires Play mode.
    /// </summary>
    public static class SafeVRPhase3Verify
    {
        public static void Verify()
        {
            int fails = 0;
            void Check(bool cond, string name)
            {
                Debug.Log((cond ? "[PASS] " : "[FAIL] ") + name);
                if (!cond) fails++;
            }

            // ---- TrainingRoom: unit state (pin + charge) ----
            EditorSceneManager.OpenScene("Assets/Scenes/TrainingRoom.unity", OpenSceneMode.Single);
            var pickups = Object.FindObjectsByType<FireExtinguisherPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Check(pickups.Length == 2, $"TrainingRoom: 2 typed stations (found {pickups.Length})");
            foreach (var p in pickups)
            {
                var so = new SerializedObject(p);
                Check(so.FindProperty("maxCharge") != null && so.FindProperty("maxCharge").floatValue > 0f,
                    $"TrainingRoom: {p.gameObject.name} has limited charge (max={so.FindProperty("maxCharge")?.floatValue})");
                Check(so.FindProperty("pinRemoved") != null && !so.FindProperty("pinRemoved").boolValue,
                    $"TrainingRoom: {p.gameObject.name} pin starts IN");
                Check(so.FindProperty("charge") != null && so.FindProperty("charge").floatValue > 0f,
                    $"TrainingRoom: {p.gameObject.name} starts fully charged");
            }

            // ---- Prop visuals: pin / hose / gauge / band / label ----
            foreach (var name in new[] { "FireExtinguisher_CO2", "FireExtinguisher_Water" })
            {
                var root = FindIncludingInactive(name);
                Check(root != null, $"TrainingRoom: {name} exists");
                if (root != null)
                {
                    Check(root.transform.Find("Pin") != null, $"TrainingRoom: {name} visible safety pin");
                    Check(root.transform.Find("Hose") != null, $"TrainingRoom: {name} hose");
                    Check(root.transform.Find("Gauge") != null, $"TrainingRoom: {name} pressure gauge");
                    Check(root.transform.Find("TypeLabel") != null, $"TrainingRoom: {name} readable type label");
                }
            }

            // ---- Held extinguisher mirrors (starts inactive until pickup) ----
            HeldExtinguisher held = null;
            var heldAll = Object.FindObjectsByType<HeldExtinguisher>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (heldAll.Length > 0) held = heldAll[0];
            Check(held != null, "TrainingRoom: HeldExtinguisher exists");
            Check(heldAll.Length == 1, $"TrainingRoom: exactly 1 held unit (found {heldAll.Length})");
            if (held != null)
            {
                var hso = new SerializedObject(held);
                Check(hso.FindProperty("pinVisual").objectReferenceValue != null, "Held: pinVisual wired");
                Check(hso.FindProperty("gaugeRenderer").objectReferenceValue != null, "Held: gaugeRenderer wired");
                var htype = typeof(HeldExtinguisher);
                Check(htype.GetMethod("SetPinRemoved") != null, "Held: SetPinRemoved() exists (VR-ready)");
                Check(htype.GetMethod("SetCharge01") != null, "Held: SetCharge01() exists (VR-ready)");
            }

            // ---- Pickup VR-ready API ----
            var ptype = typeof(FireExtinguisherPickup);
            Check(ptype.GetMethod("PullPin") != null, "Pickup: PullPin() exists (VR-ready)");
            Check(ptype.GetMethod("DrainCharge") != null, "Pickup: DrainCharge() exists (VR-ready)");
            Check(ptype.GetProperty("IsReady") != null, "Pickup: IsReady state exists");
            Check(ptype.GetProperty("HasCharge") != null, "Pickup: HasCharge state exists");

            // ---- Scenario: 5 required actions + pin API ----
            var scenario = Object.FindFirstObjectByType<ElectricalFireScenarioManager>();
            Check(scenario != null, "TrainingRoom: ElectricalFireScenarioManager exists");
            var mtype = typeof(ElectricalFireScenarioManager);
            Check(mtype.GetMethod("NotifyPinRemoved") != null, "Manager: NotifyPinRemoved() exists (VR-ready)");
            Check(mtype.GetProperty("ActivePickup") != null, "Manager: ActivePickup tracked");
            Check(mtype.GetProperty("IsPinRemoved") != null, "Manager: IsPinRemoved state");
            Check(mtype.GetProperty("ActiveCharge01") != null, "Manager: ActiveCharge01 state");
            string mgrSrc = System.IO.File.ReadAllText("Assets/SafeVR/Scripts/Scenario/ElectricalFireScenarioManager.cs");
            Check(mgrSrc.Contains("GetRequiredActionCount() => 5"), "Manager: 5 required actions (isolate/equip/pin/suppress/evacuate)");
            Check(mgrSrc.Contains("totalRequiredActions = 5"), "Manager: metrics total = 5");

            // ---- Discharge: pin/aim/sweep/charge gates ----
            var discharge = Object.FindFirstObjectByType<FireDischargeInteractable>();
            Check(discharge != null, "TrainingRoom: FireDischargeInteractable exists");
            if (discharge != null)
            {
                var dso = new SerializedObject(discharge);
                Check(Mathf.Approximately(dso.FindProperty("fullAimRadius").floatValue, 0.8f),
                    $"Discharge: forgiving aim cone (fullAimRadius={dso.FindProperty("fullAimRadius").floatValue})");
                Check(Mathf.Approximately(dso.FindProperty("zeroAimRadius").floatValue, 2.5f),
                    $"Discharge: outer miss radius (zeroAimRadius={dso.FindProperty("zeroAimRadius").floatValue})");
                Check(Mathf.Approximately(dso.FindProperty("fullSweepRange").floatValue, 0.9f),
                    $"Discharge: reachable sweep range (fullSweepRange={dso.FindProperty("fullSweepRange").floatValue})");
                Check(Mathf.Approximately(dso.FindProperty("sweepWindow").floatValue, 3.0f),
                    $"Discharge: slow-sweep window (sweepWindow={dso.FindProperty("sweepWindow").floatValue}s)");
                Check(dso.FindProperty("minSweepFactor").floatValue >= 0.5f,
                    $"Discharge: slow sweep still suppresses (minSweepFactor={dso.FindProperty("minSweepFactor").floatValue})");
                Check(Mathf.Approximately(dso.FindProperty("wasteGraceTime").floatValue, 2.0f),
                    "Discharge: waste grace period (slow sweeps never flagged)");
            }
            foreach (var p in pickups)
            {
                var so = new SerializedObject(p);
                Check(Mathf.Approximately(so.FindProperty("drainRate").floatValue, 5f),
                    $"TrainingRoom: {p.gameObject.name} reasonable drain (drainRate={so.FindProperty("drainRate").floatValue}/s ≈ 20 s spray)");
            }
            string disSrc = System.IO.File.ReadAllText("Assets/SafeVR/Scripts/Scenario/Equipment/FireDischargeInteractable.cs");
            Check(disSrc.Contains("PullPin()"), "Discharge: pin pull on tap-interact");
            Check(disSrc.Contains("without removing the safety pin"), "Discharge: unsafe log for pin-in attempt");
            Check(disSrc.Contains("empty extinguisher") || disSrc.Contains("EMPTY"), "Discharge: empty state handled");
            Check(disSrc.Contains("ComputeAimFactor") && disSrc.Contains("ComputeSweepFactor"), "Discharge: aim + sweep effectiveness");
            Check(disSrc.Contains("TryGetAimRay"), "Discharge: aim measured by ray closest-approach (forgiving volume)");
            Check(disSrc.Contains("wasteEffectiveness"), "Discharge: waste threshold separated from sweep guidance");
            Check(disSrc.Contains("DrainCharge"), "Discharge: charge drains while spraying");
            Check(disSrc.Contains("Wasting extinguisher charge"), "Discharge: unsafe log for wasted charge");
            Check(disSrc.Contains("powerOk && typeOk"), "Discharge: still gated on power + correct type");
            string fireSrc = System.IO.File.ReadAllText("Assets/SafeVR/Scripts/Scenario/FireController.cs");
            Check(fireSrc.Contains("public bool Suppress(float dt, bool powerIsolated)"), "FireController.Suppress unchanged (gradual suppression preserved)");

            // ---- HUD: charge indicator + pin guidance ----
            var hud = Object.FindFirstObjectByType<SafeVR.UI.ScenarioHUD>();
            Check(hud != null, "TrainingRoom: ScenarioHUD exists");
            if (hud != null)
            {
                var hso = new SerializedObject(hud);
                Check(hso.FindProperty("chargePanel").objectReferenceValue != null, "HUD: chargePanel wired");
                Check(hso.FindProperty("chargeFill").objectReferenceValue != null, "HUD: chargeFill wired");
                Check(hso.FindProperty("chargeText").objectReferenceValue != null, "HUD: chargeText wired");
            }
            Check(FindIncludingInactive("ChargePanel") != null, "TrainingRoom: ChargePanel object exists");

            // ---- Tutorial intact (pin step added, still completable) ----
            EditorSceneManager.OpenScene("Assets/Scenes/Tutorial.unity", OpenSceneMode.Single);
            Check(Object.FindFirstObjectByType<Tutorial.TutorialManager>() != null, "Tutorial: TutorialManager intact");
            string tutSrc = System.IO.File.ReadAllText("Assets/SafeVR/Scripts/Tutorial/TutorialManager.cs");
            Check(tutSrc.Contains("pull the pin"), "Tutorial: guidance mentions pin pull");
            var tutPickups = Object.FindObjectsByType<FireExtinguisherPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Check(tutPickups.Length == 1, $"Tutorial: 1 practice unit (found {tutPickups.Length})");

            // ---- Other scenes untouched ----
            EditorSceneManager.OpenScene("Assets/Scenes/Assessment.unity", OpenSceneMode.Single);
            Check(Object.FindFirstObjectByType<EvacuationScenarioManager>() != null, "Assessment: EvacuationScenarioManager intact");
            Check(Object.FindObjectsByType<FireDischargeInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0,
                "Assessment: no discharge target (untouched)");
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            Check(Object.FindFirstObjectByType<SafeVR.UI.MainMenuUI>() != null, "MainMenu: MainMenuUI intact");
            EditorSceneManager.OpenScene("Assets/Scenes/Results.unity", OpenSceneMode.Single);
            Check(Object.FindFirstObjectByType<SafeVR.UI.ResultsUI>() != null, "Results: ResultsUI intact");

            var buildScenes = EditorBuildSettings.scenes;
            Check(buildScenes.Length == 5, $"Build Settings: 5 scenes (found {buildScenes.Length})");

            Debug.Log(fails == 0 ? "[SafeVRPhase3Verify] ALL CHECKS PASSED" : $"[SafeVRPhase3Verify] {fails} CHECKS FAILED");
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

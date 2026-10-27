using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SafeVR.Scenario;
using SafeVR.Scenario.Equipment;
using SafeVR.UI;

namespace SafeVR.Editor
{
    public static class SafeVRDeepVerify
    {
        [MenuItem("SafeVR/Deep Verify")]
        public static void Verify()
        {
            Debug.Log("[DeepVerify] Checking TrainingRoom scenario chain...");
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/TrainingRoom.unity", OpenSceneMode.Single);
            int count = 0;
            var scen = Object.FindFirstObjectByType<ElectricalFireScenarioManager>();
            Debug.Log($"  ScenarioManager: {(scen!=null?"OK":"MISSING")}");
            var fire = Object.FindFirstObjectByType<FireController>();
            Debug.Log($"  FireController intensity={fire?.intensity} isBurning={fire!=null}: {(fire!=null?"OK":"MISSING")}");
            var pickup = Object.FindFirstObjectByType<FireExtinguisherPickup>();
            Debug.Log($"  ExtinguisherPickup: {(pickup!=null?"OK":"MISSING")} prompt={pickup?.GetPromptText()}");
            var discharge = Object.FindFirstObjectByType<FireDischargeInteractable>();
            Debug.Log($"  Discharge Hold: {(discharge!=null?"OK":"MISSING")} holdDur={discharge?.HoldDuration} isHold={discharge is Interaction.IHoldInteractable}");
            var cutoff = Object.FindFirstObjectByType<ScenarioCutoffSwitch>();
            Debug.Log($"  CutoffSwitch: {(cutoff!=null?"OK":"MISSING")}");
            var hud = Object.FindFirstObjectByType<ScenarioHUD>();
            Debug.Log($"  ScenarioHUD: {(hud!=null?"OK":"MISSING")}");
            var alarm = Object.FindFirstObjectByType<AlarmController>();
            Debug.Log($"  AlarmController: {(alarm!=null?"OK":"MISSING")}");
            var perf = Object.FindFirstObjectByType<PerformanceReportUI>();
            Debug.Log($"  PerformanceReportUI: {(perf!=null?"OK":"MISSING")}");
            // Check MainMenu
            var scene2 = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            var mainMenu = Object.FindFirstObjectByType<SafeVR.UI.MainMenuUI>();
            Debug.Log($"[DeepVerify] MainMenuUI: {(mainMenu!=null?"OK":"MISSING")}");
            var pedestal = GameObject.Find("ShowcasePedestal");
            Debug.Log($"  Pedestal should be REMOVED: {(pedestal==null?"REMOVED OK":"STILL EXISTS")}");
            var canvas = GameObject.Find("MainMenuCanvas");
            if (canvas!=null)
            {
                var texts = canvas.GetComponentsInChildren<TMPro.TMP_Text>(true);
                Debug.Log($"  MainMenu text count: {texts.Length}");
                foreach(var t in texts) if(t.text.Contains("SafeVR")||t.text.Contains("START")) Debug.Log($"    Text '{t.name}' fontSize={t.fontSize} txt='{t.text.Substring(0, System.Math.Min(30,t.text.Length))}'");
            }
            // Check HUD font sizes
            EditorSceneManager.OpenScene("Assets/Scenes/TrainingRoom.unity", OpenSceneMode.Single);
            var hud2 = Object.FindFirstObjectByType<ScenarioHUD>();
            if(hud2!=null)
            {
                var so = new SerializedObject(hud2);
                var obj = so.FindProperty("objectiveText").objectReferenceValue as TMPro.TMP_Text;
                Debug.Log($"  HUD objective fontSize: {obj?.fontSize}");
                var timer = so.FindProperty("timerText").objectReferenceValue as TMPro.TMP_Text;
                Debug.Log($"  HUD timer fontSize: {timer?.fontSize}");
                var banner = so.FindProperty("statusBannerText").objectReferenceValue as TMPro.TMP_Text;
                Debug.Log($"  HUD banner fontSize: {banner?.fontSize}");
            }
            Debug.Log("[DeepVerify] Done.");
        }
    }
}

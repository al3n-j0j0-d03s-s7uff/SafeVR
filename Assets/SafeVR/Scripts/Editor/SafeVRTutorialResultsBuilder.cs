using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using SafeVR.Core;
using SafeVR.Tutorial;
using SafeVR.UI;
using SafeVR.Scenario;
using SafeVR.Scenario.Equipment;
using SafeVR.Interaction;

namespace SafeVR.Editor
{
    /// <summary>
    /// Builds the Tutorial and Results scenes. Reuses the shared Player prefab,
    /// room/lighting/particle helpers and interaction components — no changes to
    /// TrainingRoom or Assessment content.
    /// </summary>
    public static class SafeVRTutorialResultsBuilder
    {
        private const string SCENE_DIR = "Assets/Scenes";

        [MenuItem("SafeVR/Build Tutorial Only")]
        public static void BuildTutorial()
        {
            SafeVRMaterials.EnsureMaterials();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SafeVRSceneBuilder.BuildLightingAndVolume();
            SafeVRSceneBuilder.BuildRoomShell(8f, 8f, 3.2f);

            var player = SafeVRPlayerBuilder.InstantiatePlayer(new Vector3(0f, 1f, -2.5f), 0f, true);

            BuildPracticePanel();
            var practiceDoor = BuildPracticeDoor();
            BuildPracticeExtinguisher();
            var practiceFire = BuildPracticeFire();

            // Shared HUD for reticle prompt, hold bar and warning toast (null-safe without a scenario manager).
            SafeVRSceneBuilder.BuildScenarioHUD(true);
            // Pause menu (Resume / Retry / Main Menu). ScenarioPauseMenu is null-safe
            // without a scenario manager, so Tutorial gets the same pause flow as
            // TrainingRoom/Assessment — including the mobile PAUSE button target.
            SafeVRSceneBuilder.BuildScenarioPauseMenu();
            BuildTutorialUI(player, practiceDoor, practiceFire);

            SafeVRSceneBuilder.EnsureCoreManagersScenario();
            EditorSceneManager.SaveScene(scene, $"{SCENE_DIR}/Tutorial.unity");
            Debug.Log("[SafeVR] Tutorial scene generated at Assets/Scenes/Tutorial.unity");
        }

        [MenuItem("SafeVR/Build Results Only")]
        public static void BuildResults()
        {
            SafeVRMaterials.EnsureMaterials();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SafeVRSceneBuilder.BuildLightingAndVolume();
            SafeVRSceneBuilder.BuildRoomShell(10f, 8f, 3.2f);

            SafeVRPlayerBuilder.InstantiatePlayer(new Vector3(0f, 1f, -2.5f), 0f, false);
            BuildResultsUI();
            SafeVRSceneBuilder.EnsureCoreManagersScenario();

            EditorSceneManager.SaveScene(scene, $"{SCENE_DIR}/Results.unity");
            Debug.Log("[SafeVR] Results scene generated at Assets/Scenes/Results.unity");
        }

        // --------------------------------------------------------------------
        // Tutorial content
        // --------------------------------------------------------------------
        private static void BuildPracticePanel()
        {
            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "PracticePanel";
            panel.transform.position = new Vector3(-3.8f, 1.4f, 0.5f);
            panel.transform.localScale = new Vector3(0.08f, 0.6f, 0.5f);
            panel.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatHazard;

            var eq = panel.AddComponent<EquipmentInteractable>();
            var so = new SerializedObject(eq);
            so.FindProperty("equipmentType").enumValueIndex = (int)EquipmentType.SafetySign;
            so.FindProperty("equipmentTitle").stringValue = "Practice Panel";
            so.FindProperty("actionVerb").stringValue = "Press";
            so.FindProperty("equipmentDescription").stringValue = "Tutorial practice target. Aim and press E.";
            so.ApplyModifiedProperties();
        }

        private static DoorController BuildPracticeDoor()
        {
            var doorGO = new GameObject("PracticeDoor");
            doorGO.transform.position = new Vector3(-1.5f, 0f, 1.5f);

            var frameL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frameL.name = "DoorFrame";
            frameL.transform.SetParent(doorGO.transform, false);
            frameL.transform.localPosition = new Vector3(-0.62f, 1.15f, 0f);
            frameL.transform.localScale = new Vector3(0.14f, 2.3f, 0.22f);
            frameL.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;

            var frameR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frameR.name = "DoorFrame_R";
            frameR.transform.SetParent(doorGO.transform, false);
            frameR.transform.localPosition = new Vector3(0.62f, 1.15f, 0f);
            frameR.transform.localScale = new Vector3(0.14f, 2.3f, 0.22f);
            frameR.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;

            var lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lintel.name = "DoorFrame_Top";
            lintel.transform.SetParent(doorGO.transform, false);
            lintel.transform.localPosition = new Vector3(0, 2.32f, 0f);
            lintel.transform.localScale = new Vector3(1.38f, 0.16f, 0.22f);
            lintel.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;

            // Standalone: no scenario refs, so the door always opens on interact.
            var hingeGO = new GameObject("DoorHinge");
            hingeGO.transform.SetParent(doorGO.transform, false);
            hingeGO.transform.localPosition = new Vector3(-0.51f, 0f, 0f);

            var doorPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorPanel.name = "DoorPanel";
            doorPanel.transform.SetParent(hingeGO.transform, false);
            doorPanel.transform.localPosition = new Vector3(0.51f, 1.09f, 0f);
            doorPanel.transform.localScale = new Vector3(1.02f, 2.08f, 0.07f);
            doorPanel.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatGreen;

            var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.name = "DoorHandle";
            handle.transform.SetParent(doorPanel.transform, false);
            handle.transform.localPosition = new Vector3(0.32f, 0f, 0.09f);
            handle.transform.localScale = new Vector3(0.18f, 0.06f, 0.1f);
            handle.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;

            var panelCol = doorPanel.AddComponent<BoxCollider>();
            panelCol.center = Vector3.zero;
            panelCol.size = new Vector3(1f, 1f, 1f);

            var doorCtrl = hingeGO.AddComponent<DoorController>();
            var so = new SerializedObject(doorCtrl);
            so.FindProperty("hinge").objectReferenceValue = hingeGO.transform;
            so.FindProperty("doorBlocker").objectReferenceValue = panelCol;
            so.ApplyModifiedProperties();
            return doorCtrl;
        }

        private static void BuildPracticeExtinguisher()
        {
            var station = SafeVRMainMenuBuilder.CreateExtinguisherProp("PracticeExtinguisher", new Vector3(3.3f, 0.38f, -1.0f));
            var mount = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mount.name = "WallMount";
            mount.transform.SetParent(station.transform, false);
            mount.transform.localPosition = new Vector3(0, 0.3f, -0.18f);
            mount.transform.localScale = new Vector3(0.5f, 0.5f, 0.05f);
            mount.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;

            var col = station.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.65f, 0);
            col.size = new Vector3(1.4f, 1.8f, 1.0f);

            var pickup = station.AddComponent<FireExtinguisherPickup>();
            var so = new SerializedObject(pickup);
            so.FindProperty("pickupVisualRoot").objectReferenceValue = station;
            so.FindProperty("extinguisherType").enumValueIndex = (int)ExtinguisherType.CO2;
            so.ApplyModifiedProperties();
        }

        private static FireController BuildPracticeFire()
        {
            var fireRoot = new GameObject("PracticeFire");
            fireRoot.transform.position = new Vector3(1.5f, 0.12f, 1.5f);

            var ember = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ember.name = "EmberBed";
            ember.transform.SetParent(fireRoot.transform, false);
            ember.transform.localPosition = new Vector3(0, 0.06f, 0);
            ember.transform.localScale = new Vector3(0.9f, 0.05f, 0.9f);
            ember.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatFire;
            ember.SetActive(false);

            var fireLightObj = new GameObject("FireLight", typeof(Light));
            fireLightObj.transform.SetParent(fireRoot.transform, false);
            fireLightObj.transform.localPosition = new Vector3(0, 0.6f, 0);
            var fireLight = fireLightObj.GetComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.42f, 0.08f);
            fireLight.range = 6f;
            fireLight.intensity = 0f;

            var firePS = SafeVRSceneBuilder.CreateFireParticles(fireRoot, "FirePS", new Vector3(0, 0.15f, 0));

            var fireCtrl = fireRoot.AddComponent<FireController>();
            var fireSO = new SerializedObject(fireCtrl);
            fireSO.FindProperty("fireParticles").objectReferenceValue = firePS;
            fireSO.FindProperty("emberMesh").objectReferenceValue = ember;
            fireSO.FindProperty("fireLight").objectReferenceValue = fireLight;
            fireSO.ApplyModifiedProperties();

            var dischargeCol = fireRoot.AddComponent<BoxCollider>();
            dischargeCol.center = new Vector3(0, 0.55f, 0);
            dischargeCol.size = new Vector3(3.2f, 2.0f, 3.2f);

            var discharge = fireRoot.AddComponent<FireDischargeInteractable>();
            var disSO = new SerializedObject(discharge);
            disSO.FindProperty("fireController").objectReferenceValue = fireCtrl;
            disSO.FindProperty("requiredHold").floatValue = 2.0f;
            var audioGO = new GameObject("DischargeAudio");
            audioGO.transform.SetParent(fireRoot.transform, false);
            var dischargeAudio = audioGO.AddComponent<AudioSource>();
            dischargeAudio.loop = true;
            dischargeAudio.playOnAwake = false;
            disSO.FindProperty("dischargeAudio").objectReferenceValue = dischargeAudio;
            disSO.ApplyModifiedProperties();

            return fireCtrl;
        }

        private static void BuildTutorialUI(GameObject player, DoorController practiceDoor, FireController practiceFire)
        {
            var canvas = SafeVRUIBuilder.CreateCanvas("TutorialCanvas");

            var topPanel = SafeVRUIBuilder.CreatePanel(canvas.transform, "TutorialHUD", new Vector2(0, 0), new Vector2(1100, 120), new Color(0.08f, 0.10f, 0.14f, 0.96f));
            var rt = topPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, 0); rt.sizeDelta = new Vector2(0, 120);
            var counter = SafeVRUIBuilder.CreateText(topPanel.transform, "StepCounter", "Step 1 / 8", 20, FontStyles.Bold, new Color(0.2f, 0.85f, 0.95f), new Vector2(-80, 28), new Vector2(780, 30), TextAlignmentOptions.MidlineLeft);
            var title = SafeVRUIBuilder.CreateText(topPanel.transform, "StepTitle", "Move", 26, FontStyles.Bold, Color.white, new Vector2(-80, -2), new Vector2(780, 36), TextAlignmentOptions.MidlineLeft);
            var desc = SafeVRUIBuilder.CreateText(topPanel.transform, "StepDesc", "Walk with WASD.", 16, FontStyles.Normal, new Color(0.8f, 0.85f, 0.9f), new Vector2(-80, -34), new Vector2(780, 30), TextAlignmentOptions.MidlineLeft);

            var finishPanel = SafeVRUIBuilder.CreatePanel(canvas.transform, "FinishPanel", Vector2.zero, new Vector2(640, 360), new Color(0.07f, 0.09f, 0.13f, 0.97f));
            SafeVRUIBuilder.CreateText(finishPanel.transform, "FinishTitle", "TUTORIAL COMPLETE", 26, FontStyles.Bold, new Color(0.2f, 0.85f, 0.95f), new Vector2(0, 110), new Vector2(580, 40), TextAlignmentOptions.Center);
            SafeVRUIBuilder.CreateText(finishPanel.transform, "FinishMsg", "All basics covered. Head to TrainingRoom or back to MainMenu.", 15, FontStyles.Normal, new Color(0.85f, 0.88f, 0.92f), new Vector2(0, 45), new Vector2(540, 60), TextAlignmentOptions.Center);
            var btnContinue = SafeVRUIBuilder.CreateButton(finishPanel.transform, "Btn_Continue", "CONTINUE TO TRAINING", new Vector2(0, -40), new Vector2(420, 56), new Color(0.08f, 0.65f, 0.38f), Color.white, 18);
            var btnMenu = SafeVRUIBuilder.CreateButton(finishPanel.transform, "Btn_Menu", "MAIN MENU", new Vector2(0, -110), new Vector2(420, 48), new Color(0.25f, 0.28f, 0.32f), Color.white, 16);
            finishPanel.SetActive(false);

            // Exit pad doubles as the TutorialManager host (trigger + logic in one object).
            var exitGO = new GameObject("TutorialExit");
            exitGO.transform.position = new Vector3(2.5f, 0f, 2.5f);
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "ExitPad";
            pad.transform.SetParent(exitGO.transform, false);
            pad.transform.localPosition = new Vector3(0, 0.03f, 0);
            pad.transform.localScale = new Vector3(2f, 0.06f, 2f);
            pad.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatGreen;
            var trigger = exitGO.AddComponent<BoxCollider>();
            trigger.size = new Vector3(2.2f, 2f, 2.2f);
            trigger.center = new Vector3(0, 1f, 0);
            trigger.isTrigger = true;
            // Kinematic Rigidbody guarantees OnTriggerEnter delivery for the
            // CharacterController player (no gravity, no physics response).
            var exitRb = exitGO.AddComponent<Rigidbody>();
            exitRb.isKinematic = true;
            exitRb.useGravity = false;

            var manager = exitGO.AddComponent<TutorialManager>();
            var so = new SerializedObject(manager);
            so.FindProperty("stepCounterText").objectReferenceValue = counter;
            so.FindProperty("stepTitleText").objectReferenceValue = title;
            so.FindProperty("stepDescText").objectReferenceValue = desc;
            so.FindProperty("finishPanel").objectReferenceValue = finishPanel;
            so.FindProperty("continueButton").objectReferenceValue = btnContinue;
            so.FindProperty("menuButton").objectReferenceValue = btnMenu;
            so.FindProperty("practiceDoor").objectReferenceValue = practiceDoor;
            so.FindProperty("practiceFire").objectReferenceValue = practiceFire;
            so.ApplyModifiedProperties();
        }

        // --------------------------------------------------------------------
        // Results
        // --------------------------------------------------------------------
        private static void BuildResultsUI()
        {
            var canvas = SafeVRUIBuilder.CreateCanvas("ResultsCanvas");

            // Phase 4 — sectioned review card: PERFORMANCE / OBJECTIVES / SAFETY /
            // TIMELINE / RECOMMENDATIONS / HISTORY. Compact layout fits 1080p.
            var card = SafeVRUIBuilder.CreatePanel(canvas.transform, "ResultsCard", Vector2.zero, new Vector2(1060, 880), new Color(0.07f, 0.09f, 0.13f, 0.96f));
            var title = SafeVRUIBuilder.CreateText(card.transform, "ResultsTitle", "ASSESSMENT RESULTS", 24, FontStyles.Bold, new Color(0.2f, 0.85f, 0.95f), new Vector2(0, 404), new Vector2(1000, 36), TextAlignmentOptions.Center);
            var status = SafeVRUIBuilder.CreateText(card.transform, "StatusText", "—", 14, FontStyles.Bold, Color.white, new Vector2(0, 374), new Vector2(1000, 26), TextAlignmentOptions.Center);
            var score = SafeVRUIBuilder.CreateText(card.transform, "ScoreText", "—", 19, FontStyles.Bold, Color.white, new Vector2(0, 344), new Vector2(1000, 30), TextAlignmentOptions.Center);
            var breakdown = SafeVRUIBuilder.CreateText(card.transform, "BreakdownText", "—", 11, FontStyles.Normal, new Color(0.75f, 0.82f, 0.88f), new Vector2(0, 320), new Vector2(1000, 22), TextAlignmentOptions.Center);
            var time = SafeVRUIBuilder.CreateText(card.transform, "TimeText", "—", 12, FontStyles.Normal, new Color(0.8f, 0.85f, 0.9f), new Vector2(0, 298), new Vector2(1000, 22), TextAlignmentOptions.Center);
            var objectives = SafeVRUIBuilder.CreateText(card.transform, "ObjectivesText", "—", 12, FontStyles.Normal, Color.white, new Vector2(0, 232), new Vector2(1000, 100), TextAlignmentOptions.TopLeft);
            var mistakes = SafeVRUIBuilder.CreateText(card.transform, "MistakesText", "—", 11, FontStyles.Normal, new Color(0.85f, 0.88f, 0.92f), new Vector2(0, 118), new Vector2(1000, 120), TextAlignmentOptions.TopLeft);
            var alarm = SafeVRUIBuilder.CreateText(card.transform, "AlarmText", "—", 12, FontStyles.Normal, Color.white, new Vector2(-260, 44), new Vector2(320, 24), TextAlignmentOptions.MidlineLeft);
            var exit = SafeVRUIBuilder.CreateText(card.transform, "ExitText", "—", 12, FontStyles.Normal, Color.white, new Vector2(60, 44), new Vector2(320, 24), TextAlignmentOptions.MidlineLeft);
            var safeZone = SafeVRUIBuilder.CreateText(card.transform, "SafeZoneText", "—", 12, FontStyles.Normal, Color.white, new Vector2(340, 44), new Vector2(320, 24), TextAlignmentOptions.MidlineLeft);
            var timeline = SafeVRUIBuilder.CreateText(card.transform, "TimelineText", "—", 11, FontStyles.Normal, new Color(0.75f, 0.80f, 0.85f), new Vector2(0, -58), new Vector2(1000, 160), TextAlignmentOptions.TopLeft);
            var recs = SafeVRUIBuilder.CreateText(card.transform, "RecommendationsText", "—", 11, FontStyles.Italic, new Color(0.95f, 0.85f, 0.45f), new Vector2(0, -188), new Vector2(1000, 90), TextAlignmentOptions.TopLeft);
            var history = SafeVRUIBuilder.CreateText(card.transform, "HistoryText", "—", 12, FontStyles.Bold, Color.white, new Vector2(0, -258), new Vector2(1000, 24), TextAlignmentOptions.Center);
            var btnRetry = SafeVRUIBuilder.CreateButton(card.transform, "Btn_Retry", "RETRY ASSESSMENT", new Vector2(-160, -330), new Vector2(300, 54), new Color(0.12f, 0.45f, 0.75f), Color.white, 16);
            var btnMenu = SafeVRUIBuilder.CreateButton(card.transform, "Btn_Menu", "MAIN MENU", new Vector2(160, -330), new Vector2(300, 54), new Color(0.25f, 0.28f, 0.32f), Color.white, 16);

            var ui = canvas.AddComponent<SafeVR.UI.ResultsUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("statusText").objectReferenceValue = status;
            so.FindProperty("scoreText").objectReferenceValue = score;
            so.FindProperty("breakdownText").objectReferenceValue = breakdown;
            so.FindProperty("timeText").objectReferenceValue = time;
            so.FindProperty("objectivesText").objectReferenceValue = objectives;
            so.FindProperty("mistakesText").objectReferenceValue = mistakes;
            so.FindProperty("alarmText").objectReferenceValue = alarm;
            so.FindProperty("exitText").objectReferenceValue = exit;
            so.FindProperty("safeZoneText").objectReferenceValue = safeZone;
            so.FindProperty("timelineText").objectReferenceValue = timeline;
            so.FindProperty("recommendationsText").objectReferenceValue = recs;
            so.FindProperty("historyText").objectReferenceValue = history;
            so.FindProperty("retryButton").objectReferenceValue = btnRetry;
            so.FindProperty("menuButton").objectReferenceValue = btnMenu;
            so.ApplyModifiedProperties();
        }

        // --------------------------------------------------------------------
        // Verification (read-only; never modifies TrainingRoom or Assessment)
        // --------------------------------------------------------------------
        [MenuItem("SafeVR/Verify Tutorial + Results")]
        public static void VerifyTutorialResults()
        {
            int fails = 0;
            void Check(bool cond, string name)
            {
                Debug.Log((cond ? "[PASS] " : "[FAIL] ") + name);
                if (!cond) fails++;
            }

            // Prefab + shared player in both new scenes, no duplicates
            foreach (var path in new[] { "Assets/Scenes/Tutorial.unity", "Assets/Scenes/Results.unity" })
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                string sname = System.IO.Path.GetFileNameWithoutExtension(path);
                var players = Object.FindObjectsByType<Player.DesktopFPSController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Check(players.Length == 1, $"{sname}: exactly 1 player controller (found {players.Length})");
                Check(Object.FindFirstObjectByType<Player.PlayerViewSwitcher>() != null, $"{sname}: PlayerViewSwitcher present");
                int mainCams = 0;
                foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (c.CompareTag("MainCamera")) mainCams++;
                Check(mainCams == 1, $"{sname}: exactly 1 MainCamera (found {mainCams})");
            }

            // Tutorial content
            EditorSceneManager.OpenScene("Assets/Scenes/Tutorial.unity", OpenSceneMode.Single);
            Check(FindIncludingInactive("PracticePanel") != null, "Tutorial: PracticePanel exists");
            Check(FindIncludingInactive("PracticeDoor") != null, "Tutorial: PracticeDoor exists");
            Check(FindIncludingInactive("PracticeExtinguisher") != null, "Tutorial: PracticeExtinguisher exists");
            Check(FindIncludingInactive("PracticeFire") != null, "Tutorial: PracticeFire exists");
            Check(FindIncludingInactive("TutorialExit") != null, "Tutorial: TutorialExit exists");
            Check(FindIncludingInactive("TutorialCanvas") != null, "Tutorial: TutorialCanvas exists");
            Check(FindIncludingInactive("FinishPanel") != null, "Tutorial: FinishPanel exists");
            var tutMgr = Object.FindFirstObjectByType<TutorialManager>();
            Check(tutMgr != null, "Tutorial: TutorialManager exists");
            if (tutMgr != null)
            {
                var so = new SerializedObject(tutMgr);
                Check(so.FindProperty("practiceDoor").objectReferenceValue != null, "Tutorial: practiceDoor wired");
                Check(so.FindProperty("practiceFire").objectReferenceValue != null, "Tutorial: practiceFire wired");
                Check(so.FindProperty("continueButton").objectReferenceValue != null, "Tutorial: continueButton wired");
                Check(so.FindProperty("menuButton").objectReferenceValue != null, "Tutorial: menuButton wired");
                var mgrCol = tutMgr.GetComponent<BoxCollider>();
                Check(mgrCol != null && mgrCol.isTrigger, "Tutorial: exit trigger present");
            }
            Check(Object.FindFirstObjectByType<EvacuationScenarioManager>() == null
                && Object.FindFirstObjectByType<ElectricalFireScenarioManager>() == null,
                "Tutorial: no scenario manager (standalone trainer)");
            // Tutorial pause menu (Resume / Retry / Main Menu) — mobile PAUSE target.
            var tutPause = Object.FindFirstObjectByType<SafeVR.UI.ScenarioPauseMenu>();
            Check(tutPause != null, "Tutorial: ScenarioPauseMenu exists");
            if (tutPause != null)
            {
                var so = new SerializedObject(tutPause);
                string[] pauseProps = { "pausePanel", "resumeButton", "retryButton", "menuButton", "confirmPanel", "confirmRestartButton", "cancelButton" };
                foreach (var p in pauseProps) Check(so.FindProperty(p).objectReferenceValue != null, $"Tutorial: pause {p} wired");
            }

            // Results content
            EditorSceneManager.OpenScene("Assets/Scenes/Results.unity", OpenSceneMode.Single);
            Check(FindIncludingInactive("ResultsCanvas") != null, "Results: ResultsCanvas exists");
            var resultsUI = Object.FindFirstObjectByType<SafeVR.UI.ResultsUI>();
            Check(resultsUI != null, "Results: ResultsUI exists");
            if (resultsUI != null)
            {
                var so = new SerializedObject(resultsUI);
                string[] props = { "titleText", "statusText", "scoreText", "timeText", "objectivesText", "mistakesText", "alarmText", "exitText", "safeZoneText", "recommendationsText", "retryButton", "menuButton" };
                foreach (var p in props) Check(so.FindProperty(p).objectReferenceValue != null, $"Results: {p} wired");
            }

            // Assessment wiring: recorder + View Results button (success-only)
            EditorSceneManager.OpenScene("Assets/Scenes/Assessment.unity", OpenSceneMode.Single);
            Check(Object.FindFirstObjectByType<AssessmentResultsRecorder>() != null, "Assessment: AssessmentResultsRecorder exists");
            var report = Object.FindFirstObjectByType<SafeVR.UI.PerformanceReportUI>();
            Check(report != null, "Assessment: PerformanceReportUI exists");
            if (report != null)
            {
                var so = new SerializedObject(report);
                Check(so.FindProperty("resultsButton").objectReferenceValue != null, "Assessment: resultsButton wired (View Results)");
            }

            // TrainingRoom must NOT have recorder or results button
            EditorSceneManager.OpenScene("Assets/Scenes/TrainingRoom.unity", OpenSceneMode.Single);
            Check(Object.FindFirstObjectByType<AssessmentResultsRecorder>() == null, "TrainingRoom: no recorder (untouched)");
            var trReport = Object.FindFirstObjectByType<SafeVR.UI.PerformanceReportUI>();
            bool trHasResultsBtn = false;
            if (trReport != null)
            {
                var so = new SerializedObject(trReport);
                trHasResultsBtn = so.FindProperty("resultsButton").objectReferenceValue != null;
            }
            Check(!trHasResultsBtn, "TrainingRoom: no results button (untouched)");

            // Store logic: pure C#, works in batchmode
            var m = new ScenarioMetrics
            {
                scenarioType = ScenarioType.Evacuation,
                elapsedTime = 42.5f,
                correctActions = 3,
                totalRequiredActions = 3,
                unsafeActions = 1,
                alarmActivated = true,
                evacuatedSafely = true,
                scenarioCompleted = true,
                actionLog = new System.Collections.Generic.List<string> { "[+][1.0s] ok", "[!][2.0s] bad" }
            };
            AssessmentResultsStore.Save(m, "Evacuation Assessment");
            Check(AssessmentResultsStore.HasResults, "Store: HasResults after Save");
            Check(AssessmentResultsStore.Completed, "Store: Completed copied");
            Check(AssessmentResultsStore.Score == (int)m.SafetyScore, "Store: Score copied from real metrics");
            Check(AssessmentResultsStore.AlarmActivated && AssessmentResultsStore.ReachedSafeZone, "Store: alarm + safe zone copied");
            Check(AssessmentResultsStore.ActionLog.Count == 2, "Store: action log copied");
            Check(AssessmentResultsStore.Recommendations.Count > 0, "Store: recommendations generated");
            AssessmentResultsStore.Clear();
            Check(!AssessmentResultsStore.HasResults, "Store: Clear resets");

            // Build Settings order + navigation
            var buildScenes = EditorBuildSettings.scenes;
            string[] expected = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Tutorial.unity", "Assets/Scenes/TrainingRoom.unity", "Assets/Scenes/Assessment.unity", "Assets/Scenes/Results.unity" };
            Check(buildScenes.Length == expected.Length, $"Build Settings: 5 scenes (found {buildScenes.Length})");
            for (int i = 0; i < expected.Length && i < buildScenes.Length; i++)
                Check(buildScenes[i].path == expected[i] && buildScenes[i].enabled, $"Build Settings[{i}]: {expected[i]}");
            string flowSrc = System.IO.File.ReadAllText("Assets/SafeVR/Scripts/Core/SceneFlowManager.cs");
            Check(flowSrc.Contains("LoadTutorial") && flowSrc.Contains("LoadResults"), "SceneFlowManager: LoadTutorial + LoadResults exist");
            string menuSrc = System.IO.File.ReadAllText("Assets/SafeVR/Scripts/UI/MainMenuUI.cs");
            Check(menuSrc.Contains("LoadTutorial"), "MainMenu: Tutorial button wired to LoadTutorial");

            Debug.Log(fails == 0 ? "[SafeVR Tutorial+Results Verify] ALL CHECKS PASSED" : $"[SafeVR Tutorial+Results Verify] {fails} CHECKS FAILED");
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

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using SafeVR.Core;
using SafeVR.UI;

namespace SafeVR.Editor
{
    public static class SafeVRMainMenuBuilder
    {
        public static void Build()
        {
            SafeVRMaterials.EnsureMaterials();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Lighting & Environment
            GameObject lightObj = new GameObject("Directional Light", typeof(Light));
            Light light = lightObj.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.95f, 0.95f, 1f);
            light.intensity = 1.2f;
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

            // Shared player prefab (same character as TrainingRoom/Assessment).
            // Menu needs ground for the CharacterController plus cursor unlocked for buttons.
            var menuFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            menuFloor.name = "MenuFloor";
            menuFloor.transform.position = new Vector3(0, -0.05f, -4f);
            menuFloor.transform.localScale = new Vector3(30f, 0.1f, 30f);
            menuFloor.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatFloor;
            var menuPlayer = SafeVRPlayerBuilder.InstantiatePlayer(new Vector3(0, 0.05f, -4.2f), 0f, false);

            // Managers
            new GameObject("SceneFlowManager", typeof(SceneFlowManager));
            new GameObject("SoundEffectManager", typeof(SoundEffectManager));

            // UI Canvas
            GameObject canvasObj = SafeVRUIBuilder.CreateCanvas("MainMenuCanvas");

            // Main Menu Card Panel - centered, balanced, no 3D clutter
            GameObject menuCard = SafeVRUIBuilder.CreatePanel(canvasObj.transform, "MenuCard", new Vector2(0, 0), new Vector2(640, 640), new Color(0.07f, 0.09f, 0.13f, 0.96f));

            // Header block with generous spacing
            SafeVRUIBuilder.CreateText(menuCard.transform, "TitleText", "SafeVR", 72, FontStyles.Bold, new Color(0.20f, 0.86f, 0.96f), new Vector2(0, 225), new Vector2(580, 80), TextAlignmentOptions.Center);
            SafeVRUIBuilder.CreateText(menuCard.transform, "SubtitleText", "SAFETY TRAINING SIMULATION", 20, FontStyles.Bold, new Color(0.78f, 0.82f, 0.88f), new Vector2(0, 175), new Vector2(580, 32), TextAlignmentOptions.Center);
            SafeVRUIBuilder.CreatePanel(menuCard.transform, "Divider", new Vector2(0, 142), new Vector2(480, 2), new Color(0.22f, 0.38f, 0.48f, 0.55f));
            SafeVRUIBuilder.CreateText(menuCard.transform, "InfoText", "Professional Workplace Safety  •  Desktop & VR Ready", 16, FontStyles.Normal, new Color(0.68f, 0.74f, 0.80f), new Vector2(0, 110), new Vector2(540, 28), TextAlignmentOptions.Center);

            // Buttons - consistent size, generous gap
            Button btnStart = SafeVRUIBuilder.CreateButton(menuCard.transform, "Btn_StartTraining", "START TRAINING", new Vector2(0, 56), new Vector2(480, 56), new Color(0.08f, 0.62f, 0.38f), Color.white, 20);
            SafeVRUIBuilder.CreateText(menuCard.transform, "TrainDesc", "Scenario 1 — Electrical Fire  •  Isolate power → extinguish → evacuate", 12, FontStyles.Normal, new Color(0.66f, 0.72f, 0.78f), new Vector2(0, 12), new Vector2(540, 16), TextAlignmentOptions.Center);

            Button btnTutorial = SafeVRUIBuilder.CreateButton(menuCard.transform, "Btn_Tutorial", "TUTORIAL", new Vector2(0, -28), new Vector2(480, 56), new Color(0.55f, 0.42f, 0.12f), Color.white, 20);
            SafeVRUIBuilder.CreateText(menuCard.transform, "TutDesc", "Learn controls  •  Move, interact, doors, extinguisher", 12, FontStyles.Normal, new Color(0.66f, 0.72f, 0.78f), new Vector2(0, -64), new Vector2(540, 16), TextAlignmentOptions.Center);

            Button btnAssessment = SafeVRUIBuilder.CreateButton(menuCard.transform, "Btn_Assessment", "ASSESSMENT", new Vector2(0, -104), new Vector2(480, 56), new Color(0.12f, 0.42f, 0.74f), Color.white, 20);
            SafeVRUIBuilder.CreateText(menuCard.transform, "AssessDesc", "Scenario 2 — Evacuation  •  Blocked exit → alternative route → safe zone", 12, FontStyles.Normal, new Color(0.66f, 0.72f, 0.78f), new Vector2(0, -140), new Vector2(540, 16), TextAlignmentOptions.Center);

            Button btnQuit = SafeVRUIBuilder.CreateButton(menuCard.transform, "Btn_Quit", "QUIT", new Vector2(0, -186), new Vector2(480, 44), new Color(0.20f, 0.23f, 0.27f), new Color(0.85f, 0.87f, 0.88f), 16);

            // Controls help - clean pill, not overlapping
            var helpPanel = SafeVRUIBuilder.CreatePanel(menuCard.transform, "HelpPanel", new Vector2(0, -232), new Vector2(560, 32), new Color(0.10f, 0.13f, 0.18f, 0.88f));
            SafeVRUIBuilder.CreateText(helpPanel.transform, "ControlsFooter", "WASD Move  •  Mouse Look  •  E / LMB Interact (HOLD to spray)  •  Shift Sprint  •  Esc Cursor", 12, FontStyles.Normal, new Color(0.62f, 0.66f, 0.72f), Vector2.zero, new Vector2(540, 24), TextAlignmentOptions.Center);

            // Version - subtle, bottom
            SafeVRUIBuilder.CreateText(menuCard.transform, "VersionLabel", "SafeVR v2.1  •  URP  •  OSHA Compliant  •  Scenario-Based", 11, FontStyles.Normal, new Color(0.38f, 0.43f, 0.48f), new Vector2(0, -280), new Vector2(560, 20), TextAlignmentOptions.Center);

            // View-mode label wired to the shared player.
            var viewLabel = SafeVRPlayerBuilder.AddViewModeLabel(canvasObj.transform);
            SafeVRPlayerBuilder.WireViewLabel(menuPlayer, viewLabel);

            // Wire MainMenuUI
            MainMenuUI menuUI = canvasObj.AddComponent<MainMenuUI>();
            SerializedObject so = new SerializedObject(menuUI);
            so.FindProperty("startTrainingButton").objectReferenceValue = btnStart;
            so.FindProperty("tutorialButton").objectReferenceValue = btnTutorial;
            so.FindProperty("assessmentButton").objectReferenceValue = btnAssessment;
            so.FindProperty("quitButton").objectReferenceValue = btnQuit;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
            Debug.Log("[SafeVR] MainMenu scene generated.");
        }

        public static GameObject CreateExtinguisherProp(string name, Vector3 position)
        {
            GameObject root = new GameObject(name);
            root.transform.position = position;

            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "Body";
            cylinder.transform.parent = root.transform;
            cylinder.transform.localPosition = new Vector3(0, 0.45f, 0);
            cylinder.transform.localScale = new Vector3(0.24f, 0.45f, 0.24f);
            cylinder.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatExtinguisher;

            GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = "Dome";
            dome.transform.parent = root.transform;
            dome.transform.localPosition = new Vector3(0, 0.9f, 0);
            dome.transform.localScale = new Vector3(0.24f, 0.15f, 0.24f);
            dome.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatExtinguisher;

            GameObject valve = GameObject.CreatePrimitive(PrimitiveType.Cube);
            valve.name = "Valve";
            valve.transform.parent = root.transform;
            valve.transform.localPosition = new Vector3(0, 1.0f, 0);
            valve.transform.localScale = new Vector3(0.08f, 0.1f, 0.18f);
            valve.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;

            GameObject horn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            horn.name = "HornNozzle";
            horn.transform.parent = root.transform;
            horn.transform.localPosition = new Vector3(0.12f, 0.7f, 0);
            horn.transform.localScale = new Vector3(0.08f, 0.2f, 0.08f);
            horn.transform.localRotation = Quaternion.Euler(0, 0, 20);
            horn.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatFloor;

            // Phase 3 — hose from valve to horn (dark flexible line).
            var hose = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hose.name = "Hose";
            hose.transform.parent = root.transform;
            Vector3 hoseA = new Vector3(0.02f, 1.0f, 0);
            Vector3 hoseB = new Vector3(0.12f, 0.82f, 0);
            Vector3 hoseMid = (hoseA + hoseB) * 0.5f;
            hose.transform.localPosition = hoseMid;
            float hoseLen = Vector3.Distance(hoseA, hoseB);
            hose.transform.localScale = new Vector3(0.035f, hoseLen * 0.5f, 0.035f);
            hose.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (hoseB - hoseA).normalized);
            hose.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatSmoke;

            // Phase 3 — safety pin through the valve + pull ring (hidden once pulled).
            var pin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pin.name = "Pin";
            pin.transform.parent = root.transform;
            pin.transform.localPosition = new Vector3(0.06f, 1.0f, 0.02f);
            pin.transform.localScale = new Vector3(0.09f, 0.025f, 0.025f);
            pin.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatHazard;
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "PinRing";
            ring.transform.parent = root.transform;
            ring.transform.localPosition = new Vector3(0.12f, 1.0f, 0.02f);
            ring.transform.localScale = new Vector3(0.05f, 0.012f, 0.05f);
            ring.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;

            // Phase 3 — pressure/charge gauge dial on the body (per-instance material, tinted by charge).
            var gauge = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gauge.name = "Gauge";
            gauge.transform.parent = root.transform;
            gauge.transform.localPosition = new Vector3(0, 0.55f, 0.125f);
            gauge.transform.localScale = new Vector3(0.09f, 0.02f, 0.09f);
            gauge.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) lit = Shader.Find("Standard");
            var gaugeMat = new Material(lit);
            if (gaugeMat.HasProperty("_BaseColor")) gaugeMat.SetColor("_BaseColor", new Color(0.18f, 0.82f, 0.38f));
            else if (gaugeMat.HasProperty("_Color")) gaugeMat.SetColor("_Color", new Color(0.18f, 0.82f, 0.38f));
            gauge.GetComponent<Renderer>().sharedMaterial = gaugeMat;

            return root;
        }
    }
}

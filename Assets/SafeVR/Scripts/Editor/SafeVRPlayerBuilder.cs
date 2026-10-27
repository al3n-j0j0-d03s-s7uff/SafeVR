using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using SafeVR.Player;
using SafeVR.Interaction;
using SafeVR.Scenario.Equipment;

namespace SafeVR.Editor
{
    /// <summary>
    /// Builds and maintains the single shared SafeVR Player prefab
    /// (Assets/SafeVR/Prefabs/Player.prefab). All scenes instantiate this prefab —
    /// no per-scene duplicate player systems. Mirrors the previous procedural
    /// BuildPlayer settings (controller, speeds, camera, interactor reach,
    /// WeaponHold, HeldExtinguisher) and adds the visible CharacterBody plus
    /// PlayerViewSwitcher (V toggle, first/third person).
    /// </summary>
    public static class SafeVRPlayerBuilder
    {
        public const string PrefabPath = "Assets/SafeVR/Prefabs/Player.prefab";

        [MenuItem("SafeVR/Build Player Prefab")]
        public static GameObject BuildPlayerPrefab()
        {
            SafeVRMaterials.EnsureMaterials();
            if (!AssetDatabase.IsValidFolder("Assets/SafeVR/Prefabs"))
                AssetDatabase.CreateFolder("Assets/SafeVR", "Prefabs");

            var player = new GameObject("Player");
            player.tag = "Player";

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.9f, 0); cc.skinWidth = 0.05f;

            var fps = player.AddComponent<DesktopFPSController>();
            var fpsSO = new SerializedObject(fps);
            fpsSO.FindProperty("walkSpeed").floatValue = 4f;
            fpsSO.FindProperty("sprintSpeed").floatValue = 7f;
            fpsSO.FindProperty("mouseSensitivity").floatValue = 1.8f;
            fpsSO.ApplyModifiedProperties();

            BuildCharacterBody(player);

            var camObj = new GameObject("PlayerCamera", typeof(Camera), typeof(AudioListener));
            camObj.tag = "MainCamera";
            camObj.transform.SetParent(player.transform, false);
            camObj.transform.localPosition = new Vector3(0, 1.6f, 0);
            camObj.transform.localRotation = Quaternion.identity;
            var cam = camObj.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.08f, 0.12f);
            cam.nearClipPlane = 0.1f;
            fpsSO.FindProperty("playerCamera").objectReferenceValue = camObj.transform;
            fpsSO.ApplyModifiedProperties();

            var inter = camObj.AddComponent<Interactor>();
            var iso = new SerializedObject(inter);
            iso.FindProperty("rayOrigin").objectReferenceValue = camObj.transform;
            iso.FindProperty("maxReachDistance").floatValue = 7.0f;
            iso.FindProperty("listenForInputActions").boolValue = true;
            iso.ApplyModifiedProperties();

            // WeaponHold anchor in front of camera (desktop hand proxy, VR-ready)
            var weaponHold = new GameObject("WeaponHold");
            weaponHold.transform.SetParent(camObj.transform, false);
            weaponHold.transform.localPosition = new Vector3(0.42f, -0.28f, 0.58f);
            weaponHold.transform.localRotation = Quaternion.Euler(-12f, -8f, 0f);

            // HeldExtinguisher (pre-built, inactive until pickup) — visible in Hierarchy
            var held = SafeVRMainMenuBuilder.CreateExtinguisherProp("HeldExtinguisher", Vector3.zero);
            held.transform.SetParent(weaponHold.transform, false);
            held.transform.localPosition = Vector3.zero;
            held.transform.localRotation = Quaternion.identity;
            held.transform.localScale = Vector3.one * 0.85f;
            foreach (var col in held.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
            var heldMarker = held.AddComponent<HeldExtinguisher>();
            var horn = held.transform.Find("HornNozzle");
            Transform nozzle = horn != null ? horn : held.transform;
            var sprayGO = new GameObject("SprayPS");
            sprayGO.transform.SetParent(nozzle, false);
            sprayGO.transform.localPosition = new Vector3(0f, -0.12f, 0f);
            var sprayPS = sprayGO.AddComponent<ParticleSystem>();
            SafeVRSceneBuilder.ConfigureSprayParticles(sprayPS);
            sprayPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var heldSO = new SerializedObject(heldMarker);
            heldSO.FindProperty("nozzle").objectReferenceValue = nozzle;
            heldSO.FindProperty("sprayParticles").objectReferenceValue = sprayPS;
            var heldPin = held.transform.Find("Pin");
            if (heldPin != null) heldSO.FindProperty("pinVisual").objectReferenceValue = heldPin.gameObject;
            var heldGauge = held.transform.Find("Gauge");
            if (heldGauge != null) heldSO.FindProperty("gaugeRenderer").objectReferenceValue = heldGauge.GetComponent<Renderer>();
            heldSO.ApplyModifiedProperties();
            held.SetActive(false);

            player.AddComponent<PlayerViewSwitcher>();

            // Add MobileHUD with Canvas for Android touch controls
            BuildMobileHUD(player);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(player, PrefabPath);
            Object.DestroyImmediate(player);
            AssetDatabase.Refresh();
            Debug.Log("[SafeVR] Player prefab saved at " + PrefabPath);
            return prefab;
        }

        /// <summary>Visible humanoid body. All mesh colliders stripped so the interaction
        /// ray and third-person camera collision never hit the player's own body.</summary>
        private static void BuildCharacterBody(GameObject player)
        {
            var body = new GameObject("CharacterBody");
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;

            AddBodyPart(body.transform, "LegL", PrimitiveType.Capsule, new Vector3(-0.11f, 0.38f, 0f), new Vector3(0.18f, 0.75f, 0.18f), SafeVRMaterials.MatFloor);
            AddBodyPart(body.transform, "LegR", PrimitiveType.Capsule, new Vector3(0.11f, 0.38f, 0f), new Vector3(0.18f, 0.75f, 0.18f), SafeVRMaterials.MatFloor);
            AddBodyPart(body.transform, "Torso", PrimitiveType.Capsule, new Vector3(0f, 1.05f, 0f), new Vector3(0.42f, 0.75f, 0.26f), SafeVRMaterials.MatMetal);
            AddBodyPart(body.transform, "Vest", PrimitiveType.Cube, new Vector3(0f, 1.1f, 0f), new Vector3(0.44f, 0.4f, 0.3f), SafeVRMaterials.MatHazard);
            AddBodyPart(body.transform, "ArmL", PrimitiveType.Capsule, new Vector3(-0.3f, 1.05f, 0f), new Vector3(0.13f, 0.6f, 0.13f), SafeVRMaterials.MatMetal);
            AddBodyPart(body.transform, "ArmR", PrimitiveType.Capsule, new Vector3(0.3f, 1.05f, 0f), new Vector3(0.13f, 0.6f, 0.13f), SafeVRMaterials.MatMetal);
            AddBodyPart(body.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.62f, 0f), new Vector3(0.26f, 0.26f, 0.26f), SafeVRMaterials.MatCeiling);
            AddBodyPart(body.transform, "Helmet", PrimitiveType.Sphere, new Vector3(0f, 1.72f, 0f), new Vector3(0.3f, 0.2f, 0.3f), SafeVRMaterials.MatHazard);
        }

        private static void AddBodyPart(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        /// <summary>Creates and wires the MobileHUD with Canvas for Android touch controls.</summary>
        private static void BuildMobileHUD(GameObject player)
        {
            // Create Canvas GameObject
            var canvasGO = new GameObject("MobileHUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasGO.transform.SetParent(player.transform, false);
            canvasGO.layer = 5; // UI layer

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Add MobileHUD component
            var mobileHUD = canvasGO.AddComponent<MobileHUD>();

            // Wire references using SerializedObject (only viewSwitcher remains serialized)
            var hudSO = new SerializedObject(mobileHUD);

            // viewSwitcher - find on player
            var viewSwitcher = player.GetComponent<PlayerViewSwitcher>();
            hudSO.FindProperty("viewSwitcher").objectReferenceValue = viewSwitcher;

            hudSO.ApplyModifiedProperties();
        }

        /// <summary>Instantiates the shared prefab into the open scene (building it first if missing).</summary>
        public static GameObject InstantiatePlayer(Vector3 position, float yaw, bool lockCursorOnStart)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) prefab = BuildPlayerPrefab();
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.transform.position = position;
            player.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var fps = player.GetComponent<DesktopFPSController>();
            if (fps != null)
            {
                var so = new SerializedObject(fps);
                so.FindProperty("lockCursorOnStart").boolValue = lockCursorOnStart;
                so.ApplyModifiedProperties();
            }
            return player;
        }

        /// <summary>Creates the small bottom-left view-mode label on a scene canvas.</summary>
        public static TMP_Text AddViewModeLabel(Transform canvasTransform)
        {
            var panel = SafeVRUIBuilder.CreatePanel(canvasTransform, "ViewModePanel", Vector2.zero, new Vector2(360, 30), new Color(0f, 0f, 0f, 0.55f));
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(24f, 24f);
            return SafeVRUIBuilder.CreateText(panel.transform, "ViewModeLabel", "First Person  •  [V] Third Person",
                14, FontStyles.Normal, new Color(0.75f, 0.8f, 0.85f), Vector2.zero, new Vector2(340, 24), TextAlignmentOptions.BottomLeft);
        }

        public static void WireViewLabel(GameObject player, TMP_Text label)
        {
            if (player == null || label == null) return;
            var switcher = player.GetComponent<PlayerViewSwitcher>();
            if (switcher == null) return;
            var so = new SerializedObject(switcher);
            so.FindProperty("viewLabel").objectReferenceValue = label;
            so.ApplyModifiedProperties();
        }

        [MenuItem("SafeVR/Verify Player Setup")]
        public static void VerifyPlayerSetup()
        {
            int fails = 0;
            void Check(bool cond, string name)
            {
                Debug.Log((cond ? "[PASS] " : "[FAIL] ") + name);
                if (!cond) fails++;
            }

            Check(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null, "Prefab Assets/SafeVR/Prefabs/Player.prefab exists");
            string actions = System.IO.File.ReadAllText("Assets/InputSystem_Actions.inputactions");
            Check(actions.Contains("\"ToggleView\""), "Input action Player/ToggleView exists");
            Check(actions.Contains("<Keyboard>/v"), "ToggleView bound to <Keyboard>/v");

            string[] scenes = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/TrainingRoom.unity", "Assets/Scenes/Assessment.unity" };
            foreach (var path in scenes)
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Single);
                string sname = System.IO.Path.GetFileNameWithoutExtension(path);
                var players = Object.FindObjectsByType<DesktopFPSController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Check(players.Length == 1, $"{sname}: exactly 1 player controller (found {players.Length})");
                var switchers = Object.FindObjectsByType<PlayerViewSwitcher>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Check(switchers.Length == 1, $"{sname}: exactly 1 PlayerViewSwitcher (found {switchers.Length})");
                var cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                int mainCams = 0; foreach (var c in cams) if (c.CompareTag("MainCamera")) mainCams++;
                Check(mainCams == 1, $"{sname}: exactly 1 MainCamera (found {mainCams})");
                var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Check(listeners.Length == 1, $"{sname}: exactly 1 AudioListener (found {listeners.Length})");
                Check(GameObject.Find("Main Camera") == null, $"{sname}: no legacy static 'Main Camera'");
                Check(FindIncludingInactive("PlayerCamera") != null, $"{sname}: PlayerCamera exists");
                Check(FindIncludingInactive("CharacterBody") != null, $"{sname}: CharacterBody exists");
                Check(FindIncludingInactive("WeaponHold") != null, $"{sname}: WeaponHold exists");
                Check(FindIncludingInactive("HeldExtinguisher") != null, $"{sname}: HeldExtinguisher exists");
                Check(FindIncludingInactive("ViewModeLabel") != null, $"{sname}: ViewModeLabel exists");
                Check(FindIncludingInactive("MobileHUDCanvas") != null, $"{sname}: MobileHUDCanvas exists");
                var mobileHUD = Object.FindFirstObjectByType<SafeVR.Player.MobileHUD>();
                Check(mobileHUD != null, $"{sname}: MobileHUD component exists");
                var inter = Object.FindFirstObjectByType<Interactor>();
                bool rayOk = false;
                if (inter != null)
                {
                    var iso = new SerializedObject(inter);
                    var origin = iso.FindProperty("rayOrigin").objectReferenceValue as Transform;
                    rayOk = origin != null && origin.name == "PlayerCamera";
                }
                Check(rayOk, $"{sname}: Interactor ray originates from PlayerCamera");
            }

            // Gameplay systems preserved after player swap
            var tr = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/TrainingRoom.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
            Check(FindIncludingInactive("DoorHinge") != null, "TrainingRoom: DoorHinge intact");
            Check(FindIncludingInactive("SafeZoneTrigger") != null, "TrainingRoom: SafeZoneTrigger intact");
            Check(FindIncludingInactive("FirePS") != null, "TrainingRoom: FirePS intact");
            Check(Object.FindFirstObjectByType<DoorController>() != null, "TrainingRoom: DoorController intact");
            var ass = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Assessment.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
            Check(FindIncludingInactive("PrimaryDoor") != null, "Assessment: PrimaryDoor intact");
            Check(FindIncludingInactive("EvacuationPathway") != null, "Assessment: EvacuationPathway intact");
            Check(FindIncludingInactive("SafeZoneTrigger") != null, "Assessment: SafeZoneTrigger intact");

            Debug.Log(fails == 0 ? "[SafeVRPlayerVerify] ALL CHECKS PASSED" : $"[SafeVRPlayerVerify] {fails} CHECKS FAILED");
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

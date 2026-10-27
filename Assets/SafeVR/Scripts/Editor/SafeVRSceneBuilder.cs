using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using SafeVR.Core;
using SafeVR.Player;
using SafeVR.Interaction;
using SafeVR.Scenario;
using SafeVR.Scenario.Equipment;

namespace SafeVR.Editor
{
    public static class SafeVRSceneBuilder
    {
        private const string SCENE_DIR = "Assets/Scenes";

        [MenuItem("SafeVR/Build All Scenes (MainMenu + Tutorial + TrainingRoom + Assessment + Results)")]
        public static void BuildAll()
        {
            SafeVRMaterials.EnsureMaterials();
            SafeVRMainMenuBuilder.Build();
            SafeVRTutorialResultsBuilder.BuildTutorial();
            BuildTrainingRoom();
            BuildAssessment();
            SafeVRTutorialResultsBuilder.BuildResults();
            ConfigureBuildSettings();
            Debug.Log("[SafeVR] All 5 scenes built successfully. See File > Build Settings.");
        }

        [MenuItem("SafeVR/Build TrainingRoom Only")]
        public static void BuildTrainingRoom()
        {
            SafeVRMaterials.EnsureMaterials();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildLightingAndVolume();
            BuildRoomShell(12f, 12f, 3.2f);
            BuildPlayer(new Vector3(0f, 1f, -4f), 0f);
            BuildElectricalFireScenario();
            BuildScenarioHUD(true);
            BuildScenarioPauseMenu();
            BuildPerformanceReportUI();
            new GameObject("ScenarioAttemptRecorder", typeof(ScenarioAttemptRecorder));
            EnsureCoreManagersScenario();
            EnsureSceneDir();
            EditorSceneManager.SaveScene(scene, $"{SCENE_DIR}/TrainingRoom.unity");
            Debug.Log("[SafeVR] TrainingRoom (Electrical Fire) generated.");
        }

        [MenuItem("SafeVR/Build Assessment Only")]
        public static void BuildAssessment()
        {
            SafeVRMaterials.EnsureMaterials();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildLightingAndVolume();
            BuildRoomShell(12f, 12f, 3.2f);
            BuildPlayer(new Vector3(0f, 1f, -4.2f), 0f);
            BuildEvacuationScenario();
            BuildScenarioHUD(false);
            BuildScenarioPauseMenu();
            BuildPerformanceReportUI(includeResultsButton: true);
            new GameObject("AssessmentResultsRecorder", typeof(AssessmentResultsRecorder));
            EnsureCoreManagersScenario();
            EnsureSceneDir();
            EditorSceneManager.SaveScene(scene, $"{SCENE_DIR}/Assessment.unity");
            Debug.Log("[SafeVR] Assessment (Evacuation) generated.");
        }

        [MenuItem("SafeVR/Configure Build Settings")]
        public static void ConfigureBuildSettings()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene($"{SCENE_DIR}/MainMenu.unity", true),
                new EditorBuildSettingsScene($"{SCENE_DIR}/Tutorial.unity", true),
                new EditorBuildSettingsScene($"{SCENE_DIR}/TrainingRoom.unity", true),
                new EditorBuildSettingsScene($"{SCENE_DIR}/Assessment.unity", true),
                new EditorBuildSettingsScene($"{SCENE_DIR}/Results.unity", true),
            };
            EditorBuildSettings.scenes = scenes;
            Debug.Log("[SafeVR] Build Settings configured.");
        }

        // --------------------------------------------------------------------
        // Lighting & Volume
        // --------------------------------------------------------------------
        internal static void BuildLightingAndVolume()
        {
            // Daylight primary
            GameObject lightObj = new GameObject("Directional Light", typeof(Light));
            Light light = lightObj.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.98f, 0.92f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.85f;
            light.shadowBias = 0.04f;
            light.shadowNormalBias = 0.4f;
            lightObj.transform.rotation = Quaternion.Euler(48, -35, 0);

            // Cool fill for contrast
            GameObject fillObj = new GameObject("Fill Light", typeof(Light));
            Light fill = fillObj.GetComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.78f, 0.85f, 1f);
            fill.intensity = 0.42f;
            fill.shadows = LightShadows.None;
            fillObj.transform.rotation = Quaternion.Euler(28, 135, 0);

            // Warm indoor ceiling lights
            var ceilLight1 = new GameObject("CeilingLight_A", typeof(Light));
            var l1 = ceilLight1.GetComponent<Light>();
            l1.type = LightType.Point; l1.color = new Color(1f,0.95f,0.88f); l1.range = 9f; l1.intensity = 1.1f; l1.shadows = LightShadows.Soft;
            ceilLight1.transform.position = new Vector3(-2f, 2.9f, 1.5f);
            var ceilLight2 = new GameObject("CeilingLight_B", typeof(Light));
            var l2 = ceilLight2.GetComponent<Light>();
            l2.type = LightType.Point; l2.color = new Color(1f,0.95f,0.88f); l2.range = 9f; l2.intensity = 1.1f; l2.shadows = LightShadows.Soft;
            ceilLight2.transform.position = new Vector3(2.5f, 2.9f, -1.2f);

            // Ambient / environment
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.82f, 0.86f, 0.92f) * 0.55f;
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.66f, 0.72f) * 0.45f;
            RenderSettings.ambientGroundColor = new Color(0.32f, 0.34f, 0.38f) * 0.95f;
            RenderSettings.ambientIntensity = 1.05f;
            RenderSettings.reflectionIntensity = 0.35f;

            GameObject volObj = new GameObject("Global Volume", typeof(UnityEngine.Rendering.Volume));
            var vol = volObj.GetComponent<UnityEngine.Rendering.Volume>();
            vol.isGlobal = true;
            vol.priority = 0;
            var profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>("Assets/Settings/DefaultVolumeProfile.asset");
            if (profile == null) profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
            if (profile != null) vol.sharedProfile = profile;
        }

        internal static void BuildRoomShell(float roomW, float roomD, float wallH)
        {
            float wallT = 0.2f;
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0, -0.05f, 0);
            floor.transform.localScale = new Vector3(roomW, 0.1f, roomD);
            floor.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatFloor;

            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.position = new Vector3(0, wallH + 0.05f, 0);
            ceiling.transform.localScale = new Vector3(roomW, 0.1f, roomD);
            ceiling.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatCeiling;

            // North wall split to leave a real 1.4m doorway gap at x=4.5 for evacuation.
            // Door opening: x 3.8..5.2, height 0..2.4. Lintel above door.
            float doorCx = 4.5f;
            float doorW = 1.4f;
            float doorH = 2.4f;
            float nz = roomD / 2f;
            float leftSegRight = doorCx - doorW / 2f;   // 3.8
            float rightSegLeft = doorCx + doorW / 2f;   // 5.2
            float leftW = leftSegRight - (-roomW / 2f); // 3.8+6 = 9.8
            float rightW = (roomW / 2f) - rightSegLeft; // 6-5.2 = 0.8
            CreateWall("Wall_North_Left", new Vector3((-roomW / 2f + leftSegRight) / 2f, wallH / 2f, nz), new Vector3(leftW, wallH, wallT));
            CreateWall("Wall_North_Right", new Vector3((rightSegLeft + roomW / 2f) / 2f, wallH / 2f, nz), new Vector3(rightW, wallH, wallT));
            // Lintel above doorway
            var lintelGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lintelGO.name = "Wall_North_Lintel";
            lintelGO.transform.position = new Vector3(doorCx, doorH + (wallH - doorH) / 2f, nz);
            lintelGO.transform.localScale = new Vector3(doorW, wallH - doorH, wallT);
            lintelGO.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatWall;
            CreateWall("Wall_South", new Vector3(0, wallH / 2f, -roomD / 2f), new Vector3(roomW, wallH, wallT));
            CreateWall("Wall_East", new Vector3(roomW / 2f, wallH / 2f, 0), new Vector3(wallT, wallH, roomD));
            CreateWall("Wall_West", new Vector3(-roomW / 2f, wallH / 2f, 0), new Vector3(wallT, wallH, roomD));

            CreateDesk(new Vector3(-3.2f, 0, 2.5f));
            CreateDesk(new Vector3(3.0f, 0, -1.8f));
            CreateDesk(new Vector3(0.2f, 0, 0.8f));

            // Baseboard trim for contrast and scale
            CreateBaseboard(roomW, roomD);
            // Ceiling light fixtures (visual)
            CreateCeilingFixture(new Vector3(-2f, 3.14f, 1.5f));
            CreateCeilingFixture(new Vector3( 2.5f, 3.14f,-1.2f));

            // Warning signs on walls
            CreateWarningSign(new Vector3(-5.85f, 1.6f, 0.8f), Quaternion.Euler(0,90,0));
            CreateWarningSign(new Vector3(5.85f, 1.6f, -1.5f), Quaternion.Euler(0,-90,0));
            // Exit sign above north exit
            GameObject exitSign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            exitSign.name = "ExitSign_North";
            exitSign.transform.position = new Vector3(4.5f, 2.35f, 5.75f);
            exitSign.transform.localScale = new Vector3(0.9f, 0.25f, 0.06f);
            exitSign.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatGreen;
        }

        private static void CreateWall(string name, Vector3 pos, Vector3 scale)
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w.name = name;
            w.transform.position = pos;
            w.transform.localScale = scale;
            w.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatWall;
        }

        private static void CreateDesk(Vector3 pos)
        {
            var desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            desk.name = "Desk";
            desk.transform.position = pos + new Vector3(0, 0.4f, 0);
            desk.transform.localScale = new Vector3(1.6f, 0.08f, 0.9f);
            desk.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatDesk;
            for (int ix = -1; ix <= 1; ix += 2)
                for (int iz = -1; iz <= 1; iz += 2)
                {
                    var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    leg.transform.parent = desk.transform;
                    leg.transform.localPosition = new Vector3(ix * 0.62f, -0.5f, iz * 0.32f);
                    leg.transform.localScale = new Vector3(0.08f, 1f, 0.08f);
                    leg.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;
                }
        }

        private static void CreateWarningSign(Vector3 pos, Quaternion rot)
        {
            var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "WarningSign";
            sign.transform.position = pos;
            sign.transform.rotation = rot;
            sign.transform.localScale = new Vector3(0.6f, 0.45f, 0.04f);
            sign.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatHazard;
        }

        private static void CreateBaseboard(float roomW, float roomD)
        {
            float h = 0.12f; float t = 0.05f; float y = 0.06f;
            var c = new Color(0.18f, 0.19f, 0.21f);
            CreateTrim("Baseboard_N", new Vector3(0, y, roomD/2 - t), new Vector3(roomW+0.4f, h, t), c);
            CreateTrim("Baseboard_S", new Vector3(0, y, -roomD/2 + t), new Vector3(roomW+0.4f, h, t), c);
            CreateTrim("Baseboard_E", new Vector3(roomW/2 - t, y, 0), new Vector3(t, h, roomD), c);
            CreateTrim("Baseboard_W", new Vector3(-roomW/2 + t, y, 0), new Vector3(t, h, roomD), c);
        }
        private static void CreateTrim(string name, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            var mat = SafeVRMaterials.MatMetal;
            // use shared metal but tint slightly via property block would be ideal; keep simple
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }
        private static void CreateCeilingFixture(Vector3 pos)
        {
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseObj.name = "CeilingFixture";
            baseObj.transform.position = pos;
            baseObj.transform.localScale = new Vector3(1.2f, 0.06f, 0.6f);
            baseObj.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;
            var lens = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lens.name = "Lens";
            lens.transform.parent = baseObj.transform;
            lens.transform.localPosition = new Vector3(0, -0.06f, 0);
            lens.transform.localScale = new Vector3(0.92f, 0.18f, 0.82f);
            // Use emissive ceil mat via hazard? keep metal with emission via temp material instance
            var rend = lens.GetComponent<Renderer>();
            rend.sharedMaterial = SafeVRMaterials.MatCeiling;
            if (rend.material.HasProperty("_EmissionColor"))
            {
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", new Color(1f,0.97f,0.88f)*0.9f);
            }
        }

        private static void BuildPlayer(Vector3 position, float yaw)
        {
            // Single shared prefab for every scene — no per-scene duplicate player systems.
            SafeVRPlayerBuilder.InstantiatePlayer(position, yaw, true);
        }

        internal static void ConfigureSprayParticles(ParticleSystem ps)
        {
            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = 0.45f;
            main.startSpeed = 6f;
            main.startSize = 0.14f;
            main.gravityModifier = 0.05f;
            main.startColor = new Color(0.93f, 0.96f, 1f, 0.85f);
            main.maxParticles = 300;
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 120f;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 9f;
            shape.radius = 0.05f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.8f, 0.85f, 0.9f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var rend = ps.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            if (rend.material.HasProperty("_BaseColor")) rend.material.SetColor("_BaseColor", Color.white);
        }

        internal static ParticleSystem CreateFireParticles(GameObject parent, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 2f;
            main.loop = true;
            main.startLifetime = 0.7f;
            main.startSpeed = 1.1f;
            main.startSize = 0.28f;
            main.gravityModifier = -0.05f;
            main.startColor = Color.white;
            main.maxParticles = 400;
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f; // driven by FireController intensity
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 14f;
            shape.radius = 0.22f;
            shape.position = Vector3.zero;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.85f, 0.25f), 0f),
                    new GradientColorKey(new Color(1f, 0.42f, 0.08f), 0.45f),
                    new GradientColorKey(new Color(0.45f, 0.08f, 0.02f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
            var rend = ps.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            if (rend.material.HasProperty("_BaseColor")) rend.material.SetColor("_BaseColor", new Color(1f, 0.6f, 0.15f));
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return ps;
        }

        private static ParticleSystem CreateSmokeParticles(GameObject parent, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 4f;
            main.loop = true;
            main.startLifetime = 2.6f;
            main.startSpeed = 1.0f;
            main.startSize = 0.55f;
            main.gravityModifier = -0.08f;
            main.startColor = new Color(0.25f, 0.25f, 0.27f, 0.5f);
            main.maxParticles = 250;
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f; // driven by SmokeController density
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.6f, 0.15f, 1.6f);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.22f, 0.22f, 0.24f), 0f),
                    new GradientColorKey(new Color(0.35f, 0.35f, 0.38f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.55f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(0.6f, 2.4f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.y = new ParticleSystem.MinMaxCurve(0.7f, 1.5f);
            var rend = ps.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            if (rend.material.HasProperty("_BaseColor")) rend.material.SetColor("_BaseColor", new Color(0.3f, 0.3f, 0.32f, 0.5f));
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return ps;
        }

        // --------------------------------------------------------------------
        // ELECTRICAL FIRE SCENARIO (TrainingRoom)
        // --------------------------------------------------------------------
        private static void BuildElectricalFireScenario()
        {
            // Scenario Manager root
            var scenarioGO = new GameObject("ElectricalFireScenarioManager");
            var scenario = scenarioGO.AddComponent<ElectricalFireScenarioManager>();

            // Alarm light on ceiling
            var alarmGO = new GameObject("AlarmBeacon");
            alarmGO.transform.position = new Vector3(0, 3.0f, 0);
            var alarmMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            alarmMesh.name = "AlarmMesh";
            alarmMesh.transform.parent = alarmGO.transform;
            alarmMesh.transform.localPosition = Vector3.zero;
            alarmMesh.transform.localScale = new Vector3(0.5f, 0.25f, 0.5f);
            alarmMesh.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatAlarm;
            var alarmLightObj = new GameObject("AlarmLight", typeof(Light));
            alarmLightObj.transform.parent = alarmGO.transform;
            alarmLightObj.transform.localPosition = new Vector3(0, -0.2f, 0);
            var alarmLight = alarmLightObj.GetComponent<Light>();
            alarmLight.type = LightType.Point; alarmLight.color = new Color(1f,0.15f,0.15f); alarmLight.range = 8f; alarmLight.intensity = 0f;
            var alarmCtrl = alarmGO.AddComponent<AlarmController>();
            var alarmSO = new SerializedObject(alarmCtrl);
            alarmSO.FindProperty("alarmLight").objectReferenceValue = alarmLight;
            alarmSO.FindProperty("alarmRenderer").objectReferenceValue = alarmMesh.GetComponent<Renderer>();
            alarmSO.ApplyModifiedProperties();

            // Electrical Panel on west wall
            var panelGO = new GameObject("ElectricalPanel");
            panelGO.transform.position = new Vector3(-5.75f, 1.25f, 0.2f);
            panelGO.transform.rotation = Quaternion.Euler(0,90,0);
            var panelBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panelBox.name = "PanelBox";
            panelBox.transform.parent = panelGO.transform;
            panelBox.transform.localPosition = Vector3.zero;
            panelBox.transform.localScale = new Vector3(0.9f, 1.1f, 0.18f);
            panelBox.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;
            // Warning stripes
            var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "HazardStripe";
            stripe.transform.parent = panelGO.transform;
            stripe.transform.localPosition = new Vector3(0, 0.35f, 0.11f);
            stripe.transform.localScale = new Vector3(0.7f, 0.15f, 0.02f);
            stripe.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatHazard;
            // Spark VFX cube
            var sparkVFX = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sparkVFX.name = "SparkVFX";
            sparkVFX.transform.parent = panelGO.transform;
            sparkVFX.transform.localPosition = new Vector3(0, -0.05f, 0.18f);
            sparkVFX.transform.localScale = new Vector3(0.55f, 0.5f, 0.12f);
            sparkVFX.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatHazard;
            var sparkLightObj = new GameObject("SparkLight", typeof(Light));
            sparkLightObj.transform.parent = sparkVFX.transform;
            sparkLightObj.transform.localPosition = Vector3.zero;
            var sparkLight = sparkLightObj.GetComponent<Light>();
            sparkLight.type = LightType.Point; sparkLight.color = new Color(1f,0.65f,0.15f); sparkLight.range = 3.5f; sparkLight.intensity = 0f;
            var sparkAudio = panelGO.AddComponent<AudioSource>();
            sparkAudio.loop = true; sparkAudio.playOnAwake = false; sparkAudio.spatialBlend = 1f; sparkAudio.volume = 0.5f;

            // Fire volume at base of panel (floor) — REAL particle fire, no cube
            var fireRoot = new GameObject("FireVolume");
            fireRoot.transform.position = new Vector3(-4.9f, 0.12f, 0.2f);
            // Charred base (small dark disc, not a fire cube)
            var charBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            charBase.name = "CharBase";
            charBase.transform.SetParent(fireRoot.transform, false);
            charBase.transform.localPosition = new Vector3(0, 0.03f, 0);
            charBase.transform.localScale = new Vector3(1.1f, 0.06f, 1.1f);
            charBase.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatFloor;
            var fireLightObj = new GameObject("FireLight", typeof(Light));
            fireLightObj.transform.parent = fireRoot.transform;
            fireLightObj.transform.localPosition = new Vector3(0, 0.6f, 0);
            var fireLight = fireLightObj.GetComponent<Light>();
            fireLight.type = LightType.Point; fireLight.color = new Color(1f,0.42f,0.08f); fireLight.range = 6f; fireLight.intensity = 0f;
            // REAL FIRE PARTICLES
            var firePS = CreateFireParticles(fireRoot, "FirePS", new Vector3(0, 0.15f, 0));
            // Ember quad shown after extinguish
            var ember = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ember.name = "EmberBed";
            ember.transform.SetParent(fireRoot.transform, false);
            ember.transform.localPosition = new Vector3(0, 0.06f, 0);
            ember.transform.localScale = new Vector3(0.9f, 0.05f, 0.9f);
            ember.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatFire;
            ember.SetActive(false);
            // REAL SMOKE PARTICLES (child of fire root for hierarchy clarity)
            var smokeGO = new GameObject("SmokeVolume");
            smokeGO.transform.parent = fireRoot.transform;
            smokeGO.transform.localPosition = new Vector3(0, 1.1f, 0);
            var smokePS = CreateSmokeParticles(smokeGO, "SmokePS", Vector3.zero);

            var fireCtrl = fireRoot.AddComponent<FireController>();
            var fireSO = new SerializedObject(fireCtrl);
            fireSO.FindProperty("fireParticles").objectReferenceValue = firePS;
            fireSO.FindProperty("emberMesh").objectReferenceValue = ember;
            fireSO.FindProperty("fireLight").objectReferenceValue = fireLight;
            fireSO.ApplyModifiedProperties();

            var smokeCtrl = smokeGO.AddComponent<SmokeController>();
            var smokeSO = new SerializedObject(smokeCtrl);
            smokeSO.FindProperty("smokeParticles").objectReferenceValue = smokePS;
            smokeSO.ApplyModifiedProperties();

            var hazardCtrl = panelGO.AddComponent<HazardController>();
            var hazardSO = new SerializedObject(hazardCtrl);
            hazardSO.FindProperty("sparkVFX").objectReferenceValue = sparkVFX;
            hazardSO.FindProperty("sparkLight").objectReferenceValue = sparkLight;
            hazardSO.FindProperty("sparkAudio").objectReferenceValue = sparkAudio;
            hazardSO.FindProperty("fireController").objectReferenceValue = fireCtrl;
            hazardSO.ApplyModifiedProperties();

            // Discharge hold interactable on fire volume - large forgiving collider
            var dischargeCol = fireRoot.AddComponent<BoxCollider>();
            dischargeCol.center = new Vector3(0, 0.55f, 0);
            dischargeCol.size = new Vector3(3.2f, 2.0f, 3.2f);
            dischargeCol.isTrigger = false;
            var discharge = fireRoot.AddComponent<FireDischargeInteractable>();
            var disSO = new SerializedObject(discharge);
            disSO.FindProperty("fireController").objectReferenceValue = fireCtrl;
            disSO.FindProperty("requiredHold").floatValue = 3.5f;
            // Particles/audio for discharge
            var dischargeParticlesGO = new GameObject("DischargeParticles");
            dischargeParticlesGO.transform.parent = fireRoot.transform;
            dischargeParticlesGO.transform.localPosition = new Vector3(0, 0.5f, -0.8f);
            // Simple placeholder: use empty with AudioSource; particles optional
            var dischargeAudio = dischargeParticlesGO.AddComponent<AudioSource>();
            dischargeAudio.loop = true; dischargeAudio.playOnAwake = false;
            disSO.FindProperty("dischargeAudio").objectReferenceValue = dischargeAudio;
            disSO.ApplyModifiedProperties();

            // Phase 2 — Two extinguisher stations on east wall: CO2 (correct, Class C) + Water (wrong, Class A).
            // Player must inspect labels (first [E]) then equip (second [E]); scenario enforces the correct choice.
            BuildExtinguisherStation("FireExtinguisher_CO2", new Vector3(5.25f, 0.38f, -1.4f),
                ExtinguisherType.CO2, SafeVRMaterials.MatExtinguisher, SafeVRMaterials.MatGreen,
                "CO2 — CLASS C\nELECTRICAL SAFE", new Color(0.35f, 1f, 0.6f));
            BuildExtinguisherStation("FireExtinguisher_Water", new Vector3(5.25f, 0.38f, 1.8f),
                ExtinguisherType.Water, SafeVRMaterials.MatWater, SafeVRMaterials.MatWater,
                "WATER — CLASS A\nDO NOT USE ON ELECTRICAL", new Color(1f, 0.45f, 0.4f));

            // Cutoff switch near panel (south of panel)
            var switchGO = new GameObject("EmergencyCutoffSwitch");
            switchGO.transform.position = new Vector3(-5.75f, 1.3f, 1.35f);
            switchGO.transform.rotation = Quaternion.Euler(0,90,0);
            var switchPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            switchPanel.name = "SwitchPanel";
            switchPanel.transform.parent = switchGO.transform;
            switchPanel.transform.localPosition = Vector3.zero;
            switchPanel.transform.localScale = new Vector3(0.7f, 0.9f, 0.12f);
            switchPanel.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;
            var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.name = "Handle";
            handle.transform.parent = switchGO.transform;
            handle.transform.localPosition = new Vector3(0, 0.08f, 0.1f);
            handle.transform.localScale = new Vector3(0.45f, 0.12f, 0.08f);
            handle.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatHazard;
            var switchCol = switchGO.AddComponent<BoxCollider>();
            switchCol.size = new Vector3(1f, 1.2f, 0.6f);
            switchCol.center = new Vector3(0,0,0.15f);
            var scenSwitch = switchGO.AddComponent<ScenarioCutoffSwitch>();
            var scSO = new SerializedObject(scenSwitch);
            scSO.FindProperty("handle").objectReferenceValue = handle.transform;
            scSO.FindProperty("sparkVFX").objectReferenceValue = sparkVFX;
            scSO.ApplyModifiedProperties();

            // Emergency Exit north — REAL hinged door + pathway + safe zone
            var exitGO = new GameObject("EmergencyExit");
            exitGO.transform.position = new Vector3(4.5f, 0, 5.7f);
            // DoorFrame (two jambs + lintel)
            var frameL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frameL.name = "DoorFrame";
            frameL.transform.SetParent(exitGO.transform, false);
            frameL.transform.localPosition = new Vector3(-0.62f, 1.15f, 0f);
            frameL.transform.localScale = new Vector3(0.14f, 2.3f, 0.22f);
            frameL.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;
            var frameR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frameR.name = "DoorFrame_R";
            frameR.transform.SetParent(exitGO.transform, false);
            frameR.transform.localPosition = new Vector3(0.62f, 1.15f, 0f);
            frameR.transform.localScale = new Vector3(0.14f, 2.3f, 0.22f);
            frameR.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;
            var lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lintel.name = "DoorFrame_Top";
            lintel.transform.SetParent(exitGO.transform, false);
            lintel.transform.localPosition = new Vector3(0, 2.32f, 0f);
            lintel.transform.localScale = new Vector3(1.38f, 0.16f, 0.22f);
            lintel.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;
            // Hinge pivot at left edge
            var hingeGO = new GameObject("DoorHinge");
            hingeGO.transform.SetParent(exitGO.transform, false);
            hingeGO.transform.localPosition = new Vector3(-0.51f, 0f, 0f);
            var doorPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorPanel.name = "DoorPanel";
            doorPanel.transform.SetParent(hingeGO.transform, false);
            doorPanel.transform.localPosition = new Vector3(0.51f, 1.09f, 0f);
            doorPanel.transform.localScale = new Vector3(1.02f, 2.08f, 0.07f);
            doorPanel.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatGreen;
            // Handle
            var handle2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle2.name = "DoorHandle";
            handle2.transform.SetParent(doorPanel.transform, false);
            handle2.transform.localPosition = new Vector3(0.32f, 0f, 0.09f);
            handle2.transform.localScale = new Vector3(0.18f, 0.06f, 0.1f);
            handle2.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;
            var exitSign2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            exitSign2.name = "ExitSign";
            exitSign2.transform.parent = exitGO.transform;
            exitSign2.transform.localPosition = new Vector3(0, 2.55f, 0.06f);
            exitSign2.transform.localScale = new Vector3(0.85f, 0.28f, 0.05f);
            exitSign2.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatGreen;
            // Blocker disabled when door opens
            var exitCol = doorPanel.AddComponent<BoxCollider>();
            exitCol.center = Vector3.zero;
            exitCol.size = new Vector3(1f, 1f, 1f);
            // Interactable hinge controller (replaces EvacuationExit click-to-teleport)
            var doorCtrl = hingeGO.AddComponent<DoorController>();
            var doorSO = new SerializedObject(doorCtrl);
            doorSO.FindProperty("hinge").objectReferenceValue = hingeGO.transform;
            doorSO.FindProperty("doorBlocker").objectReferenceValue = exitCol;
            doorSO.ApplyModifiedProperties();
            // EvacuationPathway beyond door (real walkable corridor with destination)
            var pathway = new GameObject("EvacuationPathway");
            pathway.transform.position = new Vector3(4.5f, -0.05f, 7.9f);
            var pathFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pathFloor.name = "PathFloor";
            pathFloor.transform.SetParent(pathway.transform, false);
            pathFloor.transform.localPosition = Vector3.zero;
            pathFloor.transform.localScale = new Vector3(2.6f, 0.1f, 5.0f); // z 5.4..10.4 walkable
            pathFloor.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatFloor;
            // Side corridor walls (leave ends open: room side + safe-area side)
            var corrWallL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            corrWallL.name = "PathWall_L";
            corrWallL.transform.SetParent(pathway.transform, false);
            corrWallL.transform.localPosition = new Vector3(-1.3f, 1.6f, 0f);
            corrWallL.transform.localScale = new Vector3(0.15f, 3.2f, 5.0f);
            corrWallL.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatWall;
            var corrWallR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            corrWallR.name = "PathWall_R";
            corrWallR.transform.SetParent(pathway.transform, false);
            corrWallR.transform.localPosition = new Vector3(1.3f, 1.6f, 0f);
            corrWallR.transform.localScale = new Vector3(0.15f, 3.2f, 5.0f);
            corrWallR.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatWall;
            // Corridor ceiling + emergency lights
            var corrCeil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            corrCeil.name = "PathCeiling";
            corrCeil.transform.SetParent(pathway.transform, false);
            corrCeil.transform.localPosition = new Vector3(0, 3.2f, 0f);
            corrCeil.transform.localScale = new Vector3(2.75f, 0.1f, 5.0f);
            corrCeil.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatCeiling;
            var corrLightObj = new GameObject("PathEmergencyLight", typeof(Light));
            corrLightObj.transform.SetParent(pathway.transform, false);
            corrLightObj.transform.localPosition = new Vector3(0, 2.8f, 0.6f);
            var corrLight = corrLightObj.GetComponent<Light>();
            corrLight.type = LightType.Point; corrLight.color = new Color(0.4f, 1f, 0.55f); corrLight.range = 7f; corrLight.intensity = 1.4f;
            // Exit signs along corridor
            var dirSign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dirSign.name = "PathExitSign";
            dirSign.transform.SetParent(pathway.transform, false);
            dirSign.transform.localPosition = new Vector3(0, 2.4f, -1.6f);
            dirSign.transform.localScale = new Vector3(0.8f, 0.25f, 0.06f);
            dirSign.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatGreen;
            // Safe area assembly point (green pad + beacon) at corridor end
            var safePad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            safePad.name = "SafeArea";
            safePad.transform.SetParent(pathway.transform, false);
            safePad.transform.localPosition = new Vector3(0, 0.08f, 1.9f);
            safePad.transform.localScale = new Vector3(2.2f, 0.06f, 1.2f);
            safePad.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatGreen;
            var safeBeaconObj = new GameObject("SafeBeacon", typeof(Light));
            safeBeaconObj.transform.SetParent(pathway.transform, false);
            safeBeaconObj.transform.localPosition = new Vector3(0, 2.2f, 1.9f);
            var safeBeacon = safeBeaconObj.GetComponent<Light>();
            safeBeacon.type = LightType.Point; safeBeacon.color = new Color(0.3f, 1f, 0.5f); safeBeacon.range = 5f; safeBeacon.intensity = 1.8f;
            // Safe zone trigger at end of pathway (ONLY completion point)
            var safeZoneGO = new GameObject("SafeZoneTrigger");
            safeZoneGO.transform.position = new Vector3(4.5f, 1f, 9.8f);
            var safeCol = safeZoneGO.AddComponent<BoxCollider>();
            safeCol.size = new Vector3(2.0f, 2f, 1.2f);
            safeCol.isTrigger = true;
            safeZoneGO.AddComponent<SafeZoneTrigger>();
            // Wire door pathway
            var doorSO2 = new SerializedObject(doorCtrl);
            doorSO2.FindProperty("pathway").objectReferenceValue = pathway;
            doorSO2.ApplyModifiedProperties();

            // Wire scenario manager
            var scenSO = new SerializedObject(scenario);
            scenSO.FindProperty("hazardController").objectReferenceValue = hazardCtrl;
            scenSO.FindProperty("fireController").objectReferenceValue = fireCtrl;
            scenSO.FindProperty("alarmController").objectReferenceValue = alarmCtrl;
            scenSO.FindProperty("smokeController").objectReferenceValue = smokeCtrl;
            scenSO.FindProperty("evacuationExitCollider").objectReferenceValue = exitGO;
            scenSO.ApplyModifiedProperties();

            // Also wire discharge scenario ref
            var dis2 = new SerializedObject(discharge);
            dis2.FindProperty("scenario").objectReferenceValue = scenario;
            dis2.ApplyModifiedProperties();
            var sw2 = new SerializedObject(scenSwitch);
            sw2.FindProperty("scenario").objectReferenceValue = scenario;
            sw2.ApplyModifiedProperties();
        }

        /// <summary>
        /// Phase 2 — builds one typed extinguisher station (wall mount, color band,
        /// floating type label, pickup interactable wired to shared WeaponHold/HeldExtinguisher).
        /// </summary>
        private static void BuildExtinguisherStation(string name, Vector3 position, ExtinguisherType type,
            Material bodyMaterial, Material bandMaterial, string labelText, Color labelColor)
        {
            var extRoot = SafeVRMainMenuBuilder.CreateExtinguisherProp(name, position);
            extRoot.transform.rotation = Quaternion.Euler(0, -90, 0);
            // Tint body by type (CO2 red, Water blue) so choice is visible at a glance.
            var body = extRoot.transform.Find("Body");
            if (body != null) body.GetComponent<Renderer>().sharedMaterial = bodyMaterial;
            var dome = extRoot.transform.Find("Dome");
            if (dome != null) dome.GetComponent<Renderer>().sharedMaterial = bodyMaterial;
            // Color band on body reinforces the label.
            var band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            band.name = "TypeBand";
            band.transform.SetParent(extRoot.transform, false);
            band.transform.localPosition = new Vector3(0, 0.62f, 0);
            band.transform.localScale = new Vector3(0.26f, 0.1f, 0.26f);
            band.GetComponent<Renderer>().sharedMaterial = bandMaterial;
            UnityEngine.Object.DestroyImmediate(band.GetComponent<Collider>());
            var mount = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mount.name = "WallMount";
            mount.transform.parent = extRoot.transform;
            mount.transform.localPosition = new Vector3(0, 0.3f, -0.18f);
            mount.transform.localScale = new Vector3(0.5f, 0.5f, 0.05f);
            mount.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;
            CreateStationLabel(extRoot, labelText, labelColor);
            var extCol = extRoot.AddComponent<BoxCollider>();
            extCol.center = new Vector3(0, 0.65f, 0);
            extCol.size = new Vector3(1.4f, 1.8f, 1.0f);
            var pickup = extRoot.AddComponent<FireExtinguisherPickup>();
            var pickSO = new SerializedObject(pickup);
            pickSO.FindProperty("pickupVisualRoot").objectReferenceValue = extRoot;
            pickSO.FindProperty("extinguisherType").enumValueIndex = (int)type;
            var wh = FindInSceneIncludingInactive("WeaponHold");
            var heldGO = FindInSceneIncludingInactive("HeldExtinguisher");
            if (wh != null) pickSO.FindProperty("weaponHold").objectReferenceValue = wh.transform;
            if (heldGO != null) pickSO.FindProperty("heldExtinguisher").objectReferenceValue = heldGO;
            else Debug.LogError("[SafeVR] HeldExtinguisher not found including inactive — check BuildPlayer order");
            pickSO.ApplyModifiedProperties();
        }

        /// <summary>Floating world-space type label above a station (procedural TMP, no external assets).</summary>
        private static void CreateStationLabel(GameObject parent, string text, Color color)
        {
            var labelGO = new GameObject("TypeLabel", typeof(TextMeshPro));
            labelGO.transform.SetParent(parent.transform, false);
            labelGO.transform.localPosition = new Vector3(0, 1.55f, 0.1f);
            labelGO.transform.localRotation = Quaternion.identity;
            var tmp = labelGO.GetComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.rectTransform.sizeDelta = new Vector2(3.2f, 0.7f);
        }

        // --------------------------------------------------------------------
        // EVACUATION SCENARIO (Assessment)
        // --------------------------------------------------------------------
        private static void BuildEvacuationScenario()
        {
            var scenarioGO = new GameObject("EvacuationScenarioManager");
            var scenario = scenarioGO.AddComponent<EvacuationScenarioManager>();

            var alarmGO = new GameObject("AlarmBeacon");
            alarmGO.transform.position = new Vector3(0, 3.0f, 0);
            var alarmMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            alarmMesh.name = "AlarmMesh";
            alarmMesh.transform.parent = alarmGO.transform;
            alarmMesh.transform.localPosition = Vector3.zero;
            alarmMesh.transform.localScale = new Vector3(0.5f,0.25f,0.5f);
            alarmMesh.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatAlarm;
            var alarmLightObj = new GameObject("AlarmLight", typeof(Light));
            alarmLightObj.transform.parent = alarmGO.transform;
            alarmLightObj.transform.localPosition = new Vector3(0,-0.2f,0);
            var alarmLight = alarmLightObj.GetComponent<Light>();
            alarmLight.type = LightType.Point; alarmLight.color = new Color(1f,0.15f,0.15f); alarmLight.range = 8f;
            var alarmCtrl = alarmGO.AddComponent<AlarmController>();
            var aSO = new SerializedObject(alarmCtrl);
            aSO.FindProperty("alarmLight").objectReferenceValue = alarmLight;
            aSO.FindProperty("alarmRenderer").objectReferenceValue = alarmMesh.GetComponent<Renderer>();
            aSO.ApplyModifiedProperties();

            var smokeGO = new GameObject("SmokeVolume");
            smokeGO.transform.position = new Vector3(0,1.2f,0);
            var smokeCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            smokeCube.name = "SmokeCube";
            smokeCube.transform.parent = smokeGO.transform;
            smokeCube.transform.localPosition = Vector3.zero;
            smokeCube.transform.localScale = new Vector3(8f,2f,8f);
            smokeCube.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatSmoke;
            Object.DestroyImmediate(smokeCube.GetComponent<Collider>());
            var smokeCtrl = smokeGO.AddComponent<SmokeController>();
            var sSO = new SerializedObject(smokeCtrl);
            sSO.FindProperty("smokeVolume").objectReferenceValue = smokeCube;
            sSO.ApplyModifiedProperties();

            // Blocked primary exit (north)
            var blockedGO = new GameObject("BlockedPrimaryExit");
            blockedGO.transform.position = new Vector3(4.5f, 0.65f, 5.55f);
            var crate1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate1.name = "CrateA";
            crate1.transform.parent = blockedGO.transform;
            crate1.transform.localPosition = new Vector3(0,0,0);
            crate1.transform.localScale = new Vector3(1.2f,1.3f,0.6f);
            crate1.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatHazard;
            var crate2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate2.name = "CrateB";
            crate2.transform.parent = blockedGO.transform;
            crate2.transform.localPosition = new Vector3(0,0.9f,0.15f);
            crate2.transform.localScale = new Vector3(1.0f,0.6f,0.5f);
            crate2.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;
            var blockCol = blockedGO.AddComponent<BoxCollider>();
            blockCol.center = new Vector3(0,0.4f,0);
            blockCol.size = new Vector3(1.4f,1.6f,0.8f);
            // Add interactable to identify block
            var blockInteract = blockedGO.AddComponent<EvacuationExit>(); // reuse as blocked identifier, but isAlternative=false
            // Will be counted as attemptBlocked if interacted

            // Primary Door (north doorway) — real door + hazard activation point.
            // Never opens: PrimaryDoorHazardActivator drives the drill (block + alarm + unlock alternate).
            var primaryDoorGO = new GameObject("PrimaryDoor");
            primaryDoorGO.transform.position = new Vector3(4.5f, 0, 5.7f);
            var pFrameL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pFrameL.name = "DoorFrame";
            pFrameL.transform.SetParent(primaryDoorGO.transform, false);
            pFrameL.transform.localPosition = new Vector3(-0.62f, 1.15f, 0f);
            pFrameL.transform.localScale = new Vector3(0.14f, 2.3f, 0.22f);
            pFrameL.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;
            var pFrameR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pFrameR.name = "DoorFrame_R";
            pFrameR.transform.SetParent(primaryDoorGO.transform, false);
            pFrameR.transform.localPosition = new Vector3(0.62f, 1.15f, 0f);
            pFrameR.transform.localScale = new Vector3(0.14f, 2.3f, 0.22f);
            pFrameR.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;
            var pLintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pLintel.name = "DoorFrame_Top";
            pLintel.transform.SetParent(primaryDoorGO.transform, false);
            pLintel.transform.localPosition = new Vector3(0, 2.32f, 0f);
            pLintel.transform.localScale = new Vector3(1.38f, 0.16f, 0.22f);
            pLintel.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;
            var pHingeGO = new GameObject("DoorHinge");
            pHingeGO.transform.SetParent(primaryDoorGO.transform, false);
            pHingeGO.transform.localPosition = new Vector3(-0.51f, 0f, 0f);
            var pDoorPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pDoorPanel.name = "DoorPanel";
            pDoorPanel.transform.SetParent(pHingeGO.transform, false);
            pDoorPanel.transform.localPosition = new Vector3(0.51f, 1.09f, 0f);
            pDoorPanel.transform.localScale = new Vector3(1.02f, 2.08f, 0.07f);
            pDoorPanel.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;
            var pHandle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pHandle.name = "DoorHandle";
            pHandle.transform.SetParent(pDoorPanel.transform, false);
            pHandle.transform.localPosition = new Vector3(0.32f, 0f, 0.09f);
            pHandle.transform.localScale = new Vector3(0.18f, 0.06f, 0.1f);
            pHandle.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatHazard;
            // Solid blocker: primary never opens, passage only via alternate route.
            var pPanelCol = pDoorPanel.AddComponent<BoxCollider>();
            pPanelCol.center = Vector3.zero;
            pPanelCol.size = new Vector3(1f, 1f, 1f);
            var hazardActivator = pHingeGO.AddComponent<PrimaryDoorHazardActivator>();
            var hzSO = new SerializedObject(hazardActivator);
            hzSO.FindProperty("primaryDoorReference").objectReferenceValue = primaryDoorGO;
            // alternateDoorReference + safeZoneReference + alarmSource wired after those objects exist below.
            hzSO.ApplyModifiedProperties();

            // Open a real 1.4m doorway in Wall_South at x=-4.0 (Assessment only — TrainingRoom file untouched).
            // BuildRoomShell created a solid Wall_South; replace it with split segments + lintel.
            var oldSouth = GameObject.Find("Wall_South");
            if (oldSouth != null) Object.DestroyImmediate(oldSouth);
            float roomW = 12f, roomD = 12f, wallH = 3.2f, wallT = 0.2f;
            float doorCxS = -4.0f, doorWS = 1.4f, doorHS = 2.4f, sz = -roomD / 2f;
            float segL = (doorCxS - doorWS / 2f) - (-roomW / 2f);
            float segR = (roomW / 2f) - (doorCxS + doorWS / 2f);
            var wallSL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallSL.name = "Wall_South_Left";
            wallSL.transform.position = new Vector3(-roomW / 2f + segL / 2f, wallH / 2f, sz);
            wallSL.transform.localScale = new Vector3(segL, wallH, wallT);
            wallSL.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatWall;
            var wallSR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallSR.name = "Wall_South_Right";
            wallSR.transform.position = new Vector3(doorCxS + doorWS / 2f + segR / 2f, wallH / 2f, sz);
            wallSR.transform.localScale = new Vector3(segR, wallH, wallT);
            wallSR.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatWall;
            var lintelS = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lintelS.name = "Wall_South_Lintel";
            lintelS.transform.position = new Vector3(doorCxS, doorHS + (wallH - doorHS) / 2f, sz);
            lintelS.transform.localScale = new Vector3(doorWS, wallH - doorHS, wallT);
            lintelS.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatWall;

            // Alternative exit (south) — SAME hinged door system as TrainingRoom
            var altGO = new GameObject("AlternativeExit");
            altGO.transform.position = new Vector3(-4.0f, 0, -5.7f);
            var frameL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frameL.name = "DoorFrame";
            frameL.transform.SetParent(altGO.transform, false);
            frameL.transform.localPosition = new Vector3(-0.62f, 1.15f, 0f);
            frameL.transform.localScale = new Vector3(0.14f, 2.3f, 0.22f);
            frameL.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;
            var frameR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frameR.name = "DoorFrame_R";
            frameR.transform.SetParent(altGO.transform, false);
            frameR.transform.localPosition = new Vector3(0.62f, 1.15f, 0f);
            frameR.transform.localScale = new Vector3(0.14f, 2.3f, 0.22f);
            frameR.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;
            var lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lintel.name = "DoorFrame_Top";
            lintel.transform.SetParent(altGO.transform, false);
            lintel.transform.localPosition = new Vector3(0, 2.32f, 0f);
            lintel.transform.localScale = new Vector3(1.38f, 0.16f, 0.22f);
            lintel.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;
            var hingeGO = new GameObject("DoorHinge");
            hingeGO.transform.SetParent(altGO.transform, false);
            hingeGO.transform.localPosition = new Vector3(-0.51f, 0f, 0f);
            var doorPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorPanel.name = "DoorPanel";
            doorPanel.transform.SetParent(hingeGO.transform, false);
            doorPanel.transform.localPosition = new Vector3(0.51f, 1.09f, 0f);
            doorPanel.transform.localScale = new Vector3(1.02f, 2.08f, 0.07f);
            doorPanel.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatGreen;
            var doorHandle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorHandle.name = "DoorHandle";
            doorHandle.transform.SetParent(doorPanel.transform, false);
            doorHandle.transform.localPosition = new Vector3(0.32f, 0f, 0.09f);
            doorHandle.transform.localScale = new Vector3(0.18f, 0.06f, 0.1f);
            doorHandle.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatMetal;
            var altSign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            altSign.name = "AltSign";
            altSign.transform.parent = altGO.transform;
            altSign.transform.localPosition = new Vector3(0, 2.55f, 0.06f);
            altSign.transform.localScale = new Vector3(0.85f, 0.28f, 0.05f);
            altSign.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatGreen;
            // Blocker on panel — disabled when door finishes opening (walk-through)
            var altCol = doorPanel.AddComponent<BoxCollider>();
            altCol.center = Vector3.zero;
            altCol.size = new Vector3(1f, 1f, 1f);
            // Hinged controller (same animation as TrainingRoom). Opening marks route found, NEVER completes.
            var doorCtrl = hingeGO.AddComponent<DoorController>();
            var doorSO = new SerializedObject(doorCtrl);
            doorSO.FindProperty("hinge").objectReferenceValue = hingeGO.transform;
            doorSO.FindProperty("doorBlocker").objectReferenceValue = altCol;
            doorSO.ApplyModifiedProperties();

            // Identify block trigger: simple cube near blocked exit to notify hazard identified when looked/interacted?
            // Instead clicking blocked exit counts as hazard identified via EvacuationScenarioManager.NotifyAttemptBlockedExit currently.
            // We will also add a dedicated inspect prompt on blockedGO to call NotifyHazardIdentified when hover? Simpler: use alt approach: make blockedGO also trigger hazard identified on first interact as correct identification, but alt attempt is unsafe. We'll handle via EvacuationExit logic: blocked exit attempt registers unsafe, but we need hazard identified. Add a separate inspect object?
            // Add a small info interactable near blocked exit that marks hazard identified
            var inspectGO = new GameObject("InspectBlockedHazard");
            inspectGO.transform.position = new Vector3(4.5f, 1f, 5.0f);
            var inspectCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            inspectCube.name = "InspectCube";
            inspectCube.transform.parent = inspectGO.transform;
            inspectCube.transform.localPosition = Vector3.zero;
            inspectCube.transform.localScale = new Vector3(0.4f,0.4f,0.04f);
            inspectCube.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatHazard;
            var inspectCol = inspectGO.AddComponent<BoxCollider>();
            inspectCol.size = new Vector3(0.6f,0.6f,0.1f);
            var inspectInteract = inspectGO.AddComponent<BlockedHazardInspect>();
            // This helper will call scenario.NotifyHazardIdentified

            // EvacuationPathway south of doorway (real walkable corridor, mirrors TrainingRoom)
            var pathway = new GameObject("EvacuationPathway");
            pathway.transform.position = new Vector3(-4.0f, -0.05f, -7.9f);
            var pathFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pathFloor.name = "PathFloor";
            pathFloor.transform.SetParent(pathway.transform, false);
            pathFloor.transform.localPosition = Vector3.zero;
            pathFloor.transform.localScale = new Vector3(2.6f, 0.1f, 5.0f); // z -5.4..-10.4 walkable
            pathFloor.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatFloor;
            var corrWallL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            corrWallL.name = "PathWall_L";
            corrWallL.transform.SetParent(pathway.transform, false);
            corrWallL.transform.localPosition = new Vector3(-1.3f, 1.6f, 0f);
            corrWallL.transform.localScale = new Vector3(0.15f, 3.2f, 5.0f);
            corrWallL.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatWall;
            var corrWallR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            corrWallR.name = "PathWall_R";
            corrWallR.transform.SetParent(pathway.transform, false);
            corrWallR.transform.localPosition = new Vector3(1.3f, 1.6f, 0f);
            corrWallR.transform.localScale = new Vector3(0.15f, 3.2f, 5.0f);
            corrWallR.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatWall;
            var corrCeil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            corrCeil.name = "PathCeiling";
            corrCeil.transform.SetParent(pathway.transform, false);
            corrCeil.transform.localPosition = new Vector3(0, 3.2f, 0f);
            corrCeil.transform.localScale = new Vector3(2.75f, 0.1f, 5.0f);
            corrCeil.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatCeiling;
            var corrLightObj = new GameObject("PathEmergencyLight", typeof(Light));
            corrLightObj.transform.SetParent(pathway.transform, false);
            corrLightObj.transform.localPosition = new Vector3(0, 2.8f, -0.6f);
            var corrLight = corrLightObj.GetComponent<Light>();
            corrLight.type = LightType.Point; corrLight.color = new Color(0.4f, 1f, 0.55f); corrLight.range = 7f; corrLight.intensity = 1.4f;
            var dirSign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dirSign.name = "PathExitSign";
            dirSign.transform.SetParent(pathway.transform, false);
            dirSign.transform.localPosition = new Vector3(0, 2.4f, 1.6f);
            dirSign.transform.localScale = new Vector3(0.8f, 0.25f, 0.06f);
            dirSign.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatGreen;
            var safePad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            safePad.name = "SafeArea";
            safePad.transform.SetParent(pathway.transform, false);
            safePad.transform.localPosition = new Vector3(0, 0.08f, -1.9f);
            safePad.transform.localScale = new Vector3(2.2f, 0.06f, 1.2f);
            safePad.GetComponent<Renderer>().sharedMaterial = SafeVRMaterials.MatGreen;
            var safeBeaconObj = new GameObject("SafeBeacon", typeof(Light));
            safeBeaconObj.transform.SetParent(pathway.transform, false);
            safeBeaconObj.transform.localPosition = new Vector3(0, 2.2f, -1.9f);
            var safeBeacon = safeBeaconObj.GetComponent<Light>();
            safeBeacon.type = LightType.Point; safeBeacon.color = new Color(0.3f, 1f, 0.5f); safeBeacon.range = 5f; safeBeacon.intensity = 1.8f;
            // SafeZoneTrigger at corridor end — ONLY completion point (never on door click)
            var safeZoneGO = new GameObject("SafeZoneTrigger");
            safeZoneGO.transform.position = new Vector3(-4.0f, 1f, -9.8f);
            var safeCol = safeZoneGO.AddComponent<BoxCollider>();
            safeCol.size = new Vector3(2.0f, 2f, 1.2f);
            safeCol.isTrigger = true;
            safeZoneGO.AddComponent<SafeZoneTrigger>();
            // Wire door pathway (same field as TrainingRoom)
            var doorSO2 = new SerializedObject(doorCtrl);
            doorSO2.FindProperty("pathway").objectReferenceValue = pathway;
            // South-facing door must swing outward (-z into corridor): mirror TrainingRoom yaw.
            doorSO2.FindProperty("openYaw").floatValue = 95f;
            doorSO2.ApplyModifiedProperties();
            // Wire door evac scenario
            var doorEvacSO = new SerializedObject(doorCtrl);
            doorEvacSO.FindProperty("evacScenario").objectReferenceValue = scenario;
            doorEvacSO.ApplyModifiedProperties();

            // Wire hazard activator references (visible Inspector fields on PrimaryDoor hinge)
            var hzSO2 = new SerializedObject(hazardActivator);
            hzSO2.FindProperty("alarmSource").objectReferenceValue = alarmCtrl;
            hzSO2.FindProperty("alternateDoorReference").objectReferenceValue = altGO;
            hzSO2.FindProperty("safeZoneReference").objectReferenceValue = safeZoneGO;
            hzSO2.ApplyModifiedProperties();

            // Wire scenario
            var scenSO = new SerializedObject(scenario);
            scenSO.FindProperty("alarmController").objectReferenceValue = alarmCtrl;
            scenSO.FindProperty("smokeController").objectReferenceValue = smokeCtrl;
            scenSO.FindProperty("blockedExitVisual").objectReferenceValue = blockedGO;
            scenSO.FindProperty("safeZoneTrigger").objectReferenceValue = safeZoneGO;
            scenSO.ApplyModifiedProperties();

            // Ensure exits know scenario
            var bSO = new SerializedObject(blockInteract);
            bSO.FindProperty("isAlternativeExit").boolValue = false;
            bSO.ApplyModifiedProperties();
        }

        /// <summary>
        /// Phase 4 — generic pause + retry menu (PAUSED: Resume/Retry/MainMenu,
        /// confirm: Restart/Cancel). Tab/R/Esc/Enter handled by ScenarioPauseMenu.
        /// </summary>
        internal static void BuildScenarioPauseMenu()
        {
            var canvas = GameObject.Find("ScenarioCanvas");
            if (canvas == null) canvas = GameObject.Find("AssessmentCanvas");
            if (canvas == null) return;

            var pausePanel = SafeVRUIBuilder.CreatePanel(canvas.transform, "PausePanel", Vector2.zero, new Vector2(480, 430), new Color(0.07f, 0.09f, 0.13f, 0.97f));
            SafeVRUIBuilder.CreateText(pausePanel.transform, "PauseTitle", "PAUSED", 32, FontStyles.Bold, Color.white, new Vector2(0, 150), new Vector2(420, 44), TextAlignmentOptions.Center);
            var btnResume = SafeVRUIBuilder.CreateButton(pausePanel.transform, "Btn_Resume", "RESUME", new Vector2(0, 70), new Vector2(360, 52), new Color(0.08f, 0.65f, 0.38f), Color.white, 18);
            var btnRetry = SafeVRUIBuilder.CreateButton(pausePanel.transform, "Btn_RetryScenario", "RETRY SCENARIO", new Vector2(0, 0), new Vector2(360, 52), new Color(0.12f, 0.45f, 0.75f), Color.white, 18);
            var btnMenu = SafeVRUIBuilder.CreateButton(pausePanel.transform, "Btn_PauseMenu", "MAIN MENU", new Vector2(0, -70), new Vector2(360, 52), new Color(0.25f, 0.28f, 0.32f), Color.white, 18);
            SafeVRUIBuilder.CreateText(pausePanel.transform, "PauseHint", "Tab Resume  •  R Retry  •  Esc Resume", 12, FontStyles.Normal, new Color(0.62f, 0.66f, 0.72f), new Vector2(0, -140), new Vector2(420, 24), TextAlignmentOptions.Center);
            pausePanel.SetActive(false);

            var confirmPanel = SafeVRUIBuilder.CreatePanel(canvas.transform, "RetryConfirmPanel", Vector2.zero, new Vector2(480, 300), new Color(0.10f, 0.08f, 0.08f, 0.98f));
            SafeVRUIBuilder.CreateText(confirmPanel.transform, "ConfirmTitle", "Restart Scenario?", 26, FontStyles.Bold, new Color(0.2f, 0.85f, 0.95f), new Vector2(0, 90), new Vector2(420, 40), TextAlignmentOptions.Center);
            SafeVRUIBuilder.CreateText(confirmPanel.transform, "ConfirmMsg", "Current attempt progress will be lost.\nCompleted history is kept.", 14, FontStyles.Normal, new Color(0.85f, 0.88f, 0.92f), new Vector2(0, 30), new Vector2(420, 50), TextAlignmentOptions.Center);
            var btnConfirm = SafeVRUIBuilder.CreateButton(confirmPanel.transform, "Btn_ConfirmRestart", "RESTART", new Vector2(-95, -60), new Vector2(220, 52), new Color(0.75f, 0.25f, 0.20f), Color.white, 18);
            var btnCancel = SafeVRUIBuilder.CreateButton(confirmPanel.transform, "Btn_CancelRestart", "CANCEL", new Vector2(95, -60), new Vector2(220, 52), new Color(0.25f, 0.28f, 0.32f), Color.white, 18);
            SafeVRUIBuilder.CreateText(confirmPanel.transform, "ConfirmHint", "Enter Confirm  •  Esc Cancel", 12, FontStyles.Normal, new Color(0.62f, 0.66f, 0.72f), new Vector2(0, -115), new Vector2(420, 24), TextAlignmentOptions.Center);
            confirmPanel.SetActive(false);

            var pause = canvas.AddComponent<SafeVR.UI.ScenarioPauseMenu>();
            var pso = new SerializedObject(pause);
            pso.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            pso.FindProperty("resumeButton").objectReferenceValue = btnResume;
            pso.FindProperty("retryButton").objectReferenceValue = btnRetry;
            pso.FindProperty("menuButton").objectReferenceValue = btnMenu;
            pso.FindProperty("confirmPanel").objectReferenceValue = confirmPanel;
            pso.FindProperty("confirmRestartButton").objectReferenceValue = btnConfirm;
            pso.FindProperty("cancelButton").objectReferenceValue = btnCancel;
            pso.ApplyModifiedProperties();
        }

        internal static void BuildScenarioHUD(bool isFireScenario)
        {
            var canvas = SafeVRUIBuilder.CreateCanvas("ScenarioCanvas");

            // Top bar: objective + timer - LARGE readable
            var topPanel = SafeVRUIBuilder.CreatePanel(canvas.transform, "TopHUD", new Vector2(0, 0), new Vector2(1100, 120), new Color(0.08f,0.10f,0.14f,0.96f));
            var rt = topPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0,1); rt.anchorMax = new Vector2(1,1); rt.pivot = new Vector2(0.5f,1);
            rt.anchoredPosition = new Vector2(0,0); rt.sizeDelta = new Vector2(0,120);
            var objective = SafeVRUIBuilder.CreateText(topPanel.transform, "ObjectiveText", isFireScenario? "Explore the room - locate safety equipment" : "Check exits - ensure evacuation route is clear", 28, FontStyles.Bold, Color.white, new Vector2(-80,0), new Vector2(780,100), TextAlignmentOptions.MidlineLeft);
            var timer = SafeVRUIBuilder.CreateText(topPanel.transform, "TimerText", "0.0s", 32, FontStyles.Bold, new Color(0.2f,0.85f,0.95f), new Vector2(470,0), new Vector2(160,50), TextAlignmentOptions.Center);

            // Banner (center top) - very large emergency warning
            var banner = SafeVRUIBuilder.CreatePanel(canvas.transform, "BannerPanel", new Vector2(0, -132), new Vector2(820, 64), new Color(0.62f,0.08f,0.08f,0.98f));
            var bannerRT = banner.GetComponent<RectTransform>();
            bannerRT.anchorMin = new Vector2(0.5f,1); bannerRT.anchorMax = new Vector2(0.5f,1); bannerRT.anchoredPosition = new Vector2(0,-132);
            var bannerText = SafeVRUIBuilder.CreateText(banner.transform, "BannerText", "!!! EMERGENCY !!!", 28, FontStyles.Bold, Color.white, Vector2.zero, new Vector2(800,56), TextAlignmentOptions.Center);
            banner.SetActive(false);

            // Fire intensity bar (only fire scenario) - very readable
            GameObject firePanel = null; Image fireFill = null; TMP_Text fireText = null;
            if (isFireScenario)
            {
                firePanel = SafeVRUIBuilder.CreatePanel(canvas.transform, "FireIntensityPanel", new Vector2(0, -208), new Vector2(520, 42), new Color(0f,0f,0f,0.78f));
                var frt = firePanel.GetComponent<RectTransform>(); frt.anchorMin = new Vector2(0.5f,1); frt.anchorMax = new Vector2(0.5f,1); frt.anchoredPosition = new Vector2(0,-208);
                var bg = SafeVRUIBuilder.CreatePanel(firePanel.transform, "FireFillBG", new Vector2(-32,0), new Vector2(360, 20), new Color(0.22f,0.22f,0.24f,1f));
                var fillObj = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                fillObj.transform.SetParent(bg.transform,false);
                var fr = fillObj.GetComponent<RectTransform>(); fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = Vector2.zero; fr.offsetMax = Vector2.zero;
                fireFill = fillObj.GetComponent<Image>(); fireFill.color = new Color(1f,0.36f,0.08f); fireFill.type = Image.Type.Filled; fireFill.fillMethod = Image.FillMethod.Horizontal;
                fireText = SafeVRUIBuilder.CreateText(firePanel.transform, "FireText", "FIRE 0%", 18, FontStyles.Bold, Color.white, new Vector2(190,0), new Vector2(110,28), TextAlignmentOptions.Center);
                firePanel.SetActive(false);
            }

            // Hold progress (center) - very prominent
            var holdPanel = SafeVRUIBuilder.CreatePanel(canvas.transform, "HoldPanel", new Vector2(0, -88), new Vector2(520, 44), new Color(0f,0f,0f,0.82f));
            var holdRT = holdPanel.GetComponent<RectTransform>(); holdRT.anchorMin = new Vector2(0.5f,0.5f); holdRT.anchorMax = new Vector2(0.5f,0.5f); holdRT.anchoredPosition = new Vector2(0,-88);
            var holdBG = SafeVRUIBuilder.CreatePanel(holdPanel.transform, "HoldBG", new Vector2(0,-8), new Vector2(400, 18), new Color(0.26f,0.26f,0.28f,1f));
            var holdFillObj = new GameObject("HoldFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            holdFillObj.transform.SetParent(holdBG.transform,false);
            var hfr = holdFillObj.GetComponent<RectTransform>(); hfr.anchorMin = Vector2.zero; hfr.anchorMax = Vector2.one; hfr.offsetMin = Vector2.zero; hfr.offsetMax = Vector2.zero;
            var holdFill = holdFillObj.GetComponent<Image>(); holdFill.color = new Color(0.18f,0.82f,0.38f); holdFill.type = Image.Type.Filled; holdFill.fillMethod = Image.FillMethod.Horizontal;
            var holdText = SafeVRUIBuilder.CreateText(holdPanel.transform, "HoldText", "HOLDING...", 18, FontStyles.Bold, Color.white, new Vector2(0,14), new Vector2(440,24), TextAlignmentOptions.Center);
            holdPanel.SetActive(false);

            // Phase 3 — extinguisher charge/pressure indicator (bottom-left, always visible once equipped).
            var chargePanel = SafeVRUIBuilder.CreatePanel(canvas.transform, "ChargePanel", new Vector2(0, 0), new Vector2(360, 44), new Color(0f,0f,0f,0.78f));
            var chargeRT = chargePanel.GetComponent<RectTransform>();
            chargeRT.anchorMin = new Vector2(0f,0f); chargeRT.anchorMax = new Vector2(0f,0f); chargeRT.pivot = new Vector2(0f,0f);
            chargeRT.anchoredPosition = new Vector2(24f, 64f);
            var chargeBG = SafeVRUIBuilder.CreatePanel(chargePanel.transform, "ChargeBG", new Vector2(-40,0), new Vector2(220, 16), new Color(0.26f,0.26f,0.28f,1f));
            var chargeFillObj = new GameObject("ChargeFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            chargeFillObj.transform.SetParent(chargeBG.transform,false);
            var cfr = chargeFillObj.GetComponent<RectTransform>(); cfr.anchorMin = Vector2.zero; cfr.anchorMax = Vector2.one; cfr.offsetMin = Vector2.zero; cfr.offsetMax = Vector2.zero;
            var chargeFill = chargeFillObj.GetComponent<Image>(); chargeFill.color = new Color(0.18f,0.82f,0.38f); chargeFill.type = Image.Type.Filled; chargeFill.fillMethod = Image.FillMethod.Horizontal;
            var chargeText = SafeVRUIBuilder.CreateText(chargePanel.transform, "ChargeText", "CHARGE 100%", 15, FontStyles.Bold, Color.white, new Vector2(100,0), new Vector2(150,24), TextAlignmentOptions.Center);
            chargePanel.SetActive(false);

            // Warning toast (bottom center) - larger
            var warnPanel = SafeVRUIBuilder.CreatePanel(canvas.transform, "WarningPanel", new Vector2(0, 130), new Vector2(760, 52), new Color(0.68f,0.10f,0.10f,0.98f));
            var wr = warnPanel.GetComponent<RectTransform>(); wr.anchorMin = new Vector2(0.5f,0); wr.anchorMax = new Vector2(0.5f,0); wr.anchoredPosition = new Vector2(0,130);
            var warnText = SafeVRUIBuilder.CreateText(warnPanel.transform, "WarningText", "Warning", 18, FontStyles.Bold, Color.white, Vector2.zero, new Vector2(740,44), TextAlignmentOptions.Center);
            warnPanel.SetActive(false);

            // Reticle (reuse) - prompt much larger
            var reticleRoot = new GameObject("ReticleHUD", typeof(RectTransform));
            reticleRoot.transform.SetParent(canvas.transform,false);
            var rrt = reticleRoot.GetComponent<RectTransform>(); rrt.anchorMin = new Vector2(0.5f,0.5f); rrt.anchorMax = new Vector2(0.5f,0.5f); rrt.anchoredPosition = Vector2.zero; rrt.sizeDelta = new Vector2(100,100);
            var dotObj = new GameObject("ReticleDot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dotObj.transform.SetParent(reticleRoot.transform,false);
            var drt = dotObj.GetComponent<RectTransform>(); drt.anchoredPosition = Vector2.zero; drt.sizeDelta = new Vector2(12,12);
            var dotImg = dotObj.GetComponent<Image>(); dotImg.color = new Color(1,1,1,0.78f);
            var promptPanel = SafeVRUIBuilder.CreatePanel(reticleRoot.transform, "PromptPanel", new Vector2(0,-48), new Vector2(620,48), new Color(0,0,0,0.78f));
            var promptTMP = SafeVRUIBuilder.CreateText(promptPanel.transform, "PromptText", "[E] Interact", 22, FontStyles.Bold, Color.white, Vector2.zero, new Vector2(600,42), TextAlignmentOptions.Center);

            var reticleHUD = reticleRoot.AddComponent<ReticleHUD>();
            var retSO = new SerializedObject(reticleHUD);
            retSO.FindProperty("reticleDot").objectReferenceValue = dotImg;
            retSO.FindProperty("promptPanel").objectReferenceValue = promptPanel;
            retSO.FindProperty("promptText").objectReferenceValue = promptTMP;
            retSO.ApplyModifiedProperties();

            // Wire ScenarioHUD
            var hud = canvas.AddComponent<SafeVR.UI.ScenarioHUD>();
            var hudSO = new SerializedObject(hud);
            hudSO.FindProperty("objectiveText").objectReferenceValue = objective;
            hudSO.FindProperty("timerText").objectReferenceValue = timer;
            hudSO.FindProperty("statusBannerText").objectReferenceValue = bannerText;
            hudSO.FindProperty("bannerPanel").objectReferenceValue = banner;
            hudSO.FindProperty("fireIntensityPanel").objectReferenceValue = firePanel;
            hudSO.FindProperty("fireIntensityFill").objectReferenceValue = fireFill;
            hudSO.FindProperty("fireIntensityText").objectReferenceValue = fireText;
            hudSO.FindProperty("holdPanel").objectReferenceValue = holdPanel;
            hudSO.FindProperty("holdFill").objectReferenceValue = holdFill;
            hudSO.FindProperty("holdText").objectReferenceValue = holdText;
            hudSO.FindProperty("chargePanel").objectReferenceValue = chargePanel;
            hudSO.FindProperty("chargeFill").objectReferenceValue = chargeFill;
            hudSO.FindProperty("chargeText").objectReferenceValue = chargeText;
            hudSO.FindProperty("warningPanel").objectReferenceValue = warnPanel;
            hudSO.FindProperty("warningText").objectReferenceValue = warnText;
            hudSO.ApplyModifiedProperties();

            // View-mode label wired to the shared player (same prefab in every scene).
            var viewLabel = SafeVRPlayerBuilder.AddViewModeLabel(canvas.transform);
            SafeVRPlayerBuilder.WireViewLabel(GameObject.Find("Player"), viewLabel);

            // Also wire hold progress via discharge: FireDischarge will call hud.ShowHold
        }

        private static void BuildPerformanceReportUI(bool includeResultsButton = false)
        {
            var canvas = GameObject.Find("ScenarioCanvas");
            if (canvas == null) canvas = GameObject.Find("AssessmentCanvas");
            if (canvas == null) return;
            var reportPanel = SafeVRUIBuilder.CreatePanel(canvas.transform, "PerformanceReportPanel", Vector2.zero, new Vector2(920, 700), new Color(0.07f,0.09f,0.13f,0.96f));
            SafeVRUIBuilder.CreateText(reportPanel.transform, "ReportTitle", "PERFORMANCE REPORT", 24, FontStyles.Bold, new Color(0.2f,0.85f,0.95f), new Vector2(0,312), new Vector2(860,36), TextAlignmentOptions.Center);
            var score = SafeVRUIBuilder.CreateText(reportPanel.transform, "ScoreText", "Score: --", 19, FontStyles.Bold, Color.white, new Vector2(0,272), new Vector2(860,32), TextAlignmentOptions.Center);
            var breakdown = SafeVRUIBuilder.CreateText(reportPanel.transform, "BreakdownText", "Breakdown: --", 12, FontStyles.Normal, new Color(0.75f,0.82f,0.88f), new Vector2(0,244), new Vector2(860,24), TextAlignmentOptions.Center);
            var time = SafeVRUIBuilder.CreateText(reportPanel.transform, "TimeText", "Time: --", 13, FontStyles.Normal, new Color(0.8f,0.85f,0.9f), new Vector2(0,218), new Vector2(860,24), TextAlignmentOptions.Center);
            var objectives = SafeVRUIBuilder.CreateText(reportPanel.transform, "ObjectivesText", "Objectives: --", 12, FontStyles.Normal, Color.white, new Vector2(0,168), new Vector2(860,70), TextAlignmentOptions.TopLeft);
            var detail = SafeVRUIBuilder.CreateText(reportPanel.transform, "DetailText", "Details...", 11, FontStyles.Normal, new Color(0.85f,0.88f,0.92f), new Vector2(0,78), new Vector2(860,100), TextAlignmentOptions.TopLeft);
            var timeline = SafeVRUIBuilder.CreateText(reportPanel.transform, "TimelineText", "Timeline: --", 11, FontStyles.Normal, new Color(0.75f,0.80f,0.85f), new Vector2(0,-2), new Vector2(860,56), TextAlignmentOptions.TopLeft);
            var rec = SafeVRUIBuilder.CreateText(reportPanel.transform, "RecommendText", "Recommendations...", 11, FontStyles.Italic, new Color(0.95f,0.85f,0.45f), new Vector2(0,-70), new Vector2(860,70), TextAlignmentOptions.TopLeft);
            var history = SafeVRUIBuilder.CreateText(reportPanel.transform, "HistoryText", "History: --", 12, FontStyles.Bold, Color.white, new Vector2(0,-128), new Vector2(860,24), TextAlignmentOptions.Center);
            var btnRetry = SafeVRUIBuilder.CreateButton(reportPanel.transform, "Btn_Retry", "RETRY SCENARIO", new Vector2(-160,-270), new Vector2(300,52), new Color(0.12f,0.45f,0.75f), Color.white, 16);
            var btnMenu = SafeVRUIBuilder.CreateButton(reportPanel.transform, "Btn_Menu", "MAIN MENU", new Vector2(160,-270), new Vector2(300,52), new Color(0.25f,0.28f,0.32f), Color.white, 16);
            // Assessment only: success-gated shortcut to the Results scene (TrainingRoom omits this).
            Button btnResults = null;
            if (includeResultsButton)
            {
                btnResults = SafeVRUIBuilder.CreateButton(reportPanel.transform, "Btn_Results", "VIEW RESULTS", new Vector2(0,-195), new Vector2(300,44), new Color(0.08f,0.65f,0.38f), Color.white, 16);
                btnResults.gameObject.SetActive(false);
            }
            var title = reportPanel.transform.Find("ReportTitle")?.GetComponent<TMP_Text>();
            // Actually title is first text; reuse
            reportPanel.SetActive(false);

            var report = canvas.AddComponent<SafeVR.UI.PerformanceReportUI>();
            var rSO = new SerializedObject(report);
            rSO.FindProperty("reportPanel").objectReferenceValue = reportPanel;
            rSO.FindProperty("titleText").objectReferenceValue = title != null ? title : score; // fallback
            // Need to map correctly: titleText is ReportTitle, scoreText is ScoreText etc.
            // Re-find by name
            rSO.FindProperty("titleText").objectReferenceValue = reportPanel.transform.Find("ReportTitle")?.GetComponent<TMP_Text>();
            rSO.FindProperty("scoreText").objectReferenceValue = score;
            rSO.FindProperty("breakdownText").objectReferenceValue = breakdown;
            rSO.FindProperty("timeText").objectReferenceValue = time;
            rSO.FindProperty("objectivesText").objectReferenceValue = objectives;
            rSO.FindProperty("detailText").objectReferenceValue = detail;
            rSO.FindProperty("timelineText").objectReferenceValue = timeline;
            rSO.FindProperty("recommendText").objectReferenceValue = rec;
            rSO.FindProperty("historyText").objectReferenceValue = history;
            rSO.FindProperty("retryButton").objectReferenceValue = btnRetry;
            rSO.FindProperty("menuButton").objectReferenceValue = btnMenu;
            rSO.FindProperty("resultsButton").objectReferenceValue = btnResults;
            rSO.ApplyModifiedProperties();
        }

        internal static void EnsureCoreManagersScenario()
        {
            if (Object.FindFirstObjectByType<SceneFlowManager>() == null) new GameObject("SceneFlowManager", typeof(SceneFlowManager));
            if (Object.FindFirstObjectByType<SoundEffectManager>() == null) new GameObject("SoundEffectManager", typeof(SoundEffectManager));
        }

        internal static GameObject FindInSceneIncludingInactive(string name)
        {
            // GameObject.Find skips inactive, so search all transforms including HeldExtinguisher
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in all)
                if (t.name == name) return t.gameObject;
            return null;
        }

        private static void EnsureSceneDir()
        {
            if (!System.IO.Directory.Exists(SCENE_DIR)) System.IO.Directory.CreateDirectory(SCENE_DIR);
        }
    }

    // Helper for blocked hazard inspect in evacuation
    public class BlockedHazardInspect : MonoBehaviour, SafeVR.Interaction.IInteractable
    {
        public string GetPromptText() => "[E] Inspect Blocked Exit";
        public bool IsInteractable { get => true; set {} }
        public void OnHoverEnter(SafeVR.Interaction.Interactor interactor) {}
        public void OnHoverExit(SafeVR.Interaction.Interactor interactor) {}
        public void OnInteract(SafeVR.Interaction.Interactor interactor)
        {
            var scen = FindFirstObjectByType<EvacuationScenarioManager>();
            scen?.NotifyHazardIdentified();
            Core.SoundEffectManager.Instance?.PlayInteract();
        }
    }
}

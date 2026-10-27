using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SafeVR.Core;
using SafeVR.Training;
using SafeVR.Assessment;

namespace SafeVR.Editor
{
    public static class SafeVRVerify
    {
        [MenuItem("SafeVR/Verify Project")]
        public static void VerifyAll()
        {
            Debug.Log("[SafeVR Verify] Starting verification...");

            string[] requiredScenes = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/TrainingRoom.unity", "Assets/Scenes/Assessment.unity" };
            foreach (var path in requiredScenes)
            {
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                bool exists = sceneAsset != null && System.IO.File.Exists(path);
                Debug.Log($"[Verify] Scene {path} : {(exists ? "OK" : "MISSING")}");
                if (!exists) Debug.LogError($"[Verify] Missing scene {path}");
            }

            // Build Settings
            var buildScenes = EditorBuildSettings.scenes;
            Debug.Log($"[Verify] BuildSettings contains {buildScenes.Length} scenes");
            foreach (var s in buildScenes) Debug.Log($"  - {s.path} enabled={s.enabled}");
            if (buildScenes.Length != 3) Debug.LogError("[Verify] Expected exactly 3 scenes in Build Settings");

            // Check materials
            SafeVRMaterials.EnsureMaterials();
            Debug.Log("[Verify] Materials ensured");

            // Validate scripts compile is implicit if this runs
            Debug.Log("[Verify] Scripts compiled OK");

            // Validate scenes can be opened
            foreach (var path in requiredScenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int rootCount = scene.rootCount;
                Debug.Log($"[Verify] Opened {scene.name} with {rootCount} roots");
                // Check for required managers
                bool hasFlow = Object.FindFirstObjectByType<SceneFlowManager>() != null || Object.FindAnyObjectByType<SceneFlowManager>() != null;
                Debug.Log($"[Verify] {scene.name} has SceneFlowManager: {hasFlow}");
            }

            Debug.Log("[SafeVR Verify] Done.");
        }
    }
}

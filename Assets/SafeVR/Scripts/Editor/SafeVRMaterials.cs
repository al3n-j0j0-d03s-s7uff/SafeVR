using UnityEngine;
using UnityEditor;
using System.IO;

namespace SafeVR.Editor
{
    public static class SafeVRMaterials
    {
        public static Material MatFloor { get; private set; }
        public static Material MatWall { get; private set; }
        public static Material MatCeiling { get; private set; }
        public static Material MatExtinguisher { get; private set; }
        public static Material MatHazard { get; private set; }
        public static Material MatGreen { get; private set; }
        public static Material MatMetal { get; private set; }
        public static Material MatDesk { get; private set; }
        public static Material MatFire { get; private set; }
        public static Material MatSmoke { get; private set; }
        public static Material MatAlarm { get; private set; }
        public static Material MatWater { get; private set; }

        public static void EnsureMaterials()
        {
            if (!Directory.Exists("Assets/SafeVR/Materials"))
            {
                Directory.CreateDirectory("Assets/SafeVR/Materials");
                AssetDatabase.Refresh();
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            MatFloor = GetOrCreate("Mat_Floor", shader, new Color(0.18f, 0.19f, 0.22f), 0.18f, 0.02f);
            MatWall = GetOrCreate("Mat_Wall", shader, new Color(0.92f, 0.93f, 0.95f), 0.08f, 0.0f);
            MatCeiling = GetOrCreate("Mat_Ceiling", shader, new Color(0.88f, 0.89f, 0.91f), 0.06f, 0.0f);
            MatExtinguisher = GetOrCreate("Mat_Extinguisher", shader, new Color(0.92f, 0.14f, 0.14f), 0.68f, 0.02f);
            MatHazard = GetOrCreate("Mat_Hazard", shader, new Color(0.98f, 0.78f, 0.06f), 0.32f, 0.0f);
            MatGreen = GetOrCreate("Mat_Green", shader, new Color(0.09f, 0.72f, 0.42f), 0.45f, 0.02f);
            MatMetal = GetOrCreate("Mat_Metal", shader, new Color(0.62f, 0.64f, 0.67f), 0.55f, 0.72f);
            MatDesk = GetOrCreate("Mat_Desk", shader, new Color(0.52f, 0.44f, 0.36f), 0.35f, 0.02f);
            MatFire = GetOrCreate("Mat_Fire", shader, new Color(1f, 0.38f, 0.06f), 0.0f, 0.0f);
            MatSmoke = GetOrCreate("Mat_Smoke", shader, new Color(0.20f, 0.20f, 0.22f), 0.12f, 0.0f);
            MatAlarm = GetOrCreate("Mat_Alarm", shader, new Color(0.98f, 0.10f, 0.10f), 0.45f, 0.0f);
            MatWater = GetOrCreate("Mat_WaterExtinguisher", shader, new Color(0.15f, 0.42f, 0.95f), 0.55f, 0.05f);

            AssetDatabase.SaveAssets();
        }

        private static Material GetOrCreate(string name, Shader shader, Color color, float smoothness, float metallic)
        {
            string path = $"Assets/SafeVR/Materials/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            // Emissive for fire/alarm/hazard to pop against matte walls
            if (name.Contains("Fire") || name.Contains("Alarm"))
            {
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", color * 1.2f);
                }
            }
            if (name.Contains("Hazard") && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 0.45f);
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}

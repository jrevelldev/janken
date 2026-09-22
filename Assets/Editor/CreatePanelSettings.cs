using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace Janken.Editor
{
    [InitializeOnLoad]
    public static class CreatePanelSettings
    {
        static CreatePanelSettings()
        {
            EditorApplication.delayCall += EnsurePanelSettingsExist;
        }

        [MenuItem("Janken/Generar Panel Settings (UI Toolkit)")]
        public static void EnsurePanelSettingsExist()
        {
            string path = "Assets/UI/TournamentPanelSettings.asset";
            
            PanelSettings existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (existing != null)
            {
                Debug.Log($"[Janken] PanelSettings ja existeix a {path}");
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/UI"))
            {
                AssetDatabase.CreateFolder("Assets", "UI");
            }

            PanelSettings settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.match = 0.5f;

            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Janken] Creat automàticament PanelSettings a {path}");
        }
    }
}

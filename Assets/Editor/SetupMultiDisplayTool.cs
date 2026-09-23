using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;
using Janken.Controllers;

namespace Janken.Editor
{
    public static class SetupMultiDisplayTool
    {
        [MenuItem("Janken/⚡ Configurar Escena MultiDisplay Automàticament")]
        public static void SetupScene()
        {
            // 1. Ensure PanelSettings exist for Display 1 and Display 2
            PanelSettings panelSettingsD1 = GetOrCreatePanelSettings("Assets/UI/TournamentPanelSettings.asset", 0);
            PanelSettings panelSettingsD2 = GetOrCreatePanelSettings("Assets/UI/Display2PanelSettings.asset", 1);

            // 2. Load UXML Visual Trees
            VisualTreeAsset uxmlD1 = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/TournamentManager.uxml");
            VisualTreeAsset uxmlD2 = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Display2Manager.uxml");

            if (uxmlD1 == null || uxmlD2 == null)
            {
                EditorUtility.DisplayDialog("Error", "No s'han trobat els fitxers UXML a Assets/UI/", "D'acord");
                return;
            }

            // 3. Setup Display 1 (Control)
            GameObject goD1 = GameObject.Find("Display1_Control");
            if (goD1 == null)
            {
                // Also check legacy name "TournamentUI"
                goD1 = GameObject.Find("TournamentUI");
            }

            if (goD1 == null)
            {
                goD1 = new GameObject("Display1_Control");
                Undo.RegisterCreatedObjectUndo(goD1, "Create Display1_Control");
            }
            else
            {
                goD1.name = "Display1_Control";
            }

            UIDocument uiDoc1 = goD1.GetComponent<UIDocument>();
            if (uiDoc1 == null) uiDoc1 = Undo.AddComponent<UIDocument>(goD1);
            uiDoc1.panelSettings = panelSettingsD1;
            uiDoc1.visualTreeAsset = uxmlD1;

            TournamentUIController controller1 = goD1.GetComponent<TournamentUIController>();
            if (controller1 == null) controller1 = Undo.AddComponent<TournamentUIController>(goD1);

            // Setup AudioSource Component on Display1_Control
            AudioSource audioSrc = goD1.GetComponent<AudioSource>();
            if (audioSrc == null) audioSrc = Undo.AddComponent<AudioSource>(goD1);
            audioSrc.loop = true;
            audioSrc.playOnAwake = false;
            audioSrc.volume = 0f;

            // Auto-assign Audio/Video Clip from Assets/Audio
            SerializedObject serController = new SerializedObject(controller1);
            SerializedProperty audioSourceProp = serController.FindProperty("audioSource");
            SerializedProperty videoClipProp = serController.FindProperty("backgroundVideoClip");
            SerializedProperty audioClipProp = serController.FindProperty("backgroundMusicClip");

            if (audioSourceProp != null)
            {
                audioSourceProp.objectReferenceValue = audioSrc;
            }

            string[] guids = AssetDatabase.FindAssets("", new[] { "Assets/Audio" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Object assetObj = AssetDatabase.LoadMainAssetAtPath(path);

                if (assetObj is AudioClip aClip)
                {
                    // Optimize audio loading mode to Streaming so 43MB file plays INSTANTLY without memory decompress lag!
                    AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                    if (importer != null)
                    {
                        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                        if (settings.loadType != AudioClipLoadType.Streaming)
                        {
                            settings.loadType = AudioClipLoadType.Streaming;
                            importer.defaultSampleSettings = settings;
                            importer.SaveAndReimport();
                            aClip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                        }
                    }

                    audioSrc.clip = aClip;
                    if (audioClipProp != null)
                    {
                        audioClipProp.objectReferenceValue = aClip;
                    }
                    break;
                }
                else if (assetObj is UnityEngine.Video.VideoClip vClip)
                {
                    if (videoClipProp != null)
                    {
                        videoClipProp.objectReferenceValue = vClip;
                    }
                    break;
                }
            }

            serController.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller1);
            EditorUtility.SetDirty(audioSrc);

            // 4. Setup Display 2 (Audience)
            GameObject goD2 = GameObject.Find("Display2_Audience");
            if (goD2 == null)
            {
                goD2 = new GameObject("Display2_Audience");
                Undo.RegisterCreatedObjectUndo(goD2, "Create Display2_Audience");
            }

            UIDocument uiDoc2 = goD2.GetComponent<UIDocument>();
            if (uiDoc2 == null) uiDoc2 = Undo.AddComponent<UIDocument>(goD2);
            uiDoc2.panelSettings = panelSettingsD2;
            uiDoc2.visualTreeAsset = uxmlD2;

            Display2Controller controller2 = goD2.GetComponent<Display2Controller>();
            if (controller2 == null) controller2 = Undo.AddComponent<Display2Controller>(goD2);

            // 5. Setup Camera for Display 2 (Eliminates "Display 2 No cameras rendering" overlay text)
            GameObject camObj2 = GameObject.Find("Display2_Camera");
            if (camObj2 == null)
            {
                camObj2 = new GameObject("Display2_Camera");
                Undo.RegisterCreatedObjectUndo(camObj2, "Create Display2_Camera");
            }

            Camera cam2 = camObj2.GetComponent<Camera>();
            if (cam2 == null) cam2 = Undo.AddComponent<Camera>(camObj2);

            cam2.targetDisplay = 1; // Display 2
            cam2.clearFlags = CameraClearFlags.SolidColor;
            cam2.backgroundColor = new Color(0.035f, 0.05f, 0.086f); // Dark background #090d16
            cam2.cullingMask = 0; // Pure UI buffer clearance

            // 6. Mark Scene Dirty so changes are saved
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog(
                "¡MultiDisplay Configurat amb Èxit!",
                "S'han creat i configurat automàticament els dos displays a la jerarquia de l'escena:\n\n" +
                "1. Display1_Control (Target Display 1 / Main)\n" +
                "2. Display2_Audience (Target Display 2 / Stage)\n\n" +
                "Prem 'PLAY' a Unity per començar a provar-ho!",
                "Genial!"
            );

            Debug.Log("[Janken] Escena MultiDisplay configurada automàticament amb èxit.");
        }

        private static PanelSettings GetOrCreatePanelSettings(string path, int targetDisplayIndex)
        {
            PanelSettings settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (settings == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/UI"))
                {
                    AssetDatabase.CreateFolder("Assets", "UI");
                }

                settings = ScriptableObject.CreateInstance<PanelSettings>();
                settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                settings.referenceResolution = new Vector2Int(1920, 1080);
                settings.match = 0.5f;

                // Set target display property if accessible via SerializedObject
                SerializedObject serializedObj = new SerializedObject(settings);
                SerializedProperty targetDisplayProp = serializedObj.FindProperty("m_TargetDisplay");
                if (targetDisplayProp != null)
                {
                    targetDisplayProp.intValue = targetDisplayIndex;
                    serializedObj.ApplyModifiedProperties();
                }

                AssetDatabase.CreateAsset(settings, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else
            {
                SerializedObject serializedObj = new SerializedObject(settings);
                SerializedProperty targetDisplayProp = serializedObj.FindProperty("m_TargetDisplay");
                if (targetDisplayProp != null && targetDisplayProp.intValue != targetDisplayIndex)
                {
                    targetDisplayProp.intValue = targetDisplayIndex;
                    serializedObj.ApplyModifiedProperties();
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                }
            }

            return settings;
        }
    }
}

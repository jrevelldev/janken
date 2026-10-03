using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;
using Janken.Controllers;
using Janken.VFX;

namespace Janken.Editor
{
    public static class SetupMultiDisplayTool
    {
        [MenuItem("Janken/⚡ Configurar Escena MultiDisplay Automàticament")]
        public static void SetupScene()
        {
            // 1. Ensure PanelSettings exist for Display 1, Display 2 (Horizontal), and Display 3 (Vertical)
            PanelSettings panelSettingsD1 = GetOrCreatePanelSettings("Assets/UI/TournamentPanelSettings.asset", 0, new Vector2Int(1920, 1080));
            PanelSettings panelSettingsD2 = GetOrCreatePanelSettings("Assets/UI/Display2PanelSettings.asset", 1, new Vector2Int(1920, 1080));
            PanelSettings panelSettingsD3 = GetOrCreatePanelSettings("Assets/UI/Display3PanelSettings.asset", 2, new Vector2Int(1080, 1920));

            // 2. Load UXML Visual Trees
            VisualTreeAsset uxmlD1 = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/TournamentManager.uxml");
            VisualTreeAsset uxmlD2 = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Display2Manager.uxml");
            VisualTreeAsset uxmlD3 = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Display3Manager.uxml");

            if (uxmlD1 == null || uxmlD2 == null || uxmlD3 == null)
            {
                EditorUtility.DisplayDialog("Error", "No s'han trobat els fitxers UXML a Assets/UI/", "D'acord");
                return;
            }

            // 3. Setup Display 1 (Control)
            GameObject goD1 = GameObject.Find("Display1_Control");
            if (goD1 == null)
            {
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

            // 4. Setup Display 2 (Audience Horizontal)
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

            // Auto-process & Assign Stingers for Display 2
            SetupStingersForController(controller2);

            // 5. Setup Display 3 (Audience Vertical)
            GameObject goD3 = GameObject.Find("Display3_Audience");
            if (goD3 == null)
            {
                goD3 = new GameObject("Display3_Audience");
                Undo.RegisterCreatedObjectUndo(goD3, "Create Display3_Audience");
            }

            UIDocument uiDoc3 = goD3.GetComponent<UIDocument>();
            if (uiDoc3 == null) uiDoc3 = Undo.AddComponent<UIDocument>(goD3);
            uiDoc3.panelSettings = panelSettingsD3;
            uiDoc3.visualTreeAsset = uxmlD3;

            Display3Controller controller3 = goD3.GetComponent<Display3Controller>();
            if (controller3 == null) controller3 = Undo.AddComponent<Display3Controller>(goD3);

            // Auto-process & Assign Stingers for Display 3
            SetupStingersForController(controller3);

            // 6. Setup Camera for Display 2
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
            cam2.backgroundColor = new Color(0.035f, 0.05f, 0.086f);
            cam2.cullingMask = 0;

            // 7. Setup Camera for Display 3
            GameObject camObj3 = GameObject.Find("Display3_Camera");
            if (camObj3 == null)
            {
                camObj3 = new GameObject("Display3_Camera");
                Undo.RegisterCreatedObjectUndo(camObj3, "Create Display3_Camera");
            }

            Camera cam3 = camObj3.GetComponent<Camera>();
            if (cam3 == null) cam3 = Undo.AddComponent<Camera>(camObj3);
            cam3.targetDisplay = 2; // Display 3
            cam3.clearFlags = CameraClearFlags.SolidColor;
            cam3.backgroundColor = new Color(0.035f, 0.05f, 0.086f);
            cam3.cullingMask = 0;

            // Wire display2Controller, display3Controller, display2Camera, and display3Camera on TournamentUIController
            SerializedProperty d2Prop = serController.FindProperty("display2Controller");
            SerializedProperty d3Prop = serController.FindProperty("display3Controller");
            SerializedProperty cam2Prop = serController.FindProperty("display2Camera");
            SerializedProperty cam3Prop = serController.FindProperty("display3Camera");

            if (d2Prop != null) d2Prop.objectReferenceValue = controller2;
            if (d3Prop != null) d3Prop.objectReferenceValue = controller3;
            if (cam2Prop != null) cam2Prop.objectReferenceValue = cam2;
            if (cam3Prop != null) cam3Prop.objectReferenceValue = cam3;

            serController.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller1);

            // 8. Mark Scene Dirty so changes are saved
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog(
                "¡MultiDisplay 3 Pantalles Configurat amb Èxit!",
                "S'han creat i configurat automàticament els tres displays a la jerarquia de l'escena:\n\n" +
                "1. Display1_Control (Target Display 1 / Control Main)\n" +
                "2. Display2_Audience (Target Display 2 / Escenari Horitzontal 1920x1080)\n" +
                "3. Display3_Audience (Target Display 3 / Escenari Vertical 1080x1920)\n\n" +
                "Pots utilitzar el botó de la barra de control '🔁 Monitors' per intercanviar els displays 2 i 3 quan connectis les pantalles físiques.",
                "Genial!"
            );

            Debug.Log("[Janken] Escena MultiDisplay (3 displays: Control, Horitzontal, Vertical) configurada automàticament amb èxit.");
        }

        private static void SetupStingersForController(Display2Controller controller)
        {
            if (controller == null) return;

            string vfxPath = "Assets/Sprites/VFX";
            if (!System.IO.Directory.Exists(vfxPath)) return;

            List<StingerAnimationData> stingerList = new List<StingerAnimationData>();
            string[] subDirs = System.IO.Directory.GetDirectories(vfxPath);
            foreach (string dir in subDirs)
            {
                string folderName = System.IO.Path.GetFileName(dir);
                if (folderName.StartsWith(".")) continue;

                StingerAnimationData sData = StingerImporterTool.ProcessStingerFolder(dir, folderName);
                if (sData != null)
                {
                    stingerList.Add(sData);
                }
            }

            if (stingerList.Count > 0)
            {
                SerializedObject serController = new SerializedObject(controller);
                SerializedProperty activeStingerProp = serController.FindProperty("activeStinger");
                SerializedProperty stingerLibProp = serController.FindProperty("stingerLibrary");

                if (activeStingerProp != null)
                {
                    activeStingerProp.objectReferenceValue = stingerList[0];
                }

                if (stingerLibProp != null)
                {
                    stingerLibProp.ClearArray();
                    for (int i = 0; i < stingerList.Count; i++)
                    {
                        stingerLibProp.InsertArrayElementAtIndex(i);
                        stingerLibProp.GetArrayElementAtIndex(i).objectReferenceValue = stingerList[i];
                    }
                }

                serController.ApplyModifiedProperties();
                EditorUtility.SetDirty(controller);
            }
        }

        private static PanelSettings GetOrCreatePanelSettings(string path, int targetDisplayIndex, Vector2Int referenceResolution)
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
                settings.referenceResolution = referenceResolution;
                settings.match = 0.5f;

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
                SerializedProperty resProp = serializedObj.FindProperty("m_ReferenceResolution");

                bool changed = false;
                if (targetDisplayProp != null && targetDisplayProp.intValue != targetDisplayIndex)
                {
                    targetDisplayProp.intValue = targetDisplayIndex;
                    changed = true;
                }
                if (resProp != null && resProp.vector2IntValue != referenceResolution)
                {
                    resProp.vector2IntValue = referenceResolution;
                    changed = true;
                }

                if (changed)
                {
                    serializedObj.ApplyModifiedProperties();
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                }
            }

            return settings;
        }
    }
}

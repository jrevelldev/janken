using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Janken.VFX;

namespace Janken.Editor
{
    public class StingerImporterTool : EditorWindow
    {
        private Vector2 scrollPos;
        private string vfxFolderPath = "Assets/Sprites/VFX";
        private List<StingerFolderInfo> detectedFolders = new List<StingerFolderInfo>();

        public class StingerFolderInfo
        {
            public string folderPath;
            public string folderName;
            public int pngCount;
            public StingerAnimationData existingAsset;
        }

        [MenuItem("Janken/🎬 Stinger Manager Tool")]
        public static void OpenWindow()
        {
            StingerImporterTool window = GetWindow<StingerImporterTool>("Stinger Manager");
            window.minSize = new Vector2(450, 500);
            window.ScanFolders();
            window.Show();
        }

        [MenuItem("Janken/⚡ Process & Build All Stingers Automàticament")]
        public static void ProcessAllStingersAuto()
        {
            string vfxPath = "Assets/Sprites/VFX";
            if (!Directory.Exists(vfxPath))
            {
                EditorUtility.DisplayDialog("Error", $"La carpeta {vfxPath} no existeix!", "OK");
                return;
            }

            string[] subDirs = Directory.GetDirectories(vfxPath);
            int count = 0;

            foreach (string dir in subDirs)
            {
                string folderName = Path.GetFileName(dir);
                if (folderName.StartsWith(".") || folderName.Equals("StingerData")) continue;

                ProcessStingerFolder(dir, folderName);
                count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Stingers Processats",
                $"S'han processat i generat {count} Stinger Assets a {vfxPath}!",
                "Genial!"
            );
        }

        private void OnEnable()
        {
            ScanFolders();
        }

        private void ScanFolders()
        {
            detectedFolders.Clear();
            if (!Directory.Exists(vfxFolderPath)) return;

            string[] subDirs = Directory.GetDirectories(vfxFolderPath);
            foreach (string dir in subDirs)
            {
                string folderName = Path.GetFileName(dir);
                if (folderName.StartsWith(".")) continue;

                string[] pngs = Directory.GetFiles(dir, "*.png");
                string assetPath = Path.Combine(dir, $"{folderName}_Data.asset").Replace("\\", "/");
                StingerAnimationData asset = AssetDatabase.LoadAssetAtPath<StingerAnimationData>(assetPath);

                detectedFolders.Add(new StingerFolderInfo
                {
                    folderPath = dir.Replace("\\", "/"),
                    folderName = folderName,
                    pngCount = pngs.Length,
                    existingAsset = asset
                });
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("🎬 Janken 2D Stinger Importer & Manager", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Aquesta eina cerca carpetes amb seqüències de PNGs a 'Assets/Sprites/VFX/', " +
                "configura les imatges com a Sprites 2D optimitzats i genera els fitxers d'animació Stinger (ScriptableObject) per al Display 2.",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            if (GUILayout.Button("🔄 Rescan Carpetes", GUILayout.Height(30)))
            {
                ScanFolders();
            }

            if (GUILayout.Button("⚡ Processar TOTS els Stingers Automàticament", GUILayout.Height(35)))
            {
                ProcessAllStingersAuto();
                ScanFolders();
            }

            EditorGUILayout.Space(15);
            GUILayout.Label($"Carpetes Detectades a {vfxFolderPath}:", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            if (detectedFolders.Count == 0)
            {
                EditorGUILayout.HelpBox("No s'ha trobat cap subcarpeta amb PNGs a Assets/Sprites/VFX/", MessageType.Warning);
            }
            else
            {
                foreach (var folder in detectedFolders)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"📁 {folder.folderName}", EditorStyles.boldLabel);
                    GUILayout.Label($"({folder.pngCount} PNGs trobats)", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.LabelField("Ruta:", folder.folderPath);

                    if (folder.existingAsset != null)
                    {
                        EditorGUILayout.ObjectField("Asset Generat:", folder.existingAsset, typeof(StingerAnimationData), false);
                        
                        EditorGUI.BeginChangeCheck();
                        int newCutFrame = EditorGUILayout.IntSlider("Frame de Tall (Cut Frame):", folder.existingAsset.cutFrameIndex, 0, Mathf.Max(0, folder.existingAsset.FrameCount - 1));
                        float newFps = EditorGUILayout.FloatField("Velocitat (FPS):", folder.existingAsset.fps);
                        AudioClip newSound = (AudioClip)EditorGUILayout.ObjectField("So d'Stinger (Opcional):", folder.existingAsset.stingerSound, typeof(AudioClip), false);

                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(folder.existingAsset, "Modify Stinger Settings");
                            folder.existingAsset.cutFrameIndex = newCutFrame;
                            folder.existingAsset.fps = newFps;
                            folder.existingAsset.stingerSound = newSound;
                            EditorUtility.SetDirty(folder.existingAsset);
                            AssetDatabase.SaveAssets();
                        }

                        EditorGUILayout.LabelField("Durada Total:", $"{folder.existingAsset.FrameCount} frames | {folder.existingAsset.Duration:F2} segons");
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Encara no s'ha creat l'Asset per aquesta seqüència.", MessageType.Warning);
                    }

                    EditorGUILayout.Space(5);
                    if (GUILayout.Button($"🚀 Re-configurar i Actualitzar Sprites de '{folder.folderName}'", GUILayout.Height(25)))
                    {
                        ProcessStingerFolder(folder.folderPath, folder.folderName);
                        ScanFolders();
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(10);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        public static StingerAnimationData ProcessStingerFolder(string folderPath, string folderName)
        {
            string[] pngFiles = Directory.GetFiles(folderPath, "*.png", SearchOption.TopDirectoryOnly);
            if (pngFiles.Length == 0)
            {
                Debug.LogWarning($"[StingerImporter] No s'han trobat PNGs a: {folderPath}");
                return null;
            }

            // 1. Configure TextureImporter for all PNGs
            bool reimportNeeded = false;
            foreach (string filePath in pngFiles)
            {
                string relativePath = filePath.Replace("\\", "/");
                if (relativePath.StartsWith(Application.dataPath))
                {
                    relativePath = "Assets" + relativePath.Substring(Application.dataPath.Length);
                }

                TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                if (importer != null)
                {
                    bool changed = false;

                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        changed = true;
                    }
                    if (importer.spriteImportMode != SpriteImportMode.Single)
                    {
                        importer.spriteImportMode = SpriteImportMode.Single;
                        changed = true;
                    }
                    if (importer.mipmapEnabled)
                    {
                        importer.mipmapEnabled = false;
                        changed = true;
                    }

                    // Disable physics shape generation for UI stinger performance
                    TextureImporterSettings settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    if (settings.spriteGenerateFallbackPhysicsShape)
                    {
                        settings.spriteGenerateFallbackPhysicsShape = false;
                        importer.SetTextureSettings(settings);
                        changed = true;
                    }

                    if (changed)
                    {
                        importer.SaveAndReimport();
                        reimportNeeded = true;
                    }
                }
            }

            if (reimportNeeded)
            {
                AssetDatabase.Refresh();
            }

            // 2. Load and natural sort Sprite assets
            List<Sprite> sprites = new List<Sprite>();
            foreach (string filePath in pngFiles)
            {
                string relativePath = filePath.Replace("\\", "/");
                if (relativePath.StartsWith(Application.dataPath))
                {
                    relativePath = "Assets" + relativePath.Substring(Application.dataPath.Length);
                }

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
                if (sprite != null)
                {
                    sprites.Add(sprite);
                }
            }

            // Natural sort by numeric suffix in filename
            sprites = sprites.OrderBy(s => ExtractNumber(s.name)).ThenBy(s => s.name).ToList();

            // 3. Create or Update StingerAnimationData ScriptableObject
            string assetPath = Path.Combine(folderPath, $"{folderName}_Data.asset").Replace("\\", "/");
            StingerAnimationData stingerData = AssetDatabase.LoadAssetAtPath<StingerAnimationData>(assetPath);

            if (stingerData == null)
            {
                stingerData = ScriptableObject.CreateInstance<StingerAnimationData>();
                stingerData.stingerName = folderName;
                stingerData.fps = 30f;
                // Default cut frame to middle of sequence if new
                int initialCut = folderName.ToLower().Contains("stinger_01") ? 13 : sprites.Count / 2;
                stingerData.cutFrameIndex = Mathf.Clamp(initialCut, 0, Mathf.Max(0, sprites.Count - 1));
                stingerData.frames = sprites.ToArray();

                AssetDatabase.CreateAsset(stingerData, assetPath);
            }
            else
            {
                stingerData.stingerName = folderName;
                stingerData.frames = sprites.ToArray();
                // Preserve user's custom cutFrameIndex, clamping to valid sprite bounds
                stingerData.cutFrameIndex = Mathf.Clamp(stingerData.cutFrameIndex, 0, Mathf.Max(0, sprites.Count - 1));
                EditorUtility.SetDirty(stingerData);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[StingerImporter] ✅ Stinger '{folderName}' actualitzat amb èxit amb {sprites.Count} frames (Frame de Tall: #{stingerData.cutFrameIndex})!");

            return stingerData;
        }

        private static int ExtractNumber(string text)
        {
            Match match = Regex.Match(text, @"\d+");
            if (match.Success && int.TryParse(match.Value, out int result))
            {
                return result;
            }
            return 0;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;
using Janken.Tournament;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Janken.Controllers
{
    [RequireComponent(typeof(UIDocument))]
    public class TournamentUIController : MonoBehaviour
    {
        [Header("MultiDisplay References")]
        [SerializeField] private Display2Controller display2Controller;
        [SerializeField] private Display3Controller display3Controller;
        [SerializeField] private Camera display2Camera;
        [SerializeField] private Camera display3Camera;

        [Header("Audio Settings")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip backgroundMusicClip;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private VideoClip backgroundVideoClip;
        [SerializeField] private float fadeDuration = 1.5f;

        [Header("Nou Àrbitre Video Settings")]
        [SerializeField] private VideoClip nouArbitreVideoClip;
        [SerializeField] private float goVideoStartTime = 0f;
        [SerializeField] private float repVideoStartTime = 0f;

        private Button btnGo;
        private Button btnRep;
        private Button btnSwapDisplays;
        private bool isDisplaySwapped = false;
        private const string PREF_SWAP_DISPLAYS = "Janken_SwapDisplays";

        private UIDocument uiDocument;
        private VisualElement rootVisualElement;

        private ScrollView playerScrollView;
        private VisualElement bracketContainer;
        private TextField playerNameInput;
        private Label tournamentTitleLabel;
        private VisualElement sidebarPanel;

        private Button btnAddPlayer;
        private Button btnPreset4;
        private Button btnPreset8;
        private Button btnPreset16;
        private Button btnShuffle;
        private Button btnResetDefaults;
        private Button btnGenerateBracket;
        private Button btnToggleSidebar;

        // Display 2 Controls (Standard & Chroma)
        private Button btnViewBracket;
        private Button btnViewRound;
        private Button btnViewCombat;
        private Button btnViewChampion;
        private Button btnChromaCleanFeed;
        private Button btnChromaBracket;
        private Button btnChromaRound;
        private Button btnChromaCombatSF;
        private Button btnChromaChampion;
        private VisualElement roundSelectorContainer;
        private Label display2StatusText;

        // Music & Stinger Control
        private Button btnToggleMusic;
        private Toggle toggleStinger;
        private Coroutine audioFadeCoroutine;
        private bool isMusicPlaying = false;
        private bool useStingerTransitions = true;

        // Quit Confirmation Modal & Close Button
        private Button btnCloseApp;
        private VisualElement confirmQuitModal;
        private Button btnConfirmQuitYes;
        private Button btnConfirmQuitNo;

        private TournamentModel tournamentModel;
        private bool hasPlayedGoInCurrentVideoSession = false;

        private void Start()
        {
            InitializeAudio();
            InitializeUI();
        }

        private void Update()
        {
            HandleHotkeys();
        }

        private void InitializeAudio()
        {
#if UNITY_EDITOR
            if (backgroundVideoClip == null && backgroundMusicClip == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("", new[] { "Assets/Audio" });
                foreach (string guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var aClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (aClip != null)
                    {
                        backgroundMusicClip = aClip;
                        break;
                    }
                    var vClip = UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>(path);
                    if (vClip != null)
                    {
                        backgroundVideoClip = vClip;
                        break;
                    }
                }
            }

            if (nouArbitreVideoClip == null)
            {
                string[] videoGuids = UnityEditor.AssetDatabase.FindAssets("", new[] { "Assets/Videos" });
                foreach (string guid in videoGuids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var vClip = UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>(path);
                    if (vClip != null)
                    {
                        nouArbitreVideoClip = vClip;
                        break;
                    }
                }
            }
#endif

            // Setup VideoPlayer for .mp4 audio clips
            if (backgroundVideoClip != null || videoPlayer != null)
            {
                if (videoPlayer == null)
                {
                    videoPlayer = GetComponent<VideoPlayer>();
                    if (videoPlayer == null)
                    {
                        videoPlayer = gameObject.AddComponent<VideoPlayer>();
                    }
                }

                if (backgroundVideoClip != null)
                {
                    videoPlayer.clip = backgroundVideoClip;
                }

                videoPlayer.playOnAwake = false;
                videoPlayer.isLooping = true;
                videoPlayer.renderMode = VideoRenderMode.APIOnly;
                videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
                videoPlayer.SetDirectAudioVolume(0, 0f);
            }

            // Setup AudioSource for standard audio clips
            if (backgroundMusicClip != null || audioSource != null)
            {
                if (audioSource == null)
                {
                    audioSource = GetComponent<AudioSource>();
                    if (audioSource == null)
                    {
                        audioSource = gameObject.AddComponent<AudioSource>();
                    }
                }

                audioSource.loop = true;
                audioSource.playOnAwake = false;
                audioSource.volume = 0f;

                if (backgroundMusicClip != null)
                {
                    audioSource.clip = backgroundMusicClip;
                }
            }
        }

        public void InitializeUI()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;
            uiDocument.sortingOrder = 100;

            rootVisualElement = uiDocument.rootVisualElement;
            if (rootVisualElement == null) return;

            tournamentModel = new TournamentModel();

            EnsureAllDisplayReferences();

            if (display2Controller != null)
            {
                display2Controller.BindModel(tournamentModel);
            }

            if (display3Controller != null)
            {
                display3Controller.BindModel(tournamentModel);
            }

            isDisplaySwapped = PlayerPrefs.GetInt(PREF_SWAP_DISPLAYS, 0) == 1;

            BindUIElements();
            RegisterEvents();
            ApplyDisplayTargetMapping();
            HideQuitConfirmationModal();

            tournamentModel.OnDisplayViewChanged += OnDisplayViewChanged;
            tournamentModel.OnSelectedMatchChanged += OnSelectedMatchChanged;
            tournamentModel.OnSelectedRoundChanged += (round) => UpdateDisplay2StatusUI();

            RenderPlayerList();
            RenderBracket();
            UpdateDisplay2StatusUI();
        }

        private void OnDestroy()
        {
            if (tournamentModel != null)
            {
                tournamentModel.OnDisplayViewChanged -= OnDisplayViewChanged;
                tournamentModel.OnSelectedMatchChanged -= OnSelectedMatchChanged;
            }
        }

        private void BindUIElements()
        {
            playerScrollView = rootVisualElement.Q<ScrollView>("PlayerScrollView");
            bracketContainer = rootVisualElement.Q<VisualElement>("BracketContainer");
            playerNameInput = rootVisualElement.Q<TextField>("PlayerNameInput");
            tournamentTitleLabel = rootVisualElement.Q<Label>("TournamentTitleLabel");
            sidebarPanel = rootVisualElement.Q<VisualElement>("SidebarPanel");

            btnAddPlayer = rootVisualElement.Q<Button>("BtnAddPlayer");
            btnPreset4 = rootVisualElement.Q<Button>("BtnPreset4");
            btnPreset8 = rootVisualElement.Q<Button>("BtnPreset8");
            btnPreset16 = rootVisualElement.Q<Button>("BtnPreset16");
            btnShuffle = rootVisualElement.Q<Button>("BtnShuffle");
            btnResetDefaults = rootVisualElement.Q<Button>("BtnResetDefaults");
            btnGenerateBracket = rootVisualElement.Q<Button>("BtnGenerateBracket");
            btnToggleSidebar = rootVisualElement.Q<Button>("BtnToggleSidebar");

            // Display Controls (Standard)
            btnViewBracket = rootVisualElement.Q<Button>("BtnViewBracket");
            btnViewRound = rootVisualElement.Q<Button>("BtnViewRound");
            btnViewCombat = rootVisualElement.Q<Button>("BtnViewCombat");
            btnViewChampion = rootVisualElement.Q<Button>("BtnViewChampion");

            // Display Controls (Chroma Key OBS)
            btnChromaCleanFeed = rootVisualElement.Q<Button>("BtnChromaCleanFeed");
            btnChromaBracket = rootVisualElement.Q<Button>("BtnChromaBracket");
            btnChromaRound = rootVisualElement.Q<Button>("BtnChromaRound");
            btnChromaCombatSF = rootVisualElement.Q<Button>("BtnChromaCombatSF");
            btnChromaChampion = rootVisualElement.Q<Button>("BtnChromaChampion");

            roundSelectorContainer = rootVisualElement.Q<VisualElement>("RoundSelectorContainer");
            display2StatusText = rootVisualElement.Q<Label>("Display2StatusText");

            // Music, Stinger & Display Swap Control
            btnToggleMusic = rootVisualElement.Q<Button>("BtnToggleMusic");
            toggleStinger = rootVisualElement.Q<Toggle>("ToggleStinger");
            btnSwapDisplays = rootVisualElement.Q<Button>("BtnSwapDisplays");

            // Video Action Controls (GO & REP)
            btnGo = rootVisualElement.Q<Button>("BtnGo");
            btnRep = rootVisualElement.Q<Button>("BtnRep");

            // Quit Confirmation Modal & Close Button
            btnCloseApp = rootVisualElement.Q<Button>("BtnCloseApp");
            confirmQuitModal = rootVisualElement.Q<VisualElement>("ConfirmQuitModal");
            btnConfirmQuitYes = rootVisualElement.Q<Button>("BtnConfirmQuitYes");
            btnConfirmQuitNo = rootVisualElement.Q<Button>("BtnConfirmQuitNo");
        }

        private void RegisterEvents()
        {
            if (btnAddPlayer != null) btnAddPlayer.clicked += OnAddPlayerClicked;
            if (playerNameInput != null)
            {
                playerNameInput.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        OnAddPlayerClicked();
                    }
                });
            }

            if (btnPreset4 != null) btnPreset4.clicked += () => LoadPreset(4);
            if (btnPreset8 != null) btnPreset8.clicked += () => LoadPreset(8);
            if (btnPreset16 != null) btnPreset16.clicked += () => LoadPreset(16);

            if (btnShuffle != null) btnShuffle.clicked += OnShuffleClicked;
            if (btnResetDefaults != null) btnResetDefaults.clicked += OnResetDefaultsClicked;
            if (btnGenerateBracket != null) btnGenerateBracket.clicked += OnGenerateBracketClicked;
            if (btnToggleSidebar != null) btnToggleSidebar.clicked += OnToggleSidebarClicked;
            if (btnSwapDisplays != null) btnSwapDisplays.clicked += OnSwapDisplaysClicked;

            // Display View Controls (Standard)
            if (btnViewBracket != null) btnViewBracket.clicked += () => RequestDisplayViewChange(DisplayViewType.Bracket);
            if (btnViewRound != null) btnViewRound.clicked += () => RequestDisplayViewChange(DisplayViewType.SingleRound);
            if (btnViewCombat != null) btnViewCombat.clicked += () => RequestDisplayViewChange(DisplayViewType.Combat);
            if (btnViewChampion != null) btnViewChampion.clicked += () => RequestDisplayViewChange(DisplayViewType.ChampionPodium);

            // Display View Controls (Chroma Key OBS)
            if (btnChromaCleanFeed != null) btnChromaCleanFeed.clicked += () => RequestDisplayViewChange(DisplayViewType.ChromaCleanFeed);
            if (btnChromaBracket != null) btnChromaBracket.clicked += () => RequestDisplayViewChange(DisplayViewType.ChromaBracket);
            if (btnChromaRound != null) btnChromaRound.clicked += () => RequestDisplayViewChange(DisplayViewType.ChromaSingleRound);
            if (btnChromaCombatSF != null) btnChromaCombatSF.clicked += () => RequestDisplayViewChange(DisplayViewType.ChromaCombatSF);
            if (btnChromaChampion != null) btnChromaChampion.clicked += () => RequestDisplayViewChange(DisplayViewType.ChromaChampion);

            // Video Action Controls (GO & REP)
            if (btnGo != null) btnGo.clicked += () => PlayNouArbitreVideo(useStinger: true);
            if (btnRep != null) btnRep.clicked += () => PlayNouArbitreVideo(useStinger: false);

            // Stinger Toggle Control
            if (toggleStinger != null)
            {
                toggleStinger.value = useStingerTransitions;
                toggleStinger.RegisterValueChangedCallback(evt =>
                {
                    useStingerTransitions = evt.newValue;
                    toggleStinger.label = useStingerTransitions ? "🎬 Stinger: ON" : "🎬 Stinger: OFF";
                    if (display2Controller != null) display2Controller.SetStingerEnabled(useStingerTransitions);
                    if (display3Controller != null) display3Controller.SetStingerEnabled(useStingerTransitions);
                });
            }

            // Music Toggle Control
            if (btnToggleMusic != null) btnToggleMusic.clicked += OnToggleMusicClicked;

            // Quit Confirmation Modal & Close Button Events
            if (btnCloseApp != null) btnCloseApp.clicked += ShowQuitConfirmationModal;
            if (btnConfirmQuitYes != null) btnConfirmQuitYes.clicked += QuitApplication;
            if (btnConfirmQuitNo != null) btnConfirmQuitNo.clicked += HideQuitConfirmationModal;
        }

        private void OnSwapDisplaysClicked()
        {
            int numDisplays = Display.displays.Length;
            if (!Application.isEditor && numDisplays <= 2)
            {
                int currentMode = PlayerPrefs.GetInt("Janken_Display2_Mode", 0);
                int nextMode = (currentMode + 1) % 3; // 0=Horiz, 1=Vert, 2=Off
                PlayerPrefs.SetInt("Janken_Display2_Mode", nextMode);
            }
            else
            {
                int currentMode = PlayerPrefs.GetInt("Janken_Display3_Mode", 0);
                int nextMode = (currentMode + 1) % 3; // 0=Default, 1=Swapped, 2=Off
                PlayerPrefs.SetInt("Janken_Display3_Mode", nextMode);
            }
            PlayerPrefs.Save();

            ApplyDisplayTargetMapping();
            UpdateDisplay2StatusUI();
            Debug.Log($"[Janken MultiDisplay] Mode de pantalles canviat amb èxit.");
        }

        private void EnsureAllDisplayReferences()
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.isLoaded)
            {
                var rootObjects = activeScene.GetRootGameObjects();
                foreach (var root in rootObjects)
                {
                    FindReferencesInHierarchy(root.transform);
                }
            }
        }

        private void FindReferencesInHierarchy(Transform current)
        {
            if (current.name == "Display2_Audience")
            {
                var d2 = current.GetComponent<Display2Controller>();
                if (d2 != null && d2.GetType() == typeof(Display2Controller))
                {
                    display2Controller = d2;
                }
            }
            else if (current.name == "Display3_Audience")
            {
                var d3 = current.GetComponent<Display3Controller>();
                if (d3 != null)
                {
                    display3Controller = d3;
                }
            }
            else if (current.name == "Display2_Camera")
            {
                var cam2 = current.GetComponent<Camera>();
                if (cam2 != null) display2Camera = cam2;
            }
            else if (current.name == "Display3_Camera")
            {
                var cam3 = current.GetComponent<Camera>();
                if (cam3 != null) display3Camera = cam3;
            }

            for (int i = 0; i < current.childCount; i++)
            {
                FindReferencesInHierarchy(current.GetChild(i));
            }
        }

        public void ApplyDisplayTargetMapping()
        {
            EnsureAllDisplayReferences();

            int numDisplays = Display.displays.Length;

            // In standalone build, explicitly activate secondary physical displays at native resolution
            if (!Application.isEditor)
            {
                for (int i = 1; i < numDisplays; i++)
                {
                    Display d = Display.displays[i];
                    int sysW = d.systemWidth > 0 ? d.systemWidth : 1920;
                    int sysH = d.systemHeight > 0 ? d.systemHeight : 1080;

                    if (!d.active)
                    {
                        d.Activate(sysW, sysH, 60);
                    }
                    d.SetRenderingResolution(sysW, sysH);
                    Debug.Log($"[Janken] Display {i + 1} activat físicament a {sysW}x{sysH}.");
                }
            }

            // -------------------------------------------------------------
            // SETUP 1: PORTÀTIL + 1 MONITOR EXTERN (2 Displays en total o 1 Monitor)
            // -------------------------------------------------------------
            if (!Application.isEditor && numDisplays <= 2)
            {
                // Mode: 0 = D2 Horitzontal, 1 = D3 Vertical, 2 = Desactivat
                int mode = PlayerPrefs.GetInt("Janken_Display2_Mode", 0);

                bool showD2 = (mode == 0);
                bool showD3 = (mode == 1);

                if (btnSwapDisplays != null)
                {
                    if (mode == 0)
                        btnSwapDisplays.text = "📺 Monitor 2: D2 Horitzontal (Clic -> D3 Vertical)";
                    else if (mode == 1)
                        btnSwapDisplays.text = "📱 Monitor 2: D3 Vertical (Clic -> Desactivat)";
                    else
                        btnSwapDisplays.text = "🚫 Monitor 2: Desactivat (Clic -> D2 Horitzontal)";
                }

                // Configure Display 2 (Horizontal - 1920x1080)
                if (display2Controller != null)
                {
                    display2Controller.gameObject.SetActive(showD2);
                    var doc2 = display2Controller.GetComponent<UIDocument>();
                    if (doc2 != null)
                    {
                        if (doc2.panelSettings != null) doc2.panelSettings.targetDisplay = 1;
                        doc2.enabled = false;
                        if (showD2) doc2.enabled = true;
                    }
                    if (showD2)
                    {
                        display2Controller.InitializeUI();
                        display2Controller.RefreshCurrentView();
                    }
                }
                if (display2Camera != null)
                {
                    display2Camera.gameObject.SetActive(showD2);
                    display2Camera.enabled = showD2;
                    if (showD2)
                    {
                        display2Camera.targetDisplay = 1;
                        display2Camera.clearFlags = CameraClearFlags.SolidColor;
                        display2Camera.backgroundColor = new Color(0.035f, 0.05f, 0.086f);
                    }
                }

                // Configure Display 3 (Vertical - 1080x1920 PanelSettings)
                if (display3Controller != null)
                {
                    display3Controller.gameObject.SetActive(showD3);
                    var doc3 = display3Controller.GetComponent<UIDocument>();
                    if (doc3 != null)
                    {
                        if (doc3.panelSettings != null) doc3.panelSettings.targetDisplay = 1;
                        doc3.enabled = false;
                        if (showD3) doc3.enabled = true;
                    }
                    if (showD3)
                    {
                        display3Controller.InitializeUI();
                        display3Controller.RefreshCurrentView();
                    }
                }
                if (display3Camera != null)
                {
                    display3Camera.gameObject.SetActive(showD3);
                    display3Camera.enabled = showD3;
                    if (showD3)
                    {
                        display3Camera.targetDisplay = 1;
                        display3Camera.clearFlags = CameraClearFlags.SolidColor;
                        display3Camera.backgroundColor = new Color(0.035f, 0.05f, 0.086f);
                    }
                }

                return;
            }

            // -------------------------------------------------------------
            // SETUP 2: PORTÀTIL + 2 MONITORS EXTERNS / EDITOR (3 Displays)
            // -------------------------------------------------------------
            int mode3 = PlayerPrefs.GetInt("Janken_Display3_Mode", 0); // 0=Default, 1=Swapped, 2=Off
            bool isSwapped = (mode3 == 1);
            bool isOff = (mode3 == 2);

            int d2Target = isSwapped ? 2 : 1;
            int d3Target = isSwapped ? 1 : 2;

            if (btnSwapDisplays != null)
            {
                if (isOff)
                    btnSwapDisplays.text = "🚫 MultiDisplay: Desactivat (Clic -> Activar)";
                else if (isSwapped)
                    btnSwapDisplays.text = "🔄 Setup [3 Displays]: D3 M2 | D2 M3";
                else
                    btnSwapDisplays.text = "🔁 Setup [3 Displays]: D2 M2 | D3 M3";
            }

            bool enableD2 = !isOff;
            bool enableD3 = !isOff;

            if (display2Controller != null)
            {
                display2Controller.gameObject.SetActive(enableD2);
                var doc2 = display2Controller.GetComponent<UIDocument>();
                if (doc2 != null)
                {
                    doc2.enabled = false;
                    if (enableD2) doc2.enabled = true;
                }
                if (enableD2)
                {
                    display2Controller.InitializeUI();
                    display2Controller.RefreshCurrentView();
                }
            }
            if (display2Camera != null)
            {
                display2Camera.gameObject.SetActive(enableD2);
                display2Camera.enabled = enableD2;
                if (enableD2) display2Camera.targetDisplay = d2Target;
            }

            if (display3Controller != null)
            {
                display3Controller.gameObject.SetActive(enableD3);
                var doc3 = display3Controller.GetComponent<UIDocument>();
                if (doc3 != null)
                {
                    doc3.enabled = false;
                    if (enableD3) doc3.enabled = true;
                }
                if (enableD3)
                {
                    display3Controller.InitializeUI();
                    display3Controller.RefreshCurrentView();
                }
            }
            if (display3Camera != null)
            {
                display3Camera.gameObject.SetActive(enableD3);
                display3Camera.enabled = enableD3;
                if (enableD3) display3Camera.targetDisplay = d3Target;
            }
        }

        public void ShowQuitConfirmationModal()
        {
            if (confirmQuitModal != null)
            {
                confirmQuitModal.RemoveFromClassList("modal-hidden");
                confirmQuitModal.style.display = DisplayStyle.Flex;
                confirmQuitModal.pickingMode = PickingMode.Position;
            }
        }

        public void HideQuitConfirmationModal()
        {
            if (confirmQuitModal != null)
            {
                confirmQuitModal.AddToClassList("modal-hidden");
                confirmQuitModal.style.display = DisplayStyle.None;
                confirmQuitModal.pickingMode = PickingMode.Ignore;
            }
        }

        public void QuitApplication()
        {
            Debug.Log("[Janken] Tancant l'aplicació...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void PlayNouArbitreVideo(bool useStinger)
        {
            if (useStinger)
            {
                hasPlayedGoInCurrentVideoSession = true;
            }
            float targetTime = useStinger ? goVideoStartTime : repVideoStartTime;
            if (display2Controller != null && display2Controller.gameObject.activeInHierarchy) display2Controller.PlayNouArbitreVideo(useStinger, targetTime);
            if (display3Controller != null && display3Controller.gameObject.activeInHierarchy) display3Controller.PlayNouArbitreVideo(useStinger, targetTime);

            if ((display2Controller == null || !display2Controller.gameObject.activeInHierarchy) &&
                (display3Controller == null || !display3Controller.gameObject.activeInHierarchy) && tournamentModel != null)
            {
                tournamentModel.SetDisplayView(DisplayViewType.NouArbitreVideo);
            }
        }

        private void HandleHotkeys()
        {
            if (tournamentModel == null) return;
            if (IsUserEditingText()) return;

            bool tabPressed = false;
            bool pageDownPressed = false;
            bool pageUpPressed = false;
            bool escapePressed = false;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                tabPressed = kb.tabKey.wasPressedThisFrame;
                pageDownPressed = kb.pageDownKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame;
                pageUpPressed = kb.pageUpKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
                escapePressed = kb.escapeKey.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            tabPressed = Input.GetKeyDown(KeyCode.Tab);
            pageDownPressed = Input.GetKeyDown(KeyCode.PageDown) || Input.GetKeyDown(KeyCode.DownArrow);
            pageUpPressed = Input.GetKeyDown(KeyCode.PageUp) || Input.GetKeyDown(KeyCode.UpArrow);
            escapePressed = Input.GetKeyDown(KeyCode.Escape);
#endif

            // Escape Hotkey: Toggle Quit Confirmation Modal
            if (escapePressed)
            {
                if (confirmQuitModal != null && confirmQuitModal.style.display == DisplayStyle.Flex && !confirmQuitModal.ClassListContains("modal-hidden"))
                {
                    HideQuitConfirmationModal();
                }
                else
                {
                    ShowQuitConfirmationModal();
                }
            }

            // TAB Hotkey: First press from another view -> GO. Subsequent presses in video view -> REP.
            if (tabPressed)
            {
                if (tournamentModel.CurrentDisplayView != DisplayViewType.NouArbitreVideo || !hasPlayedGoInCurrentVideoSession)
                {
                    hasPlayedGoInCurrentVideoSession = true;
                    PlayNouArbitreVideo(useStinger: true);
                }
                else
                {
                    PlayNouArbitreVideo(useStinger: false);
                }
            }

            // Page Down / Down Arrow: Always open Quadre (Bracket)
            if (pageDownPressed)
            {
                RequestDisplayViewChange(DisplayViewType.Bracket);
            }

            // Page Up / Up Arrow: Always open Combat
            if (pageUpPressed)
            {
                RequestDisplayViewChange(DisplayViewType.Combat);
            }
        }

        private bool IsUserEditingText()
        {
            if (rootVisualElement?.focusController?.focusedElement == null) return false;
            var focused = rootVisualElement.focusController.focusedElement;

            if (focused is TextField) return true;

            string typeName = focused.GetType().Name;
            return typeName.Contains("TextField") || typeName.Contains("TextInput") || typeName.Contains("Editor");
        }

        private Display2Controller GetActiveAudienceController()
        {
            if (display2Controller != null && display2Controller.gameObject.activeInHierarchy)
                return display2Controller;
            if (display3Controller != null && display3Controller.gameObject.activeInHierarchy)
                return display3Controller;
            return null;
        }

        private void RequestDisplayViewChange(DisplayViewType targetView)
        {
            if (tournamentModel == null) return;
            if (tournamentModel.CurrentDisplayView == targetView) return;

            Display2Controller activeController = GetActiveAudienceController();

            if (useStingerTransitions && activeController != null && activeController.gameObject.activeInHierarchy)
            {
                activeController.PlayStingerTransition(() =>
                {
                    tournamentModel.SetDisplayView(targetView);
                });
            }
            else
            {
                tournamentModel.SetDisplayView(targetView);
            }
        }

        #region Audio Control Logic

        private void OnToggleMusicClicked()
        {
            isMusicPlaying = !isMusicPlaying;

            if (audioFadeCoroutine != null) StopCoroutine(audioFadeCoroutine);

            if (isMusicPlaying)
            {
                audioFadeCoroutine = StartCoroutine(FadeInMusic());
                if (btnToggleMusic != null)
                {
                    btnToggleMusic.text = "🔊 Música: ON";
                    btnToggleMusic.AddToClassList("btn-music-active");
                }
            }
            else
            {
                audioFadeCoroutine = StartCoroutine(FadeOutMusic());
                if (btnToggleMusic != null)
                {
                    btnToggleMusic.text = "🎵 Música: OFF";
                    btnToggleMusic.RemoveFromClassList("btn-music-active");
                }
            }
        }

        private IEnumerator FadeInMusic()
        {
            // VideoPlayer (.mp4)
            if (videoPlayer != null && videoPlayer.clip != null)
            {
                if (!videoPlayer.isPlaying) videoPlayer.Play();

                float startVol = videoPlayer.GetDirectAudioVolume(0);
                float timer = 0f;

                while (timer < fadeDuration)
                {
                    timer += Time.deltaTime;
                    float vol = Mathf.Lerp(startVol, 1f, timer / fadeDuration);
                    videoPlayer.SetDirectAudioVolume(0, vol);
                    yield return null;
                }

                videoPlayer.SetDirectAudioVolume(0, 1f);
            }

            // AudioSource (.wav / .mp3)
            if (audioSource != null && audioSource.clip != null)
            {
                if (!audioSource.isPlaying) audioSource.Play();

                float startVol = audioSource.volume;
                float timer = 0f;

                while (timer < fadeDuration)
                {
                    timer += Time.deltaTime;
                    audioSource.volume = Mathf.Lerp(startVol, 1f, timer / fadeDuration);
                    yield return null;
                }

                audioSource.volume = 1f;
            }
        }

        private IEnumerator FadeOutMusic()
        {
            // VideoPlayer (.mp4)
            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                float startVol = videoPlayer.GetDirectAudioVolume(0);
                float timer = 0f;

                while (timer < fadeDuration)
                {
                    timer += Time.deltaTime;
                    float vol = Mathf.Lerp(startVol, 0f, timer / fadeDuration);
                    videoPlayer.SetDirectAudioVolume(0, vol);
                    yield return null;
                }

                videoPlayer.SetDirectAudioVolume(0, 0f);
                videoPlayer.Stop();
                videoPlayer.time = 0f; // Reset / Rewind
            }

            // AudioSource (.wav / .mp3)
            if (audioSource != null && audioSource.isPlaying)
            {
                float startVol = audioSource.volume;
                float timer = 0f;

                while (timer < fadeDuration)
                {
                    timer += Time.deltaTime;
                    audioSource.volume = Mathf.Lerp(startVol, 0f, timer / fadeDuration);
                    yield return null;
                }

                audioSource.volume = 0f;
                audioSource.Stop();
                audioSource.time = 0f; // Reset / Rewind
            }
        }

        #endregion

        private void OnDisplayViewChanged(DisplayViewType newView)
        {
            if (newView != DisplayViewType.NouArbitreVideo)
            {
                hasPlayedGoInCurrentVideoSession = false;
            }
            UpdateDisplay2StatusUI();
        }

        private void OnSelectedMatchChanged(Match match)
        {
            UpdateDisplay2StatusUI();
        }

        private void UpdateDisplay2StatusUI()
        {
            if (tournamentModel == null) return;

            // Highlight Active View Button (Standard)
            SetButtonActive(btnViewBracket, tournamentModel.CurrentDisplayView == DisplayViewType.Bracket);
            SetButtonActive(btnViewRound, tournamentModel.CurrentDisplayView == DisplayViewType.SingleRound);
            SetButtonActive(btnViewCombat, tournamentModel.CurrentDisplayView == DisplayViewType.Combat);
            SetButtonActive(btnViewChampion, tournamentModel.CurrentDisplayView == DisplayViewType.ChampionPodium);

            // Highlight Active View Button (Chroma)
            SetButtonActive(btnChromaCleanFeed, tournamentModel.CurrentDisplayView == DisplayViewType.ChromaCleanFeed);
            SetButtonActive(btnChromaBracket, tournamentModel.CurrentDisplayView == DisplayViewType.ChromaBracket);
            SetButtonActive(btnChromaRound, tournamentModel.CurrentDisplayView == DisplayViewType.ChromaSingleRound);
            SetButtonActive(btnChromaCombatSF, tournamentModel.CurrentDisplayView == DisplayViewType.ChromaCombatSF);
            SetButtonActive(btnChromaChampion, tournamentModel.CurrentDisplayView == DisplayViewType.ChromaChampion);

            RenderRoundSelector();

            // Update Status Text
            if (display2StatusText != null)
            {
                switch (tournamentModel.CurrentDisplayView)
                {
                    case DisplayViewType.Bracket:
                        display2StatusText.text = "📺 DISPLAY 2: Mostrant QUADRE GENERAL (Estàndard)";
                        break;
                    case DisplayViewType.SingleRound:
                        string roundTitle = tournamentModel.GetRoundTitle(tournamentModel.SelectedRoundIndex);
                        display2StatusText.text = $"📺 DISPLAY 2: Mostrant RONDA FOCUS ( {roundTitle} )";
                        break;
                    case DisplayViewType.Combat:
                        Match activeMatch = tournamentModel.SelectedCombatMatch ?? tournamentModel.GetNextPlayableMatch();
                        string p1 = activeMatch?.player1 != null ? activeMatch.player1.name : "?";
                        string p2 = activeMatch?.player2 != null ? activeMatch.player2.name : "?";
                        display2StatusText.text = $"📺 DISPLAY 2: Mostrant COMBAT ( {p1} VS {p2} )";
                        break;
                    case DisplayViewType.ChampionPodium:
                        string champ = tournamentModel.Champion != null ? tournamentModel.Champion.name : "EN CURS";
                        display2StatusText.text = $"📺 DISPLAY 2: Mostrant VICTÒRIA DEL TORNEIG ( {champ} )";
                        break;
                    case DisplayViewType.ChromaCleanFeed:
                        display2StatusText.text = "🟢 DISPLAY (CROMA): Fons Verd Pur Net (Clean Feed)";
                        break;
                    case DisplayViewType.ChromaBracket:
                        display2StatusText.text = "🟢 DISPLAY 2 (CROMA): Mostrant QUADRE GENERAL (Fons Verd OBS)";
                        break;
                    case DisplayViewType.ChromaSingleRound:
                        string cRoundTitle = tournamentModel.GetRoundTitle(tournamentModel.SelectedRoundIndex);
                        display2StatusText.text = $"🟢 DISPLAY 2 (CROMA): Mostrant RONDA FOCUS ( {cRoundTitle} )";
                        break;
                    case DisplayViewType.ChromaCombatSF:
                        Match sfMatch = tournamentModel.SelectedCombatMatch ?? tournamentModel.GetNextPlayableMatch();
                        string sfp1 = sfMatch?.player1 != null ? sfMatch.player1.name : "?";
                        string sfp2 = sfMatch?.player2 != null ? sfMatch.player2.name : "?";
                        display2StatusText.text = $"🟢 DISPLAY 2 (CROMA): Mostrant HUD STREET FIGHTER ( {sfp1} VS {sfp2} )";
                        break;
                    case DisplayViewType.ChromaChampion:
                        string cChamp = tournamentModel.Champion != null ? tournamentModel.Champion.name : "EN CURS";
                        display2StatusText.text = $"🟢 DISPLAY 2 (CROMA): Mostrant VICTÒRIA DEL TORNEIG ( {cChamp} )";
                        break;
                    case DisplayViewType.NouArbitreVideo:
                        display2StatusText.text = "🎬 DISPLAY 2: Reproduint vídeo NOU ÀRBITRE";
                        break;
                }
            }
        }

        private void RenderRoundSelector()
        {
            if (roundSelectorContainer == null) return;
            roundSelectorContainer.Clear();

            if (!tournamentModel.IsActive || tournamentModel.Rounds.Count == 0) return;

            for (int r = 0; r < tournamentModel.Rounds.Count; r++)
            {
                int roundIdx = r;
                string rName = tournamentModel.GetRoundTitle(r);
                var btnRoundPill = new Button(() =>
                {
                    tournamentModel.SetSelectedRound(roundIdx);
                    bool isChroma = tournamentModel.CurrentDisplayView == DisplayViewType.ChromaBracket ||
                                    tournamentModel.CurrentDisplayView == DisplayViewType.ChromaSingleRound ||
                                    tournamentModel.CurrentDisplayView == DisplayViewType.ChromaCombatSF ||
                                    tournamentModel.CurrentDisplayView == DisplayViewType.ChromaChampion ||
                                    tournamentModel.CurrentDisplayView == DisplayViewType.ChromaCleanFeed;
                    tournamentModel.SetDisplayView(isChroma ? DisplayViewType.ChromaSingleRound : DisplayViewType.SingleRound);
                    if (display2Controller != null) display2Controller.RefreshCurrentView();
                });
                btnRoundPill.text = rName;
                btnRoundPill.AddToClassList("btn-round-pill");
                if (r == tournamentModel.SelectedRoundIndex && (tournamentModel.CurrentDisplayView == DisplayViewType.SingleRound || tournamentModel.CurrentDisplayView == DisplayViewType.ChromaSingleRound))
                {
                    btnRoundPill.AddToClassList("btn-round-pill-active");
                }
                roundSelectorContainer.Add(btnRoundPill);
            }
        }

        private void SetButtonActive(Button btn, bool active)
        {
            if (btn == null) return;
            if (active) btn.AddToClassList("btn-display-view-active");
            else btn.RemoveFromClassList("btn-display-view-active");
        }

        private void LoadPreset(int count)
        {
            tournamentModel.SetDefaultPlayers(count);
            RenderPlayerList();
            tournamentModel.GenerateBracket();
            RenderBracket();
        }

        private void OnAddPlayerClicked()
        {
            if (playerNameInput == null) return;
            string name = playerNameInput.value;
            if (!string.IsNullOrWhiteSpace(name))
            {
                tournamentModel.AddPlayer(name);
                playerNameInput.value = "";
                RenderPlayerList();
                if (tournamentModel.IsActive)
                {
                    tournamentModel.GenerateBracket();
                    RenderBracket();
                }
            }
        }

        private void OnShuffleClicked()
        {
            tournamentModel.ShufflePlayers();
            RenderPlayerList();
            if (tournamentModel.IsActive)
            {
                tournamentModel.GenerateBracket();
                RenderBracket();
            }
        }

        private void OnResetDefaultsClicked()
        {
            tournamentModel.ResetToDefaults(8);
            RenderPlayerList();
            RenderBracket();
        }

        private void OnGenerateBracketClicked()
        {
            tournamentModel.GenerateBracket();
            RenderBracket();
        }

        private void OnToggleSidebarClicked()
        {
            if (sidebarPanel == null) return;
            bool isHidden = sidebarPanel.style.display == DisplayStyle.None;
            sidebarPanel.style.display = isHidden ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RenderPlayerList()
        {
            if (playerScrollView == null) return;
            playerScrollView.Clear();

            foreach (var p in tournamentModel.Players)
            {
                var row = new VisualElement();
                row.AddToClassList("player-item-row");

                var seedLabel = new Label($"#{p.seed}");
                seedLabel.AddToClassList("player-item-seed");

                var nameInput = new TextField();
                nameInput.value = p.name;
                nameInput.AddToClassList("player-item-name-input");
                nameInput.tooltip = "Haz clic para editar el nombre";

                nameInput.RegisterValueChangedCallback(evt =>
                {
                    tournamentModel.UpdatePlayerName(p.id, evt.newValue);
                    RenderBracket();
                });

                var btnRemove = new Button(() =>
                {
                    tournamentModel.RemovePlayer(p.id);
                    RenderPlayerList();
                    if (tournamentModel.IsActive)
                    {
                        tournamentModel.GenerateBracket();
                        RenderBracket();
                    }
                });
                btnRemove.text = "✕";
                btnRemove.AddToClassList("btn-danger");

                row.Add(seedLabel);
                row.Add(nameInput);
                row.Add(btnRemove);

                playerScrollView.Add(row);
            }
        }

        private void RenderBracket()
        {
            if (bracketContainer == null) return;
            bracketContainer.Clear();

            if (!tournamentModel.IsActive || tournamentModel.Rounds.Count == 0)
            {
                var emptyLabel = new Label("Afageix jugadors i prem 'GENERAR QUADRE' per començar.");
                emptyLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
                emptyLabel.style.fontSize = 16;
                emptyLabel.style.marginTop = 40;
                bracketContainer.Add(emptyLabel);
                return;
            }

            const float HEADER_HEIGHT = 36f;
            const float HEADER_MARGIN = 20f;
            const float HEADER_TOTAL = HEADER_HEIGHT + HEADER_MARGIN;
            const float CARD_HEIGHT = 80f;
            const float BASE_GAP = 24f;
            const float SLOT_HEIGHT = CARD_HEIGHT + BASE_GAP;

            int totalRounds = tournamentModel.Rounds.Count;

            for (int r = 0; r < totalRounds; r++)
            {
                int currentRoundIndex = r;
                var roundMatches = tournamentModel.Rounds[r];
                var roundColumn = new VisualElement();
                roundColumn.AddToClassList("round-column");

                var roundHeader = new Button(() =>
                {
                    tournamentModel.SetSelectedRound(currentRoundIndex);
                    bool isChroma = tournamentModel.CurrentDisplayView == DisplayViewType.ChromaBracket ||
                                    tournamentModel.CurrentDisplayView == DisplayViewType.ChromaSingleRound ||
                                    tournamentModel.CurrentDisplayView == DisplayViewType.ChromaCombatSF ||
                                    tournamentModel.CurrentDisplayView == DisplayViewType.ChromaChampion ||
                                    tournamentModel.CurrentDisplayView == DisplayViewType.ChromaCleanFeed;
                    tournamentModel.SetDisplayView(isChroma ? DisplayViewType.ChromaSingleRound : DisplayViewType.SingleRound);
                    if (display2Controller != null) display2Controller.RefreshCurrentView();
                });
                roundHeader.text = tournamentModel.GetRoundTitle(r);
                roundHeader.AddToClassList("round-header");
                roundHeader.tooltip = "Fes clic per enviar només aquesta ronda a Display 2!";
                roundColumn.Add(roundHeader);

                float multiplier = (float)Math.Pow(2, r);
                float topOffset = (SLOT_HEIGHT * (multiplier - 1f)) / 2f;
                float gap = SLOT_HEIGHT * (multiplier - 1f) + BASE_GAP;

                for (int m = 0; m < roundMatches.Count; m++)
                {
                    Match match = roundMatches[m];
                    VisualElement matchCard = CreateMatchCard(match);

                    float marginTop = (m == 0) ? topOffset : gap;
                    matchCard.style.marginTop = marginTop;

                    roundColumn.Add(matchCard);
                }

                bracketContainer.Add(roundColumn);

                // Connecting Lines
                if (r < totalRounds - 1)
                {
                    var connectorColumn = new VisualElement();
                    connectorColumn.AddToClassList("connector-column");

                    int nextRoundMatchesCount = roundMatches.Count / 2;
                    for (int m = 0; m < nextRoundMatchesCount; m++)
                    {
                        Match match1 = roundMatches[m * 2];
                        Match match2 = roundMatches[m * 2 + 1];

                        float yUpper = HEADER_TOTAL + topOffset + (m * 2) * (CARD_HEIGHT + gap) + CARD_HEIGHT / 2f;
                        float yLower = HEADER_TOTAL + topOffset + (m * 2 + 1) * (CARD_HEIGHT + gap) + CARD_HEIGHT / 2f;
                        float yMid = (yUpper + yLower) / 2f;

                        bool match1Active = match1 != null && match1.isCompleted;
                        bool match2Active = match2 != null && match2.isCompleted;

                        var upperArm = new VisualElement();
                        upperArm.AddToClassList("connector-arm");
                        if (match1Active) upperArm.AddToClassList("connector-arm-active");
                        upperArm.style.top = yUpper - 1f;
                        upperArm.style.left = 0f;
                        upperArm.style.width = 16f;
                        upperArm.style.height = 2f;

                        var lowerArm = new VisualElement();
                        lowerArm.AddToClassList("connector-arm");
                        if (match2Active) lowerArm.AddToClassList("connector-arm-active");
                        lowerArm.style.top = yLower - 1f;
                        lowerArm.style.left = 0f;
                        lowerArm.style.width = 16f;
                        lowerArm.style.height = 2f;

                        var verticalBar = new VisualElement();
                        verticalBar.AddToClassList("connector-arm");
                        if (match1Active || match2Active) verticalBar.AddToClassList("connector-arm-active");
                        verticalBar.style.top = yUpper - 1f;
                        verticalBar.style.left = 15f;
                        verticalBar.style.width = 2f;
                        verticalBar.style.height = yLower - yUpper + 2f;

                        var outArm = new VisualElement();
                        outArm.AddToClassList("connector-arm");
                        if (match1Active || match2Active) outArm.AddToClassList("connector-arm-active");
                        outArm.style.top = yMid - 1f;
                        outArm.style.left = 16f;
                        outArm.style.width = 16f;
                        outArm.style.height = 2f;

                        connectorColumn.Add(upperArm);
                        connectorColumn.Add(lowerArm);
                        connectorColumn.Add(verticalBar);
                        connectorColumn.Add(outArm);
                    }

                    bracketContainer.Add(connectorColumn);
                }
                else
                {
                    // Champion Card in final round
                    if (tournamentModel.Champion != null)
                    {
                        var championConnectorCol = new VisualElement();
                        championConnectorCol.AddToClassList("connector-column");

                        float finalTopOffset = (SLOT_HEIGHT * (multiplier - 1f)) / 2f;
                        float yFinalCenter = HEADER_TOTAL + finalTopOffset + CARD_HEIGHT / 2f;

                        var champArm = new VisualElement();
                        champArm.AddToClassList("connector-arm");
                        champArm.AddToClassList("connector-arm-active");
                        champArm.style.top = yFinalCenter - 1f;
                        champArm.style.left = 0f;
                        champArm.style.width = 32f;
                        champArm.style.height = 2f;

                        championConnectorCol.Add(champArm);
                        bracketContainer.Add(championConnectorCol);

                        var championCol = new VisualElement();
                        championCol.AddToClassList("champion-container");
                        championCol.style.marginTop = finalTopOffset + HEADER_TOTAL - 50f;

                        var championCard = new VisualElement();
                        championCard.AddToClassList("champion-card");

                        var iconLabel = new Label("🏆");
                        iconLabel.AddToClassList("champion-icon");

                        var titleLabel = new Label("VICTÒRIA DEL TORNEIG");
                        titleLabel.AddToClassList("champion-title");

                        var nameLabel = new Label(tournamentModel.Champion.name);
                        nameLabel.AddToClassList("champion-name");

                        championCard.Add(iconLabel);
                        championCard.Add(titleLabel);
                        championCard.Add(nameLabel);
                        championCol.Add(championCard);

                        bracketContainer.Add(championCol);
                    }
                }
            }
        }

        private VisualElement CreateMatchCard(Match match)
        {
            var card = new VisualElement();
            card.AddToClassList("match-card");
            card.style.flexDirection = FlexDirection.Row;
            card.style.alignItems = Align.Center;

            if (match.isCompleted)
            {
                card.AddToClassList("match-card-completed");
            }

            // Single combat select / mark button for the entire match
            var sendCombatBtn = new Button(() =>
            {
                tournamentModel.SetSelectedCombatMatch(match);
                RenderBracket();
                UpdateDisplay2StatusUI();
            });
            sendCombatBtn.text = "⚔️";
            sendCombatBtn.tooltip = "Marcar aquest combat com a actiu (prem '⚔️ Combat' o '🥊 SF Top Bar' per mostrar-lo)";
            sendCombatBtn.focusable = false;
            sendCombatBtn.AddToClassList("btn-match-send-combat");

            if (tournamentModel.SelectedCombatMatch == match)
            {
                sendCombatBtn.AddToClassList("btn-match-send-combat-active");
            }

            card.Add(sendCombatBtn);

            var slotsContainer = new VisualElement();
            slotsContainer.style.flexGrow = 1;
            slotsContainer.style.flexDirection = FlexDirection.Column;

            VisualElement slot1 = CreatePlayerSlot(match, match.player1, match.winner == match.player1 && match.player1 != null, 1);
            VisualElement slot2 = CreatePlayerSlot(match, match.player2, match.winner == match.player2 && match.player2 != null, 2);

            slotsContainer.Add(slot1);
            slotsContainer.Add(slot2);
            card.Add(slotsContainer);

            return card;
        }

        private VisualElement CreatePlayerSlot(Match match, Player player, bool isWinner, int slotIndex)
        {
            var slot = new VisualElement();
            slot.AddToClassList("match-slot");

            if (player == null)
            {
                slot.AddToClassList("match-slot-empty");
                var emptyLabel = new Label("---");
                emptyLabel.AddToClassList("slot-player-name");
                slot.Add(emptyLabel);
                return slot;
            }

            if (match.isCompleted)
            {
                if (isWinner)
                    slot.AddToClassList("match-slot-winner");
                else
                    slot.AddToClassList("match-slot-loser");
            }

            // Player Name Button (Clicking player name toggles winner)
            var nameBtn = new Button(() =>
            {
                tournamentModel.ToggleOrDeclareWinner(match, player);
                RenderBracket();
            });
            nameBtn.text = player.name;
            nameBtn.AddToClassList("slot-player-name-btn");
            slot.Add(nameBtn);

            // Star Button (Clicking star toggles winner)
            var winBtn = new Button(() =>
            {
                tournamentModel.ToggleOrDeclareWinner(match, player);
                RenderBracket();
            });

            if (isWinner)
            {
                winBtn.text = "⭐";
                winBtn.tooltip = "Desmarcar guanyador/a";
                winBtn.AddToClassList("btn-star-winner");
            }
            else
            {
                winBtn.text = "☆";
                winBtn.tooltip = "Marcar guanyador/a";
                winBtn.AddToClassList("btn-star-idle");
            }

            slot.Add(winBtn);

            return slot;
        }
    }
}

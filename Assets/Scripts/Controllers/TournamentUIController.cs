using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;
using Janken.Tournament;

namespace Janken.Controllers
{
    [RequireComponent(typeof(UIDocument))]
    public class TournamentUIController : MonoBehaviour
    {
        [Header("MultiDisplay References")]
        [SerializeField] private Display2Controller display2Controller;

        [Header("Audio Settings")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip backgroundMusicClip;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private VideoClip backgroundVideoClip;
        [SerializeField] private float fadeDuration = 1.5f;

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

        // Display 2 Controls
        private Button btnViewBracket;
        private Button btnViewCombat;
        private Button btnViewChampion;
        private Label display2StatusText;

        // Music Control
        private Button btnToggleMusic;
        private Coroutine audioFadeCoroutine;
        private bool isMusicPlaying = false;

        private TournamentModel tournamentModel;

        private void Start()
        {
            InitializeAudio();
            InitializeUI();
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

            rootVisualElement = uiDocument.rootVisualElement;
            if (rootVisualElement == null) return;

            tournamentModel = new TournamentModel();

            // Link Display 2 Controller
            if (display2Controller == null)
            {
#if UNITY_2023_1_OR_NEWER
                display2Controller = FindFirstObjectByType<Display2Controller>();
#else
                display2Controller = FindObjectOfType<Display2Controller>();
#endif
            }

            if (display2Controller != null)
            {
                display2Controller.BindModel(tournamentModel);
            }

            BindUIElements();
            RegisterEvents();

            tournamentModel.OnDisplayViewChanged += OnDisplayViewChanged;
            tournamentModel.OnSelectedMatchChanged += OnSelectedMatchChanged;

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

            // Display 2 Controls
            btnViewBracket = rootVisualElement.Q<Button>("BtnViewBracket");
            btnViewCombat = rootVisualElement.Q<Button>("BtnViewCombat");
            btnViewChampion = rootVisualElement.Q<Button>("BtnViewChampion");
            display2StatusText = rootVisualElement.Q<Label>("Display2StatusText");

            // Music Control
            btnToggleMusic = rootVisualElement.Q<Button>("BtnToggleMusic");
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

            // Display 2 View Controls
            if (btnViewBracket != null) btnViewBracket.clicked += () => tournamentModel.SetDisplayView(DisplayViewType.Bracket);
            if (btnViewCombat != null) btnViewCombat.clicked += () => tournamentModel.SetDisplayView(DisplayViewType.Combat);
            if (btnViewChampion != null) btnViewChampion.clicked += () => tournamentModel.SetDisplayView(DisplayViewType.ChampionPodium);

            // Music Toggle Control
            if (btnToggleMusic != null) btnToggleMusic.clicked += OnToggleMusicClicked;
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
            UpdateDisplay2StatusUI();
        }

        private void OnSelectedMatchChanged(Match match)
        {
            UpdateDisplay2StatusUI();
        }

        private void UpdateDisplay2StatusUI()
        {
            if (tournamentModel == null) return;

            // Highlight Active View Button
            SetButtonActive(btnViewBracket, tournamentModel.CurrentDisplayView == DisplayViewType.Bracket);
            SetButtonActive(btnViewCombat, tournamentModel.CurrentDisplayView == DisplayViewType.Combat);
            SetButtonActive(btnViewChampion, tournamentModel.CurrentDisplayView == DisplayViewType.ChampionPodium);

            // Update Status Text
            if (display2StatusText != null)
            {
                switch (tournamentModel.CurrentDisplayView)
                {
                    case DisplayViewType.Bracket:
                        display2StatusText.text = "📺 DISPLAY 2: Mostrant QUADRE GENERAL / CLASSIFICACIÓ";
                        break;
                    case DisplayViewType.Combat:
                        Match activeMatch = tournamentModel.SelectedCombatMatch ?? tournamentModel.GetNextPlayableMatch();
                        string p1 = activeMatch?.player1 != null ? activeMatch.player1.name : "?";
                        string p2 = activeMatch?.player2 != null ? activeMatch.player2.name : "?";
                        display2StatusText.text = $"📺 DISPLAY 2: Mostrant COMBAT ( {p1} VS {p2} )";
                        break;
                    case DisplayViewType.ChampionPodium:
                        string champ = tournamentModel.Champion != null ? tournamentModel.Champion.name : "EN CURS";
                        display2StatusText.text = $"📺 DISPLAY 2: Mostrant CAMPIÓ DEL TORNEIG ( {champ} )";
                        break;
                }
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
                var roundMatches = tournamentModel.Rounds[r];
                var roundColumn = new VisualElement();
                roundColumn.AddToClassList("round-column");

                var roundHeader = new Label(tournamentModel.GetRoundTitle(r));
                roundHeader.AddToClassList("round-header");
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

                        var titleLabel = new Label("CAMPIÓ DEL TORNEIG");
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

            // Single combat send button for the entire match
            var sendCombatBtn = new Button(() =>
            {
                tournamentModel.SetSelectedCombatMatch(match);
                tournamentModel.SetDisplayView(DisplayViewType.Combat);
            });
            sendCombatBtn.text = "⚔️";
            sendCombatBtn.tooltip = "Enviar aquest combat a Display 2";
            sendCombatBtn.focusable = false;
            sendCombatBtn.AddToClassList("btn-match-send-combat");

            if (tournamentModel.SelectedCombatMatch == match && tournamentModel.CurrentDisplayView == DisplayViewType.Combat)
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

            // Allow clicking anywhere on the slot / player name to declare/toggle winner!
            slot.RegisterCallback<ClickEvent>(evt =>
            {
                tournamentModel.ToggleOrDeclareWinner(match, player);
                RenderBracket();
                evt.StopPropagation();
            });

            var nameLabel = new Label(player.name);
            nameLabel.AddToClassList("slot-player-name");
            slot.Add(nameLabel);

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

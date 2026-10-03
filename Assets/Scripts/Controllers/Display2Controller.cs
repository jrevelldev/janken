using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Janken.Tournament;
using Janken.VFX;

namespace Janken.Controllers
{
    [RequireComponent(typeof(UIDocument))]
    public class Display2Controller : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement rootVisualElement;
        private VisualElement display2Root;

        [Header("Stinger Transition Settings")]
        [SerializeField] private bool useStingerTransitions = true;
        [SerializeField] private bool randomizeStingerFlip = true;
        [SerializeField] private bool useRandomStingerFromLibrary = true;
        [SerializeField] private StingerAnimationData activeStinger;
        [SerializeField] private List<StingerAnimationData> stingerLibrary = new List<StingerAnimationData>();
        private VisualElement stingerOverlay;
        private Coroutine activeStingerCoroutine;
        private bool isStingerPlaying = false;

        [Header("Nou Àrbitre Video Settings")]
        [SerializeField] private UnityEngine.Video.VideoPlayer videoPlayer;
        [SerializeField] private UnityEngine.Video.VideoClip nouArbitreClip;
        [SerializeField] private float goVideoStartTime = 0f;
        [SerializeField] private float repVideoStartTime = 0f;
        private float currentPlaybackStartTime = 0f;
        private VisualElement videoViewContainer;
        private VisualElement videoDisplayElement;
        private RenderTexture videoRenderTexture;

        public void SetStingerEnabled(bool enabled)
        {
            useStingerTransitions = enabled;
        }

        public StingerAnimationData GetStingerToPlay()
        {
            if (useRandomStingerFromLibrary && stingerLibrary != null && stingerLibrary.Count > 0)
            {
                var validStingers = stingerLibrary.FindAll(s => s != null && s.FrameCount > 0);
                if (validStingers.Count > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, validStingers.Count);
                    return validStingers[randomIndex];
                }
            }
            return activeStinger;
        }

        private Label headerTitleLabel;
        private Label headerSubtitleLabel;

        private VisualElement bracketViewContainer;
        private VisualElement bracketScrollView;
        private VisualElement bracketContainer;

        private VisualElement singleRoundViewContainer;
        private Label singleRoundHeaderTitle;
        private VisualElement singleRoundContainer;

        private VisualElement combatViewContainer;
        private Label combatRoundTitle;
        private VisualElement player1Card;
        private Label player1Initials;
        private Label player1Seed;
        private Label player1Name;
        private Label player1Status;

        private VisualElement player2Card;
        private Label player2Initials;
        private Label player2Seed;
        private Label player2Name;
        private Label player2Status;

        private VisualElement vsEmblemContainer;
        private Label combatStatusBanner;
        private Coroutine combatAnimCoroutine;

        // Street Fighter Top Bar HUD (Chroma Overlay)
        private VisualElement combatSFViewContainer;
        private Label sfRoundTitle;
        private VisualElement sfP1Card;
        private Label sfP1Name;
        private Label sfP1Seed;
        private Label sfP1Initials;
        private Label sfP1Status;

        private VisualElement sfP2Card;
        private Label sfP2Name;
        private Label sfP2Seed;
        private Label sfP2Initials;
        private Label sfP2Status;

        private VisualElement championViewContainer;
        private Label championStageName;

        [Header("Referee Character Poses (Janken Trio)")]
        [SerializeField] private Sprite refereeRockSprite;
        [SerializeField] private Sprite refereePaperSprite;
        [SerializeField] private Sprite refereeScissorsSprite;
        [SerializeField] private bool allowRefereeHorizontalFlip = true;
        private int lastRefereePoseIndex = -1;

        private VisualElement refereeCharacter;

        private TournamentModel tournamentModel;

        private void Start()
        {
#if UNITY_EDITOR
            if (nouArbitreClip == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("", new[] { "Assets/Videos" });
                foreach (string guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>(path);
                    if (clip != null)
                    {
                        nouArbitreClip = clip;
                        break;
                    }
                }
            }

            if (refereeRockSprite == null)
                refereeRockSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Referee_Rock.png");
            if (refereePaperSprite == null)
                refereePaperSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Referee_Paper.png");
            if (refereeScissorsSprite == null)
                refereeScissorsSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Referee_Scissors.png");
#endif
            ActivateSecondaryDisplay();
            InitializeUI();
        }

        private void ActivateSecondaryDisplay()
        {
            if (Application.isEditor) return;

            for (int i = 1; i < Display.displays.Length; i++)
            {
                Display d = Display.displays[i];
                int sysW = d.systemWidth > 0 ? d.systemWidth : 1920;
                int sysH = d.systemHeight > 0 ? d.systemHeight : 1080;

                if (!d.active)
                {
                    d.Activate(sysW, sysH, 60);
                }
                d.SetRenderingResolution(sysW, sysH);
                Debug.Log($"[Janken MultiDisplay] Physical Display {i + 1} activat a resolució nativa: {sysW}x{sysH}");
            }
        }

        public void BindModel(TournamentModel model)
        {
            if (tournamentModel != null)
            {
                tournamentModel.OnTournamentUpdated -= RefreshCurrentView;
                tournamentModel.OnDisplayViewChanged -= OnDisplayViewChanged;
                tournamentModel.OnSelectedMatchChanged -= OnSelectedMatchChanged;
                tournamentModel.OnSelectedRoundChanged -= OnSelectedRoundChanged;
            }

            tournamentModel = model;

            if (tournamentModel != null)
            {
                tournamentModel.OnTournamentUpdated += OnTournamentUpdated;
                tournamentModel.OnDisplayViewChanged += OnDisplayViewChanged;
                tournamentModel.OnSelectedMatchChanged += OnSelectedMatchChanged;
                tournamentModel.OnSelectedRoundChanged += OnSelectedRoundChanged;

                if (rootVisualElement == null)
                {
                    InitializeUI();
                }

                RefreshCurrentView();
            }
        }

        private void OnDestroy()
        {
            if (tournamentModel != null)
            {
                tournamentModel.OnTournamentUpdated -= RefreshCurrentView;
                tournamentModel.OnDisplayViewChanged -= OnDisplayViewChanged;
                tournamentModel.OnSelectedMatchChanged -= OnSelectedMatchChanged;
                tournamentModel.OnSelectedRoundChanged -= OnSelectedRoundChanged;
            }
        }

        private void OnTournamentUpdated()
        {
            // Do not interrupt active video playback when tournament bracket data updates
            if (tournamentModel != null && tournamentModel.CurrentDisplayView == DisplayViewType.NouArbitreVideo)
            {
                return;
            }
            RefreshCurrentView();
        }

        private void OnEnable()
        {
            InitializeUI();
        }

        public void InitializeUI()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            rootVisualElement = uiDocument.rootVisualElement;
            if (rootVisualElement == null) return;
            rootVisualElement.pickingMode = PickingMode.Ignore;

            BindUIElements();

            if (tournamentModel != null)
            {
                RefreshCurrentView();
            }
        }

        private void BindUIElements()
        {
            display2Root = rootVisualElement.Q<VisualElement>("Display2Root");
            headerTitleLabel = rootVisualElement.Q<Label>("Display2HeaderTitle");
            headerSubtitleLabel = rootVisualElement.Q<Label>("Display2HeaderSubtitle");

            bracketViewContainer = rootVisualElement.Q<VisualElement>("BracketViewContainer");
            bracketScrollView = rootVisualElement.Q<VisualElement>("Display2BracketScrollView");
            bracketContainer = rootVisualElement.Q<VisualElement>("Display2BracketContainer");

            singleRoundViewContainer = rootVisualElement.Q<VisualElement>("SingleRoundViewContainer");
            singleRoundHeaderTitle = rootVisualElement.Q<Label>("SingleRoundHeaderTitle");
            singleRoundContainer = rootVisualElement.Q<VisualElement>("Display2SingleRoundContainer");

            combatViewContainer = rootVisualElement.Q<VisualElement>("CombatViewContainer");
            combatRoundTitle = rootVisualElement.Q<Label>("CombatRoundTitle");

            player1Card = rootVisualElement.Q<VisualElement>("Player1Card");
            player1Initials = rootVisualElement.Q<Label>("Player1Initials");
            player1Seed = rootVisualElement.Q<Label>("Player1Seed");
            player1Name = rootVisualElement.Q<Label>("Player1Name");
            player1Status = rootVisualElement.Q<Label>("Player1Status");

            player2Card = rootVisualElement.Q<VisualElement>("Player2Card");
            player2Initials = rootVisualElement.Q<Label>("Player2Initials");
            player2Seed = rootVisualElement.Q<Label>("Player2Seed");
            player2Name = rootVisualElement.Q<Label>("Player2Name");
            player2Status = rootVisualElement.Q<Label>("Player2Status");

            vsEmblemContainer = rootVisualElement.Q<VisualElement>(className: "vs-emblem-container");
            combatStatusBanner = rootVisualElement.Q<Label>("CombatStatusBanner");

            // Street Fighter HUD Top Bar
            combatSFViewContainer = rootVisualElement.Q<VisualElement>("CombatSFViewContainer");
            sfRoundTitle = rootVisualElement.Q<Label>("SFRoundTitle");
            sfP1Card = rootVisualElement.Q<VisualElement>("SFP1Card");
            sfP1Name = rootVisualElement.Q<Label>("SFP1Name");
            sfP1Seed = rootVisualElement.Q<Label>("SFP1Seed");
            sfP1Initials = rootVisualElement.Q<Label>("SFP1Initials");
            sfP1Status = rootVisualElement.Q<Label>("SFP1Status");

            sfP2Card = rootVisualElement.Q<VisualElement>("SFP2Card");
            sfP2Name = rootVisualElement.Q<Label>("SFP2Name");
            sfP2Seed = rootVisualElement.Q<Label>("SFP2Seed");
            sfP2Initials = rootVisualElement.Q<Label>("SFP2Initials");
            sfP2Status = rootVisualElement.Q<Label>("SFP2Status");

            championViewContainer = rootVisualElement.Q<VisualElement>("ChampionViewContainer");
            championStageName = rootVisualElement.Q<Label>("ChampionStageName");

            videoViewContainer = rootVisualElement.Q<VisualElement>("VideoViewContainer");
            videoDisplayElement = rootVisualElement.Q<VisualElement>("VideoDisplayElement");

            refereeCharacter = rootVisualElement.Q<VisualElement>("RefereeCharacter");
            stingerOverlay = rootVisualElement.Q<VisualElement>("StingerOverlay");
        }

        private Coroutine refereeAnimCoroutine;
        private VisualElement activeFinalBanner;

        private void TriggerRefereeEntranceAnimation(bool show)
        {
            if (refereeCharacter == null) return;

            if (refereeAnimCoroutine != null)
            {
                StopCoroutine(refereeAnimCoroutine);
            }

            if (show)
            {
                refereeCharacter.style.display = DisplayStyle.Flex;
                refereeAnimCoroutine = StartCoroutine(AnimateRefereeEntranceRoutine());
            }
            else
            {
                refereeCharacter.AddToClassList("referee-hidden");
            }
        }

        private IEnumerator AnimateRefereeEntranceRoutine()
        {
            if (refereeCharacter == null) yield break;

            // 1. Force exit state off-screen right first
            refereeCharacter.AddToClassList("referee-hidden");

            // 2. Wait for character to slide completely off-screen (0.35s exit transition)
            yield return new WaitForSeconds(0.35f);

            // 3. NOW THAT HE IS OFF-SCREEN: Select next pose & horizontal flip!
            SelectRandomRefereePose();

            // 4. Brief pause off-screen before entrance (0.1f)
            yield return new WaitForSeconds(0.1f);

            // 5. Slide in smoothly from right to left with new pose!
            if (refereeCharacter != null)
            {
                refereeCharacter.RemoveFromClassList("referee-hidden");
            }
            refereeAnimCoroutine = null;
        }

        private void SelectRandomRefereePose()
        {
            if (refereeCharacter == null) return;

            // Pick next index different from last (0: Rock, 1: Paper, 2: Scissors)
            int nextPoseIndex = lastRefereePoseIndex;
            int attempts = 0;
            while (nextPoseIndex == lastRefereePoseIndex && attempts < 20)
            {
                nextPoseIndex = UnityEngine.Random.Range(0, 3);
                attempts++;
            }
            lastRefereePoseIndex = nextPoseIndex;

            Sprite selectedSprite = null;
            switch (nextPoseIndex)
            {
                case 0: selectedSprite = refereeRockSprite; break;
                case 1: selectedSprite = refereePaperSprite; break;
                case 2: selectedSprite = refereeScissorsSprite; break;
            }

            if (selectedSprite != null)
            {
                refereeCharacter.style.backgroundImage = new StyleBackground(selectedSprite);
            }

            // Horizontal Flip (50% chance for random flip)
            if (allowRefereeHorizontalFlip)
            {
                float flipX = UnityEngine.Random.value > 0.5f ? -1f : 1f;
                refereeCharacter.style.scale = new StyleScale(new Scale(new Vector2(flipX, 1f)));
            }
            else
            {
                refereeCharacter.style.scale = new StyleScale(new Scale(new Vector2(1f, 1f)));
            }
        }

        private void TriggerBracketEntranceAnimation()
        {
            if (bracketContainer == null) return;
            bracketContainer.AddToClassList("bracket-hidden");
            if (activeFinalBanner != null) activeFinalBanner.AddToClassList("final-banner-hidden");
            StartCoroutine(AnimateBracketEntranceRoutine());
        }

        private IEnumerator AnimateBracketEntranceRoutine()
        {
            yield return new WaitForSeconds(0.1f);
            if (bracketContainer != null)
            {
                bracketContainer.RemoveFromClassList("bracket-hidden");
            }
            if (activeFinalBanner != null)
            {
                activeFinalBanner.RemoveFromClassList("final-banner-hidden");
            }
        }

        private void OnDisplayViewChanged(DisplayViewType newView)
        {
            // If a stinger transition is ALREADY playing (e.g. initiated by UI button), do not launch a duplicate stinger!
            if (isStingerPlaying)
            {
                RefreshCurrentView();
                return;
            }

            StingerAnimationData targetStinger = GetStingerToPlay();
            if (useStingerTransitions && targetStinger != null && targetStinger.FrameCount > 0)
            {
                PlayStingerTransition(() => RefreshCurrentView(), targetStinger);
            }
            else
            {
                RefreshCurrentView();
            }
        }

        /// <summary>
        /// Plays a Stinger transition animation, calling the callback at the cut point.
        /// </summary>
        public void PlayStingerTransition(Action onCutPoint, StingerAnimationData stinger = null)
        {
            if (stinger == null) stinger = GetStingerToPlay();
            if (stinger == null || stinger.FrameCount == 0 || stingerOverlay == null)
            {
                onCutPoint?.Invoke();
                return;
            }

            if (activeStingerCoroutine != null)
            {
                StopCoroutine(activeStingerCoroutine);
            }

            activeStingerCoroutine = StartCoroutine(StingerRoutine(stinger, onCutPoint));
        }

        private IEnumerator StingerRoutine(StingerAnimationData stinger, Action onCutPoint)
        {
            isStingerPlaying = true;

            // Randomize horizontal and vertical flip for animation variety
            if (randomizeStingerFlip)
            {
                float scaleX = UnityEngine.Random.value > 0.5f ? -1f : 1f;
                float scaleY = UnityEngine.Random.value > 0.5f ? -1f : 1f;
                stingerOverlay.style.scale = new StyleScale(new Scale(new Vector2(scaleX, scaleY)));
            }
            else
            {
                stingerOverlay.style.scale = new StyleScale(new Scale(new Vector2(1f, 1f)));
            }

            stingerOverlay.RemoveFromClassList("display2-hidden");

            if (stinger.stingerSound != null)
            {
                AudioSource audioSource = GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(stinger.stingerSound);
                }
            }

            float frameDuration = stinger.fps > 0 ? 1f / stinger.fps : 1f / 30f;
            int totalFrames = stinger.FrameCount;
            int cutFrame = Mathf.Clamp(stinger.cutFrameIndex, 0, totalFrames - 1);

            // Phase 1: Play frames up to the cut frame (screen covered)
            for (int i = 0; i <= cutFrame; i++)
            {
                stingerOverlay.style.backgroundImage = Background.FromSprite(stinger.GetFrame(i));
                yield return new WaitForSeconds(frameDuration);
            }

            // Phase 2: Perform the view refresh behind the opaque stinger frame
            onCutPoint?.Invoke();

            // Phase 3: Play remaining frames (screen reveals)
            for (int i = cutFrame + 1; i < totalFrames; i++)
            {
                stingerOverlay.style.backgroundImage = Background.FromSprite(stinger.GetFrame(i));
                yield return new WaitForSeconds(frameDuration);
            }

            // Phase 4: Clean up
            stingerOverlay.AddToClassList("display2-hidden");
            stingerOverlay.style.backgroundImage = null;
            stingerOverlay.style.scale = new StyleScale(new Scale(new Vector2(1f, 1f)));
            activeStingerCoroutine = null;
            isStingerPlaying = false;
        }

        private void OnSelectedRoundChanged(int roundIndex)
        {
            if (tournamentModel != null && (tournamentModel.CurrentDisplayView == DisplayViewType.SingleRound || tournamentModel.CurrentDisplayView == DisplayViewType.ChromaSingleRound))
            {
                RenderSingleRoundView(roundIndex);
            }
        }

        private void OnSelectedMatchChanged(Match match)
        {
            if (tournamentModel != null)
            {
                if (tournamentModel.CurrentDisplayView == DisplayViewType.Combat)
                {
                    RenderCombatView(match);
                }
                else if (tournamentModel.CurrentDisplayView == DisplayViewType.ChromaCombatSF)
                {
                    RenderCombatSFView(match);
                }
            }
        }

        public void RefreshCurrentView()
        {
            if (tournamentModel == null) return;

            // Stop video playback if navigating away from video view
            if (tournamentModel.CurrentDisplayView != DisplayViewType.NouArbitreVideo)
            {
                if (videoPlayer != null && videoPlayer.isPlaying)
                {
                    videoPlayer.Stop();
                }
                if (videoDisplayElement != null)
                {
                    videoDisplayElement.style.display = DisplayStyle.None;
                }
            }

            // Hide all views first
            SetContainerVisible(bracketViewContainer, false);
            SetContainerVisible(singleRoundViewContainer, false);
            SetContainerVisible(combatViewContainer, false);
            SetContainerVisible(combatSFViewContainer, false);
            SetContainerVisible(championViewContainer, false);
            SetContainerVisible(videoViewContainer, false);

            bool isChroma = IsChromaView(tournamentModel.CurrentDisplayView);
            UpdateBackgroundChromaMode(isChroma);
            TriggerRefereeEntranceAnimation(!isChroma && tournamentModel.CurrentDisplayView != DisplayViewType.NouArbitreVideo);

            switch (tournamentModel.CurrentDisplayView)
            {
                case DisplayViewType.Bracket:
                case DisplayViewType.ChromaBracket:
                    SetContainerVisible(bracketViewContainer, true);
                    if (headerSubtitleLabel != null) headerSubtitleLabel.text = isChroma ? "VISTA QUADRE (CROMA KEY)" : "VISTA DEL QUADRE";
                    RenderCleanBracket();
                    TriggerBracketEntranceAnimation();
                    break;

                case DisplayViewType.SingleRound:
                case DisplayViewType.ChromaSingleRound:
                    SetContainerVisible(singleRoundViewContainer, true);
                    if (headerSubtitleLabel != null) headerSubtitleLabel.text = isChroma ? "VISTA RONDA FOCUS (CROMA KEY)" : "VISTA RONDA FOCUS";
                    RenderSingleRoundView(tournamentModel.SelectedRoundIndex);
                    break;

                case DisplayViewType.Combat:
                    SetContainerVisible(combatViewContainer, true);
                    if (headerSubtitleLabel != null) headerSubtitleLabel.text = "ENFRONTAMENT DIRECTE";
                    Match activeMatch = tournamentModel.SelectedCombatMatch ?? tournamentModel.GetNextPlayableMatch();
                    RenderCombatView(activeMatch);
                    TriggerCombatEntranceAnimation();
                    break;

                case DisplayViewType.ChromaCombatSF:
                    SetContainerVisible(combatSFViewContainer, true);
                    if (headerSubtitleLabel != null) headerSubtitleLabel.text = "STREET FIGHTER OVERLAY (CROMA)";
                    Match sfMatch = tournamentModel.SelectedCombatMatch ?? tournamentModel.GetNextPlayableMatch();
                    RenderCombatSFView(sfMatch);
                    break;

                case DisplayViewType.ChampionPodium:
                case DisplayViewType.ChromaChampion:
                    SetContainerVisible(championViewContainer, true);
                    if (headerSubtitleLabel != null) headerSubtitleLabel.text = isChroma ? "PODI DE VICTÒRIA (CROMA KEY)" : "PODI DE VICTÒRIA";
                    RenderChampionView();
                    break;

                case DisplayViewType.ChromaCleanFeed:
                    if (headerSubtitleLabel != null) headerSubtitleLabel.text = "FONS VERD PUR (CLEAN FEED)";
                    break;

                case DisplayViewType.NouArbitreVideo:
                    SetContainerVisible(videoViewContainer, true);
                    if (headerSubtitleLabel != null) headerSubtitleLabel.text = "REPRODUINT VÍDEO: NOU ÀRBITRE";
                    if (videoPlayer == null || !videoPlayer.isPlaying)
                    {
                        ExecuteVideoPlayback(currentPlaybackStartTime);
                    }
                    break;
            }
        }

        public void PlayNouArbitreVideo(bool useStinger, float startTime)
        {
            if (!useStinger && startTime == 0f && repVideoStartTime > 0f)
            {
                startTime = repVideoStartTime;
            }
            else if (useStinger && startTime == 0f && goVideoStartTime > 0f)
            {
                startTime = goVideoStartTime;
            }

            currentPlaybackStartTime = startTime;
            Action startPlaybackAction = () =>
            {
                bool wasStingerPlaying = isStingerPlaying;
                if (!useStinger) isStingerPlaying = true; // Temporarily prevent OnDisplayViewChanged from triggering a Stinger transition

                try
                {
                    if (tournamentModel != null && tournamentModel.CurrentDisplayView != DisplayViewType.NouArbitreVideo)
                    {
                        tournamentModel.SetDisplayView(DisplayViewType.NouArbitreVideo);
                    }
                    else
                    {
                        SetContainerVisible(videoViewContainer, true);
                        if (headerSubtitleLabel != null) headerSubtitleLabel.text = "REPRODUINT VÍDEO: NOU ÀRBITRE";
                    }

                    ExecuteVideoPlayback(currentPlaybackStartTime);
                }
                finally
                {
                    if (!useStinger) isStingerPlaying = wasStingerPlaying;
                }
            };

            if (useStinger && useStingerTransitions)
            {
                PlayStingerTransition(startPlaybackAction);
            }
            else
            {
                startPlaybackAction();
            }
        }

        private void ExecuteVideoPlayback(float startTime)
        {
            if (videoPlayer == null)
            {
                videoPlayer = GetComponent<UnityEngine.Video.VideoPlayer>();
                if (videoPlayer == null)
                {
                    videoPlayer = gameObject.AddComponent<UnityEngine.Video.VideoPlayer>();
                }
            }

#if UNITY_EDITOR
            if (nouArbitreClip == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("", new[] { "Assets/Videos" });
                foreach (string guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>(path);
                    if (clip != null)
                    {
                        nouArbitreClip = clip;
                        break;
                    }
                }
            }
#endif

            if (nouArbitreClip != null)
            {
                videoPlayer.clip = nouArbitreClip;
            }

            if (videoPlayer.clip == null)
            {
                Debug.LogWarning("[Janken] VideoClip NOU ARBITRE no trobat ni assignat! Revisa que hi hagi un fitxer de vídeo a Assets/Videos/NouArbitre/ o assigna'l a l'Inspector de Unity.");
                return;
            }

            int w = Screen.width > 0 ? Screen.width : 1920;
            int h = Screen.height > 0 ? Screen.height : 1080;

            if (videoRenderTexture == null || !videoRenderTexture.IsCreated() || videoRenderTexture.width != w || videoRenderTexture.height != h)
            {
                if (videoRenderTexture != null)
                {
                    videoRenderTexture.Release();
                    Destroy(videoRenderTexture);
                }
                videoRenderTexture = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
                videoRenderTexture.Create();
            }

            videoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoRenderTexture;

            if (videoDisplayElement != null)
            {
                videoDisplayElement.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(videoRenderTexture));
                videoDisplayElement.style.display = DisplayStyle.Flex;
                videoDisplayElement.style.width = new Length(100, LengthUnit.Percent);
                videoDisplayElement.style.height = new Length(100, LengthUnit.Percent);
            }

            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.Direct;
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.SetDirectAudioVolume(0, 1.0f);

            // Normalize start time: Unity VideoPlayer.time is in SECONDS.
            // If entered in milliseconds (e.g., 30000 for 30s), convert ms to seconds.
            double clipLength = videoPlayer.clip != null ? videoPlayer.clip.length : (videoPlayer.length > 0 ? videoPlayer.length : 0);
            if (clipLength > 0)
            {
                if (startTime > clipLength && startTime / 1000.0 <= clipLength)
                {
                    startTime = (float)(startTime / 1000.0);
                }
                else if (startTime >= clipLength)
                {
                    startTime = (float)Math.Max(0, clipLength - 0.5);
                }
            }
            else if (startTime >= 1000f)
            {
                startTime /= 1000f;
            }

            float finalSeekTime = startTime;

            videoPlayer.Pause();

            if (!videoPlayer.isPrepared)
            {
                UnityEngine.Video.VideoPlayer.EventHandler preparedHandler = null;
                preparedHandler = (vp) =>
                {
                    vp.prepareCompleted -= preparedHandler;
                    vp.time = finalSeekTime;
                    vp.Play();
                };
                videoPlayer.prepareCompleted += preparedHandler;
                videoPlayer.Prepare();
            }
            else
            {
                videoPlayer.time = finalSeekTime;
                videoPlayer.Play();
            }
        }

        private void UpdateBackgroundChromaMode(bool isChroma)
        {
            if (display2Root == null) return;

            if (isChroma)
            {
                display2Root.AddToClassList("display2-chroma-bg");
                display2Root.style.backgroundColor = new StyleColor(new Color(0f, 1f, 0f, 1f)); // Pure Chroma Key Green #00FF00
            }
            else
            {
                display2Root.RemoveFromClassList("display2-chroma-bg");
                display2Root.style.backgroundColor = new StyleColor(new Color(0.035f, 0.051f, 0.086f, 1f)); // Dark background #090d16
            }
        }

        private bool IsChromaView(DisplayViewType view)
        {
            return view == DisplayViewType.ChromaBracket ||
                   view == DisplayViewType.ChromaSingleRound ||
                   view == DisplayViewType.ChromaCombatSF ||
                   view == DisplayViewType.ChromaChampion ||
                   view == DisplayViewType.ChromaCleanFeed;
        }

        private void SetContainerVisible(VisualElement element, bool visible)
        {
            if (element == null) return;
            if (visible)
            {
                element.RemoveFromClassList("display2-hidden");
                element.style.display = DisplayStyle.Flex;
            }
            else
            {
                element.AddToClassList("display2-hidden");
                element.style.display = DisplayStyle.None;
            }
        }

        #region Render Combat VS Screen

        private void RenderCombatView(Match match)
        {
            if (match == null)
            {
                if (combatRoundTitle != null) combatRoundTitle.text = "SENSE COMBAT ACTIU";
                if (player1Name != null) player1Name.text = "---";
                if (player2Name != null) player2Name.text = "---";
                if (combatStatusBanner != null) combatStatusBanner.text = "Afaga jugadors o selecciona un combat per començar.";
                return;
            }

            if (combatRoundTitle != null)
            {
                string roundName = tournamentModel.GetRoundTitle(match.roundIndex);
                combatRoundTitle.text = $"{roundName} - COMBAT #{match.matchIndex + 1}";
            }

            // Player 1
            if (match.player1 != null)
            {
                if (player1Name != null) player1Name.text = match.player1.name;
                if (player1Seed != null) player1Seed.text = $"#{match.player1.seed} SEED";
                if (player1Initials != null) player1Initials.text = GetInitials(match.player1.name);
            }
            else
            {
                if (player1Name != null) player1Name.text = "PER DETERMINAR";
                if (player1Seed != null) player1Seed.text = "-";
                if (player1Initials != null) player1Initials.text = "?";
            }

            // Player 2
            if (match.player2 != null)
            {
                if (player2Name != null) player2Name.text = match.player2.name;
                if (player2Seed != null) player2Seed.text = $"#{match.player2.seed} SEED";
                if (player2Initials != null) player2Initials.text = GetInitials(match.player2.name);
            }
            else
            {
                if (player2Name != null) player2Name.text = "PER DETERMINAR";
                if (player2Seed != null) player2Seed.text = "-";
                if (player2Initials != null) player2Initials.text = "?";
            }

            // Reset Card States
            if (player1Card != null)
            {
                player1Card.RemoveFromClassList("combat-card-winner");
                player1Card.RemoveFromClassList("combat-card-loser");
            }
            if (player2Card != null)
            {
                player2Card.RemoveFromClassList("combat-card-winner");
                player2Card.RemoveFromClassList("combat-card-loser");
            }

            // Match Status
            if (match.isCompleted && match.winner != null)
            {
                if (match.winner == match.player1)
                {
                    if (player1Card != null) player1Card.AddToClassList("combat-card-winner");
                    if (player2Card != null) player2Card.AddToClassList("combat-card-loser");
                    if (player1Status != null) player1Status.text = "⭐";
                    if (player2Status != null) player2Status.text = "";
                    if (combatStatusBanner != null) combatStatusBanner.text = $"🏆 GUANYADOR/A: {match.player1.name.ToUpper()}";
                }
                else
                {
                    if (player2Card != null) player2Card.AddToClassList("combat-card-winner");
                    if (player1Card != null) player1Card.AddToClassList("combat-card-loser");
                    if (player2Status != null) player2Status.text = "⭐";
                    if (player1Status != null) player1Status.text = "";
                    if (combatStatusBanner != null) combatStatusBanner.text = $"🏆 GUANYADOR/A: {match.player2.name.ToUpper()}";
                }
            }
            else
            {
                if (player1Status != null) player1Status.text = "";
                if (player2Status != null) player2Status.text = "";
                if (combatStatusBanner != null) combatStatusBanner.text = "⚡ COMBAT EN CURS - PREPARATS PER LLUITAR!";
            }
        }

        protected void TriggerCombatEntranceAnimation()
        {
            if (combatAnimCoroutine != null) StopCoroutine(combatAnimCoroutine);
            combatAnimCoroutine = StartCoroutine(AnimateCombatEntranceRoutine());
        }

        protected IEnumerator AnimateCombatEntranceRoutine()
        {
            if (vsEmblemContainer != null) vsEmblemContainer.AddToClassList("vs-emblem-hidden");
            if (player1Card != null) player1Card.AddToClassList("p1-card-hidden");
            if (player2Card != null) player2Card.AddToClassList("p2-card-hidden");

            yield return new WaitForEndOfFrame();

            // Step 1: VS emblem scales up 0 -> 100% with bounce
            if (vsEmblemContainer != null) vsEmblemContainer.RemoveFromClassList("vs-emblem-hidden");

            // Step 2: 0.25s delay then player cards slide in from top/bottom or left/right
            yield return new WaitForSeconds(0.25f);

            if (player1Card != null) player1Card.RemoveFromClassList("p1-card-hidden");
            if (player2Card != null) player2Card.RemoveFromClassList("p2-card-hidden");

            combatAnimCoroutine = null;
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            string clean = name.Trim();
            string[] parts = clean.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            }
            return clean.Substring(0, Math.Min(2, clean.Length)).ToUpper();
        }

        #endregion

        #region Render Champion View

        private void RenderChampionView()
        {
            if (championStageName != null)
            {
                championStageName.text = tournamentModel.Champion != null ? tournamentModel.Champion.name.ToUpper() : "EN CURS";
            }
        }

        #endregion

        #region Render Clean Bracket (No Buttons)

        private void RenderCleanBracket()
        {
            activeFinalBanner = null;
            if (bracketContainer == null) return;
            bracketContainer.Clear();

            if (!tournamentModel.IsActive || tournamentModel.Rounds.Count == 0)
            {
                var emptyLabel = new Label("No hi ha cap torneig actiu en aquest moment.");
                emptyLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
                emptyLabel.style.fontSize = 24;
                emptyLabel.style.marginTop = 60;
                bracketContainer.Add(emptyLabel);
                return;
            }

            int totalRounds = tournamentModel.Rounds.Count;

            bool isDisplay3 = (this is Display3Controller);

            // Dynamic Sizing based on tournament size (4, 8, or 16 players) - Large prominent fonts
            float HEADER_HEIGHT = isDisplay3 ? 56f : 50f;
            float HEADER_MARGIN = 14f;
            float CARD_HEIGHT = isDisplay3 ? 120f : 104f;
            float BASE_GAP = isDisplay3 ? 14f : 8f;
            float COLUMN_WIDTH = isDisplay3 ? 255f : 230f;
            float SLOT_FONT_SIZE = isDisplay3 ? 28f : 24f;
            float HEADER_FONT_SIZE = isDisplay3 ? 26f : 22f;

            if (totalRounds <= 2) // 4 Players (2 Rounds) -> Extra large!
            {
                HEADER_HEIGHT = isDisplay3 ? 66f : 62f;
                HEADER_MARGIN = 24f;
                CARD_HEIGHT = isDisplay3 ? 175f : 160f;
                BASE_GAP = 36f;
                COLUMN_WIDTH = isDisplay3 ? 370f : 340f;
                SLOT_FONT_SIZE = isDisplay3 ? 42f : 36f;
                HEADER_FONT_SIZE = isDisplay3 ? 36f : 32f;
            }
            else if (totalRounds == 3) // 8 Players (3 Rounds) -> Large!
            {
                HEADER_HEIGHT = isDisplay3 ? 60f : 56f;
                HEADER_MARGIN = 20f;
                CARD_HEIGHT = isDisplay3 ? 150f : 138f;
                BASE_GAP = 28f;
                COLUMN_WIDTH = isDisplay3 ? 320f : 290f;
                SLOT_FONT_SIZE = isDisplay3 ? 34f : 30f;
                HEADER_FONT_SIZE = isDisplay3 ? 30f : 26f;
            }

            float HEADER_TOTAL = HEADER_HEIGHT + HEADER_MARGIN;
            float SLOT_HEIGHT = CARD_HEIGHT + BASE_GAP;

            for (int r = 0; r < totalRounds; r++)
            {
                var roundMatches = tournamentModel.Rounds[r];
                var roundColumn = new VisualElement();
                roundColumn.AddToClassList("round-column");
                roundColumn.style.width = COLUMN_WIDTH;
                roundColumn.style.alignItems = Align.Center;

                bool isFinalRound = (r == totalRounds - 1);

                var roundHeader = new Label(tournamentModel.GetRoundTitle(r));
                roundHeader.AddToClassList("round-header");
                roundHeader.style.height = HEADER_HEIGHT;
                roundHeader.style.marginBottom = HEADER_MARGIN;
                roundHeader.style.fontSize = HEADER_FONT_SIZE;
                roundHeader.style.width = Math.Max(COLUMN_WIDTH + 26f, 240f);
                if (isFinalRound)
                {
                    roundHeader.style.visibility = Visibility.Hidden;
                }
                roundColumn.Add(roundHeader);

                float multiplier = (float)Math.Pow(2, r);
                float topOffset = (SLOT_HEIGHT * (multiplier - 1f)) / 2f;
                float gap = SLOT_HEIGHT * (multiplier - 1f) + BASE_GAP;

                if (isFinalRound)
                {
                    var finalBanner = new VisualElement();
                    finalBanner.AddToClassList("final-banner-crown");
                    finalBanner.AddToClassList("final-banner-hidden");
                    finalBanner.style.marginTop = topOffset - 85f;
                    activeFinalBanner = finalBanner;
                    roundColumn.Add(finalBanner);
                }

                for (int m = 0; m < roundMatches.Count; m++)
                {
                    Match match = roundMatches[m];
                    VisualElement matchCard = CreateCleanMatchCard(match, CARD_HEIGHT, SLOT_FONT_SIZE);
                    matchCard.style.width = new Length(100, LengthUnit.Percent);

                    float marginTop = (m == 0) ? (isFinalRound ? 0f : topOffset) : gap;
                    matchCard.style.marginTop = marginTop;

                    roundColumn.Add(matchCard);
                }

                bracketContainer.Add(roundColumn);

                // Connecting Lines
                if (r < totalRounds - 1)
                {
                    var connectorColumn = new VisualElement();
                    connectorColumn.AddToClassList("connector-column");
                    connectorColumn.style.width = 24f;

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
                        upperArm.style.width = 12f;
                        upperArm.style.height = 4f;

                        var lowerArm = new VisualElement();
                        lowerArm.AddToClassList("connector-arm");
                        if (match2Active) lowerArm.AddToClassList("connector-arm-active");
                        lowerArm.style.top = yLower - 1f;
                        lowerArm.style.left = 0f;
                        lowerArm.style.width = 12f;
                        lowerArm.style.height = 4f;

                        var verticalBar = new VisualElement();
                        verticalBar.AddToClassList("connector-arm");
                        if (match1Active || match2Active) verticalBar.AddToClassList("connector-arm-active");
                        verticalBar.style.top = yUpper - 1f;
                        verticalBar.style.left = 10f;
                        verticalBar.style.width = 4f;
                        verticalBar.style.height = yLower - yUpper + 4f;

                        var outArm = new VisualElement();
                        outArm.AddToClassList("connector-arm");
                        if (match1Active || match2Active) outArm.AddToClassList("connector-arm-active");
                        outArm.style.top = yMid - 1f;
                        outArm.style.left = 12f;
                        outArm.style.width = 12f;
                        outArm.style.height = 4f;

                        connectorColumn.Add(upperArm);
                        connectorColumn.Add(lowerArm);
                        connectorColumn.Add(verticalBar);
                        connectorColumn.Add(outArm);
                    }

                    bracketContainer.Add(connectorColumn);
                }
                else
                {
                    // Champion Card in final round (Only on Display 2 horizontal)
                    if (tournamentModel.Champion != null && !isDisplay3)
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

        private VisualElement CreateCleanMatchCard(Match match, float cardHeight, float fontSize)
        {
            var card = new VisualElement();
            card.AddToClassList("match-card");
            card.style.height = cardHeight;

            if (match.isCompleted)
            {
                card.AddToClassList("match-card-completed");
            }

            VisualElement slot1 = CreateCleanPlayerSlot(match, match.player1, match.winner == match.player1 && match.player1 != null, cardHeight / 2f, fontSize);
            VisualElement slot2 = CreateCleanPlayerSlot(match, match.player2, match.winner == match.player2 && match.player2 != null, cardHeight / 2f, fontSize);

            card.Add(slot1);
            card.Add(slot2);

            return card;
        }

        private VisualElement CreateCleanPlayerSlot(Match match, Player player, bool isWinner, float slotHeight, float fontSize)
        {
            var slot = new VisualElement();
            slot.AddToClassList("match-slot");
            slot.style.height = slotHeight;

            if (player == null)
            {
                slot.AddToClassList("match-slot-empty");
                var emptyLabel = new Label("---");
                emptyLabel.AddToClassList("slot-player-name");
                emptyLabel.style.fontSize = fontSize;
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

            var nameLabel = new Label(player.name);
            nameLabel.AddToClassList("slot-player-name");
            nameLabel.style.fontSize = fontSize;
            slot.Add(nameLabel);

            if (isWinner)
            {
                var winBadge = new Label("⭐");
                winBadge.style.color = new StyleColor(new Color(0.96f, 0.62f, 0.04f));
                winBadge.style.fontSize = fontSize + 2;
                slot.Add(winBadge);
            }

            return slot;
        }

        #endregion

        #region Render Single Round View

        private void RenderSingleRoundView(int roundIndex)
        {
            if (singleRoundContainer == null) return;
            singleRoundContainer.Clear();

            if (!tournamentModel.IsActive || tournamentModel.Rounds.Count == 0)
            {
                var emptyLabel = new Label("No hi ha cap torneig actiu.");
                emptyLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
                emptyLabel.style.fontSize = 24;
                emptyLabel.style.marginTop = 40;
                singleRoundContainer.Add(emptyLabel);
                return;
            }

            roundIndex = Mathf.Clamp(roundIndex, 0, tournamentModel.Rounds.Count - 1);
            string title = tournamentModel.GetRoundTitle(roundIndex);

            if (singleRoundHeaderTitle != null)
            {
                singleRoundHeaderTitle.text = title;
            }

            var roundMatches = tournamentModel.Rounds[roundIndex];
            bool isDisplay3 = (this is Display3Controller);

            // Dynamic Sizing so ALL matches fit without vertical scrolling!
            float CARD_HEIGHT = 100f;
            float BASE_GAP = 16f;
            float SLOT_FONT_SIZE = 18f;
            float CARD_WIDTH = 340f;

            int matchCount = roundMatches.Count;

            if (matchCount >= 8)
            {
                // 8 Matches (e.g. Vuitens for 16 players) -> 2 columns of 4 matches!
                singleRoundContainer.style.flexDirection = FlexDirection.Row;
                singleRoundContainer.style.justifyContent = Justify.FlexStart;
                singleRoundContainer.style.alignItems = Align.FlexStart;

                CARD_HEIGHT = isDisplay3 ? 130f : 115f;
                BASE_GAP = 16f;
                SLOT_FONT_SIZE = isDisplay3 ? 32f : 28f;
                CARD_WIDTH = isDisplay3 ? 440f : 410f;

                var col1 = new VisualElement();
                col1.style.marginRight = 28f;
                var col2 = new VisualElement();

                for (int m = 0; m < matchCount; m++)
                {
                    Match match = roundMatches[m];
                    VisualElement matchCard = CreateCleanMatchCard(match, CARD_HEIGHT, SLOT_FONT_SIZE);
                    matchCard.style.marginBottom = BASE_GAP;
                    matchCard.style.width = CARD_WIDTH;

                    if (m < 4) col1.Add(matchCard);
                    else col2.Add(matchCard);
                }

                singleRoundContainer.Add(col1);
                singleRoundContainer.Add(col2);
            }
            else
            {
                // 1 column (4, 2, or 1 matches)
                singleRoundContainer.style.flexDirection = FlexDirection.Column;
                singleRoundContainer.style.alignItems = Align.FlexStart;

                if (matchCount == 4)
                {
                    CARD_HEIGHT = isDisplay3 ? 165f : 145f;
                    BASE_GAP = 20f;
                    SLOT_FONT_SIZE = isDisplay3 ? 40f : 34f;
                    CARD_WIDTH = isDisplay3 ? 530f : 490f;
                }
                else if (matchCount == 2)
                {
                    CARD_HEIGHT = isDisplay3 ? 210f : 185f;
                    BASE_GAP = 30f;
                    SLOT_FONT_SIZE = isDisplay3 ? 48f : 40f;
                    CARD_WIDTH = isDisplay3 ? 600f : 550f;
                }
                else // 1 Match (Final)
                {
                    CARD_HEIGHT = isDisplay3 ? 260f : 240f;
                    BASE_GAP = 0f;
                    SLOT_FONT_SIZE = isDisplay3 ? 56f : 48f;
                    CARD_WIDTH = isDisplay3 ? 680f : 640f;
                }

                var roundColumn = new VisualElement();
                roundColumn.style.alignItems = Align.FlexStart;

                for (int m = 0; m < matchCount; m++)
                {
                    Match match = roundMatches[m];
                    VisualElement matchCard = CreateCleanMatchCard(match, CARD_HEIGHT, SLOT_FONT_SIZE);
                    matchCard.style.marginBottom = BASE_GAP;
                    matchCard.style.width = CARD_WIDTH;
                    roundColumn.Add(matchCard);
                }

                singleRoundContainer.Add(roundColumn);
            }
        }

        #endregion

        #region Render Street Fighter Top Bar HUD View (Chroma Overlay)

        private void RenderCombatSFView(Match match)
        {
            if (match == null)
            {
                if (sfRoundTitle != null) sfRoundTitle.text = "SENSE COMBAT";
                if (sfP1Name != null) sfP1Name.text = "---";
                if (sfP2Name != null) sfP2Name.text = "---";
                return;
            }

            if (sfRoundTitle != null)
            {
                string roundName = tournamentModel.GetRoundTitle(match.roundIndex);
                sfRoundTitle.text = $"{roundName} • COMBAT #{match.matchIndex + 1}";
            }

            // Player 1
            if (match.player1 != null)
            {
                if (sfP1Name != null) sfP1Name.text = match.player1.name;
                if (sfP1Initials != null) sfP1Initials.text = GetInitials(match.player1.name);
            }
            else
            {
                if (sfP1Name != null) sfP1Name.text = "PER DETERMINAR";
                if (sfP1Initials != null) sfP1Initials.text = "?";
            }

            // Player 2
            if (match.player2 != null)
            {
                if (sfP2Name != null) sfP2Name.text = match.player2.name;
                if (sfP2Initials != null) sfP2Initials.text = GetInitials(match.player2.name);
            }
            else
            {
                if (sfP2Name != null) sfP2Name.text = "PER DETERMINAR";
                if (sfP2Initials != null) sfP2Initials.text = "?";
            }

            // Reset HUD status classes
            if (sfP1Card != null)
            {
                sfP1Card.RemoveFromClassList("sf-card-winner");
                sfP1Card.RemoveFromClassList("sf-card-loser");
            }
            if (sfP2Card != null)
            {
                sfP2Card.RemoveFromClassList("sf-card-winner");
                sfP2Card.RemoveFromClassList("sf-card-loser");
            }

            if (match.isCompleted && match.winner != null)
            {
                if (match.winner == match.player1)
                {
                    if (sfP1Card != null) sfP1Card.AddToClassList("sf-card-winner");
                    if (sfP2Card != null) sfP2Card.AddToClassList("sf-card-loser");
                }
                else
                {
                    if (sfP2Card != null) sfP2Card.AddToClassList("sf-card-winner");
                    if (sfP1Card != null) sfP1Card.AddToClassList("sf-card-loser");
                }
            }
        }

        #endregion
    }
}

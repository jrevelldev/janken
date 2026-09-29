using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Janken.Tournament;

namespace Janken.Controllers
{
    [RequireComponent(typeof(UIDocument))]
    public class Display2Controller : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement rootVisualElement;
        private VisualElement display2Root;

        private Label headerTitleLabel;
        private Label headerSubtitleLabel;

        private VisualElement bracketViewContainer;
        private ScrollView bracketScrollView;
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

        private Label combatStatusBanner;

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

        private TournamentModel tournamentModel;

        private void Start()
        {
            ActivateSecondaryDisplay();
            InitializeUI();
        }

        private void ActivateSecondaryDisplay()
        {
            if (Display.displays.Length > 1)
            {
                Display.displays[1].Activate();
                Debug.Log("[Janken MultiDisplay] Display 2 activat amb èxit.");
            }
            else
            {
                Debug.Log("[Janken MultiDisplay] Només s'ha detectat 1 display físic. Display 2 s'està provant en mode secundari.");
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
                tournamentModel.OnTournamentUpdated += RefreshCurrentView;
                tournamentModel.OnDisplayViewChanged += OnDisplayViewChanged;
                tournamentModel.OnSelectedMatchChanged += OnSelectedMatchChanged;
                tournamentModel.OnSelectedRoundChanged += OnSelectedRoundChanged;

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

        public void InitializeUI()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            rootVisualElement = uiDocument.rootVisualElement;
            if (rootVisualElement == null) return;

            BindUIElements();
        }

        private void BindUIElements()
        {
            display2Root = rootVisualElement.Q<VisualElement>("Display2Root");
            headerTitleLabel = rootVisualElement.Q<Label>("Display2HeaderTitle");
            headerSubtitleLabel = rootVisualElement.Q<Label>("Display2HeaderSubtitle");

            bracketViewContainer = rootVisualElement.Q<VisualElement>("BracketViewContainer");
            bracketScrollView = rootVisualElement.Q<ScrollView>("Display2BracketScrollView");
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
        }

        private void OnDisplayViewChanged(DisplayViewType newView)
        {
            RefreshCurrentView();
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

            // Hide all views first
            SetContainerVisible(bracketViewContainer, false);
            SetContainerVisible(singleRoundViewContainer, false);
            SetContainerVisible(combatViewContainer, false);
            SetContainerVisible(combatSFViewContainer, false);
            SetContainerVisible(championViewContainer, false);

            bool isChroma = IsChromaView(tournamentModel.CurrentDisplayView);
            UpdateBackgroundChromaMode(isChroma);

            switch (tournamentModel.CurrentDisplayView)
            {
                case DisplayViewType.Bracket:
                case DisplayViewType.ChromaBracket:
                    SetContainerVisible(bracketViewContainer, true);
                    if (headerSubtitleLabel != null) headerSubtitleLabel.text = isChroma ? "VISTA QUADRE (CROMA KEY)" : "VISTA DEL QUADRE";
                    RenderCleanBracket();
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
                    if (headerSubtitleLabel != null) headerSubtitleLabel.text = isChroma ? "PODI CAMPIÓ (CROMA KEY)" : "PODI DE CAMPIÓ";
                    RenderChampionView();
                    break;
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
                   view == DisplayViewType.ChromaChampion;
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
                    if (player1Status != null) player1Status.text = "🏆 VICTÒRIA!";
                    if (player2Status != null) player2Status.text = "ELIMINAT/ADA";
                    if (combatStatusBanner != null) combatStatusBanner.text = $"🏆 GUANYADOR/A: {match.player1.name.ToUpper()}";
                }
                else
                {
                    if (player2Card != null) player2Card.AddToClassList("combat-card-winner");
                    if (player1Card != null) player1Card.AddToClassList("combat-card-loser");
                    if (player2Status != null) player2Status.text = "🏆 VICTÒRIA!";
                    if (player1Status != null) player1Status.text = "ELIMINAT/ADA";
                    if (combatStatusBanner != null) combatStatusBanner.text = $"🏆 GUANYADOR/A: {match.player2.name.ToUpper()}";
                }
            }
            else
            {
                if (player1Status != null) player1Status.text = "COMBATENT";
                if (player2Status != null) player2Status.text = "COMBATENT";
                if (combatStatusBanner != null) combatStatusBanner.text = "⚡ COMBAT EN CURS - PREPARATS PER LLUITAR!";
            }
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

            // Dynamic Sizing based on tournament size (4, 8, or 16 players)
            float HEADER_HEIGHT = 36f;
            float HEADER_MARGIN = 20f;
            float CARD_HEIGHT = 80f;
            float BASE_GAP = 24f;
            float COLUMN_WIDTH = 260f;
            float SLOT_FONT_SIZE = 14f;
            float HEADER_FONT_SIZE = 14f;

            if (totalRounds <= 2) // 4 Players (2 Rounds) -> Make much larger!
            {
                HEADER_HEIGHT = 50f;
                HEADER_MARGIN = 26f;
                CARD_HEIGHT = 130f;
                BASE_GAP = 44f;
                COLUMN_WIDTH = 360f;
                SLOT_FONT_SIZE = 22f;
                HEADER_FONT_SIZE = 20f;
            }
            else if (totalRounds == 3) // 8 Players (3 Rounds) -> Make much larger!
            {
                HEADER_HEIGHT = 50f;
                HEADER_MARGIN = 26f;
                CARD_HEIGHT = 125f;
                BASE_GAP = 36f;
                COLUMN_WIDTH = 340f;
                SLOT_FONT_SIZE = 20f;
                HEADER_FONT_SIZE = 18f;
            }

            float HEADER_TOTAL = HEADER_HEIGHT + HEADER_MARGIN;
            float SLOT_HEIGHT = CARD_HEIGHT + BASE_GAP;

            for (int r = 0; r < totalRounds; r++)
            {
                var roundMatches = tournamentModel.Rounds[r];
                var roundColumn = new VisualElement();
                roundColumn.AddToClassList("round-column");
                roundColumn.style.width = COLUMN_WIDTH;

                var roundHeader = new Label(tournamentModel.GetRoundTitle(r));
                roundHeader.AddToClassList("round-header");
                roundHeader.style.height = HEADER_HEIGHT;
                roundHeader.style.marginBottom = HEADER_MARGIN;
                roundHeader.style.fontSize = HEADER_FONT_SIZE;
                roundColumn.Add(roundHeader);

                float multiplier = (float)Math.Pow(2, r);
                float topOffset = (SLOT_HEIGHT * (multiplier - 1f)) / 2f;
                float gap = SLOT_HEIGHT * (multiplier - 1f) + BASE_GAP;

                for (int m = 0; m < roundMatches.Count; m++)
                {
                    Match match = roundMatches[m];
                    VisualElement matchCard = CreateCleanMatchCard(match, CARD_HEIGHT, SLOT_FONT_SIZE);

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

            // Dynamic Sizing so ALL matches fit without vertical scrolling!
            float CARD_HEIGHT = 100f;
            float BASE_GAP = 16f;
            float SLOT_FONT_SIZE = 18f;
            float CARD_WIDTH = 380f;

            int matchCount = roundMatches.Count;

            if (matchCount >= 8)
            {
                // 8 Matches (e.g. Vuitens for 16 players) -> 2 columns of 4 matches!
                singleRoundContainer.style.flexDirection = FlexDirection.Row;
                singleRoundContainer.style.justifyContent = Justify.Center;

                CARD_HEIGHT = 70f;
                BASE_GAP = 10f;
                SLOT_FONT_SIZE = 14f;
                CARD_WIDTH = 340f;

                var col1 = new VisualElement();
                col1.style.marginRight = 20f;
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
                singleRoundContainer.style.alignItems = Align.Center;

                if (matchCount == 4)
                {
                    CARD_HEIGHT = 85f;
                    BASE_GAP = 14f;
                    SLOT_FONT_SIZE = 16f;
                    CARD_WIDTH = 420f;
                }
                else if (matchCount == 2)
                {
                    CARD_HEIGHT = 120f;
                    BASE_GAP = 24f;
                    SLOT_FONT_SIZE = 22f;
                    CARD_WIDTH = 480f;
                }
                else // 1 Match (Final)
                {
                    CARD_HEIGHT = 180f;
                    BASE_GAP = 0f;
                    SLOT_FONT_SIZE = 28f;
                    CARD_WIDTH = 550f;
                }

                var roundColumn = new VisualElement();
                roundColumn.style.alignItems = Align.Center;

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

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Janken.Tournament
{
    [Serializable]
    public class MatchSaveData
    {
        public string id;
        public int roundIndex;
        public int matchIndex;
        public string player1Id;
        public string player2Id;
        public int score1;
        public int score2;
        public string winnerId;
        public bool isCompleted;
        public bool isBye;
        public string nextMatchId;
        public int nextMatchSlot;
    }

    [Serializable]
    public class TournamentSaveData
    {
        public string tournamentName;
        public List<Player> players = new List<Player>();
        public List<MatchSaveData> matches = new List<MatchSaveData>();
        public string championId;
        public bool isActive;
    }

    public class TournamentModel
    {
        private const string SAVE_KEY = "Janken_Tournament_SaveData";

        public string TournamentName { get; set; } = "Gran Final Janken";
        public List<Player> Players { get; private set; } = new List<Player>();
        public List<List<Match>> Rounds { get; private set; } = new List<List<Match>>();
        public Player Champion { get; private set; }
        public bool IsActive { get; private set; } = false;

        public TournamentModel()
        {
            if (!LoadState())
            {
                SetDefaultPlayers(8);
                GenerateBracket();
            }
        }

        public void UpdatePlayerName(string playerId, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName)) return;
            var p = Players.FirstOrDefault(x => x.id == playerId);
            if (p != null)
            {
                p.name = newName.Trim();
                SaveState();
            }
        }

        public void ResetToDefaults(int targetCount = 8)
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
            Players.Clear();
            SetDefaultPlayers(targetCount);
            GenerateBracket();
        }

        public void SetDefaultPlayers(int targetCount = 8)
        {
            string[] sampleNames = {
                "Goku", "Vegeta", "Naruto", "Sasuke",
                "Luffy", "Zoro", "Gon", "Killua",
                "Saitama", "Genos", "Deku", "Bakugo",
                "Tanjiro", "Nezuko", "Jotaro", "Dio"
            };

            // If we have existing players, adjust list size while preserving custom names
            if (Players.Count > 0)
            {
                if (Players.Count > targetCount)
                {
                    Players = Players.Take(targetCount).ToList();
                }
                else
                {
                    int added = 0;
                    for (int i = 0; i < sampleNames.Length && Players.Count < targetCount; i++)
                    {
                        string candidate = sampleNames[i];
                        if (!Players.Any(p => p.name.Equals(candidate, StringComparison.OrdinalIgnoreCase)))
                        {
                            Players.Add(new Player(candidate, Players.Count + 1));
                            added++;
                        }
                    }
                    while (Players.Count < targetCount)
                    {
                        Players.Add(new Player($"Jugador {Players.Count + 1}", Players.Count + 1));
                    }
                }
            }
            else
            {
                for (int i = 0; i < targetCount; i++)
                {
                    string name = (i < sampleNames.Length) ? sampleNames[i] : $"Jugador {i + 1}";
                    Players.Add(new Player(name, i + 1));
                }
            }

            ReindexSeeds();
            SaveState();
        }

        public void AddPlayer(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            Players.Add(new Player(name.Trim(), Players.Count + 1));
            ReindexSeeds();
            SaveState();
        }

        public void RemovePlayer(string playerId)
        {
            Players.RemoveAll(p => p.id == playerId);
            ReindexSeeds();
            SaveState();
        }

        public void ShufflePlayers()
        {
            System.Random rng = new System.Random();
            int n = Players.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var value = Players[k];
                Players[k] = Players[n];
                Players[n] = value;
            }
            ReindexSeeds();
            SaveState();
        }

        private void ReindexSeeds()
        {
            for (int i = 0; i < Players.Count; i++)
            {
                Players[i].seed = i + 1;
            }
        }

        public void GenerateBracket()
        {
            Champion = null;
            Rounds.Clear();

            if (Players.Count < 2)
            {
                IsActive = false;
                SaveState();
                return;
            }

            int numPlayers = Players.Count;
            int bracketSize = 1;
            while (bracketSize < numPlayers)
            {
                bracketSize *= 2;
            }

            int totalRounds = (int)Math.Log(bracketSize, 2);

            for (int r = 0; r < totalRounds; r++)
            {
                int matchesInRound = bracketSize / (int)Math.Pow(2, r + 1);
                var roundMatches = new List<Match>();
                for (int m = 0; m < matchesInRound; m++)
                {
                    roundMatches.Add(new Match(r, m));
                }
                Rounds.Add(roundMatches);
            }

            for (int r = 0; r < totalRounds - 1; r++)
            {
                for (int m = 0; m < Rounds[r].Count; m++)
                {
                    Match currentMatch = Rounds[r][m];
                    int nextMatchIndex = m / 2;
                    int slot = (m % 2 == 0) ? 1 : 2;

                    Match nextMatch = Rounds[r + 1][nextMatchIndex];
                    currentMatch.nextMatchId = nextMatch.id;
                    currentMatch.nextMatchSlot = slot;
                }
            }

            List<Player> seededList = new List<Player>(Players);
            while (seededList.Count < bracketSize)
            {
                seededList.Add(null);
            }

            List<Match> round0 = Rounds[0];
            for (int i = 0; i < round0.Count; i++)
            {
                round0[i].player1 = seededList[i * 2];
                round0[i].player2 = seededList[i * 2 + 1];

                if (round0[i].player1 != null && round0[i].player2 == null)
                {
                    round0[i].isBye = true;
                    DeclareMatchWinner(round0[i], round0[i].player1, save: false);
                }
                else if (round0[i].player1 == null && round0[i].player2 != null)
                {
                    round0[i].isBye = true;
                    DeclareMatchWinner(round0[i], round0[i].player2, save: false);
                }
            }

            IsActive = true;
            SaveState();
        }

        public Match FindMatchById(string matchId)
        {
            foreach (var round in Rounds)
            {
                foreach (var m in round)
                {
                    if (m.id == matchId) return m;
                }
            }
            return null;
        }

        public void ToggleOrDeclareWinner(Match match, Player player)
        {
            if (match == null || player == null) return;

            if (match.winner == player)
            {
                // Unmark / Undo winner
                UndoMatchWinner(match);
            }
            else
            {
                // Set winner
                DeclareMatchWinner(match, player);
            }
        }

        public void DeclareMatchWinner(Match match, Player winner, bool save = true)
        {
            if (match == null) return;

            match.SetWinner(winner);

            if (!string.IsNullOrEmpty(match.nextMatchId))
            {
                Match nextMatch = FindMatchById(match.nextMatchId);
                if (nextMatch != null)
                {
                    if (match.nextMatchSlot == 1)
                    {
                        nextMatch.player1 = winner;
                    }
                    else
                    {
                        nextMatch.player2 = winner;
                    }

                    if (nextMatch.winner != null && nextMatch.winner != winner)
                    {
                        ResetDownstreamMatches(nextMatch);
                    }
                }
            }
            else
            {
                if (match.roundIndex == Rounds.Count - 1)
                {
                    Champion = winner;
                }
            }

            if (save) SaveState();
        }

        public void UndoMatchWinner(Match match)
        {
            if (match == null) return;
            ResetDownstreamMatches(match);
            SaveState();
        }

        private void ResetDownstreamMatches(Match match)
        {
            match.SetWinner(null);
            match.score1 = 0;
            match.score2 = 0;
            Champion = null;

            if (!string.IsNullOrEmpty(match.nextMatchId))
            {
                Match nextMatch = FindMatchById(match.nextMatchId);
                if (nextMatch != null)
                {
                    if (match.nextMatchSlot == 1) nextMatch.player1 = null;
                    else nextMatch.player2 = null;

                    ResetDownstreamMatches(nextMatch);
                }
            }
        }

        public string GetRoundTitle(int roundIndex)
        {
            int totalRounds = Rounds.Count;
            if (roundIndex == totalRounds - 1) return "FINAL";
            if (roundIndex == totalRounds - 2) return "SEMIFINALS";
            if (roundIndex == totalRounds - 3) return "QUARTS DE FINAL";
            if (roundIndex == totalRounds - 4) return "VUITENS DE FINAL";
            return $"RONDA {roundIndex + 1}";
        }

        #region Persistence Save & Load

        public void SaveState()
        {
            var saveData = new TournamentSaveData
            {
                tournamentName = TournamentName,
                players = Players,
                championId = Champion != null ? Champion.id : "",
                isActive = IsActive
            };

            foreach (var round in Rounds)
            {
                foreach (var m in round)
                {
                    saveData.matches.Add(new MatchSaveData
                    {
                        id = m.id,
                        roundIndex = m.roundIndex,
                        matchIndex = m.matchIndex,
                        player1Id = m.player1 != null ? m.player1.id : "",
                        player2Id = m.player2 != null ? m.player2.id : "",
                        score1 = m.score1,
                        score2 = m.score2,
                        winnerId = m.winner != null ? m.winner.id : "",
                        isCompleted = m.isCompleted,
                        isBye = m.isBye,
                        nextMatchId = m.nextMatchId,
                        nextMatchSlot = m.nextMatchSlot
                    });
                }
            }

            string json = JsonUtility.ToJson(saveData, false);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        public bool LoadState()
        {
            if (!PlayerPrefs.HasKey(SAVE_KEY)) return false;

            try
            {
                string json = PlayerPrefs.GetString(SAVE_KEY);
                var saveData = JsonUtility.FromJson<TournamentSaveData>(json);

                if (saveData == null || saveData.players == null || saveData.players.Count == 0) return false;

                TournamentName = saveData.tournamentName;
                Players = saveData.players;
                IsActive = saveData.isActive;

                var playerMap = Players.ToDictionary(p => p.id, p => p);

                if (!string.IsNullOrEmpty(saveData.championId) && playerMap.ContainsKey(saveData.championId))
                {
                    Champion = playerMap[saveData.championId];
                }
                else
                {
                    Champion = null;
                }

                if (saveData.matches != null && saveData.matches.Count > 0)
                {
                    Rounds.Clear();
                    int maxRound = saveData.matches.Max(m => m.roundIndex);

                    for (int r = 0; r <= maxRound; r++)
                    {
                        Rounds.Add(new List<Match>());
                    }

                    foreach (var mData in saveData.matches)
                    {
                        var match = new Match(mData.roundIndex, mData.matchIndex)
                        {
                            id = mData.id,
                            score1 = mData.score1,
                            score2 = mData.score2,
                            isCompleted = mData.isCompleted,
                            isBye = mData.isBye,
                            nextMatchId = mData.nextMatchId,
                            nextMatchSlot = mData.nextMatchSlot
                        };

                        if (!string.IsNullOrEmpty(mData.player1Id) && playerMap.ContainsKey(mData.player1Id))
                            match.player1 = playerMap[mData.player1Id];

                        if (!string.IsNullOrEmpty(mData.player2Id) && playerMap.ContainsKey(mData.player2Id))
                            match.player2 = playerMap[mData.player2Id];

                        if (!string.IsNullOrEmpty(mData.winnerId) && playerMap.ContainsKey(mData.winnerId))
                            match.SetWinner(playerMap[mData.winnerId]);

                        Rounds[mData.roundIndex].Add(match);
                    }

                    // Sort matches by matchIndex in each round
                    for (int r = 0; r < Rounds.Count; r++)
                    {
                        Rounds[r] = Rounds[r].OrderBy(m => m.matchIndex).ToList();
                    }
                }
                else
                {
                    GenerateBracket();
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Janken] Error carregant estat del torneig: {ex.Message}");
                return false;
            }
        }

        public void ResetAllData()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
            SetDefaultPlayers(8);
            GenerateBracket();
        }

        #endregion
    }
}

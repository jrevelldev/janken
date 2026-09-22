using System;

namespace Janken.Tournament
{
    [Serializable]
    public class Match
    {
        public string id;
        public int roundIndex;
        public int matchIndex;
        
        public Player player1;
        public Player player2;
        
        public int score1;
        public int score2;
        
        public Player winner;
        public bool isCompleted;
        public bool isBye;

        public string nextMatchId;
        public int nextMatchSlot; // 1 for Player1, 2 for Player2

        public Match(int roundIndex, int matchIndex)
        {
            this.id = $"R{roundIndex}_M{matchIndex}_{Guid.NewGuid().ToString().Substring(0, 5)}";
            this.roundIndex = roundIndex;
            this.matchIndex = matchIndex;
            this.score1 = 0;
            this.score2 = 0;
            this.isCompleted = false;
            this.isBye = false;
        }

        public void SetWinner(Player p)
        {
            winner = p;
            isCompleted = (p != null);
        }

        public bool CanPlay => player1 != null && player2 != null;
    }
}

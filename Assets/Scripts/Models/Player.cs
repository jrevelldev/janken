using System;

namespace Janken.Tournament
{
    [Serializable]
    public class Player
    {
        public string id;
        public string name;
        public int seed;

        public Player(string name, int seed = 0)
        {
            this.id = Guid.NewGuid().ToString();
            this.name = string.IsNullOrWhiteSpace(name) ? "Jugador" : name;
            this.seed = seed;
        }

        public Player(string id, string name, int seed)
        {
            this.id = id;
            this.name = name;
            this.seed = seed;
        }
    }
}

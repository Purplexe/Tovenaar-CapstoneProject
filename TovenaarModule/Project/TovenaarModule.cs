using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Core;

namespace TovenaarModule
{
    public class GameModule
    {
        // -----------------------------
        //   DTOs MATCHING YOUR UNITY
        // -----------------------------
        public class CardInstance
        {
            public string instanceId;
            public string cardUid;
            public int attack;
            public int health;
        }

        public class Lane
        {
            public CardInstance monster;
            public CardInstance building;
        }

        public class PlayerState
        {
            public string playerId;
            public int ziel;
            public List<CardInstance> hand;
            public List<Lane> board;
            public int graveyardCount;
        }

        public class GameState
        {
            public string gameId;
            public string currentPlayer;
            public int turn;
            public int localPlayerIndex;
            public List<PlayerState> players;
        }

        // -----------------------------
        //   HOST GAME ENDPOINT
        // -----------------------------
        [CloudCodeFunction("HostGame")]
        public async Task<GameState> HostGame(
            CloudCodeContext ctx,
            string gameCode)
        {
            var playerId = ctx.PlayerId;

            var state = new GameState
            {
                gameId = gameCode,
                currentPlayer = playerId,
                turn = 1,
                localPlayerIndex = 0,
                players = new List<PlayerState>
                {
                    new PlayerState
                    {
                        playerId = playerId,
                        ziel = 1,
                        hand = new List<CardInstance>(),
                        board = new List<Lane>
                        {
                            new Lane(), new Lane(), new Lane(), new Lane()
                        },
                        graveyardCount = 0
                    }
                }
            };

            return state;
        }
    }
}

using DE.Cefoot.BattleShips.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DE.Cefoot.BattleShips.Server
{
    public static class RunningGame
    {

        public static event EventHandler PlayerFinishedEvent;
        private static Random _rand = new Random();
        public static List<Player> LeaderBoard { get; } = new List<Player>();
        private static Dictionary<Player, PlayerInfo> PlayerInfos { get; } = new Dictionary<Player, PlayerInfo>();
        public static Field Field
        { get; }
        = new Field
        {
            TipNewTime = /*Production*/ TimeSpan.FromHours(1),/*Debug TimeSpan.FromMinutes(1),*/
            FieldHeight = 10 + _rand.Next(10),
            FieldWidth = 10 + _rand.Next(5),
            TipCount = 10 + _rand.Next(50),//default = max 1 tip / min
            ShipCount = _rand.Next(2, 20)
        };
        public static bool[,] Ships { get; set; } = new bool[Field.FieldWidth, Field.FieldHeight];
        public static int ShipParts { get; internal set; } = 0;
        public static PlayerInfo GetInfo(Player player)
        {
            if (!PlayerInfos.ContainsKey(player))
            {
                PlayerInfos[player] = new PlayerInfo
                {
                    LastTipReset = DateTime.Now
                };
                PlayerInfos[player].GameFinishedEvent += (sender, obj) => Player_GameFinishedEvent(player);
            }
            return PlayerInfos[player];
        }

        private static void Player_GameFinishedEvent(Player player)
        {
            player.TipCount = GetInfo(player).Tips.Count;
            if (PlayerFinishedEvent != null)
            {
                PlayerFinishedEvent(player, new EventArgs());
            }
        }
    }


}

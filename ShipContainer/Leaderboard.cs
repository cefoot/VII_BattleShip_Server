using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DE.Cefoot.BattleShips.Data
{
    [Serializable]
    public class Leaderboard : IGameData
    {
        public DataKind DataKind
        {
            get
            {
                return DataKind.Leaderboard;
            }
        }

        public Player[] PlayerList { get; set; }
    }
}

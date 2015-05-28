using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DE.Cefoot.BattleShips.Data
{
    [Serializable]
    public abstract class GameInfo : IGameData
    {
        public int TipCount { get; set; }
        public TimeSpan TipNewTime { get; set; }
        public int UsedTipCount { get; set; }
        public bool GameFinished { get; set; }
        public abstract DataKind DataKind { get; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DE.Cefoot.BattleShips.Data
{
    [Serializable]
    public class Tip : IGameData
    {
        public DataKind DataKind
        {
            get
            {
                return DataKind.Tip;
            }
        }

        public int PosX { get; set; }
        public int PosY { get; set; }
    }
}

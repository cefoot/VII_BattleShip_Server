using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Cefoot.BattleShips.Data
{
    [Serializable]
    public class Info : GameInfo
    {
        public override DataKind DataKind
        {
            get
            {
                return DataKind.Info;
            }
        }
    }
}

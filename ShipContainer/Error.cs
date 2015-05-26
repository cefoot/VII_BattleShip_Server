using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Cefoot.BattleShips.Data
{
    [Serializable]
    public class Error : IGameData
    {
        public DataKind DataKind
        {
            get
            {
                return DataKind.Error;
            }
        }

        public Exception Exc { get; set; }
    }
}

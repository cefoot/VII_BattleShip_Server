using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Cefoot.BattleShips.Data
{
    public interface IGameData{
        DataKind DataKind { get; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DE.Cefoot.BattleShips.Data
{
    [Serializable]
    public class Field : GameInfo
    {
        public override DataKind DataKind
        {
            get
            {
                return DataKind.Field;
            }
        }
        public int FieldWidth { get; set; }
        public int FieldHeight { get; set; }
        public int ShipCount { get; set; }
    }
}

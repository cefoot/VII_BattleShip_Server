using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Cefoot.BattleShips.Data
{
    [Serializable]
    public class Player
    {
        public string Name { get; set; }
        public int TipCount { get; set; }
        public int Points { get; set; }
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (!(obj is Player)) return false;
            var ply = obj as Player;
            if (ply.Name == null) return false;
            return ply.Name.Equals(Name);
        }
    }
}

using DE.Cefoot.BattleShips.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DE.Cefoot.BattleShips.Server
{
    public class PlayerInfo
    {
        public DateTime LastTipReset { get; set; }
        public List<Tip> Tips { get; } = new List<Tip>();
        public int TipsSinceLastReset { get; set; } = 0;
        private bool _finished = false;
        public event EventHandler GameFinishedEvent;
        public bool GameFinished
        {
            get
            {
                return _finished;
            }
            set
            {

                if (value && GameFinishedEvent != null)
                {
                    GameFinishedEvent(this, new EventArgs());
                }
                _finished = value;
            }
        }
    }
}

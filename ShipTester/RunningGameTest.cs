using DE.Cefoot.BattleShips.Data;
using DE.Cefoot.BattleShips.Server;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DE.Cefoot.BattleShips.Test
{
    [TestFixture]
    public class RunningGameTest
    {
        [Test]
        public void TestPlayer_Finished_Event()
        {
            var ply = new Player
            {
                Name = "Dummy"
            };
            var eventThrown = false;
            RunningGame.PlayerFinishedEvent += (send, args) => eventThrown = true;
            RunningGame.GetInfo(ply).GameFinished = true;
            Assert.That(eventThrown);
        }
    }
}

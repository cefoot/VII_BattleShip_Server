using DE.Cefoot.BattleShips.Server;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DE.Cefoot.BattleShips.Test
{
    [TestFixture]
    public class ServerTest
    {
        [Test]
        public void StartStop()
        {
            var serverExe = Assembly.GetAssembly(typeof(RunningGame)).CodeBase;
            var server = Process.Start(serverExe);
            Thread.Sleep(500);
            Assert.That(server.Threads.Count, Is.EqualTo(13));
            SendKeys.SendWait("x");
            //Thread.Sleep(500);
            server.WaitForExit(500);
            Assert.That(server.HasExited, Is.True);

        }
    }
}

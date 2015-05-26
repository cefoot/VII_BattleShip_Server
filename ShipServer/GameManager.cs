using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using De.Cefoot.BattleShips.Data;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace De.Cefoot.BattleShips.Server
{
    public class GameManager
    {

        Dictionary<NetworkStream, Player> ConnectedPlayer = new Dictionary<NetworkStream, Player>();
        Dictionary<NetworkStream, Socket> ConnectedSockets = new Dictionary<NetworkStream, Socket>();

        private static bool _running = false;
        private static bool _newPlayerFinished = false;

        public static class RunningGame
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
            private static Random _rand = new Random();
            public static List<Player> LeaderBoard { get; } = new List<Player>();
            private static Dictionary<Player, PlayerInfo> PlayerInfos { get; } = new Dictionary<Player, PlayerInfo>();
            public static Field Field
            { get; }
            = new Field
            {
                TipNewTime = /*Production TimeSpan.FromHours(1),*//*Debug*/TimeSpan.FromMinutes(1),
                FieldHeight = 10 + _rand.Next(10),
                FieldWidth = 10 + _rand.Next(5),
                TipCount = 10 + _rand.Next(50),//default = max 1 tip / min
                ShipCount = _rand.Next(2, 20)
            };
            public static bool[,] Ships { get; set; } = new bool[Field.FieldWidth, Field.FieldHeight];
            public static int ShipParts { get; internal set; } = 0;
            public static PlayerInfo GetInfo(Player player)
            {
                if (!PlayerInfos.ContainsKey(player))
                {
                    PlayerInfos[player] = new PlayerInfo
                    {
                        LastTipReset = DateTime.Now
                    };
                    PlayerInfos[player].GameFinishedEvent += (sender, obj) => Player_GameFinishedEvent(player);
                }
                return PlayerInfos[player];
            }
        }

        private static void Player_GameFinishedEvent(Player player)
        {
            player.TipCount = RunningGame.GetInfo(player).Tips.Count;
            _newPlayerFinished = true;
        }

        internal void Start()
        {
            LoadLeaderBoard();
            _running = true;
            PositionShips();
            ThreadPool.QueueUserWorkItem(ReceiveData);
            ThreadPool.QueueUserWorkItem(StatusInfo);
        }

        private void LoadLeaderBoard()
        {
            if (File.Exists("leaderBoard.info"))
                using (Stream stream = File.OpenRead("leaderBoard.info"))
                {
                    RunningGame.LeaderBoard.AddRange((stream.ReadData() as Leaderboard).PlayerList);
                }
        }

        private void CreateNewRound(Field field, Player player)
        {
            GivePoints();
            field.TipNewTime = RunningGame.Field.TipNewTime;
            RunningGame.Field.FieldWidth = field.FieldWidth;
            RunningGame.Field.FieldHeight = field.FieldHeight;
            RunningGame.Field.ShipCount = field.ShipCount;
            RunningGame.Field.TipCount = field.TipCount;
            RunningGame.Ships = new bool[RunningGame.Field.FieldWidth, RunningGame.Field.FieldHeight];
            try
            {
                PositionShips();
            }
            catch (ArgumentException e)
            {
                ConnectedPlayer.First(itm => itm.Value == player).Key.SendData(new Error { Exc = e });
            }
            ConnectedPlayer.Values.ToList().ForEach(pl =>
            {
                var playInfo = RunningGame.GetInfo(pl);
                playInfo.GameFinished = false;
                playInfo.LastTipReset = DateTime.Now;
                playInfo.TipsSinceLastReset = 0;
                var plStream = ConnectedPlayer.First(e => e.Value == pl).Key;
                RegisterPlayer(pl.Name, plStream, ConnectedSockets[plStream]);
            }
            );
        }

        private void GivePoints()
        {
            var plyList = RunningGame.LeaderBoard.Where(ply => ply.TipCount > 0).OrderBy(ply => ply.TipCount).Select((ply, idx) => new { Player = ply, Index = idx });
            foreach (var current in plyList)
            {
                current.Player.Points += Math.Max(0, 10 - current.Index * 2);
                current.Player.TipCount = 0;
            }
        }

        private void PositionShips()
        {
            var rand = new Random();
            var width = RunningGame.Field.FieldWidth;
            var height = RunningGame.Field.FieldHeight;
            var missingShips = 0;
            RunningGame.ShipParts = 0;
            for (int i = 0; i < RunningGame.Field.ShipCount; i++)
            {
                if (!PositionShip(rand, width, height))
                {
                    missingShips++;
                }
            }
            if (RunningGame.Field.ShipCount - missingShips < 2)
            {
                throw new ArgumentException("Es konnten nicht genug Schiffe plaziert werden");
            }
            RunningGame.Field.ShipCount -= missingShips;
        }

        private bool PositionShip(Random rand, int width, int height)
        {
            var shipLength = rand.Next(2, 6);
            var shipInPosition = false;
            var tries = 0;
            var shipsBackup = (bool[,])RunningGame.Ships.Clone();
            while (!shipInPosition && (tries++) < 10)
            {
                var startX = rand.Next(width);
                var startY = rand.Next(height);
                var direction = rand.Next(4);//0 = up;1, 2, 3 => clockwise
                var posX = startX;
                var posY = startY;
                for (int i = 0; i < shipLength; i++)
                {
                    shipInPosition = false;
                    if (posX >= width) break;
                    if (posY >= height) break;
                    if (posX < 0) break;
                    if (posY < 0) break;
                    if (RunningGame.Ships[posX, posY]) break;
                    shipInPosition = true;
                    RunningGame.Ships[posX, posY] = true;
                    posX += direction == 1 ? 1 : direction == 3 ? -1 : 0;
                    posY += direction == 0 ? 1 : direction == 2 ? -1 : 0;
                }
                if (!shipInPosition)
                {
                    RunningGame.Ships = (bool[,])shipsBackup.Clone();
                }
                else
                {
                    RunningGame.ShipParts += shipLength;
                }
            }
            return shipInPosition;
        }

        private void StatusInfo(object state)
        {
            if (!_running) return;
            Leaderboard leaderboard = null;
            if (_newPlayerFinished)
            {
                leaderboard = new Leaderboard { PlayerList = RunningGame.LeaderBoard.ToArray() };
                _newPlayerFinished = false;
            }
            foreach (var current in ConnectedPlayer.AsParallel())
            {
                var info = new Info();
                AddBasicInfo(info, current.Value);
                if (!CheckConnection(current.Key))
                {
                    continue;
                }
                try
                {
                    if (leaderboard != null)
                    {
                        current.Key.SendData(leaderboard);
                    }
                    current.Key.SendData(info);
                }
                catch (IOException e)
                {//Lost Connection?
                    CheckConnection(current.Key);
                }
            }
            //resync lists
            var keysToRemove = ConnectedPlayer.Keys.Except(ConnectedSockets.Keys);
            keysToRemove.ToList().ForEach(k => ConnectedPlayer.Remove(k));
            Thread.Sleep(1000);
            ThreadPool.QueueUserWorkItem(StatusInfo);
        }

        private bool CheckConnection(NetworkStream key)
        {
            if (!ConnectedSockets[key].Connected)
            {
                ConnectedSockets.Remove(key);
                return false;
            }
            return true;
        }

        private void ReceiveData(object state)
        {
            if (!_running) return;
            foreach (var stream in ConnectedPlayer.Keys.AsEnumerable())
            {
                if (stream.DataAvailable)
                {
                    var data = stream.ReadData();
                    ThreadPool.QueueUserWorkItem(HandleData, new object[] { data, ConnectedPlayer[stream] });
                }
            }
            Thread.Sleep(100);
            ThreadPool.QueueUserWorkItem(ReceiveData);
        }

        private void HandleData(object state)
        {
            var data = ((object[])state)[0] as IGameData;
            var player = ((object[])state)[1] as Player;
            switch (data.DataKind)
            {
                case DataKind.Tip:
                    var tip = data as Tip;
                    HandlePlayerTip(tip, player);
                    break;
                case DataKind.Field:
                    var field = data as Field;
                    CreateNewRound(field, player);
                    break;
            }
        }

        private void HandlePlayerTip(Tip tip, Player player)
        {
            var playInfo = RunningGame.GetInfo(player);
            if (playInfo.GameFinished || playInfo.TipsSinceLastReset++ >= RunningGame.Field.TipCount)
            {
                var info = new Info();
                AddBasicInfo(info, player);
                ConnectedPlayer.First(ent => ent.Value.Equals(player)).Key.SendData(info);
                return;
            }
            playInfo.Tips.Add(tip);
            playInfo.GameFinished = playInfo.Tips.Count(t => RunningGame.Ships[t.PosX, t.PosY]) == RunningGame.ShipParts;
            var answer = new TipAnswer
            {
                Hit = RunningGame.Ships[tip.PosX, tip.PosY],
                OriginalTip = tip
            };
            AddBasicInfo(answer, player);
            ConnectedPlayer.First(ent => ent.Value.Equals(player)).Key.SendData(answer);
        }

        private void AddBasicInfo(GameInfo data, Player player)
        {
            var playInfo = RunningGame.GetInfo(player);
            data.GameFinished = playInfo.GameFinished;
            data.TipCount = RunningGame.Field.TipCount - playInfo.TipsSinceLastReset;
            data.TipNewTime = RunningGame.Field.TipNewTime - (DateTime.Now - playInfo.LastTipReset);
            if (data.TipNewTime.TotalSeconds < 1)
            {
                playInfo.LastTipReset = DateTime.Now;
                playInfo.TipsSinceLastReset = 0;
            }
            data.UsedTipCount = playInfo.Tips.Count;
        }

        internal void RegisterPlayer(string playerName, NetworkStream stream, Socket socket)
        {
            var player = RunningGame.LeaderBoard.FirstOrDefault(ply => ply.Name == playerName);
            if (player == null)
            {
                player = new Player
                {
                    Name = playerName
                };
                RunningGame.LeaderBoard.Add(player);
            }
            ConnectedPlayer[stream] = player;
            ConnectedSockets[stream] = socket;
            var field = new Field
            {
                FieldHeight = RunningGame.Field.FieldHeight,
                FieldWidth = RunningGame.Field.FieldWidth,
                ShipCount = RunningGame.Field.ShipCount
            };
            AddBasicInfo(field, player);
            stream.SendData(field);
            Thread.Sleep(200);
            stream.SendData(new Leaderboard { PlayerList = RunningGame.LeaderBoard.ToArray() });
        }

        internal void Stop()
        {
            _running = false;
            RunningGame.LeaderBoard.ForEach(ply => ply.TipCount = 0);//reset current Round
            using (Stream stream = File.OpenWrite("leaderBoard.info"))
            {
                stream.SendData(new Leaderboard { PlayerList = RunningGame.LeaderBoard.ToArray() });
            }
        }
    }
}

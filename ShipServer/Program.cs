using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using DE.Cefoot.BattleShips.Data;
using System.Text;
using System.Threading.Tasks;

namespace DE.Cefoot.BattleShips.Server
{
    class Program
    {
        private static GameManager manager = new GameManager();
        private static List<Socket> connectedSockets = new List<Socket>();

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            var listeners = new List<TcpListener>();
            try
            {//listen an all network interfaces
                manager.Start();
                StartListener(listeners);
                ConsoleKeyInfo key = new ConsoleKeyInfo();
                do
                {
                    PrintInfo(key);
                    key = Console.ReadKey();
                } while (ShouldContinue(key));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.ReadKey();
            }
            finally
            {
                manager.Stop();
                connectedSockets.ForEach(sock => sock.Close());
                listeners.ForEach(lis => lis.Stop());
                Console.WriteLine("Sockets geschlossen");
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("Schwerer Fehler:");
            Console.WriteLine(e.ExceptionObject.ToString());
        }

        private static void StartListener(List<TcpListener> listeners)
        {
            var listener = new TcpListener(IPAddress.Any, Properties.Settings.Default.ServerPort);
            listener.Start();
            listener.BeginAcceptSocket(ClientConnected, listener);
            Console.WriteLine("Server für Adresse:'{0}' und Port '{1}' gestartet.", IPAddress.Any, Properties.Settings.Default.ServerPort);
            listeners.Add(listener);
        }

        private static void PrintInfo(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                default:
                    Console.WriteLine("'X' zum Beenden des Servers.");
                    break;
            }
        }

        private static bool ShouldContinue(ConsoleKeyInfo key)
        {
            return key.Key != ConsoleKey.X;
        }

        private static void ClientConnected(IAsyncResult ar)
        {
            if (!ar.IsCompleted) return;
            var listener = ar.AsyncState as TcpListener;
            try
            {
                var socket = listener.EndAcceptSocket(ar);
                connectedSockets.Add(socket);
                listener.BeginAcceptSocket(ClientConnected, listener);
                var stream = new NetworkStream(socket);
                var playerName = stream.ReadName();
                PrintConnectInfo(socket, playerName);
                manager.RegisterPlayer(playerName, stream, socket);
            }
            catch (ObjectDisposedException)
            {
                //Tritt auf, wenn der Server beendet Wird
            }
        }

        private static void PrintConnectInfo(Socket socket, string name)
        {
            var left = Console.CursorLeft;
            var top = Console.CursorTop;
            Console.SetCursorPosition(0, Console.WindowHeight - 2);
            Console.WriteLine("Neuer Client:{0}[{2}] ({1})", socket.RemoteEndPoint, DateTime.Now, name);
            Console.SetCursorPosition(left, top);
        }
    }
}

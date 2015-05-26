using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace De.Cefoot.BattleShips.Data
{
    public static class Extensions
    {
        private static BinaryFormatter _formatter = new BinaryFormatter();

        public static void SendData(this Stream stream, IGameData data)
        {
            _formatter.Serialize(stream, data);
        }

        public static void SendName(this Stream stream, string name)
        {
            _formatter.Serialize(stream, name);
        }

        public static IGameData ReadData(this Stream stream)
        {
            //System.Threading.Thread.Sleep(50);
            try
            {//using bufferdStream
                return (IGameData)_formatter.Deserialize(new BufferedStream(stream));
            }
            catch (SerializationException)
            {//fallback => try again (most of the time reading to early)
                return (IGameData)_formatter.Deserialize(new BufferedStream(stream));
            }
        }

        public static string ReadName(this Stream stream)
        {
            return _formatter.Deserialize(stream).ToString();
        }
    }
}

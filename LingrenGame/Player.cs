using Lidgren.Network;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using GameServerConsole;
using Microsoft.Xna.Framework.Graphics;

namespace LingrenGame
{


    public class GamePlayer
    {
        string playerID;
        public string gamerTag;
        public Vector2 position = new Vector2();
        NetClient _client;
        PlayerData _playerDataPacket;
        bool joined;

        public string ImageName = string.Empty;

        public GamePlayer(NetClient client, string GamerTag, string ImgName, string playerid, Vector2 StartPos)
        {
            // Created as a reult of a joined message
            position = StartPos;
            playerID = playerid;
            ImageName = ImgName;
            gamerTag = GamerTag;
            _client = client;

        }

        public GamePlayer(NetClient client, Guid playerid, string GamerTag, string ImgName, Vector2 StartPos)
        {

            position = StartPos;
            playerID = playerid.ToString();
            ImageName = ImgName;
            gamerTag = GamerTag;
            _client = client;

            // consruct a join player packet and serialise it
            _playerDataPacket = new PlayerData("Join", gamerTag, ImageName, PlayerID, StartPos.X, StartPos.Y);

            sendMessage(_playerDataPacket);

        }
        public PlayerData PlayerDataPacket
        {
            get
            {
                return _playerDataPacket;
            }

            set
            {
                _playerDataPacket = value;
                position.X = value.X;
                position.Y = value.Y;
            }
        }

        public string PlayerID
        {
            get
            {
                return playerID;
            }

            set
            {
                playerID = value;
            }
        }

        public Vector2 Position
        {
            get
            {
                return position;
            }

            set
            {
                position = value;
            }
        }

        public bool Joined
        {
            get
            {
                return joined;
            }

            set
            {
                joined = value;
            }
        }

        public void Move(Vector2 delta)
        {
            Position += delta;
            _playerDataPacket.X = Position.X;
            _playerDataPacket.Y = Position.Y;
            _playerDataPacket = new PlayerData("Move", gamerTag, ImageName, PlayerID, Position.X, Position.Y);
            sendMessage(_playerDataPacket);
        }

        private void sendMessage(PlayerData _playerDataPacket)
        {
            string json = JsonConvert.SerializeObject(_playerDataPacket);
            // construct the outgoing message
            NetOutgoingMessage sendMsg = _client.CreateMessage();
            sendMsg.Write(json);
            _client.SendMessage(sendMsg, NetDeliveryMethod.ReliableOrdered);
        }

        public void ChangePosition(Vector2 newPosition)
        {
            position = newPosition;

        }
    }
}

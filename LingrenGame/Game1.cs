﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Lidgren.Network;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using GameServerConsole;
using NSLoader;
using Utilities;

namespace LingrenGame
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        private NetPeerConfiguration ClientConfig;
        private NetClient client;
        private string InGameMessage = string.Empty;
        private SpriteFont font;
        GamePlayer thisPlayer;
        List<GamePlayer> OtherPlayers = new List<GamePlayer>();

        FadeTextManager fadeTxtMgr;

        Dictionary<string, Texture2D> playerTextures = new Dictionary<string, Texture2D>();

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            ClientConfig = new NetPeerConfiguration("myGame");
            //for the client
            ClientConfig.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
            client = new NetClient(ClientConfig);
            client.Start();
            IsMouseVisible = true;
            InGameMessage = "This Client has a unique id of " + client.UniqueIdentifier.ToString();
            // Note Named parameters for more readable code
            //client.Connect(host: "127.0.0.1", port: 12345);
            //search in local network at port 50001
            client.DiscoverLocalPeers(12346);
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("GameFont");
            this.Services.AddService(font);
            this.Services.AddService(spriteBatch);

            fadeTxtMgr = new FadeTextManager(this);
            //new FadeText(this, Vector2.Zero, "HELLLLOOOOOOOOO!!!");


            playerTextures = Loader.ContentLoad<Texture2D>(Content, @".\PlayerImages\");

            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                NetOutgoingMessage sendMsg = client.CreateMessage();
                PlayerData playerLeaving = thisPlayer.PlayerDataPacket;
                playerLeaving.header = "Leaving";
                string json = JsonConvert.SerializeObject(playerLeaving);

                Exit();

            }
            if (Keyboard.GetState().IsKeyDown(Keys.Enter))
            {
                InGameMessage = "Sending Message";
                NetOutgoingMessage sendMsg = client.CreateMessage();
                sendMsg.Write("Hello there from client at " + gameTime.TotalGameTime.ToString());
                client.SendMessage(sendMsg, NetDeliveryMethod.ReliableOrdered);
            }



            foreach (var p in OtherPlayers)
            {
                p.ChangePosition(p.position);
            }

            PlayerMovement();



            CheckMessages();
            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        private void PlayerMovement()
        {
            if (Keyboard.GetState().IsKeyDown(Keys.A))
            {
                thisPlayer.position.X = thisPlayer.Position.X - 1;

            }
            if (Keyboard.GetState().IsKeyDown(Keys.D))
            {
                thisPlayer.position.X = thisPlayer.Position.X + 1;

            }
            if (Keyboard.GetState().IsKeyDown(Keys.S))
            {
                thisPlayer.position.Y = thisPlayer.Position.Y + 1;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.W))
            {
                thisPlayer.position.Y = thisPlayer.Position.Y - 1;
            }



        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin();
            spriteBatch.DrawString(font, InGameMessage, new Vector2(10, 10), Color.White);
            if (thisPlayer != null)
            {
                spriteBatch.Draw(playerTextures[thisPlayer.ImageName], thisPlayer.Position, Color.White);
                spriteBatch.DrawString(font, thisPlayer.gamerTag, new Vector2(thisPlayer.Position.X + 8, thisPlayer.Position.Y - 15), Color.White);
            }

            foreach (GamePlayer other in OtherPlayers)
            {
                spriteBatch.Draw(playerTextures[other.ImageName], other.Position, Color.White);
                spriteBatch.DrawString(font, other.gamerTag, new Vector2(other.Position.X + 8, other.Position.Y - 15), Color.White);

            }

            spriteBatch.End();
            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
        #region Lidgren message handlers
        private void CheckMessages()
        {
            NetIncomingMessage ServerMessage;
            if ((ServerMessage = client.ReadMessage()) != null)
            {
                switch (ServerMessage.MessageType)
                {
                    case NetIncomingMessageType.Data:
                        // handle custom messages
                        string message = ServerMessage.ReadString();
                        //InGameMessage = "Data In " + message;
                        process(message);
                        break;
                    case NetIncomingMessageType.DiscoveryResponse:
                        InGameMessage = ServerMessage.ReadString();
                        client.Connect(ServerMessage.SenderEndPoint);
                        InGameMessage = "Connected to " + ServerMessage.SenderEndPoint.Address.ToString();
                        if (thisPlayer == null)
                        {
                            string ImageName = "Badges_" + Utility.NextRandom(0, playerTextures.Count - 1);
                            thisPlayer = new GamePlayer(client, Guid.NewGuid(), "Frank", ImageName,
                                          new Vector2(Utility.NextRandom(100, GraphicsDevice.Viewport.Width - 100),
                                                       Utility.NextRandom(100, GraphicsDevice.Viewport.Height - 100)));


                        }

                        break;
                    case NetIncomingMessageType.StatusChanged:
                        // handle connection status messages
                        switch (ServerMessage.SenderConnection.Status)
                        {
                            /* .. */
                        }
                        break;

                    case NetIncomingMessageType.DebugMessage:
                        // handle debug messages
                        // (only received when compiled in DEBUG mode)
                        //InGameMessage = ServerMessage.ReadString();
                        break;


                        /* .. */

                        InGameMessage = "unhandled message with type: "
                            + ServerMessage.MessageType.ToString();
                        break;
                }
            }
        }
        private void process(string v)
        {
            // Need a try catch here
            PlayerData otherPlayer = JsonConvert.DeserializeObject<PlayerData>(v);
            // if it's the same player back just ignore it
            if (otherPlayer.playerID == thisPlayer.PlayerID)
                return;

            switch (otherPlayer.header)
            {
                case "Joined":
                    // Add the player to this game as another player
                    string ImageName = "Badges_" + Utility.NextRandom(0, playerTextures.Count - 1);
                    GamePlayer newPlayer = new GamePlayer(client, otherPlayer.gamerTag, otherPlayer.imageName, otherPlayer.playerID, new Vector2(otherPlayer.X, otherPlayer.Y));
                    //Fade out text for joining

                    new FadeText(this, Vector2.Zero, otherPlayer.gamerTag + " has Joined the Game ");
                    OtherPlayers.Add(newPlayer);





                    OtherPlayers.Add(newPlayer);

                    //fadeTxtMgr = new FadeText(new Vector2(100,100),newPlayer.PlayerID+" has joined the game");
                    break;


                //case "Moved":

                //    for (OtherPlayers.Find(p => p.PlayerID == otherPlayer.playerID))

                //    break;
                //case "Leaving":
                //    if(OtherPlayers.PlayerID == otherPlayer.playerID)
                //    {

                //    }
                //    break;
                default:
                    break;
            }

        }
        #endregion

    }
}

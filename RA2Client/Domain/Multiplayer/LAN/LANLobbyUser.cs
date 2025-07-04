﻿using System;
using System.Net;
using Microsoft.Xna.Framework.Graphics;

namespace Ra2Client.Domain.Multiplayer.LAN
{
    public class LANLobbyUser
    {
        public LANLobbyUser(string name, Texture2D gameTexture, IPEndPoint endPoint)
        {
            Name = name;
            GameTexture = gameTexture;
            EndPoint = endPoint;
        }

        public string Name { get; private set; }
        public Texture2D GameTexture { get; private set; }
        public IPEndPoint EndPoint { get; set; }
        public TimeSpan TimeWithoutRefresh { get; set; }
    }
}

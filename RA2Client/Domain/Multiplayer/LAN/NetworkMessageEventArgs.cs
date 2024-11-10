﻿using System;

namespace Ra2Client.Domain.Multiplayer.LAN
{
    public class NetworkMessageEventArgs : EventArgs
    {
        public NetworkMessageEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; private set; }
    }
}

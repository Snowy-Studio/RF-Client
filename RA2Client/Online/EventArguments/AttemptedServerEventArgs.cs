﻿using System;

namespace Ra2Client.Online.EventArguments
{
    /// <summary>
    /// Event arguments for a server connection attempt.
    /// </summary>
    public class AttemptedServerEventArgs : EventArgs
    {
        public AttemptedServerEventArgs(string serverName)
        {
            ServerName = serverName;
        }

        public string ServerName { get; private set; }
    }
}

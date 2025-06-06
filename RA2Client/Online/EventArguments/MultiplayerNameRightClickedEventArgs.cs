﻿using System;

namespace Ra2Client.Online.EventArguments
{
    public class MultiplayerNameRightClickedEventArgs : EventArgs
    {
        public string PlayerName { get; }

        public MultiplayerNameRightClickedEventArgs(string playerName)
        {
            PlayerName = playerName;
        }
    }
}

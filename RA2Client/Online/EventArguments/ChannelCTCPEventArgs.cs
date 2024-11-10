﻿using System;

namespace Ra2Client.Online.EventArguments
{
    public class ChannelCTCPEventArgs : EventArgs
    {
        public ChannelCTCPEventArgs(string userName, string message)
        {
            UserName = userName;
            Message = message;
        }

        public string UserName { get; private set; }
        public string Message { get; private set; }
    }
}

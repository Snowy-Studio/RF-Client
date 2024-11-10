﻿using System;

namespace Ra2Client.Online.EventArguments
{
    public class KickEventArgs : EventArgs
    {
        public KickEventArgs(string channelName, string userName)
        {
            ChannelName = channelName;
            UserName = userName;
        }

        public string ChannelName { get; private set; }
        public string UserName { get; private set; }
    }
}

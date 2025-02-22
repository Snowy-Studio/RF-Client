﻿using System;
using System.Collections.Generic;
using System.Linq;
using Ra2Client.Domain.Multiplayer;

namespace Ra2Client.DXGUI.Multiplayer.GameLobby
{
    public class GameModeMapFilter
    {
        public Func<List<GameModeMap>> GetGameModeMaps;

        public GameModeMapFilter(Func<List<GameModeMap>> filterAction)
        {
            GetGameModeMaps = filterAction;
        }

        public bool Any() => GetGameModeMaps().Any();
    }
}

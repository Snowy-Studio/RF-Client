using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using ClientCore;
using ClientCore.Statistics;
using ClientGUI;
using Ra2Client.Domain;
using Ra2Client.Domain.Multiplayer;
using Ra2Client.DXGUI.Generic;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace Ra2Client.DXGUI.Multiplayer.GameLobby
{
    public class SkirmishLobby : GameLobbyBase, ISwitchable
    {
        private XNAClientCheckBox chkTerrain;
        private const string SETTINGS_PATH = "Client/SkirmishSettings.ini";

        public SkirmishLobby(WindowManager windowManager, TopBar topBar, MapLoader mapLoader, DiscordHandler discordHandler, Random random)
            : base(windowManager, "SkirmishLobby", mapLoader, false, discordHandler, random)
        {
            this.topBar = topBar;
            this.random = random;
        }

        public event EventHandler Exited;

        private Random random;

        TopBar topBar;

        public override void Initialize()
        {
            Name = "SkirmishLobby";
            base.Initialize();

            RandomSeed = random.Next();

            //InitPlayerOptionDropdowns(128, 98, 90, 48, 55, new Point(6, 24));
            InitPlayerOptionDropdowns();

            btnLeaveGame.Text = "Main Menu".L10N("UI:Main:MainMenu");

            //MapPreviewBox.EnableContextMenu = true;

            ddPlayerSides[0].AddItem("Spectator".L10N("UI:Main:SpectatorSide"), AssetLoader.LoadTexture("spectatoricon.png"));

            MapPreviewBox.LocalStartingLocationSelected += MapPreviewBox_LocalStartingLocationSelected;
            MapPreviewBox.StartingLocationApplied += MapPreviewBox_StartingLocationApplied;

            WindowManager.CenterControlOnScreen(this);
            btnRandomMap.Enable();
            LoadSettings();

            CheckDisallowedSides();

            CopyPlayerDataToUI();

            ProgramConstants.PlayerNameChanged += ProgramConstants_PlayerNameChanged;
            ddPlayerSides[0].SelectedIndexChanged += PlayerSideChanged;

            PlayerExtraOptionsPanel?.SetIsHost(true);

            //ReloadAI();
            ReloadMod();

            CmbGame_SelectedChanged(cmbGame, null);

            if (FindChild<GameLobbyCheckBox>("chkTerrain") != null)
            {
                chkTerrain = FindChild<GameLobbyCheckBox>("chkTerrain");
            }
            else
            {
                chkTerrain = new XNAClientCheckBox(WindowManager);
                chkTerrain.Text = "Terrain\nExpansion".L10N("UI:Main:chkTerrain");
                chkTerrain.X = FindChild<XNAClientCheckBox>("chkCorr").X;
                chkTerrain.Y = FindChild<XNAClientCheckBox>("chkRuins").Y + 25;
                chkTerrain.SetToolTipText("When checked, terrain extension will be enabled, such as TX terrain extension.\nIt may cause bugs in the game. If pop-ups or air walls appear during play, you can turn this option off.\nThis option must be enabled for some map campaigns.".L10N("UI:Main:TPchkTerrain"));
                FindChild<XNAPanel>("ChkOptionsPanel").AddChild(chkTerrain);
            }

            //UserINISettings.Instance.启动游戏 += () => BtnLaunchGame_LeftClick(null,null);
        }

        protected override void ChangeMap(GameModeMap gameModeMap)
        {

            if (Map?.Rules?.Count > 0)
            {
                foreach (var cb in ddPlayerNames.Skip(1))
                {
                    cb.SelectedIndex = 0;
                    cb.AllowDropDown = true;
                }

                foreach (var cb in ddPlayerColors.Skip(1))
                {
                    cb.SelectedIndex = -1;
                    cb.AllowDropDown = true;
                }

                foreach (var cb in ddPlayerSides.Skip(1))
                {
                    cb.SelectedIndex = -1;
                    cb.AllowDropDown = true;
                }

                foreach (var cb in ddPlayerStarts.Skip(1))
                {
                    cb.SelectedIndex = -1;
                    cb.AllowDropDown = true;
                }

                foreach (var cb in ddPlayerTeams.Skip(1))
                {
                    cb.SelectedIndex = -1;
                    cb.AllowDropDown = true;
                }

                AIPlayers.Clear();

                int i = 1;

                foreach (var r in Map.Rules)
                {
                    if (r.Type == RuleType.Suggested) continue;

                    if (r.Requirement == PositionRequirement.Player)
                    {
                        ddPlayerStarts[0].SelectedIndex = r.Position1;
                        ddPlayerStarts[0].AllowDropDown = false;
                    }
                    else if (r.Requirement == PositionRequirement.AI)
                    {
                        ddPlayerNames[i].SelectedIndex = 1;
                        ddPlayerSides[i].SelectedIndex = 0;
                        ddPlayerColors[i].SelectedIndex = 0;
                        ddPlayerTeams[i].SelectedIndex = 0;
                        ddPlayerStarts[i].SelectedIndex = r.Position1;
                        ddPlayerStarts[i++].AllowDropDown = false;
                    }
                    else if (r.Requirement == PositionRequirement.SameTeam)
                    {

                    }
                }

                // 分配团队（直接传 Map.Rules）
                var teamMap = TeamAssignmentHelper.Assign(8, Map.Rules);

                // 应用团队编号
                for (int j = 0; j < 8; j++)
                {
                    ddPlayerTeams[j].SelectedIndex = teamMap[j];
                    Console.WriteLine($"玩家 {j} 被分配到队伍 {teamMap[j]}");
                }
            }

            base.ChangeMap(gameModeMap);
        }

        protected override void ToggleFavoriteMap()
        {
            base.ToggleFavoriteMap();

            if (GameModeMap != null && GameModeMap.IsFavorite)
            {
                RefreshForFavoriteMapRemoved();
                return;
            }
        }

        protected override void AddNotice(string message, Color color)
        {
            XNAMessageBox.Show(WindowManager, "message".L10N("UI:Main:MessageTitle"), message);
        }

        protected override void OnEnabledChanged(object sender, EventArgs args)
        {
            base.OnEnabledChanged(sender, args);
            if (Enabled)
                UpdateDiscordPresence(true);
            else
                ResetDiscordPresence();
        }

        private void ProgramConstants_PlayerNameChanged(object sender, EventArgs e)
        {
            Players[0].Name = ProgramConstants.PLAYERNAME;
            CopyPlayerDataToUI();
        }

        private void MapPreviewBox_StartingLocationApplied(object sender, EventArgs e)
        {
            CopyPlayerDataToUI();
        }

        private void MapPreviewBox_LocalStartingLocationSelected(object sender, LocalStartingLocationEventArgs e)
        {
            Players[0].StartingLocation = e.StartingLocationIndex + 1;
            CopyPlayerDataToUI();
        }

        private string CheckGameValidity()
        {
            int totalPlayerCount = Players.Count(p => p.SideId < ddPlayerSides[0].Items.Count - 1)
                + AIPlayers.Count;

            if (GameMode == null)
            {
                return "Please choose a map!".L10N("UI:Main:GameModeNull");
            }

            if (GameMode.MultiplayerOnly)
            {
                return String.Format("{0} can only be played on CnCNet and LAN.".L10N("UI:Main:GameModeMultiplayerOnly"),
                    GameMode.UIName);
            }

            if (GameMode.MinPlayersOverride > -1 && totalPlayerCount < GameMode.MinPlayersOverride)
            {
                return String.Format("{0} cannot be played with less than {1} players.".L10N("UI:Main:GameModeInsufficientPlayers"),
                         GameMode.UIName, GameMode.MinPlayersOverride);
            }

            if (Map.MultiplayerOnly)
            {
                return "The selected map can only be played on CnCNet and LAN.".L10N("UI:Main:MapMultiplayerOnly");
            }

            if (Map.MaxPlayers < 1)
            {
                return "There is a suspected problem with this map, and the client does not think there is a player spawn point.".L10N("UI:Main:MapError");
            }

            //if (totalPlayerCount < Map.MinPlayers)
            //{
            //    return String.Format("The selected map cannot be played with less than {0} players.".L10N("UI:Main:MapInsufficientPlayers"),
            //        Map.MinPlayers);
            //}

            if (totalPlayerCount > Map.MaxPlayers)
            {
                return String.Format("The selected map cannot be played with more than {0} players.".L10N("UI:Main:MapTooManyPlayers"),
                    Map.MaxPlayers);
            }

            if (Map.EnforceMaxPlayers)
            {
                

                IEnumerable<PlayerInfo> concatList = Players.Concat(AIPlayers);

                foreach (PlayerInfo pInfo in concatList)
                {
                    if (pInfo.StartingLocation == 0)
                        continue;

                    if (concatList.Count(p => p.StartingLocation == pInfo.StartingLocation) > 1)
                    {
                        return "Multiple players cannot share the same starting location on the selected map.".L10N("UI:Main:StartLocationOccupied");
                    }
                }
            }


            var list = Players.Concat(AIPlayers).Where(p => p.ColorId != 0);

            if (list.Select(p => p.ColorId).Distinct().Count() != list.Count())
            {
                return "Multiple participating parties cannot choose the same color.".L10N("UI:Main:ChooseSameColor");
            }

            if (Map.IsCoop && Players[0].SideId == ddPlayerSides[0].Items.Count - 1)
            {
                return "Co-op missions cannot be spectated. You'll have to show a bit more effort to cheat here.".L10N("UI:Main:CoOpMissionSpectatorPrompt");
            }

            //检查国家
            foreach (PlayerInfo pInfo in Players)
            {

                if (pInfo.SideId >= ddPlayerSides[0].Items.Count)
                {
                    return "请选择一个国家";
                }
            }

            var teamMappingsError = GetTeamMappingsError();
            if (!string.IsNullOrEmpty(teamMappingsError))
                return teamMappingsError;

            return null;
        }

        protected string CheckRules(Domain.Multiplayer.Rule rule,PlayerInfo seat1, PlayerInfo seat2)
        {
            switch (rule.Requirement)
            {
                case PositionRequirement.Player:
                    if (seat1?.IsAI != false)  // 玩家应该是IsAI = false
                    {
                        return $"位置{rule.Position1}必须是玩家";
                    }
                    break;

                case PositionRequirement.AI:
                    if (seat1?.IsAI != true)  // AI应该是IsAI = true
                    {
                        return $"位置{rule.Position1}必须是AI";
                    }
                    break;

                case PositionRequirement.SameTeam:
                    if (seat1?.TeamId > 0 == false || seat2?.TeamId > 0 == false || seat1?.TeamId != seat2?.TeamId)
                    {
                        return $"位置{rule.Position1}和位置{rule.Position2}必须同一队";
                    }
                    break;

                case PositionRequirement.DifferentTeam:
                    if (seat1?.TeamId < 0 == false && seat2?.TeamId < 0 == false && seat1?.TeamId == seat2?.TeamId)
                    {
                        return $"位置{rule.Position1}和位置{rule.Position2}必须不同队";
                    }
                    break;
                default:
                    return null;
                    
            }
            return null;
        }


        protected override void BtnLaunchGame_LeftClick(object sender, EventArgs e)
        {
            List<PlayerInfo> AllPlayers = [..Players, .. AIPlayers];

            for (int i = 0; i < Map.Rules.Count; i++)
            {
                var rule = Map.Rules[i];
                bool isLast = (i == Map.Rules.Count - 1);

                var seat1 = AllPlayers.FirstOrDefault(s => s.StartingLocation == rule.Position1);
                var seat2 = rule.Position2.HasValue ? AllPlayers.FirstOrDefault(s => s.StartingLocation == rule.Position2.Value) : null;

                var err = CheckRules(rule, seat1, seat2);
                  if (err != null)
                {
                    if (rule.Type == RuleType.Mandatory)
                    {
                        XNAMessageBox.Show(WindowManager, "无法启动游戏".L10N("UI:Main:LaunchGameErrorTitle"), err);
                        return;
                    }
                    else
                    {
                        var mbox = new XNAMessageBox(WindowManager, "建议", e + "\n确定要继续游戏吗？", XNAMessageBoxButtons.YesNo);

                        mbox.NoClickedAction += (_) => { return; };

                        if (isLast)
                        {
                            mbox.YesClickedAction += (_) => {
                                SaveSettings();
                                // FileHelper.ReNameCustomFile();
                                StartGame();
                            };
                            return;
                        }
                    }
                }
            }

            string error = CheckGameValidity();

            if (string.IsNullOrEmpty(error))
            {
                SaveSettings();
                // FileHelper.ReNameCustomFile();
                StartGame();
                return;
            }
            else
                XNAMessageBox.Show(WindowManager, "Cannot launch game".L10N("UI:Main:LaunchGameErrorTitle"), error);
        }

        protected override void BtnLeaveGame_LeftClick(object sender, EventArgs e)
        {
            Exited?.Invoke(this, EventArgs.Empty);
            
            PlayerExtraOptionsPanel?.Disable();
            Disable();

            topBar.RemovePrimarySwitchable(this);
            ResetDiscordPresence();
        }

        private void PlayerSideChanged(object sender, EventArgs e)
        {
            UpdateDiscordPresence();
        }

        protected override void UpdateDiscordPresence(bool resetTimer = false)
        {
            if (discordHandler == null || Map == null || GameMode == null || !Initialized)
                return;

            int playerIndex = Players.FindIndex(p => p.Name == ProgramConstants.PLAYERNAME);
            if (playerIndex >= MAX_PLAYER_COUNT || playerIndex < 0)
                return;

            XNAClientDropDown sideDropDown = ddPlayerSides[playerIndex];
            if (sideDropDown.SelectedItem == null)
                return;

            string side = sideDropDown.SelectedItem.Text;
            string currentState = ProgramConstants.IsInGame ? "In Game" : "Setting Up";

            discordHandler.UpdatePresence(
                Map.Name, GameMode.Name, currentState, side, resetTimer);
        }

        protected override bool AllowPlayerOptionsChange()
        {
            return true;
        }

        protected override int GetDefaultMapRankIndex(GameModeMap gameModeMap)
        {
            if(gameModeMap.Map == null) return 0;
            return StatisticsManager.Instance.GetSkirmishRankForDefaultMap(gameModeMap.Map.Name, gameModeMap.Map.MaxPlayers);
        }

        protected override void GameProcessExited()
        {
            base.GameProcessExited();

            DdGameModeMapFilter_SelectedIndexChanged(null, EventArgs.Empty); // Refresh ranks

            RandomSeed = random.Next();
        }

        public void Open()
        {
            topBar.AddPrimarySwitchable(this);
            Enable();
        }

        public void SwitchOn()
        {
            Enable();
        }

        public void SwitchOff()
        {
            Disable();
        }

        public string GetSwitchName()
        {
            return "Skirmish Lobby".L10N("UI:Main:SkirmishLobby");
        }

        /// <summary>
        /// Saves skirmish settings to an INI file on the file system.
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                FileInfo settingsFileInfo = SafePath.GetFile(ProgramConstants.GamePath, SETTINGS_PATH);

                // Delete the file so we don't keep potential extra AI players that already exist in the file
                settingsFileInfo.Delete();

                var skirmishSettingsIni = new IniFile(settingsFileInfo.FullName);

                skirmishSettingsIni.SetStringValue("Player", "Info", Players[0].ToString());

                for (int i = 0; i < AIPlayers.Count; i++)
                {
                    skirmishSettingsIni.SetStringValue("AIPlayers", i.ToString(), AIPlayers[i].ToString());
                }

                skirmishSettingsIni.SetStringValue("Settings", "Map", Map.SHA1);
                skirmishSettingsIni.SetStringValue("Settings", "GameModeMapFilter", ddGameModeMapFilter.SelectedItem?.Text);

                if (ClientConfiguration.Instance.SaveSkirmishGameOptions)
                {
                    foreach (GameLobbyDropDown dd in DropDowns)
                    {
                        skirmishSettingsIni.SetStringValue("GameOptions", dd.Name, dd.SelectedIndex + "");
                    }

                    foreach (GameLobbyCheckBox cb in CheckBoxes)
                    {
                        skirmishSettingsIni.SetStringValue("GameOptions", cb.Name, cb.Checked.ToString());
                    }

                    //skirmishSettingsIni.SetStringValue("GameOptions", "chkExtension", chkExtension.Checked.ToString());
                }

                skirmishSettingsIni.WriteIniFile();
                Logger.Log("保存遭遇战游戏设置!");
            }
            catch (Exception ex)
            {
                Logger.Log("Saving skirmish settings failed! Reason: " + ex.Message);
            }
        }

        /// <summary>
        /// Loads skirmish settings from an INI file on the file system.
        /// </summary>
        private void LoadSettings()
        {
            if (!SafePath.GetFile(ProgramConstants.GamePath, SETTINGS_PATH).Exists )
            {
                Logger.Log("遭遇战配置文件不存在,载入默认配置!");
                InitDefaultSettings();
                return;
            }

           // disableGameOptionUpdateBroadcast = true;
            Logger.Log("载入遭遇战配置!");

            var skirmishSettingsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, SETTINGS_PATH));

            string gameModeMapFilterName = skirmishSettingsIni.GetStringValue("Settings", "GameModeMapFilter", string.Empty);
            if (string.IsNullOrEmpty(gameModeMapFilterName))
                gameModeMapFilterName = skirmishSettingsIni.GetStringValue("Settings", "GameMode", string.Empty); // legacy

            var gameModeMapFilter = ddGameModeMapFilter.Items.Find(i => i.Text == gameModeMapFilterName)?.Tag as GameModeMapFilter;
            if (gameModeMapFilter == null || !gameModeMapFilter.Any())
                gameModeMapFilter = GetDefaultGameModeMapFilter();

            var gameModeMap = gameModeMapFilter?.GetGameModeMaps()?.First();

            if (gameModeMap != null)
            {
                GameModeMap = gameModeMap;

                ddGameModeMapFilter.SelectedIndex = ddGameModeMapFilter.Items.FindIndex(i => i.Tag == gameModeMapFilter);

                string mapSHA1 = skirmishSettingsIni.GetStringValue("Settings", "Map", string.Empty);

                //int gameModeMapIndex = gameModeMapFilter.GetGameModeMaps().FindIndex(gmm => gmm.Map.SHA1 == mapSHA1);
                int gameModeMapIndex = GetSortedGameModeMaps().OrderBy(o => o.Map?.MaxPlayers).ToList().FindIndex(map => map.Map.SHA1 == mapSHA1);
                
                  if (gameModeMapIndex > -1)
                {
                    lbGameModeMapList.SelectedIndex = gameModeMapIndex;

                    while (gameModeMapIndex > lbGameModeMapList.LastIndex)
                        lbGameModeMapList.TopIndex++;
                }
            }
            else
                LoadDefaultGameModeMap();

            var player = PlayerInfo.FromString(skirmishSettingsIni.GetStringValue("Player", "Info", string.Empty));

            if (player == null)
            {
                Logger.Log("Failed to load human player information from skirmish settings!");
                InitDefaultSettings();
                return;
            }

            CheckLoadedPlayerVariableBounds(player);

            player.Name = ProgramConstants.PLAYERNAME;
            Players.Add(player);

            List<string> keys = skirmishSettingsIni.GetSectionKeys("AIPlayers");

            if (keys == null)
            {
                keys = new List<string>(); // No point skip parsing all settings if only AI info is missing.
                //Logger.Log("AI player information doesn't exist in skirmish settings!");
                //InitDefaultSettings();
                //return;
            }
            if ( Map == null || GameMode == null)
                return;
            bool AIAllowed = !(Map.MultiplayerOnly || GameMode.MultiplayerOnly) || !(Map.HumanPlayersOnly || GameMode.HumanPlayersOnly);
            foreach (string key in keys)
            {
                if (!AIAllowed) break;
                var aiPlayer = PlayerInfo.FromString(skirmishSettingsIni.GetStringValue("AIPlayers", key, string.Empty));

                CheckLoadedPlayerVariableBounds(aiPlayer, true);

                if (aiPlayer == null)
                {
                    Logger.Log("Failed to load AI player information from skirmish settings!");
                    InitDefaultSettings();
                    return;
                }

                if (AIPlayers.Count < MAX_PLAYER_COUNT - 1)
                    AIPlayers.Add(aiPlayer);
            }

            if (ClientConfiguration.Instance.SaveSkirmishGameOptions)
            {

                Logger.Log("载入游戏选项设置!");

                foreach (GameLobbyDropDown dd in DropDowns)
                {
                    // Maybe we should build an union of the game mode and map
                    // forced options, we'd have less repetitive code that way

                    if (GameMode != null)
                    {
                        int gameModeMatchIndex = GameMode.ForcedDropDownValues.FindIndex(p => p.Key.Equals(dd.Name));
                        if (gameModeMatchIndex > -1)
                        {
                            Logger.Log("Dropdown '" + dd.Name + "' has forced value in gamemode - saved settings ignored.");
                            continue;
                        }
                    }

                    if (Map != null)
                    {
                        int gameModeMatchIndex = Map.ForcedDropDownValues.FindIndex(p => p.Key.Equals(dd.Name));
                        if (gameModeMatchIndex > -1)
                        {
                            Logger.Log("Dropdown '" + dd.Name + "' has forced value in map - saved settings ignored.");
                            continue;
                        }
                    }

                    dd.UserSelectedIndex = skirmishSettingsIni.GetIntValue("GameOptions", dd.Name, dd.UserSelectedIndex);

                    if (dd.UserSelectedIndex > -1 && dd.UserSelectedIndex < dd.Items.Count)
                        dd.SelectedIndex = dd.UserSelectedIndex;
                }

                foreach (GameLobbyCheckBox cb in CheckBoxes)
                {
                    if (GameMode != null)
                    {
                        int gameModeMatchIndex = GameMode.ForcedCheckBoxValues.FindIndex(p => p.Key.Equals(cb.Name));
                        if (gameModeMatchIndex > -1)
                        {
                            Logger.Log("Checkbox '" + cb.Name + "' has forced value in gamemode - saved settings ignored.");
                            continue;
                        }
                    }

                    if (Map != null)
                    {
                        int gameModeMatchIndex = Map.ForcedCheckBoxValues.FindIndex(p => p.Key.Equals(cb.Name));
                        if (gameModeMatchIndex > -1)
                        {
                            Logger.Log("Checkbox '" + cb.Name + "' has forced value in map - saved settings ignored.");
                            continue;
                        }
                    }

                    cb.Checked = skirmishSettingsIni.GetBooleanValue("GameOptions", cb.Name, cb.Checked);
                }

               // chkExtension.Checked = skirmishSettingsIni.GetBooleanValue("GameOptions", "chkExtension", chkExtension.Checked);
            }
        }

        /// <summary>
        /// Checks that a player's color, team and starting location
        /// don't exceed allowed bounds.
        /// </summary>
        /// <param name="pInfo">The PlayerInfo.</param>
        private void CheckLoadedPlayerVariableBounds(PlayerInfo pInfo, bool isAIPlayer = false)
        {
            if(GameMode?.Maps == null || Map == null)
                return;

            int sideCount = SideCount + RandomSelectorCount;
            if (isAIPlayer) sideCount--;

            if (pInfo.SideId < 0)
            {
                pInfo.SideId = 0;
            }

            if (pInfo.ColorId < 0 || pInfo.ColorId > MPColors.Count)
            {
                pInfo.ColorId = 0;
            }

            if (pInfo.TeamId < 0 || pInfo.TeamId >= ddPlayerTeams[0].Items.Count ||
                !Map.IsCoop && (Map.ForceNoTeams || (GameMode?.ForceNoTeams ?? false)))
            {
                pInfo.TeamId = 0;
            }

            if (pInfo.StartingLocation < 0 || pInfo.StartingLocation > MAX_PLAYER_COUNT ||
                !Map.IsCoop && (Map.ForceRandomStartLocations || GameMode.ForceRandomStartLocations))
            {
                pInfo.StartingLocation = 0;
            }
        }

        private void InitDefaultSettings()
        {
            Players.Clear();
            AIPlayers.Clear();

            Players.Add(new PlayerInfo(ProgramConstants.PLAYERNAME, 0, 0, 0, 0));
            PlayerInfo aiPlayer = new PlayerInfo(ProgramConstants.AI_PLAYER_NAMES[0], 0, 0, 0, 0);
            aiPlayer.IsAI = true;
            aiPlayer.AILevel = 0;
            AIPlayers.Add(aiPlayer);

            LoadDefaultGameModeMap();
        }

        protected override void UpdateMapPreviewBoxEnabledStatus()
        {
            MapPreviewBox.EnableContextMenu = !((Map != null && Map.ForceRandomStartLocations) || (GameMode != null && GameMode.ForceRandomStartLocations) || GetPlayerExtraOptions().IsForceRandomStarts);
            MapPreviewBox.EnableStartLocationSelection = MapPreviewBox.EnableContextMenu;
        }
    }
}

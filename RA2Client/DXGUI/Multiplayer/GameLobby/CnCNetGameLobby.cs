using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using Ra2Client.Domain;
using Ra2Client.Domain.Multiplayer;
using Ra2Client.Domain.Multiplayer.CnCNet;
using Ra2Client.DXGUI.Generic;
using Ra2Client.DXGUI.Multiplayer.CnCNet;
using Ra2Client.DXGUI.Multiplayer.GameLobby.CommandHandlers;
using Ra2Client.Online;
using Ra2Client.Online.EventArguments;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using ClientCore.Settings;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ra2Client.DXGUI.Multiplayer.GameLobby
{
    public class CnCNetGameLobby : MultiplayerGameLobby
    {
        private const int HUMAN_PLAYER_OPTIONS_LENGTH = 3;
        private const int AI_PLAYER_OPTIONS_LENGTH = 2;

        private const double GAME_BROADCAST_INTERVAL = 30.0;
        private const double GAME_BROADCAST_ACCELERATION = 10.0;
        private const double INITIAL_GAME_BROADCAST_DELAY = 10.0;

        private static readonly Color ERROR_MESSAGE_COLOR = Color.Yellow;

        private const string MAP_SHARING_FAIL_MESSAGE = "MAPFAIL";
        private const string MAP_SHARING_DOWNLOAD_REQUEST = "MAPOK";
        private const string MAP_SHARING_UPLOAD_REQUEST = "MAPREQ";
        private const string MAP_SHARING_DISABLED_MESSAGE = "MAPSDISABLED";
        private const string GAME_NAME_CHANGED = "GNC";
        private const string CHEAT_DETECTED_MESSAGE = "CD";
        private const string DICE_ROLL_MESSAGE = "DR";
        private const string CHANGE_TUNNEL_SERVER_MESSAGE = "CHTNL";

        private Random random;


        public CnCNetGameLobby(
            WindowManager windowManager,
            TopBar topBar,
            CnCNetManager connectionManager,
            TunnelHandler tunnelHandler,
            GameCollection gameCollection,
            CnCNetUserData cncnetUserData,
            MapLoader mapLoader,
            DiscordHandler discordHandler,
            PrivateMessagingWindow pmWindow,
            Random random
        ) : base(windowManager, "MultiplayerGameLobby", topBar, mapLoader, discordHandler, pmWindow, random)
        {
            this.connectionManager = connectionManager;
            localGame = ClientConfiguration.Instance.LocalGame;
            this.tunnelHandler = tunnelHandler;
            this.gameCollection = gameCollection;
            this.cncnetUserData = cncnetUserData;
            this.pmWindow = pmWindow;
            this.random = random;

            ctcpCommandHandlers =
            [  
                new IntCommandHandler("OR", HandleOptionsRequest),
                new IntCommandHandler("R", HandleReadyRequest),
                new StringCommandHandler("PO", ApplyPlayerOptions),
                new StringCommandHandler(PlayerExtraOptions.CNCNET_MESSAGE_KEY, ApplyPlayerExtraOptions),
                new StringCommandHandler("GO", ApplyGameOptions),
                new StringCommandHandler("START", NonHostLaunchGame),
                new NotificationHandler("AISPECS", HandleNotification, AISpectatorsNotification),
                new NotificationHandler("GETREADY", HandleNotification, GetReadyNotification),
                new NotificationHandler("INSFSPLRS", HandleNotification, InsufficientPlayersNotification),
                new NotificationHandler("TMPLRS", HandleNotification, TooManyPlayersNotification),
                new NotificationHandler("CLRS", HandleNotification, SharedColorsNotification),
                new NotificationHandler("SLOC", HandleNotification, SharedStartingLocationNotification),
                new NotificationHandler("LCKGME", HandleNotification, LockGameNotification),
                new IntNotificationHandler("NVRFY", HandleIntNotification, NotVerifiedNotification),
                new IntNotificationHandler("INGM", HandleIntNotification, StillInGameNotification),
                new StringCommandHandler(MAP_SHARING_UPLOAD_REQUEST, HandleMapUploadRequest),
                new StringCommandHandler(MAP_SHARING_FAIL_MESSAGE, HandleMapTransferFailMessage),
                new StringCommandHandler(MAP_SHARING_DOWNLOAD_REQUEST, HandleMapDownloadRequest),
                new NoParamCommandHandler(MAP_SHARING_DISABLED_MESSAGE, HandleMapSharingBlockedMessage),
                new NoParamCommandHandler("STRTD", GameStartedNotification),
                new NoParamCommandHandler("RETURN", ReturnNotification),
                new IntCommandHandler("TNLPNG", HandleTunnelPing),
                new StringCommandHandler("FHSH", FileHashNotification),
                new StringCommandHandler("MM", CheaterNotification),
                new StringCommandHandler(DICE_ROLL_MESSAGE, HandleDiceRollResult),
                // new NoParamCommandHandler(CHEAT_DETECTED_MESSAGE, HandleCheatDetectedMessage),
                new StringCommandHandler(GAME_NAME_CHANGED, HandleGameNameChangeMessage),
                new StringCommandHandler(CHANGE_TUNNEL_SERVER_MESSAGE, HandleTunnelServerChangeMessage),

                 
            ];

            CnCNetLobby.Download_Notice += HandleMapDownloadNotice;
            CnCNetLobby.Download += HandleMapDownload;

            MapSharer.MapDownloadFailed += MapSharer_MapDownloadFailed;
            MapSharer.MapDownloadComplete += MapSharer_MapDownloadComplete;
            MapSharer.MapUploadFailed += MapSharer_MapUploadFailed;
            MapSharer.MapUploadComplete += MapSharer_MapUploadComplete;

            AddChatBoxCommand(new ChatBoxCommand("TUNNELINFO",
                "View tunnel server information".L10N("UI:Main:TunnelInfo"), false, PrintTunnelServerInformation));
            AddChatBoxCommand(new ChatBoxCommand("GAMENAME",
                "Change a game's name (game host only). Example: \"/gamename 2v2 3v3\"".L10N("UI:Main:ChangeGameNameCommand"), true, s => ChangeGameName(s)));
            AddChatBoxCommand(new ChatBoxCommand("CHANGETUNNEL",
                "Change the used CnCNet tunnel server (game host only)".L10N("UI:Main:ChangeTunnel"),
                true, (s) => ShowTunnelSelectionWindow("Select tunnel server:".L10N("UI:Main:SelectTunnelServer"))));
            AddChatBoxCommand(new ChatBoxCommand("DOWNLOADMAP",
                "Download_Notice a map from CNCNet's map server using a map ID and an optional filename.\nExample: \"/downloadmap MAPID [2] My Battle Map\"".L10N("UI:Main:DownloadMapCommandDescription"),
                false, DownloadMapByIdCommand));
        }

        private async void HandleMapDownload(object sender, EventArgs e)
        {
            var s = (sender as string)?.Split(";");
            if (s == null || s.Length != 2) return;

            var sha = s[1];
            var name = s[0];

            char replaceUnsafeCharactersWith = '-';
            HashSet<char> invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());
            string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            string safeName = $"{timestamp}_{new string(name.Select(c => invalidChars.Contains(c) ? replaceUnsafeCharactersWith : c).ToArray())}";

            var baseUrl = $"{ProgramConstants.CUR_SERVER_URL}/Client/Custom_Map/{sha}.map";
            string strLocPath = Path.Combine(ProgramConstants.GamePath, "Maps", "Multi", "Custom", safeName);
            string dir = Path.GetDirectoryName(strLocPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            try
            {
                using var httpClient = new HttpClient();

                string strTmp = Path.Combine(ProgramConstants.GamePath, "Tmp");
                if (!Directory.Exists(strTmp))
                    Directory.CreateDirectory(strTmp);

                // 如果文件已存在，尝试删除，失败则跳过
                if (File.Exists(strLocPath))
                {
                    try { File.Delete(strLocPath); }
                    catch (Exception ex)
                    {
                        Console.WriteLine("删除旧地图文件失败: " + ex);
                        return;
                    }
                }

                var response = await httpClient.GetAsync(baseUrl);
                response.EnsureSuccessStatusCode();

                using (var fs = new FileStream(strLocPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fs);
                }

                刷新地图列表();
                //MapLoader.AgainLoadMaps();

                GameModeMap = GameModeMaps.Find(gmm => gmm.Map.SHA1 == sha);
                ChangeMap(GameModeMap);
            }
            catch (Exception ex)
            {
                AddNotice("地图下载失败: " + ex.Message, Color.Red);
                Console.WriteLine(ex);
            }
        }


        private Dictionary<string, XNAMessageBox> pendingMapTransferDialogs = new Dictionary<string, XNAMessageBox>();

        private void HandleMapDownloadNotice(object sender, EventArgs e)
        {
            string requester = sender as string;
            if (requester == null)
                return;

            // 如果该请求已存在，则忽略重复请求
            if (pendingMapTransferDialogs.ContainsKey(requester))
                return;

            var map = GameModeMap?.Map;
            if (map == null)
                return;

            var path = $"{ProgramConstants.GamePath}{map.BaseFilePath}";

            var messageBox = new XNAMessageBox(
                WindowManager,
                "Request a map transfer".L10N("UI:Main:RequestMapTransfer"),
                $"{requester} 请求传输地图: {map.Name}",
                XNAMessageBoxButtons.YesNo
            );

            messageBox.YesClickedAction += (_) =>
            {
                try
                {
                    using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                    var fileContent = new StreamContent(fileStream);
                    var fileNameContent = new StringContent(Path.GetFileName(path));

                    var customMapDto = new MultipartFormDataContent
                    {
                        { fileNameContent, "name" },
                        { fileContent, "file", Path.GetFileName(path) }
                    };

                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            var result = await NetWorkINISettings.Post<string>("custom_map/upload", customMapDto);
                            if (string.IsNullOrEmpty(result.Item1))
                            {
                                AddNotice("地图上传失败，服务器未返回有效SHA1", Color.Red);
                                return;
                            }
                            string messageBody = $"{ProgramConstants.MAP_DOWNLOAD} {Path.GetFileName(path)};{result.Item1}";
                            connectionManager.SendCustomMessage(new QueuedMessage(
                                "PRIVMSG " + requester + " :\u0001" + messageBody + "\u0001", QueuedMessageType.CHAT_MESSAGE, 0
                            ));
                        }
                        catch (Exception ex)
                        {
                            AddNotice("地图上传失败: " + ex.Message, Color.Red);
                            Console.WriteLine(ex);
                        }
                    });

                    task.Wait();
                }
                catch (Exception ex)
                {
                    AddNotice("地图上传失败: " + ex.Message, Color.Red);
                    Console.WriteLine(ex);
                }
                finally
                {
                    pendingMapTransferDialogs.Remove(requester);
                    messageBox.Dispose();
                }
            };

            messageBox.NoClickedAction += (_) =>
            {
                pendingMapTransferDialogs.Remove(requester);
                messageBox.Dispose();
            };

            messageBox.Show();
            pendingMapTransferDialogs.Add(requester, messageBox);
        }


        public event EventHandler GameLeft;

        private TunnelHandler tunnelHandler;
        private TunnelSelectionWindow tunnelSelectionWindow;
        private XNAClientButton btnChangeTunnel;
        private XNAClientButton btnClosePass;

        private Channel channel;
        private CnCNetManager connectionManager;
        private string localGame;

        private GameCollection gameCollection;
        private CnCNetUserData cncnetUserData;
        private readonly PrivateMessagingWindow pmWindow;
        private GlobalContextMenu globalContextMenu;

        private string hostName;

        private CommandHandlerBase[] ctcpCommandHandlers;

        private IRCColor chatColor;

        private XNATimerControl gameBroadcastTimer;

        private int playerLimit;

        private bool closed = false;

        private int skillLevel = ClientConfiguration.Instance.DefaultSkillLevelIndex;

        private bool isCustomPassword = false;

        private string password = string.Empty;

        private string gameFilesHash;

        private List<string> hostUploadedMaps = new List<string>();
        private List<string> chatCommandDownloadedMaps = new List<string>();

        private MapSharingConfirmationPanel mapSharingConfirmationPanel;

        /// <summary>
        /// The SHA1 of the latest selected map.
        /// Used for map sharing.
        /// </summary>
        private string lastMapSHA1;

        /// <summary>
        /// The map name of the latest selected map.
        /// Used for map sharing.
        /// </summary>
        private string lastMapName;

        /// <summary>
        /// The game mode of the latest selected map.
        /// Used for map sharing.
        /// </summary>
        private string lastGameMode;

        public override void Initialize()
        {
            IniNameOverride = nameof(CnCNetGameLobby);
            base.Initialize();

            btnChangeTunnel = FindChild<XNAClientButton>(nameof(btnChangeTunnel));

            btnChangeTunnel.Text = "Change Tunnel".L10N("UI:Main:ChangeTunnel");

            //暂时修改测试
            btnChangeTunnel.LeftClick += BtnChangeTunnel_LeftClick;
            //btnChangeTunnel.LeftClick += MapSharingConfirmationPanel_MapDownloadConfirmed;
            btnClosePass = new XNAClientButton(WindowManager);
            btnClosePass.Text = "Remove the password".L10N("UI:Main:RemovePassword");
            btnClosePass.ClientRectangle = new Rectangle(btnChangeTunnel.X - 150, btnChangeTunnel.Y, btnChangeTunnel.Width, btnChangeTunnel.Height);
            btnClosePass.LeftClick += BtnClosePass_LeftClick;
            btnClosePass.Visible = false;

            gameBroadcastTimer = new XNATimerControl(WindowManager);
            gameBroadcastTimer.AutoReset = true;
            gameBroadcastTimer.Interval = TimeSpan.FromSeconds(GAME_BROADCAST_INTERVAL);
            gameBroadcastTimer.Enabled = false;
            gameBroadcastTimer.TimeElapsed += GameBroadcastTimer_TimeElapsed;

            tunnelSelectionWindow = new TunnelSelectionWindow(WindowManager, tunnelHandler);
            tunnelSelectionWindow.Initialize();
            tunnelSelectionWindow.DrawOrder = 1;
            tunnelSelectionWindow.UpdateOrder = 1;
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, tunnelSelectionWindow);
            tunnelSelectionWindow.CenterOnParent();
            tunnelSelectionWindow.Disable();
            tunnelSelectionWindow.TunnelSelected += TunnelSelectionWindow_TunnelSelected;

            mapSharingConfirmationPanel = new MapSharingConfirmationPanel(WindowManager);
            MapPreviewBox.AddChild(mapSharingConfirmationPanel);
            mapSharingConfirmationPanel.MapDownloadConfirmed += MapSharingConfirmationPanel_MapDownloadConfirmed;

            WindowManager.AddAndInitializeControl(gameBroadcastTimer);

            globalContextMenu = new GlobalContextMenu(WindowManager, connectionManager, cncnetUserData, pmWindow);
            AddChild(globalContextMenu);

            MultiplayerNameRightClicked += MultiplayerName_RightClick;
            AddChild(btnClosePass);
            PostInitialize();

            
        }

        private void BtnClosePass_LeftClick(object sender, EventArgs e)
        {
            isCustomPassword = false;
            
          
            var hg = connectionManager.FindChannel(channel.ChannelName);

            var password = Utilities.CalculateSHA1ForString
            (hg.ChannelName + hg.UIName).Substring(0, 10);
            Console.WriteLine(password);
            hg.Password = password;
            btnClosePass.Visible = false;
            AddNotice("The host has decoded the room".L10N("UI:Main:HostRemovePassword"));
        }

        private void MultiplayerName_RightClick(object sender, MultiplayerNameRightClickedEventArgs args)
        {
            globalContextMenu.Show(new GlobalContextMenuData()
            {
                PlayerName = args.PlayerName,
                PreventJoinGame = true
            }, GetCursorPoint());
        }

        private void BtnChangeTunnel_LeftClick(object sender, EventArgs e) => ShowTunnelSelectionWindow("Select tunnel server:".L10N("UI:Main:SelectTunnelServer"));

        private void GameBroadcastTimer_TimeElapsed(object sender, EventArgs e) => BroadcastGame();

        public void SetUp(Channel channel, bool isHost, int playerLimit,
            CnCNetTunnel tunnel, string hostName, bool isCustomPassword,
            int skillLevel)
        {
            this.channel = channel;
            channel.MessageAdded += Channel_MessageAdded;
            channel.CTCPReceived += Channel_CTCPReceived;
            channel.UserKicked += Channel_UserKicked;
            channel.UserQuitIRC += Channel_UserQuitIRC;
            channel.UserLeft += Channel_UserLeft;
            channel.UserAdded += Channel_UserAdded;
            channel.UserNameChanged += Channel_UserNameChanged;
            channel.UserListReceived += Channel_UserListReceived;

            this.hostName = hostName;
            this.playerLimit = playerLimit;
            this.isCustomPassword = isCustomPassword;
            this.skillLevel = skillLevel;
            // this.password = password;

            if (isHost)
            {
                RandomSeed = random.Next();
                RefreshMapSelectionUI();
                btnChangeTunnel.Enable();
            }
            else
            {
                channel.ChannelModesChanged += Channel_ChannelModesChanged;
                AIPlayers.Clear();
                btnChangeTunnel.Disable();
            }

            //if (!isCustomPassword)
            //{
            //    btnClosePass.Visible = false;
            //}
            //else
            //{
            //    btnClosePass.Visible = true;
            //}
            tunnelHandler.CurrentTunnel = tunnel;
            tunnelHandler.CurrentTunnelPinged += TunnelHandler_CurrentTunnelPinged;

            connectionManager.ConnectionLost += ConnectionManager_ConnectionLost;
            connectionManager.Disconnected += ConnectionManager_Disconnected;

            Refresh(isHost);
        }

        private void TunnelHandler_CurrentTunnelPinged(object sender, EventArgs e) => UpdatePing();

        public void OnJoined()
        {
            FileHashCalculator fhc = new FileHashCalculator();
            fhc.CalculateHashes(GameModeMaps.GameModes);

            gameFilesHash = fhc.GetCompleteHash();

            if (IsHost)
            {
                connectionManager.SendCustomMessage(new QueuedMessage(
                    string.Format("MODE {0} +klnNs {1} {2}", channel.ChannelName,
                    channel.Password, playerLimit),
                    QueuedMessageType.SYSTEM_MESSAGE, 50));

                connectionManager.SendCustomMessage(new QueuedMessage(
                    string.Format("TOPIC {0} :{1}", channel.ChannelName,
                    ProgramConstants.CNCNET_PROTOCOL_REVISION + ";" + localGame.ToLower()),
                    QueuedMessageType.SYSTEM_MESSAGE, 50));

                gameBroadcastTimer.Enabled = true;
                gameBroadcastTimer.Start();
                gameBroadcastTimer.SetTime(TimeSpan.FromSeconds(INITIAL_GAME_BROADCAST_DELAY));
            }
            else
            {
                channel.SendCTCPMessage("FHSH " + gameFilesHash, QueuedMessageType.SYSTEM_MESSAGE, 10);
            }

            TopBar.AddPrimarySwitchable(this);
            TopBar.SwitchToPrimary();
            WindowManager.SelectedControl = tbChatInput;
            ResetAutoReadyCheckbox();
            UpdatePing();
            UpdateDiscordPresence(true);
        }

        private void UpdatePing()
        {
            if (tunnelHandler.CurrentTunnel == null)
                return;

            channel.SendCTCPMessage("TNLPNG " + tunnelHandler.CurrentTunnel.PingInMs, QueuedMessageType.SYSTEM_MESSAGE, 10);

            PlayerInfo pInfo = Players.Find(p => p.Name.Equals(ProgramConstants.PLAYERNAME));
            if (pInfo != null)
            {
                pInfo.Ping = tunnelHandler.CurrentTunnel.PingInMs;
                UpdatePlayerPingIndicator(pInfo);
            }
        }

        protected override void CopyPlayerDataToUI()
        {
            base.CopyPlayerDataToUI();

            for (int i = AIPlayers.Count + Players.Count; i < MAX_PLAYER_COUNT; i++)
            {
                StatusIndicators[i].SwitchTexture(
                    i < playerLimit ? PlayerSlotState.Empty : PlayerSlotState.Unavailable);
            }
        }

        private void PrintTunnelServerInformation(string s)
        {
            if (tunnelHandler.CurrentTunnel == null)
            {
                AddNotice("Tunnel server unavailable!".L10N("UI:Main:TunnelUnavailable"));
            }
            else
            {
                AddNotice(string.Format("Current tunnel server: \n{0}\nRegion: {1}\nPlayers: {2}/{3} | Official: {4}".L10N("UI:Main:TunnelInfo"),
                        tunnelHandler.CurrentTunnel.Name, tunnelHandler.CurrentTunnel.Country, tunnelHandler.CurrentTunnel.Clients, tunnelHandler.CurrentTunnel.MaxClients, tunnelHandler.CurrentTunnel.Official
                    ));
            }
        }

        private void ShowTunnelSelectionWindow(string description)
        {
            tunnelSelectionWindow.Open(description,
                tunnelHandler.CurrentTunnel?.Address);
        }

        private void TunnelSelectionWindow_TunnelSelected(object sender, TunnelEventArgs e)
        {
            channel.SendCTCPMessage($"{CHANGE_TUNNEL_SERVER_MESSAGE} {e.Tunnel.Address}:{e.Tunnel.Port}",
                QueuedMessageType.SYSTEM_MESSAGE, 10);
            HandleTunnelServerChange(e.Tunnel);
        }

        public void ChangeChatColor(IRCColor chatColor)
        {
            this.chatColor = chatColor;
            tbChatInput.TextColor = chatColor.XnaColor;
        }

        public override void Clear()
        {
            base.Clear();

            if (channel != null)
            {
                channel.MessageAdded -= Channel_MessageAdded;
                channel.CTCPReceived -= Channel_CTCPReceived;
                channel.UserKicked -= Channel_UserKicked;
                channel.UserQuitIRC -= Channel_UserQuitIRC;
                channel.UserLeft -= Channel_UserLeft;
                channel.UserAdded -= Channel_UserAdded;
                channel.UserNameChanged -= Channel_UserNameChanged;
                channel.UserListReceived -= Channel_UserListReceived;

                if (!IsHost)
                {
                    channel.ChannelModesChanged -= Channel_ChannelModesChanged;
                }

                connectionManager.RemoveChannel(channel);
            }

            Disable();
            PlayerExtraOptionsPanel?.Disable();
            
            connectionManager.ConnectionLost -= ConnectionManager_ConnectionLost;
            connectionManager.Disconnected -= ConnectionManager_Disconnected;

            gameBroadcastTimer.Enabled = false;
            closed = false;

            tbChatInput.Text = string.Empty;

            tunnelHandler.CurrentTunnel = null;
            tunnelHandler.CurrentTunnelPinged -= TunnelHandler_CurrentTunnelPinged;

            GameLeft?.Invoke(this, EventArgs.Empty);

            TopBar.RemovePrimarySwitchable(this);
            ResetDiscordPresence();
        }

        public void LeaveGameLobby()
        {
            if (IsHost)
            {
                closed = true;
                BroadcastGame();
            }

            Clear();
            channel?.Leave();
        }

        private void ConnectionManager_Disconnected(object sender, EventArgs e) => HandleConnectionLoss();

        private void ConnectionManager_ConnectionLost(object sender, ConnectionLostEventArgs e) => HandleConnectionLoss();

        private void HandleConnectionLoss()
        {
            Clear();
            Disable();
        }

        private void Channel_UserNameChanged(object sender, UserNameChangedEventArgs e)
        {
            Logger.Log("CnCNetGameLobby: Nickname change: " + e.OldUserName + " to " + e.User.Name);
            int index = Players.FindIndex(p => p.Name == e.OldUserName);
            if (index > -1)
            {
                PlayerInfo player = Players[index];
                player.Name = e.User.Name;
                ddPlayerNames[index].Items[0].Text = player.Name;
                AddNotice(string.Format("Player {0} changed their name to {1}".L10N("UI:Main:PlayerRename"), e.OldUserName, e.User.Name));
            }
        }

        protected override void BtnLeaveGame_LeftClick(object sender, EventArgs e) => LeaveGameLobby();

        protected override void UpdateDiscordPresence(bool resetTimer = false)
        {
            if (discordHandler == null || Map == null || GameMode == null)
                return;

            PlayerInfo player = FindLocalPlayer();
            if (player == null || Players == null || !Players.Contains(player))
                return;
            string side = "";
            var playerIndex = Players.IndexOf(player);
            if (ddPlayerSides != null &&
                playerIndex >= 0 &&
                playerIndex < ddPlayerSides.Length &&
                ddPlayerSides[playerIndex]?.SelectedItem != null)
            {
                side = ddPlayerSides[playerIndex].SelectedItem.Text;
            }

            discordHandler.UpdatePresence(
                Map?.Name ?? "[No Map]",
                GameMode?.Name ?? "[No Mode]",
                "Multiplayer",
                ProgramConstants.IsInGame ? "In Game" : "In Lobby",
                Players?.Count ?? 0,
                playerLimit,
                side,
                channel?.UIName ?? "[No Channel]",
                IsHost,
                isCustomPassword,
                Locked,
                resetTimer
            );
        }

        private void Channel_UserQuitIRC(object sender, UserNameEventArgs e)
        {
            RemovePlayer(e.UserName);

            if (e.UserName == hostName)
            {
                connectionManager.MainChannel.AddMessage(new ChatMessage(
                    ERROR_MESSAGE_COLOR, "The game host abandoned the game.".L10N("UI:Main:HostAbandoned")));
                BtnLeaveGame_LeftClick(this, EventArgs.Empty);
            }
            else
                UpdateDiscordPresence();
        }

        private void Channel_UserLeft(object sender, UserNameEventArgs e)
        {
            RemovePlayer(e.UserName);

            if (e.UserName == hostName)
            {
                connectionManager.MainChannel.AddMessage(new ChatMessage(
                    ERROR_MESSAGE_COLOR, "The game host abandoned the game.".L10N("UI:Main:HostAbandoned")));
                BtnLeaveGame_LeftClick(this, EventArgs.Empty);
            }
            else
                UpdateDiscordPresence();
        }

        private void Channel_UserKicked(object sender, UserNameEventArgs e)
        {
            if (e.UserName == ProgramConstants.PLAYERNAME)
            {
                connectionManager.MainChannel.AddMessage(new ChatMessage(
                    ERROR_MESSAGE_COLOR, "You were kicked from the game!".L10N("UI:Main:YouWereKicked")));
                Clear();
                this.Visible = false;
                this.Enabled = false;
                return;
            }

            int index = Players.FindIndex(p => p.Name == e.UserName);

            if (index > -1)
            {
                Players.RemoveAt(index);
                CopyPlayerDataToUI();
                UpdateDiscordPresence();
                ClearReadyStatuses();
            }
        }

        private void Channel_UserListReceived(object sender, EventArgs e)
        {
            if (!IsHost)
            {
                if (channel.Users.Find(hostName) == null)
                {
                    connectionManager.MainChannel.AddMessage(new ChatMessage(
                        ERROR_MESSAGE_COLOR, "The game host has abandoned the game.".L10N("UI:Main:HostHasAbandoned")));
                    BtnLeaveGame_LeftClick(this, EventArgs.Empty);
                }
            }
            UpdateDiscordPresence();
        }

        private void Channel_UserAdded(object sender, ChannelUserEventArgs e)
        {
            PlayerInfo pInfo = new PlayerInfo(e.User.IRCUser.Name);
            
            if (IsHost && e.User.IRCUser.IsIgnored)
            {
                // If this ignored player constantly rejoins, he could cause the host to floodout using the normal RemovePlayer() functionality. 
                // So lets Ghost kickban from gameroom instead. This should only be needed once per created room
                GhostBanIgnoredUser(pInfo.Name);
                return;
            }
            
            Players.Add(pInfo);

            if (Players.Count + AIPlayers.Count > MAX_PLAYER_COUNT && AIPlayers.Count > 0)
                AIPlayers.RemoveAt(AIPlayers.Count - 1);

            sndJoinSound.Play();
            WindowManager.FlashWindow();

            if (!IsHost)
            {
                CopyPlayerDataToUI();
                return;
            }

            if (e.User.IRCUser.Name != ProgramConstants.PLAYERNAME)
            {
                // Changing the map applies forced settings (co-op sides etc.) to the
                // new player, and it also sends an options broadcast message
                // CopyPlayerDataToUI(); This is also called by ChangeMap()
                ChangeMap(GameModeMap);
                BroadcastPlayerOptions();
                BroadcastPlayerExtraOptions();
                UpdateDiscordPresence();
            }
            else
            {
                Players[0].Ready = true;
                CopyPlayerDataToUI();
            }

            if (Players.Count >= playerLimit)
            {
                AddNotice("Player limit reached. The game room has been locked.".L10N("UI:Main:GameRoomNumberLimitReached"));
                LockGame();
            }
        }

        private void RemovePlayer(string playerName)
        {
            PlayerInfo pInfo = Players.Find(p => p.Name == playerName);

            if (pInfo != null)
            {
                Players.Remove(pInfo);

                CopyPlayerDataToUI();

                // This might not be necessary
                if (IsHost)
                    BroadcastPlayerOptions();
            }

            sndLeaveSound.Play();

            if (IsHost && Locked && !ProgramConstants.IsInGame)
            {
                UnlockGame(true);
            }
        }

        private void Channel_ChannelModesChanged(object sender, ChannelModeEventArgs e)
        {
            if (e.ModeString == "+i")
            {
                if (Players.Count >= playerLimit)
                    AddNotice("Player limit reached. The game room has been locked.".L10N("UI:Main:GameRoomNumberLimitReached"));
                else
                    AddNotice("The game host has locked the game room.".L10N("UI:Main:RoomLockedByHost"));
                Locked = true;
            }
            else if (e.ModeString == "-i")
            {
                AddNotice("The game room has been unlocked.".L10N("UI:Main:GameRoomUnlocked"));
                Locked = false;
            }
        }

        private void Channel_CTCPReceived(object sender, ChannelCTCPEventArgs e)
        {
            Logger.Log("CnCNetGameLobby_CTCPReceived");

            foreach (CommandHandlerBase cmdHandler in ctcpCommandHandlers)
            {
                if (cmdHandler.Handle(e.UserName, e.Message))
                {
                    UpdateDiscordPresence();
                    return;
                }
            }

            Logger.Log("Unhandled CTCP command: " + e.Message + " from " + e.UserName);
        }

        private void Channel_MessageAdded(object sender, IRCMessageEventArgs e)
        {
            if (cncnetUserData.IsIgnored(e.Message.SenderIdent))
            {
                lbChatMessages.AddMessage(new ChatMessage(Color.Silver,
                    string.Format("message blocked from {0}".L10N("UI:Main:MessageBlockedFromPlayer"), e.Message.SenderName)));
            }
            else
            {
                lbChatMessages.AddMessage(e.Message);

                if (e.Message.SenderName != null)
                    sndMessageSound.Play();
            }
        }

        /// <summary>
        /// Starts the game for the game host.
        /// </summary>
        protected override async void HostLaunchGame()
        {
            if (Players.Count > 1)
            {
                AddNotice("Contacting tunnel server...".L10N("UI:Main:ConnectingTunnel"));

                List<int> playerPorts = await tunnelHandler.CurrentTunnel.GetPlayerPortInfoAsync(Players.Count);

                if (playerPorts.Count < Players.Count)
                {
                    ShowTunnelSelectionWindow(("An error occured while contacting " +
                        "the CnCNet tunnel server." + Environment.NewLine +
                        "Try picking a different tunnel server:").L10N("UI:Main:ConnectTunnelError1"));
                    AddNotice(("An error occured while contacting the specified CnCNet " +
                        "tunnel server. Please try using a different tunnel server ").L10N("UI:Main:ConnectTunnelError2"), ERROR_MESSAGE_COLOR);
                    return;
                }

                StringBuilder sb = new StringBuilder("START ");
                sb.Append(UniqueGameID);
                for (int pId = 0; pId < Players.Count; pId++)
                {
                    Players[pId].Port = playerPorts[pId];
                    sb.Append(";");
                    sb.Append(Players[pId].Name);
                    sb.Append(";");
                    sb.Append("0.0.0.0:");
                    sb.Append(playerPorts[pId]);
                }
                channel.SendCTCPMessage(sb.ToString(), QueuedMessageType.SYSTEM_MESSAGE, 10);
            }
            else
            {
                Logger.Log("One player MP -- starting!");
            }

            Players.ForEach(pInfo => pInfo.IsInGame = true);
            CopyPlayerDataToUI();

            cncnetUserData.AddRecentPlayers(Players.Select(p => p.Name), channel.UIName);

            StartGame();
        }

        protected override void RequestPlayerOptions(int side, int color, int start, int team)
        {
            byte[] value = new byte[]
            {
                (byte)side,
                (byte)color,
                (byte)start,
                (byte)team
            };

            int intValue = BitConverter.ToInt32(value, 0);

            channel.SendCTCPMessage(
                string.Format("OR {0}", intValue),
                QueuedMessageType.GAME_SETTINGS_MESSAGE, 6);
        }

        protected override void RequestReadyStatus()
        {
            if (Map == null || GameMode == null)
            {
                AddNotice(("The game host needs to select a different map or " +
                    "you will be unable to participate in the match.").L10N("UI:Main:HostMustReplaceMap"));

                if (chkAutoReady.Checked)
                    channel.SendCTCPMessage("R 0", QueuedMessageType.GAME_PLAYERS_READY_STATUS_MESSAGE, 5);

                return;
            }

            PlayerInfo pInfo = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);
            if (pInfo == null)
                return;
            int readyState = 0;

            if (chkAutoReady.Checked)
                readyState = 2;
            else if (!pInfo.Ready)
                readyState = 1;

            channel.SendCTCPMessage($"R {readyState}", QueuedMessageType.GAME_PLAYERS_READY_STATUS_MESSAGE, 5);
        }

        protected override void AddNotice(string message, Color color) => channel.AddMessage(new ChatMessage(color, message));

        /// <summary>
        /// Handles player option requests received from non-host players.
        /// </summary>
        private void HandleOptionsRequest(string playerName, int options)
        {
            if (!IsHost)
                return;

            if (ProgramConstants.IsInGame)
                return;

            PlayerInfo pInfo = Players.Find(p => p.Name == playerName);

            if (pInfo == null)
                return;

            byte[] bytes = BitConverter.GetBytes(options);

            int side = bytes[0];
            int color = bytes[1];
            int start = bytes[2];
            int team = bytes[3];

            if (side < 0 || side > SideCount + RandomSelectorCount)
                return;

            if (color < 0 || color > MPColors.Count)
                return;

            var disallowedSides = GetDisallowedSides();

            if (side > 0 && side <= SideCount && disallowedSides[side - 1])
                return;

            if (Map?.CoopInfo != null)
            {
                if (Map.CoopInfo.DisallowedPlayerSides.Contains(side - 1) || side == SideCount + RandomSelectorCount)
                    return;

                if (Map.CoopInfo.DisallowedPlayerColors.Contains(color - 1))
                    return;
            }

            if (start < 0 || start > Map?.MaxPlayers)
                return;

            if (team < 0 || team > 4)
                return;

            if (side != pInfo.SideId
                || start != pInfo.StartingLocation
                || team != pInfo.TeamId)
            {
                ClearReadyStatuses();
            }

            pInfo.SideId = side;
            pInfo.ColorId = color;
            pInfo.StartingLocation = start;
            pInfo.TeamId = team;

            CopyPlayerDataToUI();
            BroadcastPlayerOptions();
        }

        /// <summary>
        /// Handles "I'm ready" messages received from non-host players.
        /// </summary>
        private void HandleReadyRequest(string playerName, int readyStatus)
        {
            if (!IsHost)
                return;

            PlayerInfo pInfo = Players.Find(p => p.Name == playerName);

            if (pInfo == null)
                return;

            pInfo.Ready = readyStatus > 0;
            pInfo.AutoReady = readyStatus > 1;

            CopyPlayerDataToUI();
            BroadcastPlayerOptions();
        }

        /// <summary>
        /// Broadcasts player options to non-host players.
        /// </summary>
        protected override void BroadcastPlayerOptions()
        {
            // Broadcast player options
            StringBuilder sb = new StringBuilder("PO ");
            foreach (PlayerInfo pInfo in Players.Concat(AIPlayers))
            {
                if (pInfo.IsAI)
                    sb.Append(pInfo.AILevel);
                else
                    sb.Append(pInfo.Name);
                sb.Append(";");

                // Combine the options into one integer to save bandwidth in
                // cases where the player uses default options (this is common for AI players)
                // Will hopefully make GameSurge kicking people a bit less common
                byte[] byteArray = new byte[]
                {
                    (byte)pInfo.TeamId,
                    (byte)pInfo.StartingLocation,
                    (byte)pInfo.ColorId,
                    (byte)pInfo.SideId,
                };

                int value = BitConverter.ToInt32(byteArray, 0);
                sb.Append(value);
                sb.Append(";");
                if (!pInfo.IsAI)
                {
                    if (pInfo.AutoReady && !pInfo.IsInGame && !LastMapChangeWasInvalid)
                        sb.Append(2);
                    else
                        sb.Append(Convert.ToInt32(pInfo.Ready));
                    sb.Append(';');
                }
            }

            channel.SendCTCPMessage(sb.ToString(), QueuedMessageType.GAME_PLAYERS_MESSAGE, 11);
        }

        protected override void PlayerExtraOptions_OptionsChanged(object sender, EventArgs e)
        {
            base.PlayerExtraOptions_OptionsChanged(sender, e);
            BroadcastPlayerExtraOptions();
        }

        protected override void BroadcastPlayerExtraOptions()
        {
            if (!IsHost)
                return;

            var playerExtraOptions = GetPlayerExtraOptions();

            channel.SendCTCPMessage(playerExtraOptions.ToCncnetMessage(), QueuedMessageType.GAME_PLAYERS_EXTRA_MESSAGE, 11, true);
        }

        /// <summary>
        /// Handles player option messages received from the game host.
        /// </summary>
        private void ApplyPlayerOptions(string sender, string message)
        {
            if (sender != hostName)
                return;

            Players.Clear();
            AIPlayers.Clear();

            string[] parts = message.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length;)
            {
                PlayerInfo pInfo = new PlayerInfo();

                string pName = parts[i];
                int converted = Conversions.IntFromString(pName, -1);

                if (converted > -1)
                {
                    pInfo.IsAI = true;
                    pInfo.AILevel = converted;
                    pInfo.Name = AILevelToName(converted);
                }
                else
                {
                    pInfo.Name = pName;

                    // If we can't find the player from the channel user list,
                    // ignore the player
                    // They've either left the channel or got kicked before the
                    // player options message reached us
                    if (channel.Users.Find(pName) == null)
                    {
                        i += HUMAN_PLAYER_OPTIONS_LENGTH;
                        continue;
                    }
                }

                if (parts.Length <= i + 1)
                    return;

                int playerOptions = Conversions.IntFromString(parts[i + 1], -1);
                if (playerOptions == -1)
                    return;

                byte[] byteArray = BitConverter.GetBytes(playerOptions);

                int team = byteArray[0];
                int start = byteArray[1];
                int color = byteArray[2];
                int side = byteArray[3];

                if (side < 0 || side > SideCount + RandomSelectorCount)
                    return;

                if (color < 0 || color > MPColors.Count)
                    return;

                if (start < 0 || start > MAX_PLAYER_COUNT)
                    return;

                if (team < 0 || team > 4)
                    return;

                pInfo.TeamId = byteArray[0];
                pInfo.StartingLocation = byteArray[1];
                pInfo.ColorId = byteArray[2];
                pInfo.SideId = byteArray[3];

                if (pInfo.IsAI)
                {
                    pInfo.Ready = true;
                    AIPlayers.Add(pInfo);
                    i += AI_PLAYER_OPTIONS_LENGTH;
                }
                else
                {
                    if (parts.Length <= i + 2)
                        return;

                    int readyStatus = Conversions.IntFromString(parts[i + 2], -1);

                    if (readyStatus == -1)
                        return;

                    pInfo.Ready = readyStatus > 0;
                    pInfo.AutoReady = readyStatus > 1;
                    if (pInfo.Name == ProgramConstants.PLAYERNAME)
                        btnLaunchGame.Text = pInfo.Ready ? BTN_LAUNCH_NOT_READY : BTN_LAUNCH_READY;

                    Players.Add(pInfo);
                    i += HUMAN_PLAYER_OPTIONS_LENGTH;
                }
            }

            CopyPlayerDataToUI();
        }

        /// <summary>
        /// Broadcasts GameOptions to non-host players
        /// when the host has changed an option.
        /// </summary>
        protected override void OnGameOptionChanged()
        {
            base.OnGameOptionChanged();

            if (!IsHost)
                return;

            bool[] optionValues = new bool[CheckBoxes.Count];
            for (int i = 0; i < CheckBoxes.Count; i++)
                optionValues[i] = CheckBoxes[i].Checked;

            // Let's pack the booleans into bytes
            List<byte> byteList = Conversions.BoolArrayIntoBytes(optionValues).ToList();

            while (byteList.Count % 4 != 0)
                byteList.Add(0);

            int integerCount = byteList.Count / 4;
            byte[] byteArray = byteList.ToArray();

            ExtendedStringBuilder sb = new ExtendedStringBuilder("GO ", true, ';');

            for (int i = 0; i < integerCount; i++)
                sb.Append(BitConverter.ToInt32(byteArray, i * 4));

            // We don't gain much in most cases by packing the drop-down values
            // (because they're bytes to begin with, and usually non-zero),
            // so let's just transfer them as usual

            foreach (GameLobbyDropDown dd in DropDowns)
                sb.Append(dd.SelectedIndex);

            sb.Append(Convert.ToInt32(Map?.Official ?? false));
            sb.Append(Map?.SHA1 ?? string.Empty);
            sb.Append(GameMode?.Name ?? string.Empty);
            sb.Append(FrameSendRate);
            sb.Append(MaxAhead);
            sb.Append(ProtocolVersion);
            sb.Append(RandomSeed);
            sb.Append(Convert.ToInt32(RemoveStartingLocations));
            sb.Append(Map?.Name ?? string.Empty);

            channel.SendCTCPMessage(sb.ToString(), QueuedMessageType.GAME_SETTINGS_MESSAGE, 11);
        }

        /// <summary>
        /// Handles game option messages received from the game host.
        /// </summary>
        private void ApplyGameOptions(string sender, string message)
        {
            if (sender != hostName)
                return;

            string[] parts = message.Split(';');

            int checkBoxIntegerCount = (CheckBoxes.Count / 32) + 1;

            int partIndex = checkBoxIntegerCount + DropDowns.Count;

            if (parts.Length < partIndex + 6)
            {
                AddNotice(("The game host has sent an invalid GameOptions message! " +
                    "The game host's game version might be different from yours.").L10N("UI:Main:HostGameOptionInvalid"), Color.Red);
                return;
            }

            string mapOfficial = parts[partIndex];
            bool isMapOfficial = Conversions.BooleanFromString(mapOfficial, true);

            string mapSHA1 = parts[partIndex + 1];

            string gameMode = parts[partIndex + 2];

            int frameSendRate = Conversions.IntFromString(parts[partIndex + 3], FrameSendRate);
            if (frameSendRate != FrameSendRate)
            {
                FrameSendRate = frameSendRate;
                AddNotice(string.Format("The game host has changed FrameSendRate (order lag) to {0}".L10N("UI:Main:HostChangeFrameSendRate"), frameSendRate));
            }

            int maxAhead = Conversions.IntFromString(parts[partIndex + 4], MaxAhead);
            if (maxAhead != MaxAhead)
            {
                MaxAhead = maxAhead;
                AddNotice(string.Format("The game host has changed MaxAhead to {0}".L10N("UI:Main:HostChangeMaxAhead"), maxAhead));
            }

            int protocolVersion = Conversions.IntFromString(parts[partIndex + 5], ProtocolVersion);
            if (protocolVersion != ProtocolVersion)
            {
                ProtocolVersion = protocolVersion;
                AddNotice(string.Format("The game host has changed ProtocolVersion to {0}".L10N("UI:Main:HostChangeProtocolVersion"), protocolVersion));
            }

            string mapName = parts[partIndex + 8];
            GameModeMap currentGameModeMap = GameModeMap;

            lastGameMode = gameMode;
            lastMapSHA1 = mapSHA1;
            lastMapName = mapName;

            GameModeMap = GameModeMaps.Find(gmm => gmm.GameMode.Name == gameMode && gmm.Map.SHA1 == mapSHA1);
            if (GameModeMap == null)
            {
                ChangeMap(null);

                if (!string.IsNullOrEmpty(mapSHA1))
                {
                    //if (!isMapOfficial)
                        RequestMap(mapSHA1);
                    //else
                    //    ShowOfficialMapMissingMessage(mapSHA1);
                }
            }
            else if (GameModeMap != currentGameModeMap)
            {
                ChangeMap(GameModeMap);
            }

            // By changing the GameOptions after changing the map, we know which
            // GameOptions were changed by the map and which were changed by the game host

            // If the map doesn't exist on the local installation, it's impossible
            // to know which options were set by the host and which were set by the
            // map, so we'll just assume that the host has set all the options.
            // Very few (if any) custom maps force options, so it'll be correct nearly always

            for (int i = 0; i < checkBoxIntegerCount; i++)
            {
                if (parts.Length <= i)
                    return;

                int checkBoxStatusInt;
                bool success = int.TryParse(parts[i], out checkBoxStatusInt);

                if (!success)
                {
                    AddNotice(("Failed to parse check box options sent by game host!" +
                        "The game host's game version might be different from yours.").L10N("UI:Main:HostCheckBoxParseError"), Color.Red);
                    return;
                }

                byte[] byteArray = BitConverter.GetBytes(checkBoxStatusInt);
                bool[] boolArray = Conversions.BytesIntoBoolArray(byteArray);

                for (int optionIndex = 0; optionIndex < boolArray.Length; optionIndex++)
                {
                    int gameOptionIndex = i * 32 + optionIndex;

                    if (gameOptionIndex >= CheckBoxes.Count)
                        break;

                    GameLobbyCheckBox checkBox = CheckBoxes[gameOptionIndex];

                    if (checkBox.Checked != boolArray[optionIndex])
                    {
                        if (boolArray[optionIndex])
                            AddNotice(string.Format("The game host has enabled {0}".L10N("UI:Main:HostEnableOption"), checkBox.Text));
                        else
                            AddNotice(string.Format("The game host has disabled {0}".L10N("UI:Main:HostDisableOption"), checkBox.Text));
                    }

                    CheckBoxes[gameOptionIndex].Checked = boolArray[optionIndex];
                }
            }

            for (int i = checkBoxIntegerCount; i < DropDowns.Count + checkBoxIntegerCount; i++)
            {
                if (parts.Length <= i)
                {
                    AddNotice(("The game host has sent an invalid GameOptions message! " +
                    "The game host's game version might be different from yours.").L10N("UI:Main:HostGameOptionInvalid"), Color.Red);
                    return;
                }

                int ddSelectedIndex;
                bool success = int.TryParse(parts[i], out ddSelectedIndex);

                if (!success)
                {
                    AddNotice(("Failed to parse drop down options sent by game host (2)! " +
                        "The game host's game version might be different from yours.").L10N("UI:Main:HostGameOptionInvalidTheSecondTime"), Color.Red);
                    return;
                }

                GameLobbyDropDown dd = DropDowns[i - checkBoxIntegerCount];

                if (ddSelectedIndex < -1 || ddSelectedIndex >= dd.Items.Count)
                    continue;

                if (dd.SelectedIndex != ddSelectedIndex)
                {
                    string ddName = dd.OptionName;
                    if (dd.OptionName == null)
                        ddName = dd.Name;

                    AddNotice(string.Format("The game host has set {0} to {1}".L10N("UI:Main:HostSetOption"), ddName, dd.Items[ddSelectedIndex].Text));
                }

                DropDowns[i - checkBoxIntegerCount].SelectedIndex = ddSelectedIndex;
            }

            int randomSeed;
            bool parseSuccess = int.TryParse(parts[partIndex + 6], out randomSeed);

            if (!parseSuccess)
            {
                AddNotice(("Failed to parse random seed from GameOptions message! " +
                    "The game host's game version might be different from yours.").L10N("UI:Main:HostRandomSeedError"), Color.Red);
            }

            bool removeStartingLocations = Convert.ToBoolean(Conversions.IntFromString(parts[partIndex + 7],
                Convert.ToInt32(RemoveStartingLocations)));
            SetRandomStartingLocations(removeStartingLocations);

            RandomSeed = randomSeed;
        }

        private void RequestMap(string mapSHA1)
        {
            if (UserINISettings.Instance.EnableMapSharing)
            {
                AddNotice("The game host has selected a map that doesn't exist on your installation.".L10N("UI:Main:MapNotExist"));
                mapSharingConfirmationPanel.ShowForMapDownload();
            }
            else
            {
                AddNotice("The game host has selected a map that doesn't exist on your installation.".L10N("UI:Main:MapNotExist") + " " +
                    ("Because you've disabled map sharing, it cannot be transferred. The game host needs " +
                    "to change the map or you will be unable to participate in the match.").L10N("UI:Main:MapSharingDisabledNotice"));
                channel.SendCTCPMessage(MAP_SHARING_DISABLED_MESSAGE, QueuedMessageType.SYSTEM_MESSAGE, 9);
            }
        }

        private void ShowOfficialMapMissingMessage(string sha1)
        {
            AddNotice(("The game host has selected an official map that doesn't exist on your installation. " +
                "This could mean that the game host has modified game files, or is running a different game version. " +
                "They need to change the map or you will be unable to participate in the match.").L10N("UI:Main:OfficialMapNotExist"));
            channel.SendCTCPMessage(MAP_SHARING_FAIL_MESSAGE + " " + sha1, QueuedMessageType.SYSTEM_MESSAGE, 9);
        }

        private void MapSharingConfirmationPanel_MapDownloadConfirmed(object sender, EventArgs e)
        {
            //Logger.Log("Map sharing confirmed.");
            //AddNotice("Attempting to download map.".L10N("UI:Main:DownloadingMap"));
            //mapSharingConfirmationPanel.SetDownloadingStatus();
            //MapSharer.DownloadMap(lastMapSHA1, localGame, lastMapName);
            //string messageBody = ProgramConstants.MAP_DOWNLOAD_NOTICE + " " + contextMenuData.inviteChannelName + ";" + contextMenuData.inviteGameName;

            string messageBody = ProgramConstants.MAP_DOWNLOAD_NOTICE +" " + $"{ProgramConstants.PLAYERNAME}";
            connectionManager.SendCustomMessage(new QueuedMessage(
                "PRIVMSG " + hostName + " :\u0001" + messageBody + "\u0001", QueuedMessageType.CHAT_MESSAGE, 0
            ));
            //Console.WriteLine(messageBody);
            //Console.WriteLine("PRIVMSG " + hostName + " :\u0001" + messageBody + "\u0001");
        }

        protected override void ChangeMap(GameModeMap gameModeMap)
        {
            if(gameModeMap!=null)
                mapSharingConfirmationPanel.Disable();
            base.ChangeMap(gameModeMap);
        }

        /// <summary>
        /// Signals other players that the local player has returned from the game,
        /// and unlocks the game as well as generates a new random seed as the game host.
        /// </summary>
        protected override void GameProcessExited()
        {
            ResetGameState();
        }

        protected void GameStartAborted()
        {
            ResetGameState();
        }

        protected void ResetGameState()
        {
            base.GameProcessExited();

            channel.SendCTCPMessage("RETURN", QueuedMessageType.SYSTEM_MESSAGE, 20);
            ReturnNotification(ProgramConstants.PLAYERNAME);

            if (IsHost)
            {
                RandomSeed = random.Next();
                OnGameOptionChanged();
                ClearReadyStatuses();
                CopyPlayerDataToUI();
                BroadcastPlayerOptions();
                BroadcastPlayerExtraOptions();

                if (Players.Count < playerLimit)
                    UnlockGame(true);
            }
        }

        /// <summary>
        /// Handles the "START" (game start) command sent by the game host.
        /// </summary>
        private void NonHostLaunchGame(string sender, string message)
        {
            if (sender != hostName)
                return;

            if (Map == null)
            {
                GameStartAborted();
                return;
            }

            string[] parts = message.Split(';');

            if (parts.Length < 1)
                return;

            UniqueGameID = Conversions.IntFromString(parts[0], -1);
            if (UniqueGameID < 0)
                return;

            var recentPlayers = new List<string>();

            for (int i = 1; i < parts.Length; i += 2)
            {
                if (parts.Length <= i + 1)
                    return;

                string pName = parts[i];
                string[] ipAndPort = parts[i + 1].Split(':');

                if (ipAndPort.Length < 2)
                    return;

                int port;
                bool success = int.TryParse(ipAndPort[1], out port);

                if (!success)
                    return;

                PlayerInfo pInfo = Players.Find(p => p.Name == pName);

                if (pInfo == null)
                    return;

                pInfo.Port = port;
                recentPlayers.Add(pName);
            }
            cncnetUserData.AddRecentPlayers(recentPlayers, channel.UIName);

            StartGame();
        }

        protected override void StartGame()
        {
            AddNotice("Starting game...".L10N("UI:Main:StartingGame"));

            FileHashCalculator fhc = new FileHashCalculator();
            fhc.CalculateHashes(GameModeMaps.GameModes);

            if (gameFilesHash != fhc.GetCompleteHash())
            {
                Logger.Log("Game files modified during client session!");
                channel.SendCTCPMessage(CHEAT_DETECTED_MESSAGE, QueuedMessageType.INSTANT_MESSAGE, 0);
                // HandleCheatDetectedMessage(ProgramConstants.PLAYERNAME);
            }
            
            channel.SendCTCPMessage("STRTD", QueuedMessageType.SYSTEM_MESSAGE, 20);

            base.StartGame();
        }

        protected override void WriteSpawnIniAdditions(IniFile iniFile)
        {
            base.WriteSpawnIniAdditions(iniFile);

            iniFile.SetValue("Tunnel", "Ip", tunnelHandler.CurrentTunnel.Address);
            iniFile.SetValue("Tunnel", "Port", tunnelHandler.CurrentTunnel.Port);

            iniFile.SetValue("Settings", "GameID", UniqueGameID);
            iniFile.SetValue("Settings", "Host", IsHost);

            PlayerInfo localPlayer = FindLocalPlayer();

            if (localPlayer == null)
                return;

            iniFile.SetValue("Settings", "Port", localPlayer.Port);
        }

        protected override void SendChatMessage(string message) => channel.SendChatMessage(message, chatColor);

        #region Notifications

        private void HandleNotification(string sender, Action handler)
        {
            if (sender != hostName)
                return;

            handler();
        }

        private void HandleIntNotification(string sender, int parameter, Action<int> handler)
        {
            if (sender != hostName)
                return;

            handler(parameter);
        }

        protected override void GetReadyNotification()
        {
            base.GetReadyNotification();
            WindowManager.FlashWindow();
            TopBar.SwitchToPrimary();

            if (IsHost)
                channel.SendCTCPMessage("GETREADY", QueuedMessageType.GAME_GET_READY_MESSAGE, 0);
        }

        protected override void AISpectatorsNotification()
        {
            base.AISpectatorsNotification();

            if (IsHost)
                channel.SendCTCPMessage("AISPECS", QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
        }

        protected override void InsufficientPlayersNotification()
        {
            base.InsufficientPlayersNotification();

            if (IsHost)
                channel.SendCTCPMessage("INSFSPLRS", QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
        }

        protected override void TooManyPlayersNotification()
        {
            base.TooManyPlayersNotification();

            if (IsHost)
                channel.SendCTCPMessage("TMPLRS", QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
        }

        protected override void SharedColorsNotification()
        {
            base.SharedColorsNotification();

            if (IsHost)
                channel.SendCTCPMessage("CLRS", QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
        }

        protected override void SharedStartingLocationNotification()
        {
            base.SharedStartingLocationNotification();

            if (IsHost)
                channel.SendCTCPMessage("SLOC", QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
        }

        protected override void LockGameNotification()
        {
            base.LockGameNotification();

            if (IsHost)
                channel.SendCTCPMessage("LCKGME", QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
        }

        protected override void NotVerifiedNotification(int playerIndex)
        {
            base.NotVerifiedNotification(playerIndex);

            if (IsHost)
                channel.SendCTCPMessage("NVRFY " + playerIndex, QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
        }

        protected override void StillInGameNotification(int playerIndex)
        {
            base.StillInGameNotification(playerIndex);

            if (IsHost)
                channel.SendCTCPMessage("INGM " + playerIndex, QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
        }
        
        private void GameStartedNotification(string sender)
        {
            PlayerInfo pInfo = Players.Find(p => p.Name == sender);

            if (pInfo != null)
                pInfo.IsInGame = true;

            CopyPlayerDataToUI();
        }

        private void ReturnNotification(string sender)
        {
            AddNotice(string.Format("{0} has returned from the game.".L10N("UI:Main:PlayerReturned"), sender));

            PlayerInfo pInfo = Players.Find(p => p.Name == sender);

            if (pInfo != null)
                pInfo.IsInGame = false;

            sndReturnSound.Play();
            CopyPlayerDataToUI();
        }

        private void HandleTunnelPing(string sender, int ping)
        {
            PlayerInfo pInfo = Players.Find(p => p.Name.Equals(sender));
            if (pInfo != null)
            {
                pInfo.Ping = ping;
                UpdatePlayerPingIndicator(pInfo);
            }
        }

        private void FileHashNotification(string sender, string filesHash)
        {
            if (!IsHost)
                return;

            PlayerInfo pInfo = Players.Find(p => p.Name == sender);

            if (pInfo != null)
                pInfo.HashReceived = true;
            CopyPlayerDataToUI();

            /* 暂不对比文件差异 */

            //if (filesHash != gameFilesHash)
            //{
            //    channel.SendCTCPMessage("MM " + sender, QueuedMessageType.GAME_CHEATER_MESSAGE, 10);
            //    CheaterNotification(ProgramConstants.PLAYERNAME, sender);
            //}
        }

        private void CheaterNotification(string sender, string cheaterName)
        {
            if (sender != hostName)
                return;

            AddNotice(string.Format("Player {0} has different files compared to the game host. Either {0} or the game host could be cheating.".L10N("UI:Main:DifferentFileCheating"), cheaterName), Color.Red);
        }

        protected override void BroadcastDiceRoll(int dieSides, int[] results)
        {
            string resultString = string.Join(",", results);
            channel.SendCTCPMessage($"{DICE_ROLL_MESSAGE} {dieSides},{resultString}", QueuedMessageType.CHAT_MESSAGE, 0);
            PrintDiceRollResult(ProgramConstants.PLAYERNAME, dieSides, results);
        }

        #endregion

        protected override void HandleLockGameButtonClick()
        {

            if (!Locked)
            {
                AddNotice("You've locked the game room.".L10N("UI:Main:RoomLockedByYou"));
                LockGame();
            }
            else
            {
                if (Players.Count < playerLimit)
                {
                    AddNotice("You've unlocked the game room.".L10N("UI:Main:RoomUnockedByYou"));
                    UnlockGame(false);
                }
                else
                    AddNotice(string.Format(
                        "Cannot unlock game; the player limit ({0}) has been reached.".L10N("UI:Main:RoomCantUnlockAsLimit"), playerLimit));
            }
        }

        protected override void LockGame()
        {
            connectionManager.SendCustomMessage(new QueuedMessage(
                string.Format("MODE {0} +i", channel.ChannelName), QueuedMessageType.INSTANT_MESSAGE, -1));


            Locked = true;
            btnLockGame.Text = "Unlock Game".L10N("UI:Main:UnlockGame");
            AccelerateGameBroadcasting();
        }

        protected override void UnlockGame(bool announce)
        {
            connectionManager.SendCustomMessage(new QueuedMessage(
                string.Format("MODE {0} -i", channel.ChannelName), QueuedMessageType.INSTANT_MESSAGE, -1));

            Locked = false;
            if (announce)
                AddNotice("The game room has been unlocked.".L10N("UI:Main:GameRoomUnlocked"));
            btnLockGame.Text = "Lock Game".L10N("UI:Main:LockGame");
            AccelerateGameBroadcasting();
        }

        protected override void KickPlayer(int playerIndex)
        {
            if (playerIndex >= Players.Count)
                return;

            var pInfo = Players[playerIndex];

            AddNotice(string.Format("Kicking {0} from the game...".L10N("UI:Main:KickPlayer"), pInfo.Name));
            channel.SendKickMessage(pInfo.Name, 8);
        }

        protected override void BanPlayer(int playerIndex)
        {
            if (playerIndex >= Players.Count)
                return;

            var pInfo = Players[playerIndex];

            var user = connectionManager.UserList.Find(u => u.Name == pInfo.Name);

            if (user != null)
            {
                AddNotice(string.Format("Banning and kicking {0} from the game...".L10N("UI:Main:BanAndKickPlayer"), pInfo.Name));
                channel.SendBanMessage(user.Hostname, priority: 8);
                channel.SendKickMessage(user.Name, priority: 8);
            }
        }

        private void GhostBanIgnoredUser(string playerName)
        {
            var user = connectionManager.UserList.Find(u => u.Name == playerName);

            if (user != null)
            {
                // Informing the host like we do when we kick might be annoying. So keep it on the downlow.
                channel.SendBanMessage(user.Hostname, priority: 8);
                channel.SendKickMessage(user.Name, priority: 8);
            }
        }

        private void HandleCheatDetectedMessage(string sender) =>
            AddNotice(string.Format("{0} has modified game files during the client session. They are likely attempting to cheat!".L10N("UI:Main:PlayerModifyFileCheat"), sender), Color.Red);

        private void HandleGameNameChangeMessage(string sender, string newGameName)
        {
            AddNotice(String.Format("The game host has changed the game name to {0}".L10N("UI:Main:HostGameNameChanged"), newGameName));
            channel.UIName = newGameName;
        }

        private void HandleTunnelServerChangeMessage(string sender, string tunnelAddressAndPort)
        {
            if (sender != hostName)
                return;

             string tunnelAddress; 
             int tunnelPort; 
  
             if (tunnelAddressAndPort.Count(c => c == ':') > 1) 
             { 
                 // IPv6 address 
                 int lastColonIndex = tunnelAddressAndPort.LastIndexOf(':'); 
                 tunnelAddress = tunnelAddressAndPort.Substring(0, lastColonIndex); 
                 tunnelPort = int.Parse(tunnelAddressAndPort.Substring(lastColonIndex + 1)); 
             } 
             else 
             { 
                 // IPv4 address or hostname 
                 string[] detailedAddress = tunnelAddressAndPort.Split(':'); 
                 tunnelAddress = detailedAddress[0]; 
                 tunnelPort = int.Parse(detailedAddress[1]); 
             }

            CnCNetTunnel tunnel = tunnelHandler.Tunnels.Find(t => t.Address == tunnelAddress && t.Port == tunnelPort);
            if (tunnel == null)
            {
                AddNotice(("The game host has selected an invalid tunnel server! " +
                    "The game host needs to change the server or you will be unable " +
                    "to participate in the match.").L10N("UI:Main:HostInvalidTunnel"),
                    Color.Yellow);
                btnLaunchGame.AllowClick = false;
                return;
            }

            HandleTunnelServerChange(tunnel);
            btnLaunchGame.AllowClick = true;
        }

        /// <summary>
        /// Changes the tunnel server used for the game.
        /// </summary>
        /// <param name="tunnel">The new tunnel server to use.</param>
        private void HandleTunnelServerChange(CnCNetTunnel tunnel)
        {
            tunnelHandler.CurrentTunnel = tunnel;
            AddNotice(string.Format("The game host has changed the tunnel server to: {0}".L10N("UI:Main:HostChangeTunnel"), tunnel.Name));
            UpdatePing();
        }

        #region CnCNet map sharing

        private void MapSharer_MapDownloadFailed(object sender, SHA1EventArgs e)
            => WindowManager.AddCallback(new Action<SHA1EventArgs>(MapSharer_HandleMapDownloadFailed), e);

        private void MapSharer_HandleMapDownloadFailed(SHA1EventArgs e)
        {
            // If the host has already uploaded the map, we shouldn't request them to re-upload it
            if (hostUploadedMaps.Contains(e.SHA1))
            {
                AddNotice("Download_Notice of the custom map failed. The host needs to change the map or you will be unable to participate in this match.".L10N("UI:Main:DownloadCustomMapFailed"));
                mapSharingConfirmationPanel.SetFailedStatus();

                channel.SendCTCPMessage(MAP_SHARING_FAIL_MESSAGE + " " + e.SHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);
                return;
            }
            else if (chatCommandDownloadedMaps.Contains(e.SHA1))
            {
                // Notify the user that their chat command map download failed.
                // Do not notify other users with a CTCP message as this is irrelevant to them.
                AddNotice("Downloading map via chat command has failed. Check the map ID and try again.".L10N("UI:Main:DownloadMapCommandFailedGeneric"));
                mapSharingConfirmationPanel.SetFailedStatus();
                return;
            }

            AddNotice("Requesting the game host to upload the map to the CnCNet map database.".L10N("UI:Main:RequestHostUploadMapToDB"));

            channel.SendCTCPMessage(MAP_SHARING_UPLOAD_REQUEST + " " + e.SHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);
        }

        private void MapSharer_MapDownloadComplete(object sender, SHA1EventArgs e) =>
            WindowManager.AddCallback(new Action<SHA1EventArgs>(MapSharer_HandleMapDownloadComplete), e);

        private void MapSharer_HandleMapDownloadComplete(SHA1EventArgs e)
        {
            string mapFileName = MapSharer.GetMapFileName(e.SHA1, e.MapName);
            Logger.Log("Map " + mapFileName + " downloaded, parsing.");
            string mapPath = "Maps/Multi/Custom/" + mapFileName;
            Map map = MapLoader.LoadCustomMap(mapPath, out string returnMessage);
            if (map != null)
            {
                AddNotice(returnMessage);
                if (lastMapSHA1 == e.SHA1)
                {
                    GameModeMap = GameModeMaps.Find(gmm => gmm.Map.SHA1 == lastMapSHA1);
                    ChangeMap(GameModeMap);
                }
            }
            else if (chatCommandDownloadedMaps.Contains(e.SHA1))
            {
                // Somehow the user has managed to download an already existing sha1 hash.
                // This special case prevents user confusion from the file successfully downloading but showing an error anyway.
                AddNotice(returnMessage, Color.Yellow);
                AddNotice("Map was downloaded, but a duplicate is already loaded from a different filename. This may cause strange behavior.".L10N("UI:Main:DownloadMapCommandDuplicateMapFileLoaded"),
                    Color.Yellow);
            }
            else
            {
                AddNotice(returnMessage, Color.Red);
                AddNotice("Transfer of the custom map failed. The host needs to change the map or you will be unable to participate in this match.".L10N("UI:Main:MapTransferFailed"));
                mapSharingConfirmationPanel.SetFailedStatus();
                channel.SendCTCPMessage(MAP_SHARING_FAIL_MESSAGE + " " + e.SHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);
            }
        }

        private void MapSharer_MapUploadFailed(object sender, MapEventArgs e) =>
            WindowManager.AddCallback(new Action<MapEventArgs>(MapSharer_HandleMapUploadFailed), e);

        private void MapSharer_HandleMapUploadFailed(MapEventArgs e)
        {
            Map map = e.Map;

            hostUploadedMaps.Add(map.SHA1);

            AddNotice(string.Format("Uploading map {0} to the CnCNet map database failed.".L10N("UI:Main:UpdateMapToDBFailed"), map.Name));
            if (map == Map)
            {
                AddNotice("You need to change the map or some players won't be able to participate in this match.".L10N("UI:Main:YouMustReplaceMap"));
                channel.SendCTCPMessage(MAP_SHARING_FAIL_MESSAGE + " " + map.SHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);
            }
        }

        private void MapSharer_MapUploadComplete(object sender, MapEventArgs e) =>
            WindowManager.AddCallback(new Action<MapEventArgs>(MapSharer_HandleMapUploadComplete), e);

        private void MapSharer_HandleMapUploadComplete(MapEventArgs e)
        {
            hostUploadedMaps.Add(e.Map.SHA1);
            uploadingMaps.Remove(e.Map.SHA1);

            AddNotice(string.Format("Uploading map {0} to the CnCNet map database complete.".L10N("UI:Main:UpdateMapToDBSuccess"), e.Map.Name));
            if (e.Map == Map)
            {
                channel.SendCTCPMessage(MAP_SHARING_DOWNLOAD_REQUEST + " " + Map.SHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);
            }
        }

        private HashSet<string> uploadingMaps = new HashSet<string>();

        /// <summary>
        /// Handles a map upload request sent by a player.
        /// </summary>
        /// <param name="sender">The sender of the request.</param>
        /// <param name="mapSHA1">The SHA1 of the requested map.</param>
        private void HandleMapUploadRequest(string sender, string mapSHA1)
        {
            // 已上传或正在上传，直接通知请求者去服务器下载
            if (hostUploadedMaps.Contains(mapSHA1) || uploadingMaps.Contains(mapSHA1))
            {
                Logger.Log($"HandleMapUploadRequest: Map {mapSHA1} is already uploaded or uploading, notify {sender} to download from server.");
                channel.SendCTCPMessage(MAP_SHARING_DOWNLOAD_REQUEST + " " + mapSHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);
                return;
            }

            Map map = null;
            foreach (GameMode gm in GameModeMaps.GameModes)
            {
                map = gm.Maps.Find(m => m.SHA1 == mapSHA1);
                if (map != null)
                    break;
            }

            if (map == null)
            {
                Logger.Log("Unknown map upload request from " + sender + ": " + mapSHA1);
                return;
            }

            if (map.Official)
            {
                Logger.Log("HandleMapUploadRequest: Map is official, so skip request");
                AddNotice(string.Format(("{0} doesn't have the map '{1}' on their local installation. " +
                    "The map needs to be changed or {0} is unable to participate in the match.").L10N("UI:Main:PlayerMissingMap"),
                    sender, map.Name));
                return;
            }

            if (!IsHost)
                return;

            // 标记为正在上传
            uploadingMaps.Add(mapSHA1);

            AddNotice(string.Format(("{0} doesn't have the map '{1}' on their local installation. " +
                "Attempting to upload the map to the CnCNet map database.").L10N("UI:Main:UpdateMapToDBPrompt"),
                sender, map.Name));

            // 上传地图
            MapSharer.UploadMap(map, localGame);
        }

        /// <summary>
        /// Handles a map transfer failure message sent by either the player or the game host.
        /// </summary>
        private void HandleMapTransferFailMessage(string sender, string sha1)
        {
            if (sender == hostName)
            {
                AddNotice("The game host failed to upload the map to the CnCNet map database.".L10N("UI:Main:HostUpdateMapToDBFailed"));

                hostUploadedMaps.Add(sha1);

                if (lastMapSHA1 == sha1 && Map == null)
                {
                    AddNotice("The game host needs to change the map or you won't be able to participate in this match.".L10N("UI:Main:HostMustChangeMap"));
                }

                return;
            }

            if (lastMapSHA1 == sha1)
            {
                if (!IsHost)
                {
                    AddNotice(string.Format("{0} has failed to download the map from the CnCNet map database.".L10N("UI:Main:PlayerDownloadMapFailed") + " " +
                        "The host needs to change the map or {0} won't be able to participate in this match.".L10N("UI:Main:HostNeedChangeMapForPlayer"), sender));
                }
                else
                {
                    AddNotice(string.Format("{0} has failed to download the map from the CnCNet map database.".L10N("UI:Main:PlayerDownloadMapFailed") + " " +
                        "You need to change the map or {0} won't be able to participate in this match.".L10N("UI:Main:YouNeedChangeMapForPlayer"), sender));
                }
            }
        }

        private void HandleMapDownloadRequest(string sender, string sha1)
        {
            if (sender != hostName)
                return;

            hostUploadedMaps.Add(sha1);

            if (lastMapSHA1 == sha1 && Map == null)
            {
                Logger.Log("The game host has uploaded the map into the database. Re-attempting download...");
                MapSharer.DownloadMap(sha1, localGame, lastMapName);
            }
        }

        private void HandleMapSharingBlockedMessage(string sender)
        {
            AddNotice(string.Format("The selected map doesn't exist on {0}'s installation, and they " +
                "have map sharing disabled in settings. The game host needs to change to a non-custom map or " +
                "they will be unable to participate in this match.".L10N("UI:Main:PlayerMissingMaDisabledSharing"), sender));
        }

        /// <summary>
        /// Download_Notice a map from CNCNet using a map hash ID.
        ///
        /// Users and testers can get map hash IDs from this URL template:
        ///
        /// - http://mapdb.cncnet.org/search.php?game=GAME_ID&search=MAP_NAME_SEARCH_STRING
        ///
        /// </summary>
        /// <param name="parameters">
        /// This is a string beginning with the sha1 hash map ID, and (optionally) the name to use as a local filename for the map file.
        /// Every character after the first space will be treated as part of the map name.
        ///
        /// "?" characters are removed from the sha1 due to weird copy and paste behavior from the map search endpoint.
        /// </param>
        private void DownloadMapByIdCommand(string parameters)
        {
            string sha1;
            string mapName;
            string message;

            // Make sure no spaces at the beginning or end of the string will mess up arg parsing.
            parameters = parameters.Trim();
            // Check if the parameter's contain spaces.
            // The presence of spaces indicates a user-specified map name.
            int firstSpaceIndex = parameters.IndexOf(' ');

            if (firstSpaceIndex == -1)
            {
                // The user did not supply a map name.
                sha1 = parameters;
                mapName = "user_chat_command_download";
            }
            else
            {
                // User supplied a map name.
                sha1 = parameters.Substring(0, firstSpaceIndex);
                mapName = parameters.Substring(firstSpaceIndex + 1);
                mapName = mapName.Trim();
            }

            // Remove erroneous "?". These sneak in when someone double-clicks a map ID and copies it from the cncnet search endpoint.
            // There is some weird whitespace that gets copied to chat as a "?" at the end of the hash. It's hard to spot, so just hold the user's hand.
            sha1 = sha1.Replace("?", "");

            // See if the user already has this map, with any filename, before attempting to download it.
            GameModeMap loadedMap = GameModeMaps.Find(gmm => gmm.Map.SHA1 == sha1);

            if (loadedMap != null)
            {
                message = String.Format(
                    "The map for ID \"{0}\" is already loaded from \"{1}.map\", delete the existing file before trying again.".L10N("UI:Main:DownloadMapCommandSha1AlreadyExists"),
                    sha1,
                    loadedMap.Map.BaseFilePath);
                AddNotice(message, Color.Yellow);
                Logger.Log(message);
                return;
            }

            // Replace any characters that are not safe for filenames.
            char replaceUnsafeCharactersWith = '-';
            // Use a hashset instead of an array for quick lookups in `invalidChars.Contains()`.
            HashSet<char> invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());
            string safeMapName = new String(mapName.Select(c => invalidChars.Contains(c) ? replaceUnsafeCharactersWith : c).ToArray());

            chatCommandDownloadedMaps.Add(sha1);

            message = String.Format("Attempting to download map via chat command: sha1={0}, mapName={1}".L10N("UI:Main:DownloadMapCommandStartingDownload"), sha1, mapName);
            Logger.Log(message);
            AddNotice(message);

            MapSharer.DownloadMap(sha1, localGame, safeMapName);
        }

        #endregion

        #region Game broadcasting logic

        /// <summary>
        /// Handles changing the UIName
        /// </summary>
        /// <param name="gameName">The new name for the hosted game.</param>
        private void ChangeGameName(string gameName)
        {
            var gameNameValid = NameValidator.IsGameNameValid(gameName);

            if (!string.IsNullOrEmpty(gameNameValid))
            {
                XNAMessageBox.Show(WindowManager, "Invalid game name".L10N("UI:Main:GameNameInvalid"),
                    gameNameValid);
                return;
            }

            channel.UIName = gameName;

            // broadcast new name to all players
            AccelerateGameBroadcasting();

            // inform the players in the room
            channel.SendCTCPMessage(GAME_NAME_CHANGED + " " + gameName, QueuedMessageType.SYSTEM_MESSAGE, priority: 9);

            // inform ourself
            AddNotice(String.Format("Game name changed to {0}.".L10N("UI:Main:GameNameChanged"), gameName));
        }

        /// <summary>
        /// Lowers the time until the next game broadcasting message.
        /// </summary>
        private void AccelerateGameBroadcasting() =>
            gameBroadcastTimer.Accelerate(TimeSpan.FromSeconds(GAME_BROADCAST_ACCELERATION));

        private void BroadcastGame()
        {
            Channel broadcastChannel = connectionManager.FindChannel(gameCollection.GetGameBroadcastingChannelNameFromIdentifier(localGame));

            if (broadcastChannel == null)
                return;

            if (ProgramConstants.IsInGame && broadcastChannel.Users.Count > 500)
                return;

            StringBuilder sb = new StringBuilder("GAME ");
            sb.Append(ProgramConstants.CNCNET_PROTOCOL_REVISION);
            sb.Append(";");
            sb.Append(ProgramConstants.GAME_VERSION);
            sb.Append(";");
            sb.Append(playerLimit);
            sb.Append(";");
            sb.Append(channel.ChannelName);
            sb.Append(";");
            sb.Append(channel.UIName);
            sb.Append(";");
            if (Locked)
                sb.Append("1");
            else
                sb.Append("0");
            sb.Append(Convert.ToInt32(isCustomPassword));
            sb.Append(Convert.ToInt32(closed));
            sb.Append("0"); // IsLoadedGame
            sb.Append("0"); // IsLadder
            sb.Append(";");
            foreach (PlayerInfo pInfo in Players)
            {
                sb.Append(pInfo.Name);
                sb.Append(",");
            }

            sb.Remove(sb.Length - 1, 1);
            sb.Append(";");
            sb.Append(Map?.Name ?? string.Empty);
            sb.Append(";");
            sb.Append(GameMode?.UIName ?? string.Empty);
            sb.Append(";");
            sb.Append(tunnelHandler.CurrentTunnel.Address + ":" + tunnelHandler.CurrentTunnel.Port);
            sb.Append(";");
            sb.Append(0); // LoadedGameId
            sb.Append(";");
            sb.Append(skillLevel); // SkillLevel

            broadcastChannel.SendCTCPMessage(sb.ToString(), QueuedMessageType.SYSTEM_MESSAGE, 20);
        }

        #endregion

        public override string GetSwitchName() => "Game Lobby".L10N("UI:Main:GameLobby");
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using ClientCore;
using ClientGUI;
using Ra2Client.Domain.Multiplayer.CnCNet;
using Ra2Client.Online;
using Ra2Client.Online.EventArguments;
using DTAConfig;
using Localization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Rampastring.XNAUI;
using Rampastring.XNAUI.Input;
using Rampastring.XNAUI.XNAControls;
using DTAConfig.OptionPanels;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Diagnostics;
using System.IO;
using System.Timers;
using System.Threading.Tasks;
using ClientCore.Settings;
using ClientCore.Entity;
using Localization.Tools;

namespace Ra2Client.DXGUI.Generic
{
    /// <summary>
    /// A top bar that allows switching between various client windows.
    /// </summary>
    public class TopBar : XNAPanel
    {
        /// <summary>
        /// The number of seconds that the top bar will stay down after it has
        /// lost input focus.
        /// </summary>
        const double DOWN_TIME_WAIT_SECONDS = 1.0;
        const double EVENT_DOWN_TIME_WAIT_SECONDS = 2.0;
        const double STARTUP_DOWN_TIME_WAIT_SECONDS = 3.5;

        const double DOWN_MOVEMENT_RATE = 1.7;
        const double UP_MOVEMENT_RATE = 1.7;
        const int APPEAR_CURSOR_THRESHOLD_Y = 2;

        private readonly string DEFAULT_PM_BTN_LABEL = "Private Messages (F4)".L10N("UI:Main:PMButtonF4");

        public TopBar(
            WindowManager windowManager,
            CnCNetManager connectionManager,
            PrivateMessageHandler privateMessageHandler
        ) : base(windowManager)
        {
            downTimeWaitTime = TimeSpan.FromSeconds(DOWN_TIME_WAIT_SECONDS);
            this.connectionManager = connectionManager;
            this.privateMessageHandler = privateMessageHandler;
        }

        public SwitchType LastSwitchType { get; private set; }

        private List<ISwitchable> primarySwitches = new List<ISwitchable>();
        private ISwitchable cncnetLobbySwitch;
        private ISwitchable privateMessageSwitch;

        private OptionsWindow optionsWindow;

        private XNAClientButton btnMainButton;
        private XNAClientButton btnCnCNetLobby;
        private XNAClientButton btnPrivateMessages;
        private XNAClientButton btnModManager;
        private XNAClientButton btnOptions;
        private XNAClientButton btnLogout;
        private XNALabel lblTime;
        private XNALabel lblDate;
        private XNALabel lblCnCNetStatus;
        private XNALabel lblCnCNetPlayerCount;
        private XNALabel lblConnectionStatus;

        private MinesweeperGame minesweeperGameWindow;

        private CnCNetManager connectionManager;
        private readonly PrivateMessageHandler privateMessageHandler;

        private CancellationTokenSource cncnetPlayerCountCancellationSource;
        private static readonly object locker = new object();

        private TimeSpan downTime = TimeSpan.FromSeconds(DOWN_TIME_WAIT_SECONDS - STARTUP_DOWN_TIME_WAIT_SECONDS);

        private TimeSpan downTimeWaitTime;

        private bool isDown = true;

        private double locationY = -40.0;

        private bool lanMode;

        public EventHandler LogoutEvent;

        public void AddPrimarySwitchable(ISwitchable switchable)
        {
            primarySwitches.Add(switchable);
            btnMainButton.Text = switchable.GetSwitchName() + " (F2)";
            if (switchable.GetSwitchName() == "Game Lobby".L10N("UI:Main:GameLobby"))
                optionsWindow.tabControl.MakeUnselectable(4);
        }

        public void RemovePrimarySwitchable(ISwitchable switchable)
        {
            primarySwitches.Remove(switchable);
            btnMainButton.Text = primarySwitches[primarySwitches.Count - 1].GetSwitchName() + " (F2)";
        }

        public void SetSecondarySwitch(ISwitchable switchable)
            => cncnetLobbySwitch = switchable;

        public void SetTertiarySwitch(ISwitchable switchable)
            => privateMessageSwitch = switchable;

        public void SetOptionsWindow(OptionsWindow optionsWindow)
        {
            this.optionsWindow = optionsWindow;
            optionsWindow.EnabledChanged += OptionsWindow_EnabledChanged;
        }

        private void OptionsWindow_EnabledChanged(object sender, EventArgs e)
        {
            if (!lanMode)
                SetSwitchButtonsClickable(!optionsWindow.Enabled);

            SetOptionsButtonClickable(!optionsWindow.Enabled);

            if (optionsWindow != null)
                optionsWindow.ToggleMainMenuOnlyOptions(primarySwitches.Count == 1 && !lanMode);
        }

        private void MinesweeperGameWindow_EnabledChanged(object sender, EventArgs e)
        {
            if (!lanMode)
                SetSwitchButtonsClickable(!minesweeperGameWindow.Enabled);

            SetOptionsButtonClickable(!minesweeperGameWindow.Enabled);
        }

        public void Clean()
        {
            if (cncnetPlayerCountCancellationSource != null)
                cncnetPlayerCountCancellationSource.Cancel();
        }

        public override void Initialize()
        {
            Name = "TopBar";
            ClientRectangle = new Rectangle(0, -39, WindowManager.RenderResolutionX, 39);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            DrawBorders = false;
            btnMainButton = new XNAClientButton(WindowManager);
            btnMainButton.Name = nameof(btnMainButton);
            btnMainButton.ClientRectangle = new Rectangle(12, 9, UIDesignConstants.BUTTON_WIDTH_160, UIDesignConstants.BUTTON_HEIGHT);
            btnMainButton.Text = "Main Menu (F2)".L10N("UI:Main:MainMenuF2");
            btnMainButton.LeftClick += BtnMainButton_LeftClick;

            btnCnCNetLobby = new XNAClientButton(WindowManager);
            btnCnCNetLobby.Name = nameof(btnCnCNetLobby);
            btnCnCNetLobby.ClientRectangle = new Rectangle(184, 9, UIDesignConstants.BUTTON_WIDTH_160, UIDesignConstants.BUTTON_HEIGHT);
            btnCnCNetLobby.Text = "CnCNet Lobby (F3)".L10N("UI:Main:LobbyF3");
            btnCnCNetLobby.LeftClick += BtnCnCNetLobby_LeftClick;

            btnPrivateMessages = new XNAClientButton(WindowManager);
            btnPrivateMessages.Name = nameof(btnPrivateMessages);
            btnPrivateMessages.ClientRectangle = new Rectangle(356, 9, UIDesignConstants.BUTTON_WIDTH_160, UIDesignConstants.BUTTON_HEIGHT);
            btnPrivateMessages.Text = DEFAULT_PM_BTN_LABEL;
            btnPrivateMessages.LeftClick += BtnPrivateMessages_LeftClick;

            lblDate = new XNALabel(WindowManager);
            lblDate.Name = nameof(lblDate);
            lblDate.FontIndex = 1;
            lblDate.Text = Renderer.GetSafeString(DateTime.Now.ToShortDateString(), lblDate.FontIndex);
            lblDate.ClientRectangle = new Rectangle(Width -
                (int)Renderer.GetTextDimensions(lblDate.Text, lblDate.FontIndex).X - 12, 18,
                lblDate.Width, lblDate.Height);

            lblTime = new XNALabel(WindowManager);
            lblTime.Name = nameof(lblTime);
            lblTime.FontIndex = 1;
            lblTime.Text = Renderer.GetSafeString(new DateTime(1, 1, 1, 23, 59, 59).ToLongTimeString(), lblTime.FontIndex);
            lblTime.ClientRectangle = new Rectangle(Width -
                (int)Renderer.GetTextDimensions(lblTime.Text, lblTime.FontIndex).X - 12, 4,
                lblTime.Width, lblTime.Height);

            btnLogout = new XNAClientButton(WindowManager);
            btnLogout.Name = nameof(btnLogout);
            btnLogout.ClientRectangle = new Rectangle(lblDate.X - 87, 9, 75, 23);
            btnLogout.FontIndex = 1;
            btnLogout.Text = "Log Out".L10N("UI:Main:LogOut");
            btnLogout.AllowClick = false;
            btnLogout.LeftClick += BtnLogout_LeftClick;



            btnOptions = new XNAClientButton(WindowManager);
            btnOptions.Name = nameof(btnOptions);
            btnOptions.ClientRectangle = new Rectangle(btnLogout.X - 122, 9, 110, 23);
            btnOptions.Text = "Options (F12)".L10N("UI:Main:OptionsF12");
            btnOptions.LeftClick += BtnOptions_LeftClick;

            btnModManager = new XNAClientButton(WindowManager);
            btnModManager.Name = nameof(btnModManager);
            btnModManager.ClientRectangle = new Rectangle(btnOptions.X - 172, 9, UIDesignConstants.BUTTON_WIDTH_160, 23);
            btnModManager.Text = "ModManager (F11)".L10N("UI:Main:ModManagerF11");
            btnModManager.LeftClick += BtnModManager_LeftClick;

            //minesweeperGameWindow = new MinesweeperGame(WindowManager);
            //var dp = DarkeningPanel.AddAndInitializeWithControl(WindowManager, minesweeperGameWindow);
            //dp.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 0), 1, 1);

            //dp.LeftClick += (_, _) => { minesweeperGameWindow.Disable(); };

            //minesweeperGameWindow.ClientRectangle = new Rectangle(
            //    0, 
            //    0, 
            //    (38 + 2) * 21, 
            //    (25 + 4 + 2) * 21);
            ////每边空出一格 用于放边框

            //minesweeperGameWindow.CenterOnParent();
            
            //minesweeperGameWindow.Disable();

            minesweeperGameWindow = new MinesweeperGame(WindowManager);
            var dp = DarkeningPanel.AddAndInitializeWithControl(WindowManager, minesweeperGameWindow);
            dp.BackgroundTexture = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/dpbackground.png");
            // dp.ClientRectangle = new Rectangle(0, 0, (MinesweeperGame.Columns) * 21, (MinesweeperGame.Rows + 4) * 21);
            dp.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.CENTERED;
            dp.CenterOnParent();

            //dp.LeftClick += btnMinesweeper_LeftClick;
            //dp.DoubleLeftClick += btnMinesweeper_LeftClick;
            dp.LeftClick += (_, _) => { minesweeperGameWindow.Disable(); };

            minesweeperGameWindow.ClientRectangle = new Rectangle(
                0,
                0,
                (38 + 2) * 21,
                (25 + 4 + 2) * 21);
            //每边空出一格 用于放边框

            minesweeperGameWindow.CenterOnParent();
            minesweeperGameWindow.Disable();

            minesweeperGameWindow.EnabledChanged += MinesweeperGameWindow_EnabledChanged;

            lblConnectionStatus = new XNALabel(WindowManager);
            lblConnectionStatus.Name = "lblConnectionStatus";
            lblConnectionStatus.FontIndex = 1;
            lblConnectionStatus.Text = "CnCNet Status: OFFLINE".L10N("UI:Main:StatusOffline");

            AddChild(btnMainButton);
            AddChild(btnCnCNetLobby);
            AddChild(btnPrivateMessages);

            AddChild(btnOptions);
            AddChild(btnModManager);
            AddChild(lblTime);
            AddChild(lblDate);
            AddChild(btnLogout);
            AddChild(lblConnectionStatus);

            if (ClientConfiguration.Instance.DisplayPlayerCountInTopBar)
            {
                lblCnCNetStatus = new XNALabel(WindowManager);
                lblCnCNetStatus.Name = "lblCnCNetStatus";
                lblCnCNetStatus.FontIndex = 1;
                lblCnCNetStatus.Text = ClientConfiguration.Instance.LocalGame.ToUpper() + "在线玩家数:";
                lblCnCNetPlayerCount = new XNALabel(WindowManager);
                lblCnCNetPlayerCount.Name = "lblCnCNetPlayerCount";
                lblCnCNetPlayerCount.FontIndex = 1;
                lblCnCNetPlayerCount.Text = "-";
                lblCnCNetPlayerCount.ClientRectangle = new Rectangle(btnOptions.X - 50, 11, lblCnCNetPlayerCount.Width, lblCnCNetPlayerCount.Height);
                lblCnCNetStatus.ClientRectangle = new Rectangle(lblCnCNetPlayerCount.X - lblCnCNetStatus.Width - 6, 11, lblCnCNetStatus.Width, lblCnCNetStatus.Height);
                AddChild(lblCnCNetStatus);
                AddChild(lblCnCNetPlayerCount);
                CnCNetPlayerCountTask.CnCNetGameCountUpdated += CnCNetInfoController_CnCNetGameCountUpdated;
                cncnetPlayerCountCancellationSource = new CancellationTokenSource();
                CnCNetPlayerCountTask.InitializeService(cncnetPlayerCountCancellationSource);
            }

            lblConnectionStatus.CenterOnParent();

            base.Initialize();

            Keyboard.OnKeyPressed += Keyboard_OnKeyPressed;
            connectionManager.Connected += ConnectionManager_Connected;
            connectionManager.Disconnected += ConnectionManager_Disconnected;
            connectionManager.ConnectionLost += ConnectionManager_ConnectionLost;
            connectionManager.WelcomeMessageReceived += ConnectionManager_WelcomeMessageReceived;
            connectionManager.AttemptedServerChanged += ConnectionManager_AttemptedServerChanged;
            connectionManager.ConnectAttemptFailed += ConnectionManager_ConnectAttemptFailed;

            privateMessageHandler.UnreadMessageCountUpdated += PrivateMessageHandler_UnreadMessageCountUpdated;


        }


        private void BtnModManager_LeftClick(object sender, EventArgs e)
        {
            // 触发彩蛋();
            var modManager = ModManager.GetInstance(WindowManager);
            if (modManager.Enabled)
                return;
            var dp = DarkeningPanel.AddAndInitializeWithControl(WindowManager, modManager);

            modManager.DDModAI.SelectedIndex = 0;
            modManager.Enable();
            modManager.EnabledChanged += (_, _) =>
            {
                DarkeningPanel.RemoveControl(dp, WindowManager, modManager);
            };
        }

        private void PrivateMessageHandler_UnreadMessageCountUpdated(object sender, UnreadMessageCountEventArgs args)
            => UpdatePrivateMessagesBtnLabel(args.UnreadMessageCount);

        private void UpdatePrivateMessagesBtnLabel(int unreadMessageCount)
        {
            btnPrivateMessages.Text = DEFAULT_PM_BTN_LABEL;
            if (unreadMessageCount > 0)
            {
                
                // btnPrivateMessages.Text += $" ({unreadMessageCount})";
            }
        }

        private void CnCNetInfoController_CnCNetGameCountUpdated(object sender, PlayerCountEventArgs e)
        {
            lock (locker)
            {
                if (e.PlayerCount == -1)
                    lblCnCNetPlayerCount.Text = "N/A".L10N("UI:Main:N/A");
                else
                    lblCnCNetPlayerCount.Text = e.PlayerCount.ToString();
            }
        }

        private void ConnectionManager_ConnectionLost(object sender, Online.EventArguments.ConnectionLostEventArgs e)
        {
            if (!lanMode)
                ConnectionEvent("CnCNet Status: OFFLINE".L10N("UI:Main:StatusOffline"));
        }

        private void ConnectionManager_ConnectAttemptFailed(object sender, EventArgs e)
        {
            if (!lanMode)
                ConnectionEvent("CnCNet Status: OFFLINE".L10N("UI:Main:StatusOffline"));
        }

        private void ConnectionManager_AttemptedServerChanged(object sender, Online.EventArguments.AttemptedServerEventArgs e)
        {
            ConnectionEvent("CnCNet Status: CONNECTING...".L10N("UI:Main:StatusConnecting"));
            BringDown();
        }

        private void ConnectionManager_WelcomeMessageReceived(object sender, Online.EventArguments.ServerMessageEventArgs e)
            => ConnectionEvent("CnCNet Status: CONNECTED".L10N("UI:Main:StatusConnected"));

        private void ConnectionManager_Disconnected(object sender, EventArgs e)
        {
            btnLogout.AllowClick = false;
            if (!lanMode)
                ConnectionEvent("CnCNet Status: OFFLINE".L10N("UI:Main:StatusOffline"));
        }

        private void ConnectionEvent(string text)
        {
            lblConnectionStatus.Text = text;
            lblConnectionStatus.CenterOnParent();
            isDown = true;
            downTime = TimeSpan.FromSeconds(DOWN_TIME_WAIT_SECONDS - EVENT_DOWN_TIME_WAIT_SECONDS);
        }

        private void BtnLogout_LeftClick(object sender, EventArgs e)
        {
            connectionManager.Disconnect();
            LogoutEvent?.Invoke(this, null);
            SwitchToPrimary();
        }

        private void ConnectionManager_Connected(object sender, EventArgs e)
            => btnLogout.AllowClick = true;

        public void SwitchToPrimary()
            => BtnMainButton_LeftClick(this, EventArgs.Empty);

        public ISwitchable GetTopMostPrimarySwitchable()
            => primarySwitches[primarySwitches.Count - 1];

        public void SwitchToSecondary()
            => BtnCnCNetLobby_LeftClick(this, EventArgs.Empty);

        private void BtnCnCNetLobby_LeftClick(object sender, EventArgs e)
        {

            if (OptionsWindow.UseSkin)
            {
                XNAMessageBox messageBox = new XNAMessageBox(WindowManager, "Warning".L10N("UI:Main:Warning"), "Skins are prohibited when online, revert skins to default".L10N("UI:Main:SkinsProhibited"), XNAMessageBoxButtons.OK);
                messageBox.Show();
                return;
            }


            var code = ComputerUniqueHelper.GetComputerUUID();

            var r = NetWorkINISettings.Get<string>($"user/checkBanUserByMac?mac={code}").Result;

            if(r.Item1 != null)
            {
                XNAMessageBox.Show(WindowManager, "提示", $"你已被封号至 {r.Item1}");
                return;
            }

            var r2 = NetWorkINISettings.Post<bool>("user/addOnlineUser", new User()
            {
                username = ProgramConstants.PLAYERNAME,
                mac = code,
            }).Result; 

            optionsWindow.tabControl.MakeUnselectable(4);
            LastSwitchType = SwitchType.SECONDARY;
            primarySwitches[primarySwitches.Count - 1].SwitchOff();
            cncnetLobbySwitch.SwitchOn();
            privateMessageSwitch.SwitchOff();

            
            ((DarkeningPanel)((XNAControl)cncnetLobbySwitch).Parent).Alpha = 1.0f;
        }

        private void BtnMainButton_LeftClick(object sender, EventArgs e)
        {
            LastSwitchType = SwitchType.PRIMARY;
            cncnetLobbySwitch.SwitchOff();
            privateMessageSwitch.SwitchOff();
            primarySwitches[primarySwitches.Count - 1].SwitchOn();

            
            if (((XNAControl)primarySwitches[primarySwitches.Count - 1]).Parent is DarkeningPanel darkeningPanel)
                darkeningPanel.Alpha = 1.0f;
        }

        private void BtnPrivateMessages_LeftClick(object sender, EventArgs e)
            => privateMessageSwitch.SwitchOn();

        private void BtnOptions_LeftClick(object sender, EventArgs e)
        {
            //privateMessageSwitch.SwitchOff();

            //optionsWindow.tabControl.MakeUnselectable(4);
            optionsWindow.Open();
            optionsWindow.tabControl.SelectedTab = 0;
            //optionsWindow.ForbigSkin();
        }

        private List<Keys> secretCodeSequence = new List<Keys> { Keys.Up, Keys.Up, Keys.Down, Keys.Down, Keys.Left, Keys.Right, Keys.Left, Keys.Right, Keys.B, Keys.A, Keys.B, Keys.A };
        private int secretCodeIndex = 0;

        private int escPressCount = 0;
        private System.Timers.Timer escTimer = new System.Timers.Timer(1000); // 1秒超时

        private void Keyboard_OnKeyPressed(object sender, KeyPressEventArgs e)
        {
            if (!Enabled || !WindowManager.HasFocus || ProgramConstants.IsInGame)
                return;

            switch (e.PressedKey)
            {
                case Keys.F1:
                    BringDown();
                    break;
                case Keys.F2 when btnMainButton.AllowClick:
                    BtnMainButton_LeftClick(this, EventArgs.Empty);
                    break;
                case Keys.F3 when btnCnCNetLobby.AllowClick:
                    BtnCnCNetLobby_LeftClick(this, EventArgs.Empty);
                    break;
                case Keys.F4 when btnPrivateMessages.AllowClick:
                    BtnPrivateMessages_LeftClick(this, EventArgs.Empty);
                    break;
                case Keys.F12 when btnOptions.AllowClick:
                    BtnOptions_LeftClick(this, EventArgs.Empty);
                    break;
                case Keys.F11 when btnModManager.AllowClick:
                    BtnModManager_LeftClick(this, EventArgs.Empty);
                    break;
                case Keys.Escape:
                    HandleEscPress();
                    break;
                default:
                    if (e.PressedKey == secretCodeSequence[secretCodeIndex])
                    {
                        secretCodeIndex++;

                        if (secretCodeIndex == secretCodeSequence.Count)
                        {
                            触发彩蛋();
                            secretCodeIndex = 0;
                        }
                    }
                    else
                    {
                        secretCodeIndex = 0;
                    }
                    break;
            }
        }

        private void HandleEscPress()
        {
            escPressCount++;

            if (escPressCount == 2)
            {
                // 第一次按下ESC时启动计时器
                escTimer.Elapsed += ResetEscCount;
                escTimer.AutoReset = false;
                escTimer.Start();
            }

            if (escPressCount >= 5)
            {
                OpenClientLog();
                escPressCount = 1;
                escTimer.Stop();
            }
        }

        private void ResetEscCount(object sender, ElapsedEventArgs e)
        {
            escPressCount = 1;
        }

        // 打开Client文件夹并选中Client.log
        private void OpenClientLog()
        {
          
            string logFilePath = Path.Combine(ProgramConstants.GamePath,"Client", "Client.log");

            if (File.Exists(logFilePath))
            {
                Process.Start("explorer.exe", $"/select,\"{logFilePath}\"");
            }
        }

        public override void OnMouseOnControl()
        {
            if (Cursor.Location.Y > -1 && !ProgramConstants.IsInGame)
                BringDown();

            base.OnMouseOnControl();
        }

        void BringDown()
        {
            isDown = true;
            downTime = TimeSpan.Zero;
        }

        public void SetMainButtonText(string text)
            => btnMainButton.Text = text;

        public void SetSwitchButtonsClickable(bool allowClick)
        {
            if (btnMainButton != null)
                btnMainButton.AllowClick = allowClick;
            if (btnCnCNetLobby != null)
                btnCnCNetLobby.AllowClick = allowClick;
            if (btnPrivateMessages != null)
                btnPrivateMessages.AllowClick = allowClick;
            if (btnModManager != null)
                btnModManager.AllowClick = allowClick;
        }

        public void SetOptionsButtonClickable(bool allowClick)
        {
            if (btnOptions != null)
                btnOptions.AllowClick = allowClick;
        }

        public void SetLanMode(bool lanMode)
        {
            this.lanMode = lanMode;
            SetSwitchButtonsClickable(!lanMode);
            if (lanMode)


                ConnectionEvent("CnCNet Status: LAN MODE".L10N("UI:Main:StatusLanMode"));

            else
                ConnectionEvent("CnCNet Status: OFFLINE".L10N("UI:Main:StatusOffline"));
        }

        public override void Update(GameTime gameTime)
        {
            if (Cursor.Location.Y < APPEAR_CURSOR_THRESHOLD_Y && Cursor.Location.Y > -1 && !ProgramConstants.IsInGame)
                BringDown();


            if (isDown)
            {
                if (locationY < 0)
                {
                    locationY += DOWN_MOVEMENT_RATE * (gameTime.ElapsedGameTime.TotalMilliseconds / 10.0);
                    ClientRectangle = new Rectangle(X, (int)locationY,
                        Width, Height);
                }

                downTime += gameTime.ElapsedGameTime;

                isDown = downTime < downTimeWaitTime;
            }
            else
            {
                if (locationY > -Height - 1)
                {
                    locationY -= UP_MOVEMENT_RATE * (gameTime.ElapsedGameTime.TotalMilliseconds / 10.0);
                    ClientRectangle = new Rectangle(X, (int)locationY,
                        Width, Height);
                }
                else
                    return; // Don't handle input when the cursor is above our game window
            }

            DateTime dtn = DateTime.Now;

            lblTime.Text = Renderer.GetSafeString(dtn.ToLongTimeString(), lblTime.FontIndex);
            string dateText = Renderer.GetSafeString(dtn.ToShortDateString(), lblDate.FontIndex);
            if (lblDate.Text != dateText)
                lblDate.Text = dateText;

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            Renderer.DrawRectangle(new Rectangle(X, ClientRectangle.Bottom - 2, Width, 1), UISettings.ActiveSettings.PanelBorderColor);
        }

        private void 触发彩蛋()
        {

            if (!minesweeperGameWindow.Enabled)
            {
                WindowManager.progress.Report("***触发彩蛋***");
                minesweeperGameWindow.Enable();

                minesweeperGameWindow.EnabledChanged += (s, args) =>
                {

                    minesweeperGameWindow.EnabledChanged -= (s, args) => { };
                    WindowManager.progress.Report(string.Empty);

                };
            }
        }
    }

    public enum SwitchType
    {
        PRIMARY,
        SECONDARY
    }
}

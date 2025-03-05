using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Localization;
using Localization.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Newtonsoft.Json.Linq;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using ClientCore;
using ClientCore.Settings;
using ClientGUI;
using Ra2Client.Domain;
using Ra2Client.Domain.Multiplayer.CnCNet;
using Ra2Client.DXGUI.Multiplayer;
using Ra2Client.DXGUI.Multiplayer.CnCNet;
using Ra2Client.DXGUI.Multiplayer.GameLobby;
using Ra2Client.Online;
using DTAConfig;
using DTAConfig.OptionPanels;
using Microsoft.Win32;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI.Input;
using System.Net.Sockets;
using Ra2Client.Domain.Multiplayer;
using ClientCore.CnCNet5;
using DTAConfig.Entity;
using Microsoft.Extensions.FileSystemGlobbing;
using OpenRA.Mods.Cnc.FileSystem;

namespace Ra2Client.DXGUI.Generic
{

    class GuideWindow(WindowManager windowManager) : XNAWindow(windowManager), ISwitchable
    {
        public override void Initialize()
        {

            var firstLabel = new XNALabel(WindowManager)
            {
                ClientRectangle = new Rectangle(20, 10, 0, 0),
                Text = "看起来您是第一次运行游戏，先给自己取个名字吧！"
            };


            BackgroundTexture = AssetLoader.LoadTexture("msgboxform.png");
            var tbxName = new XNASuggestionTextBox(WindowManager)
            {
                ClientRectangle = new Rectangle(20, 50, 350, 25),
                Name = "nameTextBox",
                Suggestion = "(不能为空,长度不超过10位)"
            };

            var btnConfirm = new XNAClientButton(WindowManager)
            {
                ClientRectangle = new Rectangle(100, 90, UIDesignConstants.BUTTON_WIDTH_160, UIDesignConstants.BUTTON_HEIGHT),
                Text = "确认"
            };
            btnConfirm.LeftClick += (sender, e) =>
            {
                string error = NameValidator.IsNameValid(tbxName.Text);

                if (error != null)
                {
                    XNAMessageBox.Show(windowManager, "提示", error);
                    return;
                }

                ProgramConstants.PLAYERNAME = tbxName.Text;
                UserINISettings.Instance.PlayerName.Value = tbxName.Text;
                UserINISettings.Instance.SaveSettings();
                Disable();

            };

            ClientRectangle = new Rectangle(0, 0, firstLabel.Right + 24, btnConfirm.Y + 40);


            base.Initialize();

            AddChild(firstLabel);
            AddChild(tbxName);
            AddChild(btnConfirm);

            WindowManager.CenterControlOnScreen(this);

        }
        public void Show()
        {
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, this);
        }
        public string GetSwitchName()
        {
            throw new NotImplementedException();
        }

        public void SwitchOff()
        {
            throw new NotImplementedException();
        }

        public void SwitchOn()
        {
            throw new NotImplementedException();
        }
    }

    class ModSelectWindow(WindowManager windowManager) : XNAWindow(windowManager), ISwitchable
    {
        public override void Initialize()
        {

            var label = new XNALabel(WindowManager)
            {
                ClientRectangle = new Rectangle(20, 10, 0, 0),
                Text = "你想用哪个模组的地编?"
            };


            BackgroundTexture = AssetLoader.LoadTexture("msgboxform.png");
            var ddMod = new XNAClientDropDown(WindowManager)
            {
                ClientRectangle = new Rectangle(20, 50, 200, 25),
                Name = "ddMod",
            };

            Mod.Mods.ForEach(m => ddMod.AddItem(m.Name,m));

            ddMod.SelectedIndex = 0;

            var btnConfirm = new XNAClientButton(WindowManager)
            {
                ClientRectangle = new Rectangle(50, 90, UIDesignConstants.BUTTON_WIDTH_160, UIDesignConstants.BUTTON_HEIGHT),
                Text = "确认"
            };
            btnConfirm.LeftClick += (sender, e) =>
            {
                var mod = ddMod.SelectedItem.Tag as Mod; //这里应该改成玩家选择
                var section = new IniSection("Settings");
                section.AddKey("Game", mod.FilePath);

                GameProcessLogic.加载模组文件(section);

                LaunchMapEditor();

                Disable();
                Dispose();
            };

            ClientRectangle = new Rectangle(0, 0, ddMod.Right + 24, btnConfirm.Y + 40);


            base.Initialize();

            AddChild(label);
            AddChild(ddMod);
            AddChild(btnConfirm);

            WindowManager.CenterControlOnScreen(this);

        }

        private void LaunchMapEditor()
        {
            var mapEditorProcess = new Process();
            mapEditorProcess.StartInfo.FileName = Path.Combine(ProgramConstants.GamePath, "Resources\\FinalAlert2SP\\FinalAlert2SP.exe");
            mapEditorProcess.StartInfo.WorkingDirectory = Path.Combine(ProgramConstants.GamePath,"Resources\\FinalAlert2SP");
            mapEditorProcess.StartInfo.UseShellExecute = false;   //是否使用操作系统shell启动 
            mapEditorProcess.StartInfo.CreateNoWindow = true;   //是否在新窗口中启动该进程的值 (不显示程序窗口)
            mapEditorProcess.Start();
            mapEditorProcess.WaitForExit();  //等待程序执行完退出进程
            mapEditorProcess.Close();
        }

        public void Show()
        {
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, this);
        }
        public string GetSwitchName()
        {
            throw new NotImplementedException();
        }

        public void SwitchOff()
        {
            throw new NotImplementedException();
        }

        public void SwitchOn()
        {
            throw new NotImplementedException();
        }
    }

    partial

        /// <summary>
        /// The main menu of the client.
        /// </summary>
        class MainMenu : XNAWindow, ISwitchable
    {
        private const float MEDIA_PLAYER_VOLUME_EXIT_FADE_STEP = 0.025f;
        private const double UPDATE_RE_CHECK_THRESHOLD = 30.0;

        /// <summary>
        /// Creates a new instance of the main menu.
        /// </summary>
        public MainMenu(
            WindowManager windowManager,
            SkirmishLobby skirmishLobby,
            LANLobby lanLobby,
            TopBar topBar,
            OptionsWindow optionsWindow,
            CnCNetLobby cncnetLobby,
            CnCNetManager connectionManager,
            DiscordHandler discordHandler,
            CnCNetGameLoadingLobby cnCNetGameLoadingLobby,
            CnCNetGameLobby cnCNetGameLobby,
            PrivateMessagingPanel privateMessagingPanel,
            PrivateMessagingWindow privateMessagingWindow,
            GameInProgressWindow gameInProgressWindow
        ) : base(windowManager)
        {
            this.lanLobby = lanLobby;
            this.topBar = topBar;
            this.connectionManager = connectionManager;
            this.optionsWindow = optionsWindow;
            this.cncnetLobby = cncnetLobby;
            this.discordHandler = discordHandler;
            this.skirmishLobby = skirmishLobby;
            this.cnCNetGameLoadingLobby = cnCNetGameLoadingLobby;
            this.cnCNetGameLobby = cnCNetGameLobby;
            this.privateMessagingPanel = privateMessagingPanel;
            this.privateMessagingWindow = privateMessagingWindow;
            this.gameInProgressWindow = gameInProgressWindow;
            this.cncnetLobby.UpdateCheck += CncnetLobby_UpdateCheck;
            isMediaPlayerAvailable = IsMediaPlayerAvailable();
        }

        private MainMenuDarkeningPanel innerPanel;

        private XNALabel lblCnCNetPlayerCount;
        private XNALinkLabel lblUpdateStatus;
        private XNALinkLabel lblWebsite;

        private readonly CnCNetLobby cncnetLobby;

        private readonly SkirmishLobby skirmishLobby;

        private readonly LANLobby lanLobby;

        private readonly CnCNetManager connectionManager;

        private readonly OptionsWindow optionsWindow;

        private readonly DiscordHandler discordHandler;

        private readonly TopBar topBar;
        private readonly CnCNetGameLoadingLobby cnCNetGameLoadingLobby;
        private readonly CnCNetGameLobby cnCNetGameLobby;
        private readonly PrivateMessagingPanel privateMessagingPanel;
        private readonly PrivateMessagingWindow privateMessagingWindow;
        private readonly GameInProgressWindow gameInProgressWindow;

        private int index = 0;                      //list 计数
        private int timer = 0;                      //计时
        private XNATextBlock picDynamicbg;          //"动态图"

        private bool _updateInProgress;
        private bool UpdateInProgress
        {
            get { return _updateInProgress; }
            set
            {
                _updateInProgress = value;
                topBar.SetSwitchButtonsClickable(!_updateInProgress);
                topBar.SetOptionsButtonClickable(!_updateInProgress);
                SetButtonHotkeys(!_updateInProgress);
            }
        }

        private bool customComponentDialogQueued = false;

        private DateTime lastUpdateCheckTime;

        private Song themeSong;

        private static readonly object locker = new();

        private bool isMusicFading = false;


        private readonly bool isMediaPlayerAvailable;

        private CancellationTokenSource cncnetPlayerCountCancellationSource;

        // Main Menu Buttons
        private XNAClientButton btnNewCampaign;     //"战役"
        private XNAClientButton btnLoadGame;        //"载入存档"
        private XNAClientButton btnSkirmish;        //"遭遇战"
        private XNAClientButton btnCnCNet;          //"联机大厅"
        private XNAClientButton btnLan;             //"局域网大厅"
        private XNAClientButton btnOptions;         //"设置"
        private XNAClientButton btnMapEditor;       //"地图编辑器"
        private XNAClientButton btnStatistics;      //"统计数据"
        private XNAClientButton btnCredits;         //""
        private XNAClientButton btnExtras;          //""
        private XNATextBlock lblannouncement;       //"公告"

        /// <summary>
        /// Initializes the main menu's controls.
        /// </summary>
        public override void Initialize()
        {

           // Mix.UnPackMix("/mix","E:\\Documents\\My_File\\RA2Setup\\Updater\\expandmd06.mix");
            
            topBar.SetSecondarySwitch(cncnetLobby);
            GameProcessLogic.GameProcessExited += SharedUILogic_GameProcessExited;

            Name = nameof(MainMenu);

            BackgroundTexture = AssetLoader.LoadTexture("MainMenu/mainmenubg.png");
            ClientRectangle = new Rectangle(0, 0, BackgroundTexture.Width, BackgroundTexture.Height);

            WindowManager.CenterControlOnScreen(this);

            btnNewCampaign = new XNAClientButton(WindowManager);
            btnNewCampaign.Name = nameof(btnNewCampaign);
            btnNewCampaign.IdleTexture = AssetLoader.LoadTexture("MainMenu/campaign.png");
            btnNewCampaign.HoverTexture = AssetLoader.LoadTexture("MainMenu/campaign_c.png");
            btnNewCampaign.HoverSoundEffect = new EnhancedSoundEffect("MainMenu/button.wav");
            btnNewCampaign.LeftClick += BtnNewCampaign_LeftClick;
            btnNewCampaign.Tag = "游玩战役（任务包）";

            btnNewCampaign.Text = "Campaign".L10N("UI:Main:Campaign");

            btnLoadGame = new XNAClientButton(WindowManager);
            btnLoadGame.Name = nameof(btnLoadGame);
            btnLoadGame.IdleTexture = AssetLoader.LoadTexture("MainMenu/loadmission.png");
            btnLoadGame.HoverTexture = AssetLoader.LoadTexture("MainMenu/loadmission_c.png");
            btnLoadGame.HoverSoundEffect = new EnhancedSoundEffect("MainMenu/button.wav");
            btnLoadGame.LeftClick += BtnLoadGame_LeftClick;
            btnLoadGame.Text = "reLoad Game".L10N("UI:Main:LoadGame");
            btnLoadGame.Tag = "载入存档";

            btnSkirmish = new XNAClientButton(WindowManager);
            btnSkirmish.Name = nameof(btnSkirmish);
            btnSkirmish.IdleTexture = AssetLoader.LoadTexture("MainMenu/skirmish.png");
            btnSkirmish.HoverTexture = AssetLoader.LoadTexture("MainMenu/skirmish_c.png");
            btnSkirmish.HoverSoundEffect = new EnhancedSoundEffect("MainMenu/button.wav");
            btnSkirmish.LeftClick += BtnSkirmish_LeftClick;
            btnSkirmish.Text = "Skirmish".L10N("UI:Main:SkirmishLobby");
            btnSkirmish.Tag = "遭遇战";

            btnCnCNet = new XNAClientButton(WindowManager);
            btnCnCNet.Name = nameof(btnCnCNet);
            btnCnCNet.IdleTexture = AssetLoader.LoadTexture("MainMenu/cncnet.png");
            btnCnCNet.HoverTexture = AssetLoader.LoadTexture("MainMenu/cncnet_c.png");
            btnCnCNet.HoverSoundEffect = new EnhancedSoundEffect("MainMenu/button.wav");
            btnCnCNet.LeftClick += BtnCnCNet_LeftClick;
            btnCnCNet.Text = "CnCNet".L10N("UI:Main:CnCNetLobby");
            btnCnCNet.Tag = "进入联机大厅联机";

            btnLan = new XNAClientButton(WindowManager);
            btnLan.Name = nameof(btnLan);
            btnLan.IdleTexture = AssetLoader.LoadTexture("MainMenu/lan.png");
            btnLan.HoverTexture = AssetLoader.LoadTexture("MainMenu/lan_c.png");
            btnLan.HoverSoundEffect = new EnhancedSoundEffect("MainMenu/button.wav");
            btnLan.Text = "LAN".L10N("UI:Main:LANGameLobby");
            btnLan.LeftClick += BtnLan_LeftClick;
            btnLan.Tag = "和朋友一起在局域网游玩";

            btnOptions = new XNAClientButton(WindowManager);
            btnOptions.Name = nameof(btnOptions);
            btnOptions.IdleTexture = AssetLoader.LoadTexture("MainMenu/options.png");
            btnOptions.HoverTexture = AssetLoader.LoadTexture("MainMenu/options_c.png");
            btnOptions.HoverSoundEffect = new EnhancedSoundEffect("MainMenu/button.wav");
            btnOptions.LeftClick += BtnOptions_LeftClick;
            btnOptions.Text = "Options".L10N("UI:Main:Options");
            btnOptions.Tag = "进入设置";

            btnMapEditor = new XNAClientButton(WindowManager);
            btnMapEditor.Name = nameof(btnMapEditor);
            btnMapEditor.IdleTexture = AssetLoader.LoadTexture("MainMenu/mapeditor.png");
            btnMapEditor.HoverTexture = AssetLoader.LoadTexture("MainMenu/mapeditor_c.png");
            btnMapEditor.HoverSoundEffect = new EnhancedSoundEffect("MainMenu/button.wav");
            btnMapEditor.LeftClick += BtnMapEditor_LeftClick;
            btnMapEditor.Text = "Map Editor".L10N("UI:Main:MapEditor");

            btnMapEditor.Tag = "打开地图编辑器";

            btnStatistics = new XNAClientButton(WindowManager);
            btnStatistics.Name = nameof(btnStatistics);
            btnStatistics.IdleTexture = AssetLoader.LoadTexture("MainMenu/statistics.png");
            btnStatistics.HoverTexture = AssetLoader.LoadTexture("MainMenu/statistics_c.png");
            btnStatistics.HoverSoundEffect = new EnhancedSoundEffect("MainMenu/button.wav");
            btnStatistics.LeftClick += BtnStatistics_LeftClick;
            btnStatistics.Text = "Statistics".L10N("UI:Main:Statistics");
            btnStatistics.Tag = "战绩查询";


            btnCredits = new XNAClientButton(WindowManager);
            btnCredits.Name = nameof(btnCredits);
            btnCredits.IdleTexture = AssetLoader.LoadTexture("MainMenu/credits.png");
            btnCredits.HoverTexture = AssetLoader.LoadTexture("MainMenu/credits_c.png");
            btnCredits.HoverSoundEffect = new EnhancedSoundEffect("MainMenu/button.wav");
          //  btnCredits.LeftClick += BtnCredits_LeftClick;
            btnCredits.Text = "View Credits".L10N("UI:MainMenu:Credits");


            btnExtras = new XNAClientButton(WindowManager);
            btnExtras.Name = nameof(btnExtras);
            btnExtras.IdleTexture = AssetLoader.LoadTexture("MainMenu/extras.png");
            btnExtras.HoverTexture = AssetLoader.LoadTexture("MainMenu/extras_c.png");
            btnExtras.HoverSoundEffect = new EnhancedSoundEffect("MainMenu/button.wav");
            btnExtras.LeftClick += BtnExtras_LeftClick;

            var btnExit = new XNAClientButton(WindowManager);
            btnExit.Name = nameof(btnExit);
            btnExit.IdleTexture = AssetLoader.LoadTexture("MainMenu/exitgame.png");
            btnExit.HoverTexture = AssetLoader.LoadTexture("MainMenu/exitgame_c.png");
            btnExit.HoverSoundEffect = new EnhancedSoundEffect("MainMenu/button.wav");
            btnExit.LeftClick += BtnExit_LeftClick;
            btnExit.Text = "Exit".L10N("UI:Main:Exit");
            btnExit.Tag = "退出游戏";

            XNALabel lblCnCNetStatus = new XNALabel(WindowManager);
            lblCnCNetStatus.Name = nameof(lblCnCNetStatus);
            lblCnCNetStatus.Text = "DTA players on CnCNet:".L10N("UI:Main:CnCNetOnlinePlayersCountText");
            lblCnCNetStatus.ClientRectangle = new Rectangle(12, 9, 0, 0);

            lblCnCNetPlayerCount = new XNALabel(WindowManager);
            lblCnCNetPlayerCount.Name = nameof(lblCnCNetPlayerCount);
            lblCnCNetPlayerCount.Text = "-";

            lblannouncement = new XNATextBlock(WindowManager);
            lblannouncement.Name = nameof(lblannouncement);
            lblannouncement.ClientRectangle = new Rectangle(950, 170, 140, 120);
            lblannouncement.TextColor = Color.Cyan;
            lblannouncement.Tag = "公告";

            picDynamicbg = new XNATextBlock(WindowManager);
            picDynamicbg.Name = "picDynamicbg";
            picDynamicbg.ClientRectangle = new Rectangle(94, 0, 852, 731);
            picDynamicbg.DrawBorders = false;

            lblUpdateStatus = new XNALinkLabel(WindowManager);
            lblUpdateStatus.Name = nameof(lblUpdateStatus);
            lblUpdateStatus.LeftClick += LblUpdateStatus_LeftClick;
            lblUpdateStatus.ClientRectangle = new Rectangle(0, 0, UIDesignConstants.BUTTON_WIDTH_160, 20);
            lblUpdateStatus.Text = "获取更新状态失败,点击以重试";

            lblWebsite = new XNALinkLabel(WindowManager);
            lblWebsite.Name = nameof(lblWebsite);
            lblWebsite.LeftClick += lblWebsite_LeftClick;
          //  lblUpdateStatus.ClientRectangle = new Rectangle(0, 0, UIDesignConstants.BUTTON_WIDTH_160, 20);

            AddChild(picDynamicbg);
            AddChild(lblannouncement);
            AddChild(btnNewCampaign);
            AddChild(btnLoadGame);
            AddChild(btnSkirmish);
            AddChild(btnCnCNet);
            AddChild(btnLan);
            AddChild(btnOptions);
            AddChild(btnMapEditor);
            AddChild(btnStatistics);
            AddChild(btnCredits);
            AddChild(btnExtras);
            AddChild(btnExit);
            AddChild(lblCnCNetStatus);
            AddChild(lblCnCNetPlayerCount);
            
            //var (R, G, B) = FunExtensions.ConvertHSVToRGB(25, 255, 255);
            //Console.WriteLine($"RGB: ({R}, {G}, {B})");

            if (!ClientConfiguration.Instance.ModMode)
            {
                // ModMode disables version tracking and the updater if it's enabled

                AddChild(lblWebsite);
                AddChild(lblUpdateStatus);

                Updater.FileIdentifiersUpdated += Updater_FileIdentifiersUpdated;
                Updater.OnCustomComponentsOutdated += Updater_OnCustomComponentsOutdated;
            }

            base.Initialize(); // Read control attributes from INI

            lblWebsite.ClientRectangle = new Rectangle(btnExit.Left + 80, lblUpdateStatus.Y + 3, 0, 0);
            lblWebsite.Text = Updater.GameVersion;

            innerPanel = new MainMenuDarkeningPanel(WindowManager, discordHandler)
            {
                ClientRectangle = new Rectangle(0, 0,
                Width,
                Height),
                DrawOrder = int.MaxValue,
                UpdateOrder = int.MaxValue
            };

            AddChild(innerPanel);
            innerPanel.Hide();

            innerPanel.UpdateQueryWindow.UpdateDeclined += UpdateQueryWindow_UpdateDeclined;
            innerPanel.UpdateQueryWindow.UpdateAccepted += UpdateQueryWindow_UpdateAccepted;
            innerPanel.ManualUpdateQueryWindow.Closed += ManualUpdateQueryWindow_Closed;

            innerPanel.UpdateWindow.UpdateCompleted += UpdateWindow_UpdateCompleted;
            innerPanel.UpdateWindow.UpdateCancelled += UpdateWindow_UpdateCancelled;
            innerPanel.UpdateWindow.UpdateFailed += UpdateWindow_UpdateFailed;

            ClientRectangle = new Rectangle((WindowManager.RenderResolutionX - Width) / 2,
                (WindowManager.RenderResolutionY - Height) / 2,
                Width, Height);
            innerPanel.ClientRectangle = new Rectangle(0, 0,
                Math.Max(WindowManager.RenderResolutionX, Width),
                Math.Max(WindowManager.RenderResolutionY, Height));

            CnCNetPlayerCountTask.CnCNetGameCountUpdated += CnCNetInfoController_CnCNetGameCountUpdated;
            cncnetPlayerCountCancellationSource = new CancellationTokenSource();
            CnCNetPlayerCountTask.InitializeService(cncnetPlayerCountCancellationSource);

            WindowManager.GameClosing += WindowManager_GameClosing;

            innerPanel.CampaignSelector.Exited += CampaignSelector_Exited;
            skirmishLobby.Exited += SkirmishLobby_Exited;
            lanLobby.Exited += LanLobby_Exited;
            optionsWindow.EnabledChanged += OptionsWindow_EnabledChanged;

            optionsWindow.OnForceUpdate += (s, e) => ForceUpdate();

            GameProcessLogic.GameProcessStarted += SharedUILogic_GameProcessStarted;
            GameProcessLogic.GameProcessStarting += SharedUILogic_GameProcessStarting;

            UserINISettings.Instance.SettingsSaved += SettingsSaved;

            Updater.Restart += Updater_Restart;

            //NetWorkINISettings.DownloadCompleted += UseDownloadedData;

            Task.Run(NetWorkINISettings.Initialize);

            SetButtonHotkeys(true);
            SetToolTip();

            UseDownloadedData();

        }

        
        private void SetToolTip()
        {
            var lblUpdateStatus2 = new XNALabel(WindowManager)
            {
                ClientRectangle = lblUpdateStatus.ClientRectangle,
                FontIndex = 1
            };
            AddChild(lblUpdateStatus2);

            foreach (var c in Children)
            {
                c.MouseEnter += (_, _) => { { lblUpdateStatus2.Text = c.Tag as string; lblUpdateStatus2.Visible = !(lblUpdateStatus.Visible = c.Tag is not string); } };
            }
        }

        public async void UseDownloadedData()
        {
            lblannouncement.Text = GetContent((await NetWorkINISettings.Get<string>("anno/getAnnoByType?type=mainMenu")).Item1 ?? "暂无公告");
        }

        private string GetContent(string content)
        {
            string s1;
            string description = string.Empty;

            foreach (string s in content.Split("\\n"))
            {
                s1 = s + Environment.NewLine;
                if (s1.Length > 13)
                {
                    s1 = InsertFormat(s1, 13, Environment.NewLine);
                }

                description += s1;
            }

            return description;
        }

        private void CheckDDRAW()
        {
            WindowManager.progress.Report("检查渲染插件是否异常...");

            const string registryPath = @"SYSTEM\CurrentControlSet\Control\Session Manager";
            const string valueName = "ExcludeFromKnownDlls";
            const string dllName = "ddraw.dll";

            try
            {
                using (RegistryKey sessionManagerKey = Registry.LocalMachine.OpenSubKey(registryPath, true))
                {
                    if (sessionManagerKey == null)
                    {
                        Logger.Log("无法打开注册表路径。");
                        return;
                    }

                    // 获取当前值，如果不存在则创建一个新的多字符串值。
                    string[] existingValues = (string[])sessionManagerKey.GetValue(valueName, new string[] { });
                    if (Array.IndexOf(existingValues, dllName) == -1)
                    {
                        // 添加ddraw.dll到数组中
                        string[] newValues = new string[existingValues.Length + 1];
                        existingValues.CopyTo(newValues, 0);
                        newValues[newValues.Length - 1] = dllName;

                        // 设置新值
                        sessionManagerKey.SetValue(valueName, newValues, RegistryValueKind.MultiString);

                        XNAMessageBox.Show(WindowManager, "信息", "我们注意到游戏渲染插件可能无法被正确调用，已经修复了，但这需要你重启计算机才能生效.\n如果不重启电脑直接游玩,你可能会遇到 无法设置渲染模式,按ESC或者点击右上角按钮卡住,游戏运行不流畅  等问题.");
                    }
                    else
                    {
                        Logger.Log($"{dllName} 已存在于 {valueName} 中。");
                    }
                }  
            }
            catch (Exception ex)
            {
                Logger.Log($"发生错误: {ex.Message}");
            }
        }

        private string InsertFormat(string input, int interval, string value)
        {
            for (int i = interval; i < input.Length; i += interval + 1)
                input = input.Insert(i, value);
            return input;
        }

        private void SetButtonHotkeys(bool enableHotkeys)
        {
            if (!Initialized)
                return;

            if (enableHotkeys)
            {
                btnNewCampaign.HotKey = Keys.C;
                btnLoadGame.HotKey = Keys.L;
                btnSkirmish.HotKey = Keys.S;
                btnCnCNet.HotKey = Keys.M;
                btnLan.HotKey = Keys.N;
                btnOptions.HotKey = Keys.O;
                btnMapEditor.HotKey = Keys.E;
                btnStatistics.HotKey = Keys.T;
                btnCredits.HotKey = Keys.R;
                btnExtras.HotKey = Keys.X;
            }
            else
            {
                btnNewCampaign.HotKey = Keys.None;
                btnLoadGame.HotKey = Keys.None;
                btnSkirmish.HotKey = Keys.None;
                btnCnCNet.HotKey = Keys.None;
                btnLan.HotKey = Keys.None;
                btnOptions.HotKey = Keys.None;
                btnMapEditor.HotKey = Keys.None;
                btnStatistics.HotKey = Keys.None;
                btnCredits.HotKey = Keys.None;
                btnExtras.HotKey = Keys.None;
            }
        }



        private void OptionsWindow_EnabledChanged(object sender, EventArgs e)
        {
            if (!optionsWindow.Enabled)
            {
                if (customComponentDialogQueued)
                    Updater_OnCustomComponentsOutdated();
            }
        }

        /// <summary>
        /// Refreshes settings. Called when the game process is starting.
        /// </summary>
        private void SharedUILogic_GameProcessStarting()
        {
            UserINISettings.Instance.ReloadSettings();

            try
            {
                optionsWindow.RefreshSettings();
            }
            catch (Exception ex)
            {
                Logger.Log("Refreshing settings failed! Exception message: " + ex.Message);
                // We don't want to show the dialog when starting a game
                //XNAMessageBox.Show(WindowManager, "Saving settings failed",
                //    "Saving settings failed! Error message: " + ex.message);
            }
        }

        private void Updater_Restart(object sender, EventArgs e) =>
            WindowManager.AddCallback(new Action(ExitClient), null);

        /// <summary>
        /// Applies configuration changes (music playback and volume)
        /// when settings are saved.
        /// </summary>
        private void SettingsSaved(object sender, EventArgs e)
        {
            if (isMediaPlayerAvailable)
            {
                if (MediaPlayer.State == MediaState.Playing)
                {
                    if (!UserINISettings.Instance.PlayMainMenuMusic)
                        isMusicFading = true;
                }
                else if (topBar.GetTopMostPrimarySwitchable() == this &&
                    topBar.LastSwitchType == SwitchType.PRIMARY)
                {
                    PlayMusic();
                }
            }

            if (!connectionManager.IsConnected)
                ProgramConstants.PLAYERNAME = UserINISettings.Instance.PlayerName;

            if (UserINISettings.Instance.DiscordIntegration)
                discordHandler.Connect();
            else
                discordHandler.Disconnect();
        }

        private void 检查地编()
        {
            string FA2Path = ProgramConstants.GamePath + ClientConfiguration.Instance.MapEditorExePath;
            if (!File.Exists(FA2Path))
            {
                Logger.Log("没有找到地编");
                btnMapEditor.Enabled = false;
            }
            else
            {
                var ini = new IniFile(ProgramConstants.GamePath + "Resources/FinalAlert2SP/FinalAlert.ini", Encoding.GetEncoding("GBK"));
                ini.SetStringValue("TS", "Exe", Path.Combine(ProgramConstants.游戏目录, "gamemd.exe").Replace('/', '\\')); //地编路径必须是\，这里写两个是因为有一个是转义符
                ini.WriteIniFile();
                Logger.Log("写入地编游戏路径");
            }
        }

        private void 检查根目录下是否有玩家放入的Mod或任务包或多人图()
        {
            var modManager = ModManager.GetInstance(WindowManager);
            if (ModManager.判断是否为Mod(ProgramConstants.GamePath,true))
            {

                var XNAMessageBox = new XNAMessageBox(WindowManager, "提示", "检测到模组或任务包.\n选 是 则按模组导入.选 否 则按任务包导入.", XNAMessageBoxButtons.YesNo);
                XNAMessageBox.YesClickedAction += (_) => {
                    modManager.导入具体Mod(ProgramConstants.GamePath,true, false,true);
                    清理根目录();
                    modManager.刷新并渲染([]);
                };
                XNAMessageBox.NoClickedAction += (_) => {
                    var m = modManager.导入具体任务包(true, false, ProgramConstants.GamePath);
                    if (m != null)
                    {
                        清理根目录();
                        modManager.刷新并渲染(Directory.GetFiles(m.FilePath, "*.map").ToList());
                    }
                };
                XNAMessageBox.Show();
            }
            else if (ModManager.判断是否为任务包(ProgramConstants.GamePath))
            {
                var m = modManager.导入具体任务包(true, false, ProgramConstants.GamePath);
                if (m != null)
                {
                    清理根目录();
                    modManager.刷新并渲染(Directory.GetFiles(m.FilePath, "*.map").ToList());
                }
            }

            
            //var allowedExtensions = new HashSet<string>(
            //        [".map", ".yrm", ".mpr"],
            //        StringComparer.OrdinalIgnoreCase
            //    );

            //if (Directory.EnumerateFiles(ProgramConstants.GamePath)
            //    .Any(file => allowedExtensions.Contains(Path.GetExtension(file))
            //                 && MapLoader.是否为多人图(file)))
            //{
            //    UserINISettings.Instance.重新加载地图和任务包?.Invoke();
            //}




        }

        private FileSystemWatcher _watcher;
        private System.Timers.Timer _timer;
        private readonly object _lock = new();
        private void 监控根目录()
        {
            // 初始化 FileSystemWatcher
            _watcher = new FileSystemWatcher();
            _watcher.Path = ProgramConstants.GamePath;

            // 监控所有文件（按需修改筛选条件）
            _watcher.Filter = "*.*";
            // 或者监控特定类型文件，例如：_watcher.Filter = "*.txt";

            // 设置监控哪些变更类型
            _watcher.NotifyFilter = NotifyFilters.FileName;

            _timer = new System.Timers.Timer(1000); // 500ms 触发一次
            _timer.Elapsed += (_, _) => 检查根目录下是否有玩家放入的Mod或任务包或多人图();
            _timer.AutoReset = false; // 只触发一次，防止重复执行

            // 订阅事件
            _watcher.Created += (_,_)=> {
                lock (_lock)
                {
                    _timer.Stop();  // 每次触发时先停止计时器
                    _timer.Start(); // 重新启动计时器，等待新的事件
                }
            };

            // 启用监控
            _watcher.EnableRaisingEvents = true;
        }

        private void 清理根目录()
        {
            List<string> whitelist = [
                "cncnet5.dll", 
                "gamemd-spawn.exe", 
                "LiteExt.dll", 
                "RA2MD.ini", 
                "Reunion.deps.json", 
                "Reunion.dll", 
                "Reunion.dll.config", 
                "Reunion.exe", 
                "Reunion.runtimeconfig.json",
                "qres.dat",
                "qres32.dll"
                ];

            foreach (string file in Directory.GetFiles(ProgramConstants.GamePath))
            {
                if (whitelist.Contains(Path.GetFileName(file))) continue;
                if((Path.GetExtension(file) == ".map" || Path.GetExtension(file) == ".yrm" || Path.GetExtension(file) == ".mpr") && MapLoader.是否为多人图(file)) continue;
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);

            }
        }
        

        private void CheckForbiddenFiles()
        {
            WindowManager.progress.Report("检查违禁文件.....");

            List<string> presentFiles = ClientConfiguration.Instance.ForbiddenFiles.ToList()
                .FindAll(f => !string.IsNullOrWhiteSpace(f) && SafePath.GetFile(ProgramConstants.GamePath, f).Exists);

            if (presentFiles.Count > 0)
                XNAMessageBox.Show(WindowManager, "Interfering Files Detected".L10N("UI:Main:InterferingFilesDetectedTitle"),
                    "The following interfering files are present:".L10N("UI:Main:InterferingFilesDetectedTextNonTS1") +
                    Environment.NewLine + Environment.NewLine +
                    String.Join(Environment.NewLine, presentFiles) +
                    Environment.NewLine + Environment.NewLine +
                    "The mod won't work correctly without those files removed.".L10N("UI:Main:InterferingFilesDetectedTextNonTS2")
                    );
        }

        private void CheckPrivacyNotification()
        {
            WindowManager.progress.Report("检查隐私同意对话框.....");
            if (!UserINISettings.Instance.PrivacyPolicyAccepted)
            {
                innerPanel.PrivacyWindow.BoilerEventLog += FirstRun;
                WindowManager.progress.Report("等待隐私同意结果.....");
                innerPanel.Show(innerPanel.PrivacyWindow);
            }
        }



        /// <summary>
        /// Checks whether the client is running for the first time.
        /// If it is, displays a dialog asking the user if they'd like
        /// to configure settings.
        /// </summary>
        private void FirstRun()
        {
            //ProgramConstants.清理缓存();
            var guideWindow = new GuideWindow(WindowManager);
            guideWindow.Show();

            string[] filesToCreate =
            [
                    "Client/custom_rules_all.ini",
                    //"Client/custom_rules_ra2.ini",
                    //"Client/custom_rules_yr.ini",
                    "Client/custom_art_all.ini",
                    //"Client/custom_art_ra2.ini",
                    //"Client/custom_art_yr.ini",
                    "Client/CampaignSetting.ini",
                    "Resources/missioninfo.ini",
                    "Resources/rules.json",
            ];

            foreach (string filePath in filesToCreate)
            {
                if (!File.Exists(filePath))
                {
                    File.Create(filePath).Close(); // 创建并关闭文件
                }
            }

            UserINISettings.Instance.IsFirstRun.Value = false;
            UserINISettings.Instance.SaveSettings();

            optionsWindow.PostInit();

            btnOptions.OnLeftClick();
        }

        //private void Verification_File()
        //{
        //    WindowManager.progress.Report("验证文件完整性.....");
        //    string[] files = ["ra2.mix", "ra2md.mix", "language.mix", "langmd.mix", "RF.mix", "qres32.dll", "Resources/ccmixar.exe"];
        //    foreach (var file in files)
        //    {
        //        var r = File.Exists(file);
        //        if (!r)
        //        {
        //            XNAMessageBox.Show(WindowManager, "文件校验不通过", $"文件校验不通过:${file}不存在,可能会影响游戏。");
        //            return;
        //        }

        //    }
        //}

        private void SharedUILogic_GameProcessStarted() => MusicOff();

        private void WindowManager_GameClosing(object sender, EventArgs e) => Clean();

        private void CampaignSelector_Exited(object sender, EventArgs e)
        {
            if (UserINISettings.Instance.StopMusicOnMenu)
                PlayMusic();
        }

        private void SkirmishLobby_Exited(object sender, EventArgs e)
        {
            if (UserINISettings.Instance.StopMusicOnMenu)
                PlayMusic();
        }

        private void LanLobby_Exited(object sender, EventArgs e)
        {
            topBar.SetLanMode(false);

            if (UserINISettings.Instance.AutomaticCnCNetLogin)
                connectionManager.Connect();

            if (UserINISettings.Instance.StopMusicOnMenu)
                PlayMusic();
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

        /// <summary>
        /// Attemps to "clean" the client session in a nice way if the user closes the game.
        /// </summary>
        private void Clean()
        {
            Updater.FileIdentifiersUpdated -= Updater_FileIdentifiersUpdated;

            if (cncnetPlayerCountCancellationSource != null) cncnetPlayerCountCancellationSource.Cancel();
            topBar.Clean();
            if (UpdateInProgress)
                Updater.StopUpdate();

            if (connectionManager.IsConnected)
                connectionManager.Disconnect();
        }

        /// <summary>
        /// Starts playing music, initiates an update check if automatic updates
        /// are enabled and checks whether the client is run for the first time.
        /// Called after all internal client UI logic has been initialized.
        /// </summary>
        public void PostInit()
        {
            WindowManager.Report("加载菜单...");
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, skirmishLobby);
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, cnCNetGameLoadingLobby);
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, cnCNetGameLobby);
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, cncnetLobby);
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, lanLobby);
            optionsWindow.SetTopBar(topBar);
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, optionsWindow);
            WindowManager.AddAndInitializeControl(privateMessagingPanel);
            privateMessagingPanel.AddChild(privateMessagingWindow);
            topBar.SetTertiarySwitch(privateMessagingWindow);
            topBar.SetOptionsWindow(optionsWindow);
            WindowManager.AddAndInitializeControl(gameInProgressWindow);
            skirmishLobby.Disable();
            cncnetLobby.Disable();
            cnCNetGameLobby.Disable();
            cnCNetGameLoadingLobby.Disable();
            lanLobby.Disable();
            privateMessagingWindow.Disable();
            optionsWindow.Disable();

            WindowManager.AddAndInitializeControl(topBar);
            topBar.AddPrimarySwitchable(this);

            SwitchMainMenuMusicFormat();

            themeSong = AssetLoader.LoadSong(ClientConfiguration.Instance.MainMenuMusicName);

            PlayMusic();
            if (!ClientConfiguration.Instance.ModMode)
            {
                if (UserINISettings.Instance.CheckForUpdates)
                {
                    CheckForUpdates();
                }
                else
                {

                    lblUpdateStatus.Text = "点击以检查更新.";
                }
            }
            //Verification_File();
            CheckHostName();
            CheckForbiddenFiles();
            CheckPrivacyNotification();
            CheckDDRAW();
            CheckYRPath();
            检查地编();
            检查根目录下是否有玩家放入的Mod或任务包或多人图();
            监控根目录();
            try
            {
                if (Directory.Exists("./tmp"))
                    Directory.Delete("./tmp", true);
            }
            catch (Exception ex)
            {
                Logger.Log("错误", $"删除缓存文件夹出错: {ex.Message}");
            }
            //if (MapLoader.rootMaps.Count != 0)
            //{
            //    XNAMessageBox.Show(WindowManager, "加载地图", $"检测到新地图,已移动至 Maps\\Multi\\Custom 文件夹. 包含:\n {string.Join("\n", MapLoader.rootMaps)}");
            //}

            WindowManager.progress.Report(string.Empty);
        }

        private void CheckYRPath()
        {
            if (!ProgramConstants.判断目录是否为纯净尤复(UserINISettings.Instance.YRPath))
            {
                var guideWindow = new YRPathWindow(WindowManager);
                guideWindow.Show();
            }
        }

        private void CheckHostName()
        {
            WindowManager.progress.Report("验证主机名合法性.....");
            Regex regex = HostNameRegex();
            try
            {
                string hostName = Environment.MachineName;
                if (!regex.IsMatch(hostName))
                {
                    XNAMessageBox.Show(WindowManager, "警告", "你的电脑名称似乎有点复杂，这可能导致无法游玩红警。如果进入游戏反复出现英文弹窗，请更改你的电脑名称。");
                }
            }
            catch (Exception ex)
            {
                Logger.Log("错误", $"出现未知错误。错误信息: {ex.Message}");
            }
        }

        private void SwitchMainMenuMusicFormat()
        {
            FileInfo wmaMainMenuMusicFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.BASE_RESOURCE_PATH,
                FormattableString.Invariant($"{ClientConfiguration.Instance.MainMenuMusicName}.wma"));

            if (!wmaMainMenuMusicFile.Exists)
                return;

            FileInfo wmaBackupMainMenuMusicFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.BASE_RESOURCE_PATH,
                FormattableString.Invariant($"{ClientConfiguration.Instance.MainMenuMusicName}.bak"));

            try
            {
                if (!wmaBackupMainMenuMusicFile.Exists)
                    wmaMainMenuMusicFile.CopyTo(wmaBackupMainMenuMusicFile.FullName);
                wmaBackupMainMenuMusicFile.CopyTo(wmaMainMenuMusicFile.FullName, true);
            }
            catch
            {

            }
        }

        #region Updating / versioning system

        private void UpdateWindow_UpdateFailed(object sender, UpdateFailureEventArgs e)
        {
            innerPanel.Hide();
            lblUpdateStatus.Text = "检查更新失败，点击以重试.";
            lblUpdateStatus.DrawUnderline = true;
            lblUpdateStatus.Enabled = true;
            UpdateInProgress = false;

            innerPanel.Show(null); // Darkening
            XNAMessageBox msgBox = new XNAMessageBox(WindowManager, "Update failed".L10N("UI:Main:UpdateFailedTitle"),
                string.Format(("An error occured while updating. Returned error was: {0}" +
                Environment.NewLine + Environment.NewLine +
                "If you are connected to the Internet and your firewall isn't blocking" + Environment.NewLine +
                "{1}, and the issue is reproducible, contact us at " + Environment.NewLine +
                "{2} for support.").L10N("UI:Main:UpdateFailedText"),
                e.Reason, Path.GetFileName(ProgramConstants.StartupExecutable), MainClientConstants.SUPPORT_URL_SHORT), XNAMessageBoxButtons.OK);
            msgBox.OKClickedAction = MsgBox_OKClicked;
            msgBox.Show();
        }

        private void MsgBox_OKClicked(XNAMessageBox messageBox)
        {
            innerPanel.Hide();
        }

        private void UpdateWindow_UpdateCancelled(object sender, EventArgs e)
        {
            innerPanel.Hide();
            lblUpdateStatus.Text = "The update was cancelled. Click to retry.".L10N("UI:Main:UpdateCancelledClickToRetry");
            lblUpdateStatus.DrawUnderline = true;
            lblUpdateStatus.Enabled = true;
            UpdateInProgress = false;
        }

        private void UpdateWindow_UpdateCompleted(object sender, EventArgs e)
        {
            innerPanel.Hide();
            lblUpdateStatus.Text = string.Format("{0} was succesfully updated to 3.{1}".L10N("UI:Main:UpdateSuccess"),
                MainClientConstants.GAME_NAME_LONG, Updater.GameVersion);
            UpdateInProgress = false;
            lblUpdateStatus.Enabled = true;
            lblUpdateStatus.DrawUnderline = false;
        }

        private void LblUpdateStatus_LeftClick(object sender, EventArgs e)
        {
            Logger.Log(Updater.versionState.ToString());

            if (Updater.versionState == VersionState.OUTDATED ||
                Updater.versionState == VersionState.MISMATCHED ||
                Updater.versionState == VersionState.UNKNOWN ||
                Updater.versionState == VersionState.UPTODATE)
            {
                CheckForUpdates();
            }
        }

        private void lblWebsite_LeftClick(object sender, EventArgs e)
        {
            ProcessLauncher.StartShellProcess("www.yra2.com");
        }

        private void ForceUpdate()
        {
            UpdateInProgress = true;
            innerPanel.Hide();
            innerPanel.UpdateWindow.ForceUpdate();
            innerPanel.Show(innerPanel.UpdateWindow);
            lblUpdateStatus.Text = "Force updating...".L10N("UI:Main:ForceUpdating");
        }


        /// <summary>
        /// Starts a check for updates.
        /// </summary>
        private void CheckForUpdates()
        {
            WindowManager.progress.Report("检查更新...");

            if (Updater.ServerMirrors == null || Updater.ServerMirrors.Count < 1)
                return;

            Updater.CheckForUpdates();
            WindowManager.Report();
            innerPanel.UpdateQueryWindow.GetUpdateContentsAsync(Updater.versionState.ToString(), VersionState.UPTODATE.ToString());
            lblUpdateStatus.Enabled = false;
            lblUpdateStatus.Text = "检查更新中...";
            lastUpdateCheckTime = DateTime.Now;
        }

        private void Updater_FileIdentifiersUpdated()
            => WindowManager.AddCallback(new Action(HandleFileIdentifierUpdate), null);

        /// <summary>
        /// Used for displaying the result of an update check in the UI.
        /// </summary>
        private void HandleFileIdentifierUpdate()
        {
            if (UpdateInProgress)
            {
                return;
            }


            if (Updater.versionState == VersionState.UPTODATE)
            {
                lblUpdateStatus.Text = string.Format("{0} is up to date.".L10N("UI:Main:GameUpToDate"), MainClientConstants.GAME_NAME_LONG);
                lblUpdateStatus.Enabled = true;
                lblUpdateStatus.DrawUnderline = false;
            }
            else if (Updater.versionState == VersionState.OUTDATED && Updater.ManualUpdateRequired)
            {
                lblUpdateStatus.Text = "An update is available. Manual download & installation required.".L10N("UI:Main:UpdateAvailableManualDownloadRequired");
                lblUpdateStatus.Enabled = true;
                lblUpdateStatus.DrawUnderline = false;
                innerPanel.ManualUpdateQueryWindow.SetInfo(Updater.ServerGameVersion, Updater.ManualDownloadURL);

                if (!string.IsNullOrEmpty(Updater.ManualDownloadURL))
                    innerPanel.Show(innerPanel.ManualUpdateQueryWindow);
            }
            else if (Updater.versionState == VersionState.OUTDATED)
            {
                lblUpdateStatus.Text = "An update is available.".L10N("UI:Main:UpdateAvailable");
                innerPanel.UpdateQueryWindow.SetInfo(Updater.ServerGameVersion, Updater.UpdateSizeInKb, Updater.UpdateTime);
                innerPanel.Show(innerPanel.UpdateQueryWindow);
            }
            else if (Updater.versionState == VersionState.UNKNOWN)
            {
                lblUpdateStatus.Text = "检查更新失败，点击以重试.";
                lblUpdateStatus.Enabled = true;
                lblUpdateStatus.DrawUnderline = true;
            }
        }

        /// <summary>
        /// Asks the user if they'd like to update their custom components.
        /// Handles an event raised by the updater when it has detected
        /// that the custom components are out of date.
        /// </summary>
        private void Updater_OnCustomComponentsOutdated()
        {
            if (innerPanel.UpdateQueryWindow.Visible)
                return;

            if (UpdateInProgress)
                return;

            customComponentDialogQueued = false;

            XNAMessageBox ccMsgBox = XNAMessageBox.ShowYesNoDialog(WindowManager,
                "Custom Component Updates Available".L10N("UI:Main:CustomUpdateAvailableTitle"),
                ("Updates for custom components are available. Do you want to open" + Environment.NewLine +
                "the Options menu where you can update the custom components?").L10N("UI:Main:CustomUpdateAvailableText"));
            ccMsgBox.YesClickedAction = CCMsgBox_YesClicked;
        }

        private void CCMsgBox_YesClicked(XNAMessageBox messageBox)
        {
            optionsWindow.Open();
            optionsWindow.SwitchToCustomComponentsPanel();
        }

        /// <summary>
        /// Called when the user has declined an update.
        /// </summary>
        private void UpdateQueryWindow_UpdateDeclined(object sender, EventArgs e)
        {
            UpdateQueryWindow uqw = (UpdateQueryWindow)sender;

            innerPanel.Hide();
            lblUpdateStatus.Text = "An update is available, click to install.".L10N("UI:Main:UpdateAvailableClickToInstall");
            lblUpdateStatus.Enabled = true;
            lblUpdateStatus.DrawUnderline = true;
        }

        /// <summary>
        /// Called when the user has accepted an update.
        /// </summary>
        private void UpdateQueryWindow_UpdateAccepted(object sender, EventArgs e)
        {
            innerPanel.Hide();
            innerPanel.UpdateWindow.SetData(Updater.ServerGameVersion);
            innerPanel.Show(innerPanel.UpdateWindow);
            lblUpdateStatus.Text = "Updating...".L10N("UI:Main:Updating");
            UpdateInProgress = true;
            Updater.StartUpdate();
        }

        private void ManualUpdateQueryWindow_Closed(object sender, EventArgs e)
            => innerPanel.Hide();

        #endregion

        private void BtnOptions_LeftClick(object sender, EventArgs e)
            => optionsWindow.Open();

        private void BtnNewCampaign_LeftClick(object sender, EventArgs e)
        {
            innerPanel.Show(innerPanel.CampaignSelector);

            if (UserINISettings.Instance.StopMusicOnMenu)
                MusicOff();

        }

        private void BtnLoadGame_LeftClick(object sender, EventArgs e)
        => innerPanel.Show(innerPanel.GameLoadingWindow);

        private void BtnLan_LeftClick(object sender, EventArgs e)
        {

            if (OptionsWindow.UseSkin)
            {
                XNAMessageBox messageBox = new XNAMessageBox(WindowManager, "警告", "联机时禁止使用皮肤，请将皮肤还原成默认", XNAMessageBoxButtons.OK);
                messageBox.Show();
                return;
            }

            lanLobby.Open();

            if (UserINISettings.Instance.StopMusicOnMenu)
                MusicOff();

            if (connectionManager.IsConnected)
                connectionManager.Disconnect();

            topBar.SetLanMode(true);
        }

        private void BtnCnCNet_LeftClick(object sender, EventArgs e) => topBar.SwitchToSecondary();

        private void BtnSkirmish_LeftClick(object sender, EventArgs e)
        {
            skirmishLobby.Open();

            if (UserINISettings.Instance.StopMusicOnMenu)
                MusicOff();
        }

        private void BtnMapEditor_LeftClick(object sender, EventArgs e)
        {
            // RenderImage.CancelRendering();

            var guideWindow = new ModSelectWindow(WindowManager);
            guideWindow.Show();

            
        }

        private void BtnStatistics_LeftClick(object sender, EventArgs e) =>
            innerPanel.Show(innerPanel.StatisticsWindow);

        //private void BtnCredits_LeftClick(object sender, EventArgs e)
        //{
        //    ProcessLauncher.StartShellProcess(MainClientConstants.CREDITS_URL);
        //}

        private void BtnExtras_LeftClick(object sender, EventArgs e) =>
            innerPanel.Show(innerPanel.ExtrasWindow);

        private void BtnExit_LeftClick(object sender, EventArgs e)
        {
            var messageBox = new XNAMessageBox(WindowManager, "退出确认", "确定退出客户端吗？？？", XNAMessageBoxButtons.YesNo);
            messageBox.YesClickedAction += (_) =>
            {
                WindowManager.HideWindow();
                FadeMusicExit();
            };
            messageBox.Show();
        }

        private void BtnExit_LeftClick(XNAMessageBox messageBox)
        {
#if WINFORMS
            WindowManager.HideWindow();
#endif
            FadeMusicExit();
        }

        private void SharedUILogic_GameProcessExited() =>
            AddCallback(new Action(HandleGameProcessExited), null);

        private void HandleGameProcessExited()
        {
            innerPanel.GameLoadingWindow.ListSaves();
            innerPanel.Hide();

            // If music is disabled on menus, check if the main menu is the top-most
            // window of the top bar and only play music if it is
            // LAN has the top bar disabled, so to detect the LAN game lobby
            // we'll check whether the top bar is enabled
            if (!UserINISettings.Instance.StopMusicOnMenu ||
                (topBar.Enabled && topBar.LastSwitchType == SwitchType.PRIMARY &&
                topBar.GetTopMostPrimarySwitchable() == this))
                PlayMusic();
        }

        /// <summary>
        /// Switches to the main menu and performs a check for updates.
        /// </summary>
        private void CncnetLobby_UpdateCheck(object sender, EventArgs e)
        {
            CheckForUpdates();
            topBar.SwitchToPrimary();
        }

        public override void Update(GameTime gameTime)
        {
            if (isMusicFading)
                FadeMusic(gameTime);

            base.Update(gameTime);


        }


        public override void Draw(GameTime gameTime)
        {
            lock (locker)
            {
                if (topBar.GetTopMostPrimarySwitchable() == this && Directory.Exists($"{ProgramConstants.GamePath}Resources/ThemeDefault/Dynamicbg/main"))
                {

                    if (timer >= 3)//播放速度
                    {
                        timer = 0;
                        picDynamicbg.BackgroundTexture?.Dispose();
                        picDynamicbg.BackgroundTexture = (AssetLoader.LoadTextureUncached("Resources/ThemeDefault/Dynamicbg/main/ra2ts_l" + string.Format("{0:d3}", index) + ".jpg"));

                        index++;

                        if (index >= 430) index = 0; //播放完成后回到第一帧 有多少张图片就写多少 懒得做判断了

                    }
                    else
                        timer++;
                }

                base.Draw(gameTime);
            }


        }


        /// <summary>
        /// Attempts to start playing the menu music.
        /// </summary>
        private void PlayMusic()
        {
            if (!isMediaPlayerAvailable)
                return; // SharpDX fails at music playback on Vista

            if (themeSong != null && UserINISettings.Instance.PlayMainMenuMusic)
            {
                isMusicFading = false;
                MediaPlayer.IsRepeating = true;
                MediaPlayer.Volume = (float)UserINISettings.Instance.ClientVolume;

                try
                {
                    MediaPlayer.Play(themeSong);
                }
                catch (InvalidOperationException ex)
                {
                    Logger.Log("Playing main menu music failed! " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Lowers the volume of the menu music, or stops playing it if the
        /// volume is unaudibly low.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        private void FadeMusic(GameTime gameTime)
        {
            if (!isMediaPlayerAvailable || !isMusicFading || themeSong == null)
                return;

            // Fade during 1 second
            float step = SoundPlayer.Volume * (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (MediaPlayer.Volume > step)
                MediaPlayer.Volume -= step;
            else
            {
                MediaPlayer.Stop();
                isMusicFading = false;
            }
        }

        /// <summary>
        /// Exits the client. Quickly fades the music if it's playing.
        /// </summary>
        private void FadeMusicExit()
        {
            if (!isMediaPlayerAvailable || themeSong == null)
            {
                ExitClient();
                return;
            }

            float step = MEDIA_PLAYER_VOLUME_EXIT_FADE_STEP * (float)UserINISettings.Instance.ClientVolume;

            if (MediaPlayer.Volume > step)
            {
                MediaPlayer.Volume -= step;
                AddCallback(new Action(FadeMusicExit), null);
            }
            else
            {
                MediaPlayer.Stop();
                ExitClient();
            }
        }

        private void ExitClient()
        {
            Logger.Log("Exiting.");
            WindowManager.CloseGame();

            Thread.Sleep(1000);
            Environment.Exit(0);
        }

        public void SwitchOn()
        {
            if (UserINISettings.Instance.StopMusicOnMenu)
                PlayMusic();

            if (!ClientConfiguration.Instance.ModMode && UserINISettings.Instance.CheckForUpdates)
            {
                // Re-check for updates

                if ((DateTime.Now - lastUpdateCheckTime) > TimeSpan.FromSeconds(UPDATE_RE_CHECK_THRESHOLD))
                    CheckForUpdates();
            }
        }

        public void SwitchOff()
        {
            if (UserINISettings.Instance.StopMusicOnMenu)
                MusicOff();
        }

        private void MusicOff()
        {
            try
            {
                if (isMediaPlayerAvailable &&
                    MediaPlayer.State == MediaState.Playing)
                {
                    isMusicFading = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Turning music off failed! message: " + ex.Message);
            }
        }

        /// <summary>
        /// Checks if media player is available currently.
        /// It is not available on Windows Vista or other systems without the appropriate media player components.
        /// </summary>
        /// <returns>True if media player is available, false otherwise.</returns>
        private bool IsMediaPlayerAvailable()
        {
            try
            {
                MediaState state = MediaPlayer.State;
                return true;
            }
            catch (Exception e)
            {
                Logger.Log("Error encountered when checking media player availability. Error message: " + e.Message);
                return false;
            }
        }

        public string GetSwitchName() => "Main Menu".L10N("UI:Main:MainMenu");


        private static readonly Regex HostNameRegexInstance =
             new Regex(@"^[\p{L}a-zA-Z0-9\.\-_]+$", RegexOptions.Compiled);

        public static Regex HostNameRegex()
        {
            return HostNameRegexInstance;
        }
    }
}

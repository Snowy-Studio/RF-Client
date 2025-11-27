using System;
using ClientCore;
using ClientCore.CnCNet5;
using ClientCore.Settings;
using ClientGUI;
using DTAConfig.OptionPanels;
using Localization;
using Localization.Tools;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace DTAConfig
{
    public class OptionsWindow : XNAWindow
    {
        public OptionsWindow(WindowManager windowManager, GameCollection gameCollection) : base(windowManager)
        {
            this.gameCollection = gameCollection;
        }

        public event EventHandler OnForceUpdate;

        public XNAClientTabControl tabControl;

        public static bool UseSkin = false;

        //鸣谢列表
        private ThankWindow thankWindow;

        private XNAOptionsPanel[] optionsPanels;
        public ComponentsPanel componentsPanel;

        private DisplayOptionsPanel displayOptionsPanel;
        private XNAControl topBar;

        private GameCollection gameCollection;

        private XNAClientButton btnSave;
        private XNAClientButton btnCancel;


        public static void AddAndInitializeWithControl(WindowManager wm, XNAControl control)
        {
            var dp = new DarkeningPanel(wm);
            wm.AddAndInitializeControl(dp);
            dp.AddChild(control);
        }

        public override void Initialize()
        {
            
            Name = "OptionsWindow";
            ClientRectangle = new Rectangle(0, 0, 760, 475);
            BackgroundTexture = AssetLoader.LoadTextureUncached("optionsbg.png");

            tabControl = new XNAClientTabControl(WindowManager);
            tabControl.Name = "tabControl";
            tabControl.ClientRectangle = new Rectangle(12, 12, 0, 23);
            tabControl.FontIndex = 1;
            tabControl.ClickSound = new EnhancedSoundEffect("button.wav");
            tabControl.AddTab("Display".L10N("UI:DTAConfig:TabDisplay"), UIDesignConstants.BUTTON_WIDTH_92);
            tabControl.AddTab("Audio".L10N("UI:DTAConfig:TabAudio"), UIDesignConstants.BUTTON_WIDTH_92);
            tabControl.AddTab("Game".L10N("UI:DTAConfig:TabGame"), UIDesignConstants.BUTTON_WIDTH_92);
            tabControl.AddTab("CnCNet".L10N("UI:DTAConfig:TabCnCNet"), UIDesignConstants.BUTTON_WIDTH_92);
            //tabControl.AddTab("Skin".L10N("UI:DTAConfig:TabSkin"), UIDesignConstants.BUTTON_WIDTH_92);
            tabControl.AddTab("Updater".L10N("UI:DTAConfig:TabUpdater"), UIDesignConstants.BUTTON_WIDTH_92);
            tabControl.AddTab("Components".L10N("UI:DTAConfig:TabComponents"), UIDesignConstants.BUTTON_WIDTH_92);
            //tabControl.AddTab("Creator".L10N("UI:DTAConfig:TabCreator"), UIDesignConstants.BUTTON_WIDTH_92);
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = "btnCancel";
            btnCancel.ClientRectangle = new Rectangle(Width - 104,
                Height - 35, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
            btnCancel.Text = "Cancel".L10N("UI:DTAConfig:ButtonCancel");
            btnCancel.LeftClick += BtnBack_LeftClick;

            btnSave = new XNAClientButton(WindowManager);
            btnSave.Name = "btnSave";
            btnSave.ClientRectangle = new Rectangle(12, btnCancel.Y, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
            btnSave.Text = "Save".L10N("UI:DTAConfig:ButtonSave");
            btnSave.LeftClick += BtnSave_LeftClick;

            //鸣谢列表
            var btnThank = new XNAClientButton(WindowManager);
            btnThank.Name = "btnThank";
            btnThank.ClientRectangle = new Rectangle((btnSave.X + btnCancel.X) / 2, btnSave.Y, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
            btnThank.Text = "Thanks".L10N("UI:DTAConfig:ButtonThanks");
            btnThank.LeftClick += btnThank_LeftClick;

            thankWindow = new ThankWindow(WindowManager);
            AddAndInitializeWithControl(WindowManager, thankWindow);
            thankWindow.Disable();

            displayOptionsPanel = new DisplayOptionsPanel(WindowManager, UserINISettings.Instance);
            componentsPanel = new ComponentsPanel(WindowManager, UserINISettings.Instance);
            var updaterOptionsPanel = new UpdaterOptionsPanel(WindowManager, UserINISettings.Instance);
            updaterOptionsPanel.OnForceUpdate += (s, e) => { Disable(); OnForceUpdate?.Invoke(this, EventArgs.Empty); };

            optionsPanels =
            //设置菜单
            [
                displayOptionsPanel,                                                                //0 "显示"页面
                new AudioOptionsPanel(WindowManager, UserINISettings.Instance),                     //1 "音频"页面
                new GameOptionsPanel(WindowManager, UserINISettings.Instance, topBar),              //2 "游戏"页面
                new CnCNetOptionsPanel(WindowManager, UserINISettings.Instance, gameCollection),    //3 "CnCNet"页面
                // new LocalSkinPanel(WindowManager, UserINISettings.Instance),                        //4 "皮肤"页面
                updaterOptionsPanel,                                                                //5 "更新器"页面
                componentsPanel,                                                                    //6 "创意工坊"页面
                //new UserOptionsPanel(WindowManager, UserINISettings.Instance)                       //7 "创作者"页面
            ];

            // 找到拦截的方法再启用
            // Keyboard.OnKeyPressed += Keyboard_OnKeyPressed;

            if (ClientConfiguration.Instance.ModMode || null == NetWorkINISettings.Instance || NetWorkINISettings.Instance.UpdaterServers.Count < 1)
                tabControl.MakeUnselectable(4);
            RefreshUI_TabItemState();

            foreach (var panel in optionsPanels)
            {
                AddChild(panel);
                panel.Load();
                panel.Disable();
            }

            optionsPanels[0].Enable();

            AddChild(tabControl);
            AddChild(btnCancel);
            AddChild(btnSave);
            AddChild(btnThank);

            base.Initialize();

            CenterOnParent();
        }

        public void RefreshUI_TabItemState()
        {
            if(!tabControl.IsSelectable(4))
            {
                if (!ClientConfiguration.Instance.ModMode && null != NetWorkINISettings.Instance && NetWorkINISettings.Instance.UpdaterServers.Count > 0)
                {
                    tabControl.MakeSelectable(4);
                    optionsPanels[4].Load();
                }
            }

            //if (Updater.CustomComponents == null || Updater.CustomComponents.Count < 1)
            //    tabControl.MakeUnselectable(6);
        }

        private void btnThank_LeftClick(object sender, EventArgs e)
        {
            thankWindow.CenterOnParent();
            thankWindow.Enable();
        }

        public void SetTopBar(XNAControl topBar) => this.topBar = topBar;

        /// <summary>
        /// Parses extra options defined by the modder
        /// from an INI file. Called from XNAWindow.SetAttributesFromINI.
        /// </summary>
        /// <param name="iniFile">The INI file.</param>
        protected override void GetINIAttributes(IniFile iniFile)
        {
            base.GetINIAttributes(iniFile);

            foreach (var panel in optionsPanels)
                panel.ParseUserOptions(iniFile);
        }

        public void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if(tabControl.SelectedTab == 5)
            //    { FunExtensions.OpenUrl($"https://creator.yra2.com/workshop/map/list?token={UserINISettings.Instance.Token}&port={UserINISettings.Instance.startPort}");

            //    return;
            //}

            foreach (var panel in optionsPanels)
                panel.Disable();

            optionsPanels[tabControl.SelectedTab].Enable();
            optionsPanels[tabControl.SelectedTab].RefreshPanel();

            RefreshUI_TabItemState();
        }

        private void BtnBack_LeftClick(object sender, EventArgs e)
        {
            //if (Updater.IsComponentDownloadInProgress())
            //{
            //    var msgBox = new XNAMessageBox(WindowManager, "Downloads in progress".L10N("UI:DTAConfig:DownloadingTitle"),
            //        ("Optional component downloads are in progress. The downloads will be cancelled if you exit the Options menu." +
            //        Environment.NewLine + Environment.NewLine +
            //        "Are you sure you want to continue?").L10N("UI:DTAConfig:DownloadingText"), XNAMessageBoxButtons.YesNo);
            //    msgBox.Show();
            //    msgBox.YesClickedAction = ExitDownloadCancelConfirmation_YesClicked;

            //    return;
            //}
            //tabControl.MakeSelectable(4); 
            if (componentsPanel.需要刷新)
            {
                UserINISettings.Instance.重新加载地图和任务包?.Invoke(null,null);
                componentsPanel.需要刷新 = false;
            }
            
            WindowManager.SoundPlayer.SetVolume(Convert.ToSingle(UserINISettings.Instance.ClientVolume));
            Disable();
        }

        private void ExitDownloadCancelConfirmation_YesClicked(XNAMessageBox messageBox)
        {
            componentsPanel.CancelAllDownloads();
            WindowManager.SoundPlayer.SetVolume(Convert.ToSingle(UserINISettings.Instance.ClientVolume));
            Disable();
        }

        private void BtnSave_LeftClick(object sender, EventArgs e)
        {
            SaveSettings();
        }

        public bool getst()
        {
            return optionsPanels[4].Save();
        }

        private void SaveDownloadCancelConfirmation_YesClicked(XNAMessageBox messageBox)
        {
            componentsPanel.CancelAllDownloads();

            SaveSettings();
        }

        private void Keyboard_OnKeyPressed(object sender, Rampastring.XNAUI.Input.KeyPressEventArgs e)
        {
            if (Enabled)
            {
                if (e.PressedKey == Keys.Escape)
                {
                    btnCancel.OnLeftClick();
                }
                if (e.PressedKey == Keys.Enter)
                {
                    btnSave.OnLeftClick();
                }
            }
        }

        private void SaveSettings()
        {
            if (RefreshOptionPanels())
                return;

            bool restartRequired = false;

#if !DEBUG
            try
            {
#endif
                foreach (var panel in optionsPanels)
                {
                    //if (!panel.Selectable) //跳过禁用选项卡参数 By 彼得兔 2024/09/30
                    //    continue;
                    restartRequired = panel.Save() || restartRequired;
                }
                UserINISettings.Instance.SaveSettings();
                ProgramConstants.清理缓存();
#if !DEBUG
            }

            catch (Exception ex)
            {
                Logger.Log("Saving settings failed! Error message: " + ex.Message);
                XNAMessageBox.Show(WindowManager, "Saving Settings Failed".L10N("UI:DTAConfig:SaveSettingFailTitle"),
                    "Saving settings failed! Error message:".L10N("UI:DTAConfig:SaveSettingFailText") + " " + ex.Message);
            }
#endif
            Disable();

            if (restartRequired)
            {
                var msgBox = new XNAMessageBox(WindowManager, "Restart Required".L10N("UI:DTAConfig:RestartClientTitle"),
                    ("The client needs to be restarted for some of the changes to take effect." +
                    Environment.NewLine + Environment.NewLine +
                    "Do you want to restart now?").L10N("UI:DTAConfig:RestartClientText"), XNAMessageBoxButtons.YesNo);
                msgBox.Show();
                msgBox.YesClickedAction = RestartMsgBox_YesClicked;
            }
        }

        private void RestartMsgBox_YesClicked(XNAMessageBox messageBox) => WindowManager.RestartGame();

        /// <summary>
        /// Refreshes the option panels to account for possible
        /// changes that could affect theirs functionality.
        /// Shows the popup to inform the user if needed.
        /// </summary>
        /// <returns>A bool that determines whether the 
        /// settings values were changed.</returns>
        private bool RefreshOptionPanels()
        {
            bool optionValuesChanged = false;

            foreach (var panel in optionsPanels)
                optionValuesChanged = panel.RefreshPanel() || optionValuesChanged;

            if (optionValuesChanged)
            {
                XNAMessageBox.Show(WindowManager, "Setting Value(s) Changed".L10N("UI:DTAConfig:SettingChangedTitle"),
                    ("One or more setting values are" + Environment.NewLine +
                    "no longer available and were changed." +
                    Environment.NewLine + Environment.NewLine +
                    "You may want to verify the new setting" + Environment.NewLine +
                    "values in client's options window.").L10N("UI:DTAConfig:SettingChangedText"));

                return true;
            }

            return false;
        }

        public void RefreshSettings()
        {
            foreach (var panel in optionsPanels)
                panel.Load();

            RefreshOptionPanels();

            foreach (var panel in optionsPanels)
                panel.Save();

            UserINISettings.Instance.SaveSettings();
        }

        public void Open()
        {
            foreach (var panel in optionsPanels)
                panel.Load();

            RefreshOptionPanels();

            componentsPanel.Open();

            Enable();
        }

        public void ToggleMainMenuOnlyOptions(bool enable)
        {
            foreach (var panel in optionsPanels)
            {
                panel.ToggleMainMenuOnlyOptions(enable);
            }
        }

        public void SwitchToCustomComponentsPanel()
        {
            foreach (var panel in optionsPanels)
                panel.Disable();

            tabControl.SelectedTab = 5;
        }

        public void InstallCustomComponent(int id) => componentsPanel.InstallComponent(id);

        public void PostInit()
        {
        }
    }
}

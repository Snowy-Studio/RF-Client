using System;
using System.IO;
using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using DTAConfig.Settings;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAConfig.OptionPanels
{

    public class GameOptionsPanel : XNAOptionsPanel
    {
        private const int MAX_SCROLL_RATE = 6;

        public GameOptionsPanel(WindowManager windowManager, UserINISettings iniSettings, XNAControl topBar)
            : base(windowManager, iniSettings)
        {
            this.topBar = topBar;
        }

        private XNALabel lblScrollRateValue;

        private XNATrackbar trbScrollRate;
        private XNAClientCheckBox chkTargetLines;
        private XNAClientCheckBox chkScrollCoasting;
        private XNAClientCheckBox chkTooltips;
        private XNAClientCheckBox chkShowHiddenObjects;
        private XNAClientCheckBox chkIMEEnable;
        private XNAClientCheckBox chkMultinuclear;
        private XNAClientCheckBox chkForceEnableGameOptions;
        private XNAClientCheckBox chk连点器;

        private XNAControl topBar;

        private XNATextBox tbPlayerName;
        private XNATextBox tbStartCommand;
        private XNAClientCheckBox chkRenderPreviewImage;
        private XNAClientCheckBox chkSimplifiedCSF;
        private HotkeyConfigurationWindow hotkeyConfigWindow;

        public event Action MyEvent;

        public override void Initialize()
        {
            base.Initialize();

            Name = "GameOptionsPanel";

            var lblScrollRate = new XNALabel(WindowManager);
            lblScrollRate.Name = nameof(lblScrollRate);
            lblScrollRate.ClientRectangle = new Rectangle(12,
                14, 0, 0);
            lblScrollRate.Text = "Scroll Rate:".L10N("UI:DTAConfig:ScrollRate");

            lblScrollRateValue = new XNALabel(WindowManager);
            lblScrollRateValue.Name = nameof(lblScrollRateValue);
            lblScrollRateValue.FontIndex = 1;
            lblScrollRateValue.Text = "3";
            lblScrollRateValue.ClientRectangle = new Rectangle(
                Width - lblScrollRateValue.Width - 12,
                lblScrollRate.Y, 0, 0);

            trbScrollRate = new XNATrackbar(WindowManager);
            trbScrollRate.Name = nameof(trbScrollRate);
            trbScrollRate.ClientRectangle = new Rectangle(
                lblScrollRate.Right + 32,
                lblScrollRate.Y - 2,
                lblScrollRateValue.X - lblScrollRate.Right - 47,
                22);
            trbScrollRate.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2);
            trbScrollRate.MinValue = 0;
            trbScrollRate.MaxValue = MAX_SCROLL_RATE;
            trbScrollRate.ValueChanged += TrbScrollRate_ValueChanged;

            chkScrollCoasting = new SettingCheckBox(WindowManager, true, UserINISettings.OPTIONS, "ScrollMethod", true, "0", "1");
            chkScrollCoasting.Name = nameof(chkScrollCoasting);
            chkScrollCoasting.ClientRectangle = new Rectangle(
                lblScrollRate.X,
                trbScrollRate.Bottom + 20, 0, 0);
            chkScrollCoasting.Text = "Scroll Coasting".L10N("UI:DTAConfig:ScrollCoasting");

            //选择游戏
            var lblGameMod = new XNALabel(WindowManager);
            lblGameMod.Name = nameof(lblGameMod);
            lblGameMod.ClientRectangle = new Rectangle(250, chkScrollCoasting.Y, 0, 0);
            lblGameMod.Text = "Mod:".L10N("UI:DTAConfig:Mod");

            chkIMEEnable = new XNAClientCheckBox(WindowManager);
            chkIMEEnable.Name = nameof(chkIMEEnable);
            chkIMEEnable.ClientRectangle = new Rectangle(lblGameMod.X + 60, chkScrollCoasting.Y, 150, 20);
            chkIMEEnable.Text = "The client enables the input method".L10N("UI:DTAConfig:Enableinput");
            //chkIMEEnable.Visible = false;

            chkTargetLines = new SettingCheckBox(WindowManager, true, UserINISettings.OPTIONS, "UnitActionLines");
            chkTargetLines.Name = nameof(chkTargetLines);
            chkTargetLines.ClientRectangle = new Rectangle(
                lblScrollRate.X,
                chkScrollCoasting.Bottom + 24, 0, 0);
            chkTargetLines.Text = "Target Lines".L10N("UI:DTAConfig:TargetLines");

            chkMultinuclear = new XNAClientCheckBox(WindowManager);
            chkMultinuclear.Name = nameof(chkMultinuclear);
            chkMultinuclear.ClientRectangle = new Rectangle(lblGameMod.X + 60, chkTargetLines.Y, 150, 20);
            chkMultinuclear.Text = "System affinity enhancements".L10N("UI:DTAConfig:SystemAffinity");

            bool isSupportedOS = IsWinValleyOrLater() || IsWinX();
            chkMultinuclear.AllowChecking = isSupportedOS;

            chkForceEnableGameOptions = new XNAClientCheckBox(WindowManager)
            {
                Name = nameof(chkForceEnableGameOptions),
                ClientRectangle = new Rectangle(chkMultinuclear.X, chkMultinuclear.Top - 25, 150, 20),
                Text = "Third-party mods are allowed to use game options that are not recommended".L10N("UI:DTAConfig:ModsAllowNotRecommendGameOptions")
            };

            chkShowHiddenObjects = new SettingCheckBox(WindowManager, true, UserINISettings.OPTIONS, "ShowHidden");
            chkShowHiddenObjects.Name = nameof(chkShowHiddenObjects);
            chkShowHiddenObjects.ClientRectangle = new Rectangle(
                lblScrollRate.X,
                chkTargetLines.Bottom + 24, 0, 0);
            chkShowHiddenObjects.Text = "Show Hidden Objects".L10N("UI:DTAConfig:YRShowHidden");

            chkRenderPreviewImage = new XNAClientCheckBox(WindowManager);
            chkRenderPreviewImage.Name = nameof(chkRenderPreviewImage);
            chkRenderPreviewImage.ClientRectangle = new Rectangle(chkMultinuclear.X, chkShowHiddenObjects.Y, 150, 20);
            chkRenderPreviewImage.Text = "Render holographic preview in the background when importing a new map".L10N("UI:DTAConfig:RenderHolographicPreview");
             
            chkTooltips = new SettingCheckBox(WindowManager, true, UserINISettings.OPTIONS, "ToolTips");
            chkTooltips.Name = nameof(chkTooltips);
            chkTooltips.Text = "Tooltips".L10N("UI:DTAConfig:Tooltips");
            chkTooltips.ClientRectangle = new Rectangle(
                lblScrollRate.X,
                chkShowHiddenObjects.Bottom + 24, 0, 0);

            chkSimplifiedCSF = new XNAClientCheckBox(WindowManager);
            chkSimplifiedCSF.Name = nameof(chkSimplifiedCSF);
            chkSimplifiedCSF.Text = "Convert to Chinese Simplified Chinese by default when importing campaign packs/mods".L10N("UI:DTAConfig:ChineseSimpCSF");
            chkSimplifiedCSF.ClientRectangle = new Rectangle(chkRenderPreviewImage.X, chkTooltips.Y, 150, 20);

            var lblPlayerName = new XNALabel(WindowManager);
            lblPlayerName.Name = nameof(lblPlayerName);
            lblPlayerName.Text = "Player Name*:".L10N("UI:DTAConfig:PlayerName");
            lblPlayerName.ClientRectangle = new Rectangle(
                lblScrollRate.X,
                chkTooltips.Bottom + 30, 0, 0);

            var lblStartCommand = new XNALabel(WindowManager);
            lblStartCommand.Name = nameof(lblStartCommand);
            lblStartCommand.ClientRectangle = new Rectangle(chkMultinuclear.X , lblPlayerName.Y, 0, 0);
            lblStartCommand.Text = "Start the command:".L10N("UI:DTAConfig:StartCommand");

            tbStartCommand = new XNATextBox(WindowManager);
            tbStartCommand.Name = nameof(tbStartCommand);
            tbStartCommand.ClientRectangle = new Rectangle(lblStartCommand.Right + 20, lblStartCommand.Y, 240, 20);
            tbStartCommand.Text = ClientConfiguration.Instance.ExtraExeCommandLineParameters;
            AddChild(chkShowHiddenObjects);

            chk连点器 = new XNAClientCheckBox(WindowManager)
            {
                Name = nameof(chk连点器),
                ClientRectangle = new Rectangle(lblStartCommand.X, lblStartCommand.Y + 60, 0, 0),
                Text = "启用连点器"
            };

            AddChild(chk连点器);

            tbPlayerName = new XNATextBox(WindowManager);
            tbPlayerName.Name = nameof(tbPlayerName);
            tbPlayerName.MaximumTextLength = ClientConfiguration.Instance.MaxNameLength;
            tbPlayerName.ClientRectangle = new Rectangle(trbScrollRate.X,
                lblPlayerName.Y - 2, 100, 19);
            tbPlayerName.Text = ProgramConstants.PLAYERNAME;

            ProgramConstants.PlayerNameChanged += (_,_) =>
            {
                tbPlayerName.Text = ProgramConstants.PLAYERNAME;
            };
        
            var lblNotice = new XNALabel(WindowManager);
            lblNotice.Name = nameof(lblNotice);
            lblNotice.ClientRectangle = new Rectangle(lblPlayerName.X,
                lblPlayerName.Bottom + 30, 0, 0);
            lblNotice.Text = ("* If you are currently connected to CnCNet, you need to log out and reconnect" +
                Environment.NewLine + "for your new name to be applied.").L10N("UI:DTAConfig:ReconnectAfterRename");

            hotkeyConfigWindow = new HotkeyConfigurationWindow(WindowManager);
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, hotkeyConfigWindow);
            hotkeyConfigWindow.Disable();

            var btnModManager = new XNAClientButton(WindowManager);
            btnModManager.Name = nameof(btnModManager);
            btnModManager.ClientRectangle = new Rectangle(lblPlayerName.X, lblNotice.Bottom + 36, UIDesignConstants.BUTTON_WIDTH_160, UIDesignConstants.BUTTON_HEIGHT);
            btnModManager.Text = "Mod Manager (Experimental)".L10N("UI:DTAConfig:ModManager");

            btnModManager.LeftClick += (_, _) =>
            {
                var modManager = ModManager.GetInstance(WindowManager);
                if (modManager.Enabled)
                    return;
               var dp = DarkeningPanel.AddAndInitializeWithControl(WindowManager, modManager);
                
                //modManager.DDModAI.SelectedIndex = 0;
                modManager.Enable();
                modManager.EnabledChanged += (_,_) =>
                {
                    DarkeningPanel.RemoveControl(dp, WindowManager, modManager);
                };
            };

            var btnConfigureHotkeys = new XNAClientButton(WindowManager);
            btnConfigureHotkeys.Name = nameof(btnConfigureHotkeys);
            btnConfigureHotkeys.ClientRectangle = new Rectangle(lblPlayerName.X, lblNotice.Bottom + 72, UIDesignConstants.BUTTON_WIDTH_160, UIDesignConstants.BUTTON_HEIGHT);
            btnConfigureHotkeys.Text = "Configure Hotkeys".L10N("UI:DTAConfig:ConfigureHotkeys");
            btnConfigureHotkeys.LeftClick += BtnConfigureHotkeys_LeftClick;

            var btnRecover = new XNAClientButton(WindowManager);
            btnRecover.Name = nameof(btnRecover);
            btnRecover.ClientRectangle = new Rectangle(lblPlayerName.X, lblNotice.Bottom + 108, UIDesignConstants.BUTTON_WIDTH_160, UIDesignConstants.BUTTON_HEIGHT);
            btnRecover.Text = "清理游戏缓存".L10N("UI:DTAConfig:CleanupGameCache");
            btnRecover.SetToolTipText("If there is a problem with the game, you can click this button to try to fix it.".L10N("UI:DTAConfig:ButtonCleanupGameCache"));
            btnRecover.LeftClick += BtnRecover_LeftClick;

            AddChild(lblScrollRate);
            AddChild(lblScrollRateValue);
            AddChild(trbScrollRate);
            AddChild(chkScrollCoasting);
            AddChild(chkTargetLines);
            AddChild(chkIMEEnable);
            AddChild(chkMultinuclear);
            AddChild(chkRenderPreviewImage);
            AddChild(chkSimplifiedCSF);
            AddChild(lblStartCommand);
            AddChild(tbStartCommand);
            AddChild(chkTooltips);
            AddChild(lblPlayerName);
            AddChild(tbPlayerName);
            AddChild(lblNotice);
            AddChild(btnModManager);
            AddChild(btnConfigureHotkeys);
            AddChild(btnRecover);
            //AddChild(chkForceEnableGameOptions);
        }
      
        private void BtnRecover_LeftClick(object sender, EventArgs e)
        {
            XNAMessageBox xNAMessageBox = new XNAMessageBox(WindowManager, "清理确认".L10N("UI:DTAConfig:CleanupConfirmation"), "Are you sure you want to clean up the file cache?".L10N("UI:DTAConfig:CleanupCacheTips"), XNAMessageBoxButtons.YesNo);
            xNAMessageBox.Show();
            xNAMessageBox.YesClickedAction += (e) => XNAMessageBox.Show(WindowManager,"Tips".L10N("UI:Main:Tips"), ProgramConstants.清理缓存()?"Cleanup successful!".L10N("UI:DTAConfig:CleanupSuccessful"):"If the cleanup fails, a file may be occupied".L10N("UI:DTAConfig:CleanupFailed")) ;
        }

        private bool IsWinValleyOrLater()
        {
            return ProgramConstants.VersionValley.Contains(Environment.OSVersion.Version.Build);
        }

        private bool IsWinX()
        {
            return ProgramConstants.VersionX.Contains(Environment.OSVersion.Version.Build);
        }

        /// <summary>
        /// CPU亲和性增强 false/true
        /// </summary>
        /// <param name="TF"></param>
        private void Multinuclear(bool TF)
        {
            string ddrawPath = Path.Combine(ProgramConstants.GamePath, "Resources\\Render", UserINISettings.Instance.Renderer.Value, "ddraw.ini");
            if (File.Exists(ddrawPath))
            {
                var iniFile = new IniFile(ddrawPath);
                foreach (var s in iniFile.GetSections())
                {
                    if (iniFile.KeyExists(s, "singlecpu"))
                        iniFile.SetBooleanValue(s, "singlecpu", !TF);
                }
                iniFile.WriteIniFile();
            }
        }

        private void BtnConfigureHotkeys_LeftClick(object sender, EventArgs e)
        {
            hotkeyConfigWindow.Enable();

            if (topBar.Enabled)
            {
                topBar.Disable();
                hotkeyConfigWindow.EnabledChanged += HotkeyConfigWindow_EnabledChanged;
            }
        }

        private void HotkeyConfigWindow_EnabledChanged(object sender, EventArgs e)
        {
            hotkeyConfigWindow.EnabledChanged -= HotkeyConfigWindow_EnabledChanged;
            topBar.Enable();
        }

        private void TrbScrollRate_ValueChanged(object sender, EventArgs e)
        {
            lblScrollRateValue.Text = trbScrollRate.Value.ToString();
        }

        public override void Load()
        {
            base.Load();

            int scrollRate = ReverseScrollRate(IniSettings.ScrollRate);

            if (scrollRate >= trbScrollRate.MinValue && scrollRate <= trbScrollRate.MaxValue)
            {
                trbScrollRate.Value = scrollRate;
                lblScrollRateValue.Text = scrollRate.ToString();
            }

            chkIMEEnable.Checked = UserINISettings.Instance.IMEEnabled.Value;
            if (IsWinValleyOrLater() || IsWinX())
            {
                chkMultinuclear.Checked = UserINISettings.Instance.Multinuclear.Value;
            }
            else
            {
                chkMultinuclear.Checked = false; // 非兼容系统强制禁用
            }
            chkRenderPreviewImage.Checked = UserINISettings.Instance.RenderPreviewImage;
            chkSimplifiedCSF.Checked = UserINISettings.Instance.SimplifiedCSF;
            tbPlayerName.Text = UserINISettings.Instance.PlayerName;
            chkForceEnableGameOptions.Checked = UserINISettings.Instance.ForceEnableGameOptions.Value;
            chk连点器.Checked = UserINISettings.Instance.启用连点器.Value;
        }

        //public bool HasChinese(string str)
        //{
        //    return Regex.IsMatch(str, @"[\u4e00-\u9fa5]");
        //}

        public override bool Save()
        {
            bool restartRequired = base.Save();

            //if (HasChinese(tbPlayerName.Text))
            //{
            //    XNAMessageBox messageBox = new XNAMessageBox(WindowManager, "出错", "请不要使用中文作为游戏名", XNAMessageBoxButtons.OK);
            //    messageBox.Show();
            //    return false;
            //}

            Multinuclear(chkMultinuclear.Checked);

            IniSettings.ScrollRate.Value = ReverseScrollRate(trbScrollRate.Value);

            string r = NameValidator.IsNameValid(tbPlayerName.Text);

            if (r == null)
                IniSettings.PlayerName.Value = tbPlayerName.Text;
            else
                XNAMessageBox.Show(WindowManager, "错误", r);

            //if (chkIMEEnable.SelectedIndex != IniSettings.GameModSelect) {
            //    restartRequired = true;

            //    List<string> deleteFile = new List<string>();
            //    foreach (string file in Directory.GetFiles(UserINISettings.Instance.GameModPath.Value.Split(',')[UserINISettings.Instance.GameModSelect.Value]))
            //        deleteFile.Add(Path.GetFileName(file));

            //    FileHelper.DelFiles(deleteFile);
            //    FileHelper.CopyDirectory(UserINISettings.Instance.GameModPath.Value.Split(',')[chkIMEEnable.SelectedIndex],"./");
            if (IniSettings.IMEEnabled.Value != chkIMEEnable.Checked)
            {
                restartRequired = true;
                IniSettings.IMEEnabled.Value = chkIMEEnable.Checked;
            }
            if (IsWinValleyOrLater() || IsWinX())
            {
                IniSettings.Multinuclear.Value = chkMultinuclear.Checked;
            }
            IniSettings.RenderPreviewImage.Value = chkRenderPreviewImage.Checked;
            IniSettings.SimplifiedCSF.Value = chkSimplifiedCSF.Checked;
            IniSettings.ForceEnableGameOptions.Value = chkForceEnableGameOptions.Checked;
            IniSettings.启用连点器.Value = chk连点器.Checked;
            ClientConfiguration.Instance.ExtraExeCommandLineParameters = tbStartCommand.Text;

            return restartRequired;
        }

     
        private int ReverseScrollRate(int scrollRate)
        {
            return Math.Abs(scrollRate - MAX_SCROLL_RATE);
        }
    }
}

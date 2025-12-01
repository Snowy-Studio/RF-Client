using System;
using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using Localization;
using Microsoft.Xna.Framework;
using Ra2Client.Online;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;


namespace Ra2Client.DXGUI.Multiplayer.CnCNet
{
    class CnCNetLoginWindow : XNAWindow
    {
        public CnCNetLoginWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        XNALabel lblConnectToCnCNet;
        XNATextBox tbPlayerName;
        XNALabel lblPlayerName;
        XNADropDown ddRegion;
        XNALabel lblRegion;
        XNALabel lblRegionDescription;
        XNAPanel pnlTopDivider;
        XNAPanel pnlDivider;
        XNAPanel pnlBottomDivider;
        XNAClientCheckBox chkRememberMe;
        XNAClientCheckBox chkPersistentMode;
        XNAClientCheckBox chkAutoConnect;
        XNAClientButton btnConnect;
        XNAClientButton btnCancel;

        public event EventHandler Cancelled;
        public event EventHandler Connect;

        public override void Initialize()
        {
            Name = "CnCNetLoginWindow";
            ClientRectangle = new Rectangle(0, 0, 600, 270);
            BackgroundTexture = AssetLoader.LoadTextureUncached("logindialogbg.png");

            lblConnectToCnCNet = new XNALabel(WindowManager);
            lblConnectToCnCNet.Name = "lblConnectToCnCNet";
            lblConnectToCnCNet.FontIndex = 0;
            lblConnectToCnCNet.Text = "CONNECT TO CNCNET".L10N("UI:Main:ConnectToCncNet");

            AddChild(lblConnectToCnCNet);
            lblConnectToCnCNet.CenterOnParent();
            lblConnectToCnCNet.ClientRectangle = new Rectangle(lblConnectToCnCNet.X, 12, lblConnectToCnCNet.Width, lblConnectToCnCNet.Height);

            XNALabel lblServerNotice = new XNALabel(WindowManager);
            AddChild(lblServerNotice);
            lblServerNotice.Name = "lblServerNotice";
            lblServerNotice.Text = "Note: Rooms on different servers are not connected, please choose carefully (only servers in Chinese Mainland are interconnected)".L10N("UI:Main:ServerTip");
            lblServerNotice.FontIndex = 1;
            lblServerNotice.CenterOnParent();
            lblServerNotice.ClientRectangle = new Rectangle(lblServerNotice.X, lblConnectToCnCNet.Bottom + 8, lblServerNotice.Width, lblServerNotice.Height);

            pnlTopDivider = new XNAPanel(WindowManager);
            pnlTopDivider.ClientRectangle = new Rectangle(12, lblServerNotice.Bottom + 8, Width - 24, 1);
            AddChild(pnlTopDivider);

            int contentY = pnlTopDivider.Y + 10;

            tbPlayerName = new XNATextBox(WindowManager);
            tbPlayerName.Name = "tbPlayerName";
            tbPlayerName.ClientRectangle = new Rectangle(100, contentY, 188, 19);
            tbPlayerName.MaximumTextLength = ClientConfiguration.Instance.MaxNameLength;
            tbPlayerName.IMEDisabled = true;
            string defgame = ClientConfiguration.Instance.LocalGame;

            lblPlayerName = new XNALabel(WindowManager);
            lblPlayerName.Name = "lblPlayerName";
            lblPlayerName.FontIndex = 1;
            lblPlayerName.Text = "PLAYER NAME:".L10N("UI:Main:PlayerName");
            lblPlayerName.ClientRectangle = new Rectangle(12, tbPlayerName.Y + 1, lblPlayerName.Width, lblPlayerName.Height);

            ddRegion = new XNADropDown(WindowManager);
            ddRegion.Name = "ddRegion";
            ddRegion.ClientRectangle = new Rectangle(100, lblPlayerName.Bottom + 10, 188, 21);
            ddRegion.AddItem("Reunion Availability Zone 1".L10N("UI:Main:AvailabilityZone1"));
            ddRegion.AddItem("Reunion Availability Zone 2".L10N("UI:Main:AvailabilityZone2"));
            ddRegion.AddItem("Reunion Availability Zone 3".L10N("UI:Main:AvailabilityZone3"));
            ddRegion.AddItem("Reunion Availability Zone 4".L10N("UI:Main:AvailabilityZone4"));
            ddRegion.SelectedIndex = 0;

            lblRegion = new XNALabel(WindowManager);
            lblRegion.Name = "lblRegion";
            lblRegion.FontIndex = 1;
            lblRegion.Text = "SERVER REGION:".L10N("UI:Main:ServerRegion");
            lblRegion.ClientRectangle = new Rectangle(12, ddRegion.Y + 1, lblRegion.Width, lblRegion.Height);

            chkRememberMe = new XNAClientCheckBox(WindowManager);
            chkRememberMe.Name = "chkRememberMe";
            chkRememberMe.ClientRectangle = new Rectangle(12, ddRegion.Bottom + 12, 0, 0);
            chkRememberMe.Text = "Remember me".L10N("UI:Main:RememberMe");
            chkRememberMe.TextPadding = 7;
            chkRememberMe.CheckedChanged += ChkRememberMe_CheckedChanged;
            chkRememberMe.Visible = false;

            chkPersistentMode = new XNAClientCheckBox(WindowManager);
            chkPersistentMode.Name = "chkPersistentMode";
            chkPersistentMode.ClientRectangle = new Rectangle(12, chkRememberMe.Bottom + 30, 0, 0);
            chkPersistentMode.Text = "Stay connected outside of the CnCNet lobby".L10N("UI:Main:StayConnect");
            chkPersistentMode.TextPadding = chkRememberMe.TextPadding;
            chkPersistentMode.CheckedChanged += ChkPersistentMode_CheckedChanged;

            chkAutoConnect = new XNAClientCheckBox(WindowManager);
            chkAutoConnect.Name = "chkAutoConnect";
            chkAutoConnect.ClientRectangle = new Rectangle(12, chkPersistentMode.Bottom + 30, 0, 0);
            chkAutoConnect.Text = "Connect automatically on client startup".L10N("UI:Main:AutoConnect");
            chkAutoConnect.TextPadding = chkRememberMe.TextPadding;
            chkAutoConnect.Visible = false;

            pnlDivider = new XNAPanel(WindowManager);
            pnlDivider.ClientRectangle = new Rectangle(300, contentY, 1, 130);
            AddChild(pnlDivider);

            lblRegionDescription = new XNALabel(WindowManager);
            lblRegionDescription.Name = "lblRegionDescription";
            lblRegionDescription.FontIndex = 0;
            lblRegionDescription.Text = GetRegionDescription(ddRegion.SelectedIndex);
            lblRegionDescription.ClientRectangle = new Rectangle(305, contentY, 240, 130);
            AddChild(lblRegionDescription);

            ddRegion.SelectedIndexChanged += (s, e) =>
            {
                lblRegionDescription.Text = GetRegionDescription(ddRegion.SelectedIndex);
            };

            pnlBottomDivider = new XNAPanel(WindowManager);
            pnlBottomDivider.ClientRectangle = new Rectangle(12, pnlDivider.Y + pnlDivider.Height + 5, Width - 24, 1);
            AddChild(pnlBottomDivider);

            int buttonY = pnlBottomDivider.Y + 15;
            int totalButtonWidth = 110 + 110 + 10;
            int startX = (Width - totalButtonWidth) / 2;

            btnConnect = new XNAClientButton(WindowManager);
            btnConnect.Name = "btnConnect";
            btnConnect.ClientRectangle = new Rectangle(startX, buttonY, 110, 23);
            btnConnect.Text = "Connect".L10N("UI:Main:ButtonConnect");
            btnConnect.LeftClick += BtnConnect_LeftClick;

            btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = "btnCancel";
            btnCancel.ClientRectangle = new Rectangle(startX + 110 + 10, buttonY, 110, 23);
            btnCancel.Text = "Cancel".L10N("UI:Main:ButtonCancel");
            btnCancel.LeftClick += BtnCancel_LeftClick;

            AddChild(tbPlayerName);
            AddChild(lblPlayerName);
            AddChild(ddRegion);
            AddChild(lblRegion);
            AddChild(chkRememberMe);
            AddChild(chkPersistentMode);
            AddChild(chkAutoConnect);
            AddChild(btnConnect);
            AddChild(btnCancel);

            base.Initialize();

            CenterOnParent();

            UserINISettings.Instance.SettingsSaved += Instance_SettingsSaved;
        }

        private string GetRegionDescription(int index)
        {
            switch (index)
            {
                case 0: 
                    return "Availability Zone 1 Overview (China):\r\n\r\nRegion: Beijing, China / Guangdong, China\r\nAverage latency: 33ms (Delayed test location: China)\r\n\r\nLine: China BGP (Telecom, Unicom, Mobile)\r\nNetwork Support: IPv4+IPv6\r\nCloud Service Provider: Tencent Cloud".L10N("UI:Main:RegionDescCN");
                case 1: 
                    return "Availability Zone 2 Overview (Japan):\r\n\r\nRegion: Tokyo, Japan\r\nAverage latency: 92ms (Delayed test location: China)\r\n\r\nLine: Cogent+GSL+EIE+BBIX+JPIX\r\nNetwork Support: IPv4\r\nCloud Service Provider: Yunyoo".L10N("UI:Main:RegionDescJP");
                case 2: 
                    return "Availability Zone 3 Overview (United Kingdom):\n\nRegion: London, United Kingdom / Coventry, United Kingdom\nAverage latency: 235ms (Delayed test location: China)\n\nLine: GTT+NTT+PCCW+Arelion+RETN\nNetwork support: IPv4+IPv6\nCloud service provider: Yunyoo".L10N("UI:Main:RegionDescUK");
                case 3: 
                    return "Availability Zone 4 Overview (United States):\n\nRegion: Los Angeles, USA\nAverage latency: 160ms (Delayed test location: China)\n\nLine: GSL+CN2GIA+9929+10099+CMIN2\nNetwork support: IPv4+IPv6\nCloud service provider: DMIT".L10N("UI:Main:RegionDescUS");
                default: 
                    return "Please select a server region to view the region introduction.".L10N("UI:Main:RegionDescDefault");
            }
        }

        private void Instance_SettingsSaved(object sender, EventArgs e)
        {
            tbPlayerName.Text = UserINISettings.Instance.PlayerName;
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        private void ChkRememberMe_CheckedChanged(object sender, EventArgs e)
        {
            CheckAutoConnectAllowance();
        }

        private void ChkPersistentMode_CheckedChanged(object sender, EventArgs e)
        {
            CheckAutoConnectAllowance();
        }

        private void CheckAutoConnectAllowance()
        {
            chkAutoConnect.AllowChecking = chkPersistentMode.Checked && chkRememberMe.Checked;
            if (!chkAutoConnect.AllowChecking)
                chkAutoConnect.Checked = false;
        }

        private void BtnConnect_LeftClick(object sender, EventArgs e)
        {
            string errorMessage = NameValidator.IsNameValid(tbPlayerName.Text);

            if (!string.IsNullOrEmpty(errorMessage))
            {
                XNAMessageBox.Show(WindowManager, "Invalid Player Name".L10N("UI:Main:InvalidPlayerName"), errorMessage);
                return;
            }

            string selectedRegion = "Reunion Availability Zone 1";
            switch (ddRegion.SelectedIndex)
            {
                case 0: 
                    selectedRegion = "Reunion Availability Zone 1";
                    break;
                case 1: 
                    selectedRegion = "Reunion Availability Zone 2";
                    break;
                case 2: 
                    selectedRegion = "Reunion Availability Zone 3";
                    break;
                case 3: 
                    selectedRegion = "Reunion Availability Zone 4";
                    break;
            }
            Connection.SelectedRegion = selectedRegion;

            ProgramConstants.PLAYERNAME = tbPlayerName.Text;

            //UserINISettings.Instance.SkipConnectDialog.Value = chkRememberMe.Checked;
            UserINISettings.Instance.SkipConnectDialog.Value = false;
            UserINISettings.Instance.PersistentMode.Value = chkPersistentMode.Checked;
            UserINISettings.Instance.AutomaticCnCNetLogin.Value = chkAutoConnect.Checked;
            UserINISettings.Instance.PlayerName.Value = ProgramConstants.PLAYERNAME;

            UserINISettings.Instance.SaveSettings();

            Connect?.Invoke(this, EventArgs.Empty);
        }

        public void LoadSettings()
        {
            chkAutoConnect.Checked = UserINISettings.Instance.AutomaticCnCNetLogin;
            chkPersistentMode.Checked = UserINISettings.Instance.PersistentMode;
            //chkRememberMe.Checked = UserINISettings.Instance.SkipConnectDialog;
            chkRememberMe.Checked = false;

            tbPlayerName.Text = UserINISettings.Instance.PlayerName;

            if (chkRememberMe.Checked)
                BtnConnect_LeftClick(this, EventArgs.Empty);
        }
    }
}
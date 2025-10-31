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
            ClientRectangle = new Rectangle(0, 0, 340, 260);
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
            lblServerNotice.Text = "Tip: Rooms on different servers are not connected, please choose carefully.".L10N("UI:Main:ServerTip");
            lblServerNotice.FontIndex = 1;
            lblServerNotice.CenterOnParent();
            lblServerNotice.ClientRectangle = new Rectangle(lblServerNotice.X, lblConnectToCnCNet.Bottom + 8, lblServerNotice.Width, lblServerNotice.Height);

            tbPlayerName = new XNATextBox(WindowManager);
            tbPlayerName.Name = "tbPlayerName";
            tbPlayerName.ClientRectangle = new Rectangle(Width - 192, lblServerNotice.Bottom + 10, 188, 19);
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
            ddRegion.ClientRectangle = new Rectangle(Width - 192, lblPlayerName.Bottom + 10, 188, 21);
            ddRegion.AddItem("Chinese Mainland Zone 1".L10N("UI:Main:RegionChineseMainlandZone1"));
            ddRegion.AddItem("Chinese Mainland Zone 2".L10N("UI:Main:RegionChineseMainlandZone2"));
            ddRegion.AddItem("Chinese Mainland Zone 3".L10N("UI:Main:RegionChineseMainlandZone3"));
            ddRegion.AddItem("Asia Pacific Zone 1".L10N("UI:Main:RegionAsiaPacificZone1"));
            ddRegion.AddItem("Europe Zone 1".L10N("UI:Main:RegionEuropeZone1"));
            ddRegion.AddItem("North America Zone 1".L10N("UI:Main:RegionNorthAmericaZone1"));
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

            btnConnect = new XNAClientButton(WindowManager);
            btnConnect.Name = "btnConnect";
            btnConnect.ClientRectangle = new Rectangle(12, Height - 35, 110, 23);
            btnConnect.Text = "Connect".L10N("UI:Main:ButtonConnect");
            btnConnect.LeftClick += BtnConnect_LeftClick;

            btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = "btnCancel";
            btnCancel.ClientRectangle = new Rectangle(Width - 122, btnConnect.Y, 110, 23);
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

            string selectedRegion = "Chinese Mainland Zone 1";
            switch (ddRegion.SelectedIndex)
            {
                case 0: selectedRegion = "Chinese Mainland Zone 1";
                    break;
                case 1: selectedRegion = "Chinese Mainland Zone 2";
                    break;
                case 2: selectedRegion = "Chinese Mainland Zone 3";
                    break;
                case 3: selectedRegion = "Asia Pacific Zone 1";
                    break;
                case 4: selectedRegion = "Europe Zone 1";
                    break;
                case 5: selectedRegion = "North America Zone 1";
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
         //   chkRememberMe.Checked = UserINISettings.Instance.SkipConnectDialog;
            chkRememberMe.Checked = false;

            tbPlayerName.Text = UserINISettings.Instance.PlayerName;

            if (chkRememberMe.Checked)
                BtnConnect_LeftClick(this, EventArgs.Empty);
        }
    }
}
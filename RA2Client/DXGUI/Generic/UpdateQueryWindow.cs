using System;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.Settings;
using ClientGUI;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace Ra2Client.DXGUI.Generic
{
    /// <summary>
    /// A window that asks the user whether they want to update their game.
    /// </summary>
    public class UpdateQueryWindow : XNAWindow
    {
        public delegate void UpdateAcceptedEventHandler(object sender, EventArgs e);
        public event UpdateAcceptedEventHandler UpdateAccepted;

        public delegate void UpdateDeclinedEventHandler(object sender, EventArgs e);
        public event UpdateDeclinedEventHandler UpdateDeclined;

        public UpdateQueryWindow(WindowManager windowManager) : base(windowManager) { }

        private XNALabel lblDescription;
        private XNALabel lblUpdateSize;
        private XNALabel lblUpdateTime;
        private XNAListBox lstBoxUpdaterLog;
        private string changelogUrl = "www.yra2.com";

        public override void Initialize()
        {
            //changelogUrl = ClientConfiguration.Instance.ChangelogURL;
            ChangeAddress();

            Name = "UpdateQueryWindow";
            ClientRectangle = new Rectangle(0, 0, 400, 350);
            BackgroundTexture = AssetLoader.LoadTexture("updatequerybg.png");

            lblDescription = new XNALabel(WindowManager);
            lblDescription.ClientRectangle = new Rectangle(12, 9, 0, 0);
            lblDescription.Text = string.Empty;
            lblDescription.Name = nameof(lblDescription);

            var lblChangelogLink = new XNALinkLabel(WindowManager);
            lblChangelogLink.ClientRectangle = new Rectangle(12, 83, 0, 0);
            lblChangelogLink.Text = "Check out the full changelog".L10N("UI:Main:CheckOutFullChangelog");
            lblChangelogLink.IdleColor = Color.Goldenrod;
            lblChangelogLink.Name = nameof(lblChangelogLink);
            lblChangelogLink.LeftClick += LblChangelogLink_LeftClick;

            lblUpdateTime = new XNALabel(WindowManager);
            lblUpdateTime.ClientRectangle = new Rectangle(12, 58, 0, 0);
            lblUpdateTime.Text = string.Empty;
            lblUpdateTime.Name = nameof(lblUpdateTime);

            lblUpdateSize = new XNALabel(WindowManager);
            lblUpdateSize.ClientRectangle = new Rectangle(12, 33, 0, 0);
            lblUpdateSize.Text = String.Empty;
            lblUpdateSize.Name = nameof(lblUpdateSize);

            lstBoxUpdaterLog = new XNAListBox(WindowManager);
            lstBoxUpdaterLog.Name = nameof(lstBoxUpdaterLog);
            lstBoxUpdaterLog.ClientRectangle = new Rectangle(lblChangelogLink.X, lblUpdateTime.Y + 55, 380, 200);
            lstBoxUpdaterLog.LineHeight = 20;

            var btnYes = new XNAClientButton(WindowManager);
            btnYes.ClientRectangle = new Rectangle(12, 320, 75, 23);
            btnYes.Text = "Yes".L10N("UI:Main:ButtonYes");
            btnYes.LeftClick += BtnYes_LeftClick;
            btnYes.Name = nameof(btnYes);

            var btnNo = new XNAClientButton(WindowManager);
            btnNo.ClientRectangle = new Rectangle(315, 320, 75, 23);
            btnNo.Text = "No".L10N("UI:Main:ButtonNo");
            btnNo.LeftClick += BtnNo_LeftClick;
            btnNo.Name = nameof(btnNo);

            AddChild(lblDescription);
            AddChild(lblChangelogLink);
            AddChild(lstBoxUpdaterLog);
            AddChild(lblUpdateTime);
            AddChild(lblUpdateSize);
            AddChild(btnYes);
            AddChild(btnNo);

            base.Initialize();
            CenterOnParent();
        }

        public void ChangeAddress()
        {
            Task.Run(async () =>
            {
                var beta = UserINISettings.Instance.Update.Value == 1 ? "beta_log" : "log";
                changelogUrl = (await NetWorkINISettings.Get<string>($"dict/getValue?section=updater&key={beta}")).Item1 ?? "www.yra2.com";
            });
        }

        public void GetUpdateContents(string currentVersion, string latestVersion)
        {
            changelogUrl = Updater.serverVerCfg.ManualDownURL;
            lstBoxUpdaterLog.Clear();
            var logs = Updater.serverVerCfg.Logs?.Split("@") ?? Array.Empty<string>();
            foreach (var log in logs)
            {
                lstBoxUpdaterLog.AddItem(log);
            }
        }

        private void LblChangelogLink_LeftClick(object sender, EventArgs e)
        {
            ProcessLauncher.StartShellProcess(!string.IsNullOrEmpty(changelogUrl) ? changelogUrl : "www.yra2.com");
        }

        private void BtnYes_LeftClick(object sender, EventArgs e)
        {
            UpdateAccepted?.Invoke(this, e);
        }

        private void BtnNo_LeftClick(object sender, EventArgs e)
        {
            UpdateDeclined?.Invoke(this, e);
        }

        public void SetInfo(string version, int updateSize, string updateTime)
        {
            lblDescription.Text = string.Format("A new version \"{0}\" has been pushed, do you want to install this update?".L10N("UI:Main:UpdateInfo1"), version);
            lblUpdateSize.Text = string.Format("Update package size: {0}".L10N("UI:Main:UpdateInfo2"), GetSizeString(updateSize));
            lblUpdateTime.Text = string.Format("Updated: {0}".L10N("UI:Main:UpdateInfo3"), updateTime);
        }

        private string GetSizeString(long size)
        {
            if (size < 1048576)
            {
                return string.Format("{0:F2}KB", size * 1.0 / 1024);
            }
            else if (size >= 1048576 && size < 1073741824)
            {
                return string.Format("{0:F2}MB", size * 1.0 / 1048576);
            }
            else
                return string.Format("{0:F2}GB", size * 1.0 / 1073741824);
        }
    }
}
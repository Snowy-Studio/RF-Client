using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.Settings;
using ClientGUI;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAConfig.OptionPanels
{
    class UpdaterOptionsPanel : XNAOptionsPanel
    {
        private XNALabel lblBestServer;
        private XNAListBox lbServerList;
        private XNAClientCheckBox chkAutoCheck;
        private XNAClientButton btnForceUpdate;
        private XNALabel lblChannel;
        private XNADropDown ddChannel;

        private System.Threading.Timer latencyRefreshTimer;

        public event EventHandler OnForceUpdate;

        public UpdaterOptionsPanel(WindowManager windowManager, UserINISettings iniSettings)
            : base(windowManager, iniSettings)
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            Name = "UpdaterOptionsPanel";

            var lblDescription = new XNALabel(WindowManager);
            lblDescription.Name = "lblDescription";
            lblDescription.ClientRectangle = new Rectangle(12, 12, 0, 0);
            lblDescription.Text = ("The client will automatically select the best server for the update based on the actual latency.").L10N("UI:DTAConfig:ServerLatencyTip");

            lblBestServer = new XNALabel(WindowManager);
            lblBestServer.Name = "lblBestServer";
            lblBestServer.ClientRectangle = new Rectangle(lblDescription.X, lblDescription.Bottom + 12, Width - 24, 20);
            lblBestServer.Text = "Best server: N/A".L10N("UI:DTAConfig:BestServer");

            chkAutoCheck = new XNAClientCheckBox(WindowManager);
            chkAutoCheck.Name = "chkAutoCheck";
            chkAutoCheck.ClientRectangle = new Rectangle(lblBestServer.X, lblBestServer.Bottom + 24, 0, 0);
            chkAutoCheck.Text = "Check for updates automatically".L10N("UI:DTAConfig:AutoCheckUpdate");

            btnForceUpdate = new XNAClientButton(WindowManager);
            btnForceUpdate.Name = "btnForceUpdate";
            btnForceUpdate.ClientRectangle = new Rectangle(Width - 145, chkAutoCheck.Bottom + 24, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnForceUpdate.Text = "Force Update".L10N("UI:DTAConfig:ForceUpdate");
            btnForceUpdate.LeftClick += BtnForceUpdate_LeftClick;

            lblChannel = new XNALabel(WindowManager);
            lblChannel.Name = "lblChannel";
            lblChannel.ClientRectangle = new Rectangle(chkAutoCheck.X, btnForceUpdate.Bottom + 24, 60, 20);
            lblChannel.Text = "Update Channel".L10N("UI:DTAConfig:UpdateChannel");

            ddChannel = new XNADropDown(WindowManager);
            ddChannel.Name = "ddChannel";
            ddChannel.ClientRectangle = new Rectangle(lblChannel.X + 80, lblChannel.Y, 100, 30);
            ddChannel.AddItem("Stable".L10N("UI:DTAConfig:UpdateChannelStable"));
            ddChannel.AddItem("Insider".L10N("UI:DTAConfig:UpdateChannelInsider"));
            ddChannel.SelectedIndexChanged += DdChannel_SelectedIndexChanged;

            lbServerList = new XNAListBox(WindowManager);
            lbServerList.Name = "lbServerList";
            lbServerList.ClientRectangle = new Rectangle(12, ddChannel.Bottom + 24, Width - 24, 150);
            lbServerList.LineHeight = 20;
            lbServerList.DefaultItemColor = Color.White;

            AddChild(lblDescription);
            AddChild(lblBestServer);
            AddChild(chkAutoCheck);
            AddChild(btnForceUpdate);
            AddChild(lblChannel);
            AddChild(ddChannel);
            AddChild(lbServerList);
        }

        private void DdChannel_SelectedIndexChanged(object sender, EventArgs e)
        {
            PopulateServerList();
        }

        private void PopulateServerList()
        {
            lbServerList.Items.Clear();

            if (Updater.UpdaterServers != null && Updater.UpdaterServers.Count > 0)
            {
                var servers = Updater.UpdaterServers.Where(f => f.type.Equals(ddChannel.SelectedIndex)).ToList();
                foreach (var server in servers)
                {
                    lbServerList.AddItem($"{server.name}{(!string.IsNullOrEmpty(server.location) ? $" ({server.location})" : string.Empty)} - 延迟: N/A ms");
                }

                RefreshLatencies();
            }
        }

        private void RefreshLatencies()
        {
            Task.Run(() =>
            {
                if (Updater.UpdaterServers == null)
                    return;

                var servers = Updater.UpdaterServers.Where(f => f.type.Equals(ddChannel.SelectedIndex)).ToList();
                var serverLatencies = new Dictionary<ClientCore.Entity.UpdaterServer, long>();
                List<string> updatedItems = new List<string>();

                foreach (var server in servers)
                {
                    long latency = GetServerLatency(server);
                    serverLatencies[server] = latency;
                    string latencyText = latency >= 0 ? latency.ToString() : "--";
                    updatedItems.Add($"{server.name}{(!string.IsNullOrEmpty(server.location) ? $" ({server.location})" : string.Empty)} - 延迟: {latencyText} ms");
                }

                ClientCore.Entity.UpdaterServer? bestServer = null;
                long bestLatency = long.MaxValue;
                foreach (var kvp in serverLatencies)
                {
                    if (kvp.Value >= 0 && kvp.Value < bestLatency)
                    {
                        bestLatency = kvp.Value;
                        bestServer = kvp.Key;
                    }
                }

                WindowManager.AddCallback(() =>
                {
                    lbServerList.Items.Clear();
                    foreach (var item in updatedItems)
                        lbServerList.AddItem(item);

                    if (bestServer.HasValue)
                    {
                        var server = bestServer.Value;
                        lblBestServer.Text = server.name +
                            (!string.IsNullOrEmpty(server.location) ? $" ({server.location})" : string.Empty) +
                            $" 延迟: {bestLatency}ms";
                    }
                    else
                    {
                        lblBestServer.Text = "No servers found available".L10N("UI:DTAConfig:NoServerAvailable");
                    }
                });
            });
        }

        private long GetServerLatency(ClientCore.Entity.UpdaterServer server)
        {
            try
            {
                string host = new Uri(server.url).Host;
                using (Ping ping = new Ping())
                {
                    PingReply reply = ping.Send(host, 1000);
                    if (reply.Status == IPStatus.Success)
                        return reply.RoundtripTime;
                }
            }
            catch
            {
            }
            return -1;
        }

        private void BtnForceUpdate_LeftClick(object sender, EventArgs e)
        {
            var msgBox = new XNAMessageBox(WindowManager, "Force Update Confirmation".L10N("UI:DTAConfig:ForceUpdateConfirmTitle"),
                ("WARNING: Force update will result in files being re-verified" + Environment.NewLine +
                "and re-downloaded. While this may fix problems with game" + Environment.NewLine +
                "files, this also may delete some custom modifications" + Environment.NewLine +
                "made to this installation. Use at your own risk!" +
                Environment.NewLine + Environment.NewLine +
                "If you proceed, the options window will close and the" + Environment.NewLine +
                "client will proceed to checking for updates." +
                Environment.NewLine + Environment.NewLine +
                "Do you really want to force update?" + Environment.NewLine).L10N("UI:DTAConfig:ForceUpdateConfirmText"), XNAMessageBoxButtons.YesNo);
            msgBox.Show();
            msgBox.YesClickedAction = ForceUpdateMsgBox_YesClicked;
        }

        private void ForceUpdateMsgBox_YesClicked(XNAMessageBox obj)
        {
            Updater.ClearVersionInfo();
            OnForceUpdate?.Invoke(this, EventArgs.Empty);
        }

        public override void Load()
        {
            base.Load();

            if (Updater.UpdaterServers != null && Updater.UpdaterServers.Count > 0)
            {
                if (IniSettings.Update != -1)
                    ddChannel.SelectedIndex = IniSettings.Update.Value;

                chkAutoCheck.Checked = IniSettings.CheckForUpdates;

                lblBestServer.Text = "Best server: N/A".L10N("UI:DTAConfig:BestServer");

                PopulateServerList();

                latencyRefreshTimer?.Dispose();
                latencyRefreshTimer = new System.Threading.Timer((state) =>
                {
                    RefreshLatencies();
                }, null, 1000, 10000);
            }
        }

        public override bool Save()
        {
            bool restartRequired = base.Save();

            IniSettings.CheckForUpdates.Value = chkAutoCheck.Checked;

            if (IniSettings.Update.Value != ddChannel.SelectedIndex)
            {
                restartRequired = true;
                IniSettings.Update.Value = ddChannel.SelectedIndex;
            }

            NetWorkINISettings.Instance?.SetServerList();

            return restartRequired;
        }

        public override void ToggleMainMenuOnlyOptions(bool enable)
        {
            btnForceUpdate.AllowClick = enable;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                latencyRefreshTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
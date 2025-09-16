using System;
using System.Collections.Generic;
using Ra2Client.Domain.Multiplayer.CnCNet;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System.Threading.Tasks;

namespace Ra2Client.DXGUI.Multiplayer.CnCNet
{
    /// <summary>
    /// A list box for listing CnCNet tunnel servers.
    /// </summary>
    class TunnelListBox : XNAMultiColumnListBox
    {
        public TunnelListBox(WindowManager windowManager, TunnelHandler tunnelHandler) : base(windowManager)
        {
            this.tunnelHandler = tunnelHandler;

            tunnelHandler.TunnelsRefreshed += TunnelHandler_TunnelsRefreshed;
            tunnelHandler.TunnelPinged += TunnelHandler_TunnelPinged;

            SelectedIndexChanged += TunnelListBox_SelectedIndexChanged;

            int headerHeight = (int)Renderer.GetTextDimensions("Name", HeaderFontIndex).Y;

            Width = 466;
            Height = LineHeight * 12 + headerHeight + 3;
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            AddColumn("Name".L10N("UI:Main:NameHeader"), 238);
            AddColumn("Official".L10N("UI:Main:OfficialHeader"), 70);
            AddColumn("Ping".L10N("UI:Main:PingHeader"), 60);
            AddColumn("Players".L10N("UI:Main:PlayersHeader"), 88);
            AllowRightClickUnselect = false;
            AllowKeyboardInput = true;
        }

        public event EventHandler ListRefreshed;

        private readonly TunnelHandler tunnelHandler;

        private int bestTunnelIndex = 0;
        private int lowestTunnelRating = int.MaxValue;

        private bool isManuallySelectedTunnel;
        private string manuallySelectedTunnelAddress;


        /// <summary>
        /// Selects a tunnel from the list with the given address.
        /// </summary>
        /// <param name="address">The address of the tunnel server to select.</param>
        public void SelectTunnel(string address)
        {
            int index = tunnelHandler.Tunnels.FindIndex(t => t.Address == address);
            if (index > -1)
            {
                SelectedIndex = index;
                isManuallySelectedTunnel = true;
                manuallySelectedTunnelAddress = address;
            }
        }

        private int GetMinms()
        {
            int pingMin = 100000; //最低延迟  
            // int people;        //平均人数
            int index = 0;        //最适合索引

            for (int i = 0; i < ItemCount; i++)
            {
                try
                {
                    int ping = Convert.ToInt32(GetItem(2, i).Text.Replace(" ms", "").ToString());
                    int people = Convert.ToInt32(GetItem(3, i).Text.Split('/')[0].Replace(" ", "").ToString());
                    if (ping < pingMin && people != 0)
                    {
                        pingMin = ping;
                        index = i;
                    }
                }
                catch
                {
                    continue;
                }
            }
            return index;
        }

        /// <summary>
        /// Gets whether or not a tunnel from the list with the given address is selected.
        /// </summary>
        /// <param name="address">The address of the tunnel server</param>
        /// <returns>True if tunnel with given address is selected, otherwise false.</returns>
        public bool IsTunnelSelected(string address) =>
            tunnelHandler.Tunnels.FindIndex(t => t.Address == address) == SelectedIndex;

        private void TunnelHandler_TunnelsRefreshed(object sender, EventArgs e)
        {
            ClearItems();

            int tunnelIndex = 0;

            foreach (CnCNetTunnel tunnel in tunnelHandler.Tunnels)
            {
                string[] info =
                [
                    tunnel.Name,
                    Conversions.BooleanToString(tunnel.Official, BooleanStringStyle.YESNO),
                    tunnel.PingInMs < 0 ? "Unknown".L10N("UI:Main:UnknownPing") : tunnel.PingInMs + " ms",
                    tunnel.Clients + " / " + tunnel.MaxClients,
                ];

                AddItem(info, true);

                if ((tunnel.Official || tunnel.Recommended) && tunnel.PingInMs > -1)
                {
                    int rating = GetTunnelRating(tunnel);
                    if (rating < lowestTunnelRating)
                    {
                        bestTunnelIndex = tunnelIndex;
                        lowestTunnelRating = rating;
                    }
                }

                tunnelIndex++;
            }

            if (tunnelHandler.Tunnels.Count > 0)
            {
                if (!isManuallySelectedTunnel)
                {
                    SelectedIndex = bestTunnelIndex;
                    isManuallySelectedTunnel = false;
                }
                else
                {
                    int manuallySelectedIndex = tunnelHandler.Tunnels.FindIndex(t => t.Address == manuallySelectedTunnelAddress);

                    if (manuallySelectedIndex == -1)
                    {
                        SelectedIndex = bestTunnelIndex;
                        isManuallySelectedTunnel = false;
                    }
                    else
                        SelectedIndex = manuallySelectedIndex;
                }
            }

            ListRefreshed?.Invoke(this, EventArgs.Empty);
        }

        private readonly object tunnelLock = new object();

        private void TunnelHandler_TunnelPinged(int tunnelIndex)
        {
           lock (tunnelLock)
           {
               if (tunnelHandler?.Tunnels == null)
               {
                 Logger.Log("隧道列表未初始化");
                 return;
               }

            if (tunnelIndex < 0 || tunnelIndex >= tunnelHandler.Tunnels.Count)
            {
                Logger.Log($"无效的隧道索引: {tunnelIndex}，当前隧道数量: {tunnelHandler.Tunnels.Count}");
                return;
            }

            XNAListBoxItem lbItem = GetItem(2, tunnelIndex);
            CnCNetTunnel tunnel = tunnelHandler.Tunnels[tunnelIndex];

            if (tunnel.PingInMs == -1)
                lbItem.Text = "Unknown".L10N("UI:Main:UnknownPing");
            else
            {
                lbItem.Text = tunnel.PingInMs + " ms";
                int rating = GetTunnelRating(tunnel);

                if (isManuallySelectedTunnel)
                    return;

                if ((tunnel.Recommended || tunnel.Official) && rating < lowestTunnelRating)
                {
                    bestTunnelIndex = tunnelIndex;
                    lowestTunnelRating = rating;
                    SelectedIndex = tunnelIndex;
                }
            }

            int safeIndex = tunnelIndex;
            Task.Run(() =>
            {
                lock (tunnelLock)
                {
                    SelectedIndex = GetMinms();
                }
            });
          }
        }

        private int GetTunnelRating(CnCNetTunnel tunnel)
        {
            double usageRatio = (double)tunnel.Clients / tunnel.MaxClients;

            if (usageRatio == 0)
                usageRatio = 0.1;

            usageRatio *= 100.0;

            // 计算评分
            double result = Math.Pow(tunnel.PingInMs, 2.0) * usageRatio;

            // 限制结果在 int 的范围内
            result = Math.Clamp(result, int.MinValue, int.MaxValue);

            return (int)result; // 安全转换
        }

        private void TunnelListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!IsValidIndexSelected())
                return;

            isManuallySelectedTunnel = true;
            manuallySelectedTunnelAddress = tunnelHandler.Tunnels[SelectedIndex].Address;
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ClientCore;
using Ra2Client.Online;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;

namespace Ra2Client.Domain.Multiplayer.CnCNet
{
    public class TunnelHandler : GameComponent
    {
        /// <summary>
        /// Determines the time between pinging the current tunnel (if it's set).
        /// </summary>
        private const double CURRENT_TUNNEL_PING_INTERVAL = 20.0;

        /// <summary>
        /// A reciprocal to the value which determines how frequent the full tunnel
        /// refresh would be done instead of just pinging the current tunnel (1/N of 
        /// current tunnel ping refreshes would be substituted by a full list refresh).
        /// Multiply by <see cref="CURRENT_TUNNEL_PING_INTERVAL"/> to get the interval 
        /// between full list refreshes.
        /// </summary>
        private const uint CYCLES_PER_TUNNEL_LIST_REFRESH = 6;

        private const int SUPPORTED_TUNNEL_VERSION = 2;

        public TunnelHandler(WindowManager wm, CnCNetManager connectionManager) : base(wm.Game)
        {
            this.wm = wm;
            this.connectionManager = connectionManager;

            wm.Game.Components.Add(this);

            Enabled = false;

            connectionManager.Connected += ConnectionManager_Connected;
            connectionManager.Disconnected += ConnectionManager_Disconnected;
            connectionManager.ConnectionLost += ConnectionManager_ConnectionLost;
        }

        public List<CnCNetTunnel> Tunnels { get; private set; } = new List<CnCNetTunnel>();
        public CnCNetTunnel CurrentTunnel { get; set; } = null;

        public event EventHandler TunnelsRefreshed;
        public event EventHandler CurrentTunnelPinged;
        public event Action<int> TunnelPinged;

        private WindowManager wm;
        private CnCNetManager connectionManager;

        private TimeSpan timeSinceTunnelRefresh = TimeSpan.MaxValue;
        private uint skipCount = 0;

        private void DoTunnelPinged(int index)
        {
            if (TunnelPinged != null)
                wm.AddCallback(TunnelPinged, index);
        }

        private void DoCurrentTunnelPinged()
        {
            if (CurrentTunnelPinged != null)
                wm.AddCallback(CurrentTunnelPinged, this, EventArgs.Empty);
        }

        private void ConnectionManager_Connected(object sender, EventArgs e) => Enabled = true;

        private void ConnectionManager_ConnectionLost(object sender, Online.EventArguments.ConnectionLostEventArgs e) => Enabled = false;

        private void ConnectionManager_Disconnected(object sender, EventArgs e) => Enabled = false;
        private List<CnCNetTunnel> tunnels2 = new List<CnCNetTunnel>();
        private void RefreshTunnelsAsync()
        {
            List<CnCNetTunnel> tunnels = [
                CnCNetTunnel.Parse("110.42.111.242:50000;China;CN;中国华东服务器-EA01;0;0;250;0;0;0;2;0"),
                CnCNetTunnel.Parse("111.173.106.89:50000;China;CN;中国华中服务器-CT01;0;0;250;0;0;0;2;0"),
                CnCNetTunnel.Parse("43.248.118.243:50000;China;CN;中国华东服务器-EA02;0;0;250;0;0;0;2;0"),
                CnCNetTunnel.Parse("43.248.128.47:50000;China;CN;中国华东服务器-EA03;0;0;250;0;0;0;2;0"),
                CnCNetTunnel.Parse("0.0.0.0:50000;China;CN;中国西南服务器1-本区域待上线;0;0;250;0;0;0;2;0"),
                CnCNetTunnel.Parse("117.72.98.111:50000;China;CN;中国华北服务器-NR01;0;0;250;0;0;0;2;0"),
                CnCNetTunnel.Parse("39.106.254.31:50000;China;CN;中国华北服务器-NR02;0;0;250;0;0;0;2;0"),
                CnCNetTunnel.Parse("8.134.110.28:50000;China;CN;中国华南服务器-SU01;0;0;250;0;0;0;2;0"),
                CnCNetTunnel.Parse("116.205.231.144:50000;China;CN;中国华南服务器-SU02;0;0;250;0;0;0;2;0"),
                CnCNetTunnel.Parse("0.0.0.0:50000;Hong Kong;HK;亚洲服务器1-本区域待上线;0;0;250;0;0;0;2;0"),
                CnCNetTunnel.Parse("154.12.187.10:50000;United States;US;北美服务器-US01;0;0;250;0;0;0;2;0"),
                CnCNetTunnel.Parse("45.88.193.16:50000;United States;US;北美服务器-US02;0;0;250;0;0;0;2;0"),
                ];


            Task.Factory.StartNew(() =>
            {
                WindowManager.progress.Report("获取联机服务器列表...");
                tunnels2 = RefreshTunnels();
                tunnels.AddRange(tunnels2);
                wm.AddCallback(new Action<List<CnCNetTunnel>>(HandleRefreshedTunnels), tunnels);
                WindowManager.progress.Report(string.Empty );
            });
            

            wm.AddCallback(new Action<List<CnCNetTunnel>>(HandleRefreshedTunnels), tunnels);
        }

        private void HandleRefreshedTunnels(List<CnCNetTunnel> tunnels)
        {
            if (tunnels.Count > 0)
                Tunnels = tunnels;

            TunnelsRefreshed?.Invoke(this, EventArgs.Empty);

            Task[] pingTasks = new Task[Tunnels.Count];

            for (int i = 0; i < Tunnels.Count; i++)
            {
            //    if (Tunnels[i].Official || Tunnels[i].Recommended)
                    pingTasks[i] = PingListTunnelAsync(i);
            }

            if (CurrentTunnel != null)
            {
                var updatedTunnel = Tunnels.Find(t => t.Address == CurrentTunnel.Address && t.Port == CurrentTunnel.Port);
                if (updatedTunnel != null)
                {
                    // don't re-ping if the tunnel still exists in list, just update the tunnel instance and
                    // fire the event handler (the tunnel was already pinged when traversing the tunnel list)
                    CurrentTunnel = updatedTunnel;
                    DoCurrentTunnelPinged();
                }
                else
                {
                    // tunnel is not in the list anymore so it's not updated with a list instance and pinged
                    PingCurrentTunnelAsync();
                }
            }
        }

        private Task PingListTunnelAsync(int index)
        {
            return Task.Factory.StartNew(() =>
            {
                Tunnels[index].UpdatePing();
                DoTunnelPinged(index);
            });
        }

        private Task PingCurrentTunnelAsync(bool checkTunnelList = false)
        {
            return Task.Factory.StartNew(() =>
            {
                CurrentTunnel.UpdatePing();
                DoCurrentTunnelPinged();

                if (checkTunnelList)
                {
                    int tunnelIndex = Tunnels.FindIndex(t => t.Address == CurrentTunnel.Address && t.Port == CurrentTunnel.Port);
                    if (tunnelIndex > -1)
                        DoTunnelPinged(tunnelIndex);
                }
            });
        }

        /// <summary>
        /// Downloads and parses the list of CnCNet tunnels.
        /// </summary>
        /// <returns>A list of tunnel servers.</returns>
        private List<CnCNetTunnel> RefreshTunnels()
        {
            FileInfo tunnelCacheFile = SafePath.GetFile(ProgramConstants.ClientUserFilesPath, "tunnel_cache");

            List<CnCNetTunnel> returnValue = new List<CnCNetTunnel>();

            WebClient client = new WebClient();

            byte[] data;

            Logger.Log("Fetching tunnel server info.");

            try
            {
                data = client.DownloadData(MainClientConstants.CNCNET_TUNNEL_LIST_URL);
            }
            catch (Exception ex)
            {
                Logger.Log("Error when downloading tunnel server info: " + ex.Message);
                Logger.Log("Retrying.");
                try
                {
                    data = client.DownloadData(MainClientConstants.CNCNET_TUNNEL_LIST_URL);
                }
                catch
                {
                    if (!tunnelCacheFile.Exists)
                    {
                        Logger.Log("Tunnel cache file doesn't exist!");
                        return returnValue;
                    }
                    else
                    {
                        Logger.Log("Fetching tunnel server list failed. Using cached tunnel data.");
                        data = File.ReadAllBytes(tunnelCacheFile.FullName);
                    }
                }
            }

            string convertedData = Encoding.Default.GetString(data);

            string[] serverList = convertedData.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            // skip first header item ("address;country;countrycode;name;password;clients;maxclients;official;latitude;longitude;version;distance")
            foreach (string serverInfo in serverList.Skip(1))
            {
                try
                {
                    CnCNetTunnel tunnel = CnCNetTunnel.Parse(serverInfo);

                    if (tunnel == null)
                        continue;

                    if (tunnel.RequiresPassword)
                        continue;

                    if (tunnel.Version != SUPPORTED_TUNNEL_VERSION)
                        continue;

                    returnValue.Add(tunnel);
                }
                catch (Exception ex)
                {
                    Logger.Log("Caught an exception when parsing a tunnel server: " + ex.Message);
                }
            }

            if (returnValue.Count > 0)
            {
                try
                {
                    if (tunnelCacheFile.Exists)
                        tunnelCacheFile.Delete();

                    DirectoryInfo clientDirectoryInfo = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath);

                    if (!clientDirectoryInfo.Exists)
                        clientDirectoryInfo.Create();

                    File.WriteAllBytes(tunnelCacheFile.FullName, data);
                }
                catch (Exception ex)
                {
                    Logger.Log("Refreshing tunnel cache file failed! Returned error: " + ex.Message);
                }
            }

            return returnValue;
        }

        public override void Update(GameTime gameTime)
        {
            if (timeSinceTunnelRefresh > TimeSpan.FromSeconds(CURRENT_TUNNEL_PING_INTERVAL))
            {
                if (skipCount % CYCLES_PER_TUNNEL_LIST_REFRESH == 0)
                {
                    skipCount = 0;
                    RefreshTunnelsAsync();
                }
                else if (CurrentTunnel != null)
                {
                    PingCurrentTunnelAsync(true);
                }

                timeSinceTunnelRefresh = TimeSpan.Zero;
                skipCount++;
            }
            else
                timeSinceTunnelRefresh += gameTime.ElapsedGameTime;

            base.Update(gameTime);
        }
    }
}

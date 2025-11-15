using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ClientCore;
using Microsoft.Xna.Framework;
using Ra2Client.Online;
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

        private static readonly HttpClient httpClient = new HttpClient();

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

        private bool hasNewTunnelList = false;
        private bool immediateRefreshRequested = false;
        private DateTime lastTunnelRefreshTime = DateTime.MinValue;

        public void RequestImmediateRefresh()
        {
            immediateRefreshRequested = true;
        }

        private void RefreshTunnelsAsync()
        {
            List<CnCNetTunnel> tunnels = [
                //CnCNetTunnel.Parse("127.6.6.6:50000;China;CN;名称里带Reunion的都是重聚官服;0;0;1;0;0;0;2;0"),
                //CnCNetTunnel.Parse("127.8.8.8:50000;China;CN;不要选这里的这两个, 服务器无效;0;0;1;0;0;0;2;0"),
                ];


            Task.Factory.StartNew(async () =>
            {
                WindowManager.progress.Report("获取联机服务器列表...");
                var newTunnels = await RefreshTunnels();
                if (newTunnels.Count > 0 && !newTunnels.SequenceEqual(tunnels2))
                {
                    tunnels2 = newTunnels;
                    hasNewTunnelList = true;
                }
                tunnels.AddRange(tunnels2);
                wm.AddCallback(new Action<List<CnCNetTunnel>>(HandleRefreshedTunnels), tunnels);
                WindowManager.progress.Report(string.Empty);
            });
        }

        private void HandleRefreshedTunnels(List<CnCNetTunnel> tunnels)
        {
            if (tunnels.Count > 0)
                Tunnels = tunnels;

            TunnelsRefreshed?.Invoke(this, EventArgs.Empty);

            Task[] pingTasks = new Task[Tunnels.Count];

            for (int i = 0; i < Tunnels.Count; i++)
            {
                // if (Tunnels[i].Official || Tunnels[i].Recommended)
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
                var tunnel = CurrentTunnel;
                if (tunnel == null) 
                    return;

                tunnel.UpdatePing();
                DoCurrentTunnelPinged();

                if (checkTunnelList)
                {
                    int tunnelIndex = Tunnels.FindIndex(t => t.Address == tunnel.Address && t.Port == tunnel.Port);
                    if (tunnelIndex > -1)
                        DoTunnelPinged(tunnelIndex);
                }
            });
        }

        /// <summary>
        /// Downloads and parses the list of CnCNet tunnels.
        /// </summary>
        /// <returns>A list of tunnel servers.</returns>
        public async Task<List<CnCNetTunnel>> RefreshTunnels()
        {
            FileInfo tunnelCacheFile = SafePath.GetFile(ProgramConstants.ClientUserFilesPath, "tunnel_cache");

            List<CnCNetTunnel> returnValue = new List<CnCNetTunnel>();

            Logger.Log("Fetching tunnel server info.");

            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(MainClientConstants.CurrentTunnelServerUrl);
                response.EnsureSuccessStatusCode();
                byte[] data = await response.Content.ReadAsByteArrayAsync();

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
            }
            catch (Exception ex)
            {
                Logger.Log("Error when downloading tunnel server info: " + ex.Message);
                Logger.Log("Retrying.");
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(MainClientConstants.CurrentTunnelServerUrl);
                    response.EnsureSuccessStatusCode();
                    byte[] data = await response.Content.ReadAsByteArrayAsync();

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
                        catch (Exception parseEx)
                        {
                            Logger.Log("Caught an exception when parsing a tunnel server: " + parseEx.Message);
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
                        catch (Exception cacheEx)
                        {
                            Logger.Log("Refreshing tunnel cache file failed! Returned error: " + cacheEx.Message);
                        }
                    }
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
                        byte[] data = File.ReadAllBytes(tunnelCacheFile.FullName);

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
                            catch (Exception parseEx)
                            {
                                Logger.Log("Caught an exception when parsing a tunnel server: " + parseEx.Message);
                            }
                        }
                    }
                }
            }

            return returnValue;
        }

        public override void Update(GameTime gameTime)
        {
            if (immediateRefreshRequested)
            {
                immediateRefreshRequested = false;
                lastTunnelRefreshTime = DateTime.Now;
                RefreshTunnelsAsync();
            }
            else if (timeSinceTunnelRefresh > TimeSpan.FromSeconds(CURRENT_TUNNEL_PING_INTERVAL))
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

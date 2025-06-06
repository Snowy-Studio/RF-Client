﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Rampastring.Tools;

namespace Ra2Client.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// A CnCNet tunnel server.
    /// </summary>
    public class CnCNetTunnel
    {
        private const int REQUEST_TIMEOUT = 10000; // In milliseconds
        private const int PING_TIMEOUT = 1000;

        public CnCNetTunnel() { }

        /// <summary>
        /// Parses a formatted string that contains the tunnel server's 
        /// information into a CnCNetTunnel instance.
        /// </summary>
        /// <param name="str">The string that contains the tunnel server's information.</param>
        /// <returns>A CnCNetTunnel instance parsed from the given string.</returns>
        public static CnCNetTunnel Parse(string str)
        {
            // For the format, check http://cncnet.org/master-list
            try
            {
                var tunnel = new CnCNetTunnel();
                string[] parts = str.Split(';');

                string address = parts[0];
                string host;
                int port;

                if (address.Count(c => c == ':') > 1)
                {
                    // IPv6 address 
                    int lastColonIndex = address.LastIndexOf(':');
                    host = "[" + address.Substring(0, lastColonIndex) + "]";
                    port = int.Parse(address.Substring(lastColonIndex + 1));
                }
                else
                {
                    // IPv4 address or hostname 
                    string[] detailedAddress = address.Split(':');
                    host = detailedAddress[0];
                    port = int.Parse(detailedAddress[1]);
                }

                tunnel.Address = host;
                tunnel.Port = port;
                tunnel.Country = parts[1];
                tunnel.CountryCode = parts[2];
                tunnel.Name = parts[3];
                tunnel.RequiresPassword = parts[4] != "0";
                tunnel.Clients = int.Parse(parts[5]);
                tunnel.MaxClients = int.Parse(parts[6]);
                int status = int.Parse(parts[7]);
                tunnel.Official = status == 2;
                if (!tunnel.Official)
                    tunnel.Recommended = status == 1;

                CultureInfo cultureInfo = CultureInfo.InvariantCulture;

                tunnel.Latitude = double.Parse(parts[8], cultureInfo);
                tunnel.Longitude = double.Parse(parts[9], cultureInfo);
                tunnel.Version = int.Parse(parts[10]);
                tunnel.Distance = double.Parse(parts[11], cultureInfo);

                return tunnel;
            }
            catch (Exception ex)
            {
                if (ex is FormatException || ex is OverflowException || ex is IndexOutOfRangeException)
                {
                    Logger.Log("Parsing tunnel information failed: " + ex.Message + Environment.NewLine + "Parsed string: " + str);
                    return null;
                }

                throw;
            }
        }

        public string Address { get; private set; }
        public int Port { get; private set; }
        public string Country { get; private set; }
        public string CountryCode { get; private set; }
        public string Name { get; private set; }
        public bool RequiresPassword { get; private set; }
        public int Clients { get; private set; }
        public int MaxClients { get; private set; }
        public bool Official { get; private set; }
        public bool Recommended { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public int Version { get; private set; }
        public double Distance { get; private set; }
        public int PingInMs { get; set; } = -1;

        /// <summary>
        /// Gets a list of player ports to use from a specific tunnel server.
        /// </summary>
        /// <returns>A list of player ports to use.</returns>
        public async Task<List<int>> GetPlayerPortInfoAsync(int playerCount)
        {
            try
            {
                Logger.Log($"Contacting tunnel at {Address}:{Port}");
                string addressString = $"http://{Address}:{Port}/request?clients={playerCount}";
                Logger.Log($"Downloading from {addressString}");

                using (var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(REQUEST_TIMEOUT) })
                {
                    string data = await client.GetStringAsync(new Uri(addressString));
                    data = data.Replace("[", String.Empty);
                    data = data.Replace("]", String.Empty);

                    string[] portIDs = data.Split(',');

                    var playerPorts = new List<int>();
                    foreach (string _port in portIDs)
                    {
                        int port = Convert.ToInt32(_port);
                        playerPorts.Add(port);
                        Logger.Log($"Added port {_port}");
                    }

                    return playerPorts;
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Unable to connect to the specified tunnel server. Returned error message: " + ex.Message);
            }

            return new List<int>();
        }

        public void UpdatePing()
        {
            using Ping p = new Ping();
            try
            {
                PingReply reply = p.Send(IPAddress.Parse(Address), PING_TIMEOUT);
                if (reply.Status == IPStatus.Success)
                    PingInMs = Convert.ToInt32(reply.RoundtripTime);
                else
                {
                    try
                    {
                        using TcpClient tcpClient = new TcpClient();
                        Stopwatch stopwatch = Stopwatch.StartNew();
                        tcpClient.SendTimeout = PING_TIMEOUT;
                        tcpClient.ReceiveTimeout = PING_TIMEOUT;
                        // 尝试连接
                        tcpClient.Connect(Address, Port);

                        // 停止计时
                        stopwatch.Stop();
                        if (tcpClient.Connected)
                            PingInMs = (int)stopwatch.ElapsedMilliseconds;
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Caught an exception when pinging {Name} tunnel server: {ex.Message}");
            }
        }
    }
}
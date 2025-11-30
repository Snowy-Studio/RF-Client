using System;
using System.Net.Http;
using System.Threading;
using ClientCore;
using ClientCore.Settings;

namespace Ra2Client.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// A class for updating of the CnCNet game/player count.
    /// </summary>
    public static class CnCNetPlayerCountTask
    {
        public static int PlayerCount { get; private set; }

        private static int REFRESH_INTERVAL = 60000; // 1 minute

        internal static event EventHandler<PlayerCountEventArgs> CnCNetGameCountUpdated;

        private static string cncnetLiveStatusIdentifier;

        private static readonly HttpClient httpClient = new HttpClient();

        public static void InitializeService(CancellationTokenSource cts)
        {
            cncnetLiveStatusIdentifier = ClientConfiguration.Instance.CnCNetLiveStatusIdentifier;
          //  PlayerCount = GetCnCNetPlayerCount();

            //CnCNetGameCountUpdated?.Invoke(null, new PlayerCountEventArgs(PlayerCount));
           // ThreadPool.QueueUserWorkItem(new WaitCallback(RunService), cts);
        }

        private static void RunService(object tokenObj)
        {
            var waitHandle = ((CancellationTokenSource)tokenObj).Token.WaitHandle;

            while (true)
            {
                if (waitHandle.WaitOne(REFRESH_INTERVAL))
                {
                    // Cancellation signaled
                    return;
                }
                else
                {
                    CnCNetGameCountUpdated?.Invoke(null, new PlayerCountEventArgs(GetCnCNetPlayerCount()));
                }
            }
        }

        //private static async Task<int> GetCnCNetPlayerCount()
        //{
        //    try
        //    {
        //        HttpResponseMessage response = await httpClient.GetAsync("http://api.cncnet.org/status");
        //        response.EnsureSuccessStatusCode();

        //        string info = await response.Content.ReadAsStringAsync();

        //        info = info.Replace("{", String.Empty);
        //        info = info.Replace("}", String.Empty);
        //        info = info.Replace("\"", String.Empty);
        //        string[] values = info.Split(new char[] { ',' });

        //        int numGames = -1;

        //        foreach (string value in values)
        //        {
        //            if (value.Contains(cncnetLiveStatusIdentifier))
        //            {
        //                numGames = Convert.ToInt32(value.Substring(cncnetLiveStatusIdentifier.Length + 1));
        //                return numGames;
        //            }
        //        }

        //        return numGames;
        //    }
        //    catch
        //    {
        //        return -1;
        //    }
        //}

        private static int GetCnCNetPlayerCount()
        {
            return NetWorkINISettings.Get<int>("ChatRoom/getPeopleCount").GetAwaiter().GetResult().Item1;
        }
    }

    internal class PlayerCountEventArgs : EventArgs
    {
        public PlayerCountEventArgs(int playerCount)
        {
            PlayerCount = playerCount;
        }

        public int PlayerCount { get; set; }
    }
}

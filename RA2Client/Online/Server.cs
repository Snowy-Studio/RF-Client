namespace Ra2Client.Online
{
    /// <summary>
    /// A struct containing information on an IRC server.
    /// </summary>
    public struct Server
    {
        public Server(string host, string name, int[] ports, string region = "Default")
        {
            Host = host;
            Name = name;
            Ports = ports;
            Region = region;
        }

        public string Host;
        public string Name;
        public int[] Ports;
        public string Region;
    }
}
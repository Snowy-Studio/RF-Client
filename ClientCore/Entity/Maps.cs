namespace ClientCore.Entity
{
    public class Maps
    {
        public int tx { get; set; }

        public string img { get; set; } = null;
        public string csf { get; set; } = null;
        public string id { get; set; } = "0";
        public string name { get; set; } = null;
        public string maxPlayers { get; set; } = null;
        public string author { get; set; } = null;
        public int type { get; set; } = 0;
        public string base64 { get; set; } = null;
        public string file { get; set; } = null;
        public string otherFile { get; set; } = null;
        public string createTime { get; set; } = null;
        public string updateTime { get; set; } = null;
        public string createUser { get; set; } = "0";
        public int enable { get; set; } = 0;
        public string downCount { get; set; } = "0";
        public double score { get; set; } = 0.0;
        public string description { get; set; } = "";
        public string sha1 { get; set; } = "";
        public string uploadUserName { get; set; } = "";

        public string typeName { get; set; } = "";
        public string rules { get; set; }

        public int ares { get; set; }

        public string enemyHouse { get; set; } = "";

        public string allyHouse { get; set; } = "";

        public bool autoStart { get; set; } = false;

        public Maps() { }
    }

}

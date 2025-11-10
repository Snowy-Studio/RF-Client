namespace ClientCore.Entity
{
    public record class Updater
    {
        public string id { get; set; }
        public string version { get; set; }
        public string file { get; set; }
        public string hash { get; set; }
        public string size { get; set; }
        public string log { get; set; }
        public string channel { get; set; }
        public string updateTime { get; set; }

    }
}

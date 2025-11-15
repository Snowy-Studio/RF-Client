namespace DTAConfig.Entity
{
    public record class Component
    {
        public string id { get; set; }

        public string name { get; set; }

        public string description { get; set; }

        public int type { get; set; }
        public string tags { get; set; }

        public string file { get; set; }

        public long size { get; set; }

        public string hash { get; set; }

        public string uploadTime { get; set; }

        public string uploadUser { get; set; }

        public string passTime { get; set; }

        public string uploadUserName { get; set; }

        public string typeName { get; set; }

        public long downCount { get; set; }

        public string version { get; set; }
        public string apply { get; set; }
        public string author { get; set; }
    }
}

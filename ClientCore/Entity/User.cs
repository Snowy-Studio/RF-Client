namespace ClientCore.Entity
{
    public record class User
    {
        public string id { get; set; }
        public string username { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public string allow_time { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string role { get; set; } = string.Empty;
        public string tag { get; set; } = string.Empty;
        public int side { get; set; } = 3;
        public string badge { get; set; } = "0";
        public string mac { get; set; }
    }
}

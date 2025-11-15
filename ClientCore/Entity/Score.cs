namespace ClientCore.Entity
{
    public record class Score
    {
        public string name { get; set; }
        public double score { get; set; }
        public string brief { get; set; }
        public string missionPack { get; set; }
        public int total { get; set; }
    }
}

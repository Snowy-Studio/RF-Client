namespace DTAConfig.Entity
{
    public record class QuestionBank
    {
        public string? id { get; set; }
        public string name { get; set; } = "";
        public string problem { get; set; } = "";
        public string options { get; set; } = "";
        public int answer { get; set; } = 0;
        public int difficulty { get; set; } = 0;
        public string type { get; set; } = "";
        public int enable { get; set; } = 0;
    }
}

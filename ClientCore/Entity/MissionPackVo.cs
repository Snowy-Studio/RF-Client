using System.Collections.Generic;

namespace ClientCore.Entity
{
    public record class MissionPackVo
    {
        public string? id { get; set; }

        public string name { get; set; } = "";

        public string? description { get; set; }

        public List<int>? camp { get; set; }

        public List<string>? tags { get; set; }

        public string file { get; set; } = "";

        public string? author { get; set; }

        public int missionCount { get; set; } = 1;

        public int? year { get; set; }

        public int ares { get; set; }

        public int phobos { get; set; }

        public int tx { get; set; }

        public int difficulty { get; set; }

        public int gameType { get; set; }

        public string? link { get; set; }

        public string updateTime { get; set; }
    }

}

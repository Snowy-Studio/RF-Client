using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientCore.Entity
{
    public record ModVo
    {
        public string id { get; init; }
        public string name { get; init; } = "";
        public string description { get; init; } = "";
        public int? gameType { get; init; } = 1;
        public List<string>? tags { get; init; } = [];
        public string? uploadUserName { get; init; } = "";
        public string createUser { get; init; } = "0";
        public string? author { get; init; }
        public string downCount { get; init; } = "0";
        public string compatible { get; init; } = "YR";
        public int enable { get; init; } = 0;
        public string hash { get; init; } = "";
        public string file { get; init; } = "";
        public string img { get; init; } = "";
        public string downLink { get; init; } = "";
        public string link { get; init; } = "";
        public string updateTime { get; init; } = "";
    }
}

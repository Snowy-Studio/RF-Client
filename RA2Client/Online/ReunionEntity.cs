using System;

namespace Ra2Client.Online
{
    [Serializable]
    public struct QueryObj
    {
        public string id {  get; set; }

        public string op { get; set; }

        public string score { get; set; }

        public string key { get; set; }

        public QueryObj(string id, string op, string score, string key)
        {
            this.id = id;
            this.op = op;
            this.score = score;
            this.key = key;
        }
    }

    [Serializable]
    public struct RatingObejct
    {
        public string name { get; set; }

        public string score { get; set; }

        public string total { get; set; }
    }
}

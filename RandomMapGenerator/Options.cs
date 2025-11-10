using CommandLine;

namespace RandomMapGenerator
{
    public class Options
    {

        [Option('w', "width", Required = false, HelpText = "确定地图的宽度")]
        public int Width { get; set; }

        [Option('h', "height", Required = false, HelpText = "确定地图的高度")]

        public int Height { get; set; }

        [Option('n', "name", Default = "", Required = false, HelpText = "设置地图的名称")]
        public string Name { get; set; }

        [Option("type", Required = true, HelpText = "指定地图的类型（WorkingFolder下的子文件夹）")]
        public string Type { get; set; }

        [Option('g', "gamemode", Required = false, HelpText = "指定地图的游戏类型")]
        public string Gamemode { get; set; }

        [Option("nep", Required = false, HelpText = "在东北方向放置玩家（参数=数量）")]
        public int NE { get; set; }

        [Option("nwp", Required = false, HelpText = "在西北方向放置玩家（参数=数量）")]
        public int NW { get; set; }

        [Option("sep", Required = false, HelpText = "在东南方向放置玩家（参数=数量）")]
        public int SE { get; set; }

        [Option("swp", Required = false, HelpText = "在西南方向放置玩家（参数=数量）")]
        public int SW { get; set; }

        [Option("np", Required = false, HelpText = "在正北方向放置玩家（参数=数量）")]
        public int N { get; set; }

        [Option("sp", Required = false, HelpText = "在正南方向放置玩家（参数=数量）")]
        public int S { get; set; }

        [Option("wp", Required = false, HelpText = "在正西方向放置玩家（参数=数量）")]
        public int W { get; set; }

        [Option("ep", Required = false, HelpText = "在正东方向放置玩家（参数=数量）")]
        public int E { get; set; }

        [Option("no-thumbnail", Default = false ,Required = false, HelpText = "不渲染地图全图")]
        public bool NoThumbnail { get; set; }

        //[Option("no-thumbnail-output", Default = false, Required = false, HelpText = "不输出地图全图，但是会生成载入缩略图")]
        //public bool NoThumbnailOutput { get; set; }

        public int TotalRandom { get; set; }

        public int Number { get; set; }

        public bool DamangedBuilding { get; set; }

        public double Smudge { get; set; }

        public string 输出目录 { get; set; }

    }
}

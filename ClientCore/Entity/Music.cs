namespace DTAConfig.Entity
{
    public record Music
    {
        /// <summary>
        /// 音乐名
        /// </summary>
        public string Name { get; set; }
        public string Section { get; set; }
        /// <summary>
        /// 中文名
        /// </summary>
        public string CName { get; set; }

        /// <summary>
        /// 大小
        /// </summary>
        public string Size { get; set; }
        /// <summary>
        /// 音乐时长
        /// </summary>
        public string Length { get; set; }
        /// <summary>
        /// 音乐文件
        /// </summary>
        public string Sound { get; set; }
        public string Scenario { get; set; }
        /// <summary>
        /// 能听到的阵营
        /// </summary>
        public string Side { get; set; }
        /// <summary>
        /// 是否始终循环
        /// </summary>
        public string Repeat { get; set; }
        public string Normal { get; set; }
        public string Path { get; set; }
    }
}

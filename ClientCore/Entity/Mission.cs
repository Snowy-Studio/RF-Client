using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Localization;
using Rampastring.Tools;

namespace DTAConfig.Entity
{
    /// <summary>
    /// A Tiberian Sun Path listed in Battle(E).ini.
    /// </summary>
    public partial class Mission
    {

        public Mission(IniFile iniFile, string sectionName, int index)
        {
            this.SectionName = sectionName;
            if (MissionPack.MissionPacks != null && MissionPack.MissionPacks.Count > 0)
            {
                MPack = MissionPack.MissionPacks.Find(m => m.ID == iniFile.GetValue(sectionName, "MissionPack", string.Empty));
                MPack?.Missions.Add(this);
            }
            Index = index;
            CD = iniFile.GetValue(sectionName, nameof(CD), 0);
            Side = iniFile.GetValue(sectionName, nameof(Side), 0);
            GUIName = iniFile.GetValue(sectionName, "Description", MPack != null ? MPack.Name : "Undefined Path");
            Ares = iniFile.GetValue(sectionName, "Ares", MPack != null ? MPack.Ares : 0);
            Phobos = iniFile.GetValue(sectionName, "Phobos", MPack != null ? MPack.Phobos : 0);
            IconPath = iniFile.GetValue(sectionName, "SideName", MPack != null ? MPack.Sides: string.Empty);
            GUIDescription = iniFile.GetValue(sectionName, "LongDescription", MPack!=null?MPack.LongDescription: GUIName).L10N("UI:MissionText:" + sectionName);
            FinalMovie = iniFile.GetValue(sectionName, nameof(FinalMovie), "none");
            RequiredAddOn = iniFile.GetValue(sectionName, nameof(RequiredAddOn), false);
            Enabled = iniFile.GetValue(sectionName, nameof(Enabled), true);
            BuildOffAlly = iniFile.GetValue(sectionName, nameof(BuildOffAlly), MPack?.BuildOffAlly ?? false);
            PlayerAlwaysOnNormalDifficulty = iniFile.GetValue(sectionName, nameof(PlayerAlwaysOnNormalDifficulty), false);
            var modStr = iniFile.GetValue(sectionName,"Mod", MPack != null ? MPack.Mod:string.Empty);
         
            if (!string.IsNullOrEmpty(modStr))
            {
                Mod = [.. modStr.Split(',')];
                DefaultMod = iniFile.GetValue(sectionName, "DefaultMod", MPack != null ? MPack.DefaultMod : Mod[0]);
            }

            Path = iniFile.GetValue(sectionName, "Mission", MPack != null ? MPack.FilePath : string.Empty);
            Scenario = iniFile.GetValue(sectionName, nameof(Scenario), string.Empty);
            Difficulty = iniFile.GetValue(sectionName, "Difficulty", MPack != null ? MPack.Difficulty:"中等"); //难度筛选用
            Other = iniFile.GetValue(sectionName, "Other", MPack?.Other ?? false);
           
            YR = iniFile.GetValue(sectionName, "YR", MPack == null || MPack.YR);
           
            MissionInfo = string.Empty;
            
            // 如果有中文，那就自动换行。（英文会自己换行不清楚为什么）
            //if (HasChinese(GUIDescription))
            //{

            //    var description = string.Empty;
            //    foreach (var s in GUIDescription.Split("\r\n"))
            //    {
            //        var s1 = s + '@';
            //        if (s1.Length > 39)
            //        {

            //            s1 = InsertFormat(s1, 39, "@");
            //        }
            //        description += s1;
            //    }

            //    GUIDescription = description;
            //}
            GUIDescription = GUIDescription.Replace("@", Environment.NewLine);
        }

        public bool HasChinese(string str)
        {
            return 判断是否有中文正则().IsMatch(str);
        }

        /// <summary>
        /// 是否为玩家导入的任务包(不参与评分)
        /// </summary>
        public bool Other { get; set; }
        /// <summary>
        /// 任务索引
        /// </summary>
        public int Index { get; }
        /// <summary>
        /// 暂未使用
        /// </summary>
        public int CD { get; }
        /// <summary>
        /// 阵营
        /// </summary>
        public int Side { get; }
        /// <summary>
        /// 使用的地图
        /// </summary>
        public string Scenario { get; }
        /// <summary>
        /// 任务名称
        /// </summary>
        public string GUIName { get; }
        public int Ares { get; }

        public int Phobos { get; }

        /// <summary>
        /// 任务图标
        /// </summary>
        public string IconPath { get; }
        /// <summary>
        /// 任务简报
        /// </summary>
        public string GUIDescription { get; }
        /// <summary>
        /// 未知，暂未使用
        /// </summary>
        public string FinalMovie { get; }
        /// <summary>
        /// 未知，暂未使用
        /// </summary>
        public bool RequiredAddOn { get; }
        /// <summary>
        /// 任务是否启用
        /// </summary>
        public bool Enabled { get; }
        /// <summary>
        ///  属于哪个任务包
        /// </summary>
        public MissionPack MPack { get; }
        /// <summary>
        /// 友军是否可建造
        /// </summary>
        public bool BuildOffAlly { get; }
        /// <summary>
        /// 是否强制难度1，即只做了一种难度
        /// </summary>
        public bool PlayerAlwaysOnNormalDifficulty { get; }
        /// <summary>
        /// 该任务包使用的Mod列表
        /// </summary>
        public List<string> Mod { get; }
        /// <summary>
        /// 默认使用的Mod
        /// </summary>
        public string DefaultMod { get; }
        /// <summary>
        /// 任务注册名
        /// </summary>
        public string SectionName { get; }
        /// <summary>
        /// 任务解析出的信息
        /// </summary>
        public string MissionInfo { get; set; }
        /// <summary>
        /// 任务所在的文件夹
        /// </summary>
        public string Path { get; }
        /// <summary>
        /// 任务难度
        /// </summary>
        public string Difficulty { get; }
        /// <summary>
        /// 是否是尤复任务
        /// </summary>
        public bool YR { get; set; }

        /// <summary>
        /// 字符串插入
        /// </summary>
        /// <param name="input"></param>
        /// <param name="interval"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private string InsertFormat(string input, int interval, string value)
        {
            for (int i = interval; i < input.Length; i += interval + 1)
                input = input.Insert(i, value);
            return input;
        }

        private static readonly Regex 判断是否有中文正则实例 = new Regex(@"[\u4e00-\u9fa5]", RegexOptions.Compiled);

        public static Regex 判断是否有中文正则()
        {
            return 判断是否有中文正则实例;
        }
    }
}

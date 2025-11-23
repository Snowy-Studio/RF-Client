using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClientCore;
using Rampastring.Tools;

namespace DTAConfig.Entity;

/// <summary>
/// 任务包
/// </summary>
public class MissionPack : InfoBaseClass
{
    public MissionPack()
    {
        
    }

    public static List<MissionPack> MissionPacks = new();
    /// <summary>
    /// 查询ID是否存在
    /// </summary>
    /// <param name="id">需要查询的ID</param>
    /// <returns>是否存在</returns>
    public static bool QueryID(string id)
        => MissionPacks.Find(m => m.ID == id) != null;

    public MissionPack(IniFile iniFile, string id)
    {

        ID = id;
        LongDescription = iniFile.GetValue(ID, "LongDescription", 
            iniFile.GetValue(ID, "Description", 
                iniFile.GetValue(ID, "Name", ID
                )
                )
            );
        Description = iniFile.GetValue(ID, "Description", iniFile.GetValue(ID, "Name", ID));
        FilePath = iniFile.GetValue(ID, "Mission", ID);
        UpdateTime = iniFile.GetValue(ID, "UpdateTime", string.Empty);
        // FileName = iniFile.FileName;
        Sides = iniFile.GetValue(ID, "SideName", string.Empty);
        Difficulty = iniFile.GetValue(ID, "Difficulty", "中等");

        // 使用HashSet来暂存Mod，自动排除重复项
        HashSet<string> modSet = new HashSet<string>();

        foreach (var mod in iniFile.GetValue(ID, "Mod", string.Empty).Split(','))
        {
            // 调用GetCompatibleMods可能会返回多个用逗号分隔的Mod，需要再次分割
            var compatibleMods = Entity.Mod.GetCompatibleMods(mod).Split(',');
            foreach (var compatibleMod in compatibleMods)
            {
                // 将每个兼容的Mod添加到HashSet中，重复的会被自动忽略
                modSet.Add(compatibleMod.Trim()); // 使用Trim确保添加前没有多余的空格
            }
        }
        Mod = string.Join(",", modSet);
        DefaultMod = iniFile.GetValue(ID, "DefaultMod", Mod.Split(',')[0]);
        //Console.WriteLine(Mod);
        Other = iniFile.GetValue(ID, "Other", false);
        YR = iniFile.GetValue(ID, "YR", true);
        //  Author = iniFile.GetValue(ID, "Author", string.Empty);
        BuildOffAlly = iniFile.GetValue(ID, nameof(BuildOffAlly), false);
        
            if(Directory.Exists(FilePath)&&UserINISettings.Instance.RenderPreviewImage.Value)
        foreach(var map in Directory.GetFiles(FilePath,"*.map"))
        {
                var png = Path.GetFileNameWithoutExtension(map) + ".png";
                var jpg = Path.GetFileNameWithoutExtension(map) + ".jpg";
                if (File.Exists(Path.Combine(FilePath, png)) || File.Exists(Path.Combine(FilePath, jpg))) continue;

                if(!RenderImage.需要渲染的地图列表.ContainsKey(map))
                     RenderImage.需要渲染的地图列表.TryAdd(map,[FilePath]);

        }
    }

    public static void ReLoad()
    {
        MissionPacks.Clear();
        
        var missionPackFile = Directory.GetFiles("Maps/cp/", "battle*.ini");
        foreach (var file in missionPackFile)
        {
            var ini = new IniFile(file);
            if (!ini.SectionExists("MissionPack"))
                continue;
            foreach (var key in ini.GetSectionKeys("MissionPack"))
            {
                if(ini.GetValue("MissionPack",key,string.Empty) == string.Empty)
                    continue;
                var missionPackID = ini.GetValue("MissionPack", key, string.Empty);

                var missionPack = new MissionPack(ini, missionPackID);
                missionPack.Missions.Clear();
                var m = (MissionPack)Init(ini, missionPackID, missionPack);
                MissionPacks.Add(m);
            }
        }

        Task.Run(RenderImage.RenderImages);
    }

    public bool BuildOffAlly { get; set; }

     
    public override void Create()
    { 
         
        var iniFile = new IniFile(FileName ?? $"Maps/Cp/battle{ID}.ini",ANNOTATION)
            .SetValue("MissionPack", ID, ID)
            .SetValue(ID, "Name", Name)
            .SetValue(ID, "Description", Name)
            .SetValue(ID, "LongDescription", LongDescription)
            .SetValue(ID, "Mission", FilePath)
            .SetValue(ID, "YR", YR)
            .SetValue(ID, "Mod", Mod)
            .SetValue(ID, "DefaultMod", DefaultMod)
            .SetValue(ID, "Other", Other)
            .SetValue(ID, "Author", Author)
            .SetValue(ID, "MissionPack", ID)
            .SetValue(ID, "BuildOffAlly", true)
            .SetValue(ID, "UpdateTime", UpdateTime)
            .SetValue(ID, "Difficulty",Difficulty)
            ;

        if (Sides != null)
            iniFile.SetValue(ID, "Sides", Sides);
        if (Sides != null)
            iniFile.SetValue(ID, "Difficulty", Difficulty);

        iniFile.WriteIniFile();

    }

    public override string ToString()
    {
        // 使用StringBuilder来构建字符串，提高性能
        var stringBuilder = new System.Text.StringBuilder();

        // 为了增加可读性，每个属性值占用一行
        stringBuilder.AppendLine($"ID: {ID}");
        stringBuilder.AppendLine($"Name: {Name}");
        stringBuilder.AppendLine($"LongDescription: {LongDescription}");
        stringBuilder.AppendLine($"FilePath: {FilePath}");
        stringBuilder.AppendLine($"Sides: {Sides}");
        stringBuilder.AppendLine($"Difficulty: {Difficulty}");
        stringBuilder.AppendLine($"Mod: {Mod.TrimEnd(',')}"); // 移除尾部的逗号
        stringBuilder.AppendLine($"Other: {Other}");
        stringBuilder.AppendLine($"YR: {YR}");
        stringBuilder.AppendLine($"BuildOffAlly: {BuildOffAlly}");
        stringBuilder.AppendLine($"Author: {Author}");

        // 返回构建的字符串
        return stringBuilder.ToString();
    }

    /// <summary>
    /// 获取属性名和值的映射
    /// </summary>
    /// <returns>属性名和对应值的字典</returns>
    public override Dictionary<string, string> GetProperties()
    {
        var values = new Dictionary<string, string>
        {
            { "注册名", ID },
            { "名称", Name },
            { "介绍", LongDescription },
            { "路径", FilePath },
            { "阵营", Sides },
            { "难度", Difficulty },
            { "注册于", FileName},
            { "支持的Mod", Mod },
            { "是否为尤里任务包",YR? "是": "否"},
            { "是否为玩家导入", Other.ToString() },
            { "作者", Author }

        };

        return values;
    }

    /// <summary>
    /// 介绍
    /// </summary>
    public string LongDescription { get; set; }
    /// <summary>
    /// 阵营
    /// </summary>
    public string Sides { get; set; }
    /// <summary>
    /// 难度
    /// </summary>
    public string Difficulty { get; set; }
    /// <summary>
    /// 支持的游戏
    /// </summary>
    public string Mod { get; set; } = string.Empty;
    /// <summary>
    /// 默认选择的游戏
    /// </summary>
    public string DefaultMod { get; set; } = string.Empty;
    /// <summary>
    /// 是否为尤复任务
    /// </summary>
    public bool YR { get; set; } = true;
    /// <summary>
    /// 是否为玩家自行导入的任务包
    /// </summary>
    public bool Other { get; set; }

    public string UpdateTime { get; set; }

    public List<Mission> Missions { get; set; } = [];

    public static readonly string ANNOTATION = "" +
        "# 在这里注册任务和任务包，任务和任务包词条一致，任务优先使用自身的词条。\r\n" +
        "# [任务包/任务 ID]\r\n" +
        "# Other = 是否为玩家导入的任务包(不参与评分)。默认 否 \r\n" +
        "# Side = 阵营。默认 无 \r\n" +
        "# Scenario = 使用的地图。默认 无 \r\n" +
        "# GUIName = 任务名称。默认 任务ID \r\n" +
        "# YR = 是否位尤复任务。默认 是" +
        "# IconPath = 任务图标。默认 无 \r\n" +
        "# GUIDescription = 任务简报。默认 GUIName \r\n" +
        "# Enabled = 任务是否启用。 默认 是 \r\n" +
        "# MuExtension = 是否必须使用扩展平台。 默认否 \r\n" +
        "# Extension = 可用的扩展平台。默认 Phobos,Ares \r\n" +
        "# MissionPack = 属于哪个任务包。 默认 空 \r\n" +
        "# BuildOffAlly = 友军是否可建造。默认 否 \r\n" +
        "# Mod = 该任务包使用的Mod列表。默认 所有战役可用的Mod \r\n" +
        "# DefaultMod = 默认使用的Mod。默认 该任务包使用的Mod列表的第一个Mod的ID \r\n" +
        "# FilePath = 任务所在的文件夹。\r\n" +
        "# Difficulty = 任务难度。默认 中等";
}

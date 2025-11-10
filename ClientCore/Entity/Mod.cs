using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClientCore;
using Rampastring.Tools;

namespace DTAConfig.Entity;

public class Mod : InfoBaseClass
{
    /// <summary>
    /// 把Mod信息写入配置文件
    /// </summary>
    public override void Create()
    {
        var iniFile = new IniFile(FileName ?? $"Mod&AI\\Mod&AI{ID}.ini", ANNOTATION).
             SetValue("Mod", ID, ID);
        iniFile.AddSection(ID).
            SetValue("Description", Description).
            SetValue("Name",Name).
            SetValue("FilePath", FilePath).
            SetValue("Version", Version). 
            SetValue("MuVisible", MuVisible).
            SetValue("CpVisible", CpVisible).
            SetValue("Compatible", Compatible).
            SetValue("UpdateTime", UpdateTime).
            SetValue("YR", md.Equals("md")).
            SetValue("Sides", Countries).
            SetValue("Author", Author).
            SetValue("SettingsFile", SettingsFile).
            SetValue("RandomSides", RandomSides);

        for (int i = 0; i < RandomSidesIndexs.Count;i++)
        {
            iniFile.SetValue(ID, $"RandomSidesIndex{i+1}", RandomSidesIndexs[i]);
        }
        if (Colors != null)
            iniFile.SetValue(ID, "Colors", Colors);

            //if (Countries.Length < 9) iniFile.SetValue(ID, "RandomSides", string.Empty);

            iniFile.WriteIniFile();
    }

    /// <summary>
    /// 获取属性名和值的映射
    /// </summary>
    /// <returns>属性名和对应值的字典</returns>
    public override Dictionary<string, string> GetProperties()
    {
        Dictionary<string, string> properties = new()
        {
            { "注册名", ID },
            { "名称", Name },
            { "介绍", Description },
            { "文件路径", FilePath },
            { "版本号", Version },
            { "作者", Author },
            { "遭遇战可用", MuVisible.ToString() },
            { "是否为尤复Mod", md == "md" ? "是" : "否" },
            { "国家列表", Countries },
            { "随机国家列表", RandomSides },
            { "注册于", FileName },
        };

        return properties;
    }


    public static void ReLoad()
    {

        Mods.Clear();

        var modAI = Directory.GetFiles(Path.Combine(ProgramConstants.GamePath, "Mod&AI"), "Mod&AI*.ini");

        foreach (var file in modAI)
        {
            var iniFile = new IniFile(file);
            if (!iniFile.SectionExists("Mod"))
                continue;
            foreach (var key in iniFile.GetSectionKeys("Mod"))
            {
                var modID = iniFile.GetValue("Mod", key, string.Empty);
                if (!iniFile.SectionExists($"{modID}"))
                    continue;

                var mod = new Mod();
                mod = Init(iniFile, modID, mod) as Mod;
                mod.FilePath = iniFile.GetValue(modID, "FilePath", $"Mod&AI\\Mod\\{modID}");

                if (iniFile.KeyExists(modID, "YR"))
                    mod.md = iniFile.GetValue(modID, "YR", true) ? "md" : string.Empty;

                if (File.Exists(Path.Combine(mod.FilePath, $"rules{mod.md}.iniFile")))
                {
                    mod.rules = Path.Combine(mod.FilePath, $"rules{mod.md}.iniFile");

                }
                if (File.Exists(Path.Combine(mod.FilePath, $"art{mod.md}.iniFile")))
                {
                    mod.art = Path.Combine(mod.FilePath, $"art{mod.md}.iniFile");
                }

                mod.SuperWeaponBuildings = iniFile.GetValue(modID, "SuperWeaponBuildings", string.Empty);
                

                if (iniFile.KeyExists(modID, "Sides"))
                {
                    mod.Countries = iniFile.GetValue(modID, "Sides", string.Empty);
                }
                if (iniFile.KeyExists(modID, "RandomSides"))
                {
                    mod.RandomSides = iniFile.GetValue(modID, "RandomSides", string.Empty);
                    if (mod.RandomSides != string.Empty)
                    {
                        mod.RandomSidesIndexs = new List<string>();
                        for (var i = 1; i < mod.RandomSides.Length; i++)
                        {
                            mod.RandomSidesIndexs.Add(iniFile.GetValue(modID, $"RandomSidesIndex{i}", string.Empty));
                        }
                    }
                }

              
                if (iniFile.KeyExists(modID, "Compatible"))
                {
                    mod.Compatible = iniFile.GetValue(modID, "Compatible", string.Empty);
                    if (!string.IsNullOrEmpty(mod.Compatible))
                    {
                        foreach (var item in mod.Compatible.Split(','))
                        {
                            if (CompatibleDictionary.ContainsKey(item))
                            {
                                CompatibleDictionary[item] += ',' + modID;
                            }
                            else
                                CompatibleDictionary.Add(item, modID);
                        }
                    }
                }

                mod.MuVisible = iniFile.GetValue(modID, "MuVisible", true);
                mod.CpVisible = iniFile.GetValue(modID, "CpVisible", true);
                mod.UpdateTime = iniFile.GetValue(modID, "UpdateTime", string.Empty);
                mod.SettingsFile = iniFile.GetValue(modID, "SettingsFile", "RA2MD.ini");

                mod.Colors = iniFile.GetValue<string>(modID, "Colors", null);

                HashSet<string> modSet = [];

                //if (mod.md == string.Empty)
                //    mod.ExtensionOn = true;
                mod.INI = iniFile.GetValue(modID, "INI", string.Empty);
                mod.rules = iniFile.GetValue(modID, "rules", $"{mod.FilePath}/rules{mod.md}.ini");
                mod.art = iniFile.GetValue(modID, "art", $"{mod.FilePath}/art{mod.md}.ini");
                Mods.Add(mod);
            }

        }

    }

    public static List<Mod> Mods = [];
    /// <summary>
    /// 查询ID是否存在
    /// </summary>
    /// <param name="id">需要查询的ID</param>
    /// <returns>是否存在</returns>
    public static bool QueryID(string id)
        => Mods.Find(m => m.ID == id) != null;
    /// <summary>
    /// 遭遇战是否显示
    /// </summary>
    public bool MuVisible { get; set; } = true;

    /// <summary>
    /// 可以额外选择的颜色数
    /// </summary>
    /// 
    public string Colors { get; set; }

    public string UpdateTime { get; set; } = "";

    /// <summary>
    /// 战役是否显示
    /// </summary>
    /// 
    public bool CpVisible { get; set; } = true;
    /// <summary>
    /// 是尤复mod吗 md为是，空为不是
    /// </summary>
#pragma warning disable IDE1006 // 命名样式
    // ReSharper disable once InconsistentNaming
    public string md { get; set; } = "md";

    /// <summary>
    /// rules文件所在路径
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public string rules { get; set; } = string.Empty;
    /// <summary>
    /// art文件所在路径
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public string art { get; set; } = string.Empty;
#pragma warning restore IDE1006 // 命名样式
    /// <summary>
    /// 注入的INI
    /// </summary>
    public string INI { get; set; }
    /// <summary>
    /// 国家列表
    /// </summary>
    public string Countries { get; set; } = "美国,韩国,法国,德国,英国,利比亚,伊拉克,古巴,苏联,尤里"; // 国家
    /// <summary>
    /// 随机国家
    /// </summary>
    public string RandomSides { get; set; } = "随机盟军,随机苏军";
    /// <summary>
    /// 随机国家索引
    /// </summary>
    public List<string> RandomSidesIndexs { get; set; } = new List<string> { "0,1,2,3,4", "5,6,7,8" };
    /// <summary>
    /// 哪些mod能玩的任务该Mod也能玩
    /// </summary>
    public string Compatible { get; set; } = string.Empty;

    public string SettingsFile { get; set; } = "RA2MD.ini";

    public string SuperWeaponBuildings { get; set; } = string.Empty;

    /// <summary>
    /// 键,值：值Mod能玩的键Mod都能玩
    /// </summary>
    public static Dictionary<string, string> CompatibleDictionary = [];

    /// <summary>
    /// 重写ToString方法
    /// </summary>
    /// <returns></returns>
    /// <summary>
    /// 重写ToString方法，返回Mod的字符串表示形式
    /// </summary>
    /// <returns>Mod的字符串表示形式</returns>
    public override string ToString()
    {
        return $"Mod名称：{Name}\n" +
               $"注册名：{ID}\n" +
               $"介绍：{Description}\n" +
               $"文件路径：{FilePath}\n" +
               $"遭遇战是否显示：{(MuVisible ? "是" : "否")}\n" +
               $"战役是否显示：{(CpVisible ? "是" : "否")}\n" +
               $"是否为尤里复仇者mod：{(md.Equals("md") ? "是" : "否")}\n" +
               $"rules文件所在路径：{rules}\n" +
               $"art文件所在路径：{art}\n" +
               $"注入的INI：{INI}\n" +
               $"国家列表：{Countries}\n" +
               $"随机国家：{RandomSides}\n" +
               $"随机国家索引：{string.Join(",", RandomSidesIndexs)}\n" +
               $"兼容的任务mod：{Compatible}\n" +
               $"注册于哪个文件：{FileName}\n" +
               $"可以额外选择的颜色数：{Colors}";
    }

    /// <summary>
    /// 递归获取该Mod能玩的任务哪些mod也能玩
    /// </summary>
    /// <param name="name">该ModID</param>
    /// <returns>可玩的mod列表，逗号分割</returns>
    public static string GetCompatibleMods(string name) =>
        CompatibleDictionary.TryGetValue(name, out var value)
            ? name + "," + string.Join(",", value.Split(',').Select(GetCompatibleMods))
            : name;

    //等效于
    //public static string GetCompatibleMod(string name)
    //{

    //    if (!CompatibleDictionary.TryGetValue(name, out var value))
    //        return string.Empty;

    //    foreach (var id in value.Split(','))
    //    {
    //            name += ',' + GetCompatibleMod(id);
    //    }

    //    return name;
    //}
    public static readonly string ANNOTATION = "" +
        "# 在这里注册MOD。\r\n" +
        "# [MOD ID]\r\n" +
        "# MuVisible = 遭遇战是否显示。默认 true \r\n" +
        "# CpVisible = 战役是否显示。默认 true \r\n" +
        "# ExtensionOn = 是否必须使用扩展。默认 false \r\n" +
        "# Extension = 可使用的扩展列表。默认 Ares,Phobos \r\n" +
        "# Compatible = 任务兼容的mod，逗号分隔。比如如果这个mod兼容尤复的任务，那就写Compatible = YR。默认 空 \r\n" +
        "# YR = 是尤复mod吗。默认 true \r\n" +
        "# rules = rules文件所在路径。默认 无 \r\n" +
        "# art = art文件所在路径。默认 无 \r\n" +
        "# INI = 注入的INI。默认 无 \r\n" +
        "# Countries = 国家列表。默认 美国,韩国,法国,德国,英国,利比亚,伊拉克,古巴,苏联,尤里 \r\n" +
        "# RandomSides = 随机国家。默认 随机盟军,随机苏军 \r\n" +
        "# RandomSidesIndex* = 随机国家索引。默认 两组 0,1,2,3,4 5,6,7,8"
        ;

}

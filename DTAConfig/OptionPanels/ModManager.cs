using ClientCore;
using ClientCore.Entity;
using ClientGUI;
using DTAConfig.Entity;
using Localization;
using Localization.Tools;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToolTip = ClientGUI.ToolTip;

namespace DTAConfig.OptionPanels;

public class ModManager : XNAWindow
{
    private static ModManager _instance;

    //public XNADropDown AI;

    public XNADropDown DDModAI;
    private XNAListBox ListBoxModAi;
    private XNAMultiColumnListBox _mcListBoxInfo;
    private ToolTip _tooltip;
    private XNAClientButton _btnReturn;
    private XNAClientButton BtnNew;
    private XNAClientButton BtnDownload;
    private XNAClientButton BtnDel;

    private XNAContextMenu _modMenu;
    public Action 触发刷新;
    public OptionsWindow optionsWindow;

    public override void Initialize()
    {
        base.Initialize();

        ClientRectangle = new Rectangle(0, 0, 750, 384);
        CenterOnParent();

        var lblTittle = new XNALabel(WindowManager)
        {
            Text = "Mod Manager (Experimental)".L10N("UI:DTAConfig:ModManager"),
            FontIndex = 0,
            ClientRectangle = new Rectangle(Width / 2 - 40, 5, 0, 0),
            TextColor = Color.White
        };

        AddChild(lblTittle);

        DDModAI = new XNADropDown(WindowManager)
        {
            ClientRectangle = new Rectangle(25, 30, 200, 40)
        };
        DDModAI.AddItem(["Mod (Experimental support)".L10N("UI:DTAConfig:Mod"), "Mission Pack".L10N("UI:DTAConfig:MissionPack")]);
        AddChild(DDModAI);

        _modMenu = new XNAContextMenu(WindowManager);
        _modMenu.Name = nameof(_modMenu);
        _modMenu.Width = 100;

        _modMenu.AddItem(new XNAContextMenuItem
        {
            Text = "Open the file location".L10N("UI:DTAConfig:OpenFileLocation"),
            SelectAction = () => {
                if (ListBoxModAi.SelectedIndex != -1)
                {
                    var p = Path.Combine(ProgramConstants.GamePath, ((InfoBaseClass)ListBoxModAi.SelectedItem.Tag).FilePath).Replace("/", "\\");
                    Process.Start("explorer.exe", p);
                }
            }
        });

        _modMenu.AddItem(new XNAContextMenuItem
        {
            Text = "Add".L10N("UI:DTAConfig:Add"),
            SelectAction = () => BtnNew.OnLeftClick()
        });
        //_menu.AddItem(new XNAContextMenuItem
        //{
        //    Text = "Revise".L10N("UI:DTAConfig:Revise"),
        //    SelectAction = UpdateBase
        //});
        _modMenu.AddItem(new XNAContextMenuItem
        {
            Text = "Delete".L10N("UI:DTAConfig:Delete"),
            SelectAction = () => BtnDel.OnLeftClick()
        });
        _modMenu.AddItem(new XNAContextMenuItem
        {
            Text = "Package the file as a Mix".L10N("UI:DTAConfig:PackageFileMix"),
            SelectAction = 将文件打包为MIX
        });
        _modMenu.AddItem(new XNAContextMenuItem
        {
            Text = "Unzip Mix".L10N("UI:DTAConfig:UnzipMix"),
            SelectAction = 解压MIX
        });
        _modMenu.AddItem(new XNAContextMenuItem
        {
            Text = "Edit the CSF".L10N("UI:DTAConfig:EditCSF"),
            SelectAction = EditCsf
        });

        AddChild(_modMenu);

        ListBoxModAi = new XNAListBox(WindowManager)
        {
            ClientRectangle = new Rectangle(DDModAI.X, DDModAI.Y + 40, 260, 260),
            LineHeight = 25,
            SelectedIndex = -1,
            FontIndex = 2
        };
        ListBoxModAi.RightClick += (_, _) =>
        {
            ListBoxModAi.SelectedIndex = ListBoxModAi.HoveredIndex;

            if (ListBoxModAi.SelectedIndex == -1)
                return;

            var filePath = ((InfoBaseClass)ListBoxModAi.SelectedItem.Tag).FilePath;

            // 没有csf
            if (Directory.Exists(filePath) && Directory.GetFiles(filePath, "*.csf").Length == 0)
            {
                _modMenu.Items[2].Visible = false;

            }
            else
            {
                _modMenu.Items[2].Visible = true;
            }

            _modMenu.Open(GetCursorPoint());
        };
        AddChild(ListBoxModAi);

        _mcListBoxInfo = new XNAMultiColumnListBox(WindowManager)
        {
            ClientRectangle = new Rectangle(ListBoxModAi.X + ListBoxModAi.Width + 20, ListBoxModAi.Y, 420, ListBoxModAi.Height),
            LineHeight = 25,
            FontIndex = 2
        }.AddColumn("Attribute".L10N("UI:DTAConfig:Attribute"), 160).AddColumn("Info".L10N("UI:DTAConfig:Info"), 260);

        _mcListBoxInfo.SelectedIndexChanged += McListBoxInfoSelectedIndexChanged;

        AddChild(_mcListBoxInfo);

        DDModAI.SelectedIndexChanged += DDModAI_SelectedIndexChanged;

       

        _tooltip = new ToolTip(WindowManager, _mcListBoxInfo)
        {
            Text = "Select to view details".L10N("UI:DTAConfig:SelectViewDetails")
        };

        _btnReturn = new XNAClientButton(WindowManager)
        {
            Text = "Yes".L10N("UI:Main:ButtonYes"),
            ClientRectangle = new Rectangle(340, 350, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        _btnReturn.LeftClick += BtnReturn_LeftClick;
        AddChild(_btnReturn);

        BtnDownload = new XNAClientButton(WindowManager)
        {
            Text = "Download more".L10N("UI:Main:ButtonDownloadMore"),
            ClientRectangle = new Rectangle(_mcListBoxInfo.X, DDModAI.Y, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        BtnDownload.LeftClick += BtnDownload_LeftClick;
        AddChild(BtnDownload);

        BtnNew = new XNAClientButton(WindowManager)
        {
            Visible = false,
            ClientRectangle = new Rectangle(BtnDownload.X + 110, DDModAI.Y, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        BtnNew.LeftClick += BtnNew_LeftClick;
        AddChild(BtnNew);

        BtnDel = new XNAClientButton(WindowManager)
        {
            Visible = false,
            ClientRectangle = new Rectangle(BtnNew.X + 110, DDModAI.Y, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        BtnDel.LeftClick += BtnDel_LeftClick;
        AddChild(BtnDel);

        var btnReload = new XNAClientButton(WindowManager)
        {
            Text = "Refresh".L10N("UI:Main:ButtonRefresh"),
            ClientRectangle = new Rectangle(BtnDel.X + 110, DDModAI.Y, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        btnReload.LeftClick += (_, _) => ReLoad();
        AddChild(btnReload);

        Enabled = false;

        // EnabledChanged += ModManager_EnabledChanged;
        // UserINISettings.Instance.重新加载地图和任务包 += 触发刷新;
        // DDModAI.SelectedIndex = 0;
        // ReLoad();

        DDModAI_SelectedIndexChanged(DDModAI, null);
        LoadModInfo();
    }

    private void BtnDownload_LeftClick(object sender, EventArgs e)
    {
        打开创意工坊(3 - DDModAI.SelectedIndex);
    }

    public void 打开创意工坊(int selectIndex)
    {

        string type;
        if (selectIndex == 3)
            type = "mod";
        else
            type = "missionPack";

        FunExtensions.OpenUrl($"https://creator.yra2.com/workshop/{type}/list?token={UserINISettings.Instance.Token}&port={UserINISettings.Instance.startPort}");
       
    }


    private void 解压MIX()
    {
        using OpenFileDialog fileDialog = new OpenFileDialog();
        fileDialog.Filter = "MIX 文件 (*.mix)|*.mix";
        fileDialog.Title = "选择MIX文件";
        fileDialog.Multiselect = true; // 允许选择多个文件

        if (fileDialog.ShowDialog() == DialogResult.OK)
        {
            foreach (var mix in fileDialog.FileNames)
            {
                var Dname = Path.GetDirectoryName(mix);
                var Fname = Path.GetFileNameWithoutExtension(mix);
                var path = Path.Combine(Dname, Fname);

                Mix.UnPackMix(path, mix);
                if (Directory.Exists(path))
                {
                    Process.Start("explorer.exe", path);
                }
            }
        }
    }

    private void 将文件打包为MIX()
    {
        using OpenFileDialog fileDialog = new OpenFileDialog();
        fileDialog.Filter = "所有文件 (*.*)|*.*";
        fileDialog.Title = "选择文件";
        fileDialog.Multiselect = true; // 允许选择多个文件

        if (fileDialog.ShowDialog() == DialogResult.OK)
        {
            var Dname = Path.GetDirectoryName(fileDialog.FileNames[0]);
            var Fname = Path.GetFileName(Dname) + ".mix";
            Mix.PackFilesToMix(fileDialog.FileNames.ToList(), Dname, Fname);
            var path = Path.Combine(Dname, Fname);
            if (File.Exists(path))
            {
                XNAMessageBox.Show(WindowManager, "Tips".L10N("UI:Main:Tips"), $"打包成功: {path}");
            }
            else
                XNAMessageBox.Show(WindowManager, "Tips".L10N("UI:Main:Tips"), $"Packaging failed".L10N("UI:DTAConfig:PackagingFailed"));
        }
    }

    private void EditCsf()
    {
        var csfPath = Path.Combine(((InfoBaseClass)ListBoxModAi.SelectedItem.Tag).FilePath, "ra2md.csf");
        if (!File.Exists(csfPath))
            csfPath = Path.Combine(ProgramConstants.GamePath, "ra2md.csf");
        var csf = new CSF(csfPath);
        var editCSFWindows = new EditCSFWindows(WindowManager, _tooltip, csf);
        editCSFWindows.Show();
    }

    private void DDModAI_SelectedIndexChanged(object sender, EventArgs e)
    {
        ListBoxModAi.SelectedIndexChanged -= ListBoxModAISelectedIndexChanged;
        ListBoxModAi.SelectedIndex = -1;
        ListBoxModAi.Items.Clear();

        switch (DDModAI.SelectedIndex)
        {
            //Mod
            case 0:
                foreach (var mod in Mod.Mods)
                {
                    ListBoxModAi.AddItem(new XNAListBoxItem { Text = mod.Name, Tag = mod });
                }
                break;
            case 1:
                foreach (var missionPack in MissionPack.MissionPacks)
                {
                    ListBoxModAi.AddItem(new XNAListBoxItem { Text = missionPack.Name, Tag = missionPack });
                }
                break;
        }
        ListBoxModAi.SelectedIndexChanged += ListBoxModAISelectedIndexChanged;
        //ListBoxModAISelectedIndexChanged(null, null);
        ListBoxModAi.SelectedIndex = 0;
    }



    /// <summary>
    /// 复制Mod文件
    /// </summary>
    /// <param name="modPath">mod原路径</param>
    /// <param name="mod">mod信息</param>
    private static void 整合Mod文件(string startPath,string modPath, Mod mod, bool deepImport = false)
    {
        #region 导入Mod文件

        var tagerPath = Path.Combine(startPath, mod.FilePath);

        //提取Mod文件
        if (!Directory.Exists(tagerPath))
            Directory.CreateDirectory(tagerPath);

        try
        {
            CopyFiles(modPath, tagerPath, deepImport);
        }
        catch (Exception ex)
        {
            Logger.Log(ex.ToString());
            return;
        }

        #endregion
    }

    private static void CopyFiles(string sourceDir, string targetDir, bool needRecursion = false)
    {
        // 复制当前目录下的文件
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var extension = Path.GetExtension(file);

            // 排除特定文件
            //if (!needRecursion && (extension == ".png" || extension == ".jpg" || extension == ".pdb"))
            //    continue;

            // 检查文件哈希值并决定是否复制
            if (ProgramConstants.PureHashes.ContainsKey(fileName) &&
                ProgramConstants.PureHashes[fileName] == Utilities.CalculateSHA1ForFile(file))
                continue;

            // 目标路径
            var targetFilePath = Path.Combine(targetDir, fileName);
            File.Copy(file, targetFilePath, overwrite: true);
        }

        if (needRecursion)
            // 递归处理子目录
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(dir);
                var targetSubDir = Path.Combine(targetDir, dirName);

                // 如果目标子目录不存在，创建它
                if (!Directory.Exists(targetSubDir))
                {
                    Directory.CreateDirectory(targetSubDir);
                }

                // 递归复制子目录中的文件
                CopyFiles(dir, targetSubDir);
            }
    }

    private static void 整合任务包文件(string startPath, string MissionPackPath, MissionPack missionPack, bool deepImport = false)
    {
        var tagerPath = Path.Combine(startPath, missionPack.FilePath);
        if (!Directory.Exists(tagerPath))
            Directory.CreateDirectory(tagerPath);
        CopyFiles(MissionPackPath, tagerPath, deepImport);
    }

    void 查找并解压压缩包(string zip)
    {
        var zips = Directory.GetFiles(zip, "*.*", SearchOption.AllDirectories)
                         .Where(file => new[] { ".zip", ".rar", ".7z" }.Contains(Path.GetExtension(file).ToLower()))
                         .ToArray();
        if (zips.Length == 0) return;

        foreach (var item in zips)
        {
            SevenZip.ExtractWith7Zip(item, $"./tmp/{Path.GetFileNameWithoutExtension(zip)}/{Path.GetFileNameWithoutExtension(item)}", needDel: true);
            查找并解压压缩包(Path.GetDirectoryName(item));
        }
    }

    /// <summary>
    /// 导入任务包.
    /// </summary>
    public string 导入任务包(bool copyFile, bool deepImport, string filePath, string startPath = null, MissionPack m = null)
    {
        List<string> mapFiles = [];

        var id = string.Empty;

        foreach (var path in filePath.Split(','))
        {
            if (Directory.Exists(path))
            {
                List<string> list = [path, .. Directory.GetDirectories(path, "*", SearchOption.AllDirectories)];
                foreach (var item in list)
                {
                    if (Directory.GetFiles(item).Length == 0 && Directory.GetDirectories(item).Length > 0) continue;

                    if (判断是否为任务包(item))
                    {
                        var r = 导入具体任务包(copyFile, deepImport, item, startPath, m);
                        if (r != null)
                        {
                            id = r.ID;
                            mapFiles.AddRange(Directory.GetFiles(r.FilePath, "*.map"));
                        }
                    }
                }
            }
        }
            
        if (id == string.Empty)
        {
            XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), "Please select Yuri's Revenge Mission Pack or the ission pack file based on the original".L10N("UI:DTAConfig:SelectMissionPackFile"));
            return "No mission package files found".L10N("UI:DTAConfig:MissionPackageFileNotFound");
        }

        刷新并渲染(mapFiles);

        return id;
    }

    public void 刷新并渲染(List<string> mapFiles, string modPath = "")
    {
        ReLoad();

       // 渲染预览图
       // if (UserINISettings.Instance.RenderPreviewImage.Value)
        Task.Run(() =>
        {
            _ = RenderImage.RenderPreviewImageAsync(mapFiles.ToArray(), [modPath]);
            return Task.CompletedTask;
        });
        触发刷新?.Invoke();
    }

    public static MissionPack 导入具体任务包(bool copyFile, bool deepImport, string missionPath, string startPath = null, MissionPack mp = null)
    {
        if (missionPath == null) return null;

        startPath ??= ProgramConstants.GamePath;
        bool isYR = 判断是否为尤复(missionPath);

        if (判断是否为Mod(missionPath, isYR) && !isYR) return null;
        var id = Path.GetFileName(missionPath);
        if (missionPath == ProgramConstants.GamePath)
        {
            id = DateTime.Now.ToString("yyyyMMddHHmmss");
        }
        var missionPack = new MissionPack
        {
            ID = mp?.ID ?? id,
            FilePath = missionPath,
            FileName = Path.Combine(startPath, $"Maps/Cp/battle{mp?.ID ?? id}.ini"),
            Name = mp?.Name ?? Path.GetFileName(missionPath),
            YR = isYR,
            Other = mp?.Other ?? true,
            LongDescription = mp?.LongDescription ?? Path.GetFileName(missionPath),
            Mod = isYR ? "YR" : "RA2",
            DefaultMod = isYR ? "YR+" : "RA2+",
            UpdateTime = mp?.UpdateTime ?? ""
        };
        missionPack.DefaultMod = missionPack.Mod;
        var m = new Mod()
        {
            ID = missionPack.ID,
            Name = missionPack.Name,
            MuVisible = false
        };

        var mod = 导入具体Mod(missionPath, copyFile, deepImport, isYR, startPath, m);

        if (mod != null) //说明检测到Mod
        {
            missionPack.Mod = mod.ID;
            missionPack.DefaultMod = mod.ID;
            missionPack.FilePath = mod.FilePath;
        }
        else
        {
            missionPack.FilePath = $"Maps\\CP\\{missionPack.ID}";
            整合任务包文件(startPath, missionPath, missionPack);
        }

        missionPack.Create();
        写入任务INI(missionPack, startPath);

        return missionPack;
    }

    private static readonly Dictionary<string, string> 默认战役名称 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Name:TRN01", "新兵训练营 - 第一天" },
        { "Name:TRN02", "新兵训练营 - 第二天" },
        { "Name:Sov01", "军事行动：红色黎明" },
        { "Name:Sov02", "军事行动：危机四伏" },
        { "Name:Sov03", "军事行动：大苹果" },
        { "Name:Sov04", "军事行动：家乡前线" },
        { "Name:Sov05", "军事行动：灯火之城" },
        { "Name:Sov06", "军事行动：划分" },
        { "Name:Sov07", "军事行动：超时空防御战" },
        { "Name:Sov08", "军事行动：首都之辱" },
        { "Name:Sov09", "军事行动：狐狸与猎犬" },
        { "Name:Sov10", "军事行动：残兵败将" },
        { "Name:Sov11", "军事行动：红色革命" },
        { "Name:Sov12", "军事行动：北极风暴" },
        { "Name:ALL01", "军事行动：孤独守卫" },
        { "Name:ALL02", "军事行动：危机黎明" },
        { "Name:ALL03", "军事行动：为长官欢呼" },
        { "Name:ALL04", "军事行动：最后机会" },
        { "Name:ALL05", "军事行动：暗夜" },
        { "Name:ALL06", "军事行动：自由" },
        { "Name:ALL07", "军事行动：深海" },
        { "Name:ALL08", "军事行动：自由门户" },
        { "Name:ALL09", "军事行动：太阳神殿" },
        { "Name:ALL10", "军事行动：海市蜃楼" },
        { "Name:ALL11", "军事行动：核爆辐射尘" },
        { "Name:ALL12", "军事行动：超时空风暴" },
        { "Name:Sov01md", "军事行动：时空转移" },
        { "Name:Sov02md", "军事行动：似曾相识" },
        { "Name:Sov03md", "军事行动：洗脑行动" },
        { "Name:Sov04md", "军事行动：北非谍影" },
        { "Name:Sov05md", "军事行动：脱离地心引力" },
        { "Name:Sov06md", "军事行动：飞向月球" },
        { "Name:Sov07md", "军事行动：首脑游戏" },
        { "Name:ALL01md", "军事行动：光阴似箭" },
        { "Name:ALL02md", "军事行动：好莱坞，梦一场" },
        { "Name:ALL03md", "军事行动：集中攻击" },
        { "Name:ALL04md", "军事行动：古墓奇击" },
        { "Name:ALL05md", "军事行动：纽澳复制战" },
        { "Name:ALL06md", "军事行动：万圣节" },
        { "Name:ALL07md", "军事行动：脑死" },
    };

    private static void 写入任务INI(MissionPack missionPack, string startPath)
    {
        var tagerPath = Path.Combine(startPath, missionPack.FilePath);
        var maps = Directory.GetFiles(tagerPath, "*.map").ToList();
        var md = missionPack.YR ? "md" : string.Empty;

        var battleINI = new IniFile(Path.Combine(startPath, $"Maps\\CP\\battle{missionPack.ID}.ini"), MissionPack.ANNOTATION);
        if (!battleINI.SectionExists("Battles"))
            battleINI.AddSection("Battles");

        //先确定可用的ini
        var mapSelINIPath = "Resources//mapselmd.ini";
        if (File.Exists(Path.Combine(tagerPath, $"mapsel{md}.ini")))
            mapSelINIPath = Path.Combine(tagerPath, $"mapsel{md}.ini");

        var missionINIPath = "Resources//missionmd.ini";
        if (File.Exists(Path.Combine(tagerPath, $"mission{md}.ini")))
            missionINIPath = Path.Combine(tagerPath, $"mission{md}.ini");

        var csf = CSF.获取目录下的CSF字典(tagerPath);
        var missionINI = new IniFile(missionINIPath);
        var mapSelINI = new IniFile(mapSelINIPath);

        if (maps.Count == 0) //如果没有地图
        {
            添加默认地图("GDI");
            添加默认地图("Nod");
        }

        void 添加默认地图(string typeName)
        {
            var GDI = mapSelINI.GetSection(typeName);
            if (GDI == null) return;

            var keys = mapSelINI.GetSectionKeys(typeName);
            foreach (var key in keys)
            {
                var sectionName = mapSelINI.GetValue(typeName, key, string.Empty);
                if (sectionName == string.Empty) continue;

                var map = mapSelINI.GetValue(sectionName, "Scenario", string.Empty);
                if (map == string.Empty) continue;

                if (mapSelINIPath == "Resources//mapselmd.ini" && missionINIPath == "Resources//missionmd.ini")
                    if ((map.ToLower().EndsWith("md.map") && !missionPack.YR) || !map.ToLower().EndsWith("md.map") && missionPack.YR) continue;

                maps.Add(map);
            }
        }

        var count = 1;
        battleINI.SetValue("Battles", missionPack.ID, missionPack.ID);

        foreach (var map in maps)
        {
            var mapName = Path.GetFileName(map).ToUpper();
            var sectionName = missionPack.ID + $"第{count}关";

            if (!battleINI.SectionExists(sectionName))
                battleINI.AddSection(sectionName);

            var 阵营 = "";
            if (mapName.ToLower().Contains("all"))
                阵营 = "Allied";
            else if (mapName.ToLower().Contains("sov"))
                阵营 = "Soviet";

            var csfName = missionINI.GetValue(mapName, "UIName", string.Empty);
            var 任务名称 = csf?.GetValueOrDefault(csfName)?.ConvertValuesToSimplified() ?? $"第{count}关";
            var 任务地点 = csf?.GetValueOrDefault(missionINI.GetValue(mapName, "LSLoadMessage", string.Empty))?.ConvertValuesToSimplified() ?? "";
            var 任务简报 = csf?.GetValueOrDefault(missionINI.GetValue(mapName, "Briefing", string.Empty))?.ConvertValuesToSimplified() ?? "";
            var 任务目标 = csf?.GetValueOrDefault(missionINI.GetValue(mapName, "LSLoadBriefing", string.Empty))?.ConvertValuesToSimplified() ?? "";

            if (默认战役名称.ContainsKey(csfName) && (默认战役名称[csfName] == 任务名称 || $"第{count}关" == 任务名称))
            {
                if (任务地点 != string.Empty)
                    任务名称 = 任务地点.Split('-')[0].TrimEnd();
            }
            if (任务简报.Trim().Contains(任务目标.Trim()))
                任务简报 = string.Empty;

            var LongDescription = 任务地点 + "@@" + 任务简报 + "@" + 任务目标;
            battleINI.SetValue("Battles", sectionName, sectionName)
                     .SetValue(sectionName, "Scenario", mapName)
                     .SetValue(sectionName, "Description", 任务名称)
                     .SetValue(sectionName, "LongDescription", LongDescription.Replace("\n", "@"))
                     .SetValue(sectionName, "MissionPack", missionPack.ID)
                     .SetValue(sectionName, "SideName", 阵营);
            count++;
        }
        battleINI.WriteIniFile();
    }

    private static readonly List<string> IniFileWhitelist = new List<string>
    {
        "battle", "mapsel", "ai", "ra2", "spawn", "mpmaps", "spawnmap", "mpbattle", "keyboard", "ddraw", "_desktop", "desktop"
    };

    public static bool 判断是否为Mod(string path, bool isYR)
    {
        var md = isYR ? "md" : string.Empty;
        var shps = Directory.GetFiles(path, "*.shp")
           .Where(file => !Path.GetFileName(file).ToLower().StartsWith("ls") && !Path.GetFileName(file).ToLower().StartsWith("gls"))
           .ToArray();
        var vxls = Directory.GetFiles(path, "*.vxl");
        var pals = Directory.GetFiles(path, "*.pal")
            .Where(file => !Path.GetFileName(file).ToLower().StartsWith("ls") && !Path.GetFileName(file).ToLower().StartsWith("gls"))
            .ToArray();
        var mixs = Directory.GetFiles(path, $"expand*.mix")
            .ToArray();
        var inis = Directory.GetFiles(path, $"*.ini")
            .Where(file =>
            {
                var fileName = Path.GetFileName(file).ToLower();
                return !IniFileWhitelist.Any(whitelisted => fileName.StartsWith(whitelisted) || fileName.EndsWith($"{whitelisted}{md}.ini"));
            })
            .ToArray();
        return shps.Length + vxls.Length + pals.Length + mixs.Length + inis.Length != 0;
    }

    public static bool 判断是否为任务包(string path)
    {
        if (!Directory.Exists(path))
            return false;

        var maps = Directory.GetFiles(path, "*.map").Count(map =>
        {
            var fileName = Path.GetFileNameWithoutExtension(map).ToLower();
            // 如果以 all 或 sov 开头，直接当任务图
            if (fileName.StartsWith("all") || fileName.StartsWith("sov"))
                return true;

            // 否则按原逻辑判断
            return !FunExtensions.是否为多人图(map);
        });
        var mixs = Directory.GetFiles(path, "*.mix").Length;

        return maps + mixs != 0;
    }

    private static bool 判断是否为尤复(string path)
    {
        string[] YRFiles = ["gamemd.exe", "RA2MD.CSF", "expandmd01.mix", "rulesmd.ini", "artmd.ini", "glsmd.shp"];

        return Directory.Exists(path) && YRFiles.Any(file => File.Exists(Path.Combine(path, file))) || Directory.GetFiles(path, "expandmd*.mix").Length != 0 || Directory.GetFiles(path, "*md.map").Length != 0;
    }

    public string 导入Mod(bool copyFile, bool deepImport, string filePath, Mod m = null)
    {
        var id = string.Empty;
        foreach (var path in filePath.Split(','))
        {
            List<string> list = [path, .. Directory.GetDirectories(path, "*", SearchOption.AllDirectories)];
            foreach (var item in list)
            {
                if (!Directory.Exists(item))
                    continue;

                if (!判断是否为尤复(item))
                    continue;

                if (判断是否为Mod(item, true))
                {
                    var r = 导入具体Mod(item, copyFile, deepImport, true, m: m);
                    if (r != null)
                    {
                        id = r.ID;
                    }
                }
            }
        }

        if (id == string.Empty)
        {
            XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), "Please select Yuri's Revenge mod, vanilla mods are not supported at this time.".L10N("UI:DTAConfig:SelectModFile"));
            return "No suitable mod file found".L10N("UI:DTAConfig:ModFileNotFound");
        }

        ReLoad();
        触发刷新?.Invoke();

        return id;
    }

    public static Mod 导入具体Mod(string path, bool copyFile, bool deepImport, bool isYR, string startPath = null, Mod m = null)
    {
        startPath ??= ProgramConstants.GamePath;
        var md = isYR ? "md" : null;

        if (!判断是否为Mod(path, isYR))
            return null;

        var id = m?.ID ?? Path.GetFileName(path);
        if (path == ProgramConstants.GamePath)
        {
            id = DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        var Name = m?.Name ?? id;
        var Countries = string.Empty;
        var RandomSides = string.Empty;
        List<string> RandomSidesIndexs = [];
        var Colors = string.Empty;
        var SettingsFile = "RA2MD.ini";
        var BattleFile = "";

        if (Directory.Exists("Resources") && path != ProgramConstants.GamePath)
        {
            #region 从 GameOptions.ini 提取 国家 和 颜色信息

            var GameOptionsPath = Path.Combine(path, "Resources/GameOptions.ini");

            if (File.Exists(GameOptionsPath))
            {
                var ini = new IniFile(GameOptionsPath);
                if (ini.SectionExists("General"))
                    Countries = ini.GetValue("General", "Sides", string.Empty);

                if (ini.SectionExists("RandomSelectors"))
                {
                    foreach (var key in ini.GetSectionKeys("RandomSelectors"))
                    {
                        var value = ini.GetValue("RandomSelectors", key, string.Empty);
                        if (value == string.Empty) continue;
                        RandomSides += key + ',';
                        RandomSidesIndexs.Add(value);
                    }
                    RandomSides = RandomSides.TrimEnd(',');
                }

                if (ini.SectionExists("MPColors"))
                {
                    Colors = string.Join("|", ini.GetSectionValues("MPColors"));
                }
            }

            #endregion

            #region 从 ClientDefinitions.ini 提取 后缀和战役 信息

            var ClientDefinitionsPath = Path.Combine(path, "Resources/ClientDefinitions.ini");

            if (File.Exists(ClientDefinitionsPath))
            {
                var ini = new IniFile(ClientDefinitionsPath);
                if (ini.SectionExists("Settings"))
                {
                    SettingsFile = ini.GetValue("Settings", "SettingsFile", "RA2MD.ini");
                    BattleFile = ini.GetValue("Settings", "BattleFSFileName", "BattleFS.ini");
                }
            }
            #endregion

            #region 从 GameCollectionConfig.ini 提取 Mod名称 信息

            var GameCollectionConfigPath = Path.Combine(path, "Resources/GameCollectionConfig.ini");

            if (File.Exists(GameCollectionConfigPath))
            {
                var ini = new IniFile(GameCollectionConfigPath);
                ini.GetSections().ToList().ForEach(section =>
                {
                    if (ini.KeyExists(section, "UIName"))
                        Name = ini.GetValue(section, "UIName", Name);
                });
            }

            #endregion

        }

        #region 或从 rulesmd.ini 提取 国家 信息
        if (Countries == string.Empty)
        {
            var RulesPath = Path.Combine(path, "rulesmd.ini");
            if (File.Exists(RulesPath))
            {
                var ini = new IniFile(RulesPath);
                if (ini.SectionExists("Countries"))
                {
                    var d = CSF.获取目录下的CSF字典(path);
                    if (Directory.GetFiles(path,"*.csf").Length == 0)
                        d = CSF.获取目录下的CSF字典("Mod&AI\\Mod\\YR");



                    foreach (var country in ini.GetSectionValues("Countries").SkipLast(4))
                    {
                        var UIName = ini.GetValue(country, "UIName", $"Name:{country}");
                        if (d.ContainsKey(UIName))
                            Countries += d[UIName] + ',';
                        else
                            Countries += country + ',';
                    }
                    Countries = Countries.TrimEnd(',').ConvertValuesToSimplified();
                }
            }
        }
        #endregion

        var mod = m;

        mod ??= new Mod
        {
            ID = id,
            Name = Name,
            FileName = Path.Combine(startPath, $"Mod&AI\\Mod&AI{id}.ini"),
            md = md,
            MuVisible = mod?.MuVisible ?? true,
            SettingsFile = SettingsFile
        };

        if (copyFile)
            mod.FilePath = $"Mod&AI\\{id}";
        else
            mod.FilePath = path;

        if (Countries != string.Empty)
            mod.Countries = Countries;

        if (RandomSides != string.Empty && RandomSidesIndexs.Count != 0)
        {
            mod.RandomSides = RandomSides;
            mod.RandomSidesIndexs = RandomSidesIndexs;
        }

        if (Colors != string.Empty)
            mod.Colors = Colors;

        if (copyFile)
            整合Mod文件(startPath, path, mod, deepImport);

        var BattleFilePath = Path.Combine(path, "INI", BattleFile);
        var newBattleFilePath = Path.Combine("Maps\\CP\\", $"Battle{mod.ID}.ini");

        if (BattleFile != string.Empty && File.Exists(BattleFilePath))
        {
            File.Copy(BattleFilePath, newBattleFilePath, true);
            var ini = new IniFile(newBattleFilePath);
            ini.AddSection("MissionPack")
                .SetValue("Mod", mod.ID)
                .SetValue("Name", mod.Name)
                .SetValue("Other", true)
                .SetValue("Mission", mod.FilePath)
                .SetValue("Description", mod.Name)
                .SetValue("LongDescription", mod.Name)
                .SetValue("BuildOffAlly", mod.Name);
            ini.GetSections().ToList().ForEach(section =>
            {
                ini.SetValue(0, section, "MissionPack", mod.ID);
            });
            ini.WriteIniFile();
        }

        mod.Create(); //写入INI文件
        return mod;
    }

    private void UpdateBase()
    {
        if (DDModAI.SelectedIndex == 0)
            UpdateMod(ListBoxModAi.SelectedItem.Tag as Mod);
        if (DDModAI.SelectedIndex == 1)
            UpdateMissionPack(ListBoxModAi.SelectedItem.Tag as MissionPack);
    }

    /// <summary>
    /// 修改Mod
    /// </summary>
    /// <param name="mod"></param>
    private void UpdateMod(Mod mod)
    {
        var infoWindows = new ModInfoWindows(WindowManager, mod, "Modify Mod".L10N("UI:DTAConfig:ModifyMod"));

        var dp = DarkeningPanel.AddAndInitializeWithControl(WindowManager, infoWindows);

        infoWindows.EnabledChanged += (_, _) =>
        {
            mod = infoWindows.GetModInfo();
            var csf = infoWindows.GetCsf();
            infoWindows.Dispose();
            if (mod == null)
                return;

            mod.Create(); //写入INI文件

            if (csf)
            {
                foreach (var csfFile in Directory.GetFiles(mod.FilePath, "*.csf"))
                {
                    var d = new CSF(csfFile).GetCsfDictionary();

                    if (d == null)
                    {
                        File.Copy(csfFile, Path.Combine(mod.FilePath, Path.GetFileName(csfFile)), true);
                    }
                    else
                    {
                        d.ConvertValuesToSimplified();
                        CSF.WriteCSF(d, Path.Combine(mod.FilePath, Path.GetFileName(csfFile).Equals("ra2.csf", StringComparison.OrdinalIgnoreCase) ? "ra2md.csf" : Path.GetFileName(csfFile)));
                    }
                }
            }

            ReLoad(); //重新载入
            触发刷新?.Invoke();
        };
    }

    /// <summary>
    /// 修改任务包
    /// </summary>
    /// <param name="pack"></param>
    private void UpdateMissionPack(MissionPack pack)
    {
        var missionMix = false;
        var csfExist = false;

        if (!Directory.Exists(pack.FilePath))
        {
            XNAMessageBox.Show(WindowManager, "Info".L10N("UI:Main:Info"), $"任务包路径 {pack.FilePath} 不存在!");
            return;
        }

        if (Directory.GetFiles(pack.FilePath, "*.map").Length == 0)
        {
            missionMix = true;
        }

        if (Directory.GetFiles(pack.FilePath, "*.csf").Length != 0)
        {
            csfExist = true;
        }

        var infoWindows = new MissionPackInfoWindows(WindowManager, pack, "Modify Mission Package".L10N("UI:DTAConfig:ModifyMissionPackage"), missionMix, csfExist);
        var dp = DarkeningPanel.AddAndInitializeWithControl(WindowManager, infoWindows);

        //AddChild(infoWindows);

        infoWindows.EnabledChanged += async (_, _) =>
        {
            var missionPack = infoWindows.GetMissionPackInfo();
            bool rander = infoWindows.GetRander();
            bool csf = infoWindows.GetCsf();
            infoWindows.Dispose();
            if (missionPack == null)
                return;

            missionPack.Create(); //写入配置

            if (rander)
            {
                string[] mapFiles = Directory.GetFiles(missionPack.FilePath, "*.map");
                await RenderImage.RenderPreviewImageAsync(mapFiles);
            }
            ReLoad(); //重新载入
            触发刷新?.Invoke();
        };
    }

    private void BtnNew_LeftClick(object sender, EventArgs e)
    {
        var infoWindows = new 导入选择窗口(WindowManager);

        infoWindows.selected += (b1, b2, path) =>
        {
            if (DDModAI.SelectedIndex == 0)
                导入Mod(b1, b2, path);
            if (DDModAI.SelectedIndex == 1)
                导入任务包(b1, b2, path);
        };

        var dp = DarkeningPanel.AddAndInitializeWithControl(WindowManager, infoWindows);
    }

    private void ReLoad()
    {
        Mod.ReLoad();
        MissionPack.ReLoad();
        // listBoxModAI.Clear();
        DDModAI_SelectedIndexChanged(DDModAI, null);
        LoadModInfo();
    }

    private void BtnDel_LeftClick(object sender, EventArgs e)
    {
        if (((InfoBaseClass)ListBoxModAi?.SelectedItem?.Tag)?.CanDel != true)
        {
            XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), "The built-in mission package cannot be deleted".L10N("UI:DTAConfig:DeleteBuilt-inMissionPackage"));
            return;
        }

        if (DDModAI.SelectedIndex == 1)
        {
            if (ListBoxModAi.SelectedItem.Tag is not MissionPack missionPack)
                return;

            删除任务包(missionPack);
        }
        else if (DDModAI.SelectedIndex == 0)
        {
            if (ListBoxModAi.SelectedItem.Tag is not Mod mod)
                return;

            foreach (var missionPack in MissionPack.MissionPacks)
            {
                if (missionPack.Mod == mod.ID && !mod.MuVisible)
                {
                    XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), $"这个Mod被任务包 {missionPack.Name} 使用, 无法删除, 如要删除请删除任务包");
                    return;
                }
            }

            XNAMessageBox xNAMessageBox = new XNAMessageBox(WindowManager, "Delete Confirmation".L10N("UI:Main:DeleteConfirmation"),
                "Are you sure you want to delete the Mod".L10N("UI:DTAConfig:ModDeleteConfirmation") + ListBoxModAi.SelectedItem.Text + "?", XNAMessageBoxButtons.YesNo);
            xNAMessageBox.YesClickedAction += (_) => DelMod(mod);
            xNAMessageBox.Show();
        }
    }

    public void 删除任务包(MissionPack missionPack)
    {
        if (!missionPack.CanDel)
        {
            XNAMessageBox.Show(WindowManager, "Tips".L10N("UI:Main:Tips"), "The built-in mod cannot be deleted".L10N("UI:DTAConfig:DeleteBuilt-inMod"));
            return;
        }
        RenderImage.CancelRendering();
        var inifile = new IniFile(missionPack.FileName);
        var m = string.Empty;

        foreach (var s in inifile.GetSections())
        {
            if (inifile.GetValue(s, "MissionPack", string.Empty) == missionPack.ID)
                m += inifile.GetValue(s, "Description", s) + Environment.NewLine;
        }

        var xNAMessageBox = new XNAMessageBox(WindowManager, "Delete Confirmation".L10N("UI:Main:DeleteConfirmation"),
            $"您确定要删除任务包 {missionPack.Name}? 它包含以下任务: {Environment.NewLine}{m} ", XNAMessageBoxButtons.YesNo);
        xNAMessageBox.YesClickedAction += (_) => { DelMissionPack(missionPack); };
        xNAMessageBox.Show();
    }

    public void DelMissionPack(MissionPack missionPack)
    {
        if (File.Exists(Path.Combine(ProgramConstants.GamePath, missionPack.FileName)))
        {
            var iniFile = new IniFile(missionPack.FileName);
            if (iniFile.GetSection("MissionPack").Keys.Count == 1)
                File.Delete(missionPack.FileName);
            else
            {
                iniFile.RemoveKey("MissionPack", missionPack.ID);
                foreach (var fore in iniFile.GetSections())
                {
                    if (iniFile.GetValue(fore, "MissionPack", string.Empty) == missionPack.ID)
                    {
                        iniFile.RemoveKey("Battles", fore);
                        iniFile.RemoveSection(fore);
                    }
                }
                iniFile.WriteIniFile();
            }
        }

        if (!string.IsNullOrEmpty(missionPack.DefaultMod))
        {
            var mod = Mod.Mods.Find(m => m.ID == missionPack.DefaultMod);
            if (mod != null && mod.CanDel)
            {
                DelMod(mod);
            }
        }

        try
        {
            if (missionPack.FilePath.Replace('/', '\\').Contains("Maps\\CP"))
                FileHelper.ForceDeleteDirectory(missionPack.FilePath);
        }
        catch
        {
            XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), "Deleting file failed, possibly due to a file being occupied".L10N("UI:DTAConfig:DeleteFileOccupied"));
        }

        ReLoad();
        触发刷新?.Invoke();
        Task.Run(RenderImage.RenderImages);
    }

    public void DelMod(Mod mod)
    {
        if (!mod.CanDel)
        {
            XNAMessageBox.Show(WindowManager, "Tips".L10N("UI:Main:Tips"), "The built-in mod cannot be deleted".L10N("UI:DTAConfig:DeleteBuilt-inMod"));
            return;
        }
        RenderImage.CancelRendering();

        var iniFile = new IniFile(mod.FileName);
        if (iniFile.GetSection("Mod").Keys.Count == 1)
            File.Delete(mod.FileName);
        else
        {
            iniFile.RemoveKey("Mod", mod.ID);
            iniFile.RemoveSection(mod.ID);
            iniFile.WriteIniFile();
        }
        try
        {
            if (mod.FilePath.Replace('/', '\\').Contains("Mod&AI"))
                FileHelper.ForceDeleteDirectory(mod.FilePath);
        }
        catch
        {
            XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), "Deleting file failed, possibly due to a file being occupied".L10N("UI:DTAConfig:DeleteFileOccupied"));
        }
        ReLoad();
        触发刷新?.Invoke();
        RenderImage.RenderImages();
    }

    private void BtnReturn_LeftClick(object sender, EventArgs e)
    {
        //CampaignSelector.GetInstance().ScreenMission();
        //触发刷新?.Invoke();
        ListBoxModAi.Clear();
        _mcListBoxInfo.ClearItems();
        Parent.RemoveChild(this);
        Disable();
    }

    private void McListBoxInfoSelectedIndexChanged(object sender, EventArgs e)
    {
        if (_mcListBoxInfo.SelectedIndex == -1)
        {
            return;
        }

        var text = _mcListBoxInfo.GetItem(1, _mcListBoxInfo.SelectedIndex).Text;

        _tooltip.Dispose();
        _tooltip.Blocked = true;
        _mcListBoxInfo.RemoveChild(_tooltip);
        _tooltip = new ToolTip(WindowManager, _mcListBoxInfo);
        if (!string.IsNullOrEmpty(text))
            _tooltip.Text = text;
        _mcListBoxInfo.OnMouseLeave();
        _mcListBoxInfo.OnMouseEnter();
    }

    private void ListBoxModAISelectedIndexChanged(object sender, EventArgs e)
    {
        LoadModInfo();
    }

    private void LoadModInfo()
    {
        _mcListBoxInfo.ClearItems();
        Dictionary<string, string> properties = null;
        if (ListBoxModAi.SelectedItem == null)
            return;
        switch (DDModAI.SelectedIndex)
        {
            //筛选Mod
            case 0:
                var mod = Mod.Mods.Find(m => m.ID == ((Mod)ListBoxModAi.SelectedItem.Tag).ID);
                properties = mod.GetProperties();
                BtnNew.Text = "Import a new Mod".L10N("UI:DTAConfig:ImportNewMod");
                BtnDel.Text = "Delete Mod".L10N("UI:DTAConfig:DeleteMod");
                BtnNew.Enable();
                BtnDel.Enable();
                break;
            //筛选任务包
            case 1:
                var missionPack = MissionPack.MissionPacks.Find(m => m.ID == ((MissionPack)ListBoxModAi.SelectedItem.Tag).ID);
                properties = missionPack.GetProperties();
                BtnNew.Text = "Import mission package".L10N("UI:DTAConfig:ImportMissionPackage");
                BtnDel.Text = "Delete mission package".L10N("UI:DTAConfig:DeleteMissionPackage");
                BtnNew.Enable();
                BtnDel.Enable();
                break;
        }

        if (properties != null)
        {
            foreach (var property in properties)
            {
                _mcListBoxInfo.AddItem(new[] { new XNAListBoxItem(property.Key), new XNAListBoxItem(property.Value) });
            }
        }
    }

    private ModManager(WindowManager windowManager) : base(windowManager) { }

    public static ModManager GetInstance(WindowManager windowManager)
    {
        if (_instance != null)
        {
            return _instance;
        }
        
        _instance = new ModManager(windowManager);
        _instance.Initialize();
        return _instance;
    }
}

public class 导入选择窗口(WindowManager windowManager) : XNAWindow(windowManager)
{
    public XNAClientCheckBox chkCopyFile;
    public XNAClientCheckBox chkDeepImport;
    private XNALabel lblPath;
    private XNAClientButton btnCancel;
    private XNAClientButton btnOk;

    public Action<bool, bool, string> selected;

    public override void Initialize()
    {

        ClientRectangle = new Rectangle(0, 0, 300, 200);
        CenterOnParent();

        var btnFold = new XNAClientButton(windowManager)
        {
            Text = "Import from folder".L10N("UI:DTAConfig:ImportFromFolder"),
            ClientRectangle = new Rectangle(40, 20, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        btnFold.LeftClick += BtnFold_LeftClick;

        var btnZip = new XNAClientButton(windowManager)
        {
            Text = "Import from compressed package".L10N("UI:DTAConfig:ImportFromCompressedPackage"),
            ClientRectangle = new Rectangle(160, 20, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT),
            
        };
        btnZip.LeftClick += BtnZip_LeftClick;
        //btnZip.SetToolTipText

        chkCopyFile = new XNAClientCheckBox(windowManager)
        {
            Text = "Copy Mod file".L10N("UI:DTAConfig:CopyModFile"),
            ClientRectangle = new Rectangle(20, 60, 0, 0)
        };
        chkCopyFile.SetToolTipText("After selecting, the client will copy and retain the file locally".L10N("UI:DTAConfig:ClientRetainFile"));
        chkCopyFile.CheckedChanged += ChkCopyFile_LeftClick;

        chkDeepImport = new XNAClientCheckBox(windowManager)
        {
            Text = "Deep import".L10N("UI:DTAConfig:DeepImport"),
            ClientRectangle = new Rectangle(20, 90, 0, 0),
            Checked = true,
            AllowChecking = false
            
        };
        chkDeepImport.SetToolTipText("Deep import and deep copy If there are problems with the game after importing, you can check this option to import again, but it will increase the space occupied".L10N("UI:DTAConfig:DeepImportTip"));

        lblPath = new XNALabel(windowManager)
        {
            Text = string.Empty,  
            ClientRectangle = new Rectangle(40, 120, 0, 0)
        };

        btnCancel = new XNAClientButton(windowManager)
        {
            Text = "Cancel".L10N("UI:DTAConfig:ButtonCancel"),
            ClientRectangle = new Rectangle(160, 150, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        btnCancel.LeftClick += BtnCancel_LeftClick;

        btnOk = new XNAClientButton(windowManager)
        {
            Text = "Yes".L10N("UI:Main:ButtonYes"),
            ClientRectangle = new Rectangle(40, 150, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };

        btnOk.LeftClick += BtnOk_LeftClick;

        base.Initialize();

        AddChild(btnFold);
        AddChild(btnZip);
        AddChild(chkCopyFile);
        AddChild(chkDeepImport);
        AddChild(lblPath);
        AddChild(btnOk);
        AddChild(btnCancel);
    }

    private void ChkCopyFile_LeftClick(object sender, EventArgs e)
    {
        if (chkCopyFile.Checked)
        {
            chkDeepImport.AllowChecking = true;
        }
        else
        {
            chkDeepImport.Checked = true;
            chkDeepImport.AllowChecking = false;
        }
    }

    private void BtnCancel_LeftClick(object sender, EventArgs e)
    {
        var box = new XNAMessageBox(WindowManager, "Tips".L10N("UI:Main:Tips"), "Are you sure to exit the import?".L10N("UI:DTAConfig:ExitImport"),XNAMessageBoxButtons.YesNo);

        box.YesClickedAction += (_) =>
        {
            Disable();
            Dispose();
        };

        if (lblPath.Text == string.Empty)
        {
            box.YesClickedAction.Invoke(box);
        }
        else
        {
            box.Show();
        }
    }

    private void BtnOk_LeftClick(object sender, EventArgs e)
    {
        if (lblPath.Text == string.Empty)
        {
            XNAMessageBox.Show(WindowManager, "Tips".L10N("UI:Main:Tips"), "请先点击上方按钮选择目标".L10N("UI:DTAConfig:SelectTarget"));
            return;
        }

        selected?.Invoke(chkCopyFile.Checked, chkDeepImport.Checked, lblPath.Tag as string);
        Disable();
        Dispose();
    }

    private void BtnZip_LeftClick(object sender, EventArgs e)
    {
        using OpenFileDialog fileDialog = new OpenFileDialog();
        fileDialog.Filter = "压缩包 (*.zip;*.7z;*.rar)|*.zip;*.7z;*.rar";
        fileDialog.Title = "选择压缩包";
        fileDialog.Multiselect = true; // 只能选一个文件

        if (fileDialog.ShowDialog() == DialogResult.OK)
        {

            List<string> paths = [];

            foreach (var fileName in fileDialog.FileNames)
            {
                var tagerPath = Path.Combine(ProgramConstants.GamePath, "Tmp", Path.GetFileNameWithoutExtension(fileName));
                if (SevenZip.ExtractWith7Zip(fileName, tagerPath))
                    paths.Add(tagerPath);
            }

            var fileNames = paths.Select(f => Path.GetFileNameWithoutExtension(f));

            var d = string.Join(",", fileNames);

            var s = string.Join(",", paths);
            lblPath.Text = d[..Math.Min(d.Length, 25)];
            lblPath.Tag = s;
            chkCopyFile.Checked = true;
            chkCopyFile.AllowChecking = false;
        }
    }

    private void BtnFold_LeftClick(object sender, EventArgs e)
    {
        using FolderBrowserDialog folderDialog = new FolderBrowserDialog();
        if (folderDialog.ShowDialog() == DialogResult.OK)
        {
            var p = folderDialog.SelectedPath + "\\";
            if (p.Contains(ProgramConstants.GamePath))
            {
                XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), "不能选择重聚安装路径里的文件夹");
                return;
            }

            lblPath.Text = folderDialog.SelectedPath;
            lblPath.Tag = folderDialog.SelectedPath;
            chkDeepImport.AllowChecking = chkCopyFile.AllowChecking;
        }
    }
}

public class ModInfoWindows : XNAWindow
{

    private const int CtbW = 130;
    private const int CtbH = 25;

    private readonly string _title;

    //private XNATextBox _ctbModID;
    private XNATextBox _ctbModName;
    private XNATextBox _ctbAuthor;
    private XNATextBox _ctbModDescription;
    private XNATextBox _ctbVersion;
    private XNATextBox _ctbModPath;
    private XNATextBox _ctbCountries;
    private XNACheckBox _chkMutil;
    private XNACheckBox _chkCsf;

    private XNATextBox _ctbCp;

    private XNACheckBox _chkExtensionOn;
    private XNATextBox _ctbExtension;
    private bool _cancel = false;

    private Mod _mod;

    public ModInfoWindows(WindowManager windowManager, Mod mod, string Title) : base(windowManager)
    {
        _mod = mod;
        _title = Title;
    }

    public override void Initialize()
    {
        base.Initialize();
        // Disable();
        //Console.WriteLine(_mod.ToString());
        ClientRectangle = new Rectangle(0, 0, 550, 400);
        CenterOnParent();

        var _lblTitle = new XNALabel(WindowManager)
        {
            ClientRectangle = new Rectangle(230, 20, 0, 0)

        };
        AddChild(_lblTitle);

        //第一行
        //var lblModID = new XNALabel(WindowManager)
        //{
        //    Text = "ModID(唯一):",
        //    ClientRectangle = new Rectangle(20, 60, 0, 0)

        //};
        //AddChild(lblModID);

        //_ctbModID = new XNATextBox(WindowManager)
        //{
        //    ClientRectangle = new Rectangle(lblModID.Right + 100, lblModID.Y, CtbW, CtbH)
        //};
        //AddChild(_ctbModID);

        var lblModName = new XNALabel(WindowManager)
        {
            Text = "Mod Name:".L10N("UI:DTAConfig:ModName"),
            ClientRectangle = new Rectangle(20, 60, 0, 0)
        };
        AddChild(lblModName);


        _ctbModName = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblModName.Right + 100, lblModName.Y, CtbW, CtbH)
        };
        AddChild(_ctbModName);

        var lblAuthor = new XNALabel(WindowManager)
        {
            Text = "Mod Author:".L10N("UI:DTAConfig:ModAuthor"),
            ClientRectangle = new Rectangle(lblModName.X, 100, 0, 0)
        };
        AddChild(lblAuthor);

        _ctbAuthor = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblAuthor.Right + 100, lblAuthor.Y, CtbW, CtbH)
        };
        AddChild(_ctbAuthor);

        var lblVersion = new XNALabel(WindowManager)
        {
            Text = "Mod Version:".L10N("UI:DTAConfig:ModVersion"),
            ClientRectangle = new Rectangle(300, lblAuthor.Y, 0, 0)
        };
        AddChild(lblVersion);

        _ctbVersion = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblVersion.Right + 100, lblVersion.Y, CtbW, CtbH)
        };
        AddChild(_ctbVersion);

        //第二行
        var lblDescription = new XNALabel(WindowManager)
        {
            Text = "Mod Introduction:".L10N("UI:DTAConfig:ModIntroduction"),
            ClientRectangle = new Rectangle(lblModName.X, 140, 0, 0)
        };
        AddChild(lblDescription);

        _ctbModDescription = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblDescription.Right + 100, lblDescription.Y, CtbW, CtbH)
        };
        AddChild(_ctbModDescription);

        var lblCountries = new XNALabel(WindowManager)
        {
            Text = "Mod Country:".L10N("UI:DTAConfig:ModCountry"),
            ClientRectangle = new Rectangle(lblVersion.X, lblDescription.Y, 0, 0)
        };
        AddChild(lblCountries);

        _ctbCountries = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblCountries.Right + 100, lblCountries.Y, CtbW, CtbH)
        };
        AddChild(_ctbCountries);

        //第三行
        var lblModPath = new XNALabel(WindowManager)
        {
            Text = "Mod Path:".L10N("UI:DTAConfig:ModPath"),
            ClientRectangle = new Rectangle(lblModName.X, 180, 0, 0),
            Visible = false
        };
        AddChild(lblModPath);

        _ctbModPath = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblModPath.Right + 100, lblModPath.Y, CtbW, CtbH),
            Visible = false
        };
        AddChild(_ctbModPath);

        _chkMutil = new XNACheckBox(WindowManager)
        {
            Text = "Can be used in Skirmish".L10N("UI:DTAConfig:SkirmishUsed"),
            ClientRectangle = new Rectangle(lblCountries.X, _ctbModPath.Y, 0, 0),
            Checked = true
        };
        AddChild(_chkMutil);


        //第四行
        var lblCp = new XNALabel(WindowManager)
        {
            Text = "Compatible with campaigns".L10N("UI:DTAConfig:CompatibleCampaigns"),
            ClientRectangle = new Rectangle(lblModName.X, 220, 0, 0)
        };
        AddChild(lblCp);

        _ctbCp = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblCp.X + 100, lblCp.Y, CtbW, CtbH)
        };


        AddChild(_ctbCp);

        _chkExtensionOn = new XNACheckBox(WindowManager)
        {
            Text = "Extension platform must be enabled".L10N("UI:DTAConfig:EnabledExtensionPlatform"),
            ClientRectangle = new Rectangle(_chkMutil.X, _ctbCp.Y, 0, 0),
            Visible = false,
        };
        AddChild(_chkExtensionOn);

        var lblExtension = new XNALabel(WindowManager)
        {
            Text = "Supported extension platforms".L10N("UI:DTAConfig:SupportedExtensionPlatforms"),
            ClientRectangle = new Rectangle(lblCp.X, 260, 0, 0)
        };
        AddChild(lblExtension);

        _ctbExtension = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblExtension.Right + 100, lblExtension.Y, CtbW, CtbH)
        };
        AddChild(_ctbExtension);

        _chkCsf = new XNACheckBox(WindowManager)
        {
            Text = "Convert to Simplified Chinese".L10N("UI:DTAConfig:ConvertSimplifiedChinese"),
            ClientRectangle = new Rectangle(_chkExtensionOn.X, lblExtension.Y, 0, 0)
        };
        AddChild(_chkCsf);

        var btnOk = new XNAClientButton(WindowManager)
        {
            Text = "Yes".L10N("UI:Main:ButtonYes"),
            ClientRectangle = new Rectangle(150, 330, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        AddChild(btnOk);
        btnOk.LeftClick += (_, _) =>
        {
            Disable();
        };

        var btnCancel = new XNAClientButton(WindowManager)
        {
            Text = "Cancel".L10N("UI:DTAConfig:ButtonCancel"),
            ClientRectangle = new Rectangle(310, 330, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        AddChild(btnCancel);
        btnCancel.LeftClick += (_, _) =>
        {
            _cancel = true;
            Disable();
        };

        _lblTitle.Text = _title;
        _ctbCp.Text = _mod.Compatible;
        //_ctbModID.Text = _mod.ID;
        _ctbModName.Text = _mod.Name;
        _ctbModDescription.Text = _mod.Description;
        _ctbVersion.Text = _mod.Version;
        _ctbModPath.Text = _mod.FilePath ?? $"Mod&AI\\{_mod.ID}";
        _ctbCountries.Text = _mod.Countries;
        _ctbAuthor.Text = _mod.Author;
        _chkCsf.Checked = UserINISettings.Instance.SimplifiedCSF;
    }

    public bool GetCsf()
    {
        return _chkCsf.Checked && _chkCsf.Visible;
    }

    /// <summary>
    /// 获取Mod更新后的信息。
    /// </summary>
    /// <returns></returns>
    public Mod GetModInfo()
    {
        if (_cancel)
            return null;

        //    _mod.ID = _ctbModID.Text.Trim(); //id
        //  _mod.ID = _mod.ID;
        _mod.Name = _ctbModName.Text.Trim(); //名称
        _mod.Description = _ctbModDescription.Text.Trim(); //介绍
        _mod.Version = _ctbVersion.Text.Trim(); //版本号
                                                // _mod.FilePath = _mod.FilePath; //路径
        _mod.Compatible = _ctbCp.Text.Trim(); //兼容的战役
        _mod.Countries = _ctbCountries.Text.Trim(); //国家
        _mod.MuVisible = _chkMutil.Checked; //遭遇战可用
                                            //_mod.CpVisible = CpVisible.SelectedIndex != 0; //兼容的战役
                                            //    _mod.ExtensionOn = _mod.ExtensionOn; //必须启用扩展
        _mod.Author = _ctbAuthor.Text.Trim();
        return _mod;
    }
}


public class MissionPackInfoWindows : XNAWindow
{
    private const int CtbW = 130;
    private const int CtbH = 25;

    private readonly string _title;

    private XNATextBox _ctbMissionPackID;
    private XNATextBox _ctbMissionPackName;
    private XNATextBox _ctbAuthor;
    private XNATextBox _ctbMissionPackDescription;
    private XNATextBox _ctbMissionPackPath;
    private XNACheckBox _chkRender;
    private XNACheckBox _chkCsf;
    private XNATextBox _ctbMissionCount;
    private XNATextBox _ctbCp;

    private XNACheckBox _chkExtensionOn;
    private XNATextBox _ctbExtension;
    private bool _cancel = false;
    public bool missionMix;
    public bool csfExist;
    private MissionPack _pack;
    public int missionCount;

    public MissionPackInfoWindows(WindowManager windowManager, MissionPack pack, string title, bool missionMix, bool csfExist) : base(windowManager)
    {
        _pack = pack;
        _title = title;
        this.missionMix = missionMix;
        this.csfExist = csfExist;
    }

    public override void Initialize()
    {
        base.Initialize();
        // Disable();
        //Console.WriteLine(map.ToString());
        ClientRectangle = new Rectangle(0, 0, 550, 400);
        CenterOnParent();

        var _lblTitle = new XNALabel(WindowManager)
        {
            ClientRectangle = new Rectangle(230, 20, 0, 0)

        };
        AddChild(_lblTitle);

        //第一行
        //var lblMissionPackID = new XNALabel(WindowManager)
        //{
        //    Text = "任务包ID(唯一):",
        //    ClientRectangle = new Rectangle(20, 60, 0, 0)

        //};
        //AddChild(lblMissionPackID);

        //_ctbMissionPackID = new XNATextBox(WindowManager)
        //{
        //    ClientRectangle = new Rectangle(lblMissionPackID.Right + 100, lblMissionPackID.Y, CtbW, CtbH)
        //};
        //AddChild(_ctbMissionPackID);

        var lblMissionPackName = new XNALabel(WindowManager)
        {
            Text = "Mission Package Name:".L10N("UI:DTAConfig:MissionPackageName"),
            ClientRectangle = new Rectangle(300, 60, 0, 0)
        };
        AddChild(lblMissionPackName);


        _ctbMissionPackName = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblMissionPackName.Right + 100, lblMissionPackName.Y, CtbW, CtbH)
        };
        AddChild(_ctbMissionPackName);

        var lblAuthor = new XNALabel(WindowManager)
        {
            Text = "Mission Package Author:".L10N("UI:DTAConfig:MissionPackageAuthor"),
            ClientRectangle = new Rectangle(20, 100, 0, 0)
        };
        AddChild(lblAuthor);

        _ctbAuthor = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblAuthor.Right + 100, lblAuthor.Y, CtbW, CtbH)
        };
        AddChild(_ctbAuthor);

        var lblMissionCount = new XNALabel(WindowManager)
        {
            Text = "Number of mission levels".L10N("UI:DTAConfig:MissionLevels"),
            ClientRectangle = new Rectangle(lblMissionPackName.X, 100, 0, 0),
            //Visible = missionMix
            Visible = false
        };
        AddChild(lblMissionCount);

        _ctbMissionCount = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblMissionCount.Right + 100, lblMissionCount.Y, CtbW, CtbH),
            //Visible = missionMix
            Visible = false
        };

        AddChild(_ctbMissionCount);

        var lblDescription = new XNALabel(WindowManager)
        {
            Text = "Mission Package Introduction:".L10N("UI:DTAConfig:MissionPackageIntroduction"),
            ClientRectangle = new Rectangle(lblAuthor.X, 140, 0, 0)
        };
        AddChild(lblDescription);

        _ctbMissionPackDescription = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblDescription.Right + 100, lblDescription.Y, CtbW, CtbH)
        };
        AddChild(_ctbMissionPackDescription);


        //第三行
        var lblMissionPackPath = new XNALabel(WindowManager)
        {
            Text = "Mission Package Path:".L10N("UI:DTAConfig:MissionPackagePath"),
            ClientRectangle = new Rectangle(lblAuthor.X, 180, 0, 0)
        };
        AddChild(lblMissionPackPath);

        _ctbMissionPackPath = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblMissionPackPath.Right + 100, lblMissionPackPath.Y, CtbW, CtbH)
        };
        AddChild(_ctbMissionPackPath);

        _chkRender = new XNACheckBox(WindowManager)
        {
            Text = "Rendering Preview Image".L10N("UI:DTAConfig:RenderingPreviewImage"),
            ClientRectangle = new Rectangle(lblMissionPackName.X, _ctbMissionPackPath.Y, 0, 0),
            Checked = true
        };
        AddChild(_chkRender);

        //第四行
        var lblCp = new XNALabel(WindowManager)
        {
            Text = "The Mod Used:".L10N("UI:DTAConfig:ModUsed"),
            ClientRectangle = new Rectangle(lblAuthor.X, 220, 0, 0)
        };
        AddChild(lblCp);

        _ctbCp = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblCp.X + 100, lblCp.Y, CtbW, CtbH)
        };

        AddChild(_ctbCp);

        _chkCsf = new XNACheckBox(WindowManager)
        {
            Text = "Convert to Simplified Chinese".L10N("UI:DTAConfig:ConvertSimplifiedChinese"),
            ClientRectangle = new Rectangle(lblMissionPackName.X, _ctbCp.Y, 0, 0),
            Checked = true
        };
        AddChild(_chkCsf);

        var btnOk = new XNAClientButton(WindowManager)
        {
            Text = "Yes".L10N("UI:Main:ButtonYes"),
            ClientRectangle = new Rectangle(150, 330, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        AddChild(btnOk);

        btnOk.LeftClick += (_, _) =>
        {

            if (MissionPack.QueryID(_pack.ID) && _title == "导入任务包")
            {
                XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), "The MissionPackID already exists".L10N("UI:DTAConfig:MissionPackIDExists"));
                return;
            }
            //if (missionMix)
            //{
            //    if (string.IsNullOrEmpty(_ctbMissionCount?.Text))
            //    {
            //        XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), "我们无法推测这个任务包有几个关卡，需要您指定一下");
            //        return;
            //    }

            //    if (!int.TryParse(_ctbMissionCount.Text, out missionCount))
            //    {
            //        XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), "任务关数只能输入数字");
            //        return;
            //    }
            //}

            Disable();
        };

        var btnCancel = new XNAClientButton(WindowManager)
        {
            Text = "Cancel".L10N("UI:DTAConfig:ButtonCancel"),
            ClientRectangle = new Rectangle(310, 330, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        AddChild(btnCancel);
        btnCancel.LeftClick += (_, _) =>
        {
            _cancel = true;
            Disable();
        };

        _lblTitle.Text = _title;
        //_ctbMissionPackID.Text = _pack.ID;
        _ctbMissionPackName.Text = _pack.Name;
        _ctbMissionPackDescription.Text = _pack.Name;
        _ctbMissionPackPath.Text = _pack.FilePath;
        _ctbCp.Text = _pack.Mod;
        // Console.WriteLine(map.Author);
        _ctbAuthor.Text = _pack.Author;
        _chkRender.Checked = UserINISettings.Instance.RenderPreviewImage;
        _chkCsf.Checked = UserINISettings.Instance.SimplifiedCSF;


        if (missionMix)
            _chkRender.Visible = false;

        if (!csfExist)
            _chkCsf.Visible = false;

        if (_title == "修改任务包")
        {
            _chkRender.Text = "重新渲染预览图";
            _chkRender.Checked = false;
            _chkCsf.Text = "重新转换为简体中文";
            _chkCsf.Checked = false;
        }
    }

    public bool GetCsf()
    {
        return _chkCsf.Checked && _chkCsf.Visible;
    }

    public bool GetRander()
    {
        return _chkRender.Checked && _chkRender.Visible;
    }

    /// <summary>
    /// 获取MissionPack更新后的信息。
    /// </summary>
    /// <returns></returns>
    public MissionPack GetMissionPackInfo()
    {
        if (_cancel)
            return null;

        _pack.ID = _pack.ID; //id
        _pack.Name = _ctbMissionPackName.Text.Trim(); //名称
        _pack.Description = _ctbMissionPackDescription.Text.Trim(); //介绍
        _pack.FilePath = _ctbMissionPackPath.Text.Trim();
        _pack.Author = _ctbAuthor.Text.Trim();
        _pack.LongDescription = _ctbMissionPackDescription.Text.Trim();
        _pack.Mod = _ctbCp.Text.Trim();
        return _pack;
    }
}

public class EditCSFWindows : XNAWindow
{
    public EditCSFWindows(WindowManager windowManager, ToolTip _tooltip, CSF _csf) : base(windowManager)
    {
        this.windowManager = windowManager;
        this._tooltip = _tooltip;
        this._csf = _csf;
        _csfDictionary = _csf.GetCsfDictionary();
    }

    private WindowManager windowManager;
    private XNASuggestionTextBox _tbSearch;
    private XNAMultiColumnListBox _mcListBoxCsfInfo;
    private ToolTip _tooltip;
    private CSF _csf;
    private Dictionary<string, string> _csfDictionary;
    private XNAContextMenu _menu;

    private void DelCsf(string key)
    {
        _csfDictionary.Remove(key);
        Reload();
    }

    private void Reload()
    {
        if (_csfDictionary == null)
            return;
        _mcListBoxCsfInfo.ClearItems();
        if (!string.IsNullOrEmpty(_tbSearch.Text) && _tbSearch.Text != "搜索键或值")
            foreach (var (k, v) in _csfDictionary)
            {
                if (k.ToUpper().Contains(_tbSearch.Text.ToUpper()) || v.ToUpper().Contains(_tbSearch.Text.ToUpper()))
                {
                    _mcListBoxCsfInfo.AddItem(new[] { new XNAListBoxItem(k), new XNAListBoxItem(v) });
                }
            }
        else
            foreach (var (k, v) in _csfDictionary)
            {
                _mcListBoxCsfInfo.AddItem(new[] { new XNAListBoxItem(k), new XNAListBoxItem(v) });
            }
    }

    public override void Initialize()
    {
        _tbSearch = new XNASuggestionTextBox(windowManager);
        _tbSearch.ClientRectangle = new Rectangle(12, 12, 210, 25);
        _tbSearch.Suggestion = "搜索键或值";

        _tbSearch.TextChanged += (_, _) => { Reload(); };

        _mcListBoxCsfInfo = new XNAMultiColumnListBox(windowManager);
        _mcListBoxCsfInfo.ClientRectangle = new Rectangle(12, _tbSearch.Bottom + 12, 320, 250);

        _mcListBoxCsfInfo.AddColumn("键", 120);
        _mcListBoxCsfInfo.AddColumn("值", 200);

        _mcListBoxCsfInfo.SelectedIndexChanged += McListBoxCsfInfoSelectedIndexChanged;
        _mcListBoxCsfInfo.RightClick += McListBoxCsfInfoRightClick;

        AddChild(_tbSearch);
        AddChild(_mcListBoxCsfInfo);

        ClientRectangle = new Rectangle(0, 0, _mcListBoxCsfInfo.Right + 48, _mcListBoxCsfInfo.Bottom + 50);
        WindowManager.CenterControlOnScreen(this);

        var btnSave = new XNAClientButton(windowManager)
        {
            Text = "Save".L10N("UI:DTAConfig:ButtonSave"),
            ClientRectangle = new Rectangle(12, _mcListBoxCsfInfo.Bottom + 12, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        btnSave.LeftClick += Save;
        AddChild(btnSave);

        var btnCancel = new XNAClientButton(windowManager)
        {
            Text = "Cancel".L10N("UI:DTAConfig:ButtonCancel"),
            ClientRectangle = new Rectangle(btnSave.Right + 12, _mcListBoxCsfInfo.Bottom + 12, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        btnCancel.LeftClick += (_, _) => Disable();
        AddChild(btnCancel);

        _menu = new XNAContextMenu(windowManager);
        _menu.Name = nameof(_menu);
        _menu.Width = 100;

        _menu.AddItem(new XNAContextMenuItem
        {
            Text = "Add an entry".L10N("UI:DTAConfig:AddEntry"),
            SelectAction = Add
        });
        //修改
        _menu.AddItem("修改这个条目", Edit, () => _mcListBoxCsfInfo.SelectedIndex > -1);
        _menu.AddItem("删除这个条目", Del, () => _mcListBoxCsfInfo.SelectedIndex > -1);


        AddChild(_menu);

        base.Initialize();
    }

    private void Edit()
    {
        var key = _mcListBoxCsfInfo.GetItem(0, _mcListBoxCsfInfo.SelectedIndex).Text;
        var value = _mcListBoxCsfInfo.GetItem(1, _mcListBoxCsfInfo.SelectedIndex).Text;
        AddCsfWindows addCsfWindows = new AddCsfWindows(windowManager, _csfDictionary, key, value);
        addCsfWindows._reload += Reload;
        addCsfWindows.Show();
    }

    private void Add()
    {
        AddCsfWindows addCsfWindows = new AddCsfWindows(windowManager, _csfDictionary, "", "");
        addCsfWindows._reload += Reload;
        addCsfWindows.Show();
    }

    private void Del()
    {
        var key = _mcListBoxCsfInfo.GetItem(0, _mcListBoxCsfInfo.SelectedIndex).Text;
        var message = new XNAMessageBox(windowManager, "Delete Confirmation".L10N("UI:Main:DeleteConfirmation"), "Are you sure you want to delete this entry?".L10N("UI:DTAConfig:DeleteEntryConfirmation"), XNAMessageBoxButtons.YesNo);
        message.YesClickedAction += (_) => DelCsf(key);
        message.Show();
    }

    private void McListBoxCsfInfoRightClick(object sender, EventArgs e)
    {
        _mcListBoxCsfInfo.SelectedIndex = _mcListBoxCsfInfo.HoveredIndex;



        _menu.Open(GetCursorPoint());
    }

    private void McListBoxCsfInfoSelectedIndexChanged(object sender, EventArgs e)
    {
        if (_mcListBoxCsfInfo.SelectedIndex == -1)
        {
            return;
        }

        var text = _mcListBoxCsfInfo.GetItem(1, _mcListBoxCsfInfo.SelectedIndex).Text;

        _tooltip.Dispose();
        _tooltip.Blocked = true;
        _mcListBoxCsfInfo.RemoveChild(_tooltip);
        _tooltip = new ToolTip(WindowManager, _mcListBoxCsfInfo);
        if (!string.IsNullOrEmpty(text))
            _tooltip.Text = text;
        _mcListBoxCsfInfo.OnMouseLeave();
        _mcListBoxCsfInfo.OnMouseEnter();

    }

    private void Save(object sender, EventArgs e)
    {
        CSF.WriteCSF(_csfDictionary, _csf.csfPath);
        Disable();
    }

    public void Show()
    {
        DarkeningPanel.AddAndInitializeWithControl(WindowManager, this);
    }

}

public class AddCsfWindows : XNAWindow
{
    public AddCsfWindows(WindowManager windowManager, Dictionary<string, string> csfDictionary, string key, string value) : base(windowManager)
    {
        _csfDictionary = csfDictionary;
        _key = key;
        _value = value;
    }

    private Dictionary<string, string> _csfDictionary;
    private string _key;
    private string _value;
    private XNATextBox _tbKey;
    private XNATextBox _tbValue;
    public delegate void ReloadDelegate();
    public event ReloadDelegate _reload;

    public override void Initialize()
    {
        base.Initialize();

        var lblKey = new XNALabel(WindowManager);
        lblKey.Text = "键";
        lblKey.ClientRectangle = new Rectangle(20, 20, 0, 0);

        _tbKey = new XNATextBox(WindowManager);
        _tbKey.ClientRectangle = new Rectangle(lblKey.Right + 20, lblKey.Y, 150, 25);
        _tbKey.Text = _key;

        var lblValue = new XNALabel(WindowManager);
        lblValue.Text = "值";
        lblValue.ClientRectangle = new Rectangle(20, _tbKey.Bottom + 20, 0, 0);

        _tbValue = new XNATextBox(WindowManager);
        _tbValue.ClientRectangle = new Rectangle(lblValue.Right + 20, lblValue.Y, 150, 25);
        _tbValue.Text = _value;

        var btnAdd = new XNAClientButton(WindowManager);
        btnAdd.Text = "Add".L10N("UI:DTAConfig:Add");
        if (!string.IsNullOrEmpty(_key) || !string.IsNullOrEmpty(_value))
            btnAdd.Text = "Revise".L10N("UI:DTAConfig:Revise");
        btnAdd.ClientRectangle = new Rectangle(20, _tbValue.Bottom + 20, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
        btnAdd.LeftClick += Add;
        AddChild(btnAdd);

        var btnCancel = new XNAClientButton(WindowManager);
        btnCancel.Text = "Cancel".L10N("UI:Main:ButtonCancel");
        btnCancel.ClientRectangle = new Rectangle(btnAdd.Right + 12, _tbValue.Bottom + 20, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
        btnCancel.LeftClick += (_, _) => Disable();
        AddChild(btnCancel);

        AddChild(lblKey);
        AddChild(_tbKey);
        AddChild(lblValue);
        AddChild(_tbValue);

        ClientRectangle = new Rectangle(0, 0, 300, btnCancel.Bottom + 20);
        WindowManager.CenterControlOnScreen(this);
    }

    private void Add(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_tbKey.Text))
        {
            XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), "The key cannot be empty".L10N("UI:DTAConfig:KeyEmpty"));
            return;
        }

        _csfDictionary[_tbKey.Text] = _tbValue.Text;
        _reload?.Invoke();
        Disable();
    }

    public void Show()
    {
        DarkeningPanel.AddAndInitializeWithControl(WindowManager, this);
    }
}
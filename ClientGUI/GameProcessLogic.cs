using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.INIProcessing;
using DTAConfig.Entity;
using Localization;
using Localization.Tools;
using Rampastring.Tools;
using Rampastring.XNAUI;
using IniFile = Rampastring.Tools.IniFile;

namespace ClientGUI
{
    /// <summary>
    /// A static class used for controlling the launching and exiting of the game executable.
    /// </summary>
    public static class GameProcessLogic
    {
        public static event Action GameProcessStarted;

        public static event Action GameProcessStarting;

        public static event Action GameProcessExited;

        //public static string[] 旧存档数;

        public static bool UseQres { get; set; }
        public static bool SingleCoreAffinity { get; set; }

        private static string gameExecutableName;

        //public static bool 游戏中 = false;

        private static Mod mod;

        private static int DebugCount = 0;

        /// <summary>
        /// Starts the main game process.  
        /// </summary>
        /// 
        public static void StartGameProcess(WindowManager windowManager, IniFile iniFile = null)
        {
#if !DEBUG
            try
            {
#endif
                //RenderImage.CancelRendering();

            if (!加载模组文件(windowManager, iniFile)) return;

#if !DEBUG
            }
            catch(Exception ex)
            {
                XNAMessageBox.Show(windowManager,"错误",$"出现错误:{ex}");
                return;
            }
#endif
          
            WindowManager.progress.Report("正在唤起游戏");

            Logger.Log("About to launch main game executable.");

            int waitTimes = 0;
            while (PreprocessorBackgroundTask.Instance.IsRunning)
            {
                Thread.Sleep(1000);
                waitTimes++;
                if (waitTimes > 10)
                {
                    XNAMessageBox.Show(windowManager, 
                        "INI preprocessing not complete".L10N("UI:ClientGUI:INIPreprocessingNotCompleteTitle"),
                        ("INI preprocessing not complete. Please try " +
                        "launching the game again. If the problem persists, " +
                        "contact the game or mod authors for support.").L10N("UI:ClientGUI:INIPreprocessingNotCompleteText"));
                    return;
                }
            }

            OSVersion osVersion = ClientConfiguration.Instance.GetOperatingSystemVersion();

            string additionalExecutableName = string.Empty;

            string launcherExecutableName = ClientConfiguration.Instance.GameLauncherExecutableName;
            if (string.IsNullOrEmpty(launcherExecutableName))
                gameExecutableName = ClientConfiguration.Instance.GetGameExecutableName();
            else
            {
                gameExecutableName = launcherExecutableName;
                additionalExecutableName = "\"" + ClientConfiguration.Instance.GetGameExecutableName() + "\" ";
            }

            string extraCommandLine = ClientConfiguration.Instance.ExtraExeCommandLineParameters;

            //SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "DTA.LOG");
            //SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "TI.LOG");
            //SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "TS.LOG");

            GameProcessStarting?.Invoke();

            //if (UserINISettings.Instance.WindowedMode && UseQres && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            //{
            //    Logger.Log("Windowed mode is enabled - using QRes.");
            //    Process QResProcess = new Process();
            //    QResProcess.StartInfo.FileName = ProgramConstants.QRES_EXECUTABLE;

            //    if (!string.IsNullOrEmpty(extraCommandLine))
            //        QResProcess.StartInfo.Arguments = "c=16 /R " + "\"" + SafePath.CombineFilePath(ProgramConstants.GamePath, gameExecutableName) + "\" " + additionalExecutableName + "-SPAWN " + extraCommandLine;
            //    else
            //        QResProcess.StartInfo.Arguments = "c=16 /R " + "\"" + SafePath.CombineFilePath(ProgramConstants.GamePath, gameExecutableName) + "\" " + additionalExecutableName + "-SPAWN";
            //    QResProcess.EnableRaisingEvents = true;
            //    // QResProcess.Exited += new EventHandler(Process_Exited); 

            //    Logger.Log("启动命令: " + QResProcess.StartInfo.FileName);
            //    Logger.Log("启动参数: " + QResProcess.StartInfo.Arguments);
            //    try
            //    {
            //        QResProcess.Start();
            //    }
            //    catch (Exception ex)
            //    {
            //        Logger.Log("Error launching QRes: " + ex.Message);
            //        XNAMessageBox.Show(windowManager, "Error launching game", "Error launching " + ProgramConstants.QRES_EXECUTABLE + ". Please check that your anti-virus isn't blocking the CnCNet Client. " +
            //            "You can also try running the client as an administrator." + Environment.NewLine + Environment.NewLine + "You are unable to participate in this match." +
            //            Environment.NewLine + Environment.NewLine + "Returned error: " + ex.Message);
            //        Process_Exited(QResProcess, EventArgs.Empty);
            //        return;
            //    }

            //    if (Environment.ProcessorCount > 1 && SingleCoreAffinity)
            //        QResProcess.ProcessorAffinity = (IntPtr)2;
            //}
            //else
            //{
                string arguments;
                var 启用连点器 = true; //不启用连点

                if (!string.IsNullOrWhiteSpace(extraCommandLine))
                    arguments = " " + additionalExecutableName + "-SPAWN " + extraCommandLine;
                else
                    arguments = additionalExecutableName + "-SPAWN";

                if (File.Exists(Path.Combine(ProgramConstants.游戏目录, "syringe.exe")))
                {
                    gameExecutableName = "Syringe.exe";
                    arguments = "\"gamemd.exe\" -SPAWN " + extraCommandLine;

                }
                else
                {
                    gameExecutableName = "gamemd-spawn.exe";
                    arguments = "-SPAWN " + extraCommandLine;
                }

                if (File.Exists(Path.Combine(ProgramConstants.游戏目录, "ares.dll")))
                        启用连点器 = false;

                //FileInfo gameFileInfo = SafePath.GetFile(ProgramConstants.游戏目录, gameExecutableName);
                //if (!File.Exists(gameFileInfo.FullName))
                //{
                ////XNAMessageBox.Show(windowManager, "错误", $"{gameFileInfo.FullName}不存在，请前往设置清理游戏缓存后重试。");
                //    File.Copy("syringe.exe", gameFileInfo.FullName);
                //  //  return;
                //}

                ProcessStartInfo info = new ProcessStartInfo(gameExecutableName, arguments)
                {
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = ProgramConstants.游戏目录
                };

                var gameProcess = new Process
                {
                    StartInfo = info,
                    EnableRaisingEvents = true,

                };

                // 注册退出事件
                gameProcess.Exited += Process_Exited;

                Logger.Log("启动可执行文件: " + gameProcess.StartInfo.FileName);
                Logger.Log("启动参数: " + gameProcess.StartInfo.Arguments);

                if(Directory.Exists(Path.Combine(ProgramConstants.游戏目录, "Debug")))
                    DebugCount = Directory.GetDirectories(Path.Combine(ProgramConstants.游戏目录,"Debug")).Length;

                //旧存档数 = Directory.GetFiles(ProgramConstants.存档目录, "*.sav");
                try
                {
                    if(启用连点器 && UserINISettings.Instance.启用连点器.Value) ShiftClickAutoClicker.Instance.Start();
                    gameProcess.Start();
               
                WindowManager.progress.Report("游戏进行中....");
                    Logger.Log("游戏处理逻辑: 进程开始.");
                
                }
                catch (Exception ex)
                {
                    Logger.Log("Error launching " + gameExecutableName + ": " + ex.Message);
                    XNAMessageBox.Show(windowManager, "Error launching game", "Error launching " + gameExecutableName + ". Please check that your anti-virus isn't blocking the CnCNet Client. " +
                        "You can also try running the client as an administrator." + Environment.NewLine + Environment.NewLine + "You are unable to participate in this match." +
                        Environment.NewLine + Environment.NewLine + "Returned error: " + ex.Message);
                    Process_Exited(gameProcess, EventArgs.Empty);
                    return;
                }  
            //}

            GameProcessStarted?.Invoke();

            Logger.Log("等待 qres.dat 或 " + gameExecutableName + " 退出.");
        }

        static readonly FileInfo spawnerSettingsFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SPAWNER_SETTINGS);
        private static void 加载音乐(string modPath)
        {
            Mix.PackToMix($"{ProgramConstants.GamePath}Resources/thememd/", Path.Combine(ProgramConstants.游戏目录, "thememd.mix"));
            FileHelper.CopyFile($"{ProgramConstants.GamePath}Resources/thememd/thememd.ini", Path.Combine(ProgramConstants.游戏目录, "thememd.ini"),true);

            var csfPath = Path.Combine(modPath, "ra2md.csf");
            if (File.Exists(Path.Combine(ProgramConstants.游戏目录, "ra2md.csf")))
                csfPath = Path.Combine(ProgramConstants.游戏目录, "ra2md.csf");
            if (File.Exists(csfPath))
            {
                var d = new CSF(csfPath).GetCsfDictionary();
                if (d != null)
                {
                    foreach (var item in UserINISettings.Instance.MusicNameDictionary.Keys)
                    {
                        if (d.ContainsKey(item))
                        {
                            d[item] = UserINISettings.Instance.MusicNameDictionary[item];
                        }
                        else
                        {
                            d.Add(item, UserINISettings.Instance.MusicNameDictionary[item]);
                        }

                    }
                    CSF.WriteCSF(d, Path.Combine(ProgramConstants.游戏目录, "ra2md.csf"));
                }
            }
        }

        private static void 获取新的存档()
        {
            if (!Directory.Exists(ProgramConstants.存档目录)) return;

            var newSaves = Directory.GetFiles(ProgramConstants.存档目录, "*.sav");

            if (newSaves.Length == 0) return;

            var iniFile = new IniFile(Path.Combine(ProgramConstants.存档目录, "Save.ini"));
            var spawn = new IniFile(Path.Combine(ProgramConstants.GamePath, "spawn.ini"));
            var game = spawn.GetValue("Settings", "Game", string.Empty);

            var mission = spawn.GetValue<string>("Settings", "Mission", null);
            var 透明迷雾 = spawn.GetValue("Settings", "chkSatellite", false);
            var 战役ID = spawn.GetValue("Settings", "CampaignID", -1);
            var chkTerrain = spawn.GetValue("Settings", "chkTerrain", false);
            var chkAres = spawn.GetValue("Settings", "chkAres", false);
            var buildOffAlly = spawn.GetValue("Settings", "BuildOffAlly", false);
            //if (mission != null)
            //    mission = Path.GetFileName(mission);

            var missionSavePath = Path.Combine(ProgramConstants.存档目录, Path.GetFileName(mission ?? "Other"));
            if (!Directory.Exists(missionSavePath))
                Directory.CreateDirectory(missionSavePath);

            foreach (var item in newSaves)
            {
                File.Move(item,Path.Combine(missionSavePath,Path.GetFileName(item)), true);
            }

            foreach (var fileFullPath in newSaves)
            {
                string sectionName = Path.GetFileName(fileFullPath) + '-' + Path.GetFileName(mission ?? "Other");

                iniFile.SetValue(sectionName, "Game", game);
                iniFile.SetValue(sectionName, "Mission", mission ?? string.Empty);
                iniFile.SetValue(sectionName, "chkSatellite", 透明迷雾);
                if(战役ID!=-1)
                    iniFile.SetValue(sectionName, "CampaignID", 战役ID);
                iniFile.SetValue(sectionName, "chkTerrain", chkTerrain);
                iniFile.SetValue(sectionName, "chkAres", chkAres);
                iniFile.SetValue(sectionName, "BuildOffAlly", buildOffAlly);
            }

            iniFile.WriteIniFile();
        }

        private static Version GetWindowsVersion()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new Version(0, 0);

            try
            {
                var osvi = new OSVERSIONINFOEXW
                {
                    dwOSVersionInfoSize = (uint)Marshal.SizeOf(typeof(OSVERSIONINFOEXW))
                };

                RtlGetVersion(ref osvi);

                return new Version((int)osvi.dwMajorVersion, (int)osvi.dwMinorVersion, (int)osvi.dwBuildNumber);
            }
            catch
            {
                return Environment.OSVersion.Version;
            }
        }

        [DllImport("ntdll.dll", SetLastError = false)]
        private static extern int RtlGetVersion(ref OSVERSIONINFOEXW lpVersionInformation);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct OSVERSIONINFOEXW
        {
            public uint dwOSVersionInfoSize;
            public uint dwMajorVersion;
            public uint dwMinorVersion;
            public uint dwBuildNumber;
            public uint dwPlatformId;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]

            public string szCSDVersion;
            public ushort wServicePackMajor;
            public ushort wServicePackMinor;
            public short wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }

        public static bool IsNtfs(string path)
        {
            var drive = new DriveInfo(Path.GetPathRoot(path));
            return string.Equals(drive.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase);
        }

        public static bool 加载模组文件(WindowManager windowManager, IniFile iniFile) { 

            var newSection = iniFile.GetSection("Settings");

            mod = Mod.Mods.Find(m => m.FilePath == newSection.GetValue("Game", string.Empty));
            if (mod == null)
            {
                XNAMessageBox.Show(windowManager, "错误", $"模组文件已丢失：{newSection.GetValue("Game", string.Empty)}");
                return false;
            }

            if (!ProgramConstants.判断目录是否为纯净尤复(UserINISettings.Instance.YRPath))
            {
                var guideWindow = new YRPathWindow(windowManager);
                guideWindow.Show();
                return false;
            }

            FileHelper.KillGameMdProcesses();
            string newGame = newSection.GetValue("Game", string.Empty);
            string newMission = newSection.GetValue("Mission", string.Empty);
            bool Ares = newSection.GetValue("chkAres", false);
            bool Phobos = newSection.GetValue("chkPhobos", false);
            var otherFile = newSection.GetValue("OtherFile", string.Empty);

            try
            {   List<string> 所有需要复制的文件 = [];
                    
                if(!Directory.Exists(ProgramConstants.游戏目录))
                    Directory.CreateDirectory(ProgramConstants.游戏目录);

                    WindowManager.progress.Report("正在加载游戏文件");

                foreach (var file in ProgramConstants.PureHashes.Keys)
                    {
                        var newFile = Path.Combine(ProgramConstants.游戏目录, Path.GetFileName(file));
                        var sourceFile = Path.Combine(UserINISettings.Instance.YRPath, Path.GetFileName(file));

                    所有需要复制的文件.Add(sourceFile);
                }

                // 所有需要链接的文件.Add("TX");先注释了，改成只有启用地形扩展时再添加
                if (newSection.GetValue("chkTerrain", false))
                {
                    所有需要复制的文件.Add("TX");
                }
                所有需要复制的文件.Add("zh");
                所有需要复制的文件.Add("cncnet5.dll");
                所有需要复制的文件.Add($"Resources/Voice/{UserINISettings.Instance.Voice.Value}");

                // if(!Ares && !File.Exists(Path.Combine(newGame,"ares.dll")))
                所有需要复制的文件.Add("gamemd-spawn.exe");

                if (Ares)
                {
                    所有需要复制的文件.Add("Ares");
                    //所有需要复制的文件.Add("Syringe.exe");
                }
                if (Phobos)
                {
                    所有需要复制的文件.Add("Phobos");
                    //所有需要复制的文件.Add("Syringe.exe");
                }

                所有需要复制的文件.Add(newGame);

                if(newMission != newGame && newMission != string.Empty)
                    所有需要复制的文件.Add(newMission);

                if (newSection.KeyExists("CampaignID"))
                {
                    所有需要复制的文件.Add(SafePath.CombineFilePath(ProgramConstants.GamePath, "Resources\\MissionCache\\"));
                }

                if(otherFile != string.Empty)
                {
                    所有需要复制的文件.Add(otherFile);
                }

                // 所有需要复制的文件.Add("LiteExt.dll");
                所有需要复制的文件.Add("qres.dat");
                所有需要复制的文件.Add("qres32.dll");
                
                


                if (newSection.KeyExists("GameID"))
                {
                    var windowsVersion = GetWindowsVersion();
                    var thresholdVersion = new Version(10, 0, 14393);

                    if (windowsVersion >= thresholdVersion)
                    {
                        所有需要复制的文件.Add("Reunion Anti-Cheat.dll");
                    }
                }
                var keyboardMD = Path.Combine(ProgramConstants.GamePath, "KeyboardMD.ini");
                if (File.Exists(keyboardMD))
                    所有需要复制的文件.Add(keyboardMD);

                if (newSection.KeyExists("CampaignID") && newSection.GetValue("chkSatellite", false))
                {
                    所有需要复制的文件.Add(Path.Combine(ProgramConstants.GamePath, "Resources\\shroud.shp"));
                }

                var e = string.Empty;

                if (IsNtfs(ProgramConstants.GamePath))
                {
                  e = 符号链接(所有需要复制的文件, newMission, ["syringe.exe","gamemd-spawn.exe"]);
                }
                else
                {
                    e = 复制文件(所有需要复制的文件);
                }

                if (e != string.Empty)
                {
                    XNAMessageBox.Show(windowManager, "错误", e);
                    return false;
                }

                    复制CSF(newGame);
                if (newMission != newGame && newMission != string.Empty)
                        复制CSF(newMission);
                if(otherFile != string.Empty)
                    复制CSF(otherFile);

                iniFile.WriteIniFile(spawnerSettingsFile.FullName);

                if (!File.Exists(Path.Combine(ProgramConstants.游戏目录, "thememd.mix")) && !File.Exists(Path.Combine(ProgramConstants.游戏目录, "thememd.ini")))
                {
                    WindowManager.progress.Report("正在加载音乐");
                    加载音乐(ProgramConstants.游戏目录);
                }

                if (!Directory.Exists(Path.Combine(ProgramConstants.游戏目录, "Saved Games")))
                    Directory.CreateDirectory(Path.Combine(ProgramConstants.游戏目录, "Saved Games"));

                var ra2md = Path.Combine(ProgramConstants.游戏目录, mod.SettingsFile);


                if (File.Exists(ra2md))
                {
                    var ra2mdIni = new IniFile(ra2md);
                    IniFile.ConsolidateIniFiles(ra2mdIni, new IniFile("RA2MD.ini"));
                    ra2mdIni.WriteIniFile();
                }
                else
                {
                    File.Copy("RA2MD.ini", ra2md,true);
                }

                File.Copy("spawn.ini", Path.Combine(ProgramConstants.游戏目录, "spawn.ini"), true);
                if (File.Exists("spawnmap.ini"))
                    File.Copy("spawnmap.ini", Path.Combine(ProgramConstants.游戏目录, "spawnmap.ini"), true);

                try
                {
                  
                    if (File.Exists("Run\\WSOCK32.DLL"))
                            File.Delete("Run\\WSOCK32.DLL");
                }
                catch
                {

                }

                // 加载渲染插件
                var p = Path.Combine(ProgramConstants.GamePath, "Resources\\Render", UserINISettings.Instance.Renderer.Value);
                if (Directory.Exists(p))
                    foreach (var file in Directory.GetFiles(p))
                    {
                        var targetFileName = Path.Combine(ProgramConstants.游戏目录, "ddraw" + Path.GetExtension(file));
                        File.Copy(file, targetFileName,true);
                    }

                return true;
            }

            catch (FileLockedException ex)
            {
                //  XNAMessageBox.Show(windowManager, "错误", ex.Message);
                Logger.Log(ex.Message);
                return false;
            }
        }

        private static string 复制文件(List<string> 所有需要复制的文件)
        {
            Dictionary<string, string> 文件字典 = [];

            try
            {
                foreach (var path in 所有需要复制的文件)
                {
                    if (File.Exists(path))
                    {
                        // 文件：使用文件名作为 key，保留最后一个相同文件名的路径
                        string fileName = Path.GetFileName(path);
                        文件字典[fileName] = path;
                    }
                    else if (Directory.Exists(path))
                    {
                        // 文件夹：递归获取其中的所有文件，加入字典
                        var filesInDir = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                        foreach (var file in filesInDir)
                        {
                            string relativePath = Path.GetRelativePath(path, file);
                            string targetPath = relativePath;
                            文件字典[targetPath] = file;
                        }
                    }
                }

                var 去重后的文件列表 = 文件字典.ToList();

                // 清理之前目标目录中已有的目标路径文件
                ProgramConstants.清理游戏目录(去重后的文件列表.Select(kv => Path.Combine(ProgramConstants.游戏目录, kv.Key)).ToList());

                foreach (var kv in 去重后的文件列表)
                {
                    string targetPath = Path.Combine(ProgramConstants.游戏目录, kv.Key);
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                    FileHelper.CopyFile(kv.Value, targetPath);
                }
            }
            catch (Exception ex)
            {
                return $"复制文件失败：{ex.Message}";
            }
            return string.Empty;
        }

        private static string 符号链接(List<string> 所有需要链接的文件, string 存档目标, List<string> 白名单)
        {
            Dictionary<string, string> 文件字典 = new();

            try
            {
                foreach (var path in 所有需要链接的文件)
                {
                    if (File.Exists(path))
                    {
                        // 跳过 .csf 文件
                        if (Path.GetExtension(path).Equals(".csf", StringComparison.OrdinalIgnoreCase))
                            continue;

                        string fileName = Path.GetFileName(path);
                        文件字典[fileName] = path;
                    }
                    else if (Directory.Exists(path))
                    {
                        var filesInDir = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                        foreach (var file in filesInDir)
                        {
                            // 跳过 .csf 文件
                            if (Path.GetExtension(file).Equals(".csf", StringComparison.OrdinalIgnoreCase))
                                continue;

                            string relativePath = Path.GetRelativePath(path, file);
                            文件字典[relativePath] = file;
                        }
                    }
                }

                var 去重后的文件列表 = 文件字典.ToList();

                // 清理之前目标目录中已有的目标路径文件
                ProgramConstants.清理游戏目录(
                    去重后的文件列表.Select(kv => Path.Combine(ProgramConstants.游戏目录, kv.Key)).ToList()
                );

                foreach (var kv in 去重后的文件列表)
                {
                    string targetPath = Path.Combine(ProgramConstants.游戏目录, kv.Key);
                    string sourcePath = Path.Combine(ProgramConstants.GamePath, kv.Value);

                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

                    if (File.Exists(targetPath))
                        File.Delete(targetPath);

                    // 白名单判断只用文件名
                    string fileName = Path.GetFileName(kv.Key);
                    if (白名单.Any(w => string.Equals(w, fileName, StringComparison.OrdinalIgnoreCase)))
                    {
                        FileHelper.CopyFile(sourcePath, targetPath);
                    }
                    else
                    {
                        File.CreateSymbolicLink(targetPath, sourcePath);
                    }
                }

                //if (!string.IsNullOrEmpty(存档目标))

                //{
                if (存档目标 == string.Empty) 存档目标 = "Other";

                    var 目标文件夹 = Path.Combine(ProgramConstants.存档目录, Path.GetFileName(存档目标));
                    if (!Directory.Exists(目标文件夹)) return string.Empty;
                    var sourceFiles = Directory.GetFiles(目标文件夹, "*.sav", SearchOption.AllDirectories);
                    var linkDirectory = ProgramConstants.存档目录;

                    foreach (var sourcePath in sourceFiles)
                    {
                        var fileName = Path.GetFileName(sourcePath);
                        var targetPath = Path.Combine(linkDirectory, fileName);

                        if (File.Exists(targetPath))
                        {
                            File.Delete(targetPath);
                        }

                        FileHelper.CopyFile(sourcePath, targetPath);
                    }
              //  }
            }
            catch (Exception ex)
            {
                return $"符号链接失败，原因：{ex.Message}\n\n请尝试关闭杀毒软件并以管理员身份运行(Symlink需要管理员权限), 若仍未解决请到交流群询问管理员";
            }

            return string.Empty;
        }


        private static void 复制CSF(string path)
        {
            var csfs = Directory.GetFiles(path, "*.csf").OrderBy(f => f); // 按文件名升序处理   .ToArray();
            foreach (var csf in csfs)
            {
                var tagCsf = Path.GetFileName(csf).ToLower();
                if (tagCsf == "ra2.csf")
                {
                    tagCsf = "ra2md.csf";
                }
                if (UserINISettings.Instance.SimplifiedCSF.Value)
                    CSF.将繁体的CSF转化为简体CSF(csf, Path.Combine(ProgramConstants.游戏目录, tagCsf));
                else
                    File.Copy(csf, Path.Combine(ProgramConstants.游戏目录, tagCsf),true);
            }
        }

        private static void Process_Exited(object sender, EventArgs e)
        {
            Process proc = (Process)sender;

            WindowManager.progress.Report(string.Empty);
            Logger.Log("GameProcessLogic: Process exited.");

            //游戏中 = false;
            proc.Exited -= Process_Exited;
            proc.Dispose();
            ShiftClickAutoClicker.Instance.Stop();
            GameProcessExited?.Invoke();
     

            var RA2MD = Path.Combine(ProgramConstants.游戏目录, mod.SettingsFile);
            if (File.Exists(RA2MD))
                File.Copy(RA2MD, "RA2MD.ini", true);
            获取新的存档();
            
            if ( Directory.Exists(Path.Combine(ProgramConstants.游戏目录, "Debug")) && DebugCount < Directory.GetDirectories(Path.Combine(ProgramConstants.游戏目录, "Debug")).Length)
            {
                ProgramConstants.清理缓存();
            }
            Task.Run(RenderImage.RenderImages);
        }
    }
}
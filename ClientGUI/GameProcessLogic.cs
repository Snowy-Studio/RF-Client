﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using ClientCore;
using ClientCore.INIProcessing;
using DTAConfig.Entity;
using Localization.Tools;
using Rampastring.Tools;
using Rampastring.XNAUI;
using SharpDX.XAudio2;

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

        public static bool UseQres { get; set; }
        public static bool SingleCoreAffinity { get; set; }

        private static string gameExecutableName;

        private static string[] oldSaves;

        private static Mod mod;

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
                var settings = iniFile.GetSection("Settings");
                string r = 加载模组文件(settings);

                mod = Mod.Mods.Find(m => m.FilePath == settings.GetValue("Game", string.Empty));


                if (r != string.Empty)
                {
                    if (r == "尤复目录必须为纯净尤复目录")
                    {
                        var guideWindow = new YRPathWindow(windowManager);
                        guideWindow.Show();
                    }
                    else
                        XNAMessageBox.Show(windowManager, "错误", r);
                    return;
                }

            if (settings.KeyExists("CampaignID") && settings.GetValue("chkSatellite", false))
            {
                FileHelper.CopyFile(Path.Combine(ProgramConstants.GamePath, "Resources\\shroud.shp"), Path.Combine(ProgramConstants.游戏目录, "shroud.shp"));
            }
            else
            {
                File.Delete(Path.Combine(ProgramConstants.游戏目录, "shroud.shp"));
            }

            spawnerSettingsFile.Delete();
                iniFile.WriteIniFile(spawnerSettingsFile.FullName);

                if (!File.Exists(Path.Combine(ProgramConstants.游戏目录, "thememd.mix")) && !File.Exists(Path.Combine(ProgramConstants.游戏目录, "thememd.ini")))
                {
                    WindowManager.progress.Report("正在加载音乐");
                    加载音乐(ProgramConstants.游戏目录);
                }

                if(Directory.Exists(Path.Combine(ProgramConstants.游戏目录, "Saved Games")))
                {
                    FileHelper.ForceDeleteDirectory(Path.Combine(ProgramConstants.游戏目录, "Saved Games"));
                }
                FileHelper.CopyDirectory("Saved Games", Path.Combine(ProgramConstants.游戏目录, "Saved Games"));

                var ra2md = Path.Combine(ProgramConstants.游戏目录, mod.SettingsFile);


                if (File.Exists(ra2md))
                {
                    var ra2mdIni = new IniFile(ra2md);
                    IniFile.ConsolidateIniFiles(ra2mdIni, new IniFile("RA2MD.ini"));
                    ra2mdIni.WriteIniFile();
                }
                else
                {
                    File.Copy("RA2MD.ini", ra2md, true);
                }

                File.Copy("spawn.ini", Path.Combine(ProgramConstants.游戏目录, "spawn.ini"), true);

                var keyboardMD = Path.Combine(ProgramConstants.GamePath, "KeyboardMD.ini");
                if (File.Exists(keyboardMD))
                    File.Copy("KeyboardMD.ini", Path.Combine(ProgramConstants.游戏目录, "KeyboardMD.ini"), true);
                if (File.Exists("spawnmap.ini"))
                    File.Copy("spawnmap.ini", Path.Combine(ProgramConstants.游戏目录, "spawnmap.ini"), true);

            // 加载渲染插件
            var p = Path.Combine(ProgramConstants.GamePath, "Resources\\Render", UserINISettings.Instance.Renderer.Value);
            if(Directory.Exists(p))
            foreach (var file in Directory.GetFiles(p))
            {
                var targetFileName = Path.Combine(ProgramConstants.游戏目录, "ddraw" + Path.GetExtension(file));
                FileHelper.CopyFile(file, targetFileName, true);
            }

#if !DEBUG
            }
            catch(Exception ex)
            {
                XNAMessageBox.Show(windowManager,"错误",$"出现错误:{ex}");
                return;
            }
#endif
            oldSaves = Directory.GetFiles($"{ProgramConstants.GamePath}Saved Games");

            WindowManager.progress.Report("正在唤起游戏");

            Logger.Log("About to launch main game executable.");

            int waitTimes = 0;
            while (PreprocessorBackgroundTask.Instance.IsRunning)
            {
                Thread.Sleep(1000);
                waitTimes++;
                if (waitTimes > 10)
                {
                    XNAMessageBox.Show(windowManager, "INI preprocessing not complete", "INI preprocessing not complete. Please try " +
                        "launching the game again. If the problem persists, " +
                        "contact the game or mod authors for support.");
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
           // else
            //{
                string arguments;

                if (!string.IsNullOrWhiteSpace(extraCommandLine))
                    arguments = " " + additionalExecutableName + "-SPAWN " + extraCommandLine;
                else
                    arguments = additionalExecutableName + "-SPAWN";

                if (File.Exists(Path.Combine(ProgramConstants.游戏目录, "syringe.exe")))
                {
                    gameExecutableName = "Syringe.exe";
                    arguments = "\"gamemd.exe\" -SPAWN " + extraCommandLine;
                }
                //else if (File.Exists("NPatch.mix"))
                //{
                //    gameExecutableName = "gamemd-np.exe";
                //    arguments = "-SPAWN " + extraCommandLine;
                //}
                else
                {
                    gameExecutableName = "gamemd-spawn.exe";
                    arguments = "-SPAWN " + extraCommandLine;
                }

                FileInfo gameFileInfo = SafePath.GetFile(ProgramConstants.游戏目录, gameExecutableName);
                if (!File.Exists(gameFileInfo.FullName))
                {
                    XNAMessageBox.Show(windowManager, "错误", $"{gameFileInfo.FullName}不存在，请前往设置清理游戏缓存后重试。");
                    return;
                }

                ProcessStartInfo info = new ProcessStartInfo(gameFileInfo.FullName, arguments)
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


                try
                {
                    gameProcess.Start();
                    WindowManager.progress.Report("游戏进行中....");
                    Logger.Log("游戏处理逻辑: 进程开始.");
                }
                catch (Exception ex)
                {
                    Logger.Log("Error launching " + gameFileInfo.Name + ": " + ex.Message);
                    XNAMessageBox.Show(windowManager, "Error launching game", "Error launching " + gameFileInfo.Name + ". Please check that your anti-virus isn't blocking the CnCNet Client. " +
                        "You can also try running the client as an administrator." + Environment.NewLine + Environment.NewLine + "You are unable to participate in this match." +
                        Environment.NewLine + Environment.NewLine + "Returned error: " + ex.Message);
                    Process_Exited(gameProcess, EventArgs.Empty);
                    return;
                }  

          //  }

            GameProcessStarted?.Invoke();

            Logger.Log("等待 qres.dat 或 " + gameExecutableName + " 退出.");
        }

        static readonly FileInfo spawnerSettingsFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SPAWNER_SETTINGS);
        private static void 加载音乐(string modPath)
        {
            Mix.PackToMix($"{ProgramConstants.GamePath}Resources/thememd/", Path.Combine(ProgramConstants.游戏目录, "thememd.mix"));
            File.Copy($"{ProgramConstants.GamePath}Resources/thememd/thememd.ini", Path.Combine(ProgramConstants.游戏目录, "thememd.ini"),true);
            var csfPath = Path.Combine(modPath, "ra2md.csf");
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
            var newSaves = Directory.GetFiles($"{ProgramConstants.GamePath}Saved Games");

            if (oldSaves.Length < newSaves.Length)
            {

                var iniFile = new IniFile($"{ProgramConstants.GamePath}Saved Games/Save.ini");
                var spawn = new IniFile(Path.Combine(ProgramConstants.GamePath, "spawn.ini"));
                var game = spawn.GetValue("Settings", "Game", string.Empty);

                var mission = spawn.GetValue("Settings", "Mission", string.Empty);

               // var ra2Mode = spawn.GetValue("Settings", "RA2Mode", false);
                // 找到在 newSaves 中但不在 oldSaves 中的文件
                var addedFiles = newSaves.Where(newFile => !oldSaves.Contains(newFile)).ToArray();

                foreach (var fileFullPath in addedFiles)
                {
                    string fileName = Path.GetFileName(fileFullPath);

                    iniFile.SetValue(fileName, "Game", game);
                    iniFile.SetValue(fileName, "Mission", mission);
                }
                iniFile.WriteIniFile();
            }
        }
        public static string 加载模组文件(IniSection newSection)
        {

            FileHelper.KillGameMdProcesses();
            string newGame = newSection.GetValue("Game", string.Empty);
            string newMission = newSection.GetValue("Mission", string.Empty);


            var oldSettings = new IniFile(spawnerSettingsFile.FullName);

            var oldSection = oldSettings.GetSection("Settings");


            string oldGame = string.Empty;
            string oldMission = string.Empty;


            if (oldSection != null)
            {
                oldGame = oldSection.GetValue("Game", string.Empty);
                oldMission = oldSection.GetValue("Mission", string.Empty);
            }


            bool 是否修改()
            {
                if (!Directory.Exists(ProgramConstants.游戏目录)) return true;

                if (!oldSettings.SectionExists("Settings")) return true;
                


                if (oldGame != newGame || oldMission != newMission) return true;

                var newGameFiles = Directory.GetFiles(newGame);
                foreach (var newGameFile in newGameFiles)
                {
                    var fileName = Path.GetFileName(newGameFile);
                    var gameFile = Path.Combine(ProgramConstants.游戏目录, fileName);

                    // 如果目标文件存在并且修改时间一致，跳过
                    if (File.Exists(gameFile) && File.GetLastWriteTime(newGameFile) == File.GetLastWriteTime(gameFile))
                    {
                        continue;
                    }

                    // 如果目标文件不存在或修改时间不一致，进行复制
                    return true;
                }


                return false;
            }

            if (是否修改())
            {
                try
                {
                    if (Directory.Exists(ProgramConstants.游戏目录))
                    {
                        ProgramConstants.清理游戏目录();
                    }
                        

                    if (!ProgramConstants.判断目录是否为纯净尤复(UserINISettings.Instance.YRPath))
                    {
                        return "尤复目录必须为纯净尤复目录";
                    }

                    Directory.CreateDirectory(ProgramConstants.游戏目录);

                    WindowManager.progress.Report("正在加载游戏文件");


                    foreach (var file in ProgramConstants.PureHashes.Keys)
                    {
                        var newFile = Path.Combine(ProgramConstants.游戏目录, Path.GetFileName(file));
                        var sourceFile = Path.Combine(UserINISettings.Instance.YRPath, Path.GetFileName(file));

                        // 检查目标文件是否存在，并且源文件比目标文件更新
                        if (File.Exists(newFile) && File.GetLastWriteTime(newFile) >= File.GetLastWriteTime(sourceFile))
                            continue;

                        // 如果源文件更新或目标文件不存在，进行复制
                        File.Copy(sourceFile, newFile, true);
                    }

                    if (Directory.Exists("TX"))
                        FileHelper.CopyDirectory("TX", ProgramConstants.游戏目录);
                    if(Directory.Exists("zh"))
                        FileHelper.CopyDirectory("zh", ProgramConstants.游戏目录);


                    FileHelper.CopyFile("gamemd-spawn.exe", Path.Combine(ProgramConstants.游戏目录, "gamemd-spawn.exe"), true);
                    FileHelper.CopyFile("cncnet5.dll", Path.Combine(ProgramConstants.游戏目录, "cncnet5.dll"), true);
                    // 加载模组
                    FileHelper.CopyDirectory(newGame, ProgramConstants.游戏目录,killProcesses:true);

                    void 复制CSF(string path,string tag,List<string> excludes)
                    {
                        var csfs = Directory.GetFiles(path, "*.csf").OrderBy(f => f); // 按文件名升序处理                                       .ToArray();
                        foreach (var csf in csfs)
                        {
                            var tagCsf = Path.GetFileName(csf).ToLower();
                            if (tagCsf == "ra2.csf")
                            {
                                tagCsf = "ra2md.csf";
                            }
                            if (path.Contains(tag) && UserINISettings.Instance.SimplifiedCSF.Value)
                                CSF.将繁体的CSF转化为简体CSF(csf, Path.Combine(ProgramConstants.游戏目录, tagCsf));
                            else
                                File.Copy(csf, Path.Combine(ProgramConstants.游戏目录, tagCsf), true);
                        }
                        FileHelper.CopyDirectory(path, ProgramConstants.游戏目录, excludes);
                    }

                    复制CSF(newGame, "Mod&AI", [".csf"]);

                    // 加载任务
                    if (newMission != newGame && newMission != string.Empty)
                    {
                        复制CSF(newMission, "Maps\\CP", [".map", ".csf"]); //地图文件也不复制在后面处理
                       
                    }

                    // 加载战役图
                    if (newSection.KeyExists("CampaignID"))
                    {
                        var 战役临时目录 = SafePath.CombineFilePath(ProgramConstants.GamePath, "Resources\\MissionCache\\");
                        FileHelper.CopyDirectory(战役临时目录, ProgramConstants.游戏目录);
                        //if (newSection.GetValue("chkSatellite",false))
                        //{
                        //    FileHelper.CopyFile(Path.Combine(ProgramConstants.GamePath, "Resources\\shroud.shp"), Path.Combine(ProgramConstants.游戏目录, "shroud.shp"));
                        //}
                    }

                    File.Copy("LiteExt.dll", Path.Combine(ProgramConstants.游戏目录, "LiteExt.dll"), true);
                    File.Copy("qres.dat", Path.Combine(ProgramConstants.游戏目录, "qres.dat"), true);
                    File.Copy("qres32.dll", Path.Combine(ProgramConstants.游戏目录, "qres32.dll"), true);

                    WindowManager.progress.Report("正在加载语音");
                    FileHelper.CopyDirectory($"Resources/Voice/{UserINISettings.Instance.Voice.Value}", ProgramConstants.游戏目录);

                    return string.Empty;
                }

                catch (FileLockedException ex)
                {
                    //  XNAMessageBox.Show(windowManager, "错误", ex.Message);
                    Logger.Log(ex.Message);
                    return ex.Message;
                }

            }
            return string.Empty;
        }
        private static void Process_Exited(object sender, EventArgs e)
        {
            Process proc = (Process)sender;

            WindowManager.progress.Report(string.Empty);
            Logger.Log("GameProcessLogic: Process exited.");
            

            proc.Exited -= Process_Exited;
            proc.Dispose();
            GameProcessExited?.Invoke();
            var keyboardMD = Path.Combine(ProgramConstants.游戏目录, "KeyboardMD.ini");
            if (File.Exists(keyboardMD))
                File.Copy(keyboardMD, "KeyboardMD.ini", true);

            var RA2MD = Path.Combine(ProgramConstants.游戏目录, mod.SettingsFile);
            if (File.Exists(RA2MD))
                File.Copy(RA2MD, "RA2MD.ini", true);
            FileHelper.CopyDirectory(Path.Combine(ProgramConstants.游戏目录, "Saved Games"),"Saved Games");
            FileHelper.CopyDirectory(Path.Combine(ProgramConstants.游戏目录, "Debug"), "Debug");
            获取新的存档();

            RenderImage.RenderImages();
        }

    }
}
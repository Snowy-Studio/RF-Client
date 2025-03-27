﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using ClientCore;
using ClientCore.INIProcessing;
using CNCMaps.FileFormats;
using DTAConfig.Entity;
using Localization.Tools;
using Rampastring.Tools;
using Rampastring.XNAUI;
using SharpDX.XAudio2;
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

        public static bool UseQres { get; set; }
        public static bool SingleCoreAffinity { get; set; }

        private static string gameExecutableName;

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


                DebugCount = Directory.GetDirectories(Path.Combine(ProgramConstants.游戏目录,"Debug")).Count();
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
            FileHelper.CopyFile($"{ProgramConstants.GamePath}Resources/thememd/thememd.ini", Path.Combine(ProgramConstants.游戏目录, "thememd.ini"),true);
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
            var newSaves = Directory.GetFiles(ProgramConstants.存档目录);
            if (newSaves.Length == 0) return;

            var iniFile = new IniFile(Path.Combine(ProgramConstants.存档目录, "Save.ini"));
            var spawn = new IniFile(Path.Combine(ProgramConstants.GamePath, "spawn.ini"));
            var game = spawn.GetValue("Settings", "Game", string.Empty);

            var mission = spawn.GetValue("Settings", "Mission", "Other");


            var missionSavePath = Path.Combine(ProgramConstants.存档目录, Path.GetFileName(mission));
            if (!Directory.Exists(missionSavePath))
                Directory.CreateDirectory(missionSavePath);

            foreach (var item in newSaves)
            {
                File.Copy(item,Path.Combine(missionSavePath,Path.GetFileName(item)), true);
            }

            foreach (var fileFullPath in newSaves)
            {
                string sectionName = Path.GetFileName(fileFullPath) + '-' + mission;

                iniFile.SetValue(sectionName, "Game", game);
                iniFile.SetValue(sectionName, "Mission", mission);
            }
            iniFile.WriteIniFile();
            
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
            ;

            FileHelper.KillGameMdProcesses();
            string newGame = newSection.GetValue("Game", string.Empty);
            string newMission = newSection.GetValue("Mission", string.Empty);

          
            try
            {   List<string> 所有需要复制的文件 = [];
                    
                void 添加需要复制的文件夹(string folderPath)
                {
                    if (!Directory.Exists(folderPath)) return;
                    Directory.GetFiles(folderPath).ToList().ForEach(所有需要复制的文件.Add);
                }

                if(!Directory.Exists(ProgramConstants.游戏目录))
                    Directory.CreateDirectory(ProgramConstants.游戏目录);

                    WindowManager.progress.Report("正在加载游戏文件");

                foreach (var file in ProgramConstants.PureHashes.Keys)
                    {
                        var newFile = Path.Combine(ProgramConstants.游戏目录, Path.GetFileName(file));
                        var sourceFile = Path.Combine(UserINISettings.Instance.YRPath, Path.GetFileName(file));

                    所有需要复制的文件.Add(sourceFile);
                }

                添加需要复制的文件夹("TX");
                添加需要复制的文件夹("zh");
                所有需要复制的文件.Add("gamemd-spawn.exe");
                所有需要复制的文件.Add("cncnet5.dll");
                添加需要复制的文件夹(newGame);
                if(newMission != newGame && newMission != string.Empty)
                    添加需要复制的文件夹(newMission);

                if (newSection.KeyExists("CampaignID"))
                {
                    添加需要复制的文件夹(SafePath.CombineFilePath(ProgramConstants.GamePath, "Resources\\MissionCache\\"));
                }

                所有需要复制的文件.Add("LiteExt.dll");
                所有需要复制的文件.Add("qres.dat");
                所有需要复制的文件.Add("qres32.dll");
                添加需要复制的文件夹($"Resources/Voice/{UserINISettings.Instance.Voice.Value}");

                所有需要复制的文件.Add("spawn.ini");

                var keyboardMD = Path.Combine(ProgramConstants.GamePath, "KeyboardMD.ini");
                if (File.Exists(keyboardMD))
                    所有需要复制的文件.Add(keyboardMD);

                if (File.Exists("spawnmap.ini"))
                    所有需要复制的文件.Add("spawnmap.ini");

                复制文件(所有需要复制的文件);

                复制CSF(newGame);
                if (newMission != newGame && newMission != string.Empty)
                    复制CSF(newMission);

                if (newSection.KeyExists("CampaignID") && newSection.GetValue("chkSatellite", false))
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

                var ra2md = Path.Combine(ProgramConstants.游戏目录, mod.SettingsFile);


                if (File.Exists(ra2md))
                {
                    var ra2mdIni = new IniFile(ra2md);
                    IniFile.ConsolidateIniFiles(ra2mdIni, new IniFile("RA2MD.ini"));
                    ra2mdIni.WriteIniFile();
                }
                else
                {
                    FileHelper.CopyFile("RA2MD.ini", ra2md);
                }

                // 加载渲染插件
                var p = Path.Combine(ProgramConstants.GamePath, "Resources\\Render", UserINISettings.Instance.Renderer.Value);
                if (Directory.Exists(p))
                    foreach (var file in Directory.GetFiles(p))
                    {
                        var targetFileName = Path.Combine(ProgramConstants.游戏目录, "ddraw" + Path.GetExtension(file));
                        FileHelper.CopyFile(file, targetFileName);
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

        private static void 复制文件(List<string> 所有需要复制的文件)
        {
            Dictionary<string, string> 文件字典 = [];

            // 只保留最后一个相同文件名的路径
            foreach (var filePath in 所有需要复制的文件)
            {
                string fileName = Path.GetFileName(filePath);
                文件字典[fileName] = filePath; // 若文件名重复，后来的会覆盖前面的
            }

            var 去重后的文件列表 = 文件字典.Values.ToList();
            去重后的文件列表.ForEach(file =>
            {
                FileHelper.CopyFile(file,Path.Combine( ProgramConstants.游戏目录,Path.GetFileName(file)));
            });
        }

        private static void 复制CSF(string path)
        {
            var csfs = Directory.GetFiles(path, "*.csf").OrderBy(f => f); // 按文件名升序处理                                       .ToArray();
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
                    FileHelper.CopyFile(csf, Path.Combine(ProgramConstants.游戏目录, tagCsf));
            }
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
            获取新的存档();
            if (DebugCount < Directory.GetDirectories(Path.Combine(ProgramConstants.游戏目录, "Debug")).Length)
            {
                ProgramConstants.清理缓存();
            }
                RenderImage.RenderImages();
        }

    }
}
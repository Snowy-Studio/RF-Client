using System;
using System.Diagnostics;
using System.DirectoryServices;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.INIProcessing;
using ClientCore.Settings;
using Ra2Client.Domain;
using Ra2Client.DXGUI;
using Ra2Client.Online;
using Microsoft.Win32;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using DTAConfig.OptionPanels;
using ClientGUI;


namespace Ra2Client
{
    /// <summary>
    /// A class that handles initialization of the Client.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// The main method for startup and initialization.
        /// </summary>
        public void Execute()
        {
            if (File.Exists(Path.Combine(ProgramConstants.GamePath, "update.bat")))
            {
                Process process = new Process();

                // 配置进程启动信息
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(ProgramConstants.GamePath, "update.bat"), // 指定要运行的程序路径
                    UseShellExecute = true, // 使用系统外壳程序启动
                    Verb = "open" // 使用 "open" 命令打开程序（如果支持的话）
                };

                // 将启动信息分配给进程
                process.StartInfo = startInfo;

                // 启动进程
                process.Start();
                Environment.Exit(0);
            }

            UpdaterDel();

            string themePath = UserINISettings.Instance.ClientTheme;

            themePath ??= ClientConfiguration.Instance.GetThemeInfoFromIndex(1)[1];


            ProgramConstants.RESOURCES_DIR = SafePath.CombineDirectoryPath(ProgramConstants.BASE_RESOURCE_PATH, themePath);

            DirectoryInfo resourcesDirectory = SafePath.GetDirectory(ProgramConstants.GetResourcePath());

            if (!resourcesDirectory.Exists)
                throw new DirectoryNotFoundException("Theme directory not found!" + Environment.NewLine + ProgramConstants.RESOURCES_DIR);

            Logger.Log("初始化更新器.");

            SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "version_u");

            Logger.Log("操作系统: " + RuntimeInformation.OSDescription);
            Logger.Log("操作系统架构: " + RuntimeInformation.OSArchitecture);
            Logger.Log("进程架构: " + RuntimeInformation.ProcessArchitecture);
            Logger.Log("运行框架: " + RuntimeInformation.FrameworkDescription);
            Logger.Log("运行环境: " + RuntimeInformation.RuntimeIdentifier);
            Logger.Log("当前操作系统: " + MainClientConstants.OSId);
            Logger.Log("系统语言: " + CultureInfo.CurrentCulture);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // The query in CheckSystemSpecifications takes lots of time,
                // so we'll do it in a separate thread to make startup faster
                Thread thread = new Thread(CheckSystemSpecifications);
                thread.Start();
            }

            Task.Run(() => GenerateOnlineIdAsync());

            Task.Factory.StartNew(() => PruneFiles(SafePath.GetDirectory(ProgramConstants.GamePath, "Debug"), DateTime.Now.AddDays(-7)));
            Task.Factory.StartNew(MigrateOldLogFiles);

            DirectoryInfo updaterFolder = SafePath.GetDirectory(ProgramConstants.GamePath, "Updater");

            if (updaterFolder.Exists)
            {
                Logger.Log("Attempting to delete temporary updater directory.");
                try
                {
                    updaterFolder.Delete(true);
                }
                catch
                {
                }
            }

            //if (ClientConfiguration.Instance.CreateSavedGamesDirectory)
            //{
            //    DirectoryInfo savedGamesFolder = SafePath.GetDirectory(SavedGameManager.GetSaveGameDirectoryPath(), "Saved Games");

            //    if (!savedGamesFolder.Exists)
            //    {
            //        Logger.Log("Saved Games directory does not exist - attempting to create one.");
            //        try
            //        {
            //            savedGamesFolder.Create();
            //        }
            //        catch
            //        {
            //        }
            //    }
            //}

            // FinalSunSettings.WriteFinalSunIni();

            //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            //    WriteInstallPathToRegistry();

            ClientConfiguration.Instance.RefreshSettings();

            // Start INI file preprocessor
            // PreprocessorBackgroundTask.Instance.Run();

            var gameClass = new GameClass();
            // UserINISettings.标题改变 += gameClass.ChangeTiTle;

            //gameClass.Exiting += (sender, e) =>
            //{
            //    LocalHttpServer.Stop();
            //    ShiftClickAutoClicker.Instance.Stop();

            //};

         

            int currentWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            int currentHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

            UserINISettings.Instance.ClientResolutionX = new IntSetting(UserINISettings.Instance.SettingsIni, UserINISettings.VIDEO, "ClientResolutionX", currentWidth);
            UserINISettings.Instance.ClientResolutionY = new IntSetting(UserINISettings.Instance.SettingsIni, UserINISettings.VIDEO, "ClientResolutionY", currentHeight);

            try
            {
                gameClass.Run();
            }
            catch (Exception ex)
            {
               
                LocalHttpServer.Stop();
                ShiftClickAutoClicker.Instance.Stop();
                PreStartup.LogException(ex);
                CDebugView.OutputDebugInfo(ex.Message);
            }
        }

        private void UpdaterDel()
        {
            // 检查是否存在名为 "del" 的文件
            if (!File.Exists("del")) return;

            // 逐行读取 "del" 文件中的路径并删除对应的文件或文件夹
            using (var reader = new StreamReader("del"))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    try
                    {
                        // 判断路径是文件还是文件夹
                        if (File.Exists(line))
                        {
                            // 如果是文件，则直接删除
                            File.Delete(line);
                        }
                        else if (Directory.Exists(line))
                        {
                            // 如果是文件夹，则递归删除
                            Directory.Delete(line, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }

            // 删除完成后，删除 "del" 文件本身
            File.Delete("del");

          //  ProgramConstants.清理缓存();
        }

        /// <summary>
        /// Recursively deletes all files from the specified directory that were created at <paramref name="pruneThresholdTime"/> or before.
        /// If directory is empty after deleting files, the directory itself will also be deleted.
        /// </summary>
        /// <param name="directory">Directory to prune files from.</param>
        /// <param name="pruneThresholdTime">Time at or before which files must have been created for them to be pruned.</param>
        private void PruneFiles(DirectoryInfo directory, DateTime pruneThresholdTime)
        {
            if (!directory.Exists)
                return;

            try
            {
                foreach (FileSystemInfo fsEntry in directory.EnumerateFileSystemInfos())
                {
                    if ((fsEntry.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                        PruneFiles(new DirectoryInfo(fsEntry.FullName), pruneThresholdTime);
                    else
                    {
                        try
                        {
                            FileInfo fileInfo = new FileInfo(fsEntry.FullName);
                            if (fileInfo.CreationTime <= pruneThresholdTime)
                                fileInfo.Delete();
                        }
                        catch (Exception e)
                        {
                            Logger.Log("PruneFiles: Could not delete file " + fsEntry.Name +
                                ". Error message: " + e.Message);
                            continue;
                        }
                    }
                }

                if (!directory.EnumerateFileSystemInfos().Any())
                    directory.Delete();
            }
            catch (Exception ex)
            {
                Logger.Log("PruneFiles: An error occurred while pruning files from " +
                   directory.Name + ". message: " + ex.Message);
            }
        }

        /// <summary>
        /// Move log files from obsolete directories to currently used ones and adjust filenames to match currently used timestamp scheme.
        /// </summary>
        private void MigrateOldLogFiles()
        {
            MigrateLogFiles(SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath, "ClientCrashLogs"), "ClientCrashLog*.txt");
            MigrateLogFiles(SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath, "GameCrashLogs"), "EXCEPT*.txt");
            MigrateLogFiles(SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath, "SyncErrorLogs"), "SYNC*.txt");
        }

        /// <summary>
        /// Move log files matching given search pattern from specified directory to another one and adjust filename timestamps.
        /// </summary>
        /// <param name="newDirectory">New log files directory.</param>
        /// <param name="searchPattern">Search string the log file names must match against to be copied. Can contain wildcard characters (* and ?) but doesn't support regular expressions.</param>
        private static void MigrateLogFiles(DirectoryInfo newDirectory, string searchPattern)
        {
            DirectoryInfo currentDirectory = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath, "ErrorLogs");
            try
            {
                if (!currentDirectory.Exists)
                    return;

                if (!newDirectory.Exists)
                    newDirectory.Create();

                foreach (FileInfo file in currentDirectory.EnumerateFiles(searchPattern))
                {
                    string filenameTS = Path.GetFileNameWithoutExtension(file.Name);
                    string[] ts = filenameTS.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);

                    string timestamp = string.Empty;
                    string baseFilename = Path.GetFileNameWithoutExtension(ts[0]);

                    if (ts.Length >= 6)
                    {
                        timestamp = string.Format("_{0}_{1}_{2}_{3}_{4}",
                            ts[3], ts[2].PadLeft(2, '0'), ts[1].PadLeft(2, '0'), ts[4].PadLeft(2, '0'), ts[5].PadLeft(2, '0'));
                    }

                    string newFilename = SafePath.CombineFilePath(newDirectory.FullName, baseFilename, timestamp, file.Extension);
                    file.MoveTo(newFilename);
                }

                if (!currentDirectory.EnumerateFiles().Any())
                    currentDirectory.Delete();
            }
            catch (Exception ex)
            {
                Logger.Log("MigrateLogFiles: An error occured while moving log files from " +
                    currentDirectory.Name + " to " +
                    newDirectory.Name + ". message: " + ex.Message);
            }
        }

        /// <summary>
        /// Writes processor, graphics card and memory info to the log file.
        /// </summary>
        [SupportedOSPlatform("windows")]
        private static void CheckSystemSpecifications()
        {
            string cpu = string.Empty;
            string videoController = string.Empty;
            string memory = string.Empty;

            ManagementObjectSearcher searcher;

            try
            {
                searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");

                foreach (var proc in searcher.Get())
                {
                    cpu = cpu + proc["Name"].ToString().Trim() + " (" + proc["NumberOfCores"] + " cores) ";
                }

            }
            catch
            {
                cpu = "CPU info not found";
            }

            try
            {
                searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");

                foreach (ManagementObject mo in searcher.Get())
                {
                    var currentBitsPerPixel = mo.Properties["CurrentBitsPerPixel"];
                    var description = mo.Properties["Name"];
                    if (currentBitsPerPixel != null && description != null)
                    {
                        if (currentBitsPerPixel.Value != null)
                            videoController = videoController + "Video controller: " + description.Value.ToString().Trim() + " ";
                    }
                }
            }
            catch
            {
                cpu = "Video controller info not found";
            }

            try
            {
                searcher = new ManagementObjectSearcher("Select * From Win32_PhysicalMemory");
                ulong total = 0;

                foreach (ManagementObject ram in searcher.Get())
                {
                    total += Convert.ToUInt64(ram.GetPropertyValue("Capacity"));
                }

                if (total != 0)
                    memory = "Total physical memory: " + (total >= 1073741824 ? total / 1073741824 + "GB" : total / 1048576 + "MB");
            }
            catch
            {
                cpu = "Memory info not found";
            }

            Logger.Log(string.Format("硬件信息: {0} | {1} | {2}", cpu.Trim(), videoController.Trim(), memory));
        }

        /// <summary>
        /// Generate an ID for online play.
        /// </summary>
        private static async Task GenerateOnlineIdAsync()
        {
            try
                {
                    await Task.CompletedTask;
                    ManagementObjectCollection mbsList = null;
                    ManagementObjectSearcher mbs = new ManagementObjectSearcher("Select * From Win32_processor");
                    mbsList = mbs.Get();
                    string cpuid = "";

                    foreach (ManagementObject mo in mbsList)
                        cpuid = mo["ProcessorID"].ToString();

                    ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
                    var moc = mos.Get();
                    string mbid = "";

                    foreach (ManagementObject mo in moc)
                        mbid = (string)mo["SerialNumber"];

                    string sid = new SecurityIdentifier((byte[])new DirectoryEntry(string.Format("WinNT://{0},Computer", Environment.MachineName)).Children.Cast<DirectoryEntry>().First().InvokeGet("objectSID"), 0).AccountDomainSid.Value;

                    Connection.SetId(cpuid + mbid + sid);
                    using RegistryKey key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + ClientConfiguration.Instance.InstallationPathRegKey);
                    key.SetValue("Ident", cpuid + mbid + sid);
                }
                catch (Exception)
                {
                    Random rn = new Random();

                    using RegistryKey key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + ClientConfiguration.Instance.InstallationPathRegKey);
                    string str = rn.Next(Int32.MaxValue - 1).ToString();

                    try
                    {
                        Object o = key.GetValue("Ident");
                        if (o == null)
                            key.SetValue("Ident", str);
                        else
                            str = o.ToString();
                    }
                    catch { }

                    Connection.SetId(str);
                }
        }

        /// <summary>
        /// Writes the game installation path to the Windows registry.
        /// </summary>
        [SupportedOSPlatform("windows")]
        private static void WriteInstallPathToRegistry()
        {
            if (!UserINISettings.Instance.WritePathToRegistry)
            {
                Logger.Log("Skipping writing installation path to the Windows Registry because of INI setting.");
                return;
            }

            Logger.Log("将安装路径写入Windows注册表.");

            try
            {
                using RegistryKey key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + ClientConfiguration.Instance.InstallationPathRegKey);
                key.SetValue("InstallPath", ProgramConstants.GamePath);
            }
            catch
            {
                Logger.Log("Failed to write installation path to the Windows registry");
            }
        }
    }
}
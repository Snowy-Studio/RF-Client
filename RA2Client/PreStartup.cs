using System;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Forms;
using ClientCore;
using ClientCore.Settings;
using ClientGUI;
using DTAConfig.Entity;
using Localization;
using Ra2Client.Domain;
using Rampastring.Tools;

namespace Ra2Client
{
    /// <summary>
    /// Contains client startup parameters.
    /// </summary>
    struct StartupParams
    {
        public StartupParams(bool noAudio, bool multipleInstanceMode,
            List<string> unknownParams)
        {
            NoAudio = noAudio;
            MultipleInstanceMode = multipleInstanceMode;
            UnknownStartupParams = unknownParams;
        }

        public bool NoAudio { get; }
        public bool MultipleInstanceMode { get; }
        public List<string> UnknownStartupParams { get; }
    }

    static class PreStartup
    {
        /// <summary>
        /// Initializes various basic systems like the client's logger, 
        /// constants, and the general exception handler.
        /// Reads the user's settings from an INI file, 
        /// checks for necessary permissions and starts the client if
        /// everything goes as it should.
        /// </summary>
        /// <param name="parameters">The client's startup parameters.</param>
        public static void Initialize(StartupParams parameters)
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            Application.ThreadException += (sender, args) => HandleException(sender, args.Exception);
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => HandleException(sender, (Exception)args.ExceptionObject);

            //DirectoryInfo gameDirectory = SafePath.GetDirectory(ProgramConstants.GamePath);
            var gameDirectory = new DirectoryInfo(ProgramConstants.GamePath);
            Environment.CurrentDirectory = gameDirectory.FullName;

            //var clientUserFilesPath = new DirectoryInfo(ProgramConstants.ClientUserFilesPath);

            DirectoryInfo clientUserFilesDirectory = new DirectoryInfo(ProgramConstants.ClientUserFilesPath);
            if (!clientUserFilesDirectory.Exists)
                clientUserFilesDirectory.Create();

            FileInfo clientLogFile = SafePath.GetFile(clientUserFilesDirectory.FullName, "client.log");
            ProgramConstants.LogFileName = clientLogFile.FullName;

            if (clientLogFile.Exists)
            {
                // Copy client.log file as client_previous.log. Override client_previous.log if it exists.
                FileInfo clientPrevLogFile = SafePath.GetFile(clientUserFilesDirectory.FullName, "client_previous.log");
                if (clientPrevLogFile.Exists)
                    File.Delete(clientPrevLogFile.FullName);
                File.Move(clientLogFile.FullName, clientPrevLogFile.FullName);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                CheckPermissions();

            Logger.Initialize(clientUserFilesDirectory.FullName, clientLogFile.Name);
            Logger.WriteLogFile = true;

            MainClientConstants.Initialize();

            Updater.GameVersion = Assembly.GetAssembly(typeof(PreStartup)).GetName().Version.ToString();
            Updater.GamePath = ProgramConstants.GamePath;
            Updater.ResourcePath = ProgramConstants.GetBaseResourcePath();
            Logger.Log("\r\n;  ____                           _                     _     _                 __           _                         \r\n; |  _ \\    ___   _   _   _ __   (_)   ___    _ __     | |_  | |__     ___     / _|  _   _  | |_   _   _   _ __    ___ \r\n; | |_) |  / _ \\ | | | | | '_ \\  | |  / _ \\  | '_ \\    | __| | '_ \\   / _ \\   | |_  | | | | | __| | | | | | '__|  / _ \\\r\n; |  _ <  |  __/ | |_| | | | | | | | | (_) | | | | |   | |_  | | | | |  __/   |  _| | |_| | | |_  | |_| | | |    |  __/\r\n; |_| \\_\\  \\___|  \\__,_| |_| |_| |_|  \\___/  |_| |_|    \\__| |_| |_|  \\___|   |_|    \\__,_|  \\__|  \\__,_| |_|     \\___|\r\n;");
            Logger.Log($"*** {MainClientConstants.GAME_NAME_LONG} 日志系统 ***");
            Logger.Log("客户端版本: " + Updater.GameVersion);

            // Log information about given startup params
            if (parameters.NoAudio)
            {
                Logger.Log("Startup parameter: No audio");

                
                throw new NotImplementedException("-NOAUDIO is currently not implemented, please run the client without it.".L10N("UI:Main:NoAudio"));
            }

            if (parameters.MultipleInstanceMode)
                Logger.Log("Startup parameter: Allow multiple client instances");

            parameters.UnknownStartupParams.ForEach(p => Logger.Log("Unknown startup parameter: " + p));

            Logger.Log("载入客户端配置.");
            UserINISettings.Initialize(ClientConfiguration.Instance.SettingsIniName);

            Mod.ReLoad();
           

            //   Try to load translations
            try
            {
                TranslationTable translation;
                var iniFileInfo = SafePath.GetFile(ProgramConstants.GamePath, ClientConfiguration.Instance.TranslationIniName);

                if (iniFileInfo.Exists)
                {

                    translation = TranslationTable.LoadFromIniFile(iniFileInfo.FullName);
                }
                else
                {
                    Logger.Log("Failed to load the translation file. File does not exist.");

                    translation = new TranslationTable();
                }

                TranslationTable.Instance = translation;
                Logger.Log("载入翻译文件: " + translation.LanguageName);
            }
            catch (Exception ex)
            {
                Logger.Log("Failed to load the translation file. " + ex.Message);
                TranslationTable.Instance = new TranslationTable();
            }

            try
            {
                if (ClientConfiguration.Instance.GenerateTranslationStub)
                {
                    string stubPath = SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "Translation.stub.ini");
                    var stubTable = TranslationTable.Instance.Clone();
                    TranslationTable.Instance.MissingTranslationEvent += (sender, e) =>
                    {
                        stubTable.Table.Add(e.Label, e.DefaultValue);
                    };

                    AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                    {
                        Logger.Log("Writing the translation stub file.");
                        var ini = stubTable.SaveIni();
                        ini.WriteIniFile(stubPath);
                    };

                    Logger.Log("Generating translation stub feature is now enabled. The stub file will be written when the client exits.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Failed to generate the translation stub. " + ex.Message);
            }

            // Delete obsolete files from old target project versions

            // gameDirectory.EnumerateFiles("mainclient.log").SingleOrDefault()?.Delete();
            // gameDirectory.EnumerateFiles("aunchupdt.dat").SingleOrDefault()?.Delete();

            try
            {
                gameDirectory.EnumerateFiles("wsock32.dll").SingleOrDefault()?.Delete();
            }
            catch (Exception ex)
            {
                LogException(ex);

                string error = "Deleting wsock32.dll failed! Please close any " +
                    "applications that could be using the file, and then start the client again."
                    + Environment.NewLine + Environment.NewLine +
                    "message: " + ex.Message;

                ProgramConstants.DisplayErrorAction(null, error, true);
            }

            ApplicationConfiguration.Initialize();
            new Startup().Execute();
        }

        public static void LogException(Exception ex, bool innerException = false)
        {
            if (!innerException)
                Logger.Log("KABOOOOOOM!!! Info:");
            else
                Logger.Log("InnerException info:");

            Logger.Log($"错误类型: {ex.GetType()}");
            Logger.Log($"错误信息: {ex.Message}");
            Logger.Log($"出错模块: {ex.Source}");
            Logger.Log($"出错函数: {ex.TargetSite.Name}");
            Logger.Log($"意外的致命错误: {ex.StackTrace}");

            MessageBox.Show($"时间： {DateTime.Now} \n" +
                $"客户端版本： {Updater.GameVersion} \n" +
                $"错误类型: {ex.GetType()} \n " +
                $"错误信息: {ex.Message} \n " +
                $"出错模块: {ex.Source}\n " +
                $"出错函数: {ex.TargetSite.Name}\n " +
                $"错误堆栈: \n{ex.StackTrace}",
                "意外的致命错误", MessageBoxButtons.OK, MessageBoxIcon.Error);

            if (ex.InnerException is not null)
                LogException(ex.InnerException, true);
        }

        static void HandleException(object sender, Exception ex)
        {
            LogException(ex);

            string errorLogPath = SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "ClientCrashLogs", FormattableString.Invariant($"ClientCrashLog{DateTime.Now:_yyyy_MM_dd_HH_mm}.txt"));
            bool crashLogCopied = false;

            try
            {
                DirectoryInfo crashLogsDirectoryInfo = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath, "ClientCrashLogs");

                if (!crashLogsDirectoryInfo.Exists)
                    crashLogsDirectoryInfo.Create();

                File.Copy(SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "client.log"), errorLogPath, true);
                crashLogCopied = true;
            }
            catch { }

            string error = string.Format("{0} has crashed. Error message:".L10N("UI:Main:FatalErrorText1") + Environment.NewLine + Environment.NewLine +
                ex.Message + Environment.NewLine + Environment.NewLine + (crashLogCopied ?
                "A crash log has been saved to the following file:".L10N("UI:Main:FatalErrorText2") + " " + Environment.NewLine + Environment.NewLine +
                errorLogPath + Environment.NewLine + Environment.NewLine : "") +
                (crashLogCopied ? "If the issue is repeatable, contact the {1} staff at {2} and provide the crash log file.".L10N("UI:Main:FatalErrorText3") :
                "If the issue is repeatable, contact the {1} staff at {2}.".L10N("UI:Main:FatalErrorText4")),
                MainClientConstants.GAME_NAME_LONG,
                MainClientConstants.GAME_NAME_SHORT,
                MainClientConstants.SUPPORT_URL_SHORT);

            ProgramConstants.DisplayErrorAction("KABOOOOOOOM".L10N("UI:Main:FatalErrorTitle"), error, true);
        }

        [SupportedOSPlatform("windows")]
        private static void CheckPermissions()
        {
            if (UserHasDirectoryAccessRights(ProgramConstants.GamePath, FileSystemRights.Modify))
                return;

            string error = string.Format(("You seem to be running {0} from a write-protected directory." + Environment.NewLine + Environment.NewLine +
                "For {1} to function properly when run from a write-protected directory, it needs administrative priveleges." + Environment.NewLine + Environment.NewLine +
                "Would you like to restart the client with administrative rights?" + Environment.NewLine + Environment.NewLine +
                "Please also make sure that your security software isn't blocking {1}.").L10N("UI:Main:AdminRequiredText"), MainClientConstants.GAME_NAME_LONG, MainClientConstants.GAME_NAME_SHORT);

            ProgramConstants.DisplayErrorAction("Administrative privileges required".L10N("UI:Main:AdminRequiredTitle"), error, false);

            using var _ = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = SafePath.CombineFilePath(ProgramConstants.StartupExecutable),
                Verb = "runas",
                CreateNoWindow = true
            });
            Environment.Exit(1);
        }

        /// <summary>
        /// Checks whether the client has specific file system rights to a directory.
        /// See ssds's answer at https://stackoverflow.com/questions/1410127/c-sharp-test-if-user-has-write-access-to-a-folder
        /// </summary>
        /// <param name="path">The path to the directory.</param>
        /// <param name="accessRights">The file system rights.</param>
        [SupportedOSPlatform("windows")]
        private static bool UserHasDirectoryAccessRights(string path, FileSystemRights accessRights)
        {
            var currentUser = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(currentUser);

            // If the user is not running the client with administrator privileges in Program Files, they need to be prompted to do so.
            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                string progfiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string progfilesx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                if (ProgramConstants.GamePath.Contains(progfiles) || ProgramConstants.GamePath.Contains(progfilesx86))
                    return false;
            }

            var isInRoleWithAccess = false;

            try
            {
                var di = new DirectoryInfo(path);
                var acl = di.GetAccessControl();
                var rules = acl.GetAccessRules(true, true, typeof(NTAccount));

                foreach (AuthorizationRule rule in rules)
                {
                    var fsAccessRule = rule as FileSystemAccessRule;
                    if (fsAccessRule == null)
                        continue;

                    if ((fsAccessRule.FileSystemRights & accessRights) > 0)
                    {
                        var ntAccount = rule.IdentityReference as NTAccount;
                        if (ntAccount == null)
                            continue;

                        if (principal.IsInRole(ntAccount.Value))
                        {
                            if (fsAccessRule.AccessControlType == AccessControlType.Deny)
                                return false;
                            isInRoleWithAccess = true;
                        }
                    }
                }
            }
            // 防止通过域网络管理的计算机出现域信任问题导致无法启动(Linq可能会在日志中写入: Sequence contains no matching element)
            catch (System.ComponentModel.Win32Exception ex)
            {
                Logger.Log($"Failed to translate to SIDs. Error: {ex.Message}");
                return isInRoleWithAccess;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            return isInRoleWithAccess;
        }
    }
}
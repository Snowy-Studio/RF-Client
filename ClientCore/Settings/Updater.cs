namespace ClientCore.Settings;

using ClientCore.Entity;
using Localization.Tools;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public static class Updater
{
    private const string SECOND_STAGE_UPDATER = "ClientUpdater.dll";

#if DEBUG
    public const string VERSION_FILE = "VersionDev";
#else
    public const string VERSION_FILE = "Version";
#endif

    /// <summary>
    /// 游戏根路径.
    /// </summary>
    public static string GamePath { get; set; } = string.Empty;

    /// <summary>
    /// 本地游戏资源路径.
    /// </summary>
    public static string ResourcePath { get; set; } = string.Empty;

    /// <summary>
    /// 更新器的本地游戏ID.
    /// </summary>
    public static string LocalGame { get; private set; } = "None";

    /// <summary>
    /// 更新器调用的可执行文件名称.
    /// </summary>
    public static string CallingExecutableFileName { get; private set; } = string.Empty;

    /// <summary>
    /// 更新服务器组 (只读)
    /// </summary>
    public static List<UpdaterServer> UpdaterServers { get => serverMirrors; set => serverMirrors = value; }

    /// <summary>
    /// Update server URL for current update mirror if available.
    /// </summary>
    public static string CurrentUpdateServerURL
        => serverMirrors is { Count: > 0 }
            ? serverMirrors[currentUpdaterServerIndex].url
            : null;

    private static VersionState _versionState = VersionState.UNKNOWN;

    /// <summary>
    /// Current version state of the updater.
    /// </summary>
    public static VersionState versionState
    {
        get => _versionState;
        private set
        {
            _versionState = value;
            DoOnVersionStateChanged();
        }
    }

    /// <summary>
    /// 如可用，当前更新是否需要手动下载？
    /// </summary>
    public static bool ManualUpdateRequired { get; private set; }

    /// <summary>
    /// 如可用，当前更新手动下载地址
    /// </summary>
    public static string ManualDownloadURL { get; private set; } = string.Empty;

    /// <summary>
    /// 本地更新器版本
    /// </summary>
    public static string UpdaterVersion { get; private set; } = "N/A";

    /// <summary>
    /// 本地游戏版本
    /// </summary>
    public static string GameVersion { get; set; } = "N/A";

    /// <summary>
    /// 服务器游戏版本
    /// </summary>
    public static string ServerGameVersion { get; private set; } = "N/A";

    /// <summary>
    /// 更新文件大小
    /// </summary>
    public static int UpdateSizeInKb { get; private set; }

    /// <summary>
    /// 更新文件时间
    /// </summary>
    public static string UpdateTime { get; private set; }

    // Misc.
    private static IniFile settingsINI;
    private static int currentUpdaterServerIndex;
    private static List<UpdaterServer> serverMirrors;

    public static VersionFileConfig serverVerCfg;
    public static VersionFileConfig clientVerCfg;
    // File infos.
    private static readonly List<UpdaterFileInfo> serverFileInfos = new();
    private static readonly List<UpdaterFileInfo> localFileInfos = new();

    private static SslProtocols GetPreferredSslProtocols()
    {
        // 获取 Windows 版本号
        Version osVersion = Environment.OSVersion.Version;

        // 仅在 Windows 平台下判断
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (osVersion.Major > 10 || (osVersion.Major == 10 && osVersion.Build >= 20348))
            {
                return SslProtocols.Tls13 | SslProtocols.Tls12;
            }
        }
        return SslProtocols.Tls12;
    }

    private static readonly ProgressMessageHandler SharedProgressMessageHandler = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(15),
        AutomaticDecompression = DecompressionMethods.All,
        SslOptions = { EnabledSslProtocols = GetPreferredSslProtocols() }
    });

    private static readonly HttpClient SharedHttpClient = new(SharedProgressMessageHandler, true)
    {
        DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
    };

    // Current update / download related.
    private static bool terminateUpdate;
    private static string currentFilename;
    private static int currentFileSize;
    private static int totalDownloadedKbs;

    /// <summary>
    /// 初始化更新器.
    /// </summary>
    /// <param name="settingsIniName">Client settings INI 文件名.</param>
    /// <param name="localGame">当前游戏的本地游戏ID.</param>
    /// <param name="callingExecutableFileName">调用执行文件的文件名.</param>
    /// <param name="servers">服务器列表</param>
    public static void Initialize(string settingsIniName, string localGame, string callingExecutableFileName)
    {
        Logger.Log("更新: 初始化更新模块.");
        settingsINI = new(SafePath.CombineFilePath(GamePath, settingsIniName));
        LocalGame = localGame;
        CallingExecutableFileName = callingExecutableFileName;
    }

    /// <summary>
    /// 检查是否有更新.
    /// </summary>
    public static void CheckForUpdates()
    {
        Logger.Log("更新: 检查更新.");
        if (versionState is not VersionState.UPDATECHECKINPROGRESS and not VersionState.UPDATEINPROGRESS)
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            DoVersionCheckAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }

    /// <summary>
    /// 检查本地文件版本.
    /// </summary>
    public static void CheckLocalFileVersions()
    {
        Logger.Log("更新: 检查本地文件版本.");

        string strUpdaterFile = SafePath.CombineFilePath(ResourcePath, "Binaries", SECOND_STAGE_UPDATER);
        if (File.Exists(strUpdaterFile))
        {
            Assembly assembly = Assembly.LoadFile(strUpdaterFile);
            UpdaterVersion = assembly.GetName().Version.ToString();
        }

        localFileInfos.Clear();
        var version = new IniFile(SafePath.CombineFilePath(GamePath, VERSION_FILE));
        if (!File.Exists(version.FileName))
            return;

        clientVerCfg = new VersionFileConfig()
        {
            Version = version.GetStringValue("Client", "Version", string.Empty),
            UpdaterVersion = version.GetStringValue("Client", "UpdaterVersion", "N/A"),
        };

        var lstKeys = version.GetSectionKeys("");
        if (null != lstKeys && lstKeys.Count > 0)
        {
            foreach (var strKey in lstKeys)
            {
                string[] strArray = version.GetStringValue("FileVerify", strKey, string.Empty).Split(',');
                var item = new UpdaterFileInfo(
                        SafePath.CombineFilePath(strKey), Conversions.IntFromString(strArray[1], 0))
                {
                    Identifier = strArray[0],
                    ArchiveIdentifier = "",
                    ArchiveSize = 0
                };
                localFileInfos.Add(item);
            }
        }
        OnLocalFileVersionsChecked?.Invoke();
    }

    /// <summary>
    /// 开始更新过程.
    /// </summary>
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    public static void StartUpdate() => PerformUpdateAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

    /// <summary>
    /// 停止当前更新过程.
    /// </summary>
    public static void StopUpdate() => terminateUpdate = true;

    /// <summary>
    /// 清除当前版本文件信息.
    /// </summary>
    public static void ClearVersionInfo()
    {
        serverFileInfos.Clear();
        localFileInfos.Clear();
        //GameVersion = "1.5.0.00";
        //versionState = VersionState.UNKNOWN;
    }

    /// <summary>
    /// 获取并按延迟从低到高排序的服务器列表
    /// </summary>
    /// <param name="channel">更新通道</param>
    /// <returns>排序后的服务器列表</returns>
    private static List<UpdaterServer> GetSortedServersByLatency(int channel)
    {
        var mirrors = serverMirrors.Where(m => m.type == channel).ToList();
        var latencyList = new List<(UpdaterServer server, long latency)>();
        foreach (var mirror in mirrors)
        {
            long latency = MeasureLatency(mirror);
            latencyList.Add((mirror, latency >= 0 ? latency : long.MaxValue));
        }
        return latencyList.OrderBy(t => t.latency).Select(t => t.server).ToList();
    }

    /// <summary>
    /// 获取并按优先级从高到底排序的服务器列表
    /// </summary>
    /// <param name="channel">更新通道</param>
    /// <returns>排序后的服务器列表</returns>
    private static List<UpdaterServer> GetSortedServersByPriority(int channel)
    {
        var mirrors = serverMirrors.Where(m => m.type == channel).ToList();
        var latencyList = new List<(UpdaterServer server, int priority, long latency)>();

        foreach (var mirror in mirrors)
        {
            long latency = MeasureLatency(mirror);
            latencyList.Add((mirror, mirror.priority, latency >= 0 ? latency : long.MaxValue));
        }

        // 先按priority降序，再按latency升序排序
        return latencyList
            .OrderByDescending(t => t.priority)
            .ThenBy(t => t.latency)
            .Select(t => t.server)
            .ToList();
    }

    /// <summary>
    /// 根据指定更新通道返回实际延迟最低的服务器。
    /// </summary>
    /// <param name="channel">更新通道（例如：Stable=0，Insiders=1）</param>
    public static UpdaterServer? GetBestMirror(int channel)
    {
        var mirrors = serverMirrors.Where(m => m.type == channel).ToList();
        if (mirrors.Count == 0)
            return null;

        UpdaterServer bestMirror = default;
        long bestLatency = long.MaxValue;

        foreach (var mirror in mirrors)
        {
            long latency = MeasureLatency(mirror);
            if (latency >= 0 && latency < bestLatency)
            {
                bestLatency = latency;
                bestMirror = mirror;
            }
        }
        return bestMirror;
    }

    /// <summary>
    /// 使用 Ping 测试服务器延迟，返回往返时间（毫秒）。若测试失败则返回 -1。
    /// </summary>
    private static long MeasureLatency(UpdaterServer mirror)
    {
        try
        {
            // 提取 URL 主机进行Ping测试
            string host = new Uri(mirror.url).Host;
            using var ping = new Ping();
            PingReply reply = ping.Send(host, 1000);
            if (reply.Status == IPStatus.Success)
                return reply.RoundtripTime;
        }
        catch
        {
            // 忽略异常，返回-1
        }
        return -1;
    }

    internal static void UpdateUserAgent(HttpClient httpClient)
    {
        httpClient.DefaultRequestHeaders.UserAgent.Clear();

        if (GameVersion != "N/A")
            httpClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue(LocalGame, GameVersion));

        if (UpdaterVersion != "N/A")
            httpClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue(nameof(Updater), UpdaterVersion));

        httpClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Client", Assembly.GetEntryAssembly().GetName().Version.ToString()));
    }

    /// <summary>
    /// 删除文件，并等待其删除完成.
    /// </summary>
    /// <param name="filepath">待删除的文件路径.</param>
    /// <param name="timeout">等待超时时间（毫秒）.</param>
    internal static void DeleteFileAndWait(string filepath, int timeout = 10000)
    {
        FileInfo fileInfo = SafePath.GetFile(filepath);
        using var fw = new FileSystemWatcher(fileInfo.DirectoryName, fileInfo.Name);
        using var mre = new ManualResetEventSlim();

        fw.EnableRaisingEvents = true;
        fw.Deleted += (_, _) =>
        {
            mre.Set();
        };
        fileInfo.Delete();
        mre.Wait(timeout);
    }

    /// <summary>
    /// 为文件路径创建所有所需的目录.
    /// </summary>
    /// <param name="filePath">文件路径.</param>
    internal static void CreatePath(string filePath)
    {
        FileInfo fileInfo = SafePath.GetFile(filePath);
        if (!fileInfo.Directory.Exists)
            fileInfo.Directory.Create();
    }

    internal static string GetUniqueIdForFile(string filePath)
    {
        using var md = MD5.Create();
        md.Initialize();
        using FileStream fs = SafePath.GetFile(GamePath, filePath).OpenRead();
        md.ComputeHash(fs);
        var builder = new StringBuilder();
        foreach (byte num2 in md.Hash)
            builder.Append(num2);
        md.Clear();
        return builder.ToString();
    }

    /// <summary>
    /// 检查文件是否不存在或为原始文件.
    /// </summary>
    public static bool IsFileNonexistantOrOriginal(string filePath)
    {
        if (localFileInfos is null || localFileInfos.Count == 0)
            return true;
        var info = localFileInfos.Find(f => f.Filename.Equals(filePath, StringComparison.OrdinalIgnoreCase));
        if (info == null)
            return true;
        string uniqueIdForFile = GetUniqueIdForFile(info.Filename);
        return info.Identifier == uniqueIdForFile;
    }

    /// <summary>
    /// 对更新服务器执行版本信息检查.
    /// </summary>
    private static void DoVersionCheckAsync()
    {
        Logger.Log("更新: 检查文件版本.");
        UpdateSizeInKb = 0;
        try
        {
            versionState = VersionState.UPDATECHECKINPROGRESS;
            if (UpdaterServers.Count == 0)
            {
                Logger.Log("更新：这不是合法的更新地址!");
            }
            else
            {
                Logger.Log("更新：检查更新服务.");
                UpdateUserAgent(SharedHttpClient);

                // 根据更新服务器顺序依次查找合适的服务器信息
                var version = NetWorkINISettings.Get<ClientCore.Entity.Updater>($"updater/getNewLatestInfoByBaseVersion?type={UserINISettings.Instance.Beta.Value}&baseVersion={GameVersion}").GetAwaiter().GetResult().Item1 ?? throw new("Update server integrity error while checking for updates.");
                serverVerCfg = new VersionFileConfig()
                {
                    Version = version.version,
                    Package = version.file,
                    Hash = version.hash,
                    Size = int.Parse(version.size),
                    Logs = version.log,
                    time = version.updateTime
                };

                Logger.Log("更新：Server game version is " + serverVerCfg.Version + ", local version is " + GameVersion);
                ServerGameVersion = serverVerCfg.Version;
                UpdateTime = serverVerCfg.time;
                if (!CheckHasNewVersion(ServerGameVersion, GameVersion))
                {
                    versionState = VersionState.UPTODATE;
                    DoFileIdentifiersUpdatedEvent();
                }
                else
                {
                    string strServUpdaterVer = serverVerCfg.UpdaterVersion;
                    if (strServUpdaterVer != "N/A" && UpdaterVersion == "N/A" && strServUpdaterVer != UpdaterVersion)
                    {
                        Logger.Log("更新：Server updater  version is set to " + strServUpdaterVer + " and is different to local update system version " + UpdaterVersion + ". Manual update required.");
                        versionState = VersionState.OUTDATED;
                        ManualUpdateRequired = true;
                        ManualDownloadURL = serverVerCfg.ManualDownURL;
                        DoFileIdentifiersUpdatedEvent();
                    }
                    else
                    {
                        UpdateSizeInKb = serverVerCfg.Size;
                        VersionCheckHandle();
                    }
                }
            }
        }
        catch (Exception exception)
        {
            versionState = VersionState.UNKNOWN;
            Logger.Log("更新：An error occured while performing version check: " + exception.Message);
            DoFileIdentifiersUpdatedEvent();
        }
    }

    /// <summary>
    /// 版本比较逻辑，正式版 Revision 固定为 100，测试版按实际 Revision.
    /// </summary>
    private static bool CheckHasNewVersion(string strSer, string strLoc)
    {
        Version v1 = new Version(strSer);
        Version v2 = new Version(strLoc);

        // 正式版 Revision 固定为 100(因为本身就没有)，测试版用实际 Revision
        int v1Revision = (v1.Revision == 0) ? 100 : v1.Revision;
        int v2Revision = (v2.Revision == 0) ? 100 : v2.Revision;

        if (v1.Major > v2.Major)
            return true;
        if (v1.Major == v2.Major)
        {
            if (v1.Minor > v2.Minor)
                return true;
            if (v1.Minor == v2.Minor)
            {
                if (v1.Build > v2.Build)
                    return true;
                if (v1.Build == v2.Build)
                {
                    if (v1Revision > v2Revision)
                        return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 下载更新后执行的脚本文件.
    /// </summary>
    private static async ValueTask ExecuteAfterUpdateScriptAsync()
    {
        Logger.Log("更新：Downloading updateexec.");
        try
        {
            var fileStream = new FileStream(SafePath.CombineFilePath(GamePath, "updateexec"), new FileStreamOptions
            {
                Access = FileAccess.Write,
                BufferSize = 0,
                Mode = FileMode.Create,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.WriteThrough,
                Share = FileShare.None
            });
            await using (fileStream.ConfigureAwait(false))
            {
                Stream stream = await SharedHttpClient.GetStreamAsync(UpdaterServers[currentUpdaterServerIndex].url + "updateexec").ConfigureAwait(false);
                await using (stream.ConfigureAwait(false))
                {
                    await stream.CopyToAsync(fileStream).ConfigureAwait(false);
                }
            }
        }
        catch (Exception exception)
        {
            Logger.Log("更新：Warning: Downloading updateexec failed: " + exception.Message);
            return;
        }
        ExecuteScript("updateexec");
    }

    /// <summary>
    /// 执行脚本文件.
    /// </summary>
    /// <param name="fileName">脚本文件名.</param>
    private static void ExecuteScript(string fileName)
    {
        Logger.Log("更新：Executing " + fileName + ".");
        FileInfo scriptFileInfo = SafePath.GetFile(GamePath, fileName);
        var script = new IniFile(scriptFileInfo.FullName);
        // Delete files.
        foreach (string key in GetKeys(script, "Delete"))
        {
            Logger.Log("更新：" + fileName + ": Deleting file " + key);
            try
            {
                SafePath.DeleteFileIfExists(GamePath, key);
            }
            catch (Exception ex)
            {
                Logger.Log("更新：" + fileName + ": Deleting file " + key + "failed: " + ex.Message);
            }
        }
        // Rename files.
        foreach (string key in GetKeys(script, "Rename"))
        {
            string newFilename = SafePath.CombineFilePath(script.GetStringValue("Rename", key, string.Empty));
            if (string.IsNullOrWhiteSpace(newFilename))
                continue;
            try
            {
                Logger.Log("更新：" + fileName + ": Renaming file '" + key + "' to '" + newFilename + "'");
                FileInfo file = SafePath.GetFile(GamePath, key);
                if (file.Exists)
                    file.MoveTo(SafePath.CombineFilePath(GamePath, newFilename));
            }
            catch (Exception ex)
            {
                Logger.Log("更新：" + fileName + ": Renaming file '" + key + "' to '" + newFilename + "' failed: " + ex.Message);
            }
        }
        // Rename folders.
        foreach (string key in GetKeys(script, "RenameFolder"))
        {
            string newDirectoryName = script.GetStringValue("RenameFolder", key, string.Empty);
            if (string.IsNullOrWhiteSpace(newDirectoryName))
                continue;
            try
            {
                Logger.Log("更新：" + fileName + ": Renaming directory '" + key + "' to '" + newDirectoryName + "'");
                DirectoryInfo directory = SafePath.GetDirectory(GamePath, key);
                if (directory.Exists)
                    directory.MoveTo(SafePath.CombineDirectoryPath(GamePath, newDirectoryName));
            }
            catch (Exception ex)
            {
                Logger.Log("更新：" + fileName + ": Renaming directory '" + key + "' to '" + newDirectoryName + "' failed: " + ex.Message);
            }
        }
        // Rename & merge files / folders.
        foreach (string key in GetKeys(script, "RenameAndMerge"))
        {
            string directoryName = key;
            string directoryNameToMergeInto = script.GetStringValue("RenameAndMerge", key, string.Empty);
            if (string.IsNullOrWhiteSpace(directoryNameToMergeInto))
                continue;
            try
            {
                Logger.Log("更新：" + fileName + ": Merging directory '" + directoryName + "' with '" + directoryNameToMergeInto + "'");
                DirectoryInfo directoryToMergeInto = SafePath.GetDirectory(GamePath, directoryNameToMergeInto);
                DirectoryInfo gameDirectory = SafePath.GetDirectory(GamePath, directoryName);
                if (!gameDirectory.Exists)
                    continue;
                if (!directoryToMergeInto.Exists)
                {
                    Logger.Log("更新：" + fileName + ": Destination directory '" + directoryNameToMergeInto + "' does not exist, renaming.");
                    gameDirectory.MoveTo(directoryToMergeInto.FullName);
                }
                else
                {
                    Logger.Log("更新：" + fileName + ": Destination directory '" + directoryNameToMergeInto + "' exists, performing selective merging.");
                    FileInfo[] files = gameDirectory.GetFiles();
                    foreach (FileInfo file in files)
                    {
                        FileInfo fileToMergeInto = SafePath.GetFile(directoryToMergeInto.FullName, file.Name);
                        if (fileToMergeInto.Exists)
                        {
                            Logger.Log("更新：" + fileName + ": Destination file '" + directoryNameToMergeInto + "/" + file.Name + "' exists, removing original source file " + directoryName + "/" + file.Name);
                            fileToMergeInto.Delete();
                        }
                        else
                        {
                            Logger.Log("更新：" + fileName + ": Destination file '" + directoryNameToMergeInto + "/" + file.Name + "' does not exist, moving original source file " + directoryName + "/" + file.Name);
                            file.MoveTo(fileToMergeInto.FullName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("更新：" + fileName + ": Merging directory '" + directoryName + "' with '" + directoryNameToMergeInto + "' failed: " + ex.Message);
            }
        }
        // Delete folders.
        foreach (string sectionName in new string[] { "DeleteFolder", "ForceDeleteFolder" })
        {
            foreach (string key in GetKeys(script, sectionName))
            {
                try
                {
                    Logger.Log("更新：" + fileName + ": Deleting directory '" + key + "'");
                    SafePath.DeleteDirectoryIfExists(true, GamePath, key);
                }
                catch (Exception ex)
                {
                    Logger.Log("更新：" + fileName + ": Deleting directory '" + key + "' failed: " + ex.Message);
                }
            }
        }
        // Delete folders, if empty.
        foreach (string key in GetKeys(script, "DeleteFolderIfEmpty"))
        {
            try
            {
                Logger.Log("更新：" + fileName + ": Deleting directory '" + key + "' if it's empty.");
                DirectoryInfo directoryInfo = SafePath.GetDirectory(GamePath, key);
                if (directoryInfo.Exists)
                {
                    if (!directoryInfo.EnumerateFiles().Any())
                        directoryInfo.Delete();
                    else
                        Logger.Log("更新：" + fileName + ": Directory '" + key + "' is not empty!");
                }
                else
                    Logger.Log("更新：" + fileName + ": Specified directory does not exist.");
            }
            catch (Exception ex)
            {
                Logger.Log("更新：" + fileName + ": Deleting directory '" + key + "' if it's empty failed: " + ex.Message);
            }
        }
        // Create folders.
        foreach (string key in GetKeys(script, "CreateFolder"))
        {
            try
            {
                DirectoryInfo directoryInfo = SafePath.GetDirectory(GamePath, key);
                if (!directoryInfo.Exists)
                {
                    Logger.Log("更新：" + fileName + ": Creating directory '" + key + "'");
                    directoryInfo.Create();
                }
                else
                    Logger.Log("更新：" + fileName + ": Directory '" + key + "' already exists.");
            }
            catch (Exception ex)
            {
                Logger.Log("更新：" + fileName + ": Creating directory '" + key + "' failed: " + ex.Message);
            }
        }
        scriptFileInfo.Delete();
    }

    /// <summary>
    /// 处理版本检查逻辑.
    /// </summary>
    private static void VersionCheckHandle()
    {
        Logger.Log("更新：Gathering file to be downloaded. Server file is: " + serverVerCfg.Package);
        versionState = VersionState.OUTDATED;
        ManualUpdateRequired = false;
        DoFileIdentifiersUpdatedEvent();
    }

    /// <summary>
    /// 下载更新文件并启动第二阶段更新程序.
    /// </summary>
    private static async Task PerformUpdateAsync()
    {
        Logger.Log("更新：Starting update.");
        versionState = VersionState.UPDATEINPROGRESS;
        try
        {
            UpdateUserAgent(SharedHttpClient);
            SharedProgressMessageHandler.HttpReceiveProgress += ProgressMessageHandlerOnHttpReceiveProgress;
            VersionCheckHandle();
            if (string.IsNullOrEmpty(ServerGameVersion) || ServerGameVersion == "N/A" || versionState != VersionState.OUTDATED)
                throw new("Update server integrity error.");
            versionState = VersionState.UPDATEINPROGRESS;
            totalDownloadedKbs = currentFileSize = 0;
            if (terminateUpdate)
            {
                Logger.Log("更新：Terminating update because of user request.");
                versionState = VersionState.OUTDATED;
                ManualUpdateRequired = false;
                terminateUpdate = false;
                return;
            }

            // 获取并排序服务器列表
            var sortedServers = GetSortedServersByPriority(UserINISettings.Instance.Beta.Value);
            if (sortedServers.Count == 0)
                throw new("没有可用的更新服务器.");

            Exception lastException = null;
            bool downloadSuccess = false;

            // 依次尝试每个服务器，每台服务器最多重试3次
            foreach (var server in sortedServers)
            {
                int serverRetry = 0;
                Logger.Log($"更新：尝试服务器 {server.url} (延迟优先级)");
                currentUpdaterServerIndex = UpdaterServers.FindIndex(s => s.url == server.url);

                while (serverRetry < 3)
                {
                    if (terminateUpdate)
                    {
                        Logger.Log("更新：Terminating update because of user request.");
                        versionState = VersionState.OUTDATED;
                        ManualUpdateRequired = false;
                        terminateUpdate = false;
                        return;
                    }

                    currentFilename = serverVerCfg.Package;
                    currentFileSize = serverVerCfg.Size;

                    bool flag = false;
                    try
                    {
                        flag = await DownloadFileAsync(currentFilename).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        Logger.Log($"更新：下载文件 {currentFilename} 时发生异常: {ex.Message}");
                    }

                    if (flag)
                    {
                        totalDownloadedKbs += currentFileSize;
                        downloadSuccess = true;
                        break;
                    }

                    serverRetry++;
                    Logger.Log($"更新：服务器 {server.url} 下载失败，重试 {serverRetry}/3 ...");
                    await Task.Delay(2000);
                }

                if (downloadSuccess)
                    break;
            }

            if (!downloadSuccess)
            {
                Logger.Log("更新：所有服务器均下载失败。");
                throw new("所有服务器均下载失败。" + (lastException != null ? $" 最后错误: {lastException.Message}" : ""));
            }

            DirectoryInfo tmpDirInfo = SafePath.GetDirectory(GamePath, "Tmp");
            try
            {
                var pkgFile = SafePath.CombineFilePath(tmpDirInfo.FullName, currentFilename);
                if (!string.IsNullOrEmpty(serverVerCfg.Hash))
                {
                    string strFileHash = Utilities.CalculateSHA1ForFile(pkgFile);
                    if (serverVerCfg.Hash != strFileHash)
                    {
                        Logger.Log("更新：Terminating update because of file hash is incorrect.");
                        versionState = VersionState.OUTDATED;
                        ManualUpdateRequired = false;
                        DoOnUpdateFailed("更新包校验不通过");
                        return;
                    }
                }
                RenderImage.CancelRendering();
                SevenZip.ExtractWith7Zip(pkgFile, tmpDirInfo.FullName);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }
            tmpDirInfo.Refresh();
            if (tmpDirInfo.Exists)
            {
                DirectoryInfo curClientUpdaterDir = SafePath.GetDirectory(tmpDirInfo.FullName, "Resources", "Binaries");
                FileInfo curClientUpdater = SafePath.GetFile(curClientUpdaterDir.FullName, SECOND_STAGE_UPDATER);
                Logger.Log("更新：Checking & moving second-stage updater files.");
                FileInfo clientUpdaterFile = SafePath.GetFile(ResourcePath, "Binaries", SECOND_STAGE_UPDATER);
                Logger.Log("更新：Launching second-stage updater executable " + clientUpdaterFile.FullName + ".");
                string strDotnet = @"C:\Program Files\dotnet\dotnet.exe";
                if (!File.Exists(strDotnet))
                {
                    Logger.Log("dotnet not exits.");
                    DoOnUpdateFailed("dotnet.exe不存在");
                    return;
                }
                using var _ = Process.Start(new ProcessStartInfo
                {
                    FileName = strDotnet,
                    Arguments = "\"" + clientUpdaterFile.FullName + "\" " + CallingExecutableFileName + " \"" + GamePath + "\"",
                    UseShellExecute = true
                });
                Logger.Log("\"" + clientUpdaterFile.FullName + "\" " + CallingExecutableFileName + " \"" + GamePath + "\"");
                Environment.Exit(0);
                Restart?.Invoke(null, EventArgs.Empty);
            }
            else
            {
                Logger.Log("更新：Update completed successfully.");
                totalDownloadedKbs = 0;
                UpdateSizeInKb = 0;
                CheckLocalFileVersions();
                ServerGameVersion = "N/A";
                versionState = VersionState.UPTODATE;
                DoUpdateCompleted();
            }
        }
        catch (Exception ex)
        {
            Logger.Log("更新：An error occurred during the update. message: " + ex.Message);
            versionState = VersionState.UNKNOWN;
            DoOnUpdateFailed(ex.Message);
        }
        finally
        {
            SharedProgressMessageHandler.HttpReceiveProgress -= ProgressMessageHandlerOnHttpReceiveProgress;
        }
    }

    /// <summary>
    /// 下载并处理单个文件.
    /// </summary>
    /// <param name="strfile">文件名.</param>
    /// <returns>下载成功返回 true，否则 false.</returns>
    private static async ValueTask<bool> DownloadFileAsync(string strfile)
    {
        Logger.Log("更新：Initiliazing download of file " + strfile);
        UpdateDownloadProgress(0);

        string tmpDir = SafePath.CombineFilePath(GamePath, "Tmp");
        string filePath = SafePath.CombineFilePath(tmpDir, strfile);
        FileInfo locFile = new FileInfo(filePath);

        try
        {
            int currentUpdaterServerId = currentUpdaterServerIndex;
            var serversCondi = UpdaterServers.Where(f => f.type.Equals(UserINISettings.Instance.Beta.Value)).ToList();

            if (currentUpdaterServerId < 0 || currentUpdaterServerId >= serversCondi.Count)
            {
                Logger.Log($"更新：Server index out of bounds，currentUpdaterServerId={currentUpdaterServerId}, serversCondi.Count={serversCondi.Count}");
                return false;
            }

            var serverFile = (serversCondi[currentUpdaterServerId].url + strfile).Replace(@"\", "/", StringComparison.OrdinalIgnoreCase);
            CreatePath(locFile.FullName);
            Logger.Log("更新：Downloading file " + strfile);
            var fileStream = new FileStream(locFile.FullName, new FileStreamOptions
            {
                Access = FileAccess.Write,
                BufferSize = 0,
                Mode = FileMode.Create,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.WriteThrough,
                Share = FileShare.None
            });
            await using (fileStream.ConfigureAwait(false))
            {
                Stream stream = await SharedHttpClient.GetStreamAsync(new Uri(serverFile)).ConfigureAwait(false);
                await using (stream.ConfigureAwait(false))
                {
                    await stream.CopyToAsync(fileStream).ConfigureAwait(false);
                }
            }
            OnFileDownloadCompleted?.Invoke(strfile);
            Logger.Log("更新：Download of file " + strfile + " finished - verifying.");
        }
        catch (Exception exception)
        {
            Logger.Log("更新：An error occurred while downloading file " + strfile + ": " + exception.Message);
            return false;
        }
        return true;
    }

    /// <summary>
    /// 更新下载进度.
    /// </summary>
    /// <param name="progressPercentage">当前进度百分比.</param>
    private static void UpdateDownloadProgress(int progressPercentage)
    {
        double num = currentFileSize * (progressPercentage / 100.0);
        double num2 = totalDownloadedKbs + num;
        int totalPercentage = 0;
        if (UpdateSizeInKb is > 0 and < int.MaxValue)
            totalPercentage = (int)(num2 / UpdateSizeInKb * 100.0);
        DownloadProgressChanged(currentFilename, progressPercentage, totalPercentage);
    }

    /// <summary>
    /// 从INI文件的指定段获取所有键.
    /// </summary>
    /// <param name="iniFile">INI文件.</param>
    /// <param name="sectionName">段名称.</param>
    /// <returns>键列表，若没有则返回空列表.</returns>
    private static List<string> GetKeys(IniFile iniFile, string sectionName)
    {
        List<string> keys = iniFile.GetSectionKeys(sectionName);
        if (keys != null)
            return keys;
        return new();
    }

    /// <summary>
    /// 尝试获取文件的唯一标识.
    /// </summary>
    /// <param name="filePath">文件路径.</param>
    /// <returns>若成功返回文件标识，否则返回空字符串.</returns>
    private static string TryGetUniqueId(string filePath)
    {
        try
        {
            return GetUniqueIdForFile(filePath);
        }
        catch
        {
            return string.Empty;
        }
    }

    public static event NoParamEventHandler FileIdentifiersUpdated;
    public static event LocalFileCheckProgressChangedCallback LocalFileCheckProgressChanged;
    public static event NoParamEventHandler OnCustomComponentsOutdated;
    public static event NoParamEventHandler OnLocalFileVersionsChecked;
    public static event NoParamEventHandler OnUpdateCompleted;
    public static event SetExceptionCallback OnUpdateFailed;
    public static event NoParamEventHandler OnVersionStateChanged;
    public static event FileDownloadCompletedEventHandler OnFileDownloadCompleted;
    public static event EventHandler Restart;
    public static event UpdateProgressChangedCallback UpdateProgressChanged;

    public delegate void LocalFileCheckProgressChangedCallback(int checkedFileCount, int totalFileCount);
    public delegate void NoParamEventHandler();
    public delegate void SetExceptionCallback(string strMsg);
    public delegate void UpdateProgressChangedCallback(string currFileName, int currFilePercentage, int totalPercentage);
    public delegate void FileDownloadCompletedEventHandler(string archiveName);

    private static void ProgressMessageHandlerOnHttpReceiveProgress(object sender, HttpProgressEventArgs e) => UpdateDownloadProgress(e.ProgressPercentage);
    private static void DownloadProgressChanged(string currFileName, int currentFilePercentage, int totalPercentage) => UpdateProgressChanged?.Invoke(currFileName, currentFilePercentage, totalPercentage);
    private static void DoCustomComponentsOutdatedEvent() => OnCustomComponentsOutdated?.Invoke();
    private static void DoFileIdentifiersUpdatedEvent()
    {
        Logger.Log("更新：File identifiers updated.");
        FileIdentifiersUpdated?.Invoke();
    }
    private static void DoOnUpdateFailed(string strMsg) => OnUpdateFailed?.Invoke(strMsg);
    private static void DoOnVersionStateChanged() => OnVersionStateChanged?.Invoke();
    private static void DoUpdateCompleted() => OnUpdateCompleted?.Invoke();
}

//public readonly record struct UpdaterServer(int Type, string Name, string Location, string URL);

public readonly record struct VersionFileConfig(string Version, string UpdaterVersion, string ManualDownURL, string Package, string Hash, int Size, string Logs, string time);

public enum VersionState
{
    UNKNOWN,                //未知
    UPTODATE,               //最新版
    OUTDATED,               //已过期
    MISMATCHED,             //不匹配
    UPDATEINPROGRESS,       //更新中
    UPDATECHECKINPROGRESS,  //检查更新中
}

internal sealed record UpdaterFileInfo(string Filename, int Size)
{
    public string Identifier { get; set; }
    public string ArchiveIdentifier { get; set; }
    public int ArchiveSize { get; set; }
    public bool Archived => !string.IsNullOrEmpty(ArchiveIdentifier) && ArchiveSize > 0;
}
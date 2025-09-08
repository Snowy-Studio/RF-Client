using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClientCore.Entity;
using ClientCore.Enums;
using DTAConfig.Entity;
using Rampastring.Tools;

namespace ClientCore.Settings;

public class NetWorkINISettings
{
    private static NetWorkINISettings _instance;

    //private const string locServerPath = "Resources/ServerList";  // 本地服务器列表路径
    //private static string remoteFileUrl = "";                     // 远程设置路径
    private const string localFilePath = "Resources\\Settings";     // 本地设置路径
    private const string secUpdater = "Updater";                    // 更新段+组件段

#if DEBUG
    public const string Address = "https://api.yra2.com/";
 //public const string Address = "http://localhost:9088/";
#else
    public const string Address = "https://api.yra2.com/";
#endif

    public static event EventHandler DownloadCompleted;

    public IniFile SettingsIni { get; private set; }

    public List<UpdaterServer> UpdaterServers { get; private set; } = []; // 服务器更新列表

    protected NetWorkINISettings(IniFile iniFile)
    {
        SettingsIni = iniFile;

        if (SettingsIni.SectionExists(secUpdater))
        {
            string strServers = SettingsIni.GetStringValue(secUpdater, "Servers", "");
            if(!string.IsNullOrEmpty(strServers))
            {
                string[] serverGroup = strServers.Split(',');
                UpdaterServers.Clear();
                for (int i = 0; i < serverGroup.Length; i++)
                {
                    var us = new UpdaterServer(
                        id: null,
                        type: SettingsIni.GetIntValue(serverGroup[i], "Type", 0),
                        name: SettingsIni.GetStringValue(serverGroup[i], "Name", $"服务器#{i}"),
                        location: SettingsIni.GetStringValue(serverGroup[i], "Location", "Unkown"),
                        url: SettingsIni.GetStringValue(serverGroup[i], "Url", ""),
                        priority: SettingsIni.GetIntValue(serverGroup[i], "Priority", 0)
                        );
                    
                    UpdaterServers.Add(us);
                }
            }
        }
    }

    protected NetWorkINISettings(List<UpdaterServer> uss)
    {
            UpdaterServers.AddRange(uss);
    }

    public static NetWorkINISettings Instance
    {
        get => _instance;
    }

    public static void Initialize()
    {
        //var remoteServerUrl = (await Get<string>("dict/GetValue?section=dln&key=main_address")).Item1 ?? "https://autopatch1-zh-tcdn.yra2.com/Client/ServerList";

        //if (!DownloadSettingFile(remoteServerUrl, locServerPath))
        //    Logger.Log("Request Server List File Failed!");

        //var IniServer = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, locServerPath));
        //foreach (var secName in IniServer.GetSections())
        //{
        //    string strID = IniServer.GetStringValue(secName, "Id", "");
        //    string strName = IniServer.GetStringValue(secName, "Name", "");
        //    string strURL = IniServer.GetStringValue(secName, "Url", "");
        //    if (!string.IsNullOrEmpty(strURL))
        //    {
        //        remoteFileUrl = strURL + "/Client/Public/Settings";
        //        if (DownloadSettingFile(remoteFileUrl, localFilePath))
        //        {
        //            ProgramConstants.CUR_SERVER_URL = strURL;
        //            Console.WriteLine("Activated Server：{0}",strURL);
        //            Logger.Log($"Requset Server:{strID} {strName} {strURL} Successed");
        //            break;
        //        }
        //        else
        //            Logger.Log($"Requset Server:{strID} {strName} {strURL} Failed");
        //    }
        //}

        var uss = Get<List<UpdaterServer>>("updaterServer/getAllUpdaterServer").Result.Item1;

        //如果远程获取文件失败则读取本地配置
        if (uss == null)
        {
            var iniFile = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, localFilePath));
            _instance = new NetWorkINISettings(iniFile);
        }
        else
        {
            _instance = new NetWorkINISettings(uss);
            _ = Task.Run(() =>
            {
                var iniFile = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, localFilePath));

                if (uss != null && uss.Count > 0)
                {
                    // 构建 Servers 字符串，例如：Server0,Server1,...
                    var serverNames = new List<string>();
                    for (int i = 0; i < uss.Count; i++)
                    {
                        string sectionName = $"Server{i}";
                        var server = uss[i];

                        // 写每个 Server 的字段到对应 section
                        iniFile.SetIntValue(sectionName, "Type", server.type);
                        iniFile.SetStringValue(sectionName, "Name", server.name);
                        iniFile.SetStringValue(sectionName, "Location", server.location);
                        iniFile.SetStringValue(sectionName, "Url", server.url);
                        iniFile.SetValue(sectionName, "Priority", server.priority);

                        serverNames.Add(sectionName);
                    }

                    // 写 Servers 字段（逗号分隔）
                    string joined = string.Join(",", serverNames);
                    iniFile.SetStringValue(secUpdater, "Servers", joined);
                }
                else
                {
                    // 没有服务器，清空 Servers 字段
                    iniFile.RemoveSection("Servers");
                }
                iniFile.WriteIniFile();
            });

        }

        Updater.Initialize(
                ClientConfiguration.Instance.SettingsIniName,
                ClientConfiguration.Instance.LocalGame,
                SafePath.GetFile(ProgramConstants.StartupExecutable).Name);
        Updater.UpdaterServers = _instance.UpdaterServers;
        DownloadCompleted?.Invoke(null, EventArgs.Empty);
    }

    //protected static bool DownloadSettingFile(string strSerPath, string strLocPath)
    //{
    //    try
    //    {
    //        return WebHelper.HttpDownFile(strSerPath, strLocPath);
    //    }
    //    catch (Exception ex)
    //    {
    //        Logger.Log("连接服务器出错。" + ex);
    //        return false;
    //    }
    //}

    public void SetServerList()
    {
        UpdaterServers = Updater.UpdaterServers;
    }

    public static async Task<(T,string)> Post<T>(string url, object obj)
    {
        using var client = new HttpClient();

        // 将对象转换为 JSON 字符串
        string jsonContent = JsonSerializer.Serialize(obj);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", UserINISettings.Instance.Token.Value);
        client.Timeout = new TimeSpan(30 * TimeSpan.TicksPerSecond);
        // 发送 POST 请求并获取响应
        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync($"{Address}{url}", new StringContent(jsonContent, Encoding.UTF8, "application/json")).ConfigureAwait(false);
        

        // 读取响应内容
        T responseData;

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var resResult = JsonSerializer.Deserialize<ResResult<T>>(responseContent);
            responseData = resResult.data;
            if(responseData == null 
                    ||responseData.Equals(default(T)))
            {
                return (default,resResult.message);
            }
        }
        else
        {
            return default;
        }

        // 返回响应数据
        return (responseData, string.Empty);

        }
        catch (Exception ex)
        {
            // 处理请求异常
            Console.WriteLine($"请求失败：{ex.Message}");
            return (default, ex.Message); ;
        }
    }

    public static async Task<(T, string)> Post<T>(string url, MultipartFormDataContent formData)
    {
        
        using var client = new HttpClient();

        client.Timeout = new TimeSpan(30 * TimeSpan.TicksPerSecond);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", UserINISettings.Instance.Token.Value);
        
        HttpResponseMessage response;
        try
        {

            // 发送 POST 请求并传递 formData
            response = await client.PostAsync($"{Address}{url}", formData).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            // 处理请求异常
            Console.WriteLine($"请求失败：{ex.Message}");
            return default;
        }

        // 读取响应内容
        T responseData;

        if (response.IsSuccessStatusCode)
        {
            //var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            //var responseContent = Encoding.UTF8.GetString(bytes);
            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            
            var resResult = JsonSerializer.Deserialize<ResResult<T>>(responseContent);
            responseData = resResult.data;
            if(responseData == null
                    || responseData.Equals(default(T)))
                return (default,resResult.message);
        }
        else
        {
            return default;
        }

        // 返回响应数据
        return (responseData, string.Empty);
    }

    public static async Task<(T,string)> Get<T>(string url,int timeOut = 30)
    {
        try
        {
            using var client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", UserINISettings.Instance.Token.Value);
            // 发送 GET 请求并获取响应
            HttpResponseMessage response;

            client.Timeout = new TimeSpan(timeOut * TimeSpan.TicksPerSecond);
            response = await client.GetAsync($"{Address}{url}").ConfigureAwait(false);
        
            // 读取响应内容
            T responseData = default;
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var resResult = JsonSerializer.Deserialize<ResResult<T>>(responseContent);
                responseData = resResult.data;
                if (responseData == null)
                    return (default, resResult.message);
            }
            else
            {
                return (default(T), "网络错误");
            }

            // 返回响应数据
            return (responseData,string.Empty);
        }
        catch (Exception ex)
        {
            // 处理请求异常
            Console.WriteLine($"请求失败：{ex.Message}");
            return default;
        }
    }
    public static async Task<(bool, string)> DownLoad(string url, string outputPath, bool useToken = true)
    {
        using var client = new HttpClient();

        if (useToken)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", UserINISettings.Instance.Token.Value);
        }

        try
        {
            var fullUrl = new Uri(new Uri(Address.TrimEnd('/') + "/"), url.TrimStart('/'));
            Console.WriteLine($"下载地址: {fullUrl}");

            var response = await client.GetAsync(fullUrl).ConfigureAwait(false);

            Console.WriteLine($"状态码: {(int)response.StatusCode} {response.ReasonPhrase}");

            if (!response.IsSuccessStatusCode)
            {
                return (false, $"下载失败: {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            // 确保目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            await using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
            await contentStream.CopyToAsync(fileStream).ConfigureAwait(false);

            return (true, "文件下载成功");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发生错误：{ex.Message}");
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// 下载网络图片并保存为本地文件
    /// </summary>
    /// <param name="url">图片URL</param>
    /// <param name="saveDir">保存目录</param>
    /// <param name="fileName">重命名的文件名（不带扩展名）</param>
    /// <returns>保存后的完整路径</returns>
    public static async Task<string> DownloadImageAsync(string url, string saveDir, string fileName)
    {
        if (!Directory.Exists(saveDir))
        {
            Directory.CreateDirectory(saveDir);
        }

        using var httpClient = new HttpClient();

        try
        {
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsByteArrayAsync();

            // 获取扩展名（从 URL 或默认 jpg）
            //var ext = Path.GetExtension(url);
            //if (string.IsNullOrWhiteSpace(ext) || ext.Contains("?")) ext = ".jpg";

            var fullPath = Path.Combine(saveDir, fileName);
            await File.WriteAllBytesAsync(fullPath, content);

            Console.WriteLine($"图片已保存到：{fullPath}");
            return fullPath.Replace("\\", "/");
        }
        catch (Exception ex)
        {
            Console.WriteLine("图片下载失败：" + ex.Message);
            return null;
        }
    }

    public static async Task<bool> DownloadFileAsync(
        string downloadUrl,
        string localPath,
        Action<int, string>? onProgress = null)
    {
        try
        {
            using HttpClient httpClient = new HttpClient();

            TaskbarProgress.Instance.SetState(TaskbarProgress.TaskbarStates.Normal);

            using var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var contentStream = await response.Content.ReadAsStreamAsync();

            // ✅ 确保目录存在
            var directory = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None, 131072, true);

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            long totalRead = 0L;
            var buffer = new byte[131072];
            bool isMoreToRead = true;
            int lastProgress = 0;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            while (isMoreToRead)
            {
                int read = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length));
                if (read == 0)
                {
                    isMoreToRead = false;
                    continue;
                }

                await fileStream.WriteAsync(buffer.AsMemory(0, read));
                totalRead += read;

                if (totalBytes > 0)
                {
                    int progress = (int)((totalRead * 100) / totalBytes);
                    if (progress - lastProgress >= 1)
                    {
                        double seconds = stopwatch.Elapsed.TotalSeconds;
                        double kbSpeed = seconds > 0 ? totalRead / 1024d / seconds : 0;
                        string speedStr = kbSpeed >= 1024
                            ? $"{(kbSpeed / 1024):F2} MB/s"
                            : $"{kbSpeed:F2} KB/s";

                        onProgress?.Invoke(progress, speedStr);
                        TaskbarProgress.Instance.SetValue(progress, 100);

                        lastProgress = progress;
                    }
                }
            }

            // ✅ 下载完成后，清除任务栏进度
            TaskbarProgress.Instance.SetState(TaskbarProgress.TaskbarStates.NoProgress);
            return true;
        }
        catch (Exception ex)
        {
            // ❗ 异常时也清除进度，避免一直卡住
            TaskbarProgress.Instance.SetState(TaskbarProgress.TaskbarStates.Error);
            Console.WriteLine(ex.ToString());
            return false;
        }
    }


}
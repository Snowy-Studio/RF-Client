using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClientCore;
using ClientGUI;
using DTAConfig.OptionPanels;
using Rampastring.Tools;
/* !! We cannot use references to other projects or non-framework assemblies in this class, assembly loading events not hooked up yet !! */

namespace Ra2Client
{
    static class Program
    {
        static Program()
        {
            /* We have different binaries depending on build platform, but for simplicity
             * the target projects (DTA, TI, MO, YR) supply them all in a single download.
             * To avoid DLL hell, we load the binaries from different directories
             * depending on the build platform. */

            string startupPath = new FileInfo(Assembly.GetEntryAssembly().Location).Directory.Parent.Parent.FullName + Path.DirectorySeparatorChar;

            COMMON_LIBRARY_PATH = Path.Combine(startupPath, "Resources", "Binaries") + Path.DirectorySeparatorChar;

            SPECIFIC_LIBRARY_PATH = Path.Combine(startupPath, "Resources", "Binaries") + Path.DirectorySeparatorChar;

            // Set up DLL load paths as early as possible
            AssemblyLoadContext.Default.Resolving += DefaultAssemblyLoadContextOnResolving;
        }

        private static string COMMON_LIBRARY_PATH;
        private static string SPECIFIC_LIBRARY_PATH;

        //private static readonly string VerifyUrl = "https://www.yra2.com/v9/verify.txt";
        //private static readonly string BetaKeyCodeUrl = "https://www.yra2.com/v9/beta/key.txt";
        //private static readonly string DevKeyCodeUrl = "https://www.yra2.com/v9/dev/key.txt";

        //private static readonly string AuthKeyFileName = "auth.key";

        //private static readonly string LocalFallbackKeyCode = "f0f53a6656f0b01aa2019d4cf0ba1751801b8270";
        //private static readonly DateTime LocalFallbackKeyExpiry = new DateTime(2038, 1, 1, 0, 0, 0);

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Run(args);
        }

        private static void Run(string[] args)
        {
            CDebugView.SetDebugName("Ra2Client");

            bool noAudio = false;
            bool multipleInstanceMode = false;
            List<string> unknownStartupParams = new List<string>();

            for (int arg = 0; arg < args.Length; arg++)
            {
                string argument = args[arg].ToUpperInvariant();

                switch (argument)
                {
                    case "-NOAUDIO":
                        noAudio = true;
                        break;
                    case "-MULTIPLEINSTANCE":
                        multipleInstanceMode = true;
                        break;
                    case "-NOLOGO":
                        ProgramConstants.SkipLogo = true;
                        break;
                    default:
                        unknownStartupParams.Add(argument);
                        break;
                }
            }

            if (!Directory.Exists("Resources/Dynamicbg"))
                ProgramConstants.SkipLogo = true;

            var parameters = new StartupParams(noAudio, multipleInstanceMode, unknownStartupParams);

            //bool canStart = true;
            //int versionDigits = 0; // 默认值
            //try
            //{
            //    var result = Task.Run(async () => await CheckVersionFast().ConfigureAwait(false)).GetAwaiter().GetResult();
            //    canStart = result.canStart;
            //    versionDigits = result.versionDigits; // 获取版本状态码
            //}
            //catch (Exception ex)
            //{
            //    Logger.Log("版本检查失败: " + ex.Message);
            //    canStart = true;
            //    versionDigits = 0; // 默认状态码
            //}

            //if (!canStart)
            //{
            //    bool userCancelled = versionDigits < 0;
            //    int originalStatus = Math.Abs(versionDigits);

            //    if (userCancelled)
            //    {
            //        Logger.Log("用户取消了授权码输入，程序退出。");
            //        return;
            //    }

            //    if (originalStatus != 1 && originalStatus != 2)
            //    {
            //        if (originalStatus == 3)
            //        {
            //            MessageBox.Show(
            //                "当前版本已停止维护! 部分功能可能无法正常使用\n请及时到重聚未来官网 www.yra2.com 更新客户端以获得后续的技术支持",
            //                "版本确认",
            //                MessageBoxButtons.OK,
            //                MessageBoxIcon.Warning,
            //                MessageBoxDefaultButton.Button1,
            //                MessageBoxOptions.ServiceNotification
            //            );
            //        }
            //        else if (originalStatus == 4 || originalStatus == 5)
            //        {
            //            MessageBox.Show(
            //                "当前测试版本已停止维护! 部分功能可能无法正常使用\n请及时到重聚未来 官方QQ群/微信群 更新客户端以获得后续的技术支持\n\n(群号见重聚未来官网: www.yra2.com)",
            //                "版本确认",
            //                MessageBoxButtons.OK,
            //                MessageBoxIcon.Warning,
            //                MessageBoxDefaultButton.Button1,
            //                MessageBoxOptions.ServiceNotification
            //            );
            //        }
            //        else
            //        {
            //            MessageBox.Show(
            //               "当前版本已停止维护! 部分功能可能无法正常使用\n请及时到重聚未来官网 www.yra2.com 更新客户端以获得后续的技术支持",
            //               "警告",
            //               MessageBoxButtons.OK,
            //               MessageBoxIcon.Warning,
            //               MessageBoxDefaultButton.Button1,
            //               MessageBoxOptions.ServiceNotification
            //           );
            //        }
            //    }
            //    return;
            //}

            if (multipleInstanceMode)
            {
                // Proceed to client startup
                PreStartup.Initialize(parameters);
                return;
            }

            // We're a single instance application!
            // http://stackoverflow.com/questions/229565/what-is-a-good-pattern-for-using-a-global-mutex-in-c/229567
            // Global prefix means that the mutex is global to the machine
            string mutexId = FormattableString.Invariant($"Global{Guid.Parse("4C2EC0A0-94FB-4075-953D-8A3F62E490AA")}");
            using var mutex = new Mutex(false, mutexId, out _);
            bool hasHandle = false;

            try
            {
                try
                {
                    hasHandle = mutex.WaitOne(8000, false);
                    if (hasHandle == false)
                        throw new TimeoutException("Timeout waiting for exclusive access");
                }
                catch (AbandonedMutexException)
                {
                    hasHandle = true;
                }
                catch (TimeoutException)
                {
                    return;
                }

                // Proceed to client startup
                PreStartup.Initialize(parameters);
            }
            finally
            {
                if (hasHandle)
                    mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// 版本检测，3秒超时
        /// </summary>
        //private static async Task<(bool canStart, int versionDigits)> CheckVersionFast()
        //{
        //    using HttpClient client = new HttpClient();
        //    var verifyTask = client.GetStringAsync(VerifyUrl);
        //    var timeoutTask = Task.Delay(3000);

        //    var completedTask = await Task.WhenAny(verifyTask, timeoutTask).ConfigureAwait(false);

        //    if (completedTask == timeoutTask)
        //    {
        //        Logger.Log("Warning: Version list check timed out!");
        //        return (true, 0);
        //    }

        //    string content;
        //    try
        //    {
        //        content = await verifyTask.ConfigureAwait(false);
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Log("版本检查异常: " + ex.Message);
        //        return (true, 0);
        //    }

        //    var parseResult = await ParseVersionContent(content, client).ConfigureAwait(false);
        //    return parseResult; // 返回解析结果
        //}

        /// <summary>
        /// 获取主程序的版本号字符串
        /// </summary>
        //private static string GetMainProgramVersion()
        //{
        //    try
        //    {
        //        return Assembly.GetAssembly(typeof(Program)).GetName().Version.ToString();
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Log("获取主程序版本失败: " + ex.Message);
        //        // 返回一个默认值，避免异常
        //        return "0.0.0";
        //    }
        //}

        /// <summary>
        /// 检查服务器返回内容，决定是否允许启动以及是否弹窗或需要 Code
        /// </summary>
        /// <param name="content">从服务器获取的文本内容</param>
        /// <param name="httpClient">用于发起 HTTP 请求的客户端</param>
        /// <returns>(是否允许启动, 版本状态码)</returns>
        //private static async Task<(bool canStart, int versionDigits)> ParseVersionContent(string content, HttpClient httpClient)
        //{
        //    string currentVersion = GetMainProgramVersion();
        //    string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        //    foreach (string line in lines)
        //    {
        //        string[] parts = line.Split(',');
        //        if (parts.Length == 2)
        //        {
        //            string version = parts[0].Trim();
        //            if (version.Equals(currentVersion, StringComparison.OrdinalIgnoreCase))
        //            {
        //                if (int.TryParse(parts[1].Trim(), out int status))
        //                {
        //                    switch (status)
        //                    {
        //                        case 0:
        //                            return (true, status);
        //                        case 1:
        //                        case 2:
        //                        case 4:
        //                        case 5:
        //                            string storedKeyCode = ReadStoredKeyCode();
        //                            if (!string.IsNullOrEmpty(storedKeyCode))
        //                            {
        //                                // 如果存在 auth.key，验证其有效性
        //                                string keyCodeUrl = status switch
        //                                {
        //                                    1 or 4 => BetaKeyCodeUrl,
        //                                    2 or 5 => DevKeyCodeUrl,
        //                                    _ => BetaKeyCodeUrl // 默认，理论上不会到达
        //                                };
        //                                bool isValid = await CheckKeyCodeOnlineAsync(httpClient, keyCodeUrl, storedKeyCode).ConfigureAwait(false);
        //                                if (isValid)
        //                                {
        //                                    // 本地授权码有效，直接允许启动
        //                                    Logger.Log($"版本 {currentVersion} 状态为 {status}，使用本地有效的授权码启动。");
        //                                    return (true, status);
        //                                }
        //                                else
        //                                {
        //                                    // 本地授权码无效或已失效
        //                                    Logger.Log($"版本 {currentVersion} 状态为 {status}，本地授权码无效或已失效。");
        //                                }
        //                            }
        //                            else
        //                            {
        //                                // auth.key 文件不存在
        //                                Logger.Log($"版本 {currentVersion} 状态为 {status}，未找到本地授权码文件。");
        //                            }

        //                            var (codeValid, userCancelled) = await ValidateKeyCodeAsync(httpClient, status).ConfigureAwait(false);
        //                            if (!codeValid)
        //                            {
        //                                Logger.Log($"版本 {currentVersion} 状态为 {status}，Code 验证失败{(userCancelled ? "或用户取消" : "")}，阻止启动。");
        //                                return (false, userCancelled ? -status : status);
        //                            }
        //                            if (status == 4 || status == 5)
        //                            {
        //                                return (true, status);
        //                            }
        //                            return (true, status);
        //                        case 3:
        //                            return (true, status);
        //                        default:
        //                            Logger.Log($"版本 {currentVersion} 状态码 {status} 未知，阻止启动。");
        //                            return (false, status);
        //                    }
        //                }
        //                else
        //                {
        //                    Logger.Log($"版本 {currentVersion} 对应的状态码 '{parts[1]}' 无效。");
        //                    continue;
        //                }
        //            }
        //        }
        //    }

        //    Logger.Log($"版本 {currentVersion} 未在允许列表中找到，阻止启动。");
        //    return (false, 0);
        //}


        /// <summary>
        /// 验证用户输入或本地存储的授权码
        /// </summary>
        /// <param name="httpClient">用于发起 HTTP 请求的客户端</param>
        /// <param name="status">版本状态码 (1, 2, 4, 5)</param>
        /// <returns>(授权码是否有效, 是否是用户取消操作)</returns>
        //private static async Task<(bool isValid, bool isCancelled)> ValidateKeyCodeAsync(HttpClient httpClient, int status)
        //{
        //    string keyCodeUrl = status switch
        //    {
        //        1 or 4 => BetaKeyCodeUrl,
        //        2 or 5 => DevKeyCodeUrl,
        //        _ => throw new ArgumentException("Invalid status for key code validation", nameof(status))
        //    };

        //    string userInputCode = PromptForKeyCode(status);
        //    if (string.IsNullOrEmpty(userInputCode))
        //    {
        //        if (status == 1 || status == 2)
        //        {
        //            MessageBox.Show(
        //                "需要有效的授权码才能启动此版本。\n启动已取消。",
        //                "启动阻止",
        //                MessageBoxButtons.OK,
        //                MessageBoxIcon.Warning,
        //                MessageBoxDefaultButton.Button1,
        //                MessageBoxOptions.ServiceNotification
        //            );
        //        }
        //        return (false, true);
        //    }

        //    if (userInputCode.Equals(LocalFallbackKeyCode, StringComparison.OrdinalIgnoreCase))
        //    {
        //        if (DateTime.Now <= LocalFallbackKeyExpiry)
        //        {
        //            Logger.Log("用户输入了本地备用授权码且在有效期内。");
        //            SaveKeyCode(userInputCode);
        //            return (true, false);
        //        }
        //        else
        //        {
        //            Logger.Log("用户输入了本地备用授权码，但已过期。");
        //            MessageBox.Show(
        //                "输入的备用授权码已过期。\n请获取正确的授权码后重试。",
        //                "授权码无效",
        //                MessageBoxButtons.OK,
        //                MessageBoxIcon.Error,
        //                MessageBoxDefaultButton.Button1,
        //                MessageBoxOptions.ServiceNotification
        //            );
        //            return (false, false);
        //        }
        //    }

        //    bool isInputValid = await CheckKeyCodeOnlineAsync(httpClient, keyCodeUrl, userInputCode).ConfigureAwait(false);
        //    if (isInputValid)
        //    {
        //        SaveKeyCode(userInputCode);
        //        Logger.Log("用户输入的授权码验证成功并已保存。");
        //        return (true, false);
        //    }
        //    else
        //    {
        //        Logger.Log("用户输入的授权码验证失败。");
        //        MessageBox.Show(
        //            "输入的授权码无效或已过期。\n请获取正确的授权码后重试。",
        //            "授权码无效",
        //            MessageBoxButtons.OK,
        //            MessageBoxIcon.Error,
        //            MessageBoxDefaultButton.Button1,
        //            MessageBoxOptions.ServiceNotification
        //        );
        //        return (false, false);
        //    }
        //}

        /// <summary>
        /// 从在线文件检查授权码是否有效且未过期
        /// </summary>
        /// <param name="httpClient">HTTP 客户端</param>
        /// <param name="keyCodeUrl">包含授权码列表的 URL</param>
        /// <param name="inputCode">用户提供的授权码</param>
        /// <returns>授权码是否有效</returns>
        //private static async Task<bool> CheckKeyCodeOnlineAsync(HttpClient httpClient, string keyCodeUrl, string inputCode)
        //{
        //    try
        //    {
        //        string content = await httpClient.GetStringAsync(keyCodeUrl).ConfigureAwait(false);
        //        string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        //        foreach (string line in lines)
        //        {
        //            string[] codeParts = line.Split(',');
        //            if (codeParts.Length >= 2)
        //            {
        //                string code = codeParts[0].Trim();
        //                string expiryStr = codeParts[1].Trim();

        //                if (code.Equals(inputCode, StringComparison.OrdinalIgnoreCase))
        //                {
        //                    if (DateTime.TryParseExact(expiryStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime expiryDate))
        //                    {
        //                        if (DateTime.Now <= expiryDate)
        //                        {
        //                            return true;
        //                        }
        //                        else
        //                        {
        //                            Logger.Log($"授权码 {inputCode} 已过期 (过期时间: {expiryDate})。");
        //                            return false;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        Logger.Log($"无法解析授权码 {inputCode} 的过期时间: {expiryStr}");
        //                        return false;
        //                    }
        //                }
        //            }
        //        }
        //        Logger.Log($"在线列表中未找到授权码 {inputCode}。");
        //        return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Log($"检查在线授权码时发生异常: {ex.Message}");
        //        return false;
        //    }
        //}

        /// <summary>
        /// 读取本地存储的授权码
        /// </summary>
        /// <returns>授权码，如果不存在或读取失败则返回 null 或 empty</returns>
        //private static string ReadStoredKeyCode()
        //{
        //    try
        //    {
        //        string filePath = Path.Combine(GetStartupPath(), AuthKeyFileName);
        //        if (File.Exists(filePath))
        //        {
        //            string code = File.ReadAllText(filePath).Trim();
        //            if (!string.IsNullOrEmpty(code))
        //            {
        //                return code;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Log($"读取本地授权码文件失败: {ex.Message}");
        //    }
        //    return null;
        //}

        /// <summary>
        /// 保存授权码到本地文件
        /// </summary>
        /// <param name="keyCode">要保存的授权码</param>
        //private static void SaveKeyCode(string keyCode)
        //{
        //    try
        //    {
        //        string filePath = Path.Combine(GetStartupPath(), AuthKeyFileName);
        //        File.WriteAllText(filePath, keyCode, Encoding.UTF8);
        //        Logger.Log($"授权码已保存到 {filePath}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Log($"保存授权码到文件失败: {ex.Message}");
        //        MessageBox.Show(
        //            "授权码保存失败，下次启动可能需要重新输入。",
        //            "保存失败",
        //            MessageBoxButtons.OK,
        //            MessageBoxIcon.Error
        //        );
        //    }
        //}

        /// <summary>
        /// 获取程序启动目录
        /// </summary>
        /// <returns>启动目录路径</returns>
        private static string GetStartupPath()
        {
            return new FileInfo(Assembly.GetEntryAssembly().Location).Directory.FullName;
        }

        /// <summary>
        /// 弹出对话框提示用户输入授权码
        /// </summary>
        /// <param name="status">版本状态码</param>
        /// <returns>用户输入的授权码，如果取消则返回 null 或 empty</returns>
        //private static string PromptForKeyCode(int status)
        //{
        //    string message = status switch
        //    {
        //        1 => "此为 Beta 测试版本，需要有效的 Beta 授权码才能启动。\n请输入您的授权码：",
        //        2 => "此为 Dev 开发版本，需要有效的 Dev 授权码才能启动。\n请输入您的授权码：",
        //        4 => "此为特殊 Beta 测试版本，需要有效的 Beta 授权码并确认后才能启动。\n请输入您的授权码：",
        //        5 => "此为特殊 Dev 开发版本，需要有效的 Dev 授权码并确认后才能启动。\n请输入您的授权码：",
        //        _ => "此版本需要授权码才能启动。\n请输入您的授权码："
        //    };

        //    if (Application.OpenForms.Count > 0)
        //    {
        //        return Application.OpenForms[0].Invoke((Func<string>)(() => ShowInputDialog(message))) as string;
        //    }
        //    else
        //    {
        //        return ShowInputDialog(message);
        //    }
        //}

        /// <summary>
        /// 显示一个简单的输入对话框 (WinForm)
        /// </summary>
        /// <param name="prompt">提示信息</param>
        /// <returns>用户输入的文本，如果取消则返回 null</returns>
        //private static string ShowInputDialog(string prompt)
        //{
        //    Form promptForm = new Form()
        //    {
        //        Width = 520,
        //        Height = 180,
        //        FormBorderStyle = FormBorderStyle.FixedDialog,
        //        Text = "请输入授权码",
        //        StartPosition = FormStartPosition.CenterScreen,
        //        MaximizeBox = false,
        //        MinimizeBox = false,
        //    };
        //    Label textLabel = new Label()
        //    {
        //        Left = 20,
        //        Top = 20,
        //        Width = 460,
        //        Text = prompt,
        //        AutoSize = true, // 允许标签根据文本内容自动调整大小
        //        TextAlign = System.Drawing.ContentAlignment.TopLeft // 文本顶部左对齐
        //    };
        //    // 计算 TextBox 的位置，确保在 Label 下方且有足够的间距
        //    int textBoxTop = textLabel.Bottom + 15;
        //    TextBox textBox = new TextBox()
        //    {
        //        Left = 20,
        //        Top = textBoxTop,
        //        Width = 460,
        //        Height = 25
        //    };
        //    // 计算按钮的位置，确保在 TextBox 下方且有足够的间距
        //    int buttonTop = textBox.Bottom + 15;
        //    Button confirmation = new Button()
        //    {
        //        Text = "确定",
        //        Left = 310,
        //        Width = 75,
        //        Top = buttonTop,
        //        DialogResult = DialogResult.OK
        //    };
        //    Button cancel = new Button()
        //    {
        //        Text = "取消",
        //        Left = 405,
        //        Width = 75,
        //        Top = buttonTop,
        //        DialogResult = DialogResult.Cancel
        //    };
        //    confirmation.Click += (sender, e) => { promptForm.Close(); };
        //    cancel.Click += (sender, e) => { promptForm.Close(); };
        //    promptForm.Controls.Add(textBox);
        //    promptForm.Controls.Add(confirmation);
        //    promptForm.Controls.Add(cancel);
        //    promptForm.Controls.Add(textLabel);
        //    promptForm.AcceptButton = confirmation; // 回车键触发确定
        //    promptForm.CancelButton = cancel;       // ESC键触发取消

        //    int totalContentHeight = textLabel.Height + 15 + textBox.Height + 15 + confirmation.Height;
        //    int desiredFormHeight = 20 + totalContentHeight + 20 + 30;
        //    promptForm.Height = Math.Max(promptForm.Height, desiredFormHeight);

        //    promptForm.Width = Math.Max(promptForm.Width, 500);

        //    DialogResult result = promptForm.ShowDialog();
        //    if (result == DialogResult.OK)
        //    {
        //        return textBox.Text.Trim();
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

        private static Assembly DefaultAssemblyLoadContextOnResolving(AssemblyLoadContext assemblyLoadContext, AssemblyName assemblyName)
        {
            if (assemblyName.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                return null;

            var commonFileInfo = new FileInfo(Path.Combine(COMMON_LIBRARY_PATH, FormattableString.Invariant($"{assemblyName.Name}.dll")));

            if (commonFileInfo.Exists)
                return assemblyLoadContext.LoadFromAssemblyPath(commonFileInfo.FullName);

            var specificFileInfo = new FileInfo(Path.Combine(SPECIFIC_LIBRARY_PATH, FormattableString.Invariant($"{assemblyName.Name}.dll")));

            if (specificFileInfo.Exists)
                return assemblyLoadContext.LoadFromAssemblyPath(specificFileInfo.FullName);

            return null;
        }
    }
}
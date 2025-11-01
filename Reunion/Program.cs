using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Reunion
{
    internal class Program
    {
        private const string Resources = "Resources";
        private const string Binaries = "Binaries";

        private const string LicenseFile = "License-GPLv3.txt";
        private const string RequiredFile = "使用前必读.txt";
        private const string FreeFile = "本游戏完全免费，祝倒卖的寿比昙花.txt";
        private const string AntiCheatFile = "Reunion Anti-Cheat.dll";

        private const string LicenseFileHash = "dc447a64136642636d7aa32e50c76e2465801c5f";
        private const string RequiredFileHash = "1dbb412e60371c4b5aff6a8609df1d1b0446e4517b46ec4a4b99a23b132d9f86";
        private const string FreeFileHash = "bf46a8a320bf587de81190854b210fa2";
        private const string AntiCheatFileHash = "77d7dcd1448a96696cb1ba494f1c9e0d920a32dcbe91546da9363e06c6778ee6892ad41c23c4e527088f608956f6c91b1481bad4d4365c70b8f23ac310fabb62";

        private static readonly string dotnetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet");
        private static string sharedPath = @"shared\Microsoft.WindowsDesktop.App";

        private static string[] Args;

        static void Main(string[] args)
        {
            Args = args;

            if (!CheckRequiredFile())
            {
                return;
            }

            StartProcess(GetClientProcessPath("Ra2Client.dll"));
        }

        private static bool CheckRequiredFile()
        {
            try
            {
                if (!File.Exists(RequiredFile) || !File.Exists(FreeFile) || !File.Exists(LicenseFile) || !File.Exists(AntiCheatFile))
                {
                    MessageBox.Show("发现未知错误，请联系重聚未来制作组", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                if (!ComputeFileSHA256(RequiredFile).Equals(RequiredFileHash, StringComparison.OrdinalIgnoreCase) || !ComputeFileMD5(FreeFile).Equals(FreeFileHash, StringComparison.OrdinalIgnoreCase) || !ComputeFileSHA1(LicenseFile).Equals(LicenseFileHash, StringComparison.OrdinalIgnoreCase) || !ComputeFileSHA512(AntiCheatFile).Equals(AntiCheatFileHash, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("发现未知错误，请联系重聚未来制作组", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"文件校验出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 计算文件的MD5哈希值
        /// </summary>
        private static string ComputeFileMD5(string filePath)
        {
            using (FileStream stream = File.OpenRead(filePath))
            using (MD5 md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// 计算文件的SHA1哈希值
        /// </summary>
        private static string ComputeFileSHA1(string filePath)
        {
            using (FileStream stream = File.OpenRead(filePath))
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] hashBytes = sha1.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// 计算文件的SHA256哈希值
        /// </summary>
        private static string ComputeFileSHA256(string filePath)
        {
            using (FileStream stream = File.OpenRead(filePath))
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// 计算文件的SHA384哈希值
        /// </summary>
        private static string ComputeFileSHA384(string filePath)
        {
            using (FileStream stream = File.OpenRead(filePath))
            using (SHA384 sha384 = SHA384.Create())
            {
                byte[] hashBytes = sha384.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// 计算文件的SHA512哈希值
        /// </summary>
        private static string ComputeFileSHA512(string filePath)
        {
            using (FileStream stream = File.OpenRead(filePath))
            using (SHA512 sha512 = SHA512.Create())
            {
                byte[] hashBytes = sha512.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// 计算文件的RIPEMD160哈希值
        /// </summary>
        public static string ComputeFileRipeMd160(string filePath)
        {
            using (FileStream stream = File.OpenRead(filePath))
            using (RIPEMD160 ripeMd160 = new RIPEMD160Managed())
            {
                byte[] hashBytes = ripeMd160.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        private static string GetClientProcessPath(string file) => Path.Combine(Resources, Binaries, file);

        private static void StartProcess(string relPath)
        {
            try
            {
                var dotnetHost = CheckAndRetrieveDotNetHost();
                if (dotnetHost == null)
                {
                    string arch = RuntimeInformation.OSArchitecture.ToString().ToLower();
                    string message;
                    string url;

                    switch (arch)
                    {
                        case "x86":
                            message = "您必须安装 .NET 桌面运行时来运行此应用程序\n\n架构: x86\n运行时版本: 6.0.36\n\n如果不能正常跳转到下载地址, 请使用此地址手动下载x86运行时: https://url.yra2.com/net61\n您现在想下载吗? (点击确定即可自动下载)";
                            url = $"https://url.yra2.com/net61";
                            break;
                        case "x64":
                            message = "您必须安装 .NET 桌面运行时来运行此应用程序\n\n架构: x64\n运行时版本: 6.0.36\n\n如果不能正常跳转到下载地址, 请使用此地址手动下载x86运行时: https://url.yra2.com/net60\n您现在想下载吗? (点击确定即可自动下载)";
                            url = $"https://url.yra2.com/net60";
                            break;
                        case "arm64":
                            message = "您必须安装 .NET 桌面运行时来运行此应用程序\n\n架构: Arm64\n运行时版本: 6.0.36\n\n如果不能正常跳转到下载地址, 请使用此地址手动下载x86运行时: https://url.yra2.com/net62\n您现在想下载吗? (点击确定即可自动下载)";
                            url = $"https://url.yra2.com/net62";
                            break;
                        default:
                            message = "您必须安装 .NET 桌面运行时来运行此应用程序\n\n架构: Unknown\n运行时版本: 6.0.36\n\n应用程序貌似与您的系统不兼容, 请尝试更换系统";
                            url = "https://dotnet.microsoft.com/zh-cn/download/dotnet/6.0";
                            break;
                    }

                    var result = MessageBox.Show(message, "错误: 缺少桌面运行时环境", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                    if (result == DialogResult.OK)
                    {
                        Process.Start(url);
                    }
                    Environment.Exit(1);
                    return;
                }

                string absPath = Path.Combine(Environment.CurrentDirectory, relPath);
                if (!File.Exists(absPath))
                {
                    _ = MessageBox.Show($"客户端入口 ({relPath}) 不存在!", "客户端启动异常");
                    Environment.Exit(3);
                }

                OperatingSystem os = Environment.OSVersion;

                // Required on Win7 due to W^X causing issues there.
                if (os.Platform == PlatformID.Win32NT && os.Version.Major == 6 && os.Version.Minor == 1)
                {
                    Environment.SetEnvironmentVariable("DOTNET_EnableWriteXorExecute", "0");
                }

                foreach (var zip in Directory.GetFiles("./", "Updater*.7z"))
                {
                    ZIP.SevenZip.ExtractWith7Zip(zip, "./", needDel: true);
                }

                var arguments = $"\"{absPath}\" {GetArguments(Args)}";

                Console.WriteLine(dotnetHost);
                Console.WriteLine(arguments);

                Process p = Process.Start(new ProcessStartInfo
                {
                    FileName = dotnetHost,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    Verb = "runas",
                    WorkingDirectory = Environment.CurrentDirectory // 指定运行目录
                });
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"启动客户端异常：{ex.Message}", "客户端启动异常");
                Environment.Exit(4);
            }
        }

        private static string GetArguments(string[] args)
        {
            string result = string.Empty;

            // 使用 foreach 代替 LINQ 的 Select
            foreach (var arg in args)
            {
                result += $"\"{arg}\" ";
            }

            return result.Trim(); // 去掉最后的空格
        }

        private static string CheckAndRetrieveDotNetHost()
        {
            string dotnetExePath = Path.Combine(dotnetPath, "dotnet.exe");
            string fullSharedPath = Path.Combine(dotnetPath, sharedPath);

            var result = TryFindDotNet(dotnetExePath, fullSharedPath);
            if (result != null) return result;

            if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
            {
                string altDotnetPath = Path.Combine(dotnetPath, "x64");
                string altDotnetExePath = Path.Combine(altDotnetPath, "dotnet.exe");
                string altFullSharedPath = Path.Combine(altDotnetPath, sharedPath);

                result = TryFindDotNet(altDotnetExePath, altFullSharedPath);
                if (result != null) return result;
            }

            return null;
        }

        private static string TryFindDotNet(string dotnetExePath, string fullSharedPath)
        {
            if (!File.Exists(dotnetExePath) || !Directory.Exists(fullSharedPath))
                return null;

            var dir = FindDotNetInPath(fullSharedPath);
            return dir != null ? dotnetExePath : null;
        }

        private static string FindDotNetInPath(string path)
        {
            if (Directory.Exists(path))
            {
                var directories = Directory.GetDirectories(path);
                foreach (var dir in directories)
                {
                    var folderName = Path.GetFileName(dir);
                    if (Version.TryParse(folderName, out var version))
                    {
                        if (version.Major == 6 && (version.Minor > 0 || version.Build >= 12))
                        {
                            return dir;
                        }
                    }
                }
            }
            return null;
        }
    }
}
using Fun;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ClientUpdater;

/// <summary>
/// 更新器主程序.
/// </summary>
internal sealed class Program
{
    private const int MOVEFILEDELAYUNTILREBOOT = 0x00000004;
    private const int MOVEFILEREPLACEEXISTING = 0x00000001;

    private static ConsoleColor defaultColor = ConsoleColor.White;

    // private static StreamWriter errorWriter;

    // P/Invoke 声明，用于安排文件在下次重启时替换
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, int dwFlags);

    /// <summary>
    /// 更新程序 For Ra2Client Reunion.
    /// </summary>
    /// <param name="args">可执行程序路径根目录.</param>
    private static void Main(string[] args)
    {
        foreach (var arg in args)
        {
            Write(arg);
        }

        defaultColor = Console.ForegroundColor;

        var errorLogPath = "Client/update_log.txt";
        var logDirectory = Path.GetDirectoryName(errorLogPath);

        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        var fileWriter = new StreamWriter(errorLogPath, append: true, Encoding.UTF8);

        using (var dualWriter = new DualWriter(Console.Out, fileWriter))
        {
            Console.SetOut(dualWriter);
        }

        try
        {
            Write("Ra2Client更新器", ConsoleColor.Green);
            Write(string.Empty);

            // 调试使用的参数
            // args = new string[] { "Ra2Client.dll", @"D:\RF-Client\Bin" };
            if (args.Length < 2 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]) ||
                !SafePath.GetDirectory(args[1].Replace("\"", null, StringComparison.OrdinalIgnoreCase)).Exists)
            {
                Write("无效参数!", ConsoleColor.Red);
                Write("格式: <client_executable_name> <base_directory>");
                Write(string.Empty);
                Write("按任意键退出更新器.");
                Console.ReadKey();
                Environment.Exit(1);
            }
            else
            {
                FileInfo clientExecutable = SafePath.GetFile(args[0]);
                DirectoryInfo baseDirectory = SafePath.GetDirectory(args[1].Replace("\"", null, StringComparison.OrdinalIgnoreCase));

                Write("根目录: " + baseDirectory.FullName);
                Write("正在等待客户端(" + clientExecutable.Name + ")退出...");

                string clientMutexId = FormattableString.Invariant($"Global{Guid.Parse("4C2EC0A0-94FB-4075-953D-8A3F62E490AA")}");
                using var clientMutex = new Mutex(false, clientMutexId, out _);

                try
                {
                    clientMutex.WaitOne(-1, false);
                }
                catch (AbandonedMutexException)
                {
                }

                DirectoryInfo updaterDirectory = SafePath.GetDirectory(baseDirectory.FullName, "Tmp");
                if (!updaterDirectory.Exists)
                {
                    Write($"{updaterDirectory.Name} 目录不存在!", ConsoleColor.Red);
                    Write("按任意键退出更新器.");
                    Console.ReadKey();
                    Environment.Exit(1);
                }
                else
                {
                    try
                    {
                        // 递归清空目录
                        ClearDirectory(updaterDirectory);
                        Write($"已清空 {updaterDirectory.Name} 目录.", ConsoleColor.Yellow);
                    }
                    catch (IOException ex)
                    {
                        Write($"清空目录失败: {ex.Message}", ConsoleColor.Red);

                        // Write($"请关闭窗口，并手动清空", ConsoleColor.Red);
                        // Console.ReadKey();
                        // return;
                    }
                }

                Write("开始更新文件.", ConsoleColor.Green);

                IEnumerable<FileInfo> files = updaterDirectory.EnumerateFiles("*", SearchOption.AllDirectories);

                // Console.ReadKey();
                // foreach (var file in files)
                // {
                //     Write(file.FullName, ConsoleColor.Green);
                // }
                FileInfo executableFile = SafePath.GetFile(Assembly.GetExecutingAssembly().Location);
                FileInfo relativeExecutableFile = SafePath.GetFile(executableFile.FullName[baseDirectory.FullName.Length..]);

#pragma warning disable CA1851 // “IEnumerable”集合可能的多个枚举
                FileInfo delUpdateFile = files.FirstOrDefault(file => file.Name.Equals("delUpdate", StringComparison.OrdinalIgnoreCase));
#pragma warning restore CA1851 // “IEnumerable”集合可能的多个枚举
                if (delUpdateFile != null)
                {
                    DeleteListedFiles(baseDirectory, delUpdateFile);
                }

                bool bUpdateSuc = true;
                const int defaultMaxRetryCount = 60;
                const int retryDelay = 1000; // 1秒

                // 对每个文件进行更新操作
#pragma warning disable CA1851 // “IEnumerable”集合可能的多个枚举
                foreach (FileInfo fileInfo in files)
                {
                    FileInfo relativeFileInfo = SafePath.GetFile(fileInfo.FullName[updaterDirectory.FullName.Length..]);
                    AssemblyName[] assemblies = Assembly.LoadFrom(executableFile.FullName).GetReferencedAssemblies();

                    // 检查文件是否为当前更新程序或其依赖项
                    if (relativeFileInfo.ToString()[..^relativeFileInfo.Extension.Length].Equals(
                            relativeExecutableFile.ToString()[..^relativeExecutableFile.Extension.Length], StringComparison.OrdinalIgnoreCase)
                        || relativeFileInfo.ToString()[..^relativeFileInfo.Extension.Length]
                            .Equals(SafePath.CombineFilePath("Resources", Path.GetFileNameWithoutExtension(relativeExecutableFile.Name)), StringComparison.OrdinalIgnoreCase))
                    {
                        Write($"跳过 {nameof(ClientUpdater)} 文件 {relativeFileInfo}");
                        continue;
                    }
                    else if (assemblies.Any(q => relativeFileInfo.ToString()[..^relativeFileInfo.Extension.Length].Equals(q.Name, StringComparison.OrdinalIgnoreCase))
                             || assemblies.Any(q => relativeFileInfo.ToString()[..^relativeFileInfo.Extension.Length]
                                 .Equals(SafePath.CombineFilePath("Resources", q.Name), StringComparison.OrdinalIgnoreCase)))
                    {
                        Write($"跳过 {nameof(ClientUpdater)} 依赖 {relativeFileInfo}");
                        continue;
                    }
                    else
                    {
                        int maxRetry = defaultMaxRetryCount;

                        FileInfo targetFile = SafePath.GetFile(baseDirectory.FullName, relativeFileInfo.ToString());
                        Write($"更新文件 -> {relativeFileInfo}");
                        if (!CopyFileWithRetry(fileInfo, targetFile, maxRetry, retryDelay))
                        {
                            Write($"文件 {relativeFileInfo} 更新失败，超过最大重试次数.", ConsoleColor.Red);
                            bUpdateSuc = false;
                            break;
                        }
                    }
                }
#pragma warning restore CA1851 // “IEnumerable”集合可能的多个枚举

                if (updaterDirectory.Exists)
                {
                    try
                    {
                        Directory.Delete(updaterDirectory.FullName, true);
                    }
                    catch (IOException)
                    {
                        Write("删除临时目录失败", ConsoleColor.Yellow);
                    }
                }

                if (bUpdateSuc)
                {
                    Write("文件已经全部更新成功. 正在启动主程序..", ConsoleColor.Green);
                }
                else
                {
                    Write("更新失败");
                }

                // 直接通过Ra2Client.dll启动
                // string launcherExe = clientExecutable.Name;
                // FileInfo launcherExeFile = SafePath.GetFile(baseDirectory.FullName, "Resources", "Binaries", launcherExe);

                // if (launcherExeFile.Exists)
                // {
                //     Write("发现启动程序: " + launcherExeFile.FullName, ConsoleColor.Green);

                    // string strDotnet = @"C:\Program Files\dotnet\dotnet.exe";
                //     using var process = Process.Start(new ProcessStartInfo
                //     {
                //         FileName = strDotnet,
                //         Arguments = "\"" + launcherExeFile.FullName + "\"",
                //         CreateNoWindow = true,
                //         UseShellExecute = false,
                //     });
                // }
                // else
                // {
                //     Write($"当前启动主程序：{launcherExeFile.FullName}");
                //     Write("没有找到启动主程序，更新器将不再进行任何程序的启动任务.", ConsoleColor.Yellow);
                //     Write("按任意键退出更新器.");
                //     Console.ReadKey();
                //     Environment.Exit(1);
                // }

                // 通过Reunion.exe启动
                string reunionExeName = "Reunion.exe";
                FileInfo reunionExeFile = SafePath.GetFile(baseDirectory.FullName, reunionExeName);

                if (reunionExeFile.Exists)
                {
                    Write("发现启动程序: " + reunionExeFile.FullName, ConsoleColor.Green);

                    using var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = reunionExeFile.FullName,
                    });
                }
                else
                {
                    Write($"当前启动主程序：{reunionExeFile.FullName}");
                    Write("没有找到启动主程序，更新器将不再进行任何程序的启动任务.", ConsoleColor.Yellow);
                    Write("按任意键退出更新器.");
                    Console.ReadKey();
                    Environment.Exit(1);
                }
            }
        }
        catch (IOException ex)
        {
            Write("An error occured during the Launcher Updater's operation.", ConsoleColor.Red);
            Write($"Returned error was: {ex}");
            Write(string.Empty);
            Write("If you were updating a game, please try again. If the problem continues, contact the staff for support.");
            Write("Press any key to exit.");
            Console.ReadKey();
            Environment.Exit(1);
        }

        // errorWriter.Close();
    }

    /// <summary>
    /// 复制文件并重试.
    /// </summary>
    /// <param name="sourceFile">源文件信息.</param>
    /// <param name="destFile">目标文件信息.</param>
    /// <param name="maxRetryCount">最大重试次数.</param>
    /// <param name="retryDelay">重试延迟(ms).</param>
    /// <returns>复制成功返回 true，否则 false.</returns>
    private static bool CopyFileWithRetry(FileInfo sourceFile, FileInfo destFile, int maxRetryCount, int retryDelay)
    {
        int retry = 0;
        while (retry < maxRetryCount)
        {
            try
            {
                // 如果目标文件存在，先检查是否被占用
                if (File.Exists(destFile.FullName))
                {
                    FileInfo targetInfo = new FileInfo(destFile.FullName);
                    if (IsFileInUse(targetInfo))
                    {
                        Write($"目标文件 {destFile.Name} 正在使用中，等待1秒后重试...", ConsoleColor.Yellow);
                        retry++;
                        Thread.Sleep(retryDelay);
                        continue;
                    }
                    else
                    {
                        File.SetAttributes(destFile.FullName, FileAttributes.Normal);
                    }
                }

                // 检查源文件是否被占用
                if (IsFileInUse(sourceFile))
                {
                    Write($"源文件 {sourceFile.Name} 被占用，等待1秒后重试...", ConsoleColor.Yellow);
                    retry++;
                    Thread.Sleep(retryDelay);
                    continue;
                }

                if (!destFile.Directory.Exists)
                {
                    destFile.Directory.Create();
                }

                // 尝试复制文件
                sourceFile.CopyTo(destFile.FullName, true);
                return true;
            }
            catch (IOException ex)
            {
                Write($"更新文件失败: {ex}", ConsoleColor.Yellow);
                retry++;
                Thread.Sleep(retryDelay);
            }
        }

        // 如果直接复制失败，则尝试安排下次重启时更新
        Write($"无法直接更新 {sourceFile.Name}，尝试安排下次重启时更新...", ConsoleColor.Yellow);
        if (ScheduleFileReplacement(sourceFile.FullName, destFile.FullName))
        {
            Write($"已安排 {sourceFile.Name} 在重启后更新.", ConsoleColor.Yellow);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 调用 MoveFileEx 安排文件在下次重启时替换.
    /// </summary>
    /// <param name="source">更新文件的完整路径.</param>
    /// <param name="target">目标文件的完整路径.</param>
    /// <returns>安排成功返回 true, 否则 false.</returns>
    private static bool ScheduleFileReplacement(string source, string target)
    {
        try
        {
            // 将更新文件复制一份到与目标文件同目录下的临时文件
            string tempPath = Path.Combine(Path.GetDirectoryName(target), Path.GetFileName(target) + ".upd");
            File.Copy(source, tempPath, true);

            // 安排在重启时将临时文件移动到目标位置（替换现有文件）
            if (MoveFileEx(tempPath, target, MOVEFILEDELAYUNTILREBOOT | MOVEFILEREPLACEEXISTING))
            {
                return true;
            }
            else
            {
                int err = Marshal.GetLastWin32Error();
                Write($"MoveFileEx 失败，错误码: {err}", ConsoleColor.Yellow);
            }
        }
        catch (IOException ex)
        {
            Write($"安排更新失败: {ex}", ConsoleColor.Yellow);
        }

        return false;
    }

    /// <summary>
    /// 更新前需要删除的文件.
    /// </summary>
    /// <param name="directoryInfo">基础目录.</param>
    /// <param name="delUpdateFile">需要删除的文件列表文件.</param>
    private static void DeleteListedFiles(DirectoryInfo directoryInfo, FileInfo delUpdateFile)
    {
        if (delUpdateFile.Exists)
        {
            string[] lines = File.ReadAllLines(delUpdateFile.FullName);
            foreach (string line in lines)
            {
                string path = Path.Combine(directoryInfo.FullName, line);
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Console.WriteLine($"删除文件: {path}");
                }
                else if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                    Console.WriteLine($"删除目录: {path}");
                }
                else
                {
                    Console.WriteLine($"路径不存在: {path}");
                }
            }

            // 删除 delUpdate 文件本身
            delUpdateFile.Delete();
        }
    }

    private static bool IsFileInUse(FileInfo file)
    {
        try
        {
            using (FileStream fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                fs.Close(); // 文件可用，正常关闭流
            }

            return false; // 文件未被占用
        }
        catch (IOException)
        {
            return true; // 捕获异常表示文件可能被占用
        }
    }

    private static void Write(string text)
    {
        Console.ForegroundColor = defaultColor;
        Console.WriteLine(text);
    }

    private static void Write(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = defaultColor;
    }

    /// <summary>
    /// 清空指定目录（包括子文件夹和文件），并解除只读属性.
    /// </summary>
    private static void ClearDirectory(DirectoryInfo dir)
    {
        // 先确保当前目录本身可写
        if ((dir.Attributes & FileAttributes.ReadOnly) != 0)
        {
            dir.Attributes &= ~FileAttributes.ReadOnly;
        }

        // 删除所有文件
        foreach (FileInfo file in dir.GetFiles())
        {
            try
            {
                if ((file.Attributes & FileAttributes.ReadOnly) != 0)
                {
                    file.Attributes &= ~FileAttributes.ReadOnly;
                }

                file.Delete();
            }
            catch (IOException ex)
            {
                Write($"无法删除文件 {file.FullName}: {ex.Message}", ConsoleColor.Red);
            }
        }

        // 递归删除所有子目录
        foreach (DirectoryInfo subDir in dir.GetDirectories())
        {
            try
            {
                ClearDirectory(subDir);
                subDir.Delete(true);
            }
            catch (IOException ex)
            {
                Write($"无法删除目录 {subDir.FullName}: {ex.Message}", ConsoleColor.Red);
            }
        }
    }
}

/// <summary>
/// 同时写入控制台和文件的 TextWriter.
/// </summary>
#pragma warning disable SA1400 // File may only contain a single type
#pragma warning disable SA1402 // Access modifier should be declared
class DualWriter : TextWriter, IDisposable
#pragma warning restore SA1400 // Access modifier should be declared
#pragma warning restore SA1402 // File may only contain a single type
{
    private readonly TextWriter consoleOut;
    private readonly TextWriter fileOut;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DualWriter"/> class.
    /// </summary>
    /// <param name="consoleOut">控制台输出流.</param>
    /// <param name="fileOut">文件输出流.</param>
    public DualWriter(TextWriter consoleOut, TextWriter fileOut)
    {
        this.consoleOut = consoleOut;
        this.fileOut = fileOut;
    }

    /// <summary>
    /// Gets the encoding of the underlying console output.
    /// </summary>
    public override Encoding Encoding => this.consoleOut.Encoding;

#nullable enable
    /// <summary>
    /// 将字符串写入控制台和文件(不带换行).
    /// </summary>
    /// <param name="value">要写入的文本内容.</param>
    public override void WriteLine(string? value)
    {
        this.consoleOut.WriteLine(value);
        this.fileOut.WriteLine(value);
        this.fileOut.Flush(); // 确保实时写入文件
    }

    /// <summary>
    /// 将字符串写入控制台和文件，并在末尾追加换行符.
    /// </summary>
    /// <param name="value">要写入的文本内容.</param>
    public override void Write(string? value)
    {
        this.consoleOut.Write(value);
        this.fileOut.Write(value);
    }
#nullable restore

    /// <summary>
    /// 释放由 <see cref="DualWriter"/> 占用的资源。
    /// 当 <paramref name="disposing"/> 为 <c>true</c> 时，同时释放托管与非托管资源；
    /// 为 <c>false</c> 时，仅释放非托管资源，供终结器调用.
    /// </summary>
    /// <param name="disposing">
    /// <c>true</c> 表示由用户代码显式调用；<c>false</c> 表示由终结器线程调用.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                this.fileOut?.Dispose();
                this.consoleOut?.Dispose();
            }

            this.disposed = true;
            base.Dispose(disposing);
        }
    }
}
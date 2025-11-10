using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Localization.SevenZip;
using Rampastring.Tools;

namespace Localization.Tools;

public class SevenZip
{
    /// <summary>
    /// 根据文件和输出目录解压文件
    /// </summary>
    /// <param name="strFile">待解压文件，支持多种格式</param>
    /// <param name="strOutDir">输出目录</param>
    /// <returns>解压成功true、解压失败为false</returns>
    public static bool Unpack(string strFile, string strOutDir,bool isDelete = true)
    {
        try
        {
            using (ArchiveFile archiveFile = new ArchiveFile(strFile))
            {
                archiveFile.Extract(strOutDir);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
        if(isDelete)
            File.Delete(strFile);
        return true;
    }

    public delegate void ProgressCallback(double progressPercentage);

    /// <summary>
    /// 根据文件和正则解压文件并输出压缩包中的文件列表(含相对路径)
    /// </summary>
    /// <param name="strFile">待解压文件，支持多种格式</param>
    /// <param name="strRegPattern">文件路径匹配正则表达式</param>
    /// <returns>成功返回解压的文件列表，失败返回列表为0</returns>
    public static List<string> UnpackList(string strFile, string strRegPattern, ProgressCallback progressCallback = null)
    {
        List<string> files = new List<string>();
        Regex regex = new Regex(strRegPattern);

        try
        {
            using ArchiveFile archiveFile = new ArchiveFile(strFile);
            int totalEntries = archiveFile.Entries.Count;
            int currentEntry = 0;
            int lastReportedProgress = 0;
            object lockObject = new object();
            object progressLock = new object();  // 进度锁

            foreach (var entry in archiveFile.Entries)
            {
                try
                {
                    currentEntry++;
                    // 直接解压到文件系统并匹配文件名
                    entry.Extract(entry.FileName);
                    var match = regex.Match(entry.FileName);
                    if (match.Success)
                    {
                        lock (lockObject)
                        {
                            files.Add(match.Value);
                        }
                    }

                    // 更新百分比进度（减少更新频率）
                    int progress = (int)((currentEntry / (double)totalEntries) * 100);
                    lock (progressLock)
                    {
                        if (progress != lastReportedProgress)
                        {
                            lastReportedProgress = progress;
                            progressCallback?.Invoke(progress);
                        }
                    }
                }
                catch (Exception innerEx)
                {
                    Console.WriteLine($"Error processing entry {entry.FileName}: {innerEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return files;
        }

        // 移动删除操作到循环之外
        try
        {
            File.Delete(strFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting file {strFile}: {ex.Message}");
        }
        return files;
    }

    public static bool ExtractWith7Zip(string archivePath, string extractPath, ProgressCallback progressCallback = null, bool needDel = false)
    {
        try
        {
            // 构造命令行参数
            string arguments = $"x -y -aoa \"{archivePath}\" -o\"{extractPath}\"";

            string architecture = RuntimeInformation.OSArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.X86 => "x86",
                Architecture.Arm64 => "arm64",
                _ => "unknown"
            };

            ProcessStartInfo startInfo = new()
            {
                FileName = $"Resources/Binaries/{architecture}/7z.exe",
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            Logger.Log(startInfo.FileName);
            Logger.Log(arguments);

            using Process process = new();
            process.StartInfo = startInfo;
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    if (int.TryParse(e.Data, out int progress))
                    {
                        progressCallback?.Invoke(progress);
                    }
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();

            bool success = process.ExitCode == 0;

            if (success)
            {
                // ✅ 去掉解压出来文件的只读属性
                try
                {
                    foreach (string file in Directory.EnumerateFiles(extractPath, "*", SearchOption.AllDirectories))
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                    }
                }
                catch (Exception attrEx)
                {
                    Logger.Log($"Error removing read-only attributes: {attrEx.Message}");
                }
            }

            if (success && needDel)
            {
                try
                {
                    File.Delete(archivePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting file {archivePath}: {ex.Message}");
                }
            }

            return success;
        }
        catch (Exception ex)
        {
            Logger.Log($"Error extracting archive {archivePath} to {extractPath} with 7z.exe: {ex.Message}");
            Console.WriteLine($"Error extracting archive {archivePath} to {extractPath} with 7z.exe: {ex.Message}");
            return false;
        }
    }

    public static void CompressWith7Zip(string sourcePath, string archivePath, ProgressCallback progressCallback = null)
    {
        try
        {
            // 构造命令行参数，将文件压缩为 .7z
            string arguments = $"a -t7z \"{archivePath}\" \"{sourcePath}\" -r";

            Logger.Log(arguments);

            string architecture = RuntimeInformation.OSArchitecture switch
            {
                Architecture.X64 => "x64",      // x64 系统
                Architecture.X86 => "x86",      // x86 系统
                Architecture.Arm64 => "arm64",  // ARM64 系统
                _ => "unknown"                  // 其他架构 (如 Arm32、WASM 等)
            };

            // 启动 7z.exe 进程
            ProcessStartInfo startInfo = new()
            {
                FileName = $"Resources\\Binaries\\{architecture}\\7z.exe",
                Arguments = arguments,
                CreateNoWindow = true, // 不显示命令行窗口
                UseShellExecute = false, // 不使用操作系统外壳程序启动进程
                RedirectStandardOutput = true // 重定向标准输出流
            };

            using Process process = new();
            process.StartInfo = startInfo;
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    // 检查输出流中的进度信息
                    if (int.TryParse(e.Data, out int progress))
                    {
                        progressCallback?.Invoke(progress);
                    }
                }
            };

            process.Start();
            process.BeginOutputReadLine(); // 开始异步读取标准输出流

            process.WaitForExit(); // 等待压缩完成
        }
        catch (Exception ex)
        {
            Logger.Log($"Error compressing {sourcePath} to {archivePath} with 7z.exe: {ex.Message}");
            Console.WriteLine($"Error compressing {sourcePath} to {archivePath} with 7z.exe: {ex.Message}");
        }
    }

    public static void CompressWith7Zip(List<string> sourcePaths, string archivePath, ProgressCallback progressCallback = null)
    {
        try
        {
            // 构造命令行参数，将多个文件压缩为 .7z
            string sourceFiles = string.Join("\" \"", sourcePaths); // 用双引号分隔文件路径
            string arguments = $"a -t7z \"{archivePath}\" \"{sourceFiles}\" -r";
            Logger.Log(arguments);

            string architecture = RuntimeInformation.OSArchitecture switch
            {
                Architecture.X64 => "x64",      // x64 系统
                Architecture.X86 => "x86",      // x86 系统
                Architecture.Arm64 => "arm64",  // ARM64 系统
                _ => "unknown"                  // 其他架构 (如 Arm32、WASM 等)
            };

            // 启动 7z.exe 进程
            ProcessStartInfo startInfo = new()
            {
                FileName = $"Resources/Binaries/{architecture}/7z.exe",
                Arguments = arguments,
                CreateNoWindow = true, // 不显示命令行窗口
                UseShellExecute = false, // 不使用操作系统外壳程序启动进程
                RedirectStandardOutput = true // 重定向标准输出流
            };

            using Process process = new();
            process.StartInfo = startInfo;
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    // 检查输出流中的进度信息
                    if (int.TryParse(e.Data, out int progress))
                    {
                        progressCallback?.Invoke(progress);
                    }
                }
            };

            process.Start();
            process.BeginOutputReadLine(); // 开始异步读取标准输出流

            process.WaitForExit(); // 等待压缩完成
        }
        catch (Exception ex)
        {
            Logger.Log($"Error compressing files to {archivePath} with 7z.exe: {ex.Message}");
            Console.WriteLine($"Error compressing files to {archivePath} with 7z.exe: {ex.Message}");
        }
    }


    public static List<string> GetFile(string path)
    {

        using ArchiveFile archiveFile = new ArchiveFile(path);

        return archiveFile.Entries.Select(e => e.FileName).Where(e => e.Contains('.')).ToList();
    }
}
using System;
using System.Diagnostics;
using System.Management;

namespace Ra2Client.DXGUI.Generic
{
    public class ComputerUniqueHelper
    {
        public static string GetComputerUUID()
        {
            //var uuid = GetSmBIOSUUID();
            //if (string.IsNullOrWhiteSpace(uuid))
            //{
                var cpuID = GetCpuId();
                var biosSerialNumber = GetSmBIOSUUID();
                var diskSerialNumber = GetDiskSerialNumber();
                var uuid = $"{cpuID}__{biosSerialNumber}__{diskSerialNumber}";
            //}
            return uuid;
        }
#nullable enable
        private static string? GetSmBIOSUUID()
        {
            try
            {
                // Win32_ComputerSystemProduct 的 UUID 字段即为 SMBIOS UUID
                using var searcher = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct");
                foreach (ManagementObject mo in searcher.Get())
                {
                    var raw = mo["UUID"]?.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(raw)) return null;

                    // 有时会返回全 F 的保留值，按你原来逻辑过滤
                    if (string.Equals(raw, "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF", StringComparison.OrdinalIgnoreCase))
                        return null;

                    return raw;
                }
            }
            catch
            {
                // 忽略异常并返回 null（你也可以记录日志）
            }

            return null;
        }
        public static string? GetCpuId()
        {
            try
            {
                using var mc = new ManagementClass("Win32_Processor");
                foreach (var obj in mc.GetInstances())
                {
                    return obj["ProcessorId"]?.ToString();
                }
            }
            catch { }
            return null;
        }

        public static string? GetBiosSerialNumber()
        {
            try
            {
                using var mc = new ManagementClass("Win32_BIOS");
                foreach (var obj in mc.GetInstances())
                {
                    return obj["SerialNumber"]?.ToString();
                }
            }
            catch { }
            return null;
        }

        public static string? GetDiskSerialNumber()
        {
            try
            {
                using var mc = new ManagementClass("Win32_PhysicalMedia");
                foreach (var obj in mc.GetInstances())
                {
                    var serial = obj["SerialNumber"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(serial))
                        return serial;
                }
            }
            catch { }
            return null;
        }
        private static string? GetTextAfterSpecialText(string fullText, string specialText)
        {
            if (string.IsNullOrWhiteSpace(fullText) || string.IsNullOrWhiteSpace(specialText))
            {
                return null;
            }
            string? lastText = null;
            var idx = fullText.LastIndexOf(specialText);
            if (idx > 0)
            {
                lastText = fullText.Substring(idx + specialText.Length).Trim();
            }
            return lastText;
        }
        private static string? ExecuteCMD(string cmd, Func<string, string?> filterFunc)
        {
            using var process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.UseShellExecute = false;//是否使用操作系统shell启动
            process.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
            process.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
            process.StartInfo.RedirectStandardError = true;//重定向标准错误输出
            process.StartInfo.CreateNoWindow = true;//不显示程序窗口
            process.Start();//启动程序
            process.StandardInput.WriteLine(cmd + " &exit");
            process.StandardInput.AutoFlush = true;
            //获取cmd窗口的输出信息
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            process.Close();
            return filterFunc(output);
        }
#nullable restore
    }

}

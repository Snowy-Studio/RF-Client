using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rampastring.Tools;
using Rampastring.XNAUI;

namespace ClientCore
{ 
    public static class RenderImage
    {
        public static int RenderCount = 0;
        public static event EventHandler RenderCompleted;

        public static CancellationTokenSource cts = new CancellationTokenSource();
        public static ManualResetEventSlim pauseEvent = new ManualResetEventSlim(true); // 初始为可运行状态

        public static Dictionary<string, string> 需要渲染的地图列表 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string,string> 正在渲染的地图列表 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static bool RenderOneImage(string mapPath)
        {
            //if (!File.Exists(mapPath)) return false;
            //var mapName = Path.GetFileNameWithoutExtension(mapPath);

            //var ini = new IniFile(mapPath);
            //if (ini.otherChar.Count != 0) return false;


            //var engine = new RenderEngine();
            //RenderSettings settings = new RenderSettings()
            //{
            //    OutputFile = Path.GetFileNameWithoutExtension(mapPath),
            //    InputFile = mapPath,
            //    MixFilesDirectory = "E:\\Documents\\file\\RF-Client\\Bin\\YR",
            //    Engine = EngineType.YurisRevenge,
            //    ThumbnailConfig = "+(1280,768)",
            //    // SavePNGThumbnails = true,
            //    //  Backup = true,
            //    SaveJPEG = true
            //};
            //if (engine.ConfigureFromArgs(settings))
            //{
            //    var result = engine.Execute();
            //}


            string mapName = Path.GetFileNameWithoutExtension(mapPath);
            string inputPath = Path.Combine(Path.GetDirectoryName(mapPath), $"thumb_{mapName}.jpg");
            string outputPath = Path.Combine(Path.GetDirectoryName(mapPath), $"{mapName}.jpg");
            string strCmdText = $"-i \"{mapPath}\" -o \"{mapName}\" -m \"{ProgramConstants.GamePath}{UserINISettings.Instance.YRPath}\" -Y -z +(1280,768) --bkp ";

            using Process process = new Process();
            process.StartInfo.FileName = $"{ProgramConstants.GamePath}Resources\\RandomMapGenerator_RA2\\Map Renderer\\CNCMaps.Renderer.exe";
            process.StartInfo.Arguments = strCmdText;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            WindowManager.progress.Report($"正在渲染预览图{mapName}...");

            process.Start();
            process.WaitForExit();
            process.Close();

            if (File.Exists(inputPath))
            {
                try
                {
                    File.Move(inputPath, outputPath, true);
                }
                catch
                {

                }
            }

            // 渲染成功，增加计数并触发事件

            Interlocked.Increment(ref RenderCount);
            RenderCompleted?.Invoke(null, EventArgs.Empty);




            return true;
        }
        public static string 当前Mod;
        // 渲染多张图片的方法
        public static void RenderImages()
        {

            if (需要渲染的地图列表.Count == 0) return;

            IsCancelled = false; // 先清除取消标志
            RenderCount = 0;

            try
            {
                _ = Task.Run(() =>
                {
                    TaskbarProgress.Instance.SetState(TaskbarProgress.TaskbarStates.Normal);
                    foreach (var map in 需要渲染的地图列表.ToList())
                    {
                        if (IsCancelled)
                        {
                            //Console.WriteLine("渲染任务已取消");
                            break;
                        }

                        try
                        {
                            // 渲染任务
                            WindowManager.Report($"正在渲染地图:{map}");
                            if (正在渲染的地图列表.Contains(map))
                            {
                                continue;
                            }
                            正在渲染的地图列表.Add(map.Key,map.Value);
                            设置Mod(map.Value);
                            RenderOneImage(map.Key);
                            Interlocked.Increment(ref RenderCount);
                            TaskbarProgress.Instance.SetValue(RenderCount, 需要渲染的地图列表.Count);
                            WindowManager.Report("");
                            lock (需要渲染的地图列表)
                            {
                                正在渲染的地图列表.Remove(map.Key);
                                需要渲染的地图列表.Remove(map.Key);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"渲染异常: {ex.Message}");
                        }
                    }
                    IsCancelled = true;
                    TaskbarProgress.Instance.SetState(TaskbarProgress.TaskbarStates.NoProgress);
                    WindowManager.progress.Report(""); // 更新进度
                });



            }
            catch (Exception ex)
            {
                Console.WriteLine($"渲染过程中发生异常: {ex.Message}");
            }
        }

        private static void 设置Mod(string path)
        {
            foreach (var file in Directory.GetFiles(UserINISettings.Instance.YRPath))
            {
                string fileName = Path.GetFileName(file);
                if (ProgramConstants.PureHashes.ContainsKey(fileName))
                {
                    // Console.WriteLine($"白名单文件，跳过删除: {fileName}");
                    continue;
                }

                try
                {
                    File.Delete(file);
                    // Console.WriteLine($"已删除: {file}");
                }
                catch (Exception ex)
                {
                    // Console.WriteLine($"删除失败: {file}, 错误: {ex.Message}");
                }
            }

            if (!Directory.Exists(path))
            {
                return;
            }

            if (!Directory.Exists(UserINISettings.Instance.YRPath))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(path))
            {
                string fileName = Path.GetFileName(file);
                string linkPath = Path.Combine(UserINISettings.Instance.YRPath, fileName);

                if (File.Exists(linkPath))
                {
                    Console.WriteLine($"已存在，跳过: {linkPath}");
                    continue;
                }

                try
                {
                    File.CreateSymbolicLink(linkPath, file);
                //    Console.WriteLine($"符号链接成功: {linkPath} -> {file}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"创建符号链接失败: {linkPath} -> {file}, 错误: {ex.Message}");
                }
            }
        }

        public static bool IsCancelled = false;
        public static Task RenderPreviewImageAsync(string[] mapFiles,string modPath = "")
        {

            if (mapFiles.Length == 0)
                return Task.CompletedTask;
            if (!UserINISettings.Instance.RenderPreviewImage.Value)
                return Task.CompletedTask;
        
            foreach (var map in mapFiles)
            {
                if (!需要渲染的地图列表.ContainsKey(map))
                {
                    需要渲染的地图列表.Add(map, modPath);
                }
            }
            CancelRendering();
            RenderImages();
            return Task.CompletedTask;
        }
        public static void PauseRendering()
        {
            pauseEvent.Reset(); // 暂停
        }

        public static void ResumeRendering() => pauseEvent.Set(); // 继续

        public static void CancelRendering() {
            IsCancelled = true;
        }
        
    }
}

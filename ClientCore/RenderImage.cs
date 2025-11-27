using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rampastring.XNAUI;

namespace ClientCore
{ 
    public static class RenderImage
    {
        public static int RenderCount = 0;
        public static event EventHandler RenderCompleted;
        public static string 渲染目录 = "Resources\\RenderImage";
        public static CancellationTokenSource cts = new CancellationTokenSource();
        public static ManualResetEventSlim pauseEvent = new ManualResetEventSlim(true); // 初始为可运行状态

        public static Dictionary<string, List<string>> 需要渲染的地图列表 = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, List<string>> 正在渲染的地图列表 = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

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
            string strCmdText = $"-i \"{mapPath}\" -o \"{mapName}\" -m \"{ProgramConstants.GamePath}{渲染目录}\" -Y -z +(1280,768) --bkp ";

            using Process process = new Process();
            process.StartInfo.FileName = $"{ProgramConstants.GamePath}Resources\\RandomMapGenerator_RA2\\Map Renderer\\CNCMaps.Renderer.exe";
            process.StartInfo.Arguments = strCmdText;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            WindowManager.progress.Report($"正在渲染预览图{mapName}...");

            //Console.WriteLine(strCmdText);

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
        private static List<string> 上次渲染配置 = [];
        // 渲染多张图片的方法
        public static void RenderImages()
        {
            return;

            if (需要渲染的地图列表.Count == 0)
                return;

            IsCancelled = false; // 先清除取消标志
            RenderCount = 0;

            try
            {
                //_ = Task.Run(() =>
                //{
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
                            正在渲染的地图列表.TryAdd(map.Key,map.Value);

                            if (!是否与上次配置相同(map.Value))
                            {
                                上次渲染配置 = map.Value;
                                设置渲染配置(map.Value);
                            }
                            

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
                          //Console.WriteLine($"渲染异常: {ex.Message}");
                        }
                    }
                    IsCancelled = true;
                    TaskbarProgress.Instance.SetState(TaskbarProgress.TaskbarStates.NoProgress);
                    WindowManager.progress.Report(""); // 更新进度
                //});

            }
            catch (Exception ex)
            {
                Console.WriteLine($"渲染过程中发生异常: {ex.Message}");
            }
        }

        private static bool 是否与上次配置相同(List<string> 当前配置)
        {
            if (上次渲染配置 == null)
                return false;

            if (当前配置.Count != 上次渲染配置.Count)
                return false;

            for (int i = 0; i < 当前配置.Count; i++)
            {
                if (!string.Equals(当前配置[i], 上次渲染配置[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }
        private static void 设置渲染配置(List<string> paths)
        {
            string yrPath = UserINISettings.Instance.YRPath;
            

            // 1️⃣ 准备渲染目录
            if (!Directory.Exists(渲染目录))
                Directory.CreateDirectory(渲染目录);
            else
            {
                // 清空旧内容
                foreach (var file in Directory.GetFiles(渲染目录))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"删除失败: {file}, 错误: {ex.Message}");
                    }
                }
            }

            // 2️⃣ 用字典记录最终文件映射（文件名 -> 源文件路径）
            var fileMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // 先处理 YRPath
            if (Directory.Exists(yrPath))
            {
                foreach (var file in Directory.GetFiles(yrPath))
                {
                    string fileName = Path.GetFileName(file);
                    if (ProgramConstants.PureHashes.ContainsKey(fileName))
                        fileMap[fileName] = file;
                }
            }

            // 再按顺序处理列表路径（索引越大优先级越高）
            foreach (var path in paths)
            {
                if (!Directory.Exists(path))
                {
                    Console.WriteLine($"路径不存在，跳过: {path}");
                    continue;
                }

                foreach (var file in Directory.GetFiles(path))
                {
                    string fileName = Path.GetFileName(file);
                    fileMap[fileName] = file; // 覆盖前面的
                }
            }

            // 3️⃣ 创建符号链接
            foreach (var kv in fileMap)
            {
                string linkPath = Path.Combine(渲染目录, kv.Key);

                try
                {
                    File.CreateSymbolicLink(linkPath, Path.Combine(ProgramConstants.GamePath, kv.Value));
                    // Console.WriteLine($"符号链接: {kv.Key} <- {kv.Value}");
                }
                catch (Exception ex)
                {
                    // Console.WriteLine($"创建符号链接失败: {linkPath} -> {kv.Value}, 错误: {ex.Message}");
                }
            }

            // Console.WriteLine($"渲染配置设置完成，共 {fileMap.Count} 个文件。");
        }

        public static bool IsCancelled = false;
        public static async Task RenderPreviewImageAsync(string[] mapFiles,List<string> filePaths = null)
        {
            filePaths ??= [];
            if (mapFiles.Length == 0)
                return ;
            if (!UserINISettings.Instance.RenderPreviewImage.Value)
                return ;
        
            foreach (var map in mapFiles)
            {
                if (!需要渲染的地图列表.ContainsKey(map))
                {
                    需要渲染的地图列表.TryAdd(map, filePaths);
                }
            }
            CancelRendering();

            await Task.Run(RenderImage.RenderImages);
            return ;
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

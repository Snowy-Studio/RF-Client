﻿
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ClientCore
{
    public static class RenderImage
    {
        public static int RenderCount = 0;
        public static event EventHandler RenderCompleted;

        public static CancellationTokenSource cts = new CancellationTokenSource();
        public static ManualResetEventSlim pauseEvent = new ManualResetEventSlim(true); // 初始为可运行状态

        public static List<string> 需要渲染的地图列表 = [];
        public static async Task<bool> RenderOneImageAsync(string mapPath)
        {
            if(!File.Exists(mapPath)) return false;

            try
            {
                string mapName = Path.GetFileNameWithoutExtension(mapPath);
                string inputPath = Path.Combine(Path.GetDirectoryName(mapPath), $"thumb_{mapName}.png");
                string outputPath = Path.Combine(Path.GetDirectoryName(mapPath), $"{mapName}.png");
                string strCmdText = $"-i \"{ProgramConstants.GamePath}{mapPath}\" -o \"{mapName}\" -m \"{ProgramConstants.GamePath}\\\" -Y -z +(1280,768) --thumb-png --bkp ";
               //  Console.WriteLine(strCmdText);
                using Process process = new Process();
                process.StartInfo.FileName = $"{ProgramConstants.GamePath}Resources\\RandomMapGenerator_RA2\\Map Renderer\\CNCMaps.Renderer.exe";
                process.StartInfo.Arguments = strCmdText;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                WindowManager.progress.Report($"正在渲染预览图{mapName}...");
                // 异步执行渲染单张图片的逻辑
                await Task.Run(() =>
                {
                    
                    process.Start();
                    process.WaitForExit();
                    process.Close();

                    if (File.Exists(inputPath))
                    {
                        try
                        {
                            File.Move(inputPath, outputPath, true);
                        }
                        catch {
                           
                        }
                    }
                    
                    // 渲染成功，增加计数并触发事件
                    
                    Interlocked.Increment(ref RenderCount);
                    RenderCompleted?.Invoke(null, EventArgs.Empty);
                });

                if (File.Exists(outputPath))
                    return true;
                return false;
            }
            catch (Exception ex)
            {
                // 渲染出错，记录异常并触发事件
                Logger.Log($"Error rendering image {mapPath}: {ex.Message}");
                Interlocked.Increment(ref RenderCount);
                RenderCompleted?.Invoke(null, EventArgs.Empty);
                return false;
            }
        }

        static readonly List<Task> tasks = [];
        // 渲染多张图片的方法
        public static async Task RenderImagesAsync()
        {
            cts = new CancellationTokenSource();
            pauseEvent = new ManualResetEventSlim(true); // 初始为可运行状态

            RenderCount = 0;
            tasks.Clear();

            var semaphore = new SemaphoreSlim(1, 1); // 限制并发任务数量为1

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 1, // 初始并发数，使用 CPU 核心数的两倍
                CancellationToken = cts.Token // 支持任务取消
            };

            try
            {
                TaskbarProgress.Instance.SetState(TaskbarProgress.TaskbarStates.Normal);

                // 使用 Parallel.ForEach 执行并行渲染
                Parallel.ForEach(需要渲染的地图列表, parallelOptions, (map) =>
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await semaphore.WaitAsync(); // 等待信号量

                        try
                        {
                            while (!cts.Token.IsCancellationRequested)
                            {
                                pauseEvent.Wait(); // 等待继续信号

                                try
                                {
                                    // 渲染任务
                                    await RenderOneImageAsync(map);
                                    Interlocked.Increment(ref RenderCount);
                                    TaskbarProgress.Instance.SetValue(RenderCount, 需要渲染的地图列表.Count);
                                }
                                catch (OperationCanceledException)
                                {
                                    Console.WriteLine("渲染任务已取消");
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"渲染异常: {ex.Message}");
                                }

                                break; // 渲染成功后退出循环
                            }
                        }
                        finally
                        {
                            semaphore.Release(); // 释放信号量
                        }
                    }));
                });

                await Task.WhenAll(tasks); // 等待所有任务完成
                TaskbarProgress.Instance.SetState(TaskbarProgress.TaskbarStates.NoProgress);
                WindowManager.progress.Report(""); // 更新进度
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("所有渲染任务已取消");
            }
            catch (AggregateException ex)
            {
                Console.WriteLine($"并行任务发生异常: {ex.Flatten().Message}");
            }
        }

        public static void PauseRendering()
        {
            pauseEvent.Reset(); // 暂停
        }

        public static void ResumeRendering() => pauseEvent.Set(); // 继续

        public static void CancelRendering() {
            cts.Cancel();// 取消任务
            Task.WhenAll(tasks).Wait(); // 等待所有任务完成
        }
        
    }
}
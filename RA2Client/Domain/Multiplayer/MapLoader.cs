﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClientCore;
using ClientGUI;
using DTAConfig;
using Localization;
using Localization.Tools;
using Rampastring.Tools;
using Rampastring.XNAUI;
using SharpDX.Direct2D1;
using static System.Collections.Specialized.BitVector32;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
//using SharpDX.Direct3D9;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace Ra2Client.Domain.Multiplayer
{
    public class MapLoader
    {
        public const string MAP_FILE_EXTENSION = ".map";
        private const string CUSTOM_MAPS_DIRECTORY = "Maps\\Multi\\Custom";
        private static readonly string CUSTOM_MAPS_CACHE = SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "custom_map_cache");
        private const string MultiMapsSection = "MultiMaps";
        private const string GameModesSection = "GameModes";
        private const string GameModeAliasesSection = "GameModeAliases";
        /// <summary>
        /// List of game modes.
        /// </summary>
        public List<GameMode> GameModes = new List<GameMode>();

        public GameModeMapCollection GameModeMaps;

        /// <summary>
        /// An event that is fired when the maps have been loaded.
        /// </summary>
        public event EventHandler MapLoadingComplete;

        /// <summary>
        /// A list of game mode aliases.
        /// Every game mode entry that exists in this dictionary will get 
        /// replaced by the game mode entries of the value string array
        /// when map is added to game mode map lists.
        /// </summary>
        private Dictionary<string, string[]> GameModeAliases = new Dictionary<string, string[]>();

        /// <summary>
        /// List of gamemodes allowed to be used on custom maps in order for them to display in map list.
        /// </summary>
        private string[] AllowedGameModes = ClientConfiguration.Instance.AllowedCustomGameModes.Split(',');

        /// <summary>
        /// Loads multiplayer map info asynchonously.
        /// </summary>
        public Task LoadMapsAsync() => Task.Run(LoadMaps2);

        public static List<Map> 需要渲染的地图列表 = [];
     
        CancellationTokenSource cts = new CancellationTokenSource();
        ManualResetEventSlim pauseEvent = new ManualResetEventSlim(true); // 初始为可运行状态

        private void LoadMultiMaps2(IniFile mpMapsIni, string file)
        {
            var map = new Map(file);


            if (!map.SetInfoFromMpMapsINI(mpMapsIni)) return;


            AddMapToGameModes(map, false);
        }

        public void LoadMaps2()
        {
            
            需要渲染的地图列表.Clear();
            Logger.Log("开始加载地图...");

            WindowManager.progress.Report("正在转移根目录地图");
            LoadRootMaps();

            string[] maps = Directory.GetDirectories("Maps\\Multi");

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            var gameModeIni = new IniFile(ClientConfiguration.Instance.GameModesIniPath, GameMode.ANNOTATION);

            WindowManager.progress.Report("正在加载游戏模式");

            LoadGameModes(gameModeIni);
            LoadGameModeAliases(gameModeIni);

            var exceptions = new ConcurrentBag<Exception>();

            // 使用ConcurrentBag代替List
            var concurrentGameModes = new ConcurrentBag<GameMode>(GameModes);

            int 加载地图数量 = 0;
            int 总数 = 0;

            Parallel.ForEach(maps, parallelOptions, map =>
            {
                try
                {
                    var files = 
                    Directory.GetFiles(map, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(file => file.ToLower().EndsWith(".map") ||
                                       file.ToLower().EndsWith(".yrm") ||
                                       file.ToLower().EndsWith(".mpr"))
                    .Distinct()
                    .ToList();

                    if (!files.Any()) return;

                    Interlocked.Add(ref 总数, files.Count);

                    var ini = $"Maps\\Multi\\MPMaps{Path.GetFileName(map)}.ini";
                    if (!File.Exists(ini))
                        File.WriteAllText(ini, string.Empty);

                    var mpMapsIni = new IniFile(ini, Map.ANNOTATION);

                    Parallel.ForEach(files, parallelOptions, file =>
                    {
                        try
                        {
                            LoadMultiMaps2(mpMapsIni, file);
                            Interlocked.Increment(ref 加载地图数量);
                         
                            WindowManager.progress.Report($"已加载地图{加载地图数量}/{总数},正在加载{file}");
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                        }
                    });
                        
                    mpMapsIni.WriteIniFile();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            WindowManager.progress.Report(string.Empty);

            _ = Parallel.ForEach(concurrentGameModes, gameMode =>
            {
                try
                {
                    if (!GameModeAliases.TryGetValue(gameMode.UIName, out string[] gameModeAliases))
                        gameModeAliases = [gameMode.UIName];

                    foreach (string gameModeAlias in gameModeAliases)
                    {
                        GameMode gm;
                        lock (GameModes) // 确保线程安全
                        {
                            gm = concurrentGameModes.FirstOrDefault(g => g.Name == gameModeAlias.L10N("UI:GameMode:" + gameModeAlias));
                        }

                        if (gm != null)
                        {
                            lock (gm.Maps) // 确保线程安全
                            {
                                gm.Maps = [.. gm.Maps.OrderBy(o => o?.MaxPlayers)];
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // 将线程安全的类型转换回普通列表

            GameModes = concurrentGameModes
            .Where(g => g.Maps != null && g.Maps.Count > 0)
            .Select(g =>
            {
                g.Maps = g.Maps.OrderBy(m => m?.Name ?? string.Empty).ToList();
                return g;
            })
            .DistinctBy(g => g.Name)
            .ToList();



            GameModeMaps = new GameModeMapCollection(GameModes);

            GameModeMaps.Reverse();

            Logger.Log("地图加载完成。");
            WindowManager.Report();

            if (!exceptions.IsEmpty)
            {
                Logger.Log("执行过程中出现以下错误:");
                foreach (var ex in exceptions)
                {
                    Logger.Log(ex.Message);
                }
            }

            渲染地图();
        }

        internal static List<string> 检测重复地图()
        {
            string baseDirectory = "Maps\\Multi";
            var allFiles = new ConcurrentBag<string>();

            // 使用多线程获取所有符合条件的文件
            Parallel.ForEach(Directory.GetDirectories(baseDirectory), subDirectory =>
            {
                var files = Directory.EnumerateFiles(subDirectory, "*.*", SearchOption.TopDirectoryOnly)
                                     .Where(file => file.EndsWith(".map", StringComparison.OrdinalIgnoreCase) ||
                                                    file.EndsWith(".yrm", StringComparison.OrdinalIgnoreCase) ||
                                                    file.EndsWith(".mpr", StringComparison.OrdinalIgnoreCase));

                foreach (var file in files) allFiles.Add(file);

            });

            var hashSet = new ConcurrentDictionary<string, string>();
            var duplicateFiles = new ConcurrentBag<string>();

            // 并行处理检测重复文件
            Parallel.ForEach(allFiles, filePath =>
            {
                string fileHash = filePath.ComputeHash();

                if (hashSet.TryGetValue(fileHash, out _)) 
                    duplicateFiles.Add(filePath);
                else 
                    hashSet.TryAdd(fileHash, filePath);
            });

            return [.. duplicateFiles];
        }

        private static bool 是否为任务图(string mapFilePath)
        {
            try
            {
                var ini = new IniFile(mapFilePath);
              
                return ini.GetIntValue("Basic", "MultiplayerOnly", 1) == 0;
                
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 这两其实有区别,因为还存在一种加密的地图,你压根不知道他是单人还是多人图 
        /// </summary>
        private static bool 是否为多人图(string mapFilePath)
        {
            try
            {
                var ini = new IniFile(mapFilePath);

                return ini.GetIntValue("Basic", "MultiplayerOnly", 0) == 1;

            }
            catch
            { 
                return false;
            }
        }

        public void PauseRendering() => pauseEvent.Reset(); // 暂停

        public void ResumeRendering() => pauseEvent.Set(); // 继续

        public void CancelRendering() => cts.Cancel(); // 取消任务

        private void 渲染地图()
        {
            Task.Run(async () =>
            {
                cts?.Cancel();
                cts = new CancellationTokenSource();
                var semaphore = new SemaphoreSlim(1); // 最大并发数，例如10
                var tasks = 需要渲染的地图列表.Select(async map =>
                {
                    await semaphore.WaitAsync(cts.Token); // 支持取消

                    try
                    {
                        while (!cts.Token.IsCancellationRequested)
                        {
                            pauseEvent.Wait(); // 如果暂停，等待继续信号

                            if (await RenderImage.RenderOneImageAsync(map.BaseFilePath))
                            {
                                map.PreviewTexture = AssetLoader.LoadTexture(map.PreviewPath);
                            }
                            else
                            {
                                Console.WriteLine($"渲染失败 {map.BaseFilePath}");
                            }
                            break; // 成功渲染后跳出循环
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("渲染任务已取消");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"渲染异常: {ex.Message}");
                    }
                    finally
                    {
                        semaphore.Release(); // 确保释放信号量
                    }
                });

                await Task.WhenAll(tasks); // 等待所有任务完成
                WindowManager.progress.Report("");
            });
        }
        
        public static List<string> rootMaps = [];

        private void LoadRootMaps()
        {
       

            // 确保目标目录存在
            if (!Directory.Exists(CUSTOM_MAPS_DIRECTORY))
            {
                Directory.CreateDirectory(CUSTOM_MAPS_DIRECTORY);
            }

            var customMaps = Directory.EnumerateFiles("./", "*.*", SearchOption.TopDirectoryOnly)
                                     .Where(file => (file.ToLower().EndsWith(".map") ||
                                    file.ToLower().EndsWith(".yrm") ||
                                    file.ToLower().EndsWith(".mpr")
                                    ) && 是否为多人图(file)
                                    ).ToList();

            

            //foreach(var d in Directory.GetDirectories("maps"))
            //{
            //    if (Path.GetFileName(d) == "CP" || Path.GetFileName(d) == "Multi") continue;
            //    var maps = Directory.EnumerateFiles(d, "*.*", SearchOption.TopDirectoryOnly)
            //                         .Where(file => (file.ToLower().EndsWith(".map") ||
            //                        file.ToLower().EndsWith(".yrm") ||
            //                        file.ToLower().EndsWith(".mpr")
            //                        ) && 是否为多人图(file)
            //                        ).ToList();
            //    customMaps.AddRange(maps);
            //}

            rootMaps = customMaps;

            foreach (var customMap in customMaps)
            {
                var fileName = Path.GetFileName(customMap);
                var destFileName = Path.Combine(CUSTOM_MAPS_DIRECTORY, fileName);
                File.Move(customMap, destFileName,true);
            }

        }

        public void AgainLoadMaps()
        {
            //AllowedGameModes = null;
            GameModes.Clear();
            GameModeMaps.Clear();
            GameModeAliases.Clear();
            LoadMaps2();
        }
        private void LoadGameModes(IniFile mpMapsIni)
        {
            var gameModes = mpMapsIni.GetSectionKeys(GameModesSection);
            if (gameModes != null)
            {
                foreach (string key in gameModes)
                {
                    string gameModeName = mpMapsIni.GetStringValue(GameModesSection, key, string.Empty);
                    if (!string.IsNullOrEmpty(gameModeName))
                    {
                        var gm = new GameMode(gameModeName);

                        GameModes.Add(gm);
                    }
                }
            }
        }

        private void LoadGameModeAliases(IniFile mpMapsIni)
        {
            var gmAliases = mpMapsIni.GetSectionKeys(GameModeAliasesSection);

            if (gmAliases != null)
            {
                if (!GameModeAliases.ContainsKey("常规作战"))
                    GameModeAliases.Add("常规作战", mpMapsIni.GetStringValue(GameModeAliasesSection, gmAliases[0], string.Empty).Split(
                           new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                foreach (string key in gmAliases)
                {
                    if (!GameModeAliases.ContainsKey(key))
                        GameModeAliases.Add(key, mpMapsIni.GetStringValue(GameModeAliasesSection, key, string.Empty).Split(
                            new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                }

            }
        }

        /// <summary>
        /// Attempts to load a custom map.
        /// </summary>
        /// <param name="mapPath">The path to the map file relative to the game directory.</param>
        /// <param name="resultMessage">When method returns, contains a message reporting whether or not loading the map failed and how.</param>
        /// <returns>The map if loading it was succesful, otherwise false.</returns>
        public Map LoadCustomMap(string mapPath, out string resultMessage)
        {
            string customMapFilePath = SafePath.CombineFilePath(ProgramConstants.GamePath, FormattableString.Invariant($"{mapPath}{MAP_FILE_EXTENSION}"));

            // Logger.Log(customMapFilePath);

            FileInfo customMapFile = SafePath.GetFile(customMapFilePath);

            if (!customMapFile.Exists)
            {
                Logger.Log("LoadCustomMap: Map " + customMapFile.FullName + " not found!");
                resultMessage = $"Map file {customMapFile.Name} doesn't exist!";

                return null;
            }

            Logger.Log("LoadCustomMap: Loading custom map " + customMapFile.FullName);

            Map map = new Map(mapPath, customMapFilePath);

            if (map.SetInfoFromCustomMap())
            {
                foreach (GameMode gm in GameModes)
                {
                    if (gm.Maps.Find(m => m.SHA1 == map.SHA1) != null)
                    {
                        Logger.Log("LoadCustomMap: Custom map " + customMapFile.FullName + " is already loaded!");
                        resultMessage = $"Map {customMapFile.FullName} is already loaded.";

                        return null;
                    }
                }

                Logger.Log("LoadCustomMap: Map " + customMapFile.FullName + " added succesfully.");

                AddMapToGameModes(map, true);
                var gameModes = GameModes.Where(gm => gm.Maps.Contains(map));
                GameModeMaps.AddRange(gameModes.Select(gm => new GameModeMap(gm, map, false)));

                resultMessage = $"Map {customMapFile.FullName} loaded succesfully.";

                return map;
            }

            Logger.Log("LoadCustomMap: Loading map " + customMapFile.FullName + " failed!");
            resultMessage = $"Loading map {customMapFile.FullName} failed!";

            return null;
        }

        public void DeleteCustomMap(GameModeMap gameModeMap)
        {
            Logger.Log("Deleting map " + gameModeMap.Map.Name);
            File.Delete(gameModeMap.Map.CompleteFilePath);
            foreach (GameMode gameMode in GameModeMaps.GameModes)
            {
                gameMode.Maps.Remove(gameModeMap.Map);
            }

            GameModeMaps.Remove(gameModeMap);
        }

        /// <summary>
        /// Adds map to all eligible game modes.
        /// </summary>
        /// <param name="map">Map to add.</param>
        /// <param name="enableLogging">If set to true, a message for each game mode the map is added to is output to the log file.</param>
        private void AddMapToGameModes(Map map, bool enableLogging)
        {

            map.GameModes ??= GameModes.Select(gm => gm.Name).ToArray();

            foreach (string gameMode in map.GameModes)
            {
                if (!GameModeAliases.TryGetValue(gameMode, out string[] gameModeAliases))
                    gameModeAliases = [gameMode];

                foreach (string gameModeAlias in gameModeAliases)
                {
                    GameMode gm = GameModes.Find(g => g.Name == gameModeAlias.L10N("UI:GameMode:" + gameModeAlias));

                    if (gm == null)
                    {
                        gm = new GameMode(gameModeAlias.L10N("UI:GameMode:" + gameModeAlias));
                        GameModes.Add(gm);
                        //Logger.Log(gm.Name);
                    }

                    gm.Maps.Add(map);
                    if (enableLogging)
                        Logger.Log("AddMapToGameModes: Added map " + map.Name + " to game mode " + gm.Name);
                }
            }
        }

   
    }
}

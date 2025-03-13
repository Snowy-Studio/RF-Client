﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ClientCore;
using DTAConfig;
using DTAConfig.Entity;
using Localization;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Utilities = Rampastring.Tools.Utilities;

namespace Ra2Client.Domain.Multiplayer
{
    public struct ExtraMapPreviewTexture
    {
        public string TextureName;
        public Point Point;
        public int Level;
        public bool Toggleable;

        public ExtraMapPreviewTexture(string textureName, Point point, int level, bool toggleable)
        {
            TextureName = textureName;
            Point = point;
            Level = level;
            Toggleable = toggleable;
        }
    }

    /// <summary>
    /// A multiplayer map.
    /// </summary>
    public class Map
    {
        private const int MAX_PLAYERS = 8;

        public Map(string baseFilePath, string customMapFilePath = null)
        {
            BaseFilePath = baseFilePath;
            this.customMapFilePath = customMapFilePath;
            //Official = string.IsNullOrEmpty(this.customMapFilePath);
        }

        /// <summary>
        /// 地图名称.
        /// </summary>
        [JsonInclude]
        public string Name { get; private set; }

        /// <summary>
        /// 地图支持的最大玩家数量.
        /// </summary>
        [JsonInclude]
        public int MaxPlayers { get; private set; }

        /// <summary>
        /// 地图支持的最少玩家数量.
        /// </summary>
        [JsonInclude]
        public int MinPlayers { get; private set; }

        /// <summary>
        /// 是否使用MaxPlayers来限制地图的玩家数量
        /// </summary>
        [JsonInclude]
        public bool EnforceMaxPlayers { get; private set; }

        /// <summary>
        /// 控制地图是否为合作游戏模式。(启用简报逻辑和强制选项等)。
        /// </summary>
        [JsonInclude]
        public bool IsCoop { get; private set; }

        /// <summary>
        /// If set, this map won't be automatically transferred over CnCNet when
        /// a player doesn't have it.
        /// </summary>
        [JsonIgnore]
        public bool Official { get; private set; }

        /// <summary>
        /// Contains co-op information.
        /// </summary>
        [JsonInclude]
        public CoopMapInfo CoopInfo { get; private set; }

        /// <summary>
        /// The briefing of the map.
        /// </summary>
        [JsonInclude]
        public string Briefing { get; private set; }

        /// <summary>
        /// The author of the map.
        /// </summary>
        [JsonInclude]
        public string Author { get; private set; }

        /// <summary>
        /// The calculated SHA1 of the map.
        /// </summary>
        [JsonIgnore]
        public string SHA1 { get; private set; }

        /// <summary>
        /// The path to the map file.
        /// </summary>
        [JsonInclude]
        public string BaseFilePath { get; private set; }

        /// <summary>
        /// Returns the complete path to the map file.
        /// Includes the game directory in the path.
        /// </summary>
        [JsonInclude]
        public string CompleteFilePath => SafePath.CombineFilePath(ProgramConstants.GamePath, FormattableString.Invariant($"{BaseFilePath}"));

        /// <summary>
        /// The file name of the preview image.
        /// </summary>
        [JsonInclude]
        public string PreviewPath { get; set; }

        /// <summary>
        /// If set, this map cannot be played on Skirmish.
        /// </summary>
        [JsonInclude]
        public bool MultiplayerOnly { get; private set; }

        /// <summary>
        /// If set, this map cannot be played with AI players.
        /// </summary>
        [JsonInclude]
        public bool HumanPlayersOnly { get; private set; }

        /// <summary>
        /// If set, players are forced to random starting locations on this map.
        /// </summary>
        [JsonInclude]
        public bool ForceRandomStartLocations { get; private set; }

        /// <summary>
        /// If set, players are forced to different teams on this map.
        /// </summary>
        [JsonInclude]
        public bool ForceNoTeams { get; private set; }

        /// <summary>
        /// The name of an extra INI file in INI\MapCode\ that should be
        /// embedded into this map's INI code when a game is started.
        /// </summary>
        [JsonInclude]
        public string ExtraININame { get; private set; }

        /// <summary>
        /// The game modes that the map is listed for.
        /// </summary>
        [JsonInclude]
        public string[] GameModes;

        public List<string> Mod { get; private set; }

        /// <summary>
        /// The forced UnitCount for the map. -1 means none.
        /// </summary>
        [JsonInclude]
        public int UnitCount = -1;

        /// <summary>
        /// The forced starting credits for the map. -1 means none.
        /// </summary>
        [JsonInclude]
        public int Credits = -1;

        [JsonInclude]
        public int NeutralHouseColor = -1;

        [JsonInclude]
        public int SpecialHouseColor = -1;

        [JsonInclude]
        public int Bases = -1;

        [JsonInclude]
        public string[] localSize;

        [JsonInclude]
        public string[] actualSize;

        [JsonInclude]
        public int x;

        [JsonInclude]
        public int y;

        [JsonInclude]
        public int width;

        [JsonInclude]
        public int height;

        [JsonIgnore]
        private IniFile customMapIni;

        [JsonInclude]
        public string customMapFilePath;

        [JsonInclude]
        public List<string> waypoints = new List<string>();

        [JsonInclude]
        public string Mission = string.Empty;

        public int Money = 0;

        public static readonly string ANNOTATION = "" +
           "# 这里用来设置地图信息。\r\n" +
           "# [MAP ID]\r\n" +
           "# Description = 地图名。默认 地图文件名 \r\n" +
           "# LocalSize，Size = 地图大小。默认地图大小 \r\n" +
           "# ResourcesNum = 资源数。默认 地图资源数 \r\n" +
           "# Waypoint* = 出生点位置。默认 地图出生点位置 \r\n" +
           "# MaxPlayers = 支持最大玩家数。默认 出生点数 \r\n" +
           "# MinPlayers = 支持最小玩家数。默认 1 \r\n" +
           "# Author = 作者。默认 佚名 \r\n" +
           "# GameModes = 支持的游戏模式。默认 常规作战 \r\n" +
           "# Mission = 附带文件所在文件夹。默认 无 \r\n"
           ;

        /// <summary>
        /// The pixel coordinates of the map's player starting locations.
        /// </summary>
        [JsonInclude]
        public List<Point> startingLocations;

        [JsonInclude]
        public List<TeamStartMappingPreset> TeamStartMappingPresets = new List<TeamStartMappingPreset>();

        [JsonIgnore]
        public List<TeamStartMapping> TeamStartMappings => TeamStartMappingPresets?.FirstOrDefault()?.TeamStartMappings;

        [JsonIgnore]
        public Texture2D PreviewTexture { get; set; }

        public void CalculateSHA()
        {
            SHA1 = Utilities.CalculateSHA1ForFile(CompleteFilePath);
        }

        /// <summary>
        /// If false, the preview shouldn't be extracted for this (custom) map.
        /// </summary>
        [JsonInclude]
        public bool ExtractCustomPreview { get; set; } = true;

        [JsonInclude]
        public List<KeyValuePair<string, bool>> ForcedCheckBoxValues = new List<KeyValuePair<string, bool>>(0);

        [JsonInclude]
        public List<KeyValuePair<string, int>> ForcedDropDownValues = new List<KeyValuePair<string, int>>(0);

        [JsonIgnore]
        private List<ExtraMapPreviewTexture> extraTextures = new List<ExtraMapPreviewTexture>(0);

        public List<ExtraMapPreviewTexture> GetExtraMapPreviewTextures() => extraTextures;

        [JsonIgnore]
        private List<KeyValuePair<string, string>> ForcedSpawnIniOptions = new List<KeyValuePair<string, string>>(0);
        private static readonly char[] separator = new char[] { ',' };
        private static readonly char[] separatorArray = new char[] { ',' };

        readonly List<string> whitelist = ["WorkShop", "Standard", "MadHQ", "DDLY"];

        /// <summary>
        /// This is used to load a map from the MPMaps.ini (default name) file.
        /// </summary>
        /// <param name="iniFile">The configuration file for the multiplayer maps.</param>
        /// <returns>True if loading the map succeeded, otherwise false.</returns>
        public  bool SetInfoFromMpMapsINI(IniFile iniFile)
        {
            if(iniFile == null) return false;

            try
            {
                IniFile mapini = null;

                IniFile GetMapIni(string path) {

                    if (mapini != null && mapini.FileName == path) return mapini;
                    mapini = new IniFile(path);
                    return mapini;
                };

                var sectionName = BaseFilePath.Remove(BaseFilePath.Length - 4).Replace('\\', '/');
                if(!iniFile.SectionExists(sectionName))
                    iniFile.AddSection(sectionName);
                
                var section = iniFile.GetSection(sectionName);

                Official = whitelist.Any(sectionName.Contains);

                #region 处理预览图
                if(File.Exists($"{sectionName}.jpg"))
                    PreviewPath = SafePath.CombineFilePath(SafePath.GetFile(BaseFilePath).DirectoryName, FormattableString.Invariant($"{section.GetStringValue("PreviewImage", Path.GetFileNameWithoutExtension(BaseFilePath))}.jpg"));
                else 
                    {
                    if (!File.Exists($"{sectionName}.png") && UserINISettings.Instance.RenderPreviewImage.Value)
                    {
                        RenderImage.需要渲染的地图列表.Add(BaseFilePath);
                    }
                    PreviewPath = SafePath.CombineFilePath(SafePath.GetFile(BaseFilePath).DirectoryName, FormattableString.Invariant($"{section?.GetStringValue("PreviewImage", Path.GetFileNameWithoutExtension(BaseFilePath))}.png"));
                    }
                    
                
                #endregion

                if (!iniFile.SectionExists(sectionName))
                    iniFile.AddSection(sectionName);

                section = iniFile.GetSection(sectionName);

                Author = section.GetValue("Author", string.Empty);
                if (Author == string.Empty)
                {
                    // 尝试从其他地方获取作者信息
                    Author = GetMapIni(BaseFilePath).GetValue("Basic", "Author", "佚名"); 

                    if (Author == "佚名")
                    {
                        // 获取地图名称
                        var mapName = mapini.GetValue("Basic", "Name", string.Empty);

                        // 查找包含 "by" 或 "By" 的部分，忽略大小写
                        int byIndex = mapName.IndexOf("by ", StringComparison.OrdinalIgnoreCase);

                        if (byIndex != -1)
                            Author = mapName[(byIndex + 3)..].Trim();
                    }
                    // 保存作者信息
                    section.SetValue("Author", Author);
                }

                GameModes = section.GetStringValue("GameModes", "常规作战").Split(',');
                Mission = section.GetStringValue("Mission", string.Empty);
                if(!Directory.Exists(Mission))
                    section.RemoveKey("Mission");
                string modstr = section.GetStringValue("Mod", string.Empty);
                if (!string.IsNullOrEmpty(modstr))
                {
                    Mod = [.. modstr.Split(',')];
                }
                MinPlayers = section.GetValueOrSetDefault("MinPlayers", () => 0);
                MaxPlayers = section.GetValueOrSetDefault("MaxPlayers", () => 0);

                //这里最大人数0极有可能是因为没写MaxPlayers,实际上可以通过路径点0-7来判断人数.
                if (MaxPlayers == 0)
                {
                    for (int j = 0; j < 8; j++)
                        if (iniFile.GetStringValue("Waypoints", j.ToString(), string.Empty) == string.Empty)
                            MaxPlayers++;
                        else
                            break;
                }

                EnforceMaxPlayers = section.GetBooleanValue("EnforceMaxPlayers", false);
                Briefing = section.GetStringValue("Briefing", string.Empty).Replace("@", Environment.NewLine);
                CalculateSHA();

                //ExtensionOn = section.GetBooleanValue("ExtensionOn", true);
                //if (!ExtensionOn)
                //    Console.WriteLine();
                IsCoop = section.GetBooleanValue("IsCoopMission", false);
                Credits = section.GetIntValue("Credits", -1);
                UnitCount = section.GetIntValue("UnitCount", -1);
                NeutralHouseColor = section.GetIntValue("NeutralColor", -1);
                SpecialHouseColor = section.GetIntValue("SpecialColor", -1);
                MultiplayerOnly = section.GetBooleanValue("MultiplayerOnly", false);
                HumanPlayersOnly = section.GetBooleanValue("HumanPlayersOnly", false);
                ForceRandomStartLocations = section.GetBooleanValue("ForceRandomStartLocations", false);
                ForceNoTeams = section.GetBooleanValue("ForceNoTeams", false);
                ExtraININame = section.GetStringValue("ExtraININame", string.Empty);
                string bases = section.GetStringValue("Bases", string.Empty);

                if (!string.IsNullOrEmpty(bases))
                {
                    Bases = Convert.ToInt32(Conversions.BooleanFromString(bases, false));
                }

                int i = 0;
                while (true)
                {
                    // Format example:
                    // ExtraTexture0=oilderrick.png,200,150,1,false
                    // Third value is optional map cell level, defaults to 0 if unspecified.
                    // Fourth value is optional boolean value that determines if the texture can be toggled on / off.
                    string value = section.GetStringValue("ExtraTexture" + i, null);

                    if (string.IsNullOrWhiteSpace(value))
                        break;

                    string[] parts = value.Split(',');

                    if (parts.Length is < 3 or > 5)
                    {
                        Logger.Log($"Invalid format for ExtraTexture{i} in map " + BaseFilePath);
                        continue;
                    }

                    bool success = int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x);
                    success &= int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y);

                    int level = 0;
                    bool toggleable = false;

                    if (parts.Length > 3)
                        int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out level);

                    if (parts.Length > 4)
                        toggleable = Conversions.BooleanFromString(parts[4], false);

                    extraTextures.Add(new ExtraMapPreviewTexture(parts[0], new Point(x, y), level, toggleable));

                    i++;
                }

                if (IsCoop)
                {
                    CoopInfo = new CoopMapInfo();
                    string[] disallowedSides = section.GetStringValue("DisallowedPlayerSides", string.Empty).Split(
                        separator, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string sideIndex in disallowedSides)
                        CoopInfo.DisallowedPlayerSides.Add(int.Parse(sideIndex, CultureInfo.InvariantCulture));

                    string[] disallowedColors = section.GetStringValue("DisallowedPlayerColors", string.Empty).Split(
                        separatorArray, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string colorIndex in disallowedColors)
                        CoopInfo.DisallowedPlayerColors.Add(int.Parse(colorIndex, CultureInfo.InvariantCulture));

                    CoopInfo.SetHouseInfos(section);
                }

                if (MainClientConstants.USE_ISOMETRIC_CELLS)
                {
              
                    localSize = section.GetValueOrSetDefault("LocalSize", () => GetMapIni(BaseFilePath).GetStringValue("Map", "LocalSize", "0,0,0,0")).Split(',');
              
                    actualSize = section.GetValueOrSetDefault("Size", () => GetMapIni(BaseFilePath).GetStringValue("Map", "Size", "0,0,0,0")).Split(',');
           
                    //Task.Run(() =>
                    //{
                       // Money = section.GetValueOrSetDefault("ResourcesNum", () => GetMoney(new IniFile($"{BaseFilePath}")));
                    //}
                    //);
                    
                }
                else
                {
                    x = section.GetIntValue("X", 0);
                    y = section.GetIntValue("Y", 0);
                    width = section.GetIntValue("Width", 0);
                    height = section.GetIntValue("Height", 0);
                }

                if (section.Keys.FindIndex(key => key.Key.Contains("Waypoint")) == -1)
                {
                    MaxPlayers = GenerateWaypoints(iniFile, section, GetMapIni(BaseFilePath)) ?? MaxPlayers;
                }

                for (i = 0; i < MAX_PLAYERS; i++)
                {
                    string waypoint = section.GetStringValue("Waypoint" + i, string.Empty);

                    if (string.IsNullOrEmpty(waypoint))
                        break;

                    Debug.Assert(int.TryParse(waypoint, out _), $"waypoint should be a number, got {waypoint}");
                    waypoints.Add(waypoint);
                }

                Name = section.GetValueOrSetDefault("Description", () => $"[{MaxPlayers}]{Path.GetFileNameWithoutExtension(BaseFilePath)}");
                if (!Name.Contains('['))
                {
                    Name = $"[{MaxPlayers}]{Name}";
                    section.SetValue("Description", Name);
                }


                GetTeamStartMappingPresets(section);
                //if (UserINISettings.Instance.PreloadMapPreviews)
                //    PreviewTexture = LoadPreviewTexture();

                // Parse forced options

                string forcedOptionsSections = iniFile.GetStringValue(BaseFilePath, "ForcedOptions", string.Empty);

                if (!string.IsNullOrEmpty(forcedOptionsSections))
                {
                    string[] sections = forcedOptionsSections.Split(',');
                    foreach (string foSection in sections)
                        ParseForcedOptions(iniFile, foSection);
                }

                string forcedSpawnIniOptionsSections = iniFile.GetStringValue(BaseFilePath, "ForcedSpawnIniOptions", string.Empty);

                if (!string.IsNullOrEmpty(forcedSpawnIniOptionsSections))
                {
                    string[] sections = forcedSpawnIniOptionsSections.Split(',');
                    foreach (string fsioSection in sections)
                        ParseSpawnIniOptions(iniFile, fsioSection);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log("Setting info for " + BaseFilePath + " failed! Reason: " + ex.Message);
                PreStartup.LogException(ex);
                return false;
            }
        }

        /// <summary>
        /// 初始化地图信息
        /// </summary>
        /// <param name="iniFile">传递ini引用</param>
        /// <returns>地图信息</returns>
        private TileLayer ReadTiles(IniFile iniFile)
        {
           
            var FullSize = new Rectangle(int.Parse(actualSize[0]), int.Parse(actualSize[1]), int.Parse(actualSize[2]), int.Parse(actualSize[3]));
            var Tiles = new TileLayer(FullSize.Width, FullSize.Height);
            var mapSection = iniFile.GetSection("IsoMapPack5");

            byte[] lzoData = Convert.FromBase64String(mapSection.ConcatenatedValues());
            int cells = (FullSize.Width * 2 - 1) * FullSize.Height;
            int lzoPackSize = cells * 11 + 4; // last 4 bytes contains a lzo pack header saying no more data is left

            var isoMapPack = new byte[lzoPackSize];

            // In case, IsoMapPack5 contains less entries than the number of cells, fill up any number greater 
            // than 511 and filter later.
            int j = 0;
            for (int i = 0; i < cells; i++)
            {
                isoMapPack[j] = 0x88;
                isoMapPack[j + 1] = 0x40;
                isoMapPack[j + 2] = 0x88;
                isoMapPack[j + 3] = 0x40;
                j += 11;
            }

            Format5.DecodeInto(lzoData, isoMapPack);

            // Fill level 0 clear tiles for all array values
            for (ushort y = 0; y < FullSize.Height; y++)
            {
                for (ushort x = 0; x <= FullSize.Width * 2 - 2; x++)
                {
                    ushort dx = x;
                    ushort dy = (ushort)(y * 2 + x % 2);
                    ushort rx = (ushort)((dx + dy) / 2 + 1);
                    ushort ry = (ushort)(dy - rx + FullSize.Width + 1);
                    Tiles[x, y] = new IsoTile(dx, dy, rx, ry, 0, 0, 0, 0);
                }
            }

            // Overwrite with actual entries found in IsoMapPack5
            var mf = new MemoryFile(isoMapPack);
            int numtiles = 0;
            for (int i = 0; i < cells; i++)
            {
                ushort rx = mf.ReadUInt16();
                ushort ry = mf.ReadUInt16();
                int tilenum = mf.ReadInt32();
                byte subtile = mf.ReadByte();
                byte z = mf.ReadByte();
                byte icegrowth = mf.ReadByte();

                if (tilenum >= 65535) tilenum = 0; // Tile 0xFFFF used as empty/clear

                if (rx <= 511 && ry <= 511)
                {
                    int dx = rx - ry + FullSize.Width - 1;
                    int dy = rx + ry - FullSize.Width - 1;
                    numtiles++;
                    if (dx >= 0 && dx < 2 * Tiles.Width && dy >= 0 && dy < 2 * Tiles.Height)
                    {
                        var tile = new IsoTile((ushort)dx, (ushort)dy, rx, ry, z, tilenum, subtile, icegrowth);
                        Tiles[(ushort)dx, (ushort)dy / 2] = tile;
                    }
                }
            }

            return Tiles;

        }
        
        /// <summary>
        /// 计算地图资源值
        /// </summary>
        /// <param name="iniFile"> 传递ini引用 </param>
        /// <returns>资源值</returns>
        private int GetMoney(IniFile iniFile)
        {
            try
            {
                var Tiles = ReadTiles(iniFile);

                IniSection overlaySection = iniFile.GetSection("OverlayPack");
                IniSection overlayDataSection = iniFile.GetSection("OverlayDataPack");
                if (overlaySection == null || overlayDataSection == null) return 0;

                var count = 0;

                byte[] format80Data = Convert.FromBase64String(overlaySection.ConcatenatedValues());
                var overlayPack = new byte[1 << 18];
                Format5.DecodeInto(format80Data, overlayPack, 80);

                format80Data = Convert.FromBase64String(overlayDataSection.ConcatenatedValues());
                var overlayDataPack = new byte[1 << 18];
                Format5.DecodeInto(format80Data, overlayDataPack, 80);

                var FullSize = new Rectangle(int.Parse(actualSize[0]), int.Parse(actualSize[1]), int.Parse(actualSize[2]), int.Parse(actualSize[3]));

                for (int y = 0; y < FullSize.Height; y++)
                    for (int x = FullSize.Width * 2 - 2; x >= 0; x--)
                    {
                        var t = Tiles[x, y];
                        if (t == null) continue;
                        int idx = t.Rx + 512 * t.Ry;
                        var (overlay_id, overlay_value) = (overlayPack[idx], overlayDataPack[idx]);

                        count += overlay_id switch
                        {
                            // TODO 这里的数字可能要根据实际的金矿注册顺序来调整
                            112 or 102 => 25 * overlay_value,
                            30 or 27 => 50 * overlay_value,
                            _ => 0
                        };
                    }
                return count;
            }
            catch (Exception)
            {
                return 0;
            }
            
        }

        /// <summary>
        /// 生成路径点顺便计算玩家数
        /// </summary>
        /// <param name="iniFile"></param>
        /// <param name="section"></param>
        public static int? GenerateWaypoints(IniFile iniFile, IniSection section, IniFile mapIni)
        {

            var playerCount = 8;

            for (int i = 1; i < 9; i++)
            {
                var waypoint = mapIni.GetStringValue("Header", $"Waypoint{i}", string.Empty);
                if (waypoint.Length == 0) continue;

                var parts = waypoint.Split('=', ',');
                int x = int.Parse(parts[0]);
                int y = int.Parse(parts[1]);

                if (x != 0 || y != 0)
                {
                    x = (int)(x * Math.Sqrt(2));
                    y = (int)(y * Math.Sqrt(2));
                    x -= (int)(256 * Math.Sqrt(2));

                    // Swap coordinates
                    (y, x) = (x, y);
                    double length = Math.Sqrt(x * x + y * y);
                    double alpha = Math.Atan2(y, x);
                    double beta = Math.PI / 4 + alpha;

                    int newX = (int)(Math.Cos(beta) * length);
                    int newY = (int)(Math.Sin(beta) * length);

                    iniFile.SetValue(section.SectionName, $"Waypoint{i - 1}", $"{newX:000}{newY:000}");

                }
                else
                {
                    playerCount--;
                }
            }

            iniFile.SetValue(section.SectionName, "MaxPlayers", playerCount);

            return playerCount;
        }

        private void GetTeamStartMappingPresets(IniSection section)
        {
            TeamStartMappingPresets = new List<TeamStartMappingPreset>();
            for (int i = 0; ; i++)
            {
                try
                {
                    var teamStartMappingPreset = section.GetStringValue($"TeamStartMapping{i}", string.Empty);
                    if (string.IsNullOrEmpty(teamStartMappingPreset))
                        return; // mapping not found

                    var teamStartMappingPresetName = section.GetStringValue($"TeamStartMapping{i}Name", string.Empty);
                    if (string.IsNullOrEmpty(teamStartMappingPresetName))
                        continue; // mapping found, but no name specified

                    TeamStartMappingPresets.Add(new TeamStartMappingPreset()
                    {
                        Name = teamStartMappingPresetName,
                        TeamStartMappings = TeamStartMapping.FromListString(teamStartMappingPreset)
                    });
                }
                catch (Exception e)
                {
                    Logger.Log($"Unable to parse team start mappings. Map: \"{Name}\", Error: {e.Message}");
                    TeamStartMappingPresets = new List<TeamStartMappingPreset>();
                }
            }
        }

        public List<Point> GetStartingLocationPreviewCoords(Point previewSize)
        {
            if (startingLocations == null)
            {
                startingLocations = new List<Point>();

                foreach (string waypoint in waypoints)
                {
                    if (MainClientConstants.USE_ISOMETRIC_CELLS)
                        startingLocations.Add(GetIsometricWaypointCoords(waypoint, actualSize, localSize, previewSize));
                    else
                        startingLocations.Add(GetTDRAWaypointCoords(waypoint, x, y, width, height, previewSize));
                }
            }

            return startingLocations;
        }

        public Point MapPointToMapPreviewPoint(Point mapPoint, Point previewSize, int level)
        {
            if (MainClientConstants.USE_ISOMETRIC_CELLS)
                return GetIsoTilePixelCoord(mapPoint.X, mapPoint.Y, actualSize, localSize, previewSize, level);

            return GetTDRACellPixelCoord(mapPoint.X, mapPoint.Y, x, y, width, height, previewSize);
        }

        /// <summary>
        /// Due to caching, this may not have been loaded on application start.
        /// This function provides the ability to load when needed.
        /// </summary>
        /// <returns>Returns the loaded INI file of a custom map.</returns>
        private IniFile GetCustomMapIniFile()
        {
            if (customMapIni != null)
                return customMapIni;


            customMapIni = new IniFile { FileName = customMapFilePath };
            customMapIni.AddSection("Basic");
            customMapIni.SetBooleanValue("Basic", "EnforceMaxPlayers", false);
            customMapIni.AddSection("Map");
            customMapIni.AddSection("Waypoints");
            customMapIni.AddSection("Preview");
            customMapIni.AddSection("PreviewPack");
            customMapIni.AddSection("ForcedOptions");
            customMapIni.AddSection("ForcedSpawnIniOptions");
            customMapIni.AllowNewSections = false;
            customMapIni.Parse();

            return customMapIni;
        }

        /// <summary>
        /// Loads map information from a TS/RA2 map INI file.
        /// Returns true if successful, otherwise false.
        /// </summary>
        public bool SetInfoFromCustomMap()
        {
            if (!File.Exists(customMapFilePath))
                return false;

            try
            {
                IniFile iniFile = GetCustomMapIniFile();

                IniSection basicSection = iniFile.GetSection("Basic");

                //if(!basicSection.GetBooleanValue("MultiplayerOnly", false))
                //    return false;

                //Name = basicSection.GetStringValue("Name", "Unnamed map

                string mapNameStr = Path.GetFileNameWithoutExtension(iniFile.FileName);
                var mapTheater = iniFile.GetStringValue("Map","Theater","未知地形");
                if (mapNameStr.Length == 18 && mapNameStr[..4] == "随机地图")
                {
                    mapTheater = mapTheater.ToUpper() switch
                    {
                        "NEWURBAN" => "新城市",
                        "TEMPERATE" => "温带",
                        "LUNAR" => "月球",
                        "SNOW" => "雪地",
                        "URBAN" => "城市",
                        "DESERT" => "沙漠",
                        _ => "未知地形",
                    };

                    Name = $"[{basicSection.GetStringValue("MaxPlayer", "0")}] {mapNameStr[..2]}{mapTheater}{mapNameStr[2..4]}{mapNameStr[8..16]}";
                }
                else
                    Name = mapNameStr;

                //Name = Path.GetFileNameWithoutExtension(iniFile.FileName);
                Author = basicSection.GetStringValue("Author", "Unknown author");

                string gameModesString = basicSection.GetStringValue("GameModes", string.Empty);
                if (string.IsNullOrEmpty(gameModesString))
                {
                    gameModesString = basicSection.GetStringValue("GameMode", "常规作战");
                }

                GameModes = gameModesString.Split(',');

                if (GameModes.Length == 0)
                {
                    Logger.Log("Custom map " + customMapFilePath + " has no game modes!");
                    return false;
                }

                for (int i = 0; i < GameModes.Length; i++)
                {
                    string gameMode = GameModes[i].Trim().L10N("UI:GameMode:" + GameModes[i].Trim());
                    GameModes[i] = string.Concat(gameMode[..1].ToUpperInvariant(), gameMode.AsSpan(1));
                }

                MinPlayers = 0;
                if (basicSection.KeyExists("ClientMaxPlayer"))
                    MaxPlayers = basicSection.GetIntValue("ClientMaxPlayer", 0);
                else
                    MaxPlayers = basicSection.GetIntValue("MaxPlayer", 0);
                EnforceMaxPlayers = basicSection.GetBooleanValue("EnforceMaxPlayers", false);
                // PreviewPath = Path.GetDirectoryName(BaseFilePath) + "/" +
                //    iniFile.GetStringValue(BaseFilePath, "PreviewImage", Path.GetFileNameWithoutExtension(BaseFilePath) + ".png");
                Briefing = basicSection.GetStringValue("Briefing", string.Empty).Replace("@", Environment.NewLine);
                CalculateSHA();
                IsCoop = basicSection.GetBooleanValue("IsCoopMission", false);
                Credits = basicSection.GetIntValue("Credits", -1);
                UnitCount = basicSection.GetIntValue("UnitCount", -1);
                NeutralHouseColor = basicSection.GetIntValue("NeutralColor", -1);
                SpecialHouseColor = basicSection.GetIntValue("SpecialColor", -1);
                HumanPlayersOnly = basicSection.GetBooleanValue("HumanPlayersOnly", false);
                ForceRandomStartLocations = basicSection.GetBooleanValue("ForceRandomStartLocations", false);
                ForceNoTeams = basicSection.GetBooleanValue("ForceNoTeams", false);
                PreviewPath = Path.ChangeExtension(customMapFilePath.Substring(ProgramConstants.GamePath.Length), ".png");
                MultiplayerOnly = basicSection.GetBooleanValue("ClientMultiplayerOnly", false);

                string bases = basicSection.GetStringValue("Bases", string.Empty);
                if (!string.IsNullOrEmpty(bases))
                {
                    Bases = Convert.ToInt32(Conversions.BooleanFromString(bases, false));
                }

                if (IsCoop)
                {
                    CoopInfo = new CoopMapInfo();
                    string[] disallowedSides = iniFile.GetStringValue("Basic", "DisallowedPlayerSides", string.Empty).Split(
                        new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string sideIndex in disallowedSides)
                        CoopInfo.DisallowedPlayerSides.Add(int.Parse(sideIndex, CultureInfo.InvariantCulture));

                    string[] disallowedColors = iniFile.GetStringValue("Basic", "DisallowedPlayerColors", string.Empty).Split(
                        new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string colorIndex in disallowedColors)
                        CoopInfo.DisallowedPlayerColors.Add(int.Parse(colorIndex, CultureInfo.InvariantCulture));

                    CoopInfo.SetHouseInfos(basicSection);
                }

                localSize = iniFile.GetStringValue("Map", "LocalSize", "0,0,0,0").Split(',');
                actualSize = iniFile.GetStringValue("Map", "Size", "0,0,0,0").Split(',');

                if (MainClientConstants.USE_ISOMETRIC_CELLS)
                {
                    localSize = iniFile.GetStringValue("Map", "LocalSize", "0,0,0,0").Split(',');
                    actualSize = iniFile.GetStringValue("Map", "Size", "0,0,0,0").Split(',');
                }
                else
                {
                    x = iniFile.GetIntValue("Map", "X", 0);
                    y = iniFile.GetIntValue("Map", "Y", 0);
                    width = iniFile.GetIntValue("Map", "Width", 0);
                    height = iniFile.GetIntValue("Map", "Height", 0);
                }

                for (int i = 0; i < MAX_PLAYERS; i++)
                {

                    string waypoint = GetCustomMapIniFile().GetStringValue("Waypoints", i.ToString(CultureInfo.InvariantCulture), string.Empty);

                    if (string.IsNullOrEmpty(waypoint))
                        break;

                    waypoints.Add(waypoint);
                }

                GetTeamStartMappingPresets(basicSection);

                ParseForcedOptions(iniFile, "ForcedOptions");
                ParseSpawnIniOptions(iniFile, "ForcedSpawnIniOptions");

                return true;
            }
            catch
            {
                Logger.Log("Loading custom map " + customMapFilePath + " failed!");
                return false;
            }
        }

        private void ParseForcedOptions(IniFile iniFile, string forcedOptionsSection)
        {
            List<string> keys = iniFile.GetSectionKeys(forcedOptionsSection);

            if (keys == null)
            {
                Logger.Log("Invalid ForcedOptions Section \"" + forcedOptionsSection + "\" in map " + BaseFilePath);
                return;
            }

            foreach (string key in keys)
            {
                string value = iniFile.GetStringValue(forcedOptionsSection, key, string.Empty);

                if (int.TryParse(value, out int intValue))
                {
                    ForcedDropDownValues.Add(new KeyValuePair<string, int>(key, intValue));
                }
                else
                {
                    ForcedCheckBoxValues.Add(new KeyValuePair<string, bool>(key, Conversions.BooleanFromString(value, false)));
                }
            }
        }

        private void ParseSpawnIniOptions(IniFile forcedOptionsIni, string spawnIniOptionsSection)
        {
            List<string> spawnIniKeys = forcedOptionsIni.GetSectionKeys(spawnIniOptionsSection);

            foreach (string key in spawnIniKeys)
            {
                ForcedSpawnIniOptions.Add(new KeyValuePair<string, string>(key,
                    forcedOptionsIni.GetStringValue(spawnIniOptionsSection, key, string.Empty)));
            }
        }

        /// <summary>
        /// Loads and returns the map preview texture.
        /// </summary>
        public Texture2D LoadPreviewTexture()
        {
            var mapIni = new IniFile(BaseFilePath);

            if (SafePath.GetFile(ProgramConstants.GamePath, PreviewPath).Exists)
                return AssetLoader.LoadTextureUncached(PreviewPath);

           // if (!Official)
           // {
                // Extract preview from the map itself
                using Image preview = MapPreviewExtractor.ExtractMapPreview(mapIni??GetCustomMapIniFile());

                if (preview != null)
                {
                    Texture2D texture = AssetLoader.TextureFromImage(preview);
                    if (texture != null)
                        return texture;
                }
           // }

            return AssetLoader.CreateTexture(Color.Black, 10, 10);
        }

        public IniFile GetMapIni()
        {


            var mapIni = new IniFile(CompleteFilePath);

            if (!string.IsNullOrEmpty(ExtraININame))
            {
                var extraIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "INI", "MapCode", ExtraININame));
                IniFile.ConsolidateIniFiles(mapIni, extraIni);
            }

            return mapIni;
        }

        public void ApplySpawnIniCode(IniFile spawnIni, int totalPlayerCount,
            int aiPlayerCount, int coopDifficultyLevel)
        {
            foreach (KeyValuePair<string, string> key in ForcedSpawnIniOptions)
                spawnIni.SetValue("Settings", key.Key, key.Value);

            if (Credits != -1)
                spawnIni.SetValue("Settings", "Credits", Credits);

            if (UnitCount != -1)
                spawnIni.SetValue("Settings", "UnitCount", UnitCount);

            int neutralHouseIndex = totalPlayerCount + 1;
            int specialHouseIndex = totalPlayerCount + 2;

            if (IsCoop)
            {
                var allyHouses = CoopInfo.AllyHouses;
                var enemyHouses = CoopInfo.EnemyHouses;

                int multiId = totalPlayerCount + 1;
                foreach (var houseInfo in allyHouses.Concat(enemyHouses))
                {
                    spawnIni.SetValue("HouseHandicaps", "Multi" + multiId, coopDifficultyLevel);
                    spawnIni.SetValue("HouseCountries", "Multi" + multiId, houseInfo.Side);
                    spawnIni.SetValue("HouseColors", "Multi" + multiId, houseInfo.Color);
                    spawnIni.SetValue("SpawnLocations", "Multi" + multiId, houseInfo.StartingLocation);

                    multiId++;
                }

                for (int i = 0; i < allyHouses.Count; i++)
                {
                    int aMultiId = totalPlayerCount + i + 1;

                    int allyIndex = 0;

                    // Write alliances
                    for (int pIndex = 0; pIndex < totalPlayerCount + allyHouses.Count; pIndex++)
                    {
                        int allyMultiIndex = pIndex;

                        if (pIndex == aMultiId - 1)
                            continue;

                        spawnIni.SetValue("Multi" + aMultiId + "_Alliances",
                            "HouseAlly" + HouseAllyIndexToString(allyIndex), allyMultiIndex);
                        spawnIni.SetValue("Multi" + (allyMultiIndex + 1) + "_Alliances",
                            "HouseAlly" + HouseAllyIndexToString(totalPlayerCount + i - 1), aMultiId - 1);
                        allyIndex++;
                    }
                }

                for (int i = 0; i < enemyHouses.Count; i++)
                {
                    int eMultiId = totalPlayerCount + allyHouses.Count + i + 1;

                    int allyIndex = 0;

                    // Write alliances
                    for (int enemyIndex = 0; enemyIndex < enemyHouses.Count; enemyIndex++)
                    {
                        int allyMultiIndex = totalPlayerCount + allyHouses.Count + enemyIndex;

                        if (enemyIndex == i)
                            continue;

                        spawnIni.SetValue("Multi" + eMultiId + "_Alliances",
                            "HouseAlly" + HouseAllyIndexToString(allyIndex), allyMultiIndex);
                        allyIndex++;
                    }
                }

                spawnIni.SetValue("Settings", "AIPlayers",
                    aiPlayerCount + allyHouses.Count + enemyHouses.Count);

                neutralHouseIndex += allyHouses.Count + enemyHouses.Count;
                specialHouseIndex += allyHouses.Count + enemyHouses.Count;
            }

            if (NeutralHouseColor > -1)
                spawnIni.SetValue("HouseColors", "Multi" + neutralHouseIndex, NeutralHouseColor);

            if (SpecialHouseColor > -1)
                spawnIni.SetValue("HouseColors", "Multi" + specialHouseIndex, SpecialHouseColor);

            if (Bases > -1)
                spawnIni.SetValue("Settings", "Bases", Convert.ToBoolean(Bases));
        }

        private static string HouseAllyIndexToString(int index)
        {
            string[] houseAllyIndexStrings = new string[]
            {
                "One",
                "Two",
                "Three",
                "Four",
                "Five",
                "Six",
                "Seven"
            };

            return houseAllyIndexStrings[index];
        }

        public string GetSizeString()
        {
            if (MainClientConstants.USE_ISOMETRIC_CELLS)
            {
                if (actualSize == null || actualSize.Length < 4)
                    return "Not available";

                return actualSize[2] + "x" + actualSize[3];
            }
            else
            {
                return width + "x" + height;
            }
        }

        private static Point GetTDRAWaypointCoords(string waypoint, int x, int y, int width, int height, Point previewSizePoint)
        {
            int waypointCoordsInt = Conversions.IntFromString(waypoint, -1);

            if (waypointCoordsInt < 0)
                return new Point(0, 0);

            // https://modenc.renegadeprojects.com/Waypoints
            int waypointX = waypointCoordsInt % MainClientConstants.TDRA_WAYPOINT_COEFFICIENT;
            int waypointY = waypointCoordsInt / MainClientConstants.TDRA_WAYPOINT_COEFFICIENT;

            return GetTDRACellPixelCoord(waypointX, waypointY, x, y, width, height, previewSizePoint);
        }

        private static Point GetTDRACellPixelCoord(int cellX, int cellY, int x, int y, int width, int height, Point previewSizePoint)
        {
            int rx = cellX - x;
            int ry = cellY - y;

            double ratioX = rx / (double)width;
            double ratioY = ry / (double)height;

            int pixelX = (int)(ratioX * previewSizePoint.X);
            int pixelY = (int)(ratioY * previewSizePoint.Y);

            return new Point(pixelX, pixelY);
        }

        /// <summary>
        /// Converts a waypoint's coordinate string into pixel coordinates on the preview image.
        /// </summary>
        /// <returns>The waypoint's location on the map preview as a point.</returns>
        private static Point GetIsometricWaypointCoords(string waypoint, string[] actualSizeValues, string[] localSizeValues,
            Point previewSizePoint)
        {
            string[] parts = waypoint.Split(',');

            int xCoordIndex = parts[0].Length - 3;

            int isoTileY = Convert.ToInt32(parts[0].Substring(0, xCoordIndex), CultureInfo.InvariantCulture);
            int isoTileX = Convert.ToInt32(parts[0].Substring(xCoordIndex), CultureInfo.InvariantCulture);

            int level = 0;

            if (parts.Length > 1)
                level = Conversions.IntFromString(parts[1], 0);

            return GetIsoTilePixelCoord(isoTileX, isoTileY, actualSizeValues, localSizeValues, previewSizePoint, level);
        }

        private static Point GetIsoTilePixelCoord(int isoTileX, int isoTileY, string[] actualSizeValues, string[] localSizeValues, Point previewSizePoint, int level)
        {
            try
            {
                int rx = isoTileX - isoTileY + Convert.ToInt32(actualSizeValues[2], CultureInfo.InvariantCulture) - 1;
                int ry = isoTileX + isoTileY - Convert.ToInt32(actualSizeValues[2], CultureInfo.InvariantCulture) - 1;

                int pixelPosX = rx * MainClientConstants.MAP_CELL_SIZE_X / 2;
                int pixelPosY = ry * MainClientConstants.MAP_CELL_SIZE_Y / 2 - level * MainClientConstants.MAP_CELL_SIZE_Y / 2;

                pixelPosX = pixelPosX - (Convert.ToInt32(localSizeValues[0], CultureInfo.InvariantCulture) * MainClientConstants.MAP_CELL_SIZE_X);
                pixelPosY = pixelPosY - (Convert.ToInt32(localSizeValues[1], CultureInfo.InvariantCulture) * MainClientConstants.MAP_CELL_SIZE_Y);

                // Calculate map size
                int mapSizeX = Convert.ToInt32(localSizeValues[2], CultureInfo.InvariantCulture) * MainClientConstants.MAP_CELL_SIZE_X;
                int mapSizeY = Convert.ToInt32(localSizeValues[3], CultureInfo.InvariantCulture) * MainClientConstants.MAP_CELL_SIZE_Y;

                double ratioX = Convert.ToDouble(pixelPosX) / mapSizeX;
                double ratioY = Convert.ToDouble(pixelPosY) / mapSizeY;

                int pixelX = Convert.ToInt32(ratioX * previewSizePoint.X);
                int pixelY = Convert.ToInt32(ratioY * previewSizePoint.Y);

                return new Point(pixelX, pixelY);
            }
            catch
            {
                return new Point(0, 0);
            }

        }

        protected bool Equals(Map other) => string.Equals(SHA1, other?.SHA1, StringComparison.InvariantCultureIgnoreCase);

        public override int GetHashCode() => SHA1 != null ? SHA1.GetHashCode() : 0;
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using Serilog;
using RandomMapGenerator.NonTileObjects;
using RandomMapGenerator.TileInfo;
using Rampastring.Tools;
using Weighted_Randomizer;

namespace RandomMapGenerator
{
    public static class WorkingMap
    {
        public static int Width { get; private set; }
        public static int Height { get; private set; }
        public static int[] Size { get; private set; }
        public static AbstractTile[,] AbsTile { get; private set; }
        public static int MapTheater { get; private set; }
        public static List<AbstractMapUnit> AbstractMapUnitList { get; private set; }
        public static AbstractMapMember[,] AbstractMapMemberMatrix { get; private set; }
        public static List<int[]> PlacedAbstractMapUnitRecord { get; private set; }
        public static List<FailureAbstractMapUnitRecord> FailureAbsMapUnitRecordList { get; private set; }
        public static List<Unit> UnitList { get; private set; }
        public static List<Infantry> InfantryList { get; private set; }
        public static List<Structure> StructureList { get; private set; }
        public static List<Terrain> TerrainList { get; private set; }
        public static List<Aircraft> AircraftList { get; private set; }
        public static List<Smudge> SmudgeList { get; private set; }
        public static List<Overlay> OverlayList { get; private set; }
        public static List<Waypoint> WaypointList { get; private set; }
        public static int IndicatorNum { get; private set; }
        public static string Path { get; private set; }
        public static int MapUnitWidth { get; private set; }
        public static int MapUnitHeight { get; private set; }
        public static int StartingX { get; private set; }
        public static int StartingY { get; private set; }
        public static IniFile Rules { get; private set; }
        public static IniFile Art { get; private set; }
        public static List<AbstractTileType> CannotPlaceSmudgeList { get; private set; }
        public static Random Randomizer { get; private set; }
        public static int BottomSpace { get; private set; }

        private const string RulesPath = "Resources\\RandomMapGenerator_RA2\\rulesmd.ini";
        private const string ArtPath = "Resources\\RandomMapGenerator_RA2\\artmd.ini";

        public static void SterilizeMapUnit(string path)
        {
            if (!path.EndsWith("\\"))
                path = path + "\\";
            Path = path;

            AbstractMapUnitList = new List<AbstractMapUnit>();
            CannotPlaceSmudgeList = new List<AbstractTileType>();
            DirectoryInfo root = new DirectoryInfo(Path);

            var indicatorMap = new MapFile();
            indicatorMap.CreateIsoTileList(Path + "indicator.map");
            IndicatorNum = indicatorMap.IsoTileList[0].TileNum;

            if (File.Exists(Path + "cannotplacesmudge.map"))
            {
                var smudgeMap = new MapFile();
                smudgeMap.CreateIsoTileList(Path + "cannotplacesmudge.map");

                foreach (var tile in smudgeMap.IsoTileList)
                {
                    if (tile.TileNum != 0 && tile.TileNum != -1)
                    {

                        var absTileType = new AbstractTileType();
                        absTileType.TileNum = tile.TileNum;
                        absTileType.SubTile = tile.SubTile;
                        CannotPlaceSmudgeList.Add(absTileType);
                    }
                }
            }

            foreach (FileInfo f in root.GetFiles())
            {
                if ((f.Extension == ".map" || f.Extension == ".yrm" || f.Extension == ".mpr") && f.Name != "indicator.map" && f.Name != "cannotplacesmudge.map")
                {
                    var absMapUnit = new AbstractMapUnit();
                    absMapUnit.Initialize(f);
                    AbstractMapUnitList.Add(absMapUnit);
                }
            }
        }
        public static void Initialize(int width, int height, string path)
        {
            var settings = new IniFile(path + "settings.ini");
            MapUnitWidth = int.Parse(settings.GetStringValue("settings", "MapUnitSize", "25x25").Split('x')[0]);
            MapUnitHeight = int.Parse(settings.GetStringValue("settings", "MapUnitSize", "25x25").Split('x')[1]);
            StartingX = int.Parse(settings.GetStringValue("settings", "TopCorner", "18,18").Split(',')[0]);
            StartingY = int.Parse(settings.GetStringValue("settings", "TopCorner", "18,18").Split(',')[1]);

            Width = width;
            Height = height;
            int range = Width + Height;
            AbsTile = new AbstractTile[range, range];
            Size = new int[2] { Width, Height };

            AbstractMapMemberMatrix = new AbstractMapMember [(int)Math.Ceiling((float)range / (float)MapUnitWidth), (int)Math.Ceiling((float)range / (float)MapUnitHeight)];
            PlacedAbstractMapUnitRecord = new List<int[]>();
            FailureAbsMapUnitRecordList = new List<FailureAbstractMapUnitRecord>();
            UnitList = new List<Unit>();
            InfantryList = new List<Infantry>();
            StructureList = new List<Structure>();
            TerrainList = new List<Terrain>();
            AircraftList = new List<Aircraft>();
            SmudgeList = new List<Smudge>();
            OverlayList = new List<Overlay>();
            WaypointList = new List<Waypoint>();
            Randomizer = new Random();

            Rules = new IniFile(RulesPath);
            Art = new IniFile(ArtPath);

            BottomSpace = settings.GetIntValue("settings", "BottomSpace", 4);


            string theater = settings.GetStringValue("settings", "Theater", "NEWURBAN");
            if (!string.IsNullOrEmpty(theater))
            {
                switch (Enum.Parse(typeof(Theater), theater))
                {
                    case Theater.NEWURBAN:
                        MapTheater = (int)Theater.NEWURBAN;
                        break;
                    case Theater.URBAN:
                        MapTheater = (int)Theater.URBAN;
                        break;
                    case Theater.TEMPERATE:
                        MapTheater = (int)Theater.TEMPERATE;
                        break;
                    case Theater.LUNAR:
                        MapTheater = (int)Theater.LUNAR;
                        break;
                    case Theater.DESERT:
                        MapTheater = (int)Theater.DESERT;
                        break;
                    case Theater.SNOW:
                        MapTheater = (int)Theater.SNOW;
                        break;
                    default:
                        MapTheater = (int)Theater.TEMPERATE;
                        break;
                }
            }

            for (int y = 0; y < range; y++)
            {
                for (int x = 0; x < range; x++)
                {
                    var absTile = new AbstractTile();
                    absTile.Initialize(x, y);
                    AbsTile[x, y] = absTile;
                }
            }
            for (int i = 0; i < AbstractMapMemberMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < AbstractMapMemberMatrix.GetLength(1); j++)
                {
                    AbstractMapMemberMatrix[i, j] = new AbstractMapMember();
                }
            }

            for (int i = 0; i < AbstractMapMemberMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < AbstractMapMemberMatrix.GetLength(1); j++)
                {
                    AbstractMapMemberMatrix[i, j].IsAllOnVisibleMap = true;
                    for (int x = 0; x < MapUnitWidth; x++)
                    {
                        for (int y = 0; y < MapUnitHeight; y++)
                        {
                            if (i * MapUnitWidth + x < range && j * MapUnitHeight + y < range)
                            {
                                if (IsValidAT(i * MapUnitWidth + x, j * MapUnitHeight + y))
                                {
                                    if (AbsTile[i * MapUnitWidth + x, j * MapUnitHeight + y].IsOnMap)
                                        AbstractMapMemberMatrix[i, j].IsOnMap = true;
                                    if (!AbsTile[i * MapUnitWidth + x, j * MapUnitHeight + y].IsOnVisibleMap)
                                        AbstractMapMemberMatrix[i, j].IsAllOnVisibleMap = false;
                                }
                            }
                        }
                    }
                    if (!AbstractMapMemberMatrix[i, j].IsOnMap)
                        AbstractMapMemberMatrix[i, j].IsAllOnVisibleMap = false;

                    if (!AbstractMapMemberMatrix[i, j].IsOnMap)
                        AbstractMapMemberMatrix[i, j].Placed = true;

                    if (IsValidAMMM(i,j -1))
                    {
                        if (!AbstractMapMemberMatrix[i, j - 1].IsOnMap)
                            AbstractMapMemberMatrix[i, j].NEConnected = true;
                    }
                    else
                        AbstractMapMemberMatrix[i, j].NEConnected = true;

                    if (IsValidAMMM(i, j + 1))
                    {
                        if (!AbstractMapMemberMatrix[i, j + 1].IsOnMap)
                            AbstractMapMemberMatrix[i, j].SWConnected = true;
                    }
                    else
                        AbstractMapMemberMatrix[i, j].SWConnected = true;

                    if (IsValidAMMM(i - 1, j))
                    {
                        if (!AbstractMapMemberMatrix[i - 1, j].IsOnMap)
                            AbstractMapMemberMatrix[i, j].NWConnected = true;
                    }
                    else
                        AbstractMapMemberMatrix[i, j].NWConnected = true;

                    if (IsValidAMMM(i + 1, j))
                    {
                        if (!AbstractMapMemberMatrix[i + 1, j].IsOnMap)
                            AbstractMapMemberMatrix[i, j].SEConnected = true;
                    }
                    else
                        AbstractMapMemberMatrix[i, j].SEConnected = true;
                }
            }

        }
        public static bool IsValidAMMM(int x, int y)
        {
            if (x < AbstractMapMemberMatrix.GetLength(0) && x >= 0 && y < AbstractMapMemberMatrix.GetLength(1) && y >= 0)
                return true;
            else
                return false;
        }
        public static bool IsValidAT(int x, int y)
        {
            if (x < Width + Height && y < Width + Height && x >= 0 && y >= 0)
                return true;
            else
                return false;
        }

        public static bool IsOnMapAT(int x, int y)
        {
            bool isOnMap = false;
            if (y + x > Width
                && x + y < 2 * Height + Width + 1
                && y - x < Width
                && x - y < Width)
            {
                isOnMap = true;
            }
            return isOnMap;
        }

        public static AbstractMapUnit GetAbstractMapUnitByName(string mapUnitName)
        {
            var absMapUnit = new AbstractMapUnit();
            foreach (var pAbsMapUnit in AbstractMapUnitList)
            {
                if (mapUnitName == pAbsMapUnit.MapUnitName)
                {
                    absMapUnit = pAbsMapUnit;
                }
            }
            return absMapUnit;
        }
        public static AbstractMapMember[] GetNearbyAbstractMapMemberInfo(int x, int y)
        {
            //order : NE NW SW SE
            var absMapMember = new AbstractMapMember[4];
            if (IsValidAMMM(x, y - 1))
            {
                absMapMember[0] = AbstractMapMemberMatrix[x, y - 1];
            }
            if (IsValidAMMM(x - 1, y))
            {
                absMapMember[1] = AbstractMapMemberMatrix[x - 1, y];
            }
            if (IsValidAMMM(x, y + 1))
            {
                absMapMember[2] = AbstractMapMemberMatrix[x, y + 1];
            }
            if (IsValidAMMM(x + 1, y))
            {
                absMapMember[3] = AbstractMapMemberMatrix[x + 1, y];
            }
            return absMapMember;
        }

        public static void PlaceMapUnitToWorkingMap(int x, int y, string mapUnitName)
        {
            foreach (var mapUnit in AbstractMapUnitList)
            {
                if (mapUnit.MapUnitName == mapUnitName)
                {
                    mapUnit.UseTimes++;
                    break;
                }
            }
            var absMapUnit = GetAbstractMapUnitByName(mapUnitName);
            AbstractMapMemberMatrix[x, y].Placed = true;

            for (int i = 0; i < MapUnitWidth; i++)
            {
                for (int j = 0; j < MapUnitHeight; j++)
                {
                    if (IsValidAT(x * MapUnitWidth + i, y * MapUnitHeight + j))
                    {
                        var absTileType = absMapUnit.AbsTileType[i, j];
                        var absTile = new AbstractTile();
                        absTile.SetProperty(x * MapUnitWidth + i, y * MapUnitHeight + j, 0, absTileType);
                        AbsTile[x * MapUnitWidth + i, y * MapUnitHeight + j] = absTile;
                    }
                }
            }            
        }

        public static void CreateNonTileObjectLists()
        {
            Log.Information("******************************************************");
            Log.Information("Start creating non-tile objects");
            Log.Information("******************************************************");
            for (int i = 0; i < AbstractMapMemberMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < AbstractMapMemberMatrix.GetLength(1); j++)
                {
                    var absMapMember = AbstractMapMemberMatrix[i, j];
                    var unitList = absMapMember.GetAbstractMapUnit().UnitList;
                    var infantryList = absMapMember.GetAbstractMapUnit().InfantryList;
                    var structureList = absMapMember.GetAbstractMapUnit().StructureList;
                    var terrainList = absMapMember.GetAbstractMapUnit().TerrainList;
                    var aircraftList = absMapMember.GetAbstractMapUnit().AircraftList;
                    var smudgeList = absMapMember.GetAbstractMapUnit().SmudgeList;
                    var overlayList = absMapMember.GetAbstractMapUnit().OverlayList;
                    var waypointList = absMapMember.GetAbstractMapUnit().WaypointList;

                    if (structureList != null && structureList.Count > 0)
                    {
                        for (int k = 0; k < structureList.Count; k++)
                        {
                            var newStructure = structureList[k].Clone();
                            newStructure.X = newStructure.RelativeX + i * MapUnitWidth;
                            newStructure.Y = newStructure.RelativeY + j * MapUnitHeight;

                            if (IsValidAT(newStructure.X, newStructure.Y) && IsOnMapAT(newStructure.X, newStructure.Y))
                            {
                                if (CanPlaceStructure(newStructure.X, newStructure.Y, newStructure.Name))
                                {
                                    StructureList.Add(newStructure);
                                    Log.Information("Add structure [{0}] in [{1},{2}]", newStructure.Name, newStructure.X, newStructure.Y);
                                    for (int l = 0; l < GetStructureSize(newStructure.Name)[0]; l++)
                                    {
                                        for (int m = 0; m < GetStructureSize(newStructure.Name)[1]; m++)
                                        {
                                            AbsTile[newStructure.X + l, newStructure.Y + m].HasStructure = true;
                                            if (IsNeuralTechBuilding(newStructure.Name))
                                            {
                                                AbsTile[newStructure.X + l, newStructure.Y + m].HasNeuralTechStructure = true;
                                            }
                                        }
                                    }
                                }
                                else
                                    Log.Warning("Cannot structure unit [{0}] in [{1},{2}] because it is blocked", newStructure.Name, newStructure.X, newStructure.Y);
                            }
                        }
                    }
                    if (unitList != null && unitList.Count > 0)
                    {    
                        for (int k = 0; k < unitList.Count; k++)
                        {
                            var newUnit = unitList[k].Clone();
                            newUnit.X = newUnit.RelativeX + i * MapUnitWidth;
                            newUnit.Y = newUnit.RelativeY + j * MapUnitHeight;

                            if (IsValidAT(newUnit.X, newUnit.Y) && IsOnMapAT(newUnit.X, newUnit.Y))
                            {
                                if (CanPlaceUnit(newUnit.X, newUnit.Y))
                                {
                                    UnitList.Add(newUnit);
                                    Log.Information("Add unit [{0}] in [{1},{2}]", newUnit.Name, newUnit.X, newUnit.Y);
                                    AbsTile[newUnit.X, newUnit.Y].HasUnit = true;
                                }
                                else
                                    Log.Warning("Cannot place unit [{0}] in [{1},{2}] because it is blocked", newUnit.Name, newUnit.X, newUnit.Y);
                            }
                        }
                    }
                    if (infantryList != null && infantryList.Count > 0)
                    {
                        for (int k = 0; k < infantryList.Count; k++)
                        {
                            var newInfantry = infantryList[k].Clone();
                            newInfantry.X = newInfantry.RelativeX + i * MapUnitWidth;
                            newInfantry.Y = newInfantry.RelativeY + j * MapUnitHeight;

                            if (IsValidAT(newInfantry.X, newInfantry.Y) && IsOnMapAT(newInfantry.X, newInfantry.Y))
                            {
                                if (CanPlaceInfantry(newInfantry.X, newInfantry.Y))
                                {
                                    InfantryList.Add(newInfantry);
                                    Log.Information("Add infantry [{0}] in [{1},{2}]", newInfantry.Name, newInfantry.X, newInfantry.Y);
                                    AbsTile[newInfantry.X, newInfantry.Y].HasInfantry = true;
                                    AbsTile[newInfantry.X, newInfantry.Y].InfantryCount++;
                                }
                                else
                                    Log.Warning("Cannot place infantry [{0}] in [{1},{2}] because it is blocked", newInfantry.Name, newInfantry.X, newInfantry.Y);
                            }
                        }
                    }
                    if (terrainList != null && terrainList.Count > 0)
                    {
                        for (int k = 0; k < terrainList.Count; k++)
                        {
                            var newTerrain = terrainList[k].Clone();
                            newTerrain.X = newTerrain.RelativeX + i * MapUnitWidth;
                            newTerrain.Y = newTerrain.RelativeY + j * MapUnitHeight;

                            if (IsValidAT(newTerrain.X, newTerrain.Y) && IsOnMapAT(newTerrain.X, newTerrain.Y))
                            {
                                if (newTerrain.Name.Contains("TRFF")) // make sure traffic lights can be placed.
                                {
                                    if (CanPlaceTRFF(newTerrain.X, newTerrain.Y))
                                    {
                                        TerrainList.Add(newTerrain);
                                        Log.Information("Add terrain [{0}] in [{1},{2}]", newTerrain.Name, newTerrain.X, newTerrain.Y);
                                        AbsTile[newTerrain.X, newTerrain.Y].HasTerrain = true;
                                        AbsTile[newTerrain.X, newTerrain.Y].TerrainName = newTerrain.Name;
                                    }
                                    else
                                        Log.Warning("Cannot place terrain [{0}] in [{1},{2}] because it is blocked", newTerrain.Name, newTerrain.X, newTerrain.Y);
                                }
                                else
                                {
                                    if (CanPlaceTerrain(newTerrain.X, newTerrain.Y))
                                    {
                                        TerrainList.Add(newTerrain);
                                        Log.Information("Add terrain [{0}] in [{1},{2}]", newTerrain.Name, newTerrain.X, newTerrain.Y);
                                        AbsTile[newTerrain.X, newTerrain.Y].HasTerrain = true;
                                        AbsTile[newTerrain.X, newTerrain.Y].TerrainName = newTerrain.Name;
                                    }
                                    else
                                        Log.Warning("Cannot place terrain [{0}] in [{1},{2}] because it is blocked", newTerrain.Name, newTerrain.X, newTerrain.Y);
                                }
                            }
                        }
                    }
                    if (aircraftList != null && aircraftList.Count > 0)
                    {
                        for (int k = 0; k < aircraftList.Count; k++)
                        {
                            var newAircraft = aircraftList[k].Clone();
                            newAircraft.X = newAircraft.RelativeX + i * MapUnitWidth;
                            newAircraft.Y = newAircraft.RelativeY + j * MapUnitHeight;

                            if (IsValidAT(newAircraft.X, newAircraft.Y) && IsOnMapAT(newAircraft.X, newAircraft.Y))
                            {
                                if (CanPlaceAircraft(newAircraft.X, newAircraft.Y))
                                {
                                    AircraftList.Add(newAircraft);
                                    Log.Information("Add aircraft [{0}] in [{1},{2}]", newAircraft.Name, newAircraft.X, newAircraft.Y);
                                    AbsTile[newAircraft.X, newAircraft.Y].HasAircraft = true;
                                }
                                else
                                    Log.Warning("Cannot place aircraft [{0}] in [{1},{2}] because it is blocked", newAircraft.Name, newAircraft.X, newAircraft.Y);
                            }
                        }
                    }
                    if (smudgeList != null && smudgeList.Count > 0)
                    {
                        for (int k = 0; k < smudgeList.Count; k++)
                        {
                            var newSmudge = smudgeList[k].Clone();
                            newSmudge.X = newSmudge.RelativeX + i * MapUnitWidth;
                            newSmudge.Y = newSmudge.RelativeY + j * MapUnitHeight;

                            if (IsValidAT(newSmudge.X, newSmudge.Y) && IsOnMapAT(newSmudge.X, newSmudge.Y))
                            {
                                if (CanPlaceSmudge(newSmudge.X, newSmudge.Y, newSmudge.Name))
                                {
                                    SmudgeList.Add(newSmudge);
                                    Log.Information("Add smudge [{0}] in [{1},{2}]", newSmudge.Name, newSmudge.X, newSmudge.Y);
                                    for (int l = 0; l < GetSmudgeSize(newSmudge.Name)[0]; l++)
                                    {
                                        for (int m = 0; m < GetSmudgeSize(newSmudge.Name)[1]; m++)
                                        {
                                            AbsTile[newSmudge.X + l, newSmudge.Y + m].HasSmudge = true;
                                        }
                                    }
                                }
                                else
                                    Log.Warning("Cannot place smudge [{0}] in [{1},{2}] because it is blocked", newSmudge.Name, newSmudge.X, newSmudge.Y);
                            }
                        }
                    }
                    if (waypointList != null && waypointList.Count > 0)
                    {
                        for (int k = 0; k < waypointList.Count; k++)
                        {
                            var newWaypoint = waypointList[k].Clone();
                            newWaypoint.X = newWaypoint.RelativeX + i * MapUnitWidth;
                            newWaypoint.Y = newWaypoint.RelativeY + j * MapUnitHeight;

                            if (IsValidAT(newWaypoint.X, newWaypoint.Y) && IsOnMapAT(newWaypoint.X, newWaypoint.Y))
                            {
                                WaypointList.Add(newWaypoint);
                                Log.Information("Add waypoint [{0}] in [{1},{2}]", WaypointList.Count() - 1, newWaypoint.X, newWaypoint.Y);
                            }
                        }
                    }
                    if (overlayList != null && overlayList.Count > 0)
                    {
                        for (int k = 0; k < overlayList.Count; k++)
                        {
                            var newOverlay =  new Overlay(overlayList[k].OverlayID, overlayList[k].OverlayValue);//overlayList[k].Clone();
                            var tile = overlayList[k].Tile;
                            newOverlay.Tile = new IsoTile(tile.Dx, tile.Dy, tile.Rx, tile.Ry, tile.Z, tile.TileNum, tile.SubTile);
                            newOverlay.Tile.Rx = (ushort)(newOverlay.Tile.Rx + i * MapUnitWidth);
                            newOverlay.Tile.Ry = (ushort)(newOverlay.Tile.Ry + j * MapUnitHeight);

                            if (IsValidAT(newOverlay.Tile.Rx, newOverlay.Tile.Ry) && IsOnMapAT(newOverlay.Tile.Rx, newOverlay.Tile.Ry))
                            {
                                if (CanPlaceOverlay(newOverlay.Tile.Rx, newOverlay.Tile.Ry))
                                {
                                    OverlayList.Add(newOverlay);
                                    Log.Information("Add overlay [{0},{1}] in [{2},{3}]", newOverlay.OverlayID, newOverlay.OverlayValue, newOverlay.Tile.Rx, newOverlay.Tile.Ry);
                                    AbsTile[newOverlay.Tile.Rx, newOverlay.Tile.Ry].HasOverlay = true;
                                    AbsTile[newOverlay.Tile.Rx, newOverlay.Tile.Ry].OverlayID = (int)newOverlay.OverlayID;
                                    AbsTile[newOverlay.Tile.Rx, newOverlay.Tile.Ry].OverlayValue = (int)newOverlay.OverlayValue;
                                }
                                else
                                    Log.Warning("Cannot place overlay [{0},{1}] in [{2},{3}] because it is blocked", newOverlay.OverlayID, newOverlay.OverlayValue, newOverlay.Tile.Rx, newOverlay.Tile.Ry);
                            }
                        }
                    }
                }
            }
            Log.Information("******************************************************");
            Log.Information("End of creating non-tile objects");
            Log.Information("******************************************************");
        }

        public static void PlaceMapUnitByAbsMapMatrix()
        {
            for (int i = 0; i < AbstractMapMemberMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < AbstractMapMemberMatrix.GetLength(1); j++)
                { 
                    if (AbstractMapMemberMatrix[i,j].IsOnMap)
                    {
                        PlaceMapUnitToWorkingMap(i, j, AbstractMapMemberMatrix[i, j].MapUnitName);
                    }
                }
            }
            CreateNonTileObjectLists();
        }
        public static void CountMapUnitUsage()
        {
            Log.Information("******************************************************");
            Log.Information("Map units usage statistics:");
            foreach (var mapUnit in AbstractMapUnitList)
            {
                Log.Information($"{mapUnit.MapUnitName}: {mapUnit.UseTimes}");
            }
            Log.Information("******************************************************");
        }
        public static void SetMapUnit(int x, int y, string mapUnitName)
        {
            if (IsValidAMMM(x,y))
            {
                AbstractMapMemberMatrix[x, y].MapUnitName = mapUnitName;
                AbstractMapMemberMatrix[x, y].Placed = true;
                RecordPlacedMapUnit(x, y);
            }
        }

        public static void SetMapUnitTest(int x, int y, string mapUnitName)
        {
            if (IsValidAMMM(x, y))
            {
                AbstractMapMemberMatrix[x, y].MapUnitName = mapUnitName;
                AbstractMapMemberMatrix[x, y].Placed = true;
            }
        }
        public static void DeleteMapUnitTest(int x, int y)
        {
            if (IsValidAMMM(x, y))
            {
                AbstractMapMemberMatrix[x, y].MapUnitName = "empty";
                AbstractMapMemberMatrix[x, y].Placed = false;
            }
        }

        public static void RandomSetMapUnit(int x, int y, List<string> mapUnitName)
        {
            IWeightedRandomizer<string> randomizer = new DynamicWeightedRandomizer<string>();

            var nearby = GetNearbyAbstractMapMemberInfo(x, y);
            if (nearby[0] != null)
            {
                for (int i = mapUnitName.Count - 1; i >= 0; i--)
                {
                    if (mapUnitName.Count > 1 && mapUnitName[i] == nearby[0].MapUnitName)
                        mapUnitName.RemoveAt(i);
                }
            }
            if (nearby[1] != null)
            {
                for (int i = mapUnitName.Count - 1; i >= 0; i--)
                {
                    if (mapUnitName.Count > 1 && mapUnitName[i] == nearby[1].MapUnitName)
                        mapUnitName.RemoveAt(i);
                }
            }
            if (nearby[2] != null)
            {
                for (int i = mapUnitName.Count - 1; i >= 0; i--)
                {
                    if (mapUnitName.Count > 1 && mapUnitName[i] == nearby[2].MapUnitName)
                        mapUnitName.RemoveAt(i);
                }
            }
            if (nearby[3] != null)
            {
                for (int i = mapUnitName.Count - 1; i >= 0; i--)
                {
                    if (mapUnitName.Count > 1 && mapUnitName[i] == nearby[3].MapUnitName)
                        mapUnitName.RemoveAt(i);
                }
            }

            foreach (var name in mapUnitName)
            {
                randomizer.Add(name, 1);
            }
            SetMapUnit(x, y, randomizer.NextWithReplacement());
        }

        public static int[] MovePositionCloseToCenter(int x, int y)
        {
            int[] centerL = GetCentralAbsMapMemberLocation();
            if (x < centerL[0])
                x++;
            else if (x > centerL[0])
                x--;

            if (y < centerL[0])
                y++;
            else if (y > centerL[0])
                y--;

            return new int[2] { x, y };
        }

        public static void RandomSetMapUnitNoOverlap(int x, int y, List<string> mapUnitName)
        {
            int[,] group = new int[8,2]
            {
                { x - 1, y - 1},
                { x - 1, y},
                { x - 1, y + 1},
                { x, y - 1},
                { x, y + 1},
                { x + 1, y - 1},
                { x + 1, y},
                { x + 1, y + 1},
            };
            if (AbstractMapMemberMatrix[x,y].Placed || !AbstractMapMemberMatrix[x, y].IsAllOnVisibleMap)
            {

                for (int i = 0; i < group.GetLength(0); i++)
                {
                    if (IsValidAMMM(group[i, 0], group[i, 1]))
                    {
                        if (!AbstractMapMemberMatrix[group[i, 0], group[i, 1]].Placed && AbstractMapMemberMatrix[group[i, 0], group[i, 1]].IsAllOnVisibleMap)
                        {
                            RandomSetMapUnit(group[i, 0], group[i, 1], mapUnitName);
                            return;
                        }
                    }
                }
            }
            else
            {
                RandomSetMapUnit(x, y, mapUnitName);
                return;
            }
            int newX = MovePositionCloseToCenter(x, y)[0];
            int newY = MovePositionCloseToCenter(x, y)[1];

            int[] centerL = GetCentralAbsMapMemberLocation();

            if (newX != centerL[0] || newY != centerL[1])
                RandomSetMapUnitNoOverlap(newX, newY, mapUnitName);
        }

        public static void RecordPlacedMapUnit(int x, int y)
        {
            int[] record = { x, y };
            PlacedAbstractMapUnitRecord.Add(record);
        }
        public static void DeleteMapUnit(int x, int y)
        {
            if (IsValidAMMM(x, y))
            {
                AbstractMapMemberMatrix[x, y].MapUnitName = "empty";
                AbstractMapMemberMatrix[x, y].Placed = false;
                UpdateMapUnitInfo();
                Log.Warning("Delete [{0},{1}] because the next step has no valid options", x, y);
                Log.Information("");
                int removeIndex = -1;
                for(int i = 0; i < PlacedAbstractMapUnitRecord.Count; i++)
                {
                    int[] record = PlacedAbstractMapUnitRecord[i];
                    if (record[0] == x && record[1] == y)
                        removeIndex = i;
                }
                if (removeIndex > -1)
                    PlacedAbstractMapUnitRecord.RemoveAt(removeIndex);
            }
        }

        public static void SetMapUnitByEntropy()
        {
            bool notAllMapUnitsSet = true;
            int failureTimes = 0;
            while (notAllMapUnitsSet)
            {
                UpdateMapUnitInfo();
                notAllMapUnitsSet = false;
                foreach (var absMapMember in AbstractMapMemberMatrix)
                {
                    if (!(absMapMember.SEConnected
                        && absMapMember.NWConnected
                        && absMapMember.SWConnected
                        && absMapMember.NEConnected))
                    {
                        notAllMapUnitsSet = true;
                    }
                }
                int[] targetMapUnit = GetLowestEntropyMU();
                if (targetMapUnit[0] == -1 && targetMapUnit[1] == -1)
                {
                    Log.Information("******************************************************");
                    Log.Information("Successfully place all map units!");
                    Log.Information("******************************************************");
                    return;
                }
                //order : NE NW SW SE
                var nearbyAbsMapMember = GetNearbyAbstractMapMemberInfo(targetMapUnit[0], targetMapUnit[1]);
                var validMapUnitList = GetValidAbsMapUnitList(nearbyAbsMapMember);

                var failureRecord = new FailureAbstractMapUnitRecord();
                List<int> deleteUnitMap = new List<int>();
                foreach (var record in FailureAbsMapUnitRecordList)
                {
                    if (record.IsTargetFailureRecord(targetMapUnit[0], targetMapUnit[1]))
                            failureRecord = record;
                }
                for (int i = 0; i < validMapUnitList.Count; i++)
                {
                    if (failureRecord.IsInFailureRecord(validMapUnitList[i].MapUnitName))
                        deleteUnitMap.Add(i);
                }
                for (int i = validMapUnitList.Count() - 1; i >= 0; i--)
                { 
                    foreach(int record in deleteUnitMap)
                    {
                        if (i == record)
                            validMapUnitList.RemoveAt(i);
                    }
                }

                for (int i = validMapUnitList.Count() - 1; i >= 0; i--)
                {
                    //spawn and tiberium cannot be included into normal lists.
                    if (validMapUnitList[i].MapUnitName.Contains("spawn") || validMapUnitList[i].MapUnitName.Contains("tiberium"))
                        validMapUnitList.RemoveAt(i);
                }

                //check after placing some MU, its nearby MUs have options or not.
                var nearbyAbsMapMemberOfNE = GetNearbyAbstractMapMemberInfo(targetMapUnit[0], targetMapUnit[1] - 1);
                var nearbyAbsMapMemberOfNW = GetNearbyAbstractMapMemberInfo(targetMapUnit[0] - 1, targetMapUnit[1]);
                var nearbyAbsMapMemberOfSE = GetNearbyAbstractMapMemberInfo(targetMapUnit[0] + 1, targetMapUnit[1]);
                var nearbyAbsMapMemberOfSW = GetNearbyAbstractMapMemberInfo(targetMapUnit[0], targetMapUnit[1] + 1);

                if (validMapUnitList.Count > 0)
                {
                    for (int i = validMapUnitList.Count() - 1; i >= 0; i--)
                    {
                        SetMapUnitTest(targetMapUnit[0], targetMapUnit[1], validMapUnitList[i].MapUnitName);
                        var validMapUnitListOfNE = GetValidAbsMapUnitList(nearbyAbsMapMemberOfNE);
                        if (validMapUnitListOfNE.Count < 1)
                        {
                            Log.Warning(validMapUnitList[i].MapUnitName + " is removed because it will cause further struggle");
                            validMapUnitList.RemoveAt(i);
                        }
                            
                        DeleteMapUnitTest(targetMapUnit[0], targetMapUnit[1]);
                    }
                }

                IWeightedRandomizer<string> randomizer = new DynamicWeightedRandomizer<string>();
                if (validMapUnitList.Count > 0)
                {
                    Log.Information("Valid map unit list:");
                    int weight = 0;
                    foreach (var abstractMapUnit in validMapUnitList)
                    {
                        weight += abstractMapUnit.Weight;
                    }
                    if (weight > 0)
                    {
                        foreach (var abstractMapUnit in validMapUnitList)
                        {
                            randomizer.Add(abstractMapUnit.MapUnitName, abstractMapUnit.Weight);
                            weight += abstractMapUnit.Weight;
                            Log.Information("  Name: " + abstractMapUnit.MapUnitName + ", Weight: " + abstractMapUnit.Weight);
                        }
                    }
                    else
                    {
                        foreach (var abstractMapUnit in validMapUnitList)
                        {
                            randomizer.Add(abstractMapUnit.MapUnitName, 1);
                            weight += abstractMapUnit.Weight;
                            Log.Information("  Name: " + abstractMapUnit.MapUnitName + ", Weight: 1");
                        }
                        Log.Warning("Weights are modified because all of them are 0");
                    }
                    var result = randomizer.NextWithReplacement();
                    SetMapUnit(targetMapUnit[0], targetMapUnit[1], result);
                    Log.Information("Choose {0} to place in [{1},{2}]", result, targetMapUnit[0], targetMapUnit[1]);
                    Log.Information("");
                }
                else
                {
                    int[] previousLocation = PlacedAbstractMapUnitRecord.Last();
                    var previousName = AbstractMapMemberMatrix[previousLocation[0], previousLocation[1]].MapUnitName;
                    var failure = new FailureAbstractMapUnitRecord();
                    int existFailureIndex = -1;
                    for (int i = 0; i < FailureAbsMapUnitRecordList.Count; i++)
                    {
                        if (FailureAbsMapUnitRecordList[i].IsTargetFailureRecord(previousLocation[0], previousLocation[1]))
                            existFailureIndex = i;
                    }
                    if (existFailureIndex > -1)
                    {
                        FailureAbsMapUnitRecordList[existFailureIndex].AddFailureRecord(previousLocation[0], previousLocation[1], previousName);
                    }
                    else
                    {
                        failure.AddFailureRecord(previousLocation[0], previousLocation[1], previousName);
                        FailureAbsMapUnitRecordList.Add(failure);
                    }
                    foreach (var failureRec in FailureAbsMapUnitRecordList)
                    {
                        if (failureRec.IsTargetFailureRecord(targetMapUnit[0], targetMapUnit[1]))
                            failureRec.Name.Clear();
                    }
                    DeleteMapUnit(previousLocation[0], previousLocation[1]);


                    failureTimes++;
                    if (failureTimes >= Constants.FailureTimes)
                    {
                        Log.Error("No valid map unit to place!");
                        return;
                    }
                }
            }
        }

        public static int[] GetFirstEmptyMapMember()
        {
            for (int j = 0; j < AbstractMapMemberMatrix.GetLength(1); j++)
            {
                for (int i = 0; i < AbstractMapMemberMatrix.GetLength(0); i++)
                {
                    if (AbstractMapMemberMatrix[i, j].IsOnMap && AbstractMapMemberMatrix[i, j].MapUnitName == "empty")
                        return new int[] { i, j };
                }
            }
            return new int[] { 0, 0 };
        }
        public static void SetMapUnitByOrder()
        {
            bool notAllMapUnitsSet = true;
            int failureTimes = 0;
            while (notAllMapUnitsSet)
            {
                UpdateMapUnitInfo();
                notAllMapUnitsSet = false;
                foreach (var absMapMember in AbstractMapMemberMatrix)
                {
                    if (!(absMapMember.SEConnected
                        && absMapMember.NWConnected
                        && absMapMember.SWConnected
                        && absMapMember.NEConnected))
                    {
                        notAllMapUnitsSet = true;
                    }
                }
                int[] targetMapUnit = GetFirstEmptyMapMember();
                if (targetMapUnit[0] == -1 && targetMapUnit[1] == -1)
                {
                    Log.Information("******************************************************");
                    Log.Information("Successfully place all map units!");
                    Log.Information("******************************************************");
                    return;
                }
                //order : NE NW SW SE
                var nearbyAbsMapMember = GetNearbyAbstractMapMemberInfo(targetMapUnit[0], targetMapUnit[1]);

                var validMapUnitList = GetValidAbsMapUnitList(nearbyAbsMapMember);

                var failureRecord = new FailureAbstractMapUnitRecord();
                List<int> deleteUnitMap = new List<int>();
                foreach (var record in FailureAbsMapUnitRecordList)
                {
                    if (record.IsTargetFailureRecord(targetMapUnit[0], targetMapUnit[1]))
                        failureRecord = record;
                }
                for (int i = 0; i < validMapUnitList.Count; i++)
                {
                    if (failureRecord.IsInFailureRecord(validMapUnitList[i].MapUnitName))
                        deleteUnitMap.Add(i);
                }
                for (int i = validMapUnitList.Count() - 1; i >= 0; i--)
                {
                    foreach (int record in deleteUnitMap)
                    {
                        if (i == record)
                            validMapUnitList.RemoveAt(i);
                    }
                }

                //check after placing some MU, its nearby MUs have options or not.
                var nearbyAbsMapMemberOfNE = GetNearbyAbstractMapMemberInfo(targetMapUnit[0], targetMapUnit[1] - 1);
                var nearbyAbsMapMemberOfNW = GetNearbyAbstractMapMemberInfo(targetMapUnit[0] - 1, targetMapUnit[1]);
                var nearbyAbsMapMemberOfSE = GetNearbyAbstractMapMemberInfo(targetMapUnit[0] + 1, targetMapUnit[1]);
                var nearbyAbsMapMemberOfSW = GetNearbyAbstractMapMemberInfo(targetMapUnit[0], targetMapUnit[1] + 1);

                if (validMapUnitList.Count > 0)
                {
                    for (int i = validMapUnitList.Count() - 1; i >= 0; i--)
                    {
                        SetMapUnitTest(targetMapUnit[0], targetMapUnit[1], validMapUnitList[i].MapUnitName);
                        var validMapUnitListOfNE = GetValidAbsMapUnitList(nearbyAbsMapMemberOfNE);
                        if (validMapUnitListOfNE.Count < 1)
                        {
                            Log.Warning(validMapUnitList[i].MapUnitName + " is removed because it will cause further struggle");
                            validMapUnitList.RemoveAt(i);
                        }

                        DeleteMapUnitTest(targetMapUnit[0], targetMapUnit[1]);
                    }
                }

                IWeightedRandomizer<string> randomizer = new DynamicWeightedRandomizer<string>();
                if (validMapUnitList.Count > 0)
                {
                    Log.Information("Valid map unit list:");
                    int weight = 0;
                    foreach (var abstractMapUnit in validMapUnitList)
                    {
                        weight += abstractMapUnit.Weight;
                    }
                    if (weight > 0)
                    {
                        foreach (var abstractMapUnit in validMapUnitList)
                        {
                            randomizer.Add(abstractMapUnit.MapUnitName, abstractMapUnit.Weight);
                            weight += abstractMapUnit.Weight;
                            Log.Information("  Name: " + abstractMapUnit.MapUnitName + ", Weight: " + abstractMapUnit.Weight);
                        }
                    }
                    else
                    {
                        foreach (var abstractMapUnit in validMapUnitList)
                        {
                            randomizer.Add(abstractMapUnit.MapUnitName, 1);
                            weight += abstractMapUnit.Weight;
                            Log.Information("  Name: " + abstractMapUnit.MapUnitName + ", Weight: 1");
                        }
                        Log.Warning("Weights are modified because all of them are 0");
                    }
                    var result = randomizer.NextWithReplacement();
                    SetMapUnit(targetMapUnit[0], targetMapUnit[1], result);
                    Log.Information("Choose {0} to place in [{1},{2}]", result, targetMapUnit[0], targetMapUnit[1]);
                    Log.Information("");
                }
                else
                {
                    int[] previousLocation = PlacedAbstractMapUnitRecord.Last();
                    var previousName = AbstractMapMemberMatrix[previousLocation[0], previousLocation[1]].MapUnitName;
                    var failure = new FailureAbstractMapUnitRecord();
                    int existFailureIndex = -1;
                    for (int i = 0; i < FailureAbsMapUnitRecordList.Count; i++)
                    {
                        if (FailureAbsMapUnitRecordList[i].IsTargetFailureRecord(previousLocation[0], previousLocation[1]))
                            existFailureIndex = i;
                    }
                    if (existFailureIndex > -1)
                    {
                        FailureAbsMapUnitRecordList[existFailureIndex].AddFailureRecord(previousLocation[0], previousLocation[1], previousName);
                    }
                    else
                    {
                        failure.AddFailureRecord(previousLocation[0], previousLocation[1], previousName);
                        FailureAbsMapUnitRecordList.Add(failure);
                    }
                    foreach (var failureRec in FailureAbsMapUnitRecordList)
                    {
                        if (failureRec.IsTargetFailureRecord(targetMapUnit[0], targetMapUnit[1]))
                            failureRec.Name.Clear();
                    }
                    DeleteMapUnit(previousLocation[0], previousLocation[1]);


                    failureTimes++;
                    if (failureTimes >= Constants.FailureTimes)
                    {
                        Log.Error("No valid map unit to place!");
                        return;
                    }
                }
            }
        }
        public static void FillRemainingEmptyUnitMap()
        {
            UpdateMapUnitInfo();
            Log.Information("******************************************************");
            Log.Information("Strat filling remaining empty unit map");
            Log.Information("******************************************************");
            int count = 0;
            for (int i = 0; i < AbstractMapMemberMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < AbstractMapMemberMatrix.GetLength(1); j++)
                {
                    var unitMap = AbstractMapMemberMatrix[i, j];
                    if (unitMap.MapUnitName == "empty" && unitMap.IsOnMap && !unitMap.Placed)
                    {
                        var nearbyAbsMapMember = GetNearbyAbstractMapMemberInfo(i,j);
                        var validMapUnitList = GetValidAbsMapUnitList(nearbyAbsMapMember);
                        IWeightedRandomizer<string> randomizer = new DynamicWeightedRandomizer<string>();
                        if (validMapUnitList.Count > 0)
                        {
                            Log.Information("Valid map unit list:");
                            int weight = 0;
                            foreach (var abstractMapUnit in validMapUnitList)
                            {
                                weight += abstractMapUnit.Weight;
                            }
                            if (weight > 0)
                            {
                                foreach (var abstractMapUnit in validMapUnitList)
                                {
                                    randomizer.Add(abstractMapUnit.MapUnitName, abstractMapUnit.Weight);
                                    weight += abstractMapUnit.Weight;
                                    Log.Information("  Name: " + abstractMapUnit.MapUnitName + ", Weight: " + abstractMapUnit.Weight);
                                }
                            }
                            else
                            {
                                foreach (var abstractMapUnit in validMapUnitList)
                                {
                                    randomizer.Add(abstractMapUnit.MapUnitName, 1);
                                    weight += abstractMapUnit.Weight;
                                    Log.Information("  Name: " + abstractMapUnit.MapUnitName + ", Weight: 1");
                                    Log.Information("Weights are modified because all of them are 0");
                                }
                            }
                            var result = randomizer.NextWithReplacement();
                            SetMapUnit(i, j, result);
                            count++;
                            Log.Information("Choose {0} to place in [{1},{2}]", result, i,j);
                            Log.Information("");
                        }
                        else
                        {
                            Log.Error("Failed to fill empty unit map in [{0},{1}]", i, j);
                            Log.Information("");
                        }
                    }
                }
            }
            Log.Information("End of filling remaining empty unit map");
            Log.Information("Counts: {0}",count);
            Log.Information("******************************************************");
        }

        public static List<AbstractMapUnit> GetValidAbsMapUnitList(AbstractMapMember[] nearbyUnitMap)
        {
            var validMapUnitList = new List<AbstractMapUnit>();

            for (int i = 0; i < AbstractMapUnitList.Count; i++)
            {
                if (AbstractMapUnitList[i].MapUnitName == "empty")
                    continue;
                int conditionsMet = 0;
                if (nearbyUnitMap[0] != null)
                {
                    if (nearbyUnitMap[0].MapUnitName != "empty" && nearbyUnitMap[0].IsOnMap)
                    {
                        if (AbstractMapUnitList[i].NEConnectionType == nearbyUnitMap[0].GetAbstractMapUnit().SWConnectionType)
                            conditionsMet++;
                    }
                    else
                        conditionsMet++;
                }
                else
                    conditionsMet++;
                if (nearbyUnitMap[1] != null)
                {
                    if (nearbyUnitMap[1].MapUnitName != "empty" && nearbyUnitMap[1].IsOnMap)
                    {
                        if (AbstractMapUnitList[i].NWConnectionType == nearbyUnitMap[1].GetAbstractMapUnit().SEConnectionType)
                            conditionsMet++;
                    }
                    else
                        conditionsMet++;
                }
                else
                    conditionsMet++;
                if (nearbyUnitMap[2] != null)
                {
                    if (nearbyUnitMap[2].MapUnitName != "empty" && nearbyUnitMap[2].IsOnMap)
                    {
                        if (AbstractMapUnitList[i].SWConnectionType == nearbyUnitMap[2].GetAbstractMapUnit().NEConnectionType)
                            conditionsMet++;
                    }
                    else
                        conditionsMet++;
                }
                else
                    conditionsMet++;
                if (nearbyUnitMap[3] != null)
                {
                    if (nearbyUnitMap[3].MapUnitName != "empty" && nearbyUnitMap[3].IsOnMap)
                    {
                        if (AbstractMapUnitList[i].SEConnectionType == nearbyUnitMap[3].GetAbstractMapUnit().NWConnectionType)
                            conditionsMet++;
                    }
                    else
                        conditionsMet++;
                }
                else
                    conditionsMet++;

                if (conditionsMet == 4)
                    validMapUnitList.Add(AbstractMapUnitList[i]);
            }

            int validCount = 0;
            foreach (var mapUnit in validMapUnitList)
            {
                if (mapUnit.Weight > 0)
                    validCount++;
            }
            //remove the same Map Unit with nearby
            for (int i = validMapUnitList.Count() - 1; i >= 0; i--)
            {

                if (validCount > 1)
                {
                    var name = validMapUnitList[i].MapUnitName;
                    if (nearbyUnitMap[0] != null)
                    {
                        if (nearbyUnitMap[0].MapUnitName == name)
                        {
                            validMapUnitList.RemoveAt(i);
                            validCount--;
                            continue;
                        }
                    }
                    if (nearbyUnitMap[1] != null)
                    {
                        if (nearbyUnitMap[1].MapUnitName == name)
                        {
                            validMapUnitList.RemoveAt(i);
                            validCount--;
                            continue;
                        }
                    }
                    if (nearbyUnitMap[2] != null)
                    {
                        if (nearbyUnitMap[2].MapUnitName == name)
                        {
                            validMapUnitList.RemoveAt(i);
                            validCount--;
                            continue;
                        }
                    }
                    if (nearbyUnitMap[3] != null)
                    {
                        if (nearbyUnitMap[3].MapUnitName == name)
                        {
                            validMapUnitList.RemoveAt(i);
                            validCount--;
                            continue;
                        }
                    }
                }
            }
            return validMapUnitList;
        }

        public static int[] GetLowestEntropyMU()
        {
            UpdateMapUnitInfo();
            int[] lowestEntropyMU = { -1, -1 };
            int lowestEntropy = int.MaxValue;
            for (int i = 0; i < AbstractMapMemberMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < AbstractMapMemberMatrix.GetLength(1); j++)
                {
                    if (AbstractMapMemberMatrix[i, j].Entropy < lowestEntropy && AbstractMapMemberMatrix[i, j].MapUnitName == "empty" && AbstractMapMemberMatrix[i, j].IsOnMap)
                    {
                        lowestEntropy = AbstractMapMemberMatrix[i, j].Entropy;
                        lowestEntropyMU[0] = i;
                        lowestEntropyMU[1] = j;
                    }
                }
            }
            return lowestEntropyMU;
        }

        public static void UpdateMapUnitInfo()
        {
            for (int i = 0; i < AbstractMapMemberMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < AbstractMapMemberMatrix.GetLength(1); j++)
                {
                    AbstractMapMemberMatrix[i, j].Entropy = 50;
                    if (IsValidAMMM(i, j - 1))
                    {
                        if (AbstractMapMemberMatrix[i, j - 1].MapUnitName != "empty")
                        {
                            AbstractMapMemberMatrix[i, j].NEConnected = true;
                            AbstractMapMemberMatrix[i, j].Entropy -= 10;
                        }
                    }
                    if (IsValidAMMM(i, j + 1))
                    {
                        if (AbstractMapMemberMatrix[i, j + 1].MapUnitName != "empty")
                        {
                            AbstractMapMemberMatrix[i, j].SWConnected = true;
                            AbstractMapMemberMatrix[i, j].Entropy -= 10;
                        }
                    }
                    if (IsValidAMMM(i - 1, j))
                    {
                        if (AbstractMapMemberMatrix[i - 1, j].MapUnitName != "empty")
                        {
                            AbstractMapMemberMatrix[i, j].NWConnected = true;
                            AbstractMapMemberMatrix[i, j].Entropy -= 10;
                        }
                    }
                    if (IsValidAMMM(i + 1, j))
                    {
                        if (AbstractMapMemberMatrix[i + 1, j].MapUnitName != "empty")
                        {
                            AbstractMapMemberMatrix[i, j].SEConnected = true;
                            AbstractMapMemberMatrix[i, j].Entropy -= 10;
                        }
                    }
                    if (!(AbstractMapMemberMatrix[i, j].SEConnected 
                        && AbstractMapMemberMatrix[i, j].NWConnected 
                        && AbstractMapMemberMatrix[i, j].SWConnected 
                        && AbstractMapMemberMatrix[i, j].NEConnected))
                    {
                        if (AbstractMapMemberMatrix[i, j].SEConnected)
                            AbstractMapMemberMatrix[i, j].Entropy -= 2;
                        if (AbstractMapMemberMatrix[i, j].NWConnected)
                            AbstractMapMemberMatrix[i, j].Entropy -= 2;
                        if (AbstractMapMemberMatrix[i, j].SWConnected)
                            AbstractMapMemberMatrix[i, j].Entropy -= 2;
                        if (AbstractMapMemberMatrix[i, j].NEConnected)
                            AbstractMapMemberMatrix[i, j].Entropy -= 2;
                    }
                    else
                        AbstractMapMemberMatrix[i, j].Entropy = 50;
                }
            }
        }
        
        public static int[] GetCentralAbsMapMemberLocation()
        {
            int range = Width + Height;
            int[] location = { (int)Math.Round((float)range / 2.0 / (float)MapUnitWidth - 0.15), (int)Math.Round((float)range / 2.0 / (float)MapUnitHeight - 0.15) };
            return location;
        }

        public static int[] GetCentralSideLocation(string direction)
        {
            int xLength = AbstractMapMemberMatrix.GetLength(0);
            int yLength = AbstractMapMemberMatrix.GetLength(1);
            if (direction == "N")
            {
                int order = 0;
                int x = (int)Math.Round((float)xLength / 4.0);
                int y = (int)Math.Round((float)yLength / 4.0);
                while (!AbstractMapMemberMatrix[x, y].IsAllOnVisibleMap)
                {
                    if (order % 2 == 0)
                        y += 1;
                    else
                        x += 1;
                    order++;
                }
                return new int[2] { x, y };
            }
            if (direction == "W")
            {
                int order = 0;
                int x = (int)Math.Round((float)xLength / 4.0 - 0.5);
                int y = (int)Math.Round((float)yLength * 3.0 / 4.0);
                while (!AbstractMapMemberMatrix[x, y].IsAllOnVisibleMap)
                {
                    if (order % 2 == 0)
                        y -= 1;
                    else
                        x += 1;
                    order++;
                }
                return new int[2] { x, y };
            }
            if (direction == "S")
            {
                int order = 0;
                int x = (int)Math.Round((float)xLength * 3.0 / 4.0);
                int y = (int)Math.Round((float)yLength * 3.0 / 4.0);
                while (!AbstractMapMemberMatrix[x, y].IsAllOnVisibleMap)
                {
                    if (order % 2 == 0)
                        x -= 1;
                    else
                        y -= 1;
                    order++;
                }
                return new int[2] { x, y };
            }
            if (direction == "E")
            {
                int order = 0;
                int x = (int)Math.Round((float)xLength * 3.0 / 4.0);
                int y = (int)Math.Round((float)yLength / 4.0 - 0.15);
                while (!AbstractMapMemberMatrix[x, y].IsAllOnVisibleMap)
                {
                    if (order % 2 == 0)
                        x -= 1;
                    else
                        y += 1;
                    order++;
                }
                return new int[2] { x, y };
            }
            return null;
        }

        //make sure a group of Abs Map Units have enough place to place in four corners
        public static int[] GetEnoughPlaceAbsMapMemberLocation(string direction, int length)
        {
            int[] result = new int[2] { 1, 1 };
            if (direction == "NW")
            {

                for (int i = 0; i < AbstractMapMemberMatrix.GetLength(0); i++)
                {
                    for (int j = 0; j < AbstractMapMemberMatrix.GetLength(1); j++)
                    {
                        if (IsValidAMMM(i, j) && IsValidAMMM(i, j + length - 1))
                        {
                            if (AbstractMapMemberMatrix[i, j].IsAllOnVisibleMap && AbstractMapMemberMatrix[i, j + length - 1].IsAllOnVisibleMap)
                            {
                                result[0] = i;
                                result[1] = j;
                                return result;
                            }
                        }
                    }
                }
            }
            if (direction == "NE")
            {

                for (int i = 0; i < AbstractMapMemberMatrix.GetLength(1); i++)
                {
                    for (int j = AbstractMapMemberMatrix.GetLength(0) - 1; j >= 0; j--)
                    {
                        if (IsValidAMMM(j, i) && IsValidAMMM(j - length + 1, i))
                        {
                            if (AbstractMapMemberMatrix[j, i].IsAllOnVisibleMap && AbstractMapMemberMatrix[j - length + 1, i].IsAllOnVisibleMap)
                            {
                                result[0] = j;
                                result[1] = i;
                                return result;
                            }
                        }
                    }
                }
            }
            if (direction == "SW")
            {

                for (int i = AbstractMapMemberMatrix.GetLength(1) - 1 ; i >= 0; i--)
                {
                    for (int j = 0 ; j < AbstractMapMemberMatrix.GetLength(0) ; j++)
                    {
                        if (IsValidAMMM(j, i) && IsValidAMMM(j + length - 1, i))
                        {
                            if (AbstractMapMemberMatrix[j, i].IsAllOnVisibleMap && AbstractMapMemberMatrix[j + length - 1, i].IsAllOnVisibleMap)
                            {
                                result[0] = j;
                                result[1] = i;
                                return result;
                            }
                        }
                    }
                }
            }
            if (direction == "SE")
            {

                for (int i = AbstractMapMemberMatrix.GetLength(0) - 1; i >= 0; i--)
                {
                    for (int j = AbstractMapMemberMatrix.GetLength(1) - 1; j >= 0; j--)
                    {
                        if (IsValidAMMM(i, j) && IsValidAMMM(i, j - length + 1))
                        {
                            if (AbstractMapMemberMatrix[i, j].IsAllOnVisibleMap && AbstractMapMemberMatrix[i, j - length + 1].IsAllOnVisibleMap)
                            {
                                result[0] = i;
                                result[1] = j;
                                return result;
                            }
                        }
                    }
                }
            }
            return result;
        }
        public static void ReadyForMiniMap()
        {
            for (int k = 0; k < WaypointList.Count; k++)
            {
                if (k > 7)
                    break;

                var waypoint = WaypointList[k];

                AbsTile[waypoint.X, waypoint.Y].AroundPlayerLocation = true;
                AbsTile[waypoint.X + 1, waypoint.Y + 1].AroundPlayerLocation = true;
                AbsTile[waypoint.X - 1, waypoint.Y - 1].AroundPlayerLocation = true;
                AbsTile[waypoint.X + 1, waypoint.Y - 1].AroundPlayerLocation = true;
                AbsTile[waypoint.X - 1, waypoint.Y + 1].AroundPlayerLocation = true;
                AbsTile[waypoint.X + 1, waypoint.Y].AroundPlayerLocation = true;
                AbsTile[waypoint.X - 1, waypoint.Y].AroundPlayerLocation = true;
                AbsTile[waypoint.X, waypoint.Y - 1].AroundPlayerLocation = true;
                AbsTile[waypoint.X, waypoint.Y + 1].AroundPlayerLocation = true;
            }

            for (int k = 0; k < OverlayList.Count; k++)
            {
                var overlay = OverlayList[k];
                var x = overlay.Tile.Rx;
                var y = overlay.Tile.Ry;


                if (overlay.OverlayID >= 74 && overlay.OverlayID <= 82
                    || overlay.OverlayID >= 92 && overlay.OverlayID <= 94
                    || overlay.OverlayID >= 205 && overlay.OverlayID <= 213
                    || overlay.OverlayID >= 223 && overlay.OverlayID <= 226
                    || overlay.OverlayID >= 233 && overlay.OverlayID <= 234) // low bridge NW-SE
                {
                    if (overlay.OverlayValue == 1)
                    {
                        AbsTile[x, y].HasBridge = true;
                        AbsTile[x, y + 1].HasBridge = true;
                        AbsTile[x, y - 1].HasBridge = true;
                    }
                }
                else if (overlay.OverlayID >= 83 && overlay.OverlayID <= 91
                    || overlay.OverlayID >= 96 && overlay.OverlayID <= 99
                    || overlay.OverlayID >= 214 && overlay.OverlayID <= 222
                    || overlay.OverlayID >= 227 && overlay.OverlayID <= 230
                    || overlay.OverlayID >= 235 && overlay.OverlayID <= 236) // low bridge SW-NE
                {
                    if (overlay.OverlayValue == 1)
                    {
                        AbsTile[x, y].HasBridge = true;
                        AbsTile[x + 1, y].HasBridge = true;
                        AbsTile[x - 1, y].HasBridge = true;
                    }
                }
                else if (overlay.OverlayID == 24 || overlay.OverlayID == 25 || overlay.OverlayID == 237 || overlay.OverlayID == 238) // High bridge
                { 
                    if (overlay.OverlayValue >= 0 && overlay.OverlayValue <= 8) //NW-SE
                    {
                        AbsTile[x, y].HasBridge = true;
                        AbsTile[x, y + 1].HasBridge = true;
                        AbsTile[x, y - 1].HasBridge = true;
                    }
                    else if (overlay.OverlayValue >= 9 && overlay.OverlayValue <= 17) //NE-SW
                    {
                        AbsTile[x, y].HasBridge = true;
                        AbsTile[x + 1, y].HasBridge = true;
                        AbsTile[x - 1, y].HasBridge = true;
                    }
                }
            }
        }
        public static int SafeColorInt(int x)
        {
            if (x > 255)
                x = 255;
            if (x < 0)
                x = 0;
            return x;
        }

        public static List<IsoTile> CreateTileList()
        {
            var tileList = new List<IsoTile>();
            int range = Width + Height;

            var minimapIni = new IniFile("Resources\\RandomMapGenerator_RA2\\minimap.ini");
            var section = minimapIni.GetSection(((Theater)MapTheater).ToString());

            for (int y = 0; y < range; y++)
            {
                for (int x = 0; x < range; x++)
                {
                    var absTile = AbsTile[x, y];
                    if (absTile.IsOnMap)
                    {
                        var tile = new IsoTile((ushort)(absTile.X - absTile.Y + Width - 1),
                            (ushort)(absTile.X + absTile.Y - Width - 1),
                            (ushort)absTile.X,
                            (ushort)absTile.Y,
                            (byte)absTile.Z,
                            (short)absTile.TileNum,
                            (byte)absTile.SubTile);

                        var value = section.GetStringValue(absTile.TileNum.ToString(), "255,255,255,255,255,255");
                        var colorcombos = value.Split('/');
                        int index = absTile.SubTile;
                        if (index >= colorcombos.Count())
                            index = 0;
                        var colorcombo = colorcombos[index];
                        var rgbs = colorcombo.Split(',');
                        tile.RadarLeft = Color.FromArgb(SafeColorInt(int.Parse(rgbs[0])), SafeColorInt(int.Parse(rgbs[1])), SafeColorInt(int.Parse(rgbs[2])));
                        tile.RadarRight = Color.FromArgb(SafeColorInt(int.Parse(rgbs[3])), SafeColorInt(int.Parse(rgbs[4])), SafeColorInt(int.Parse(rgbs[5])));


                        if (absTile.HasOverlay)
                        {
                            if (absTile.OverlayID >= 27 && absTile.OverlayID <= 38) //gems
                            {
                                tile.RadarLeft = Color.FromArgb(132, 0, 132);
                                tile.RadarRight = Color.FromArgb(132, 0, 132);
                            }
                            else if (absTile.OverlayID >= 102 && absTile.OverlayID <= 166) //ores
                            {
                                tile.RadarLeft = Color.FromArgb(220, 217, 0);
                                tile.RadarRight = Color.FromArgb(220, 217, 0);
                            }
                            else
                            {
                                tile.RadarLeft = Color.FromArgb(91, 91, 93);
                                tile.RadarRight = Color.FromArgb(91, 91, 93);
                            }
                            if (absTile.OverlayID == 100 || absTile.OverlayID == 101 || absTile.OverlayID == 231 || absTile.OverlayID == 231) //broken bridge
                            {
                                tile.RadarLeft = Color.FromArgb(SafeColorInt(int.Parse(rgbs[0])), SafeColorInt(int.Parse(rgbs[1])), SafeColorInt(int.Parse(rgbs[2])));
                                tile.RadarRight = Color.FromArgb(SafeColorInt(int.Parse(rgbs[3])), SafeColorInt(int.Parse(rgbs[4])), SafeColorInt(int.Parse(rgbs[5])));
                            }
                        }
                        if (absTile.HasBridge)
                        {
                            tile.RadarLeft = Color.FromArgb(107, 109, 107);
                            tile.RadarRight = Color.FromArgb(107, 109, 107);
                        }
                        if (absTile.HasTerrain)
                        {
                            if (absTile.TerrainName.Contains("TREE"))
                            {
                                tile.RadarLeft = Color.FromArgb(0, 194, 0);
                                tile.RadarRight = Color.FromArgb(0, 194, 0);
                            }
                            else if (absTile.TerrainName.Contains("TIBTRE"))
                            {
                                tile.RadarLeft = Color.FromArgb(10, 10, 10);
                                tile.RadarRight = Color.FromArgb(10, 10, 10);
                            }
                            else
                            {
                                tile.RadarLeft = Color.FromArgb(69, 68, 69);
                                tile.RadarRight = Color.FromArgb(69, 68, 69);
                            }
                        }
                        if (absTile.HasStructure || absTile.HasInfantry || absTile.HasUnit || absTile.HasAircraft)
                        {
                            tile.RadarLeft = Color.FromArgb(123, 125, 123);
                            tile.RadarRight = Color.FromArgb(123, 125, 123);
                        }
                        if (absTile.HasNeuralTechStructure)
                        {
                            tile.RadarLeft = Color.FromArgb(215, 215, 215);
                            tile.RadarRight = Color.FromArgb(215, 215, 215);
                        }
                        if (absTile.AroundPlayerLocation)
                        {
                            tile.RadarLeft = Color.FromArgb(220, 0, 0);
                            tile.RadarRight = Color.FromArgb(220, 0, 0);
                        }

                        if (absTile.Z <= 10)
                        {
                            tile.RadarLeft = Color.FromArgb(SafeColorInt(tile.RadarLeft.R + absTile.Z * 2 - 1), SafeColorInt(tile.RadarLeft.G + absTile.Z * 2 - 1), SafeColorInt(tile.RadarLeft.B + absTile.Z * 2 - 1));
                            tile.RadarRight = Color.FromArgb(SafeColorInt(tile.RadarLeft.R + absTile.Z * 2 - 1), SafeColorInt(tile.RadarLeft.G + absTile.Z * 2 - 1), SafeColorInt(tile.RadarLeft.B + absTile.Z * 2 - 1));
                        }
                        else
                        {
                            tile.RadarLeft = Color.FromArgb(SafeColorInt(tile.RadarLeft.R + (int)(absTile.Z * 2.5) - 1), SafeColorInt(tile.RadarLeft.G + (int)(absTile.Z * 2.5) - 1), SafeColorInt(tile.RadarLeft.B + (int)(absTile.Z * 2.5) - 1));
                            tile.RadarRight = Color.FromArgb(SafeColorInt(tile.RadarLeft.R + (int)(absTile.Z * 2.5) - 1), SafeColorInt(tile.RadarLeft.G + (int)(absTile.Z * 2.5) - 1), SafeColorInt(tile.RadarLeft.B + (int)(absTile.Z * 2.5) - 1));
                        }

                        tileList.Add(tile);
                    }
                }
            } 
            return tileList;
        }

        public static IniSection CreateUnitINI()
        {
            if (UnitList.Count == 0)
                return null;
            Log.Information("Creating Unit ini...");
            var unitIniSection = new IniSection("Units");
            int index = 0;
            foreach (var unit in UnitList)
            {
                unitIniSection.AddKey(index.ToString(), unit.CreateINIValue());
                index++;
            }
            return unitIniSection;
        }
        public static IniSection CreateInfantryINI()
        {
            if (InfantryList.Count == 0)
                return null;
            Log.Information("Creating Infantry ini...");
            var infantryIniSection = new IniSection("Infantry");
            int index = 0;
            foreach (var infantry in InfantryList)
            {
                infantryIniSection.AddKey(index.ToString(), infantry.CreateINIValue());
                index++;
            }
            return infantryIniSection;
        }
        public static IniSection CreateStructureINI()
        {
            if (StructureList.Count == 0)
                return null;
            Log.Information("Creating Structure ini...");
            var structureIniSection = new IniSection("Structures");
            int index = 0;
            foreach (var structure in StructureList)
            {
                structureIniSection.AddKey(index.ToString(), structure.CreateINIValue());
                index++;
            }
            return structureIniSection;
        }
        public static IniSection CreateTerrainINI()
        {
            if (TerrainList.Count == 0)
                return null;
            Log.Information("Creating Terrain ini...");
            var terrainIniSection = new IniSection("Terrain");
            foreach (var terrain in TerrainList)
            {
                var iniLine = terrain.CreateINILine();
                //make sure traffic lights can be placed
                if (terrainIniSection.KeyExists(iniLine.Key))
                {
                    if (terrainIniSection.GetStringValue(iniLine.Key,"TREE").Contains("TREE") || terrain.Name.Contains("TRFF"))
                    {
                        terrainIniSection.RemoveKey(iniLine.Key);
                        terrainIniSection.AddKey(iniLine.Key, iniLine.Value);
                    }
                }
                else
                    terrainIniSection.AddKey(iniLine.Key, iniLine.Value);
            }
            return terrainIniSection;
        }
        public static IniSection CreateAircraftINI()
        {
            if (AircraftList.Count == 0)
                return null;
            Log.Information("Creating Aircraft ini...");
            var aircraftIniSection = new IniSection("Aircraft");
            int index = 0;
            foreach (var aircraft in AircraftList)
            {
                aircraftIniSection.AddKey(index.ToString(), aircraft.CreateINIValue());
                index++;
            }
            return aircraftIniSection;
        }
        public static IniSection CreateSmudgeINI()
        {
            if (SmudgeList.Count == 0)
                return null;
            Log.Information("Creating Smudge ini...");
            var smudgeIniSection = new IniSection("Smudge");
            int index = 0;
            foreach (var smudge in SmudgeList)
            {
                smudgeIniSection.AddKey(index.ToString(), smudge.CreateINIValue());
                index++;
            }
            return smudgeIniSection;
        }
        public static IniSection CreateWaypointINI()
        {
            if (WaypointList.Count == 0)
                return null;
            Log.Information("Creating Waypoints ini...");
            var waypointIniSection = new IniSection("Waypoints");
            int index = 0;
            foreach (var waypoint in WaypointList)
            {
                var iniLine = waypoint.CreateINILine();
                waypointIniSection.AddKey(index.ToString(), iniLine.Value);
                index++;
            }
            return waypointIniSection;
        }
        public static void PlacePlayerLocation(int number, string direction)
        {
            if (number == 0)
                return;
            List<string> startingUnits = new List<string>();
            foreach (var absMU in AbstractMapUnitList)
            {
                if (absMU.MapUnitName.Contains("spawn"))
                {
                    startingUnits.Add(absMU.MapUnitName);
                }
            }
            if (startingUnits.Count == 0)
                return;
            int[] playerLocation = new int[2];
            if (direction == "NW")
                playerLocation = GetEnoughPlaceAbsMapMemberLocation("NW", number);
            if (direction == "SW")
                playerLocation = GetEnoughPlaceAbsMapMemberLocation("SW", number);
            if (direction == "SE")
                playerLocation = GetEnoughPlaceAbsMapMemberLocation("SE", number);
            if (direction == "NE")
                playerLocation = GetEnoughPlaceAbsMapMemberLocation("NE", number);
            if (direction == "N")
                playerLocation = GetCentralSideLocation("N");
            if (direction == "S")
                playerLocation = GetCentralSideLocation("S");
            if (direction == "W")
                playerLocation = GetCentralSideLocation("W");
            if (direction == "E")
                playerLocation = GetCentralSideLocation("E");


            if (direction == "NW")
            {
                for (int i = 0; i < number; i++)
                {
                    RandomSetMapUnitNoOverlap(playerLocation[0], playerLocation[1] + i, startingUnits);
                    Log.Information("Player is set in abstract map member [{0},{1}]", playerLocation[0], playerLocation[1] + i);
                }
            }
            else if (direction == "SW")
            {
                for (int i = 0; i < number; i++)
                {
                    RandomSetMapUnitNoOverlap(playerLocation[0] + i, playerLocation[1], startingUnits);
                    Log.Information("Player is set in abstract map member [{0},{1}]", playerLocation[0] + i, playerLocation[1]);
                }
            }
            else if (direction == "SE")
            {
                for (int i = 0; i < number; i++)
                {
                    RandomSetMapUnitNoOverlap(playerLocation[0], playerLocation[1] - i, startingUnits);
                    Log.Information("Player is set in abstract map member [{0},{1}]", playerLocation[0], playerLocation[1] - i);
                }
            }
            else if (direction == "NE")
            {
                for (int i = 0; i < number; i++)
                {
                    RandomSetMapUnitNoOverlap(playerLocation[0] - i, playerLocation[1], startingUnits);
                    Log.Information("Player is set in abstract map member [{0},{1}]", playerLocation[0] - i, playerLocation[1]);
                }
            }
            if (direction == "N")
            {
                RandomSetMapUnitNoOverlap(playerLocation[0], playerLocation[1], startingUnits);
                Log.Information("Player is set in abstract map member [{0},{1}]", playerLocation[0], playerLocation[1]);
                if (number == 1)
                {
                    PlaceTiberiumMUNearPlayer();
                    return;
                }
                RandomSetMapUnitNoOverlap(playerLocation[0] + 1, playerLocation[1] - 1, startingUnits);
                Log.Information("Player is set in abstract map member [{0},{1}]", playerLocation[0] + 1, playerLocation[1] - 1);
                if (number == 2)
                {
                    PlaceTiberiumMUNearPlayer();
                    return;
                }
                RandomSetMapUnitNoOverlap(playerLocation[0] - 1, playerLocation[1] + 1, startingUnits);
                Log.Information("Player is set in abstract map member [{0},{1}]", playerLocation[0] - 1, playerLocation[1] + 1);
                if (number == 3)
                {
                    PlaceTiberiumMUNearPlayer();
                    return;
                }
                RandomSetMapUnitNoOverlap(playerLocation[0] + 2, playerLocation[1] - 2, startingUnits);
                Log.Information("Player is set in abstract map member [{0},{1}]", playerLocation[0] + 2, playerLocation[1] - 2);
            }
            else if (direction == "W")
            {
                RandomSetMapUnitNoOverlap(playerLocation[0], playerLocation[1], startingUnits);
                Log.Information("Player is set in abstract map member [{0},{1}]", playerLocation[0], playerLocation[1]);
                if (number == 1)
                {
                    PlaceTiberiumMUNearPlayer();
                    return;
                }
                RandomSetMapUnitNoOverlap(playerLocation[0] - 1, playerLocation[1] - 1, startingUnits);
                Log.Information("Player is set in abstract map member [{0},{1}]", playerLocation[0] - 1, playerLocation[1] - 1);
                if (number == 2)
                {
                    PlaceTiberiumMUNearPlayer();
                    return;
                }
                RandomSetMapUnitNoOverlap(playerLocation[0] + 1, playerLocation[1] + 1, startingUnits);
                Log.Information("Player is set in abstract map member [{0},{1}]", playerLocation[0] + 1, playerLocation[1] + 1);
                if (number == 3)
                {
                    PlaceTiberiumMUNearPlayer();
                    return;
                }
                RandomSetMapUnitNoOverlap(playerLocation[0] - 2, playerLocation[1] - 2, startingUnits);
                Log.Information("Player is set in abstract map member [{0},{1}]", playerLocation[0] - 2, playerLocation[1] - 2);
            }
            else if (direction == "S")
            {
                RandomSetMapUnitNoOverlap(playerLocation[0], playerLocation[1], startingUnits);
                Log.Information("Player is set in abstract map member [{0},{1}]", playerLocation[0], playerLocation[1]);
                if (number == 1)
                {
                    PlaceTiberiumMUNearPlayer();
                    return;
                }
                RandomSetMapUnitNoOverlap(playerLocation[0] - 1, playerLocation[1] + 1, startingUnits);
                Log.Information("Player is set in abstract map member [{0},{1}]", playerLocation[0] - 1, playerLocation[1] + 1);
                if (number == 2)
                {
                    PlaceTiberiumMUNearPlayer();
                    return;
                }
                RandomSetMapUnitNoOverlap(playerLocation[0] + 1, playerLocation[1] - 1, startingUnits);
                Log.Information("Player is set in abstract map member [{0},{1}]", playerLocation[0] + 1, playerLocation[1] - 1);
                if (number == 3)
                {
                    PlaceTiberiumMUNearPlayer();
                    return;
                }
                RandomSetMapUnitNoOverlap(playerLocation[0] - 2, playerLocation[1] + 2, startingUnits);
                Log.Information("Player is set in abstract map member [{0},{1}]", playerLocation[0] - 2, playerLocation[1] + 2);
            }
            else if (direction == "E")
            {
                RandomSetMapUnitNoOverlap(playerLocation[0], playerLocation[1], startingUnits);
                Log.Information("Player is set in abstract map member [{0},{1}]", playerLocation[0], playerLocation[1]);
                if (number == 1)
                {
                    PlaceTiberiumMUNearPlayer();
                    return;
                }
                RandomSetMapUnitNoOverlap(playerLocation[0] + 1, playerLocation[1] + 1, startingUnits);
                Log.Information("Player is set in abstract map member [{0},{1}]", playerLocation[0] + 1, playerLocation[1] + 1);
                if (number == 2)
                {
                    PlaceTiberiumMUNearPlayer();
                    return;
                }
                RandomSetMapUnitNoOverlap(playerLocation[0] - 1, playerLocation[1] - 1, startingUnits);
                Log.Information("Player is set in abstract map member [{0},{1}]", playerLocation[0] - 1, playerLocation[1] - 1);
                if (number == 3)
                {
                    PlaceTiberiumMUNearPlayer();
                    return;
                }
                RandomSetMapUnitNoOverlap(playerLocation[0] + 2, playerLocation[1] + 2, startingUnits);
                Log.Information("Player is set in abstract map member [{0},{1}]", playerLocation[0] + 2, playerLocation[1] + 2);
            }
            PlaceTiberiumMUNearPlayer();
        }
        //start to place the next group of tiberium
        public static void PlaceTiberiumMUNearPlayer()
        {
            List<string> tiberium1 = new List<string>();
            foreach (var absMU in AbstractMapUnitList)
            {
                if (absMU.MapUnitName.Contains("tiberium1"))
                {
                    tiberium1.Add(absMU.MapUnitName);
                }
            }

            List<string> tiberium2 = new List<string>();
            foreach (var absMU in AbstractMapUnitList)
            {
                if (absMU.MapUnitName.Contains("tiberium2"))
                {
                    tiberium2.Add(absMU.MapUnitName);
                }
            }
            if (tiberium1.Count + tiberium2.Count == 0)
                return;

            int count = 0;
            IWeightedRandomizer<string> randomizer = new DynamicWeightedRandomizer<string>();
            for (int i = 0; i < AbstractMapMemberMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < AbstractMapMemberMatrix.GetLength(1); j++)
                {
                    var amm = AbstractMapMemberMatrix[i, j];
                    if (amm.MapUnitName.Contains("spawn") && !amm.PlayerLocationHasTiberium)
                    {
                        count++;
                        randomizer.Add(i.ToString() + "," + j.ToString(), 1);
                    }
                }
            }
            while (count > 0)
            {
                if (randomizer.Count == 0)
                    break;
                var result = randomizer.NextWithRemoval().Split(',');
                int placeTiberium2Chance = Randomizer.Next(100);
                bool success = false;
                if (count >= 2)
                {
                    if (placeTiberium2Chance > 63 && tiberium2.Count > 0)
                    { 
                        success = RandomPlaceMUNearbyAndAllOnMap(int.Parse(result[0]), int.Parse(result[1]), tiberium2);
                        if (success)
                            count -= 2;
                        continue;
                    }
                }
                if (tiberium1.Count > 0)
                    success = RandomPlaceMUNearbyAndAllOnMap(int.Parse(result[0]), int.Parse(result[1]), tiberium1);
                if (success)
                    count -= 1;
            }

            //sometimes there is no place to set tiberium...
            if (count > 0)
            {
                for (int i = 0; i < AbstractMapMemberMatrix.GetLength(0); i++)
                {
                    for (int j = 0; j < AbstractMapMemberMatrix.GetLength(1); j++)
                    {
                        var amm = AbstractMapMemberMatrix[i, j];
                        if (amm.MapUnitName.Contains("spawn") && !amm.PlayerLocationHasTiberium)
                        {
                            randomizer.Add(i.ToString() + "," + j.ToString(), 1);
                        }
                    }
                }
                int failure = 0;
                while (count > 0)
                {
                    if (randomizer.Count == 0)
                        break;
                    var result = randomizer.NextWithReplacement().Split(',');
                    int placeTiberium2Chance = Randomizer.Next(100);
                    bool success = false;
                    if (count >= 2)
                    {
                        if (placeTiberium2Chance > 63 && tiberium2.Count > 0)
                        {
                            success = RandomPlaceMUNearbyAndAllOnMap(int.Parse(result[0]), int.Parse(result[1]), tiberium2);
                            if (success)
                                count -= 2;
                            continue;
                        }
                    }
                    if (tiberium1.Count > 0)
                        success = RandomPlaceMUNearbyAndAllOnMap(int.Parse(result[0]), int.Parse(result[1]), tiberium1);
                    if (success)
                        count -= 1;
                    failure++;
                    if (failure > 30)
                    {
                        Log.Warning("No place to set tiberium map unit!");
                        break;
                    }
                       
                }
            }

            foreach (var amm in AbstractMapMemberMatrix)
            {
                if (amm.MapUnitName.Contains("spawn") && !amm.PlayerLocationHasTiberium)
                {
                    amm.PlayerLocationHasTiberium = true;
                }
            }
        }
        

        public static bool RandomPlaceMUNearbyAndAllOnMap(int x, int y, List<string> mapUnitName)
        {
            IWeightedRandomizer<int> randomizer = new DynamicWeightedRandomizer<int>();
            if (!AbstractMapMemberMatrix[x, y - 1].Placed && AbstractMapMemberMatrix[x, y - 1].IsAllOnVisibleMap)
                randomizer.Add(1, 1);
            if (!AbstractMapMemberMatrix[x - 1, y].Placed && AbstractMapMemberMatrix[x - 1, y].IsAllOnVisibleMap)
                randomizer.Add(2, 1);
            if (!AbstractMapMemberMatrix[x, y + 1].Placed && AbstractMapMemberMatrix[x, y + 1].IsAllOnVisibleMap)
                randomizer.Add(3, 1);
            if (!AbstractMapMemberMatrix[x + 1, y].Placed && AbstractMapMemberMatrix[x + 1, y].IsAllOnVisibleMap)
                randomizer.Add(4, 1);
            if (randomizer.Count == 0)
                return false;
            var result = randomizer.NextWithReplacement();
            if (result == 1)
                RandomSetMapUnitNoOverlap(x, y - 1, mapUnitName);
            if (result == 2)
                RandomSetMapUnitNoOverlap(x - 1, y, mapUnitName);
            if (result == 3)
                RandomSetMapUnitNoOverlap(x, y + 1, mapUnitName);
            if (result == 4)
                RandomSetMapUnitNoOverlap(x + 1, y, mapUnitName);
            return true;
        }

        public static void RandomPlaceMUInCenter(int chance)
        {
            if (chance < Randomizer.Next(100))
                return;
            List<string> center = new List<string>();
            foreach (var absMU in AbstractMapUnitList)
            {
                if (absMU.MapUnitName.Contains("center"))
                {
                    center.Add(absMU.MapUnitName);
                }
            }
            int[] centerL = GetCentralAbsMapMemberLocation();
            if (center.Count > 0)
                RandomSetMapUnit(centerL[0], centerL[1], center);
        }

        public static void IncreaseWeightContainsX(int type, int times = 1)
        {
            type -= 1;
            for (int j = 0; j < times; j ++)
            {
                for (int i = 0; i < AbstractMapUnitList.Count; i++)
                {
                    var absMU = AbstractMapUnitList[i];
                    if (absMU.Weight != 0)
                    {
                        if (absMU.NEConnectionType == type)
                        {
                            AbstractMapUnitList[i].Weight += 1;
                        }
                        if (absMU.NWConnectionType == type)
                        {
                            AbstractMapUnitList[i].Weight += 1;
                        }
                        if (absMU.SEConnectionType == type)
                        {
                            AbstractMapUnitList[i].Weight += 1;
                        }
                        if (absMU.SWConnectionType == type)
                        {
                            AbstractMapUnitList[i].Weight += 1;
                        }
                    }
                }
            }
        }
        public static bool IsNeuralTechBuilding(string name)
        {
            string[] TechBuildingList = new string[]
            {
                "CATHOSP",
                "CAOILD",
                "CAOUTP",
                "CAMACH",
                "CAPOWR",
                "CASLAB",
                "CAHOSP",
                "CAAIRP"
            };
            if (TechBuildingList.Contains(name))
                return true;
            else
                return false;
        }

        public static void ChangeStructureHealth(int min, int max, int destroyPercentage = 0)
        {
            if (min < 0)
                min = 0;
            if (max > 256)
                max = 256;

            string[] TechBuildingList = new string[]
            {
                "CATHOSP",
                "CAOILD",
                "CAOUTP",
                "CAMACH",
                "CAPOWR",
                "CASLAB",
                "CAHOSP",
                "CAAIRP"
            };

            foreach (var structure in StructureList)
            {
                if (structure.Name != "CABHUT")
                {
                    structure.Strength = Randomizer.Next(min, max);
                    int destroyed = Randomizer.Next(100);
                    if (destroyed < destroyPercentage && !TechBuildingList.Contains(structure.Name))
                        structure.Strength = 0;
                }
            }
        }
        public static void ChangeUnitAirInfHealth(int min, int max)
        {
            if (min < 0)
                min = 0;
            if (max > 256)
                max = 256;

            foreach (var unit in UnitList)
            {
                unit.Strength = Randomizer.Next(min, max);
            }
            foreach (var aircraft in AircraftList)
            {
                aircraft.Strength = Randomizer.Next(min, max);
            }
            foreach (var infantry in InfantryList)
            {
                infantry.Strength = Randomizer.Next(min, max);
            }
        }
        public static int[] GetStructureSize(string name)
        {
            string artName = name;
            if (Rules.SectionExists(name))
            {
                if (Rules.KeyExists(name, "Image"))
                    artName = Rules.GetStringValue(name, "Image", name);
            }
            else return new int[2] { 1, 1 };

            if (!Art.KeyExists(artName, "Foundation"))
                return new int[2] { 1, 1 };
            var foundation = Art.GetStringValue(artName, "Foundation", "1x1");
            int width = int.Parse(foundation.Split(new char[2]{ 'x', 'X'})[0]);
            int height = int.Parse(foundation.Split(new char[2] { 'x', 'X' })[1]);
            return new int[2] { width, height };
        }

        public static int[] GetSmudgeSize(string name)
        {
            int width = 1;
            int height = 1;

            if (Rules.SectionExists(name))
            {
                if (Rules.KeyExists(name, "Width"))
                    width = Rules.GetIntValue(name, "Width", 1);
                if (Rules.KeyExists(name, "Height"))
                    height = Rules.GetIntValue(name, "Height", 1);
            }
            return new int[2] { width, height };
        }

        public static bool CanPlaceStructure(int x, int y, string name)
        {
            for (int i = 0; i < GetStructureSize(name)[0]; i ++)
            {
                for (int j = 0; j < GetStructureSize(name)[1]; j++)
                {
                    if (!IsValidAT(x + i, y + j))
                        return false;
                    var absTile = AbsTile[x + i, y + j];
                    if (absTile.HasStructure || absTile.HasAircraft || absTile.HasUnit || absTile.HasInfantry 
                        || absTile.HasTerrain || absTile.HasOverlay || absTile.HasSmudge)
                        return false;
                }
            }
            return true;
        }

        public static bool CanPlaceUnit(int x, int y)
        {
            var absTile = AbsTile[x , y];
            if (absTile.HasAircraft || absTile.HasUnit || absTile.HasInfantry
                || absTile.HasTerrain) //absTile.HasStructure : sometimes, units are placed on structures like Service Depot.
                return false;
            return true;
        }
        public static bool CanPlaceAircraft(int x, int y)
        {
            return CanPlaceUnit(x, y);
        }
        public static bool CanPlaceTerrain(int x, int y)
        {
            var absTile = AbsTile[x, y];
            if (absTile.HasStructure || absTile.HasAircraft || absTile.HasUnit || absTile.HasInfantry
                || absTile.HasTerrain)
                return false;
            return true;
        }
        public static bool CanPlaceTRFF(int x, int y)
        {
            var absTile = AbsTile[x, y];
            if (absTile.HasStructure || absTile.HasAircraft || absTile.HasUnit || absTile.HasInfantry)
                return false;
            return true;
        }
        public static bool CanPlaceSmudge(int x, int y, string name)
        {
            for (int i = 0; i < GetSmudgeSize(name)[0]; i++)
            {
                for (int j = 0; j < GetSmudgeSize(name)[1]; j++)
                {
                    if (!IsValidAT(x + i, y + j))
                        return false;
                    var absTile = AbsTile[x + i, y + j];
                    if (absTile.HasStructure || absTile.HasTerrain || absTile.HasOverlay || absTile.HasSmudge)
                    {
                        return false;
                    }
                    foreach (var absTileType in CannotPlaceSmudgeList)
                    {
                        if (absTileType.TileNum == absTile.TileNum)
                            return false;
                    }
                }
            }
            return true;
        }
        public static bool CanPlaceInfantry(int x, int y)
        {
            var absTile = AbsTile[x, y];
            if (absTile.HasStructure || absTile.HasAircraft || absTile.HasUnit || absTile.InfantryCount >= 3
                || absTile.HasTerrain)
                return false;
            return true;
        }
        public static bool CanPlaceOverlay(int x, int y)
        {
            var absTile = AbsTile[x, y];
            if (absTile.HasStructure || absTile.HasTerrain || absTile.HasSmudge || absTile.HasOverlay)
                return false;
            return true;
        }
        public static void RandomPlaceSmudge(double density)
        {
            if (density > 0.5)
            {
                Log.Warning("The density is considered too high.");
                return;
            }
            if (density < 0)
            {
                Log.Warning("The density should between 0 and 0.5!");
                return;
            }

            double currentDensity = 0;
            int range = Width + Height;
            string[] smudgeList = new string[] 
            {
                "BURNT01",
                "BURNT02",
                "BURNT03",
                "BURNT04",
                "BURNT05",
                "BURNT06",
                "BURNT07",
                "BURNT08",
                "BURNT09",
                "BURNT10",
                "BURNT11",
                "BURNT12",
                "CRATER01",
                "CRATER02",
                "CRATER03",
                "CRATER04",
                "CRATER05",
                "CRATER06",
                "CRATER07",
                "CRATER08",
                "CRATER09",
                "CRATER10",
                "CRATER11",
                "CRATER12"
            };
            int loopTimes = 0;

            while (density > currentDensity)
            {
                int x = Randomizer.Next(0, range);
                int y = Randomizer.Next(0, range);

                loopTimes++;
                if (loopTimes > (Width * 2 - 1) * Height * 10)
                {
                    Log.Warning("Random place smudge is forcefully stopped because of too many retries.");
                    Log.Warning("Please make sure the density is not too high");
                    break;
                }

                if (!AbsTile[x, y].IsOnMap)
                    continue;

                int choice = Randomizer.Next(smudgeList.Length);
                if (!CanPlaceSmudge(x, y, smudgeList[choice]))
                    continue;

                var newSmudge = new Smudge();
                newSmudge.X = x;
                newSmudge.Y = y;
                newSmudge.Name = smudgeList[choice];

                SmudgeList.Add(newSmudge);
                Log.Information("Random place smudge [{0}] in [{1},{2}]", newSmudge.Name, newSmudge.X, newSmudge.Y);
                for (int l = 0; l < GetSmudgeSize(newSmudge.Name)[0]; l++)
                {
                    for (int m = 0; m < GetSmudgeSize(newSmudge.Name)[1]; m++)
                    {
                        AbsTile[newSmudge.X + l, newSmudge.Y + m].HasSmudge = true;
                    }
                }

                currentDensity = (double)SmudgeList.Count / (double)((Width * 2 - 1) * Height);   
            }
        }
    }
}

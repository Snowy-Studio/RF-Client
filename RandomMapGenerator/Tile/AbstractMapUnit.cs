using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RandomMapGenerator.NonTileObjects;
using Rampastring.Tools;

namespace RandomMapGenerator.TileInfo
{
    public class AbstractMapUnit
    {
        public string MapUnitName;
        public int Width = WorkingMap.MapUnitWidth;
        public int Height = WorkingMap.MapUnitHeight;
        public AbstractTileType[,] AbsTileType = new AbstractTileType[WorkingMap.MapUnitWidth, WorkingMap.MapUnitHeight];
        public int NWConnectionType = -1;
        public int NEConnectionType = -1;
        public int SWConnectionType = -1;
        public int SEConnectionType = -1;
        public int Weight = 0;

        public int UseTimes { get; set; }

        public List<Unit> UnitList { get; private set; }
        public List<Infantry> InfantryList { get; private set; }
        public List<Structure> StructureList { get; private set; }
        public List<Terrain> TerrainList { get; private set; }
        public List<Aircraft> AircraftList { get; private set; }
        public List<Smudge> SmudgeList { get; private set; }
        public List<Overlay> OverlayList { get; private set; }
        public List<Waypoint> WaypointList { get; private set; }

        public void Initialize(FileInfo file)
        {
            UnitList = new List<Unit>();
            InfantryList = new List<Infantry>();
            StructureList = new List<Structure>();
            TerrainList = new List<Terrain>();
            AircraftList = new List<Aircraft>();
            SmudgeList = new List<Smudge>();
            OverlayList = new List<Overlay>();
            WaypointList = new List<Waypoint>();
            UseTimes = 0;

            var map = new MapFile();
            map.CreateIsoTileList(file.FullName);
            var overlayList = map.ReadOverlay(file.FullName);

            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    var absTileType = new AbstractTileType();
                    foreach (var tile in map.IsoTileList)
                    {
                        if (tile.Rx == i + WorkingMap.StartingX && tile.Ry == j + WorkingMap.StartingY)
                        {
                            string name = "";
                            for (int k = 0; k < file.Name.Split('.').Count() - 1; k ++)
                            {
                                name += file.Name.Split('.')[k];
                                if (k != file.Name.Split('.').Count() - 2)
                                    name += ".";
                            }
                            MapUnitName = name;
                            absTileType.TileNum = tile.TileNum;
                            absTileType.SubTile = tile.SubTile;
                            absTileType.Z = tile.Z;
                            AbsTileType[i, j] = absTileType;
                        }
                    }
                    foreach (var overlay in overlayList)
                    {
                        if (overlay.Tile.Rx == i + WorkingMap.StartingX && overlay.Tile.Ry == j + WorkingMap.StartingY)
                        {
                            overlay.Tile.Rx = (ushort)i;
                            overlay.Tile.Ry = (ushort)j;
                            OverlayList.Add(overlay);
                        }
                    }
                }
            }
            for (int i = 0; i < Width; i++)
            {
                foreach (var tile in map.IsoTileList)
                {
                    if (tile.Rx == WorkingMap.StartingX - 3 && tile.Ry == i + WorkingMap.StartingY && tile.TileNum == WorkingMap.IndicatorNum)
                    {
                        NWConnectionType = i;
                    }
                    if (tile.Rx == WorkingMap.StartingX + Width + 2 && tile.Ry == i + WorkingMap.StartingY && tile.TileNum == WorkingMap.IndicatorNum)
                    {
                        SEConnectionType = i;
                    }
                    if (tile.Ry == WorkingMap.StartingX - 3 && tile.Rx == i + WorkingMap.StartingY && tile.TileNum == WorkingMap.IndicatorNum)
                    {
                        NEConnectionType = i;
                    }
                    if (tile.Ry == WorkingMap.StartingX + Width + 2 && tile.Rx == i + WorkingMap.StartingY && tile.TileNum == WorkingMap.IndicatorNum)
                    {
                        SWConnectionType = i;
                    }
                }
            }
            foreach (var tile in map.IsoTileList)
            { 
                if (tile.Rx < WorkingMap.StartingX - 5 && tile.TileNum == WorkingMap.IndicatorNum)
                {
                    Weight++;
                }
            }

            var mapFile = new IniFile(file.FullName);
            if (mapFile.SectionExists("Units"))
            {
                var unitSection = mapFile.GetSection("Units");
                foreach (var unitString in unitSection.Keys)
                {
                    var unit = new Unit();
                    unit.Initialize(unitString.Value);
                    if (unit != null && unit.RelativeX < WorkingMap.MapUnitWidth && unit.RelativeX >= 0 && unit.RelativeY < WorkingMap.MapUnitHeight && unit.RelativeY >= 0)
                        UnitList.Add(unit);
                }
            }
            if (mapFile.SectionExists("Infantry"))
            {
                var infantrySection = mapFile.GetSection("Infantry");
                foreach (var infantryString in infantrySection.Keys)
                {
                    var infantry = new Infantry();
                    infantry.Initialize(infantryString.Value);
                    if (infantry != null && infantry.RelativeX < WorkingMap.MapUnitWidth && infantry.RelativeX >= 0 && infantry.RelativeY < WorkingMap.MapUnitHeight && infantry.RelativeY >= 0)
                        InfantryList.Add(infantry);
                }
            }
            if (mapFile.SectionExists("Structures"))
            {
                var structureSection = mapFile.GetSection("Structures");
                foreach (var structureString in structureSection.Keys)
                {
                    var structure = new Structure();
                    structure.Initialize(structureString.Value);
                    if (structure != null && structure.RelativeX < WorkingMap.MapUnitWidth && structure.RelativeX >= 0 && structure.RelativeY < WorkingMap.MapUnitHeight && structure.RelativeY >= 0)
                        StructureList.Add(structure);
                }
            }
            if (mapFile.SectionExists("Terrain"))
            {
                var terrainSection = mapFile.GetSection("Terrain");
                foreach (var terrainLine in terrainSection.Keys)
                {
                    var terrain = new Terrain();
                    terrain.Initialize(terrainLine);
                    //out of bounder is disabled here for placing lamps
                    if (terrain != null) //&& terrain.RelativeX < WorkingMap.MapUnitWidth && terrain.RelativeX >= 0 && terrain.RelativeY < WorkingMap.MapUnitHeight && terrain.RelativeY >= 0)
                        TerrainList.Add(terrain);
                }
            }
            if (mapFile.SectionExists("Aircraft"))
            {
                var aircraftSection = mapFile.GetSection("Aircraft");
                foreach (var aircraftString in aircraftSection.Keys)
                {
                    var aircraft = new Aircraft();
                    aircraft.Initialize(aircraftString.Value);
                    if (aircraft != null && aircraft.RelativeX < WorkingMap.MapUnitWidth && aircraft.RelativeX >= 0 && aircraft.RelativeY < WorkingMap.MapUnitHeight && aircraft.RelativeY >= 0)
                        AircraftList.Add(aircraft);
                }
            }
            if (mapFile.SectionExists("Smudge"))
            {
                var smudgeSection = mapFile.GetSection("Smudge");
                foreach (var smudgeString in smudgeSection.Keys)
                {
                    var smudge = new Smudge();
                    smudge.Initialize(smudgeString.Value);
                    if (smudge != null && smudge.RelativeX < WorkingMap.MapUnitWidth && smudge.RelativeX >= 0 && smudge.RelativeY < WorkingMap.MapUnitHeight && smudge.RelativeY >= 0)
                        SmudgeList.Add(smudge);
                }
            }
            if (mapFile.SectionExists("Waypoints"))
            {
                var waypointSection = mapFile.GetSection("Waypoints");
                foreach (var waypointLine in waypointSection.Keys)
                {
                    var waypoint = new Waypoint();
                    waypoint.Initialize(waypointLine);
                    if (waypoint != null && waypoint.RelativeX < WorkingMap.MapUnitWidth && waypoint.RelativeX >= 0 && waypoint.RelativeY < WorkingMap.MapUnitHeight && waypoint.RelativeY >= 0)
                        WaypointList.Add(waypoint);
                }
            }
        }
    }
}

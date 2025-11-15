namespace RandomMapGenerator
{
    public class 随机地图生成
    {
        public static string WorkingFolder;
        
        private const string MapUnitsFolder = "MapUnits\\";

        public static void RunOptions(Options option)
        {
            var mapFile = new MapFile();

            //var settings = new IniFile("settings.ini").GetSection("settings");
            //var workingFokderTemp = settings.GetStringValue("WorkingFolder", ".").EndsWith("\\") ? settings.GetStringValue("WorkingFolder", ".") : settings.GetStringValue("WorkingFolder", ".") + "\\";

            WorkingFolder = Path.Combine(Constants.ProgramFolder, MapUnitsFolder, option.Type) + "\\";

            if (!Directory.Exists(WorkingFolder))
            {
                throw new Exception("指定的地图类型文件夹不存在");
            }

            // option.输出目录 = settings.GetStringValue("option.输出目录", ".").EndsWith("\\") ? settings.GetStringValue("option.输出目录", ".") : settings.GetStringValue("option.输出目录", ".") + "\\";
            // ProgramFolder = Environment.CurrentDirectory;

            if (!Directory.Exists(option.输出目录)) Directory.CreateDirectory(option.输出目录);

            var 输出地图名 = "随机地图";
            var 输出扩展名 = "map";
            var 内部名称 = "内部名称";


            bool loop = true;
            int count = 0;

            var r = new Random();

            if (option.TotalRandom > 0 && option.Number > 0)
                return;

            while (loop)
            {

                string fullPath = "";
                string internalNameRandom = "";
                if (option.TotalRandom == 0 && option.Number == 0)
                {
                    if (!string.IsNullOrEmpty(option.Name))
                    {
                        输出地图名 = option.Name;
                        内部名称 = option.Name;
                    }

                    fullPath = option.输出目录 + 输出地图名 + "." + 输出扩展名;
                    Console.WriteLine("Generating random map " + 输出地图名 + "." + 输出扩展名 + " ...");
                }

                else
                {
                    fullPath = option.输出目录 + 输出地图名 + (count + 1).ToString() + "." + 输出扩展名;
                    Console.WriteLine("Generating random map " + 输出地图名 + (count + 1).ToString() + "." + 输出扩展名 + " ...");
                    internalNameRandom = 内部名称 + string.Format(" {0:D2}", count + 1);
                }


                //for total random
                int player = r.Next(1300);

                if (option.TotalRandom == 0)
                {
                    if (option.Width > 0 && option.Height > 0)
                        WorkingMap.Initialize(option.Width, option.Height, WorkingFolder);
                    else if (option.Width == 0 && option.Height > 0)
                        WorkingMap.Initialize(r.Next(130, 200), option.Height, WorkingFolder);
                    else if (option.Width > 0 && option.Height == 0)
                        WorkingMap.Initialize(option.Width, r.Next(130, 200), WorkingFolder);
                    else
                        WorkingMap.Initialize(r.Next(130, 200), r.Next(130, 200), WorkingFolder);
                }
                else
                {
                    if (player <= 200)
                        WorkingMap.Initialize(r.Next(90, 140), r.Next(90, 140), WorkingFolder);
                    else if (player <= 500)
                        WorkingMap.Initialize(r.Next(100, 170), r.Next(100, 160), WorkingFolder);
                    else if (player <= 1100)
                        WorkingMap.Initialize(r.Next(130, 200), r.Next(130, 200), WorkingFolder);
                    else
                        WorkingMap.Initialize(r.Next(70, 130), r.Next(70, 130), WorkingFolder);
                    //small map case
                }

                //Sterilize once
                if (count == 0)
                    WorkingMap.SterilizeMapUnit(WorkingFolder);

                int range = WorkingMap.Width + WorkingMap.Height;

                if (option.TotalRandom == 0)
                {
                    if (option.NE + option.NW + option.SE + option.SW + option.N + option.S + option.W + option.E > 8)
                    {
                        Console.WriteLine("Player number cannot exceed 8!");
                        return;
                    }
                    else
                    {
                        WorkingMap.PlacePlayerLocation(option.N, "N");
                        WorkingMap.PlacePlayerLocation(option.S, "S");
                        WorkingMap.PlacePlayerLocation(option.W, "W");
                        WorkingMap.PlacePlayerLocation(option.E, "E");
                        WorkingMap.PlacePlayerLocation(option.NE, "NE");
                        WorkingMap.PlacePlayerLocation(option.SE, "SE");
                        WorkingMap.PlacePlayerLocation(option.NW, "NW");
                        WorkingMap.PlacePlayerLocation(option.SW, "SW");
                    }
                }
                else
                {
                    if (player > 1200)
                    {
                        WorkingMap.PlacePlayerLocation(1, "N");
                        WorkingMap.PlacePlayerLocation(1, "SW");
                        WorkingMap.PlacePlayerLocation(1, "SE");
                    }
                    else if (player > 1150)
                    {
                        WorkingMap.PlacePlayerLocation(1, "NE");
                        WorkingMap.PlacePlayerLocation(1, "SW");
                    }
                    else if (player > 1100)
                    {
                        WorkingMap.PlacePlayerLocation(1, "NW");
                        WorkingMap.PlacePlayerLocation(1, "SE");
                    }
                    else if (player > 1000)
                    {
                        WorkingMap.PlacePlayerLocation(2, "NW");
                        WorkingMap.PlacePlayerLocation(2, "SW");
                        WorkingMap.PlacePlayerLocation(2, "NE");
                        WorkingMap.PlacePlayerLocation(2, "SE");
                    }
                    else if (player > 900)
                    {
                        WorkingMap.PlacePlayerLocation(2, "N");
                        WorkingMap.PlacePlayerLocation(2, "S");
                        WorkingMap.PlacePlayerLocation(2, "SE");
                        WorkingMap.PlacePlayerLocation(2, "NW");
                    }
                    else if (player > 800)
                    {
                        WorkingMap.PlacePlayerLocation(4, "NW");
                        WorkingMap.PlacePlayerLocation(4, "SE");
                    }
                    else if (player > 600)
                    {
                        WorkingMap.PlacePlayerLocation(1, "N");
                        WorkingMap.PlacePlayerLocation(1, "S");
                        WorkingMap.PlacePlayerLocation(1, "W");
                        WorkingMap.PlacePlayerLocation(1, "E");
                        WorkingMap.PlacePlayerLocation(1, "NE");
                        WorkingMap.PlacePlayerLocation(1, "SE");
                        WorkingMap.PlacePlayerLocation(1, "NW");
                        WorkingMap.PlacePlayerLocation(1, "SW");
                    }
                    else if (player > 500)
                    {
                        WorkingMap.PlacePlayerLocation(2, "N");
                        WorkingMap.PlacePlayerLocation(2, "S");
                        WorkingMap.PlacePlayerLocation(2, "W");
                        WorkingMap.PlacePlayerLocation(2, "E");

                    }
                    else if (player > 400)
                    {
                        WorkingMap.PlacePlayerLocation(3, "SW");
                        WorkingMap.PlacePlayerLocation(3, "NE");
                    }
                    else if (player > 300)
                    {
                        WorkingMap.PlacePlayerLocation(3, "N");
                        WorkingMap.PlacePlayerLocation(3, "S");
                    }
                    else if (player > 200)
                    {
                        WorkingMap.PlacePlayerLocation(3, "E");
                        WorkingMap.PlacePlayerLocation(3, "W");
                    }
                    else if (player > 100)
                    {
                        WorkingMap.PlacePlayerLocation(2, "NW");
                        WorkingMap.PlacePlayerLocation(2, "SE");
                    }
                    else
                    {
                        WorkingMap.PlacePlayerLocation(1, "NW");
                        WorkingMap.PlacePlayerLocation(1, "SW");
                        WorkingMap.PlacePlayerLocation(1, "NE");
                        WorkingMap.PlacePlayerLocation(1, "SE");
                    }
                }

                //WorkingMap.RandomPlaceMUInCenter(r.Next(20, 80));

                WorkingMap.SetMapUnitByOrder();
                WorkingMap.FillRemainingEmptyUnitMap();
                WorkingMap.PlaceMapUnitByAbsMapMatrix();

                if (option.DamangedBuilding)
                {
                    int damangeMin = r.Next(10, 90);
                    int damangeMax = r.Next(damangeMin + 10, 200);
                    int destroyP = (100 - damangeMin) / 10 - 2;
                    WorkingMap.ChangeStructureHealth(damangeMin, damangeMax, destroyP);
                    //WorkingMap.ChangeUnitAirInfHealth(damangeMin, damangeMax); //this should not be used
                    // because neutral units will go to neutral service depots
                }

                if (option.Smudge > 0)
                {
                    WorkingMap.RandomPlaceSmudge(option.Smudge); //not stable
                }
                WorkingMap.ReadyForMiniMap();

                mapFile.Width = WorkingMap.Width;
                mapFile.Height = WorkingMap.Height;
                mapFile.MapTheater = WorkingMap.MapTheater;
                mapFile.IsoTileList = WorkingMap.CreateTileList();
                mapFile.OverlayList = WorkingMap.OverlayList;
                mapFile.Unit = WorkingMap.CreateUnitINI();
                mapFile.Infantry = WorkingMap.CreateInfantryINI();
                mapFile.Structure = WorkingMap.CreateStructureINI();
                mapFile.Terrain = WorkingMap.CreateTerrainINI();
                mapFile.Aircraft = WorkingMap.CreateAircraftINI();
                mapFile.Smudge = WorkingMap.CreateSmudgeINI();
                mapFile.Waypoint = WorkingMap.CreateWaypointINI();

                mapFile.SaveFullMap(fullPath);

                //mapFile.CorrectPreviewSize(fullPath);
                mapFile.CalculateStartingWaypoints(fullPath);
                mapFile.RandomSetLighting(fullPath);
                mapFile.ChangeGamemode(fullPath, option.Gamemode);
                mapFile.AddAdditionalINI(fullPath, WorkingFolder + "addition.ini");

                if (option.TotalRandom == 0 && option.Number == 0)
                {
                    mapFile.ChangeName(fullPath, 内部名称);
                }
                else
                {
                    mapFile.ChangeName(fullPath, internalNameRandom);
                }

                /* 
                 if (option.NoThumbnailOutput)
                 {
                     mapFile.GeneratePreview(fullPath);
                     option.NoThumbnail = true;
                 }
                */

                //if (!option.NoThumbnail)
                //   mapFile.RenderMap(fullPath);



                mapFile.CreateBitMapbyMap(fullPath);

                mapFile.ChangeDigest(fullPath);

                mapFile.AddComment(fullPath);

                if (option.TotalRandom == 0 && option.Number == 0)
                    loop = false;
                else
                {
                    count++;
                    if ((option.TotalRandom != 0 && count > option.TotalRandom - 1) || (option.Number != 0 && count > option.Number - 1))
                        break;
                }
            }
            WorkingMap.CountMapUnitUsage();
        }

        //public static string WorkingFolder;
        //public static string OutputFolder;
        //public static string RenderderPath;
        //private readonly static string ProgramFolder = Environment.CurrentDirectory;
        //private const string MapUnitsFolder = "Resources\\RandomMapGenerator_RA2\\MapUnits\\";
        //public static void RunOptions(Options option)
        //{
        //    var mapFile = new MapFile();

        //    var settings = new IniFile("settings.ini").GetSection("settings");
        //    //var workingFokderTemp = settings.GetStringValue("WorkingFolder", ".").EndsWith("\\") ? settings.GetStringValue("WorkingFolder", ".") : settings.GetStringValue("WorkingFolder", ".") + "\\";
        //    WorkingFolder = Path.Combine(ProgramFolder, MapUnitsFolder, option.Type) + "\\";
        //    Path.Combine(ProgramFolder, MapUnitsFolder, option.Type);
        //    Console.WriteLine(WorkingFolder);
        //    if (!Directory.Exists(WorkingFolder))
        //    {
        //        Console.WriteLine("指定的地图类型文件夹不存在！");
        //        Console.ReadKey();
        //        return;
        //    }

        //    OutputFolder = settings.GetStringValue("OutputFolder", ".").EndsWith("\\") ? settings.GetStringValue("OutputFolder", ".") : settings.GetStringValue("OutputFolder", ".") + "\\";
        //    //ProgramFolder = Environment.CurrentDirectory;

        //    if (!Directory.Exists(OutputFolder))
        //    {
        //        Directory.CreateDirectory(OutputFolder);
        //    }

        //        var 输出地图名 = "随机地图";
        //        var 输出扩展名 = "map";
        //        var 内部名称 = "内部名称";

        //    bool loop = true;
        //    int count = 0;

        //    var r = new Random();

        //    if (option.TotalRandom > 0 && option.Number > 0)
        //        return;

        //    while (loop)
        //    {

        //        string fullPath = "";
        //        string internalNameRandom = "";
        //        if (option.TotalRandom == 0 && option.Number == 0)
        //        {
        //            if (option.Name != "")
        //            {
        //                输出地图名 = option.Name;
        //                内部名称 = option.Name;
        //            }

        //            fullPath = OutputFolder + 输出地图名 + "." + 输出扩展名;
        //            Console.WriteLine("Generating random map " + 输出地图名 + "." + 输出扩展名 + " ...");
        //        }

        //        else
        //        {
        //            fullPath = OutputFolder + 输出地图名 + (count + 1).ToString() + "." + 输出扩展名;
        //            Console.WriteLine("Generating random map " + 输出地图名 + (count + 1).ToString() + "." + 输出扩展名 + " ...");
        //            internalNameRandom = 内部名称 + string.Format(" {0:D2}", count + 1);
        //        }


        //        //for total random
        //        int player = r.Next(1300);

        //        if (option.TotalRandom == 0)
        //        {
        //            if (option.Width > 0 && option.Height > 0)
        //                WorkingMap.Initialize(option.Width, option.Height, WorkingFolder);
        //            else if (option.Width == 0 && option.Height > 0)
        //                WorkingMap.Initialize(r.Next(130, 200), option.Height, WorkingFolder);
        //            else if (option.Width > 0 && option.Height == 0)
        //                WorkingMap.Initialize(option.Width, r.Next(130, 200), WorkingFolder);
        //            else
        //                WorkingMap.Initialize(r.Next(130, 200), r.Next(130, 200), WorkingFolder);
        //        }
        //        else
        //        {
        //            if (player <= 200)
        //                WorkingMap.Initialize(r.Next(90, 140), r.Next(90, 140), WorkingFolder);
        //            else if (player <= 500)
        //                WorkingMap.Initialize(r.Next(100, 170), r.Next(100, 160), WorkingFolder);
        //            else if (player <= 1100)
        //                WorkingMap.Initialize(r.Next(130, 200), r.Next(130, 200), WorkingFolder);
        //            else
        //                WorkingMap.Initialize(r.Next(70, 130), r.Next(70, 130), WorkingFolder);
        //            //small map case
        //        }

        //        //Sterilize once
        //        if (count == 0)
        //            WorkingMap.SterilizeMapUnit(WorkingFolder);

        //        int range = WorkingMap.Width + WorkingMap.Height;

        //        if (option.TotalRandom == 0)
        //        {
        //            if (option.NE + option.NW + option.SE + option.SW + option.N + option.S + option.W + option.E > 8)
        //            {

        //                return;
        //            }
        //            else
        //            {
        //                WorkingMap.PlacePlayerLocation(option.N, "N");
        //                WorkingMap.PlacePlayerLocation(option.S, "S");
        //                WorkingMap.PlacePlayerLocation(option.W, "W");
        //                WorkingMap.PlacePlayerLocation(option.E, "E");
        //                WorkingMap.PlacePlayerLocation(option.NE, "NE");
        //                WorkingMap.PlacePlayerLocation(option.SE, "SE");
        //                WorkingMap.PlacePlayerLocation(option.NW, "NW");
        //                WorkingMap.PlacePlayerLocation(option.SW, "SW");
        //            }
        //        }
        //        else
        //        {
        //            if (player > 1200)
        //            {
        //                WorkingMap.PlacePlayerLocation(1, "N");
        //                WorkingMap.PlacePlayerLocation(1, "SW");
        //                WorkingMap.PlacePlayerLocation(1, "SE");
        //            }
        //            else if (player > 1150)
        //            {
        //                WorkingMap.PlacePlayerLocation(1, "NE");
        //                WorkingMap.PlacePlayerLocation(1, "SW");
        //            }
        //            else if (player > 1100)
        //            {
        //                WorkingMap.PlacePlayerLocation(1, "NW");
        //                WorkingMap.PlacePlayerLocation(1, "SE");
        //            }
        //            else if (player > 1000)
        //            {
        //                WorkingMap.PlacePlayerLocation(2, "NW");
        //                WorkingMap.PlacePlayerLocation(2, "SW");
        //                WorkingMap.PlacePlayerLocation(2, "NE");
        //                WorkingMap.PlacePlayerLocation(2, "SE");
        //            }
        //            else if (player > 900)
        //            {
        //                WorkingMap.PlacePlayerLocation(2, "N");
        //                WorkingMap.PlacePlayerLocation(2, "S");
        //                WorkingMap.PlacePlayerLocation(2, "SE");
        //                WorkingMap.PlacePlayerLocation(2, "NW");
        //            }
        //            else if (player > 800)
        //            {
        //                WorkingMap.PlacePlayerLocation(4, "NW");
        //                WorkingMap.PlacePlayerLocation(4, "SE");
        //            }
        //            else if (player > 600)
        //            {
        //                WorkingMap.PlacePlayerLocation(1, "N");
        //                WorkingMap.PlacePlayerLocation(1, "S");
        //                WorkingMap.PlacePlayerLocation(1, "W");
        //                WorkingMap.PlacePlayerLocation(1, "E");
        //                WorkingMap.PlacePlayerLocation(1, "NE");
        //                WorkingMap.PlacePlayerLocation(1, "SE");
        //                WorkingMap.PlacePlayerLocation(1, "NW");
        //                WorkingMap.PlacePlayerLocation(1, "SW");
        //            }
        //            else if (player > 500)
        //            {
        //                WorkingMap.PlacePlayerLocation(2, "N");
        //                WorkingMap.PlacePlayerLocation(2, "S");
        //                WorkingMap.PlacePlayerLocation(2, "W");
        //                WorkingMap.PlacePlayerLocation(2, "E");

        //            }
        //            else if (player > 400)
        //            {
        //                WorkingMap.PlacePlayerLocation(3, "SW");
        //                WorkingMap.PlacePlayerLocation(3, "NE");
        //            }
        //            else if (player > 300)
        //            {
        //                WorkingMap.PlacePlayerLocation(3, "N");
        //                WorkingMap.PlacePlayerLocation(3, "S");
        //            }
        //            else if (player > 200)
        //            {
        //                WorkingMap.PlacePlayerLocation(3, "E");
        //                WorkingMap.PlacePlayerLocation(3, "W");
        //            }
        //            else if (player > 100)
        //            {
        //                WorkingMap.PlacePlayerLocation(2, "NW");
        //                WorkingMap.PlacePlayerLocation(2, "SE");
        //            }
        //            else
        //            {
        //                WorkingMap.PlacePlayerLocation(1, "NW");
        //                WorkingMap.PlacePlayerLocation(1, "SW");
        //                WorkingMap.PlacePlayerLocation(1, "NE");
        //                WorkingMap.PlacePlayerLocation(1, "SE");
        //            }
        //        }

        //        //WorkingMap.RandomPlaceMUInCenter(r.Next(20, 80));

        //        WorkingMap.SetMapUnitByOrder();
        //        WorkingMap.FillRemainingEmptyUnitMap();
        //        WorkingMap.PlaceMapUnitByAbsMapMatrix();

        //        if (option.DamangedBuilding)
        //        {
        //            int damangeMin = r.Next(10, 90);
        //            int damangeMax = r.Next(damangeMin + 10, 200);
        //            int destroyP = (100 - damangeMin) / 10 - 2;
        //            WorkingMap.ChangeStructureHealth(damangeMin, damangeMax, destroyP);
        //            //WorkingMap.ChangeUnitAirInfHealth(damangeMin, damangeMax); //this should not be used
        //            // because neutral units will go to neutral service depots
        //        }

        //        if (option.Smudge > 0)
        //        {
        //            WorkingMap.RandomPlaceSmudge(option.Smudge); //not stable
        //        }
        //        WorkingMap.ReadyForMiniMap();

        //        mapFile.Width = WorkingMap.Width;
        //        mapFile.Height = WorkingMap.Height;
        //        mapFile.MapTheater = WorkingMap.MapTheater;
        //        mapFile.IsoTileList = WorkingMap.CreateTileList();
        //        mapFile.OverlayList = WorkingMap.OverlayList;
        //        mapFile.Unit = WorkingMap.CreateUnitINI();
        //        mapFile.Infantry = WorkingMap.CreateInfantryINI();
        //        mapFile.Structure = WorkingMap.CreateStructureINI();
        //        mapFile.Terrain = WorkingMap.CreateTerrainINI();
        //        mapFile.Aircraft = WorkingMap.CreateAircraftINI();
        //        mapFile.Smudge = WorkingMap.CreateSmudgeINI();
        //        mapFile.Waypoint = WorkingMap.CreateWaypointINI();

        //        mapFile.SaveFullMap(fullPath);

        //        //mapFile.CorrectPreviewSize(fullPath);
        //        mapFile.CalculateStartingWaypoints(fullPath);
        //        mapFile.RandomSetLighting(fullPath);
        //        mapFile.ChangeGamemode(fullPath, option.Gamemode);
        //        mapFile.AddAdditionalINI(fullPath, WorkingFolder + "addition.ini");

        //        if (option.TotalRandom == 0 && option.Number == 0)
        //        {
        //            mapFile.ChangeName(fullPath, 内部名称);
        //        }
        //        else
        //        {
        //            mapFile.ChangeName(fullPath, internalNameRandom);
        //        }

        //        /* if (option.NoThumbnailOutput)
        //         {
        //             mapFile.GeneratePreview(fullPath);
        //             option.NoThumbnail = true;
        //         }*/

        //        //if (!option.NoThumbnail)
        //        //    mapFile.RenderMap(fullPath);



        //        mapFile.CreateBitMapbyMap(fullPath);

        //        mapFile.ChangeDigest(fullPath);

        //        mapFile.AddComment(fullPath);

        //        if (option.TotalRandom == 0 && option.Number == 0)
        //            loop = false;
        //        else
        //        {
        //            count++;
        //            if ((option.TotalRandom != 0 && count > option.TotalRandom - 1) || (option.Number != 0 && count > option.Number - 1))
        //                break;
        //        }
        //    }
        //    WorkingMap.CountMapUnitUsage();
        //}
    }
}

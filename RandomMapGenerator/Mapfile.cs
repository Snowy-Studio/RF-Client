using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using Serilog;
using Rampastring.Tools;
using RandomMapGenerator.TileInfo;

namespace RandomMapGenerator
{
    class MapFile
    {
        public int Width;
        public int Height;
        public int MapTheater;
        public List<IsoTile> IsoTileList;
        public List<Overlay> OverlayList;
        public IniSection Unit = new IniSection("Units");
        public IniSection Infantry = new IniSection("Infantry");
        public IniSection Structure = new IniSection("Structures");
        public IniSection Terrain = new IniSection("Terrain");
        public IniSection Aircraft = new IniSection("Aircraft");
        public IniSection Smudge = new IniSection("Smudge");
        public IniSection Waypoint = new IniSection("Waypoints");
        public int[] LocalSize = new int[2];
        public void CreateIsoTileList(string filePath)
        {
            var MapFile = new IniFile(filePath);
            var MapPackSections = MapFile.GetSection(Constants.MapPackName);
            var MapSize = MapFile.GetStringValue("Map", "Size", "0,0,0,0");
            string IsoMapPack5String = "";

            int sectionIndex = 1;
            while (MapPackSections.KeyExists(sectionIndex.ToString()))
            {
                IsoMapPack5String += MapPackSections.GetStringValue(sectionIndex.ToString(), "");
                sectionIndex++;
            }

            string[] sArray = MapSize.Split(',');
            Width = Int32.Parse(sArray[2]);
            Height = Int32.Parse(sArray[3]);
            int cells = (Width * 2 - 1) * Height;
            IsoTile[,] Tiles = new IsoTile[Width * 2 - 1, Height];//这里值得注意
            byte[] lzoData = Convert.FromBase64String(IsoMapPack5String);

            //Log.Information(cells);
            int lzoPackSize = cells * 11 + 4;
            var isoMapPack = new byte[lzoPackSize];
            uint totalDecompressSize = Format5.DecodeInto(lzoData, isoMapPack);//TODO 源，目标 输入应该是解码后长度，isoMapPack被赋值解码值了
                                                                               //uint	0 to 4,294,967,295	Unsigned 32-bit integer	System.UInt32
            var mf = new MemoryFile(isoMapPack);

            //Log.Information(BitConverter.ToString(lzoData));
            int count = 0;
            //List<List<IsoTile>> TilesList = new List<List<IsoTile>>(Width * 2 - 1);
            IsoTileList = new List<IsoTile>();
            //Log.Information(TilesList.Capacity);
            for (int i = 0; i < cells; i++)
            {
                ushort rx = mf.ReadUInt16();//ushort	0 to 65,535	Unsigned 16-bit integer	System.UInt16
                ushort ry = mf.ReadUInt16();
                short tilenum = mf.ReadInt16();//short	-32,768 to 32,767	Signed 16-bit integer	System.Int16
                short zero1 = mf.ReadInt16();//Reads a 2-byte signed integer from the current stream and advances the current position of the stream by two bytes.
                byte subtile = mf.ReadByte();//Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.
                byte z = mf.ReadByte();
                byte zero2 = mf.ReadByte();

                count++;
                int dx = rx - ry + Width - 1;

                int dy = rx + ry - Width - 1;
                //Log.Information("{1}", rx, ry, tilenum, subtile, z, dx, dy,count);
                //上面是一个线性变换 旋转45度、拉长、平移
                if (dx >= 0 && dx < 2 * Width &&
                    dy >= 0 && dy < 2 * Height)
                {
                    var tile = new IsoTile((ushort)dx, (ushort)dy, rx, ry, z, tilenum, subtile);//IsoTile定义是NumberedMapObject

                    Tiles[(ushort)dx, (ushort)dy / 2] = tile;//给瓷砖赋值
                    IsoTileList.Add(tile);
                }
            }
            //用来检查有没有空着的
            for (ushort y = 0; y < Height; y++)
            {
                for (ushort x = 0; x < Width * 2 - 1; x++)
                {
                    var isoTile = Tiles[x, y];//从这儿来看，isoTile指的是一块瓷砖，Tile是一个二维数组，存着所有瓷砖
                                              //isoTile的定义在TileLayer.cs里
                    if (isoTile == null)
                    {
                        // fix null tiles to blank
                        ushort dx = (ushort)(x);
                        ushort dy = (ushort)(y * 2 + x % 2);
                        ushort rx = (ushort)((dx + dy) / 2 + 1);
                        ushort ry = (ushort)(dy - rx + Width + 1);
                        Tiles[x, y] = new IsoTile(dx, dy, rx, ry, 0, 0, 0);
                    }
                }

            }
        }

        public void SaveIsoMapPack5(string path)
        {
            long di = 0;
            int cells = (Width * 2 - 1) * Height;
            int lzoPackSize = cells * 11 + 4;
            var isoMapPack2 = new byte[lzoPackSize];
            foreach (var tile in IsoTileList)
            {
                var bs = tile.ToMapPack5Entry().ToArray();//ToMapPack5Entry的定义在MapObjects.cs
                                                          //ToArray将ArrayList转换为Array：
                Array.Copy(bs, 0, isoMapPack2, di, 11);//把bs复制给isoMapPack,从di索引开始复制11个字节
                di += 11;//一次循环复制11个字节
            }

            var compressed = Format5.Encode(isoMapPack2, 5);

            string compressed64 = Convert.ToBase64String(compressed);
            int j = 1;
            int idx = 0;

            var saveFile = new IniFile(path);
            saveFile.AddSection(Constants.MapPackName);
            var saveMapPackSection = saveFile.GetSection(Constants.MapPackName);

            while (idx < compressed64.Length)
            {
                int adv = Math.Min(74, compressed64.Length - idx);//74 is the length of each line
                saveMapPackSection.SetStringValue(j.ToString(), compressed64.Substring(idx, adv));
                j++;
                idx += adv;//idx=adv+1
            }
            saveFile.WriteIniFile();
        }
        public void SaveWorkingMapPack(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            var mapPack = new IniFile(path);
            mapPack.AddSection("mapPack");
            var mapPackSection = mapPack.GetSection("mapPack");
            int mapPackIndex = 1;
            mapPackSection.SetStringValue("0", "Dx,Dy,Rx,Ry,Z,TileNum,SubTile");
            mapPack.SetStringValue("Map", "Size", Width.ToString() + "," + Height.ToString());

            for (int i = 0; i < IsoTileList.Count; i++)
            {
                var isoTile = IsoTileList[i];
                mapPackSection.SetStringValue(mapPackIndex++.ToString(),
                       isoTile.Dx.ToString() + "," +
                       isoTile.Dy.ToString() + "," +
                       isoTile.Rx.ToString() + "," +
                       isoTile.Ry.ToString() + "," +
                       isoTile.Z.ToString() + "," +
                       isoTile.TileNum.ToString() + "," +
                       isoTile.SubTile.ToString());
            }
            mapPack.WriteIniFile();
        }

        public void CreateEmptyMap(int width, int height)
        {
            Width = width;
            Height = height;
            IsoTileList = new List<IsoTile>();

            int cells = (Width * 2 - 1) * Height;
            IsoTile[,] Tiles = new IsoTile[Width * 2 - 1, Height];
            for (ushort y = 0; y < Height; y++)
            {
                for (ushort x = 0; x < Width * 2 - 1; x++)
                {
                    ushort dx = (ushort)(x);
                    ushort dy = (ushort)(y * 2 + x % 2);
                    ushort rx = (ushort)((dx + dy) / 2 + 1);
                    ushort ry = (ushort)(dy - rx + Width + 1);
                    Tiles[x, y] = new IsoTile(dx, dy, rx, ry, 0, (int)Common._000_Empty, 0);
                    IsoTileList.Add(Tiles[x, y]);
                }
            }
        }

        public void CreateMapbyBitMap(string filename)
        {
            var srcBitmap = (Bitmap)Bitmap.FromFile(filename, false);
            Width = srcBitmap.Width;
            Height = srcBitmap.Height;
            IsoTileList = new List<IsoTile>();

            int cells = (Width * 2 - 1) * Height;
            IsoTile[,] Tiles = new IsoTile[Width * 2 - 1, Height];
            for (ushort y = 0; y < Height; y++)
            {
                for (ushort x = 0; x < Width * 2 - 1; x++)
                {
                    ushort dx = (ushort)(x);
                    ushort dy = (ushort)(y * 2 + x % 2);
                    ushort rx = (ushort)((dx + dy) / 2 + 1);
                    ushort ry = (ushort)(dy - rx + Width + 1);

                    int bmpX = (int)Math.Floor((decimal)(x / 2));
                    int bmpY = y;
                    var color = srcBitmap.GetPixel(bmpX, bmpY);
                    int drawTile;
                    if (color.B > color.R)
                    {
                        if (color.B > color.G)
                        {
                            Random random = new Random(x * y);
                            drawTile = random.Next(322, 326);//sea
                        }
                        else
                        {
                            drawTile = (int)Common._000_Empty;//grass
                        }
                    }
                    else
                    {
                        if (color.R > color.G)
                        {
                            drawTile = 493;//sand
                        }
                        else
                        {
                            drawTile = (int)Common._000_Empty;//grass
                        }
                    }
                    Tiles[x, y] = new IsoTile(dx, dy, rx, ry, 0, (short)drawTile, 0);
                    IsoTileList.Add(Tiles[x, y]);
                }
            }
        }

        public void CreateBitMapbyMap(string filename)
        {
            var srcBitmap = new Bitmap(Width * 2, Height);

            foreach (var tile in IsoTileList)
            {
                int x = tile.Dx ;
                int y = (tile.Dy - tile.Dx % 2) / 2;
                if (x < srcBitmap.Width && y < srcBitmap.Height)
                    srcBitmap.SetPixel(x, y, tile.RadarLeft);
            }
            for (int i = 0; i < srcBitmap.Height; i++)
            {
                var color = srcBitmap.GetPixel(srcBitmap.Width - 2, i);
                srcBitmap.SetPixel(srcBitmap.Width - 1, i, color);
            }

            var outputBitmap = new Bitmap(LocalSize[0] * 2, LocalSize[1]);
            for ( int i = 2; i < 2 + outputBitmap.Width; i++ )
            {
                for (int j = 5; j < 5 + outputBitmap.Height; j++)
                {
                    outputBitmap.SetPixel(i - 2, j - 5, srcBitmap.GetPixel(i, j+1));
                }
            }
            InjectThumb(outputBitmap, filename);
            //outputBitmap.Save(filename + ".bmp");
        }

        public static unsafe void InjectThumb(Bitmap preview, string path)
        {
            BitmapData bmd = preview.LockBits(new Rectangle(0, 0, preview.Width, preview.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            byte[] image = new byte[preview.Width * preview.Height * 3];
            int idx = 0;

            // invert rgb->bgr
            for (int y = 0; y < bmd.Height; y++)
            {
                byte* p = (byte*)bmd.Scan0 + bmd.Stride * y;
                for (int x = 0; x < bmd.Width; x++)
                {
                    byte r = *p++;
                    byte g = *p++;
                    byte b = *p++;

                    image[idx++] = b;
                    image[idx++] = g;
                    image[idx++] = r;
                }
            }

            // encode
            byte[] image_compressed = Format5.Encode(image, 5);

            // base64 encode
            string image_base64 = Convert.ToBase64String(image_compressed, Base64FormattingOptions.None);

            var map = new IniFile(path);
            // now overwrite [Preview] and [PreviewPack], inserting them directly after [Basic] if not yet existing
            map.SetStringValue("Preview","Size", string.Format("0,0,{0},{1}", preview.Width, preview.Height));

            if (map.SectionExists("PreviewPack"))
                map.RemoveSection("PreviewPack");

            map.AddSection("PreviewPack");
            var section = map.GetSection("PreviewPack");

            int rowNum = 1;
            for (int i = 0; i < image_base64.Length; i += 70)
            {
                section.SetStringValue(rowNum++.ToString(CultureInfo.InvariantCulture), image_base64.Substring(i, Math.Min(70, image_base64.Length - i)));
            }
            map.MoveSectionToFirst("PreviewPack");
            map.MoveSectionToFirst("Preview");
            map.MoveSectionToFirst("Header");
            map.WriteIniFile();
        }


        public void ChangeDigest(string path)
        {
            var map = new IniFile(path);

            if (map.SectionExists("Digest"))
                map.RemoveSection("Digest");

            map.AddSection("Digest");
            var section = map.GetSection("Digest");

            var r = new Random();
            byte[] digest = new byte[20];
            for (int i = 0; i < 20; i++)
            {
                byte b = Convert.ToByte(r.Next(16));
                digest[i] = b;
            }

            section.SetStringValue("1", Convert.ToBase64String(digest));

            map.WriteIniFile();
        }

       // private const string TemplateMap = "templateMap.map";

        public void SaveFullMap(string path)
        {
            var fullMap = new IniFile(Constants.TemplateMapPath);
            //var settings = new IniFile(随机地图生成.WorkingFolder + "settings.ini");
            //var bottomSpace = settings.GetIntValue("settings", "BottomSpace", 4);
            fullMap.SetStringValue("Map", "Size", "0,0," + Width.ToString() + "," + Height.ToString());
            LocalSize[0] = Width - 4;
            LocalSize[1] = Height - 11;
            fullMap.SetStringValue("Map", "LocalSize", "2,5," + LocalSize[0].ToString() + "," + LocalSize[1].ToString());
            fullMap.SetStringValue("Map", "Theater", Enum.GetName(typeof(Theater), MapTheater));
            if (Unit != null)
                fullMap.AddSection(Unit);
            if (Infantry != null)
                fullMap.AddSection(Infantry);
            if (Structure != null)
                fullMap.AddSection(Structure);
            if (Terrain != null)
                fullMap.AddSection(Terrain);
            if (Aircraft != null)
                fullMap.AddSection(Aircraft);
            if (Smudge != null)
                fullMap.AddSection(Smudge);
            if (Waypoint != null)
                fullMap.AddSection(Waypoint);
            fullMap.WriteIniFile(path);
            SaveIsoMapPack5(path);
            SaveOverlay(path);
            Log.Information("******************************************************");
            Log.Information("Successfully create random map:");
            Log.Information(path);
            Log.Information("******************************************************");
        }
        public void LoadWorkingMapPack(string path)
        {
            IsoTileList = new List<IsoTile>();
            var mapPack = new IniFile(path);
            var mapPackSection = mapPack.GetSection("mapPack");
            string[] size = mapPack.GetStringValue("Map", "Size", "0,0").Split(',');
            Width = int.Parse(size[0]);
            Height = int.Parse(size[1]);

            int i = 1;
            while (mapPackSection.KeyExists(i.ToString()))
            {
                if (mapPackSection.KeyExists(i.ToString()))
                {
                    string[] isoTileInfo = mapPackSection.GetStringValue(i.ToString(), "").Split(',');
                    var isoTile = new IsoTile(ushort.Parse(isoTileInfo[0]),
                        ushort.Parse(isoTileInfo[1]),
                        ushort.Parse(isoTileInfo[2]),
                        ushort.Parse(isoTileInfo[3]),
                        (byte)int.Parse(isoTileInfo[4]),
                        short.Parse(isoTileInfo[5]),
                        (byte)int.Parse(isoTileInfo[6]));
                    IsoTileList.Add(isoTile);
                    i++;
                }
            }
        }

        public List<Overlay> ReadOverlay(string path)
        {
            OverlayList = new List<Overlay>();
            var mapFile = new IniFile(path);
            if (!mapFile.SectionExists("OverlayPack") || !mapFile.SectionExists("OverlayDataPack"))
                return null;
            IniSection overlaySection = mapFile.GetSection("OverlayPack");
            if (overlaySection == null)
                return null;

            string OverlayPackString = "";
            int sectionIndex = 1;
            while (overlaySection.KeyExists(sectionIndex.ToString()))
            {
                OverlayPackString += overlaySection.GetStringValue(sectionIndex.ToString(), "");
                sectionIndex++;
            }

            byte[] format80Data = Convert.FromBase64String(OverlayPackString);
            var overlayPack = new byte[1 << 18];
            Format5.DecodeInto(format80Data, overlayPack, 80);

            IniSection overlayDataSection = mapFile.GetSection("OverlayDataPack");
            if (overlayDataSection == null)
                return null;

            string OverlayDataPackString = "";
            sectionIndex = 1;
            while (overlayDataSection.KeyExists(sectionIndex.ToString()))
            {
                OverlayDataPackString += overlayDataSection.GetStringValue(sectionIndex.ToString(), "");
                sectionIndex++;
            }

            format80Data = Convert.FromBase64String(OverlayDataPackString);
            var overlayDataPack = new byte[1 << 18];
            Format5.DecodeInto(format80Data, overlayDataPack, 80);

            foreach (var tile in IsoTileList)
            {
                if (tile == null) continue;
                int idx = tile.Rx + 512 * tile.Ry;
                byte overlay_id = overlayPack[idx];

                if (overlay_id != 0xff)
                {
                    byte overlay_value = overlayDataPack[idx];
                    var ovl = new Overlay(overlay_id, overlay_value);
                    ovl.Tile = tile.Clone();
                    OverlayList.Add(ovl);
                }
            }

            return OverlayList;
        }
        public void AddComment(string path)
        {
            var saveFile = new IniFile(path);
            saveFile.Comment = "This map is created by HFX's Random map generator.\n; Visit https://github.com/handama/RandomMapGenerator_RA2 to get the latest version.";
            saveFile.WriteIniFile();
        }

        public void ChangeGamemode(string path, string modes)
        {
            if (modes == null || modes == "")
                return;
            var saveFile = new IniFile(path);
            saveFile.SetStringValue("Basic", "GameMode", modes);
            saveFile.WriteIniFile();
        }

        public void AddAdditionalINI(string path, string iniPath)
        {
            if (!File.Exists(iniPath))
                return;

            var saveFile = new IniFile(path);
            var additional = new IniFile(iniPath);
            var additionalSections = additional.GetSections();
            foreach(var section in additionalSections)
            {
                var newSection = additional.GetSection(section);
                saveFile.AddSection(newSection);
            }
            saveFile.WriteIniFile();
        }

        public void SaveOverlay(string path)
        {

            var overlayPack = new byte[1 << 18];
            for (int i = 0; i < overlayPack.Length; i++)
            {
                overlayPack[i] = 0xff;
            }
            var overlayDataPack = new byte[1 << 18];
            foreach (var overlay in OverlayList)
            {
                int index = overlay.Tile.Rx + 512 * overlay.Tile.Ry;
                overlayPack[index] = overlay.OverlayID;
                overlayDataPack[index] = overlay.OverlayValue;

            }

            var compressedPack = Format5.Encode(overlayPack, 80);
            var compressedDataPack = Format5.Encode(overlayDataPack, 80);

            string compressedPack64 = Convert.ToBase64String(compressedPack);
            string compressedDataPack64 = Convert.ToBase64String(compressedDataPack);
            int j = 1;
            int idx = 0;

            int j2 = 1;
            int idx2 = 0;

            var saveFile = new IniFile(path);
            if (saveFile.SectionExists("OverlayPack"))
                saveFile.RemoveSection("OverlayPack");

            saveFile.AddSection("OverlayPack");
            if (saveFile.SectionExists("OverlayDataPack"))
                saveFile.RemoveSection("OverlayDataPack");

            saveFile.AddSection("OverlayDataPack");

            var OverlayPackSection = saveFile.GetSection("OverlayPack");
            var OverlayDataPackSection = saveFile.GetSection("OverlayDataPack");

            while (idx < compressedPack64.Length)
            {
                int adv = Math.Min(70, compressedPack64.Length - idx);//70 is the length of each line
                OverlayPackSection.SetStringValue(j.ToString(), compressedPack64.Substring(idx, adv));
                j++;
                idx += adv;//idx=adv+1
            }
            while (idx2 < compressedDataPack64.Length)
            {
                int adv = Math.Min(70, compressedDataPack64.Length - idx2);//70 is the length of each line
                OverlayDataPackSection.SetStringValue(j2.ToString(), compressedDataPack64.Substring(idx2, adv));
                j2++;
                idx2 += adv;//idx=adv+1
            }
            saveFile.WriteIniFile();
        }

        public void SaveWorkingOverlay(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            var overlayPack = new IniFile(path);
            overlayPack.AddSection("overlayPack");
            var mapPackSection = overlayPack.GetSection("overlayPack");
            int mapPackIndex = 1;
            //mapPackSection.SetStringValue("0", "Dx,Dy,Rx,Ry,Z,TileNum,SubTile");

            for (int i = 0; i < OverlayList.Count; i++)
            {
                var overlay = OverlayList[i];
                mapPackSection.SetStringValue(mapPackIndex++.ToString(),
                        overlay.OverlayID.ToString() + "," +
                        overlay.OverlayValue.ToString());

            }
            overlayPack.WriteIniFile();
        }

        public void CalculateStartingWaypoints(string filePath)
        {
            var MapFile = new IniFile(filePath);

            var LocalSize = MapFile.GetStringValue("Map", "LocalSize", "0,0,0,0");
            var LocalWidth = int.Parse(LocalSize.Split(',')[2]);
            var LocalHeight = int.Parse(LocalSize.Split(',')[3]);
            MapFile.SetStringValue("Header", "Width", (LocalWidth - 1).ToString());
            MapFile.SetStringValue("Header", "Height", LocalHeight.ToString());

            int playerNum = 0;
            while (MapFile.KeyExists("Waypoints", playerNum.ToString()) && playerNum < 8)
            {
                var waypoint = MapFile.GetStringValue("Waypoints", playerNum.ToString(), "000000");
                int length = waypoint.Length;
                int x = int.Parse(waypoint.Substring(length - 3, 3));
                int y = int.Parse(waypoint.Substring(0, length - 3));
                float former = (x - y - 1 + Width)/2;
                float later = y + former - Width;
                var wpstring = ((int)(256 - Width / 2 + former)).ToString() + "," + ((int)(Width / 2 + later)).ToString();
                MapFile.SetStringValue("Header", "Waypoint" + (playerNum + 1).ToString(), wpstring);

                playerNum++;
            }
            MapFile.SetStringValue("Header", "NumberStartingPoints", playerNum.ToString());
            MapFile.SetStringValue("Basic", "MaxPlayer", playerNum.ToString());
            if (playerNum == 1)
                MapFile.SetStringValue("Basic", "MinPlayer", "1");
            else if (playerNum == 0)
                MapFile.SetStringValue("Basic", "MinPlayer", "0");
            MapFile.WriteIniFile();
        }
        public void CorrectPreviewSize(string filePath)
        {
            var MapFile = new IniFile(filePath);
            var LocalSize = MapFile.GetStringValue("Map", "LocalSize", "0,0,0,0");
            var LocalWidth = int.Parse(LocalSize.Split(',')[2]);
            var LocalHeight = int.Parse(LocalSize.Split(',')[3]);
            MapFile.SetStringValue("Preview", "Size", "0,0," + (int)(LocalWidth * 1.975) + "," + LocalHeight);
            MapFile.WriteIniFile();
        }

        public void ChangeName(string filePath, string name)
        {
            var MapFile = new IniFile(filePath);
            MapFile.SetStringValue("Basic", "Name", name);
            MapFile.WriteIniFile();
        }

        public void RandomSetLighting(string filePath)
        {
            var MapFile = new IniFile(filePath);
            var settings = new IniFile(随机地图生成.WorkingFolder + "settings.ini");

            var Red = settings.GetStringValue("settings", "Red", "9000,10600");
            var Red1 = int.Parse(Red.Split(',')[0]);
            var Red2 = int.Parse(Red.Split(',')[1]);

            var Green = settings.GetStringValue("settings", "Green", "8900,10100");
            var Green1 = int.Parse(Green.Split(',')[0]);
            var Green2 = int.Parse(Green.Split(',')[1]);

            var Blue = settings.GetStringValue("settings", "Blue", "9000,10600");
            var Blue1 = int.Parse(Blue.Split(',')[0]);
            var Blue2 = int.Parse(Blue.Split(',')[1]);

            var Level = settings.GetStringValue("settings", "Level", "120,320");
            var Level1 = int.Parse(Level.Split(',')[0]);
            var Level2 = int.Parse(Level.Split(',')[1]);

            var Ambient = settings.GetStringValue("settings", "Ambient", "8500,10600");
            var Ambient1 = int.Parse(Ambient.Split(',')[0]);
            var Ambient2 = int.Parse(Ambient.Split(',')[1]);

            var IonRed = settings.GetStringValue("settings", "IonRed", "7000,8500");
            var IonRed1 = int.Parse(IonRed.Split(',')[0]);
            var IonRed2 = int.Parse(IonRed.Split(',')[1]);

            var IonGreen = settings.GetStringValue("settings", "IonGreen", "7500,8800");
            var IonGreen1 = int.Parse(IonGreen.Split(',')[0]);
            var IonGreen2 = int.Parse(IonGreen.Split(',')[1]);

            var IonBlue = settings.GetStringValue("settings", "IonBlue", "9800,12000");
            var IonBlue1 = int.Parse(IonBlue.Split(',')[0]);
            var IonBlue2 = int.Parse(IonBlue.Split(',')[1]);

            var IonLevel = settings.GetStringValue("settings", "IonLevel", "120,320");
            var IonLevel1 = int.Parse(IonLevel.Split(',')[0]);
            var IonLevel2 = int.Parse(IonLevel.Split(',')[1]);

            var IonAmbient = settings.GetStringValue("settings", "IonAmbient", "3000,6000");
            var IonAmbient1 = int.Parse(IonAmbient.Split(',')[0]);
            var IonAmbient2 = int.Parse(IonAmbient.Split(',')[1]);

            if(!MapFile.SectionExists("Lighting"))
                MapFile.AddSection("Lighting");
            

            var lighting = MapFile.GetSection("Lighting");
            var r = new Random();
            double ambient = (double)r.Next(Ambient1, Ambient2) / 10000.0;
            if (r.Next(1, 1000) > 700)
                ambient = (double)r.Next(Ambient1 - 1500 , Ambient2 - 500) / 10000.0;
            double level = (double)r.Next(Level1, Level2) / 10000.0;
            double red = (double)r.Next(Red1, Red2) / 10000.0;
            double green = (double)r.Next(Green1, Green2) / 10000.0;
            double blue = (double)r.Next(Blue1, Blue2) / 10000.0;

            double wambient = (double)r.Next(IonAmbient1, IonAmbient2) / 10000.0;
            double wlevel = (double)r.Next(IonLevel1, IonLevel2) / 10000.0;
            double wred = (double)r.Next(IonRed1, IonRed2) / 10000.0;
            double wgreen = (double)r.Next(IonGreen1, IonGreen2) / 10000.0;
            double wblue = (double)r.Next(IonBlue1, IonBlue2) / 10000.0;

            lighting.SetStringValue("Ambient", String.Format("{0:0.000000}", ambient));
            lighting.SetStringValue("Level", String.Format("{0:0.000000}", level));
            lighting.SetStringValue("Red", String.Format("{0:0.000000}", red));
            lighting.SetStringValue("Green", String.Format("{0:0.000000}", green));
            lighting.SetStringValue("Blue", String.Format("{0:0.000000}", blue));

            lighting.SetStringValue("IonAmbient", String.Format("{0:0.000000}", wambient));
            lighting.SetStringValue("IonLevel", String.Format("{0:0.000000}", wlevel));
            lighting.SetStringValue("IonRed", String.Format("{0:0.000000}", wred));
            lighting.SetStringValue("IonGreen", String.Format("{0:0.000000}", wgreen));
            lighting.SetStringValue("IonBlue", String.Format("{0:0.000000}", wblue));

            MapFile.WriteIniFile();
        }

        /*unsafe public void GenerateMapPreview(Bitmap preview, string filePath)
        {
            var MapFile = new IniFile(filePath);
            var LocalSize = MapFile.GetStringValue("Map", "LocalSize", "0,0,0,0");
            var LocalWidth = int.Parse(LocalSize.Split(',')[2]);
            var LocalHeight = int.Parse(LocalSize.Split(',')[3]);

            BitmapData bmd = preview.LockBits(new Rectangle(0, 0, preview.Width, preview.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            byte[] image = new byte[preview.Width * preview.Height * 3];
            int idx = 0;

            // invert rgb->bgr
            for (int y = 0; y < bmd.Height; y++)
            {
                byte* p = (byte*)bmd.Scan0 + bmd.Stride * y;
                for (int x = 0; x < bmd.Width; x++)
                {
                    byte r = *p++;
                    byte g = *p++;
                    byte b = *p++;

                    image[idx++] = b;
                    image[idx++] = g;
                    image[idx++] = r;
                }
            }

            // encode
            byte[] image_compressed = Format5.Encode(image, 5);

            // base64 encode
            string image_base64 = Convert.ToBase64String(image_compressed, Base64FormattingOptions.None);

            // now overwrite [Preview] and [PreviewPack], inserting them directly after [Basic] if not yet existing
           
            
            if (MapFile.SectionExists("PreviewPack"))
                MapFile.RemoveSection("PreviewPack");
            MapFile.AddSection("PreviewPack");
            
            int rowNum = 1;
            for (int i = 0; i < image_base64.Length; i += 70)
            {
                MapFile.SetStringValue("PreviewPack",rowNum++.ToString(), image_base64.Substring(i, Math.Min(70, image_base64.Length - i)));
            }

            MapFile.WriteIniFile();
        }*/
    
        //public void RenderMap(string path)
        //{
        //    path = Program.ProgramFolder + "\\" + path;
        //    Log.Information("******************************************************");
        //    Log.Information("Rendering Map...");
        //    Process MapRenderer = new Process();
        //    var outputName = path.Split('\\').Last().Split('.')[0];
        //    MapRenderer.StartInfo.FileName = Program.RenderderPath;
        //    MapRenderer.StartInfo.UseShellExecute = false;
        //    MapRenderer.StartInfo.CreateNoWindow = true;
        //    MapRenderer.StartInfo.Arguments = $@"-i ""{path}"" -p -o ""{outputName}"" -m ""{Program.GameFolder.Trim('\\')}"" -r --mark-start-pos -s=4  -z +(1500,0)"; //--preview-markers-selected
        //    MapRenderer.Start();
        //    while (!MapRenderer.HasExited) { }
        //    Log.Information("Image is saved as " + outputName + ".png");
        //    Log.Information("******************************************************");
        //}

        //public void GeneratePreview(string path)
        //{
        //    path = Program.ProgramFolder + "\\" + path;
        //    Log.Information("******************************************************");
        //    Log.Information("Rendering Map...");
        //    Process MapRenderer = new Process();
        //    var outputName = path.Split('\\').Last().Split('.')[0];
        //    MapRenderer.StartInfo.FileName = Program.RenderderPath;
        //    MapRenderer.StartInfo.UseShellExecute = false;
        //    MapRenderer.StartInfo.CreateNoWindow = true;
        //    MapRenderer.StartInfo.Arguments = $@"-i ""{path}"" -m ""{Program.GameFolder.Trim('\\')}"" -r --mark-start-pos -s=4 --preview-markers-selected"; //"-i \"" + path + "\" -p -o \"" + outputName + "\" -m \"" + Program.游戏目录 + "\" -r --mark-start-pos --preview-markers-selected";
        //    MapRenderer.Start();
        //    while (!MapRenderer.HasExited) { }
        //    Log.Information("******************************************************");
        //}
    }
}

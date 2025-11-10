namespace RandomMapGenerator
{
    static class Constants
    {
        public readonly static string ProgramFolder = Path.Combine(Environment.CurrentDirectory, "Resources\\RandomMapGenerator_RA2\\");
        public static string FileName = "map.yrm";
        public static string SaveFileName = "Output.txt";
        public static string MapPackName = "IsoMapPack5";
        public static string FilePath = ProgramFolder + FileName;
        public static string SaveFilePath = ProgramFolder + SaveFileName;
        public static string BitMapName = "bitmap.bmp";
        public static string BitMapPath = ProgramFolder + BitMapName;
        public static string TemplateMapName = "templateMap.map";
        public static string TemplateMapPath = ProgramFolder + TemplateMapName;
        public static string TEMPERATEPath = ProgramFolder + @"TileInfo\TEMPERATE\";
        public static string SNOWPath = ProgramFolder + @"TileInfo\SNOW\";
        public static string URBANPath = ProgramFolder + @"TileInfo\URBAN\";
        public static string NEWURBANPath = ProgramFolder + @"TileInfo\NEWURBAN\";
        public static string LUNARPath = ProgramFolder + @"TileInfo\LUNAR\";
        public static string DESERTPath = ProgramFolder + @"TileInfo\DESERT\";

        public static int FailureTimes = 10000;
        public static string RenderPath = ProgramFolder + @"Map Renderer\CNCMaps.Renderer.exe";
        public static string GamePath = @"D:\Games\YURI\Red Alert 2";
    }
}

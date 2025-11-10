namespace RandomMapGenerator.NonTileObjects
{
    public class Terrain
    {
        public int RelativeX;
        public int X;
        public int RelativeY;
        public int Y;
        public string Name;

        public void Initialize(KeyValuePair<string, string> iniLine)
        {
            string key = iniLine.Key;
            int length = key.Length;
            string x = key.Substring(key.Length - 3, 3);
            string y = key.Substring(0, key.Length - 3);
            Name = iniLine.Value;
            RelativeX = int.Parse(x) - WorkingMap.StartingX;
            RelativeY = int.Parse(y) - WorkingMap.StartingY;
        }
        public Terrain Clone()
        {
            return (Terrain)this.MemberwiseClone();
        }
        public KeyValuePair<string, string> CreateINILine()
        {
            string key = Y.ToString() + string.Format("{0:000}", X);
            var iniLine = new KeyValuePair<string, string>(key, Name);
            return iniLine;
        }
    }
}

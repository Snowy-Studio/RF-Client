namespace RandomMapGenerator.NonTileObjects
{
    public class Waypoint
    {
        public int RelativeX;
        public int X;
        public int RelativeY;
        public int Y;
        public int Index;

        public void Initialize(KeyValuePair<string, string> iniLine)
        {
            string value = iniLine.Value;
            int length = value.Length;
            string x = value.Substring(value.Length - 3, 3);
            string y = value.Substring(0, value.Length - 3);
            Index = int.Parse(iniLine.Key);
            RelativeX = int.Parse(x) - WorkingMap.StartingX;
            RelativeY = int.Parse(y) - WorkingMap.StartingY;
        }
        public Waypoint Clone()
        {
            return (Waypoint)this.MemberwiseClone();
        }
        public KeyValuePair<string, string> CreateINILine()
        {
            string value = Y.ToString() + string.Format("{0:000}", X);
            var iniLine = new KeyValuePair<string, string>(Index.ToString(),value);
            return iniLine;
        }
    }
}

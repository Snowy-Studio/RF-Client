namespace RandomMapGenerator.TileInfo
{
    public class FailureAbstractMapUnitRecord
    {
        public int X;
        public int Y;
        public List<string> Name = new List<string>();

        public void AddFailureRecord(int x, int y, string name)
        {
            X = x;
            Y = y;
            Name.Add(name);
        }
        public bool IsTargetFailureRecord(int x, int y)
        {
            if (x == X && y == Y)
                return true;
            else
                return false;
        }
        //units with the same connection type
        public bool IsInFailureRecord(string name)
        {
            var targetMU = WorkingMap.GetAbstractMapUnitByName(name);
            foreach (var n in Name)
            {
                var thisMU = WorkingMap.GetAbstractMapUnitByName(n);
                if (targetMU.NEConnectionType == thisMU.NEConnectionType 
                    && targetMU.SEConnectionType == thisMU.SEConnectionType 
                    && targetMU.NWConnectionType == thisMU.NWConnectionType 
                    && targetMU.SWConnectionType == thisMU.SWConnectionType)
                    return true;
            }
            return false;
        }
    }
}

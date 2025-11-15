namespace RandomMapGenerator.NonTileObjects
{
    public class Structure
    {
        public string Owner;
        public string Name;
        public int Strength;
        public int RelativeX;
        public int X;
        public int RelativeY;
        public int Y;
        public int Direction;
        public string Tag;
        public int Sellable;
        public int Rebuild;
        public int EnergySupport;
        public int UpgradeCount;
        public int SpotLight;
        public string Upgrade1;
        public string Upgrade2;
        public string Upgrade3;
        public int AIRepairs;
        public int ShowName;

        public void Initialize(string iniValue)
        {
            string[] values = iniValue.Split(',');
            if (values.Count() == 17)
            {
                Owner = values[0];
                Name = values[1];
                Strength = int.Parse(values[2]);
                RelativeX = int.Parse(values[3]) - WorkingMap.StartingX;
                RelativeY = int.Parse(values[4]) - WorkingMap.StartingY;
                Direction = int.Parse(values[5]);
                Tag = values[6];
                Sellable = int.Parse(values[7]);
                Rebuild = int.Parse(values[8]);
                EnergySupport = int.Parse(values[9]);
                UpgradeCount = int.Parse(values[10]);
                SpotLight = int.Parse(values[11]);
                Upgrade1 = values[12];
                Upgrade2 = values[13];
                Upgrade3 = values[14];
                AIRepairs = int.Parse(values[15]);
                ShowName = int.Parse(values[16]);
            }
            else
                Console.WriteLine("An Structure cannot be parsed.");
        }
        public Structure Clone()
        {
            return (Structure)this.MemberwiseClone();
        }
        public string CreateINIValue()
        {
            return Owner + "," + Name + "," + Strength + "," + X + "," + Y + "," + Direction + "," + Tag + "," + Sellable
                + "," + Rebuild + "," + EnergySupport + "," + UpgradeCount + "," + SpotLight + "," + Upgrade1 + "," + Upgrade2
                + "," + Upgrade3 + "," + AIRepairs + "," + ShowName;
        }
    }
}

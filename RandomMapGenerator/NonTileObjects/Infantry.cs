namespace RandomMapGenerator.NonTileObjects
{
    public class Infantry
    {
        public string Owner;
        public string Name;
        public int Strength;
        public int RelativeX;
        public int X;
        public int RelativeY;
        public int Y;
        public int Unknown;
        public int Direction;
        public string State;
        public string Tag;
        public int Veteran;
        public int Group;
        public int OnBridge;
        public int AutocreateNoRecruitable;
        public int AutocreateYesRecruitable;

        public void Initialize(string iniValue)
        {
            string[] values = iniValue.Split(',');
            if (values.Count() == 14)
            {
                Owner = values[0];
                Name = values[1];
                Strength = int.Parse(values[2]);
                RelativeX = int.Parse(values[3]) - WorkingMap.StartingX;
                RelativeY = int.Parse(values[4]) - WorkingMap.StartingY;
                Unknown = int.Parse(values[5]);
                State = values[6];
                Direction = int.Parse(values[7]);
                Tag = values[8];
                Veteran = int.Parse(values[9]);
                Group = int.Parse(values[10]);
                OnBridge = int.Parse(values[11]);
                AutocreateNoRecruitable = int.Parse(values[12]);
                AutocreateYesRecruitable = int.Parse(values[13]);
            }
            else
                Console.WriteLine("An infantry cannot be parsed.");
        }
        public Infantry Clone()
        {
            return (Infantry)this.MemberwiseClone();
        }
        public string CreateINIValue()
        {
            return Owner + "," + Name + "," + Strength + "," + X + "," + Y + "," + Unknown + "," + State + "," + Direction
                + "," + Tag + "," + Veteran + "," + Group + "," + OnBridge + "," + AutocreateNoRecruitable + "," + AutocreateYesRecruitable;
        }
    }
}

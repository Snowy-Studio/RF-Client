using System;
using System.Collections.Generic;

namespace DTAConfig.Entity
{
    public class MapObject
    {
        public IsoTile Tile;
    }

    public class NamedMapObject : MapObject
    {
        public string Name { get; set; }
    }

    public class NumberedMapObject : MapObject
    {
        public virtual int Number { get; set; }
    }

    public class IsoTile : NumberedMapObject
    {
        public ushort Dx;
        public ushort Dy;
        public ushort Rx;
        public ushort Ry;
        public byte Z;
        public int TileNum;
        public byte SubTile;
        public byte IceGrowth;

        public IsoTile(ushort p1, ushort p2, ushort rx, ushort ry, byte z, int tilenum, byte subtile, byte icegrowth)
        {
            Dx = p1;
            Dy = p2;
            Rx = rx;
            Ry = ry;
            Z = z;
            TileNum = tilenum;
            SubTile = subtile;
            IceGrowth = icegrowth;
        }

        public List<byte> ToMapPack5Entry()
        {
            var ret = new List<byte>();
            ret.AddRange(BitConverter.GetBytes(Rx));
            ret.AddRange(BitConverter.GetBytes(Ry));
            ret.AddRange(BitConverter.GetBytes(TileNum));
            ret.Add(SubTile);
            ret.Add(Z);
            ret.Add(IceGrowth);
            return ret;
        }
    }
}

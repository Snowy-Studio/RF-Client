namespace RandomMapGenerator.TileInfo
{
    public class AbstractTileType
    {
        public int TileNum { get; set; }
        public int SubTile { get; set; }
        public int Z { get; set; }
        public bool Used { get; set; }
    }
}

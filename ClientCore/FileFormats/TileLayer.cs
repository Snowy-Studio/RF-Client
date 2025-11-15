using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Rampastring.Tools;

namespace DTAConfig.Entity
{
	public class TileLayer : IEnumerable<IsoTile> {
		IsoTile[,] isoTiles;
		private Size fullSize;

		public TileLayer(int w, int h)
			: this(new Size(w, h)) {
		}

		public TileLayer(Size fullSize) {
			this.fullSize = fullSize;
			isoTiles = new IsoTile[fullSize.Width * 2 - 1, fullSize.Height];
		}

		public int Width {
			get { return fullSize.Width; }
		}

		public int Height {
			get { return fullSize.Height; }
		}

		public virtual IsoTile this[int x, int y] {
			get {
				if (0 <= x && x < isoTiles.GetLength(0) && 0 <= y && y < isoTiles.GetLength(1))
					return isoTiles[x, y];
				else
					return null;
			}
			set {
				isoTiles[x, y] = value;
			}
		}

		/// <summary>Gets a tile at display coordinates.</summary>
		/// <param name="dx">The dx.</param>
		/// <param name="dy">The dy.</param>
		/// <returns>The tile.</returns>
		public IsoTile GetTile(int dx, int dy) {
			return isoTiles[dx, dy];
		}

		/// <summary>Gets a tile at map coordinates.</summary>
		/// <param name="rx">The rx.</param>
		/// <param name="ry">The ry.</param>
		/// <returns>The tile r.</returns>
		public IsoTile GetTileR(int rx, int ry) {
			int dx = (rx - ry + fullSize.Width - 1);
			int dy = rx + ry - fullSize.Width - 1;

			if (dx < 0 || dy < 0 || dx >= isoTiles.GetLength(0) || (dy / 2) >= isoTiles.GetLength(1))
				return null;
			else
				return GetTile(dx, dy / 2);
		}

		#region enumerator stuff
		public IEnumerator<IsoTile> GetEnumerator() {
			return new TwoDimensionalEnumerator<IsoTile>(isoTiles);
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return new TwoDimensionalEnumerator<IsoTile>(isoTiles);
		}
		#endregion

		public void SerializeIsoMapPack5(IniSection isoMapPack5, bool compress = false) {
			List<IsoTile> tileSet = new List<IsoTile>();
			byte[] encoded;

			foreach (var isoTile in this.isoTiles) {
				tileSet.Add(isoTile);
			}

			// Compressing involves removing level 0 clear tiles and then sort the tiles before encoding
			if (compress) {
				List<IsoTile> tileSetStage = new List<IsoTile>();
				List<byte[]> sortedTiles = new List<byte[]>();

				foreach (var t in tileSet) {
					if (t.TileNum > 0 || t.Z > 0 || t.SubTile > 0 || t.IceGrowth > 0)
						tileSetStage.Add(t);
				}
				if (tileSetStage.Count == 0) {
					tileSetStage.Add(tileSet.First());
					encoded = GetEncoded(tileSetStage);
				}
				else {
					sortedTiles.Add(GetEncoded(tileSetStage.OrderBy(x => x.Rx).ThenBy(x => x.SubTile).ThenBy(x => x.TileNum).ThenBy(x => x.Z).ToList()));
					sortedTiles.Add(GetEncoded(tileSetStage.OrderBy(x => x.Rx).ThenBy(x => x.TileNum).ThenBy(x => x.SubTile).ThenBy(x => x.Z).ToList()));
					sortedTiles.Add(GetEncoded(tileSetStage.OrderBy(x => x.SubTile).ThenBy(x => x.TileNum).ThenBy(x => x.Rx).ThenBy(x => x.Z).ToList()));
					sortedTiles.Add(GetEncoded(tileSetStage.OrderBy(x => x.SubTile).ThenBy(x => x.TileNum).ThenBy(x => x.Z).ThenBy(x => x.Rx).ToList()));
					sortedTiles.Add(GetEncoded(tileSetStage.OrderBy(x => x.SubTile).ThenBy(x => x.TileNum).ThenBy(x => x.Z).ThenBy(x => x.Ry).ToList()));
					sortedTiles.Add(GetEncoded(tileSetStage.OrderBy(x => x.SubTile).ThenBy(x => x.Z).ThenBy(x => x.TileNum).ThenBy(x => x.Rx).ToList()));
					sortedTiles.Add(GetEncoded(tileSetStage.OrderBy(x => x.SubTile).ThenBy(x => x.Z).ThenBy(x => x.TileNum).ThenBy(x => x.Ry).ToList()));
					sortedTiles.Add(GetEncoded(tileSetStage.OrderBy(x => x.TileNum).ThenBy(x => x.Rx).ThenBy(x => x.SubTile).ThenBy(x => x.Z).ToList()));
					sortedTiles.Add(GetEncoded(tileSetStage.OrderBy(x => x.TileNum).ThenBy(x => x.SubTile).ThenBy(x => x.Ry).ThenBy(x => x.Z).ToList()));
					sortedTiles.Add(GetEncoded(tileSetStage.OrderBy(x => x.TileNum).ThenBy(x => x.SubTile).ThenBy(x => x.Z).ThenBy(x => x.Rx).ToList()));
					sortedTiles.Add(GetEncoded(tileSetStage.OrderBy(x => x.TileNum).ThenBy(x => x.SubTile).ThenBy(x => x.Z).ThenBy(x => x.Ry).ToList()));
					sortedTiles.Add(GetEncoded(tileSetStage.OrderBy(x => x.TileNum).ThenBy(x => x.Z).ThenBy(x => x.SubTile).ThenBy(x => x.Rx).ToList()));
					sortedTiles.Add(GetEncoded(tileSetStage.OrderBy(x => x.TileNum).ThenBy(x => x.Z).ThenBy(x => x.SubTile).ThenBy(x => x.Ry).ToList()));
					sortedTiles.Add(GetEncoded(tileSetStage.OrderBy(x => x.Z).ThenBy(x => x.SubTile).ThenBy(x => x.TileNum).ThenBy(x => x.Rx).ToList()));
					sortedTiles.Add(GetEncoded(tileSetStage.OrderBy(x => x.Z).ThenBy(x => x.SubTile).ThenBy(x => x.TileNum).ThenBy(x => x.Ry).ToList()));
					sortedTiles.Add(GetEncoded(tileSetStage.OrderBy(x => x.Z).ThenBy(x => x.TileNum).ThenBy(x => x.Rx).ThenBy(x => x.SubTile).ToList()));
					sortedTiles.Add(GetEncoded(tileSetStage.OrderBy(x => x.Z).ThenBy(x => x.TileNum).ThenBy(x => x.Ry).ThenBy(x => x.SubTile).ToList()));
					sortedTiles.Add(GetEncoded(tileSetStage.OrderBy(x => x.Z).ThenBy(x => x.TileNum).ThenBy(x => x.SubTile).ThenBy(x => x.Rx).ToList()));
					sortedTiles.Add(GetEncoded(tileSetStage.OrderBy(x => x.Z).ThenBy(x => x.TileNum).ThenBy(x => x.SubTile).ThenBy(x => x.Ry).ToList()));
					int smallest = sortedTiles[0].Length;
					int smallestIndex = 0;
					for (int index = 0; index < sortedTiles.Count; index++) {
						if (sortedTiles[index].Length < smallest) {
							smallest = sortedTiles[index].Length;
							smallestIndex = index;
						}
					}
					encoded = sortedTiles[smallestIndex];
				}
			}
			else {
				encoded = GetEncoded(tileSet);
			}

			string compressed64 = Convert.ToBase64String(encoded, Base64FormattingOptions.None);

			int i = 1;
			int idx = 0;
			isoMapPack5.Clear();
			while (idx < compressed64.Length) {
				int adv = Math.Min(74, compressed64.Length - idx);
				isoMapPack5.SetValue(i++.ToString(), compressed64.Substring(idx, adv));
				idx += adv;
			}
		}

		private byte[] GetEncoded(List<IsoTile> tileSetParam) {
			// A tile is of 11 bytes. Last 4 bytes of padding is used for termination
			byte[] isoMapPack = new byte[tileSetParam.Count * 11 + 4];

			long di = 0;
			foreach (var tile in tileSetParam) {
				var bs = tile.ToMapPack5Entry().ToArray();
				Array.Copy(bs, 0, isoMapPack, di, 11);
				di += 11;
			}

			return Format5.Encode(isoMapPack, 5);
		}
	}
}

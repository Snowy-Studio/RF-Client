#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Localization.Tools;
using OpenRA.FileSystem;
using OpenRA.Mods.Cnc.FileFormats;
using OpenRA.Primitives;
using FS = OpenRA.FileSystem.FileSystem;

namespace OpenRA.Mods.Cnc.FileSystem
{
	public class MixLoader : IPackageLoader
	{
		public sealed class MixFile : IReadOnlyPackage
		{
			public string Name { get; }
			public IEnumerable<string> Contents => index.Keys;

			readonly Dictionary<string, PackageEntry> index;
			readonly long dataStart;
			readonly Stream s;

            public MixFile(Stream s, string filename, string[] globalFilenames)
            {
                Name = filename;
                this.s = s;

                // ----- ⭐ 从 Resources 目录加载 global mix database.dat ⭐ -----
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "global mix database.dat");
                if (globalFilenames.Length == 0 && File.Exists(dbPath))
                {
                    using (var mixDatabase = File.OpenRead(dbPath))
                    using (var db = new XccGlobalDatabase(mixDatabase))
                        globalFilenames = db.Entries.ToHashSet().ToArray();

                    Console.WriteLine($"Loaded global mix database: {globalFilenames.Length} entries");
                }
                else
                {
                    Console.WriteLine("Failed to load global mix database.dat");
                }
                // --------------------------------------------------------------------

                try
                {
                    // Detect format type
                    var isCncMix = s.ReadUInt16() != 0;

                    // The C&C mix format doesn't contain any flags or encryption
                    var isEncrypted = false;
                    if (!isCncMix)
                        isEncrypted = (s.ReadUInt16() & 0x2) != 0;

                    List<PackageEntry> entries;
                    if (isEncrypted)
                        entries = ParseHeader(DecryptHeader(s, 4, out dataStart), 0, out _);
                    else
                        entries = ParseHeader(s, isCncMix ? 0 : 4, out dataStart);

                    index = ParseIndex(
                        entries.ToDictionaryWithConflictLog(
                            x => x.Hash,
                            $"{filename} ({(isCncMix ? "C&C" : "RA/TS/RA2")} format, Encrypted: {isEncrypted}, DataStart: {dataStart})",
                            null,
                            x => $"(offs={x.Offset}, len={x.Length})"),
                        globalFilenames);
                }
                catch (Exception)
                {
                    Dispose();
                    throw;
                }
            }

            public MixFile(string filename) : this(File.OpenRead(filename), filename, [])
            {
                
            }

			public static List<byte[]> GetDirFiles(string folderPath, string type)
			{
                var csfs = new List<byte[]>();
                foreach (var file in Directory.GetFiles(folderPath, "*.mix").OrderBy(f => f))
                    csfs.AddRange(GetFiles(file,type));
                return csfs;
            }

            public static List<string> GetDirFileNames(string folderPath, string type)
            {
                var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // HashSet 自动去重，不区分大小写
                foreach (var mixFile in Directory.GetFiles(folderPath, "*.mix"))
                {
                    try
                    {
                        var mix = new MixFile(mixFile);
                        foreach (var entry in mix.Index)
                        {
                            if (entry.Key.EndsWith(type, StringComparison.InvariantCultureIgnoreCase))
                            {
                                result.Add(entry.Key); // HashSet 自动去重
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // 忽略出错的 mix 文件
                    }
                }

                return result.OrderBy(f => f).ToList(); // 排序返回
            }


            public static List<byte[]> GetFiles(string filename,string type)
			{
				var csfs = new List<byte[]>();
				try
				{
					var mix = new MixFile(filename);
					foreach (var entry in mix.Index.OrderBy(e => e.Key))
					{
						if (entry.Key.EndsWith(type, StringComparison.InvariantCultureIgnoreCase))
						{
							csfs.Add(mix.GetFileContent(entry.Key));
						}
					}
				}
				catch (Exception)
				{
				}
                    return csfs;
			}

            public byte[] GetFileContent(string filename)
			{
				if (index.TryGetValue(filename, out PackageEntry entry))
				{
					// 确保流未被释放
					if (s == null || !s.CanRead)
						throw new ObjectDisposedException("Stream has been closed.");

					// 定位到数据区的偏移位置
					long actualOffset = dataStart + entry.Offset;
					s.Seek(actualOffset, SeekOrigin.Begin);

					// 读取文件内容
					byte[] content = new byte[entry.Length];
					int bytesRead = s.Read(content, 0, content.Length);

					// 检查是否读取完整
					if (bytesRead != entry.Length)
						throw new InvalidOperationException("Failed to read the entire file content.");

					return content;
				}

				// 文件不存在于索引中
				return null;
			}

			Dictionary<string, PackageEntry> ParseIndex(Dictionary<uint, PackageEntry> entries, string[] globalFilenames)
			{
				var allPossibleFilenames = new HashSet<string>(globalFilenames);

				// Try and find a local mix database
				var dbNameClassic = PackageEntry.HashFilename("local mix database.dat", PackageHashType.Classic);
				var dbNameCRC = PackageEntry.HashFilename("local mix database.dat", PackageHashType.CRC32);
				foreach (var kv in entries)
				{
					if (kv.Key == dbNameClassic || kv.Key == dbNameCRC)
					{
						using (var content = GetContent(kv.Value))
						{
							var db = new XccLocalDatabase(content);
							allPossibleFilenames.EnsureCapacity(allPossibleFilenames.Count + db.Entries.Length);
							allPossibleFilenames.UnionWith(db.Entries);
						}

						break;
					}
				}

				var classicIndex = new Dictionary<string, PackageEntry>(entries.Count);
				var crcIndex = new Dictionary<string, PackageEntry>(entries.Count);

				foreach (var filename in allPossibleFilenames)
				{
					var classicHash = PackageEntry.HashFilename(filename, PackageHashType.Classic);
					var crcHash = PackageEntry.HashFilename(filename, PackageHashType.CRC32);

					if (entries.TryGetValue(classicHash, out var e))
						classicIndex.Add(filename, e);

					if (entries.TryGetValue(crcHash, out e))
						crcIndex.Add(filename, e);
				}

				var bestIndex = crcIndex.Count > classicIndex.Count ? crcIndex : classicIndex;

				var unknown = entries.Count - bestIndex.Count;
				if (unknown > 0)
					Console.WriteLine($"{Name}: failed to resolve filenames for {unknown} unknown hashes");

				bestIndex.TrimExcess();
				return bestIndex;
			}

			static List<PackageEntry> ParseHeader(Stream s, long offset, out long headerEnd)
			{
				s.Seek(offset, SeekOrigin.Begin);
				var numFiles = s.ReadUInt16();
				s.ReadUInt32(); // dataSize

				var items = new List<PackageEntry>();
				for (var i = 0; i < numFiles; i++)
					items.Add(new PackageEntry(s));

				headerEnd = offset + 6 + numFiles * PackageEntry.Size;
				return items;
			}

			static MemoryStream DecryptHeader(Stream s, long offset, out long headerEnd)
			{
				s.Seek(offset, SeekOrigin.Begin);

				// Decrypt blowfish key
				var keyblock = s.ReadBytes(80);
				var blowfishKey = new BlowfishKeyProvider().DecryptKey(keyblock);
				var fish = new Blowfish(blowfishKey);

				// Decrypt first block to work out the header length
				var ms = Decrypt(ReadBlocks(s, offset + 80, 1), fish);
				var numFiles = ms.ReadUInt16();

				// Decrypt the full header - round bytes up to a full block
				var blockCount = (13 + numFiles * PackageEntry.Size) / 8;
				headerEnd = offset + 80 + blockCount * 8;

				return Decrypt(ReadBlocks(s, offset + 80, blockCount), fish);
			}

			static MemoryStream Decrypt(uint[] h, Blowfish fish)
			{
				var decrypted = fish.Decrypt(h);

				var ms = new MemoryStream(decrypted.Length * 4);
				var writer = new BinaryWriter(ms);
				foreach (var t in decrypted)
					writer.Write(t);
				writer.Flush();

				ms.Seek(0, SeekOrigin.Begin);
				return ms;
			}

			static uint[] ReadBlocks(Stream s, long offset, int count)
			{
				if (offset < 0)
					throw new ArgumentOutOfRangeException(nameof(offset), "Non-negative number required.");

				if (count < 0)
					throw new ArgumentOutOfRangeException(nameof(count), "Non-negative number required.");

				if (offset + count * 2 > s.Length)
					throw new ArgumentException($"Bytes to read {count * 2} and offset {offset} greater than stream length {s.Length}.");

				s.Seek(offset, SeekOrigin.Begin);

				// A block is a single encryption unit (represented as two 32-bit integers)
				var ret = new uint[2 * count];
				for (var i = 0; i < ret.Length; i++)
					ret[i] = s.ReadUInt32();

				return ret;
			}

			public Stream GetContent(PackageEntry entry)
			{
				return SegmentStream.CreateWithoutOwningStream(s, dataStart + entry.Offset, (int)entry.Length);
			}

			public Stream GetStream(string filename)
			{
				if (!index.TryGetValue(filename, out var e))
					return null;

				return GetContent(e);
			}

			public IReadOnlyDictionary<string, PackageEntry> Index
			{
				get
				{
					var absoluteIndex = index.ToDictionary(e => e.Key, e => new PackageEntry(e.Value.Hash, (uint)(e.Value.Offset + dataStart), e.Value.Length));
					return new ReadOnlyDictionary<string, PackageEntry>(absoluteIndex);
				}
			}

			public bool Contains(string filename)
			{
				return index.ContainsKey(filename);
			}

			public IReadOnlyPackage OpenPackage(string filename, FS context)
			{
				var childStream = GetStream(filename);
				if (childStream == null)
					return null;

				if (context.TryParsePackage(childStream, filename, out var package))
					return package;

				childStream.Dispose();
				return null;
			}

			public void Dispose()
			{
				s.Dispose();
			}
		}

		string[] globalFilenames;

		bool IPackageLoader.TryParsePackage(Stream s, string filename, FS context, out IReadOnlyPackage package)
		{
			if (!filename.EndsWith(".mix", StringComparison.InvariantCultureIgnoreCase))
			{
				package = null;
				return false;
			}

			// Load the global mix database
			if (globalFilenames == null && context.TryOpen("global mix database.dat", out var mixDatabase))
				using (var db = new XccGlobalDatabase(mixDatabase))
					globalFilenames = db.Entries.ToHashSet().ToArray();

			package = new MixFile(s, filename, globalFilenames ?? Array.Empty<string>());
			return true;
		}
	}
}

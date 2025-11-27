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
using System.IO;
using System.Linq;
using OpenRA.Primitives;

namespace OpenRA.FileSystem
{
	public interface IReadOnlyFileSystem
	{
		Stream Open(string filename);
		bool TryGetPackageContaining(string path, out IReadOnlyPackage package, out string filename);
		bool TryOpen(string filename, out Stream s);
		bool Exists(string filename);
		bool IsExternalFile(string filename);
	}

	public class FileSystem : IReadOnlyFileSystem
	{
		public IEnumerable<IReadOnlyPackage> MountedPackages => mountedPackages.Keys;
		readonly Dictionary<IReadOnlyPackage, int> mountedPackages = new();
		readonly Dictionary<string, IReadOnlyPackage> explicitMounts = new();
		readonly string modID;

		// Mod packages that should not be disposed
		readonly List<IReadOnlyPackage> modPackages = new();
		//readonly IReadOnlyDictionary<string, Manifest> installedMods;
		readonly IPackageLoader[] packageLoaders;

		Cache<string, List<IReadOnlyPackage>> fileIndex = new(_ => new List<IReadOnlyPackage>());

		public bool TryParsePackage(Stream stream, string filename, out IReadOnlyPackage package)
		{
			package = null;
			foreach (var packageLoader in packageLoaders)
				if (packageLoader.TryParsePackage(stream, filename, this, out package))
					return true;

			return false;
		}

		Stream GetFromCache(string filename)
		{
			var package = fileIndex[filename]
				.LastOrDefault(x => x.Contains(filename));

			return package?.GetStream(filename);
		}

		public Stream Open(string filename)
		{
			if (!TryOpen(filename, out var s))
				throw new FileNotFoundException($"File not found: {filename}", filename);

			return s;
		}

		public bool TryGetPackageContaining(string path, out IReadOnlyPackage package, out string filename)
		{
			var explicitSplit = path.IndexOf('|');
			if (explicitSplit > 0 && explicitMounts.TryGetValue(path[..explicitSplit], out package))
			{
				filename = path[(explicitSplit + 1)..];
				return true;
			}

			package = fileIndex[path].LastOrDefault(x => x.Contains(path));
			filename = path;

			return package != null;
		}

		public bool TryOpen(string filename, out Stream s)
		{
			var explicitSplit = filename.IndexOf('|');
			if (explicitSplit > 0 && explicitMounts.TryGetValue(filename[..explicitSplit], out var explicitPackage))
			{
				s = explicitPackage.GetStream(filename[(explicitSplit + 1)..]);
				if (s != null)
					return true;
			}

			s = GetFromCache(filename);
			if (s != null)
				return true;

			// The file should be in an explicit package (but we couldn't find it)
			// Thus don't try to find it using the filename (which contains the invalid '|' char)
			// This can be removed once the TODO below is resolved
			if (explicitSplit > 0)
				return false;

			// Ask each package individually
			// TODO: This fallback can be removed once the filesystem cleanups are complete
			var package = mountedPackages.Keys.LastOrDefault(x => x.Contains(filename));
			if (package != null)
			{
				s = package.GetStream(filename);
				return s != null;
			}

			s = null;
			return false;
		}

		public bool Exists(string filename)
		{
			var explicitSplit = filename.IndexOf('|');
			if (explicitSplit > 0 &&
				explicitMounts.TryGetValue(filename[..explicitSplit], out var explicitPackage) &&
				explicitPackage.Contains(filename[(explicitSplit + 1)..]))
				return true;

			return fileIndex.ContainsKey(filename);
		}

		/// <summary>
		/// Returns true if the given filename references any file outside the mod mount.
		/// </summary>
		public bool IsExternalFile(string filename)
		{
			return !filename.StartsWith($"{modID}|", StringComparison.Ordinal);
		}

		public static string ResolveCaseInsensitivePath(string path)
		{
			var resolved = Path.GetPathRoot(path);

			if (resolved == null)
				return null;

			foreach (var name in path[resolved.Length..].Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
			{
				// Filter out paths of the form /foo/bar/./baz
				if (name == ".")
					continue;

				resolved = Directory.GetFileSystemEntries(resolved)
					.FirstOrDefault(e => e.Equals(Path.Combine(resolved, name), StringComparison.InvariantCultureIgnoreCase));

				if (resolved == null)
					return null;
			}

			return resolved;
		}

		public string GetPrefix(IReadOnlyPackage package)
		{
			return explicitMounts.ContainsValue(package) ? explicitMounts.First(f => f.Value == package).Key : null;
		}
	}
}

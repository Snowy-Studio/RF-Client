using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Fun;

/// <summary>
/// Provides safe cross platform Path handling.
/// </summary>
internal static class SafePath
{
    /// <summary>
    /// Safely combines multiple directory paths for all platforms.
    /// The first path can be a relative or absolute path.
    /// </summary>
    /// <param name="paths">Ordered list of directory paths.</param>
    /// <returns>The combined directory path of all <paramref name="paths"/>, with trailing separator character and with leading separator character if the path has a root.</returns>
    public static string CombineDirectoryPath(params string[] paths)
    {
        return Combine(true, paths);
    }

    /// <summary>
    /// Safely combines multiple directory paths with a file name for all platforms.
    /// The first path can be a relative or absolute path.
    /// </summary>
    /// <param name="paths">Ordered list of directory paths with a file name.</param>
    /// <returns>The combined directory path and file name of all <paramref name="paths"/> with leading separator character if the path has a root.</returns>
    public static string CombineFilePath(params string[] paths)
    {
        return Combine(false, paths);
    }

    /// <summary>
    /// Safely combines multiple directory paths for all platforms.
    /// The first path can be a relative or absolute path.
    /// </summary>
    /// <param name="paths">Ordered list of directory paths.</param>
    /// <returns>A <see cref="DirectoryInfo"/> instance representing the combined directory path of all <paramref name="paths"/>.</returns>
    public static DirectoryInfo GetDirectory(params string[] paths)
    {
        return new DirectoryInfo(CombineDirectoryPath(paths));
    }

    /// <summary>
    /// Safely combines multiple directory paths with a file name for all platforms.
    /// The first path can be a relative or absolute path.
    /// </summary>
    /// <param name="paths">Ordered list of directory paths with a file name.</param>
    /// <returns>A <see cref="FileInfo"/> instance representing the combined directory path and file name of all <paramref name="paths"/>.</returns>
    public static FileInfo GetFile(params string[] paths)
    {
        return new FileInfo(CombineFilePath(paths));
    }

    /// <summary>
    /// Safely delete a file represented by multiple directory paths with a file name for all platforms.
    /// Does not throw an exception if the file or directory path does not exist.
    /// </summary>
    /// <param name="paths">Ordered list of directory paths with a file name.</param>
    public static void DeleteFileIfExists(params string[] paths)
    {
        var fileInfo = new FileInfo(CombineFilePath(paths));

        if (fileInfo.Exists)
        {
            fileInfo.Delete();
        }
    }

    /// <summary>
    /// Safely delete a directory represented by multiple directory paths for all platforms.
    /// Does not throw an exception if the directory path does not exist.
    /// </summary>
    /// <param name="recursive">true to remove directories, subdirectories, and files in path; otherwise, false.</param>
    /// <param name="paths">Ordered list of directory paths.</param>
    public static void DeleteDirectoryIfExists(bool recursive, params string[] paths)
    {
        var directoryInfo = new DirectoryInfo(CombineFilePath(paths));

        if (directoryInfo.Exists)
        {
            directoryInfo.Delete(recursive);
        }
    }

    /// <summary>
    /// Safely retrieves the name of a directory represented by multiple directory paths and a file name for all platforms.
    /// </summary>
    /// <param name="paths">Ordered list of directory paths and a file name.</param>
    /// <returns>The name (not path) of the directory a given file resides in.</returns>
    public static string GetFileDirectoryName(params string[] paths)
    {
        var fileInfo = new FileInfo(CombineFilePath(paths));

        return fileInfo.DirectoryName;
    }

    private static string Combine(bool isDirectoryPath, params string[] paths)
    {
        if (paths is null)
        {
            return null;
        }

        paths = paths.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();

        if (paths.Length == 0)
        {
            return null;
        }

        if (paths.Length == 1)
        {
            paths = new[] { paths[0], null };
        }

        string combinedPath = paths.Aggregate((x, y) => y is null ? GetPath(x) : Path.Combine(GetPath(x), GetPath(y)))
            .TrimStart(Path.DirectorySeparatorChar)
            .TrimStart(Path.AltDirectorySeparatorChar)
            .TrimEnd(Path.DirectorySeparatorChar)
            .TrimEnd(Path.AltDirectorySeparatorChar);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (paths[0].Length > 1 && paths[0][1] == Path.VolumeSeparatorChar)
            {
                return FormattableString.Invariant($"{combinedPath}{(isDirectoryPath ? Path.DirectorySeparatorChar : null)}");
            }

            return FormattableString.Invariant($"{(Path.IsPathRooted(paths[0]) ? Path.DirectorySeparatorChar : null)}{combinedPath}{(isDirectoryPath ? Path.DirectorySeparatorChar : null)}");
        }

        return FormattableString.Invariant($"{(Path.IsPathRooted(paths[0]) ? Path.DirectorySeparatorChar : null)}{combinedPath}{(isDirectoryPath ? Path.DirectorySeparatorChar : null)}");
    }

    private static string GetPath(string path)
    {
        return FormattableString.Invariant($"{path?
            .Replace("///", "/", StringComparison.Ordinal)
            .Replace("//", "/", StringComparison.Ordinal)
            .Replace("/", Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            .Replace("\\\\\\", "\\", StringComparison.Ordinal)
            .Replace("\\\\", "\\", StringComparison.Ordinal)
            .Replace("\\", Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            .TrimStart(Path.DirectorySeparatorChar)
            .TrimStart(Path.AltDirectorySeparatorChar)
            .TrimEnd(Path.DirectorySeparatorChar)
            .TrimEnd(Path.AltDirectorySeparatorChar)}{Path.DirectorySeparatorChar}");
    }
}
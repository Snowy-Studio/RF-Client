﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ClientCore;
using Rampastring.Tools;

namespace Ra2Client.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// Handles sharing maps.
    /// </summary>
    public static class MapSharer
    {
        public static event EventHandler<MapEventArgs> MapUploadFailed;
        public static event EventHandler<MapEventArgs> MapUploadComplete;
        public static event EventHandler<MapEventArgs> MapUploadStarted;
        public static event EventHandler<SHA1EventArgs> MapDownloadFailed;
        public static event EventHandler<SHA1EventArgs> MapDownloadComplete;
        public static event EventHandler<SHA1EventArgs> MapDownloadStarted;

        private volatile static List<string> MapDownloadQueue = new List<string>();
        private volatile static List<Map> MapUploadQueue = new List<Map>();
        private volatile static List<string> UploadedMaps = new List<string>();

        private static readonly object locker = new object();

        private const string MAPDB_URL = "http://mapdb.cncnet.org/upload";

        /// <summary>
        /// Adds a map into the CnCNet map upload queue.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <param name="myGame">The short name of the game that is being played (DTA, TI, MO, etc).</param>
        public static void UploadMap(Map map, string myGame)
        {
            lock (locker)
            {
                if (UploadedMaps.Contains(map.SHA1) || MapUploadQueue.Contains(map))
                {
                    Logger.Log("MapSharer: Already uploading map " + map.BaseFilePath + " - returning.");
                    return;
                }

                MapUploadQueue.Add(map);

                if (MapUploadQueue.Count == 1)
                {
                    _ = Task.Run(() => UploadAsync(map, myGame.ToLower()));
                }
            }
        }

        private static async Task UploadAsync(Map map, string myGameId)
        {
            MapUploadStarted?.Invoke(null, new MapEventArgs(map));
            Logger.Log("MapSharer: Starting upload of " + map.BaseFilePath);

            var (message, success) = await MapUpload(MAPDB_URL, map, myGameId);
            if (success)
            {
                MapUploadComplete?.Invoke(null, new MapEventArgs(map));

                lock (locker)
                {
                    UploadedMaps.Add(map.SHA1);
                }

                Logger.Log("MapSharer: Uploading map " + map.BaseFilePath + " completed successfully.");
            }
            else
            {
                MapUploadFailed?.Invoke(null, new MapEventArgs(map));
                Logger.Log("MapSharer: Uploading map " + map.BaseFilePath + " failed! Returned message: " + message);
            }

            lock (locker)
            {
                MapUploadQueue.Remove(map);
                if (MapUploadQueue.Count > 0)
                {
                    Map nextMap = MapUploadQueue[0];
                    Logger.Log("MapSharer: There are additional maps in the queue.");
                    _ = Task.Run(() => UploadAsync(nextMap, myGameId));
                }
            }
        }

        private static async Task<(string, bool)> MapUpload(string _URL, Map map, string gameName)
        {
            ServicePointManager.Expect100Continue = false;

            FileInfo zipFile = SafePath.GetFile(ProgramConstants.GamePath, "Maps", "Custom", FormattableString.Invariant($"{map.SHA1}.zip"));

            if (zipFile.Exists) zipFile.Delete();

            string mapFileName = map.SHA1 + MapLoader.MAP_FILE_EXTENSION;

            File.Copy(SafePath.CombineFilePath(map.CompleteFilePath), SafePath.CombineFilePath(ProgramConstants.GamePath, mapFileName));

            CreateZipFile(mapFileName, zipFile.FullName);

            try
            {
                SafePath.DeleteFileIfExists(ProgramConstants.GamePath, mapFileName);
            }
            catch { }

            try
            {
                using (FileStream stream = zipFile.Open(FileMode.Open))
                {
                    List<FileToUpload> files = new List<FileToUpload>();

                    FileToUpload file = new FileToUpload()
                    {
                        Name = "file",
                        Filename = zipFile.Name,
                        ContentType = "mapZip",
                        Stream = stream
                    };

                    files.Add(file);

                    NameValueCollection values = new NameValueCollection
                    {
                        { "game", gameName.ToLower() },
                    };

                    byte[] responseArray = await UploadFiles(_URL, files, values);
                    string response = Encoding.UTF8.GetString(responseArray);

                    if (!response.Contains("Upload succeeded!"))
                    {
                        return (response, false);
                    }
                    Logger.Log("MapSharer: Upload response: " + response);

                    return (string.Empty, true);
                }
            }
            catch (Exception ex)
            {
                return (ex.Message, false);
            }
        }

        private static async Task<byte[]> UploadFiles(string address, List<FileToUpload> files, NameValueCollection values)
        {
            using (var client = new HttpClient())
            using (var content = new MultipartFormDataContent())
            {
                foreach (string name in values.Keys)
                {
                    content.Add(new StringContent(values[name]), name);
                }

                foreach (FileToUpload file in files)
                {
                    var fileContent = new StreamContent(file.Stream);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                    content.Add(fileContent, file.Name, file.Filename);
                }

                var response = await client.PostAsync(address, content);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync();
            }
        }

        private static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[32768];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, read);
            }
        }

        private static void CreateZipFile(string file, string zipName)
        {
            using var zipFileStream = new FileStream(zipName, FileMode.CreateNew, FileAccess.Write);
            using var archive = new ZipArchive(zipFileStream, ZipArchiveMode.Create);
            archive.CreateEntryFromFile(SafePath.CombineFilePath(ProgramConstants.GamePath, file), file);
        }

        private static string ExtractZipFile(string zipFile, string destDir)
        {
            using ZipArchive zipArchive = ZipFile.OpenRead(zipFile);

            // here, we extract every entry, but we could extract conditionally
            // based on entry name, size, date, checkbox status, etc.  
            zipArchive.ExtractToDirectory(destDir);
            return zipArchive.Entries.FirstOrDefault()?.Name;
        }

        public static void DownloadMap(string sha1, string myGame, string mapName)
        {
            lock (locker)
            {
                if (MapDownloadQueue.Contains(sha1))
                {
                    Logger.Log("MapSharer: Map " + sha1 + " already exists in the download queue.");
                    return;
                }

                MapDownloadQueue.Add(sha1);

                if (MapDownloadQueue.Count == 1)
                {
                    object[] details = new object[3];
                    details[0] = sha1;
                    details[1] = myGame.ToLower();
                    details[2] = mapName;

                    Task.Run(() => DownloadAsync(sha1, myGame.ToLower(), mapName));
                }
            }
        }

        private static async Task DownloadAsync(string sha1, string myGameId, string mapName)
        {
            Logger.Log("MapSharer: Preparing to download map " + sha1 + " with name: " + mapName);

            try
            {
                Logger.Log("MapSharer: MapDownloadStarted");
                MapDownloadStarted?.Invoke(null, new SHA1EventArgs(sha1, mapName));
            }
            catch (Exception ex)
            {
                Logger.Log("MapSharer: ERROR " + ex.Message);
            }

            var (mapPath, downloadSuccess) = await DownloadMain(sha1, myGameId, mapName);

            lock (locker)
            {
                if (downloadSuccess)
                {
                    Logger.Log("MapSharer: Download_Notice of map " + sha1 + " completed successfully.");
                    MapDownloadComplete?.Invoke(null, new SHA1EventArgs(sha1, mapName));
                }
                else
                {
                    Logger.Log("MapSharer: Download_Notice of map " + sha1 + " failed! Reason: " + mapPath);
                    MapDownloadFailed?.Invoke(null, new SHA1EventArgs(sha1, mapName));
                }

                MapDownloadQueue.Remove(sha1);

                if (MapDownloadQueue.Count > 0)
                {
                    Logger.Log("MapSharer: Continuing custom map downloads.");
                    string nextSha1 = MapDownloadQueue[0];
                    // 这里简单将 mapName 和 myGameId 传递给下一个下载任务，根据业务需求可自行调整
                    Task.Run(() => DownloadAsync(nextSha1, myGameId, mapName));
                }
            }
        }

        public static string GetMapFileName(string sha1, string mapName)
            => mapName + "_" + sha1;

        private static async Task<(string, bool)> DownloadMain(string sha1, string myGame, string mapName)
        {
            string customMapsDirectory = SafePath.CombineDirectoryPath(ProgramConstants.GamePath, "Maps", "Custom");
            string mapFileName = GetMapFileName(sha1, mapName);
            FileInfo destinationFile = SafePath.GetFile(customMapsDirectory, FormattableString.Invariant($"{mapFileName}.zip"));

            // This string is up here so we can check that there isn't already a .map file for this download.
            // This prevents the client from crashing when trying to rename the unzipped file to a duplicate filename.
            FileInfo newFile = SafePath.GetFile(customMapsDirectory, FormattableString.Invariant($"{mapFileName}{MapLoader.MAP_FILE_EXTENSION}"));

            destinationFile.Delete();
            newFile.Delete();

            try
            {
                using (var client = new HttpClient())
                {
                    Logger.Log("MapSharer: Downloading URL: " + "http://mapdb.cncnet.org/" + myGame + "/" + sha1 + ".zip");
                    var response = await client.GetAsync("http://mapdb.cncnet.org/" + myGame + "/" + sha1 + ".zip");
                    response.EnsureSuccessStatusCode();
                    var data = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(destinationFile.FullName, data);
                }
            }
            catch (Exception ex)
            {
                return (ex.Message, false);
            }

            destinationFile.Refresh();

            if (!destinationFile.Exists)
            {
                return (null, false);
            }

            string extractedFile = ExtractZipFile(destinationFile.FullName, customMapsDirectory);

            if (String.IsNullOrEmpty(extractedFile))
            {
                return (null, false);
            }

            // We can safely assume that there will not be a duplicate file due to deleting it
            // earlier if one already existed.
            File.Move(SafePath.CombineFilePath(customMapsDirectory, extractedFile), newFile.FullName);
            destinationFile.Delete();

            return (extractedFile, true);
        }

        class FileToUpload
        {
            public FileToUpload() => ContentType = "application/octet-stream";
            public string Name { get; set; }
            public string Filename { get; set; }
            public string ContentType { get; set; }
            public Stream Stream { get; set; }
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClientCore;
using Ra2Client.Domain.Multiplayer;
using Rampastring.Tools;
using Utilities = Rampastring.Tools.Utilities;

namespace Ra2Client.Online
{
    public class FileHashCalculator
    {
        private FileHashes fh;
        private const string CONFIGNAME = "FHCConfig.ini";
        private bool calculateGameExeHash = true;

        string[] fileNamesToCheck = new string[]
        {
            "Ares.dll",
            "Ares.dll.inj",
            "Ares.mix",
            "Syringe.exe",
            "cncnet5.dll",
            "rulesmd.ini",
            "artmd.ini",
            "soundmd.ini",
            "aimd.ini",
            "shroud.shp",
        };

        public FileHashCalculator() => ParseConfigFile();

        public void CalculateHashes(string modPath = "",string otherFilePath = "")
        {
            fh = new FileHashes
            {
                GameOptionsHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ProgramConstants.BASE_RESOURCE_PATH, "GameOptions.ini")),
                ClientDXHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "Binaries",  "Ra2Client.dll")),
                //GameExeHash = calculateGameExeHash ?
                //Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.GetGameExecutableName())) : string.Empty,
                LauncherExeHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.GameLauncherExecutableName)),
                //MPMapsHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.游戏目录, ClientConfiguration.Instance.GameModesIniPath)),
              //  FHCConfigHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.BASE_RESOURCE_PATH, CONFIGNAME)),
                INIHashes = string.Empty
            };

          //  Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + CONFIGNAME + ": " + fh.FHCConfigHash);
            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + "\\GameOptions.ini: " + fh.GameOptionsHash);
            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + "\\Binaries\\Ra2Client.dll: " + fh.ClientDXHash);
            // Logger.Log("Hash for " + ClientConfiguration.Instance.GameModesIniPath + ": " + fh.MPMapsHash);

            if (calculateGameExeHash)
                Logger.Log("Hash for " + ClientConfiguration.Instance.GetGameExecutableName() + ": " + fh.GameExeHash);

            if (!string.IsNullOrEmpty(ClientConfiguration.Instance.GameLauncherExecutableName))
                Logger.Log("Hash for " + ClientConfiguration.Instance.GameLauncherExecutableName + ": " + fh.LauncherExeHash);

            foreach (string filePath in fileNamesToCheck)
            {
                fh.INIHashes = AddToStringIfFileExists(fh.INIHashes, filePath);
                Logger.Log("Hash for " + filePath + ": " +
                    Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, filePath)));
            }

            DirectoryInfo[] iniPaths =
            {
               SafePath.GetDirectory(ProgramConstants.GamePath, "INI", "Multi", "MapCode"),
               SafePath.GetDirectory(ProgramConstants.GamePath, "INI", "Multi", "GameOptions"),
               SafePath.GetDirectory(modPath),
               SafePath.GetDirectory(otherFilePath),
            };

            foreach (DirectoryInfo path in iniPaths)
            {
                if (path.Exists)
                {
                    List<string> files = path.EnumerateFiles("*", SearchOption.AllDirectories).Select(s => s.Name).ToList();

                    files.Sort(StringComparer.Ordinal);

                    foreach (string filename in files)
                    {
                        string sha1 = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, filename));
                        fh.INIHashes += sha1;
                        Logger.Log("Hash for " + filename + ": " + sha1);
                    }
                }
            }

            fh.INIHashes = Utilities.CalculateSHA1ForString(fh.INIHashes);
        }

        string AddToStringIfFileExists(string str, string path)
        {
            if (File.Exists(path))
                return str + Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, path));

            return str;
        }

        public string GetCompleteHash()
        {
            string str = fh.GameOptionsHash;
            str += fh.ClientDXHash;
            str += fh.GameExeHash;
            str += fh.LauncherExeHash;
            str += fh.INIHashes;

            Logger.Log("Complete hash: " + Utilities.CalculateSHA1ForString(str));

            return Utilities.CalculateSHA1ForString(str);
        }

        private void ParseConfigFile()
        {
            IniFile config = new IniFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), CONFIGNAME));
            calculateGameExeHash = config.GetBooleanValue("Settings", "CalculateGameExeHash", true);

            List<string> keys = config.GetSectionKeys("FilenameList");
            if (keys == null || keys.Count < 1)
                return;

            List<string> filenames = new List<string>();
            foreach (string key in keys)
            {
                string value = config.GetStringValue("FilenameList", key, string.Empty);
                filenames.Add(value == string.Empty ? key : value);
            }

            fileNamesToCheck = filenames.ToArray();
        }

        private record struct FileHashes(
            string GameOptionsHash,
            string ClientDXHash,
            string INIHashes,
            // string MPMapsHash,
            string GameExeHash,
            string LauncherExeHash
            );
    }
}

﻿using System.IO;
using ClientCore;
using ClientCore.PlatformShim;
using Rampastring.Tools;

namespace Ra2Client.Domain
{
    public static class FinalSunSettings
    {
        /// <summary>
        /// Checks for the existence of the FinalSun settings file and writes it if it doesn't exist.
        /// </summary>
        public static void WriteFinalSunIni()
        {
            // The encoding of the FinalSun/FinalAlert ini file should be legacy ANSI, not Windows-1252 and also not any specific encoding.
            // Otherwise, the map editor will not work in a non-ASCII path. ANSI doesn't mean a specific codepage,
            // it means the default non-Unicode codepage which can be changed from Control Panel.
            try
            {
                string finalSunIniPath = ClientConfiguration.Instance.FinalSunIniPath;
                var finalSunIniFile = new FileInfo(Path.Combine(ProgramConstants.GamePath, finalSunIniPath));

                Logger.Log("检查地图编辑器.");
                if (finalSunIniFile.Exists)
                {
                    Logger.Log("地编配置文件存在.");

                    IniFile iniFile = new IniFile();
                    iniFile.FileName = finalSunIniFile.FullName;
                    iniFile.Encoding = EncodingExt.ANSI;
                    iniFile.Parse();

                    iniFile.SetStringValue("FinalSun", "Language", "English");
                    iniFile.SetStringValue("FinalSun", "FileSearchLikeTS", "yes");
                    iniFile.SetStringValue("TS", "Exe", SafePath.CombineDirectoryPath(ProgramConstants.GamePath));
                    iniFile.WriteIniFile();

                    return;
                }

                Logger.Log("FinalSun.ini doesn't exist - writing default settings.");

                if (!finalSunIniFile.Directory.Exists)
                    finalSunIniFile.Directory.Create();

                using var sw = new StreamWriter(finalSunIniFile.FullName, false, EncodingExt.ANSI);

                sw.WriteLine("[FinalSun]");
                sw.WriteLine("Language=English");
                sw.WriteLine("FileSearchLikeTS=yes");
                sw.WriteLine("");
                sw.WriteLine("[TS]");
                sw.WriteLine("Exe=" + SafePath.CombineDirectoryPath(ProgramConstants.GamePath));
                sw.WriteLine("");
                sw.WriteLine("[UserInterface]");
                sw.WriteLine("EasyView=0");
                sw.WriteLine("NoSounds=0");
                sw.WriteLine("DisableAutoLat=0");
                sw.WriteLine("ShowBuildingCells=0");
            }
            catch
            {
                Logger.Log("An exception occurred while checking the existence of FinalSun settings");
            }
        }
    }
}
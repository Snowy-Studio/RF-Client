using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Rampastring.Tools;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ClientCore
{
    public class ClientConfiguration
    {
        private const string GENERAL = "General";
        private const string AUDIO = "Audio";
        private const string SETTINGS = "Settings";
        private const string LINKS = "Links";

        private const string CLIENT_SETTINGS = "DTACnCNetClient.ini";
        private const string GAME_OPTIONS = "GameOptions.ini";
        private const string CLIENT_DEFS = "ClientDefinitions.ini";

        private static ClientConfiguration _instance;

        private IniFile gameOptions_ini;
        private IniFile DTACnCNetClient_ini;
        private IniFile clientDefinitionsIni;

        protected ClientConfiguration()
        {
            var baseResourceDirectory = SafePath.GetDirectory(ProgramConstants.GetBaseResourcePath());

            if (!baseResourceDirectory.Exists)
                throw new FileNotFoundException($"Couldn't find {CLIENT_DEFS} at {baseResourceDirectory} (directory doesn't exist). Please verify that you're running the client from the correct directory.");

            FileInfo clientDefinitionsFile = SafePath.GetFile(baseResourceDirectory.FullName, CLIENT_DEFS);

            if (clientDefinitionsFile is null)
                throw new FileNotFoundException($"Couldn't find {CLIENT_DEFS} at {baseResourceDirectory}. Please verify that you're running the client from the correct directory.");

            clientDefinitionsIni = new IniFile(clientDefinitionsFile.FullName);

            DTACnCNetClient_ini = new IniFile(SafePath.CombineFilePath(ProgramConstants.GetResourcePath(), CLIENT_SETTINGS));

            gameOptions_ini = new IniFile(SafePath.CombineFilePath(baseResourceDirectory.FullName, GAME_OPTIONS));
        }

        /// <summary>
        /// Singleton Pattern. Returns the object of this class.
        /// </summary>
        /// <returns>The object of the ClientConfiguration class.</returns>
        public static ClientConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ClientConfiguration();
                }
                return _instance;
            }
        }

        public void RefreshSettings()
        {
            DTACnCNetClient_ini = new IniFile(SafePath.CombineFilePath(ProgramConstants.GetResourcePath(), CLIENT_SETTINGS));
        }

        #region Client settings

        public string MainMenuMusicName => SafePath.CombineFilePath(DTACnCNetClient_ini.GetStringValue(GENERAL, "MainMenuTheme", "mainmenu"));

        public float DefaultAlphaRate => DTACnCNetClient_ini.GetSingleValue(GENERAL, "AlphaRate", 0.005f);

        public float CheckBoxAlphaRate => DTACnCNetClient_ini.GetSingleValue(GENERAL, "CheckBoxAlphaRate", 0.05f);

        public float IndicatorAlphaRate => DTACnCNetClient_ini.GetSingleValue(GENERAL, "IndicatorAlphaRate", 0.05f);

        #region Color settings

        public string UILabelColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "UILabelColor", "0,0,0");

        public string UIHintTextColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "HintTextColor", "128,128,128");

        public string DisabledButtonColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "DisabledButtonColor", "108,108,108");

        public string AltUIColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "AltUIColor", "255,255,255");

        public string ButtonHoverColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "ButtonHoverColor", "255,192,192");

        public string MapPreviewNameBackgroundColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "MapPreviewNameBackgroundColor", "0,0,0,144");

        public string MapPreviewNameBorderColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "MapPreviewNameBorderColor", "128,128,128,128");

        public string MapPreviewStartingLocationHoverRemapColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "StartingLocationHoverColor", "255,255,255,128");

        public bool MapPreviewStartingLocationUsePlayerRemapColor => DTACnCNetClient_ini.GetBooleanValue(GENERAL, "StartingLocationsUsePlayerRemapColor", false);

        public string AltUIBackgroundColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "AltUIBackgroundColor", "196,196,196");

        public string WindowBorderColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "WindowBorderColor", "128,128,128");

        public string PanelBorderColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "PanelBorderColor", "255,255,255");

        public string ListBoxHeaderColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "ListBoxHeaderColor", "255,255,255");

        public string DefaultChatColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "DefaultChatColor", "0,255,0");

        public string AdminNameColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "AdminNameColor", "255,0,0");

        public string ReceivedPMColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "PrivateMessageOtherUserColor", "196,196,196");

        public string SentPMColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "PrivateMessageColor", "128,128,128");

        public int DefaultPersonalChatColorIndex => DTACnCNetClient_ini.GetIntValue(GENERAL, "DefaultPersonalChatColorIndex", 0);

        public string ListBoxFocusColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "ListBoxFocusColor", "64,64,168");

        public string HoverOnGameColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "HoverOnGameColor", "32,32,84");

        public IniSection GetParserConstants() => DTACnCNetClient_ini.GetSection("ParserConstants");

        #endregion

        #region Tool tip settings

        public int ToolTipFontIndex => DTACnCNetClient_ini.GetIntValue(GENERAL, "ToolTipFontIndex", 0);

        public int ToolTipOffsetX => DTACnCNetClient_ini.GetIntValue(GENERAL, "ToolTipOffsetX", 0);

        public int ToolTipOffsetY => DTACnCNetClient_ini.GetIntValue(GENERAL, "ToolTipOffsetY", 0);

        public int ToolTipMargin => DTACnCNetClient_ini.GetIntValue(GENERAL, "ToolTipMargin", 4);

        public float ToolTipDelay => DTACnCNetClient_ini.GetSingleValue(GENERAL, "ToolTipDelay", 0.67f);

        public float ToolTipAlphaRatePerSecond => DTACnCNetClient_ini.GetSingleValue(GENERAL, "ToolTipAlphaRate", 4.0f);

        #endregion

        #region Audio options

        public float SoundGameLobbyJoinCooldown => DTACnCNetClient_ini.GetSingleValue(AUDIO, "SoundGameLobbyJoinCooldown", 0.25f);

        public float SoundGameLobbyLeaveCooldown => DTACnCNetClient_ini.GetSingleValue(AUDIO, "SoundGameLobbyLeaveCooldown", 0.25f);

        public float SoundMessageCooldown => DTACnCNetClient_ini.GetSingleValue(AUDIO, "SoundMessageCooldown", 0.25f);

        public float SoundPrivateMessageCooldown => DTACnCNetClient_ini.GetSingleValue(AUDIO, "SoundPrivateMessageCooldown", 0.25f);

        public float SoundGameLobbyGetReadyCooldown => DTACnCNetClient_ini.GetSingleValue(AUDIO, "SoundGameLobbyGetReadyCooldown", 5.0f);

        public float SoundGameLobbyReturnCooldown => DTACnCNetClient_ini.GetSingleValue(AUDIO, "SoundGameLobbyReturnCooldown", 1.0f);

        #endregion

        #endregion

        #region GameOptions

        public string Sides => gameOptions_ini.GetStringValue(GENERAL, nameof(Sides), "GDI,Nod,Allies,Soviet");

        public string InternalSideIndices => gameOptions_ini.GetStringValue(GENERAL, nameof(InternalSideIndices), string.Empty);

        public string SpectatorInternalSideIndex => gameOptions_ini.GetStringValue(GENERAL, nameof(SpectatorInternalSideIndex), string.Empty);

        #endregion

        #region Client definitions

        public string DiscordAppId => clientDefinitionsIni.GetStringValue(SETTINGS, "DiscordAppId", string.Empty);

        public int SendSleep => clientDefinitionsIni.GetIntValue(SETTINGS, "SendSleep", 2500);

        public int LoadingScreenCount => clientDefinitionsIni.GetIntValue(SETTINGS, "LoadingScreenCount", 2);

        public int ThemeCount => clientDefinitionsIni.GetSectionKeys("Themes").Count;

        public int LanguageCount => clientDefinitionsIni.GetSectionKeys("Language") == null ? 0 : clientDefinitionsIni.GetSectionKeys("Language").Count;

        public string LocalGame => clientDefinitionsIni.GetStringValue(SETTINGS, "LocalGame", "DTA");

        public bool SidebarHack => clientDefinitionsIni.GetBooleanValue(SETTINGS, "SidebarHack", false);

        public int MinimumRenderWidth => clientDefinitionsIni.GetIntValue(SETTINGS, "MinimumRenderWidth", 1280);

        public int MinimumRenderHeight => clientDefinitionsIni.GetIntValue(SETTINGS, "MinimumRenderHeight", 768);

        public int MaximumRenderWidth => clientDefinitionsIni.GetIntValue(SETTINGS, "MaximumRenderWidth", 1280);

        public int MaximumRenderHeight => clientDefinitionsIni.GetIntValue(SETTINGS, "MaximumRenderHeight", 768);

        public string[] RecommendedResolutions => clientDefinitionsIni.GetStringValue(SETTINGS, "RecommendedResolutions", "1280x768,2560x1440,3840x2160").Split(',');

        public string WindowTitle => clientDefinitionsIni.GetStringValue(SETTINGS, "WindowTitle", string.Empty);

        public string InstallationPathRegKey => clientDefinitionsIni.GetStringValue(SETTINGS, "RegistryInstallPath", "Reunion");

        public string CnCNetLiveStatusIdentifier => clientDefinitionsIni.GetStringValue(SETTINGS, "CnCNetLiveStatusIdentifier", "cncnet5_ts");

        public string BattleFSFileName => clientDefinitionsIni.GetStringValue(SETTINGS, "BattleFSFileName", "BattleFS.ini");

        public string MapEditorExePath => SafePath.CombineFilePath(clientDefinitionsIni.GetStringValue(SETTINGS, "MapEditorExePath", SafePath.CombineFilePath("FinalSun", "FinalSun.exe")));

        public string UnixMapEditorExePath => clientDefinitionsIni.GetStringValue(SETTINGS, "UnixMapEditorExePath", Instance.MapEditorExePath);

        public bool ModMode => clientDefinitionsIni.GetBooleanValue(SETTINGS, "ModMode", false);

        public string LongGameName => clientDefinitionsIni.GetStringValue(SETTINGS, "LongGameName", "Reunion");

        public string LongSupportURL => clientDefinitionsIni.GetStringValue(SETTINGS, "LongSupportURL", "");

        public string ShortSupportURL => clientDefinitionsIni.GetStringValue(SETTINGS, "ShortSupportURL", "");

        public string CreditsURL => clientDefinitionsIni.GetStringValue(SETTINGS, "CreditsURL", "");

        public string ManualDownloadURL => clientDefinitionsIni.GetStringValue(SETTINGS, "ManualDownloadURL", string.Empty);

        public string FinalSunIniPath => SafePath.CombineFilePath(clientDefinitionsIni.GetStringValue(SETTINGS, "FSIniPath", SafePath.CombineFilePath("FinalSun", "FinalSun.ini")));

        public string Mod_AiIniPath => SafePath.CombineFilePath(clientDefinitionsIni.GetStringValue(SETTINGS, "Mod&Ai", string.Empty));

        public int MaxNameLength => clientDefinitionsIni.GetIntValue(SETTINGS, "MaxNameLength", 12);

        public bool UseIsometricCells => clientDefinitionsIni.GetBooleanValue(SETTINGS, "UseIsometricCells", true);

        public int WaypointCoefficient => clientDefinitionsIni.GetIntValue(SETTINGS, "WaypointCoefficient", 128);

        public int MapCellSizeX => clientDefinitionsIni.GetIntValue(SETTINGS, "MapCellSizeX", 48);

        public int MapCellSizeY => clientDefinitionsIni.GetIntValue(SETTINGS, "MapCellSizeY", 24);

        public bool UseBuiltStatistic => clientDefinitionsIni.GetBooleanValue(SETTINGS, "UseBuiltStatistic", false);

        public bool CopyResolutionDependentLanguageDLL => clientDefinitionsIni.GetBooleanValue(SETTINGS, "CopyResolutionDependentLanguageDLL", true);

        public string StatisticsLogFileName => clientDefinitionsIni.GetStringValue(SETTINGS, "StatisticsLogFileName", "Debug/debug.log");

        public string[] GetLanguageInfoFromIndex(int languageIndex) => clientDefinitionsIni.GetStringValue("Language", languageIndex.ToString(), ",").Split(',');

        public string[] GetVoiceInfoFromIndex(int voiceIndex) => clientDefinitionsIni.GetStringValue("Voice", voiceIndex.ToString(), ",").Split(',');

        public string[] GetThemeInfoFromIndex(int themeIndex) => clientDefinitionsIni.GetStringValue("Themes", themeIndex.ToString(), ",").Split(',');

        /// <summary>
        /// Returns the directory path for a theme, or null if the specified
        /// theme name doesn't exist.
        /// </summary>
        /// <param name="themeName">The name of the theme.</param>
        /// <returns>The path to the theme's directory.</returns>
        public string GetThemePath(string themeName)
        {
            var themeSection = clientDefinitionsIni.GetSection("Themes");
            foreach (var key in themeSection.Keys)
            {
                string[] parts = key.Value.Split(',');
                if (parts[0] == themeName)
                    return parts[1];
            }

            return null;
        }

        public string GetVoicePath(string voiceName)
        {
            var voiceSection = clientDefinitionsIni.GetSection("Voice");
            foreach (var key in voiceSection.Keys)
            {
                string[] parts = key.Value.Split(',');
                if (parts[0] == voiceName)
                    return parts[1];
            }

            return null;
        }

        public string GetLanguagePath(string languageName)
        {
            var languageSection = clientDefinitionsIni.GetSection("Language");
            foreach (var key in languageSection.Keys)
            {
                string[] parts = key.Value.Split(',');
                if (parts[0] == languageName)
                    return parts[1];
            }

            return null;
        }

        public string SkinIniName => clientDefinitionsIni.GetStringValue(SETTINGS, "SkinFile", "Resources/Skin.ini");

        public string SettingsIniName => clientDefinitionsIni.GetStringValue(SETTINGS, "SettingsFile", "RA2MD.ini");

        public string TranslationIniName => SafePath.CombineFilePath(clientDefinitionsIni.GetStringValue(SETTINGS, "TranslationFile", SafePath.CombineFilePath("Resources", "Translation.ini")));

        public bool GenerateTranslationStub => clientDefinitionsIni.GetBooleanValue(SETTINGS, "GenerateTranslationStub", false);

        public string ExtraExeCommandLineParameters
        {
            get
            {
                return clientDefinitionsIni.GetStringValue(SETTINGS, "ExtraCommandLineParams", string.Empty);
            }
            set
            {
                clientDefinitionsIni.SetStringValue(SETTINGS, "ExtraCommandLineParams", value);
                clientDefinitionsIni.WriteIniFile(); // 根据需要，保存更改到文件
            }
        }
        public string GameModesIniPath => SafePath.CombineFilePath(clientDefinitionsIni.GetStringValue(SETTINGS, "GameModesIniPath", SafePath.CombineFilePath("Maps/Multi", "GameModes.ini")));

        public string KeyboardINI => clientDefinitionsIni.GetStringValue(SETTINGS, "KeyboardINI", "KeyboardMD.ini");

        public int MinimumIngameWidth => clientDefinitionsIni.GetIntValue(SETTINGS, "MinimumIngameWidth", 640);

        public int MinimumIngameHeight => clientDefinitionsIni.GetIntValue(SETTINGS, "MinimumIngameHeight", 480);

        public int MaximumIngameWidth => clientDefinitionsIni.GetIntValue(SETTINGS, "MaximumIngameWidth", int.MaxValue);

        public int MaximumIngameHeight => clientDefinitionsIni.GetIntValue(SETTINGS, "MaximumIngameHeight", int.MaxValue);

        public bool CopyMissionsToSpawnmapINI => clientDefinitionsIni.GetBooleanValue(SETTINGS, "CopyMissionsToSpawnmapINI", true);

        public string AllowedCustomGameModes => clientDefinitionsIni.GetStringValue(SETTINGS, "AllowedCustomGameModes", "Standard,Custom Map");

        public string SkillLevelOptions => clientDefinitionsIni.GetStringValue(SETTINGS, "SkillLevelOptions", "无,萌新,一般,高手");
        
        public int DefaultSkillLevelIndex => clientDefinitionsIni.GetIntValue(SETTINGS, "DefaultSkillLevelIndex", 0);

        public string GetGameExecutableName()
        {
            string[] exeNames = clientDefinitionsIni.GetStringValue(SETTINGS, "GameExecutableNames", "Game.exe").Split(',');

            return exeNames[0];
        }

        public string GameLauncherExecutableName => clientDefinitionsIni.GetStringValue(SETTINGS, "GameLauncherExecutableName", string.Empty);

        public bool SaveSkirmishGameOptions => clientDefinitionsIni.GetBooleanValue(SETTINGS, "SaveSkirmishGameOptions", false);

        public bool CreateSavedGamesDirectory => clientDefinitionsIni.GetBooleanValue(SETTINGS, "CreateSavedGamesDirectory", false);

        public bool DisableMultiplayerGameLoading => clientDefinitionsIni.GetBooleanValue(SETTINGS, "DisableMultiplayerGameLoading", false);

        public bool DisplayPlayerCountInTopBar => clientDefinitionsIni.GetBooleanValue(SETTINGS, "DisplayPlayerCountInTopBar", false);

        /// <summary>
        /// The name of the executable in the main game directory that selects 
        /// the correct main client executable.
        /// For example, DTA.exe in case of DTA.
        /// </summary>
        public string LauncherExe => clientDefinitionsIni.GetStringValue(SETTINGS, "LauncherExe", string.Empty);

        public bool UseClientRandomStartLocations => clientDefinitionsIni.GetBooleanValue(SETTINGS, "UseClientRandomStartLocations", false);

        /// <summary>
        /// Returns the name of the game executable file that is used on
        /// Linux and macOS.
        /// </summary>
        public string UnixGameExecutableName => clientDefinitionsIni.GetStringValue(SETTINGS, "UnixGameExecutableName", "wine-dta.sh");

        /// <summary>
        /// List of files that are not distributed but required to play.
        /// </summary>
        public string[] RequiredFiles => clientDefinitionsIni.GetStringValue(SETTINGS, "RequiredFiles", String.Empty).Split(',');

        /// <summary>
        /// List of files that can interfere with the mod functioning.
        /// </summary>
        public string[] ForbiddenFiles => clientDefinitionsIni.GetStringValue(SETTINGS, "ForbiddenFiles", String.Empty).Split(',');

        /// <summary>
        /// This tells the client which supplemental map files are ok to copy over during "spawnmap.ini" file creation.
        /// IE, if "BIN" is listed, then the client will look for and copy the file "map_a.bin"
        /// when writing the spawnmap.ini file for map file "map_a.ini".
        /// 
        /// This configuration should be in the form "SupplementalMapFileExtensions=bin,mix"
        /// </summary>
        public IEnumerable<string> SupplementalMapFileExtensions
            => clientDefinitionsIni.GetStringValue(SETTINGS, "SupplementalMapFileExtensions", null)?
                .Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                
        /// <summary>
        /// This prevents users from joining games that are incompatible/on a different game version than the current user.
        /// Default: false
        /// </summary>
        public bool DisallowJoiningIncompatibleGames => clientDefinitionsIni.GetBooleanValue(SETTINGS, nameof(DisallowJoiningIncompatibleGames), false);

#endregion

        #region Game networking defaults

        /// <summary>
        /// Default value for FrameSendRate setting written in spawn.ini.
        /// </summary>
        public int DefaultFrameSendRate => clientDefinitionsIni.GetIntValue(SETTINGS, nameof(DefaultFrameSendRate), 3);

        /// <summary>
        /// Default value for Protocol setting written in spawn.ini.
        /// </summary>
        public int DefaultProtocolVersion => clientDefinitionsIni.GetIntValue(SETTINGS, nameof(DefaultProtocolVersion), 2);

        /// <summary>
        /// Default value for MaxAhead setting written in spawn.ini.
        /// </summary>
        public int DefaultMaxAhead => clientDefinitionsIni.GetIntValue(SETTINGS, nameof(DefaultMaxAhead), 0);

        #endregion

        public OSVersion GetOperatingSystemVersion()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var version = Environment.OSVersion.Version;

                if (version.Major == 10)
                {
                    if (version.Build == 22000)
                    {
                        return OSVersion.Windows11_21H2;
                    }
                    else if (version.Build == 22621)
                    {
                        return OSVersion.Windows11_22H2;
                    }
                    else if (version.Build == 22631)
                    {
                        return OSVersion.Windows11_23H2;
                    }
                    else if (version.Build == 26100)
                    {
                        return OSVersion.Windows11_24H2_LTSC2024_SERVER2025;
                    }
                    else if (version.Build == 26120)
                    {
                        return OSVersion.Windows11_24H2_Beta;
                    }
                    else if (version.Build == 26200)
                    {
                        return OSVersion.Windows11_25H2;
                    }
                    //else if (version.Build == 27817)
                    //{
                    //    return OSVersion.Windows11_30H1;
                    //}
                    else if (version.Build == 10240)
                    {
                        return OSVersion.Windows10_1507_LTSB2015;
                    }
                    else if (version.Build == 10586)
                    {
                        return OSVersion.Windows10_1511;
                    }
                    else if (version.Build == 14393)
                    {
                        return OSVersion.Windows10_1607_LTSB2016_SERVER2016;
                    }
                    else if (version.Build == 15063)
                    {
                        return OSVersion.Windows10_1703;
                    }
                    else if (version.Build == 16299)
                    {
                        return OSVersion.Windows10_1709;
                    }
                    else if (version.Build == 17134)
                    {
                        return OSVersion.Windows10_1803;
                    }
                    else if (version.Build == 17763)
                    {
                        return OSVersion.Windows10_1809_LTSC2019_SERVER2019;
                    }
                    else if (version.Build == 18362)
                    {
                        return OSVersion.Windows10_1903;
                    }
                    else if (version.Build == 18363)
                    {
                        return OSVersion.Windows10_1909;
                    }
                    else if (version.Build == 19041)
                    {
                        return OSVersion.Windows10_2004;
                    }
                    else if (version.Build == 19042)
                    {
                        return OSVersion.Windows10_20H2;
                    }
                    else if (version.Build == 19043)
                    {
                        return OSVersion.Windows10_21H1;
                    }
                    else if (version.Build == 19044)
                    {
                        return OSVersion.Windows10_21H2_LTSC2021;
                    }
                    else if (version.Build == 19045)
                    {
                        return OSVersion.Windows10_22H2_2009;
                    }
                    else if (version.Build == 20348)
                    {
                        return OSVersion.Windows10_SERVER2022;
                    }
                    else
                    {
                        return OSVersion.Experimental_Version_Of_Windows;
                    }
                }

                if (OperatingSystem.IsWindowsVersionAtLeast(6, 3))
                    return OSVersion.Windows8_1_SERVER2012R2;
                else if (OperatingSystem.IsWindowsVersionAtLeast(6, 2))
                    return OSVersion.Windows8_SERVER2012;
                else if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
                    return OSVersion.Windows7_SERVER2008R2;
                else if (OperatingSystem.IsWindowsVersionAtLeast(6, 0))
                    return OSVersion.WindowsVista_SERVER2008;
                else if (OperatingSystem.IsWindowsVersionAtLeast(5, 2))
                    return OSVersion.WindowsXP_SERVER2003_SERVER2003R2;
                else if (OperatingSystem.IsWindowsVersionAtLeast(5, 1))
                    return OSVersion.WindowsXP;
                else if (OperatingSystem.IsWindowsVersionAtLeast(5, 0))
                    return OSVersion.Windows2000;
                else
                    return OSVersion.UNKNOWN;
            }

            return OSVersion.UNIX;
        }
    }

    /// <summary>
    /// An exception that is thrown when a client configuration file contains invalid or
    /// unexpected settings / data or required settings / data are missing.
    /// </summary>
    public class ClientConfigurationException : Exception
    {
        public ClientConfigurationException(string message) : base(message)
        {
        }
    }
}
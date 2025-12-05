using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClientCore;
using ClientGUI;
using Localization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Ra2Client.Domain;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace Ra2Client.DXGUI.Generic
{
    /// <summary>
    /// A window for loading saved singleplayer games.
    /// </summary>
    public class GameLoadingWindow : XNAWindow
    {

        public GameLoadingWindow(WindowManager windowManager, DiscordHandler discordHandler) : base(windowManager)
        {
            this.discordHandler = discordHandler;
        }

        private DiscordHandler discordHandler;

        private XNAMultiColumnListBox lbSaveGameList;
        private XNAClientButton btnLaunch;
        private XNAClientButton btnDelete;
        private XNAClientButton btnCancel;

        private List<SavedGame> savedGames = new List<SavedGame>();

        public override void Initialize()
        {
            Name = "GameLoadingWindow";
            BackgroundTexture = AssetLoader.LoadTexture("loadmissionbg.png");

            ClientRectangle = new Rectangle(0, 0, 600, 380);
            CenterOnParent();

            lbSaveGameList = new XNAMultiColumnListBox(WindowManager);
            lbSaveGameList.Name = nameof(lbSaveGameList);
            lbSaveGameList.ClientRectangle = new Rectangle(13, 13, 574, 317);
            lbSaveGameList.AddColumn("SAVED GAME NAME".L10N("UI:Main:SavedGameNameColumnHeader"), 380);
            lbSaveGameList.AddColumn("DATE / TIME".L10N("UI:Main:SavedGameDateTimeColumnHeader"), 194);
            lbSaveGameList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbSaveGameList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbSaveGameList.SelectedIndexChanged += ListBox_SelectedIndexChanged;
            lbSaveGameList.AllowKeyboardInput = true;

            btnLaunch = new XNAClientButton(WindowManager);
            btnLaunch.Name = nameof(btnLaunch);
            btnLaunch.ClientRectangle = new Rectangle(125, 345, 110, 23);
            btnLaunch.Text = "ReLoad".L10N("UI:Main:ButtonLoad");
            btnLaunch.AllowClick = false;
            btnLaunch.LeftClick += BtnLaunch_LeftClick;

            btnDelete = new XNAClientButton(WindowManager);
            btnDelete.Name = nameof(btnDelete);
            btnDelete.ClientRectangle = new Rectangle(btnLaunch.Right + 10, btnLaunch.Y, 110, 23);
            btnDelete.Text = "Delete".L10N("UI:Main:ButtonDelete");
            btnDelete.AllowClick = false;
            btnDelete.LeftClick += BtnDelete_LeftClick;

            btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = nameof(btnCancel);
            btnCancel.ClientRectangle = new Rectangle(btnDelete.Right + 10, btnLaunch.Y, 110, 23);
            btnCancel.Text = "Cancel".L10N("UI:Main:ButtonCancel");
            btnCancel.LeftClick += BtnCancel_LeftClick;

            Keyboard.OnKeyPressed += Keyboard_OnKeyPressed;

            AddChild(lbSaveGameList);
            AddChild(btnLaunch);
            AddChild(btnDelete);
            AddChild(btnCancel);

            base.Initialize();

            ListSaves();
        }

        private void Keyboard_OnKeyPressed(object sender, Rampastring.XNAUI.Input.KeyPressEventArgs e)
        {
            if (Enabled)
            {
                if (e.PressedKey == Keys.Escape)
                {
                    btnCancel.OnLeftClick();
                }
                if (e.PressedKey == Keys.Enter)
                {
                    btnLaunch.OnLeftClick();
                }
            }
        }

        private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbSaveGameList.SelectedIndex == -1)
            {
                btnLaunch.AllowClick = false;
                btnDelete.AllowClick = false;
            }
            else
            {
                btnLaunch.AllowClick = true;
                btnDelete.AllowClick = true;
            }
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Enabled = false;
        }

        private void BtnLaunch_LeftClick(object sender, EventArgs e)
        {
            SavedGame sg = savedGames[lbSaveGameList.SelectedIndex];
            Logger.Log("Loading saved game " + sg.FileName);

            var newGame = sg.Game;
            var newMission = sg.Mission;
            var ares = sg.Ares;
            var phobos = sg.Phobos;
            var tx = sg.Tx;
            var buildOffAlly = sg.BuildOffAlly;

            if ((newGame != string.Empty &&!Directory.Exists(newGame)) || (newMission != string.Empty && !Directory.Exists(newMission)))
            {
                XNAMessageBox.Show(WindowManager,"Error".L10N("UI:Main:Error"), "The mission pack file or mod file has been deleted and the save cannot be loaded.".L10N("UI:Main:CannotLoadedSave"));
                return;
            }

            var spawnIni = new IniFile();

            var settings = new IniSection("Settings");

            //写入新游戏
            settings.SetValue("Game", newGame);

            settings.SetValue("Mission", newMission);
            settings.SetValue("chkAres", ares);
            settings.SetValue("chkPhobos", phobos);
            settings.SetValue("chkTerrain", tx);
            settings.SetValue("OtherFile", newMission);
            settings.SetValue("BuildOffAlly", buildOffAlly);

            settings.SetValue("Ra2Mode",false);
            settings.SetValue("chkSatellite",sg.透明迷雾);
            if(sg.战役ID != -1)
                settings.SetValue("CampaignID", sg.战役ID);
            settings.SetValue("Scenario", "spawnmap.ini");
            settings.SetValue("SaveGameName", sg.FileName);
            settings.SetValue("LoadSaveGame","Yes");
            settings.SetValue("SidebarHack", ClientConfiguration.Instance.SidebarHack);
            settings.SetValue("CustomLoadScreen", LoadingScreenController.GetLoadScreenName("g"));
            settings.SetValue("Firestorm","No");
            settings.SetValue("GameSpeed", UserINISettings.Instance.GameSpeed.Value);
            
            spawnIni.AddSection(settings);
            

            FileInfo spawnMapIniFile = SafePath.GetFile(ProgramConstants.GamePath, "spawnmap.ini");

            if (spawnMapIniFile.Exists)
                spawnMapIniFile.Delete();

            using StreamWriter spawnMapStreamWriter = new StreamWriter(spawnMapIniFile.FullName);
            spawnMapStreamWriter.WriteLine("[Map]");
            spawnMapStreamWriter.WriteLine("Size=0,0,50,50");
            spawnMapStreamWriter.WriteLine("LocalSize=0,0,50,50");
            spawnMapStreamWriter.WriteLine();

            discordHandler.UpdatePresence(sg.GUIName, true);

            Enabled = false;
            GameProcessLogic.GameProcessExited += GameProcessExited_Callback;

            try
            {
                var savFiles = Directory.GetFiles(ProgramConstants.存档目录, "*.sav");
                foreach (var file in savFiles)
                    File.Delete(file);
                File.Copy(sg.FilePath, Path.Combine(ProgramConstants.存档目录, sg.FileName), true);
            }
            catch(Exception ex)
            {
                XNAMessageBox.Show(WindowManager, "唤起游戏失败", ex.ToString());
            }

            GameProcessLogic.StartGameProcess(WindowManager, spawnIni);
        }


        private void BtnDelete_LeftClick(object sender, EventArgs e)
        {
            

            SavedGame sg = savedGames[lbSaveGameList.SelectedIndex];
            var msgBox = new XNAMessageBox(WindowManager, "Delete confirmation".L10N("UI:Main:DeleteConfirmationTitle"),
                string.Format(
                    "Are you sure you want to proceed?".L10N("UI:Main:DeleteConfirmationText"),
                    sg.FileName, Renderer.GetSafeString(sg.GUIName, lbSaveGameList.FontIndex), sg.LastModified.ToString()),
                XNAMessageBoxButtons.YesNo);
            msgBox.Show();
            msgBox.YesClickedAction = DeleteMsgBox_YesClicked;
        }

        private void DeleteMsgBox_YesClicked(XNAMessageBox obj)
        {
            SavedGame sg = savedGames[lbSaveGameList.SelectedIndex];

            Logger.Log("删除存档文件" + sg.FilePath);
            SafePath.DeleteFileIfExists(sg.FilePath);
            var saveIni = new IniFile(Path.Combine(ProgramConstants.存档目录, "Save.ini"));
            saveIni.RemoveSection($"{sg.FileName}-{Path.GetFileName(Path.GetDirectoryName(sg.FilePath))}");
            saveIni.WriteIniFile();
            ListSaves();
        }

        private void GameProcessExited_Callback()
        {
            WindowManager.AddCallback(new Action(GameProcessExited), null);
        }

        protected virtual void GameProcessExited()
        {
            GameProcessLogic.GameProcessExited -= GameProcessExited_Callback;
            discordHandler.UpdatePresence();
        }

        public void ListSaves()
        {
            savedGames.Clear();
         
            lbSaveGameList.ClearItems();
            lbSaveGameList.SelectedIndex = -1;
            if (!Directory.Exists(ProgramConstants.存档目录)) return;

            var saveIni = new IniFile(Path.Combine(ProgramConstants.存档目录, "Save.ini"));

            foreach (var d in Directory.GetDirectories(ProgramConstants.存档目录))
            {
                DirectoryInfo savedGamesDirectoryInfo = SafePath.GetDirectory(d);

                if (!savedGamesDirectoryInfo.Exists)
                {
                    Logger.Log("Saved Games directory not found!");
                    return;
                }

                IEnumerable<FileInfo> files = savedGamesDirectoryInfo.EnumerateFiles("*.SAV", SearchOption.TopDirectoryOnly);

                

                foreach (FileInfo file in files)
                {
                    var sectionName = $"{Path.GetFileName(file.FullName)}-{Path.GetFileName(d)}";
                    var game = saveIni.GetValue(sectionName, "Game", string.Empty);
                    var mission = saveIni.GetValue(sectionName, "Mission", string.Empty);
                    if(mission == string.Empty)
                        mission = saveIni.GetValue(sectionName, "OtherFile", string.Empty);
                    var 透明迷雾 = saveIni.GetValue(sectionName, "chkSatellite", false);
                    var 战役ID = saveIni.GetValue(sectionName, "CampaignID", -1);
                    var tx = saveIni.GetValue(sectionName, "chkTerrain", false);
                    var ares = saveIni.GetValue(sectionName, "chkAres", false);
                    var phobos = saveIni.GetValue(sectionName, "chkPhobos", false);
                    var buildOffAlly = saveIni.GetValue(sectionName, "BuildOffAlly", false);


                    ParseSaveGame(file.FullName, game, mission, 透明迷雾, 战役ID, ares, phobos, tx, buildOffAlly);
                }
            }

            savedGames = savedGames.OrderBy(sg => sg.LastModified.Ticks).ToList();
            savedGames.Reverse();
        

            foreach (SavedGame sg in savedGames)
            {
                string[] item = [
                    Renderer.GetSafeString(sg.GUIName, lbSaveGameList.FontIndex),sg.LastModified.ToString().Replace(" ","-") ];
              //  Console.WriteLine(sg.LastModified.ToString());
                lbSaveGameList.AddItem(item, true);
            }
        }

        private void ParseSaveGame(string fileName, string game, string mission, bool 透明迷雾, int 战役ID,bool ares,bool phobos,bool tx,bool buildOffAlly)
        {
            string shortName = Path.GetFileName(fileName);
            SavedGame sg = new SavedGame(shortName, game, mission, ares,phobos,tx, buildOffAlly);
            sg.FilePath = fileName;
            sg.透明迷雾 = 透明迷雾;
            sg.战役ID = 战役ID;
            if (sg.ParseInfo())
                savedGames.Add(sg);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using ClientCore;
using ClientGUI;
using Ra2Client.Domain;
using Ra2Client.Online;
using Ra2Client.DXGUI.Multiplayer.GameLobby;
using Localization;
using Localization.Tools;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using ClientCore.Settings;
using DTAConfig.Entity;
using DTAConfig.OptionPanels;
using Mission = DTAConfig.Entity.Mission;
using DTAConfig;
using DTAConfig.Settings;
using System.Reflection;
using System.Windows.Forms;
using System.Threading;
using System.Text;
using System.Diagnostics;


namespace Ra2Client.DXGUI.Generic
{
    public class CampaignSelector(WindowManager windowManager, DiscordHandler discordHandler) : INItializableXNAWindow(windowManager)
    {
        private XNAClientCheckBox chkTerrain; // 地形扩展选项       ——dfyh

        private const int DefaultWidth = 650;
        private const int DefaultHeight = 600;

        private static readonly string[] DifficultyNames = ["Easy", "Medium", "Hard"];

        private static readonly string[] DifficultyIniPaths =
        {
            "INI/MapCode/Difficulty Easy.ini",
            "INI/MapCode/Difficulty Medium.ini", 
            "INI/MapCode/Difficulty Hard.ini"
        };
        private readonly DiscordHandler _discordHandler = discordHandler;

        private readonly List<Mission> _missions = [];
        private readonly List<Mission> _screenMissions = [];
        private XNAListBox _lbxCampaignList;
        private XNADropDown _ddDifficulty;
        private XNADropDown _ddSide;
        private XNADropDown _ddMissionPack;
        private GameLobbyDropDown _cmbCredits;
        private XNAClientButton _btnLaunch;
        private XNAListBox _tbMissionDescriptionList;
        private XNATrackbar _trbDifficultySelector;

        public readonly List<GameLobbyCheckBox> CheckBoxes = [];
        public readonly List<GameLobbyDropDown> DropDowns = [];

        private XNAButton _mapPreviewBox;

        public Rectangle MapPreviewBoxPosition { get; private set; }

        private Rectangle MapPreviewBoxAspectPosition { get; set; }

        private GameLobbyDropDown _cmbGame;
        private GameLobbyDropDown _cmbGameSpeed;
        private XNAListBox _lbxInforBox;
        private XNAPanel _gameOptionsPanel;
        private XNAClientRatingBox _ratingBox;
        private XNAClientButton _btnRatingDone;
        private XNALabel _lblRatingResult;
        private ModManager _modManager;
        private CheaterWindow _cheaterWindow;

        private List<string> _difficultyList = [];
        private List<string> _sideList = [];

        private const string SETTINGS_PATH = "Client/CampaignSetting.ini";

        private readonly string[] _filesToCheck =
        {
            "INI/AI.ini",
            "INI/AIE.ini",
            "INI/Art.ini",
            "INI/ArtE.ini",
            "INI/Enhance.ini",
            "INI/Rules.ini",
            "INI/MapCode/Difficulty Hard.ini",
            "INI/MapCode/Difficulty Medium.ini",
            "INI/MapCode/Difficulty Easy.ini"
        };

        private Mission _missionToLaunch;
        private XNAContextMenu _campaignMenu; //战役列表右击菜单
        private XNAContextMenu _modMenu; //mod选择器右击菜单
        //private XNAContextMenuItem toggleFavoriteItem;

        //private EventArgs ReLoad;

        //打分参数
        private int _scoreLevel = -1;

        private int count = 0;

        public event EventHandler Exited;

        public override void Initialize()
        {
            if (!Directory.Exists(Path.Combine(ProgramConstants.存档目录)))
                Directory.CreateDirectory(Path.Combine(ProgramConstants.存档目录));

                BackgroundTexture = AssetLoader.LoadTexture("missionselectorbg.png");
            ClientRectangle = new Rectangle(0, 0, DefaultWidth, DefaultHeight);
            BorderColor = UISettings.ActiveSettings.PanelBorderColor;

            Name = "CampaignSelector";

            var lblSelectCampaign = new XNALabel(WindowManager);
            lblSelectCampaign.Name = "lblSelectCampaign";
            lblSelectCampaign.FontIndex = 1;
            lblSelectCampaign.ClientRectangle = new Rectangle(12, 12, 0, 0);
            lblSelectCampaign.Text = "MISSIONS:".L10N("UI:Main:Missions");

            _lbxCampaignList = new XNAListBox(WindowManager);
            _lbxCampaignList.Name = "lbCampaignList";
            _lbxCampaignList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2);
            _lbxCampaignList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            _lbxCampaignList.ClientRectangle = new Rectangle(12, lblSelectCampaign.Bottom + 36, 300, 480);
            _lbxCampaignList.LineHeight = 20;
            _lbxCampaignList.SelectedIndexChanged += LbxCampaignListSelectedIndexChanged;
            _lbxCampaignList.RightClick += LbxCampaignListRightClick;

            _modManager = ModManager.GetInstance(WindowManager);
            _modManager.触发刷新 += ReadMissionList;
            //modManager.EnabledChanged += CampaignSelector_EnabledChanged;

            var btnImport = new XNAClientButton(WindowManager)
            {
                Text = "Import Mission Packs".L10N("UI:Main:ImportMissionPacks"),
                ClientRectangle = new Rectangle(10, 32, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT)
            }; 
            btnImport.LeftClick += BtnImport_LeftClick;

            var btnDownLoad = new XNAClientButton(WindowManager)
            {
                Text = "下载房主传输的地图 Mission Packs".L10N("UI:Main:DownloadMissionPacks"),
                ClientRectangle = new Rectangle(btnImport.Right + 10, 32, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT)
            };
            btnDownLoad.LeftClick += BtnDownLoad_LeftClick;

            var _lblScreen = new XNALabel(WindowManager);
            _lblScreen.Name = "lblScreen";
            _lblScreen.Text = "Filter:".L10N("UI:Main:Filter");
            _lblScreen.ClientRectangle = new Rectangle(10, 60, 0, 0);

            _ddDifficulty = new XNADropDown(WindowManager);
            _ddDifficulty.Name = nameof(_ddDifficulty);
            _ddDifficulty.ClientRectangle = new Rectangle(50, 60, 100, 25);

            _ddSide = new XNADropDown(WindowManager);
            _ddSide.Name = nameof(_ddSide);
            _ddSide.ClientRectangle = new Rectangle(_ddDifficulty.X + _ddDifficulty.Width + 5, _ddDifficulty.Y, _ddDifficulty.Width, _ddDifficulty.Height);

            _ddMissionPack = new XNADropDown(WindowManager);
            _ddMissionPack.Name = nameof(_ddMissionPack);
            _ddMissionPack.ClientRectangle = new Rectangle(_lblScreen.X, _ddDifficulty.Y + _ddDifficulty.Height + 5, _lbxCampaignList.Width, _ddDifficulty.Height);


            var lblMissionDescriptionHeader = new XNALabel(WindowManager);
            lblMissionDescriptionHeader.Name = "lblMissionDescriptionHeader";
            lblMissionDescriptionHeader.FontIndex = 1;
            lblMissionDescriptionHeader.ClientRectangle = new Rectangle(
                _lbxCampaignList.Right + 12,
                lblSelectCampaign.Y, 0, 0);
            lblMissionDescriptionHeader.Text = "MISSION DESCRIPTION:".L10N("UI:Main:MissionDescription");

            _tbMissionDescriptionList = new XNAListBox(WindowManager);
            _tbMissionDescriptionList.Name = "tbMissionDescription";
            _tbMissionDescriptionList.ClientRectangle = new Rectangle(
                lblMissionDescriptionHeader.X,
                lblMissionDescriptionHeader.Bottom + 6,
                600, 300);
            _tbMissionDescriptionList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            _tbMissionDescriptionList.Alpha = 1.0f;
            _tbMissionDescriptionList.FontIndex = 1;
            _tbMissionDescriptionList.LineHeight = 20;
            _tbMissionDescriptionList.BackgroundTexture = AssetLoader.CreateTexture(AssetLoader.GetColorFromString(ClientConfiguration.Instance.AltUIBackgroundColor),
                _tbMissionDescriptionList.Width, _tbMissionDescriptionList.Height);

         
            var lblDifficultyLevel = new XNALabel(WindowManager);
            lblDifficultyLevel.Name = "lblDifficultyLevel";
            lblDifficultyLevel.Text = "DIFFICULTY LEVEL".L10N("UI:Main:DifficultyLevel");
            lblDifficultyLevel.FontIndex = 1;
            Vector2 textSize = Renderer.GetTextDimensions(lblDifficultyLevel.Text, lblDifficultyLevel.FontIndex);
            lblDifficultyLevel.ClientRectangle = new Rectangle(
                _tbMissionDescriptionList.X + (_tbMissionDescriptionList.Width - (int)textSize.X),
                _tbMissionDescriptionList.Bottom, (int)textSize.X, (int)textSize.Y);

            _trbDifficultySelector = new XNATrackbar(WindowManager);
            _trbDifficultySelector.Name = "trbDifficultySelector";
            _trbDifficultySelector.ClientRectangle = new Rectangle(
                _tbMissionDescriptionList.X, lblDifficultyLevel.Bottom + 6,
                _tbMissionDescriptionList.Width - 255, 35);
            _trbDifficultySelector.MinValue = 0;
            _trbDifficultySelector.MaxValue = 2;
            _trbDifficultySelector.BackgroundTexture = AssetLoader.CreateTexture(
                new Color(0, 0, 0, 128), 2, 2);
            _trbDifficultySelector.ButtonTexture = AssetLoader.LoadTextureUncached(
                "trackbarButton_difficulty.png");

         

            _campaignMenu = new XNAContextMenu(WindowManager);
            _campaignMenu.Name = nameof(_campaignMenu);
            _campaignMenu.Width = 100;
            //_campaignMenu.AddItem("删除这组任务");
            _campaignMenu.AddItem(new XNAContextMenuItem
            {
                Text = "刷新任务列表".L10N("UI:Main:RefreshMapList"),
                SelectAction = () => ReadMissionList()
            });
            _campaignMenu.AddItem(new XNAContextMenuItem
            {
                Text = "Delete Mission Packs".L10N("UI:Main:DeleteMissionPacks"),
                SelectAction = DelConf
            });
            _campaignMenu.AddItem(new XNAContextMenuItem
            {
                Text = "Import Mission Packs".L10N("UI:Main:ImportMissionPacks"),
                SelectAction = btnImport.OnLeftClick
            });
            _campaignMenu.AddItem(new XNAContextMenuItem
            {
                Text = "Mission Packs Management".L10N("UI:Main:MissionPacksManagement"),
                SelectAction = () => ModManagerEnabled(1)
            });
            

            AddChild(_campaignMenu);

            _modMenu = new XNAContextMenu(WindowManager);
            _modMenu.Name = nameof(_modMenu);
            _modMenu.Width = 100;

            _modMenu.AddItem(new XNAContextMenuItem
            {
                Text = "模组管理器",
                SelectAction = () => ModManagerEnabled(0)
            });
            _modMenu.AddItem(new XNAContextMenuItem
            {
                Text = "导入Mod",
                SelectAction = () =>
                {
                    var infoWindows = new 导入选择窗口(WindowManager);

                    infoWindows.selected += (b1, b2, path) =>
                    {
                        var modID = _modManager.导入Mod(b1, b2, path);
                        var index = _cmbGame.Items.FindIndex(item => ((Mod)(item.Tag)).ID == modID);
                        if (index > -1) _cmbGame.SelectedIndex = index;
                    };

                    var dp = DarkeningPanel.AddAndInitializeWithControl(WindowManager, infoWindows);
                }
            });
            AddChild(_modMenu);


            var lblEasy = new XNALabel(WindowManager);
            lblEasy.Name = "lblEasy";
            lblEasy.FontIndex = 1;
            lblEasy.Text = "EASY".L10N("UI:Main:DifficultyEasy");
            lblEasy.ClientRectangle = new Rectangle(_trbDifficultySelector.X,
                _trbDifficultySelector.Bottom + 20, 1, 1);

            var lblNormal = new XNALabel(WindowManager);
            lblNormal.Name = "lblNormal";
            lblNormal.FontIndex = 1;
            lblNormal.Text = "NORMAL".L10N("UI:Main:DifficultyNormal");
            textSize = Renderer.GetTextDimensions(lblNormal.Text, lblNormal.FontIndex);
            lblNormal.ClientRectangle = new Rectangle(
                _tbMissionDescriptionList.X + (_tbMissionDescriptionList.Width - (int)textSize.X) / 2,
                lblEasy.Y, (int)textSize.X, (int)textSize.Y);

            var lblHard = new XNALabel(WindowManager);
            lblHard.Name = "lblHard";
            lblHard.FontIndex = 1;
            lblHard.Text = "HARD".L10N("UI:Main:DifficultyHard");
            lblHard.ClientRectangle = new Rectangle(
                _tbMissionDescriptionList.Right - lblHard.Width,
                lblEasy.Y, 1, 1);

            _btnLaunch = new XNAClientButton(WindowManager);
            _btnLaunch.Name = "btnLaunch";
            _btnLaunch.ClientRectangle = new Rectangle(12, Height - 35, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            _btnLaunch.Text = "Launch".L10N("UI:Main:ButtonLaunch");
            _btnLaunch.AllowClick = false;
            _btnLaunch.LeftClick += BtnLaunch_LeftClick;

            var btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = "btnCancel";
            btnCancel.ClientRectangle = new Rectangle(Width - 145, _btnLaunch.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnCancel.Text = "Cancel".L10N("UI:Main:ButtonCancel");
            btnCancel.LeftClick += BtnCancel_LeftClick;

            _ratingBox = new XNAClientRatingBox(WindowManager);
            _ratingBox.Name = nameof(_ratingBox);
            _ratingBox.Text = "Score".L10N("UI:Main:Score");
            _ratingBox.ClientRectangle = new Rectangle(_lbxCampaignList.X, _lbxCampaignList.Y + _lbxCampaignList.Height + 150, 0, 0);
            _ratingBox.CheckedChanged += RatingBox_CheckedChanged;
            _ratingBox.Visible = false;
            AddChild(_ratingBox);

            _btnRatingDone = new XNAClientButton(WindowManager);
            _btnRatingDone.Name = nameof(_btnRatingDone);
            _btnRatingDone.Text = "Rating".L10N("UI:Main:Rating");
            _btnRatingDone.ClientRectangle = new Rectangle(_ratingBox.Right + 20, _ratingBox.Y, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
            _btnRatingDone.LeftClick += BtnRatingDone_LeftClick;
            _btnRatingDone.Visible = false;
            AddChild(_btnRatingDone);

            _lblRatingResult = new XNALabel(WindowManager);
            _lblRatingResult.Name = nameof(_lblRatingResult);
            _lblRatingResult.ClientRectangle = new Rectangle(_ratingBox.X, _ratingBox.Bottom + 10, 100, 25);
            _lblRatingResult.Visible = false;
            AddChild(_lblRatingResult);

            _lbxInforBox = new XNAListBox(WindowManager);

            var lblalter = new XNALabel(WindowManager);
            lblalter.Text = "The mission package comes with a description file that can be opened by double-clicking".L10N("UI:Main:OpenMissionPackageDescription");

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 延迟时间; // 延迟 500ms
            timer.Tick += Timer_Tick;

            AddChild(lblSelectCampaign);
            AddChild(lblMissionDescriptionHeader);
            AddChild(_lbxCampaignList);
            AddChild(_lblScreen);
            AddChild(_ddDifficulty);
            AddChild(_ddSide);
            AddChild(_lbxInforBox);
            AddChild(_ddMissionPack);
            AddChild(lblalter);
            AddChild(btnImport);
            AddChild(btnDownLoad);
            AddChild(_tbMissionDescriptionList);
            AddChild(lblDifficultyLevel);
            AddChild(_btnLaunch);
            AddChild(btnCancel);
            AddChild(_trbDifficultySelector);
            AddChild(lblEasy);
            AddChild(lblNormal);
            AddChild(lblHard);
            base.Initialize();


            _ddSide.SelectedIndexChanged += DDDifficultySelectedIndexChanged;
            _ddDifficulty.SelectedIndexChanged += DDDifficultySelectedIndexChanged;
            _ddMissionPack.SelectedIndexChanged += DDDifficultySelectedIndexChanged;

            ReadMissionList();

            UserINISettings.Instance.重新加载地图和任务包 += (_,_) => ReadMissionList();

            //ReadDrop();


            _lblGame = new XNALabel(WindowManager)
            {
                Text = "Use mod:".L10N("UI:Main:UseMod"),
                ClientRectangle = new Rectangle(_tbMissionDescriptionList.X + _tbMissionDescriptionList.Width + 10, _tbMissionDescriptionList.Y, 0,0)
            };

            lblModify = new XNALabel(WindowManager);
            lblModify.Name = nameof(lblModify);
            lblModify.Text = "Note: Changes may not take effect, and some may disrupt the flow of the mission".L10N("UI:Main:TurnOnCheat");
            lblModify.ClientRectangle = new Rectangle(_lblGame.X, _tbMissionDescriptionList.Y + 40, 0, 0);
            AddChild(lblModify);

            _cmbGame = new GameLobbyDropDown(WindowManager) {
                ClientRectangle = new Rectangle(_lblGame.Right + 75, _tbMissionDescriptionList.Y, 250, 23)
            };
            // lbCampaignList.SelectedIndex = 1;
            //    LbxCampaignListSelectedIndexChanged(lbCampaignList, new EventArgs());
            _cmbGame.SelectedIndexChanged += CmbGame_SelectedChanged;

            AddChild(_lblGame);
            AddChild(_cmbGame);

            _cmbGame.RightClick += (_, _) => _modMenu.Open(GetCursorPoint());
            _gameOptionsPanel = FindChild<XNAPanel>("GameOptionsPanel");
            _gameOptionsPanel.Visible = false;
            _mapPreviewBox = FindChild<XNAButton>("mapPreviewBox");
            MapPreviewBoxPosition = _mapPreviewBox.ClientRectangle;
            _mapPreviewBox.LeftClick += MapPreviewBox_LeftClick;
            _cmbGameSpeed = FindChild<GameLobbyDropDown>("cmbGameSpeed");

            _cmbCredits = FindChild<GameLobbyDropDown>("cmbCredits");

            chkTerrain = new XNAClientCheckBox(WindowManager);  // 地形扩展选项       ——dfyh
            chkTerrain.Text = "扩展地形".L10N("UI:Main:chkTerrain");
            chkTerrain.X = FindChild<XNAClientCheckBox>("chkSatellite").X;
            chkTerrain.Y = FindChild<XNAClientCheckBox>("chkCorr").Y + 25;
           
            chkTerrain.SetToolTipText("启用TX扩展地形，如遇到空气墙或闪退可尝试关闭此选项后重试.\nThis option must be enabled for some map campaigns.".L10N("UI:Main:TPchkTerrain"));
            _gameOptionsPanel.AddChild(chkTerrain);     // 添加地形扩展选项到游戏选项面板      ——dfyh

            _lbxInforBox.ClientRectangle = new Rectangle(_gameOptionsPanel.X, _mapPreviewBox.Y + 25, 345, _mapPreviewBox.Height - 185);
            _lbxInforBox.FontIndex = 1;
            _lbxInforBox.LineHeight = 25;
            _lbxInforBox.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            _lbxInforBox.DoubleLeftClick += _lbxInforBox_DoubleLeftClick;

            lblalter.ClientRectangle = new Rectangle(_gameOptionsPanel.X, _mapPreviewBox.Y, 0, 0);

            // Center on screen
            CenterOnParent();

            _trbDifficultySelector.Value = UserINISettings.Instance.Difficulty;

            _cheaterWindow = new CheaterWindow(WindowManager);
            var dp = new DarkeningPanel(WindowManager);
            dp.AddChild(_cheaterWindow);
            AddChild(dp);
            dp.CenterOnParent();
            _cheaterWindow.CenterOnParent();
            _cheaterWindow.YesClicked += CheaterWindow_YesClicked;
            _cheaterWindow.Disable();

            DropDowns.Add(_cmbGame);

            LoadSettings();

            RemoveChild(_mapPreviewBox);
            AddChild(_mapPreviewBox);

            _ddDifficulty.ClickSoundEffect = _cmbCredits.ClickSoundEffect;
            _ddSide.ClickSoundEffect = _cmbCredits.ClickSoundEffect;
            _ddMissionPack.ClickSoundEffect = _cmbCredits.ClickSoundEffect;

            chkTerrain.Visible = false;
            chkTerrain.Checked = false;
        }

        private void _lbxInforBox_DoubleLeftClick(object sender, EventArgs e)
        {
            if (_lbxInforBox.SelectedIndex == -1) return;

            var path = _lbxInforBox.SelectedItem.Tag as string;

            if (File.Exists(path))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
                catch(Exception ex)
                {
                    XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), ex.ToString());
                }
            }
            else
                XNAMessageBox.Show(WindowManager, "Info".L10N("UI:Main:Info"), $"文件 {path} 不存在!".L10N("UI:Main:FileDoesNotExist"));

        }

        private void BtnDownLoad_LeftClick(object sender, EventArgs e)
        {
            //var _modManager = ModManager.GetInstance(WindowManager);
            //_modManager.打开创意工坊(2);
            FunExtensions.OpenUrl($"https://creator.yra2.com/workshop/missionPack/list?token={UserINISettings.Instance.Token}&port={LocalHttpServer.Port}");
        }

        private void BtnImport_LeftClick(object sender, EventArgs e)
        {
            var infoWindows = new 导入选择窗口(WindowManager);

            infoWindows.selected += (b1, b2, path) =>
            {
                var missionPackID = _modManager.导入任务包(b1, b2, path);
                var index = _lbxCampaignList.Items.FindIndex(item => item.Tag as string == missionPackID);
                if (index > -1) { 
                    _lbxCampaignList.SelectedIndex = index;
                    _lbxCampaignList.TopIndex = index;
                }
            };

            var dp = DarkeningPanel.AddAndInitializeWithControl(WindowManager, infoWindows);
        }

        private void MapPreviewBox_LeftClick(object sender, EventArgs e)
        {

            if (count % 2 == 0)
            {
                // 计算缩放后的大小
                int maxWidth = 1280;
                int maxHeight = 768;

                // 计算缩放比例
                float scaleX = (float)maxWidth / MapPreviewBoxAspectPosition.Width;
                float scaleY = (float)maxHeight / MapPreviewBoxAspectPosition.Height;
                float scale = Math.Min(scaleX, scaleY);

                // 计算缩放后的位置和大小
                int width = (int)(MapPreviewBoxAspectPosition.Width * scale);
                int height = (int)(MapPreviewBoxAspectPosition.Height * scale);
                int x = (maxWidth - width) / 2;
                int y = (maxHeight - height) / 2;

                // 设置预览框的位置和大小
                _mapPreviewBox.ClientRectangle = new Rectangle(x, y, width, height);
            }
            else
            {
                // 恢复到设计时的位置和大小
                _mapPreviewBox.ClientRectangle = MapPreviewBoxAspectPosition;
            }

            count++;

            base.OnLeftClick();

        }

        private void BtnRatingDone_LeftClick(object sender, EventArgs e)
        {
            if (-1 == _scoreLevel)
            {
                XNAMessageBox.Show(WindowManager, "Info".L10N("UI:Main:Info"), "You haven't scored it yet!".L10N("UI:Main:NotScored"));
                return;
            }

            string missionName = _screenMissions[_lbxCampaignList.SelectedIndex].SectionName;
            var missionPack = _screenMissions[_lbxCampaignList.SelectedIndex].MPack.Name;
            var brief = _screenMissions[_lbxCampaignList.SelectedIndex].GUIName;
            var ini = new IniFile(ProgramConstants.GamePath + SETTINGS_PATH);
            if (!ini.SectionExists(missionName))
                ini.AddSection(missionName);

            int mark = ini.GetValue(missionName, "Mark", -1);
            if (-1 != mark)
                XNAMessageBox.Show(WindowManager, "Info".L10N("UI:Main:Info"), "You've already scored this mission!".L10N("UI:Main:Scored"));
            else
            {
                _ = Task.Run(async () =>
                 {
                     await UploadScore(missionName, missionPack, brief, _scoreLevel);

                     ini.SetValue(missionName, "Mark", _scoreLevel);
                     ini.WriteIniFile();

                    _ = updateMark(missionName);
                 });

            }
        }

        private void Credits(IniFile iniFile, int money)
        {
            string player = iniFile.GetStringValue("Basic", "Player", string.Empty);

            if (!string.IsNullOrEmpty(player)) {
                iniFile.SetIntValue(player, "Credits", money);
            }

        }

        
        private void CmbGame_SelectedChanged(object sender, EventArgs e)
        {
            if (_cmbGame.SelectedItem == null || _cmbGame.SelectedItem == null)
                return;

            if (_lbxCampaignList.SelectedIndex == -1 || _lbxCampaignList.SelectedIndex >= _screenMissions.Count) return;


            base.OnSelectedChanged();

        }

        // IniFile infoini = null;

        private void 显示任务包TxT文件列表(string mpPath)
        {

            _lbxInforBox.Clear();
            if(Directory.Exists(mpPath))
            foreach (var txt in Directory.GetFiles(mpPath, "*.txt"))
            {
                    _lbxInforBox.AddItem(new XNAListBoxItem()
                    {
                        Text = Path.GetFileNameWithoutExtension(txt),
                        Tag = Path.Combine(ProgramConstants.GamePath,txt)
                    });

            }
        }

        ///// <summary>
        ///// 异步获取任务信息
        ///// </summary>
        ///// <param name="modChange"> 是否忽视缓存 </param>
        //private void GetMissionInfo(bool modChange)
        //{
            
        //    if (_lbxCampaignList.SelectedIndex == -1 || _lbxCampaignList.SelectedIndex >= _screenMissions.Count) return;

        //    _lbxInforBox.Clear();

        //    try
        //    {
        //        Mission mission = _screenMissions[_lbxCampaignList.SelectedIndex];

        //        if (_cmbGame.SelectedItem == null || _cmbGame.SelectedItem.Tag is not Mod mod)
        //            return;

        //        string missionInfo = string.Empty;
        //        if (!modChange)
        //            missionInfo = mission.MissionInfo;
        //        Rulesmd rulesmd = null;
        //        if (mod.Rules != string.Empty)
        //            rulesmd = new(mod.Rules, mod.ID);

        //        if (string.IsNullOrEmpty(missionInfo) || modChange) // 如果不在内存中
        //        {
        //            infoini ??= new IniFile(Path.Combine(ProgramConstants.GamePath, "Resources/missioninfo.ini"));
        //            if (!modChange)
        //            {

        //                if (infoini.GetSection(mission.SectionName) != null)
        //                {
        //                    missionInfo = infoini.GetValue(mission.SectionName, "info", string.Empty);
        //                }
        //                else { missionInfo = string.Empty; }
        //            }
        //            if (string.IsNullOrEmpty(missionInfo) && rulesmd != null || modChange) // 如果不在缓存中
        //            {
        //                //解析
        //                string mapPath = Path.Combine(mission.Path, mission.Scenario);
        //                var iniFile = new IniFile(mapPath);

        //                var csfPath = Path.Combine(ProgramConstants.GamePath, mission.Path, "ra2md.csf");
        //                if (!File.Exists(csfPath))
        //                    csfPath = Path.Combine(ProgramConstants.GamePath, mod.FilePath, "ra2md.csf");
        //                Dictionary<string, string> csf = new CSF(csfPath).GetCsfDictionary();

        //                if (csf != null && rulesmd != null) // 若csf解析成功
        //                {
        //                    List<string> allSession = iniFile.GetSections();

        //                    foreach (string session in allSession)
        //                    {

        //                        string info = new GameObject(session, csf, iniFile.GetSection(session), rulesmd).GetInfo();
        //                        if (!string.IsNullOrEmpty(info))
        //                        {
        //                            missionInfo += info + "@";
        //                        }

        //                    }
        //                }

        //                infoini.SetValue(mission.SectionName, "info", missionInfo);
        //                infoini.WriteIniFile();
        //            }
        //        }

        //        mission.MissionInfo = missionInfo;

        //        foreach (string info in missionInfo.Split("@"))
        //        {
        //            _lbxInforBox.AddItem(info);
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //    }

        //}

        /// <summary>
        /// 保存战役界面配置。
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                FileInfo settingsFileInfo = SafePath.GetFile(ProgramConstants.GamePath, SETTINGS_PATH);

                // Delete the file so we don't keep potential extra AI players that already exist in the file
                // settingsFileInfo.Delete();

                var campaignSettingsIni = new IniFile(settingsFileInfo.FullName);
                if (!campaignSettingsIni.SectionExists("Settings"))
                    campaignSettingsIni.AddSection("Settings");
                // 写入当前配置
                campaignSettingsIni.SetValue("Settings", "Map", (string)_lbxCampaignList.SelectedItem.Tag ?? string.Empty);
                campaignSettingsIni.SetValue("Settings", "SidesFilter", (string)_ddSide.SelectedItem.Tag ?? string.Empty);
                campaignSettingsIni.SetValue("Settings", "DifficultyFilter", (string)_ddDifficulty.SelectedItem.Tag ?? string.Empty);
                campaignSettingsIni.SetValue("Settings", "MissionPackFilter", ((MissionPack)_ddMissionPack.SelectedItem.Tag)?.ID ?? string.Empty);
                campaignSettingsIni.SetValue("Settings", "DifficultySelector", _trbDifficultySelector.Value);

                //if (ClientConfiguration.Instance.SaveSkirmishGameOptions)
                //{
                if (!campaignSettingsIni.SectionExists("GameOptions"))
                    campaignSettingsIni.AddSection("GameOptions");
                foreach (GameLobbyDropDown dd in DropDowns)
                {
                    campaignSettingsIni.SetValue("GameOptions", dd.Name ?? nameof(dd), dd.UserSelectedIndex + "");
                }

                foreach (GameLobbyCheckBox cb in CheckBoxes)
                {
                    campaignSettingsIni.SetValue("GameOptions", cb.Name ?? nameof(cb), cb.Checked.ToString());
                }
             
                campaignSettingsIni.WriteIniFile();
        }
            catch (Exception ex)
            {
                Logger.Log("Saving campaign settings failed! Reason: " + ex.Message);
            }
}

        /// <summary>
        /// 载入上次保存的设置
        /// </summary>
        private void LoadSettings()
        {
            if (!SafePath.GetFile(ProgramConstants.GamePath, SETTINGS_PATH).Exists)
            {
                return;
            }

            var campaignSettingsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, SETTINGS_PATH));
            if (campaignSettingsIni.SectionExists("Settings"))
            {
                string MapTag = campaignSettingsIni.GetValue("Settings", "Map", string.Empty);
                int foundIndex = _lbxCampaignList.Items.FindIndex(i => i.Tag as string == MapTag);
                if (foundIndex >= 0)
                {
                    _lbxCampaignList.SelectedIndex = foundIndex;
                    _lbxCampaignList.TopIndex = foundIndex;
                }

                string SidesFilterTag = campaignSettingsIni.GetValue("Settings", "SidesFilter", string.Empty);
                foundIndex = _ddSide.Items.FindIndex(i => i.Tag as string == SidesFilterTag);
                if (foundIndex >= 0)
                {
                    _ddSide.SelectedIndex = foundIndex;
                }

                string DifficultyFilterTag = campaignSettingsIni.GetValue("Settings", "DifficultyFilter", string.Empty);
                foundIndex = _ddDifficulty.Items.FindIndex(i => i.Tag as string == DifficultyFilterTag);
                if (foundIndex >= 0)
                {
                    _ddDifficulty.SelectedIndex = foundIndex;
                }

                string MissionPackFilterTag = campaignSettingsIni.GetValue("Settings", "MissionPackFilter", string.Empty);
                foundIndex = _ddMissionPack.Items.FindIndex(i => (((MissionPack)i.Tag)?.ID ?? string.Empty) == MissionPackFilterTag);
                if (foundIndex >= 0)
                {
                    _ddMissionPack.SelectedIndex = foundIndex;
                }

                int DifficultySelector = campaignSettingsIni.GetValue("Settings", "DifficultySelector", 0);
                if (DifficultySelector >= 0)
                {
                    _trbDifficultySelector.Value = DifficultySelector;
                }
            
            }
            //if (ClientConfiguration.Instance.SaveSkirmishGameOptions)
            //{
            if (campaignSettingsIni.SectionExists("GameOptions"))
            {
                foreach (GameLobbyDropDown dd in DropDowns)
                {
                    dd.UserSelectedIndex = campaignSettingsIni.GetValue("GameOptions", dd.Name ?? nameof(dd), dd.UserSelectedIndex);

                    if (dd.UserSelectedIndex > -1 && dd.UserSelectedIndex < dd.Items.Count)
                        dd.SelectedIndex = dd.UserSelectedIndex;
                }

                foreach (GameLobbyCheckBox cb in CheckBoxes)
                {
                    cb.Checked = campaignSettingsIni.GetValue("GameOptions", cb.Name ?? nameof(cb), cb.Checked);
                }
            }
        }

        protected virtual void DelConf()
        {
            //ModManagerEnabled(2);

            var missionPack = _screenMissions[_lbxCampaignList.SelectedIndex].MPack;
            if (missionPack == null) return;


            //var index = _modManager.ListBoxModAi.Items.FindIndex(m => ((MissionPack)m.Tag).ID == missionPack.ID);
            //if (index == -1) return;
            //_modManager.ListBoxModAi.SelectedIndex = index;

            _modManager.DDModAI.SelectedIndex = -1;
            _modManager.删除任务包(missionPack);

            
        }

        /// <summary>
        /// 显示Mod选择器窗口
        /// </summary>
        protected void ModManagerEnabled(int index)
        {

            var modManager = ModManager.GetInstance(WindowManager);
            if (modManager.Enabled)
                return;
            var dp = DarkeningPanel.AddAndInitializeWithControl(WindowManager, modManager);

            //modManager.DDModAI.SelectedIndex = 0;
            modManager.Enable();
            modManager.EnabledChanged += (_, _) =>
            {
                DarkeningPanel.RemoveControl(dp, WindowManager, modManager);
            };

            //_modManager.DDModAI.SelectedIndex = index;

        }

        private Task updateMark(string name)
        {
            //显示远程总分数
            try
            {
                var score = NetWorkINISettings.Get<ClientCore.Entity.Score>($"score/getScore?name={name}").GetAwaiter().GetResult().Item1;
                if (score != null)
                    _lblRatingResult.Text = string.Format("Mission Rating: {0:F1} (Number of participants: {1})".L10N("UI:Main:MissionRating"), score.score, score.total);
                else
                    _lblRatingResult.Text = "Come and grab your first rating ^_^".L10N("UI:Main:FirstRating");
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message);
            }

            var ini = new IniFile(ProgramConstants.GamePath + "Client/Campaign.ini");
            if (ini.SectionExists(name))
            {
                //显示本地打分
                int mark = ini.GetValue(name, "Mark", -1);
                _ratingBox.CheckedIndex = _scoreLevel = mark;
                _ratingBox.Enabled = mark == -1;
                _btnRatingDone.Visible = mark == -1;
            }
            else
            {
                _ratingBox.CheckedIndex = _scoreLevel = -1;
                _ratingBox.Enabled = true;
                _btnRatingDone.Visible = true;
            }

            return Task.CompletedTask;
        }

        private void RatingBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_lbxCampaignList.SelectedIndex == -1 || _lbxCampaignList.SelectedIndex >= _screenMissions.Count) return;

            XNAClientRatingBox ratingBox = (XNAClientRatingBox)sender;
            if (null != ratingBox)
            {
                string name = _screenMissions[_lbxCampaignList.SelectedIndex].SectionName;
                _scoreLevel = ratingBox.CheckedIndex + 1;
                CDebugView.OutputDebugInfo("Mission: {0}, Rating: {1}".L10N("UI:Main:Rating2"), name, _scoreLevel);
            }
        }

        private async Task UploadScore(string strName,string missionPack,string brief, int strScore)
        {

            var score = new ClientCore.Entity.Score()
            {
                missionPack = missionPack,
                name = strName,
                brief = brief,
                score = strScore,
                total = 1
            };

            await NetWorkINISettings.Post<bool?>($"score/updateScore", score);
        }

        private void LbxCampaignListRightClick(object sender, EventArgs e)
        {
            if (_lbxCampaignList.HoveredIndex < 0 || _lbxCampaignList.HoveredIndex >= _lbxCampaignList.Items.Count)
                return;

            _lbxCampaignList.SelectedIndex = _lbxCampaignList.HoveredIndex;

            _campaignMenu.Open(GetCursorPoint());
        }

        private void DDDifficultySelectedIndexChanged(object sender, EventArgs e)
        {
            ScreenMission();
        }

        private void SetDescriptionList(string description)
        {
            foreach (var s in description.Split("\r\n"))
            {
                _tbMissionDescriptionList.AddItem(s, Color.White, false);
            }
        }

        private string 上次选择的任务包ID = string.Empty;

        private bool isUpdating = false;
        private System.Windows.Forms.Timer timer;
        private int 延迟时间 = 300;
        private DateTime lastActionTime = DateTime.MinValue;
        private XNALabel _lblGame;
        private XNALabel lblModify;

        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            ExecuteUpdate(); // 定时触发
        }

        private void ExecuteUpdate()
        {
            if (isUpdating) return;

            try
            {
                isUpdating = true;

                if (_lbxCampaignList.SelectedIndex == -1 || _lbxCampaignList.SelectedIndex >= _screenMissions.Count)
                {

                    _btnLaunch.AllowClick = false;
                    return;
                }

                Mission mission = _screenMissions[_lbxCampaignList.SelectedIndex];


                // 如果不是任务
                if (string.IsNullOrEmpty(mission.Scenario) || !mission.Enabled)
                {
                    _btnLaunch.AllowClick = false;
                    return;
                }

                _cmbGame.SelectedIndexChanged -= CmbGame_SelectedChanged;

                var oldModID = (_cmbGame.SelectedItem?.Tag as Mod)?.ID;

                _cmbGame.Items.Clear();

                if (null == mission.Mod)
                    return;


                if (mission.Mod.Count != 0) //如果任务指定了Mod
                {
                    foreach (var item in mission.Mod)
                    {

                        Mod mod = Mod.Mods.Find(i => i.ID == item && i.CpVisible);
                        if (mod != null)
                            _cmbGame.AddItem(new XNADropDownItem() { Text = mod.Name, Tag = mod });
                    }
                }
                else
                {
                    foreach (var mod in Mod.Mods.Where(mod => mod.CpVisible))
                    {
                        _cmbGame.AddItem(new XNADropDownItem() { Text = mod.Name, Tag = mod });
                    }
                }

                if (上次选择的任务包ID == mission?.MPack?.ID)
                {
                    _cmbGame.SelectedIndex = _cmbGame.Items.FindIndex(item => ((Mod)(item.Tag)).ID == oldModID);

                }
                else
                {
                    _cmbGame.SelectedIndex = _cmbGame.Items.FindIndex(item => ((Mod)(item.Tag)).ID == mission.DefaultMod);
                    显示任务包TxT文件列表(mission.MPack.FilePath);
                }

                上次选择的任务包ID = mission?.MPack?.ID ?? string.Empty;

                if (_cmbGame.SelectedIndex == -1 || _cmbGame.SelectedItem == null)
                    _cmbGame.SelectedIndex = 0;

                if (_cmbGame.Items.Count == 1)
                {
                    _lblGame.Visible = false;
                    _cmbGame.Visible = false;
                }
                else
                {
                    _lblGame.Visible = true;
                    _cmbGame.Visible = true;
                }

                CmbGame_SelectedChanged(null, null);

                _cmbGame.SelectedIndexChanged += CmbGame_SelectedChanged;

                _tbMissionDescriptionList.Clear();
                SetDescriptionList(mission.GUIDescription);

                if ((_cmbGame.SelectedItem?.Tag as Mod)?.ID != oldModID)
                    CmbGame_SelectedChanged(null, null);

                    _ = Task.Run(async () =>
                {
                    // 如果地图文件存在
                    _gameOptionsPanel.Visible = File.Exists(Path.Combine(ProgramConstants.GamePath, mission.Path, mission.Scenario));

                    lblModify.Visible = _gameOptionsPanel.Visible;
                    _mapPreviewBox.Visible = false;

                    //重新加载Mod选择器

                    _btnLaunch.AllowClick = true;

                    

                    if (!string.IsNullOrEmpty(mission.Scenario))
                    {
                        string img = Path.Combine(ProgramConstants.GamePath, mission.Path,
                            mission.Scenario[..mission.Scenario.LastIndexOf('.')] + ".png");
                        if(!File.Exists(img))
                            img = Path.Combine(ProgramConstants.GamePath, mission.Path,
                            mission.Scenario[..mission.Scenario.LastIndexOf('.')] + ".jpg");
                        if (File.Exists(img))
                        {
                            // 加载图像
                            var originalImage = System.Drawing.Image.FromFile(img);

                            // 获取图像的宽高比例
                            float imageAspectRatio = (float)originalImage.Width / originalImage.Height;

                            // 设置预览框的大小为设计时的大小
                            float boxWidth = MapPreviewBoxPosition.Width;
                            float boxHeight = MapPreviewBoxPosition.Height;

                            // 计算预览框的宽高比例
                            float boxAspectRatio = boxWidth / boxHeight;

                            // 如果图像的宽高比例大于预览框的宽高比例，则以预览框的宽度为基准调整高度
                            if (imageAspectRatio > boxAspectRatio)
                            {
                                boxHeight = boxWidth / imageAspectRatio;
                            }
                            // 如果图像的宽高比例小于预览框的宽高比例，则以预览框的高度为基准调整宽度
                            else
                            {
                                boxWidth = boxHeight * imageAspectRatio;
                            }

                            // 计算预览框的位置
                            int x = MapPreviewBoxPosition.Left + (MapPreviewBoxPosition.Width - (int)boxWidth) / 2;
                            int y = MapPreviewBoxPosition.Top + (MapPreviewBoxPosition.Height - (int)boxHeight) / 2;

                            // 设置预览框的大小


                            MapPreviewBoxAspectPosition = new Rectangle(x, y, (int)boxWidth, (int)boxHeight);

                            _mapPreviewBox.ClientRectangle = MapPreviewBoxAspectPosition;

                            // 将图像设置为预览框的纹理
                            _mapPreviewBox.IdleTexture = AssetLoader.LoadTexture(img);

                            // 设置预览框可见
                            _mapPreviewBox.Visible = true;
                        }


                    }

                    if (!mission.Other)
                    {
                        await updateMark(mission.SectionName).ConfigureAwait(false);
                        if (!_ratingBox.Visible)
                        {
                            _lblRatingResult.Visible = _ratingBox.Visible = _btnRatingDone.Visible = true;
                        }
                    }
                    else
                    {
                        if (_ratingBox.Visible)
                            _lblRatingResult.Visible = _ratingBox.Visible = _btnRatingDone.Visible = false;
                    }
                });
            }
            finally
            {
                isUpdating = false;
            }
        }

        private void LbxCampaignListSelectedIndexChanged(object sender, EventArgs e)
        {


            if (isUpdating) return;

            // 计算与上次触发时间的间隔
            var now = DateTime.Now;
            var timeSinceLastAction = (now - lastActionTime).TotalMilliseconds;

            if (timeSinceLastAction < 延迟时间)
            {
                // 如果间隔小于 500ms，重置 Timer 进行延迟触发
                timer.Stop();
                timer.Start();
            }
            else
            {
                // 直接触发
                ExecuteUpdate();
            }

            lastActionTime = now;

        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Enabled = false;
            //ShiftClickAutoClicker.Instance.Stop();
            Exited?.Invoke(this, e);
        }

        private void BtnLaunch_LeftClick(object sender, EventArgs e)
        {
            if (_lbxCampaignList.SelectedIndex == -1 || _lbxCampaignList.SelectedIndex >= _screenMissions.Count) return;

            int selectedMissionId = _lbxCampaignList.SelectedIndex;

            Mission mission = _screenMissions[selectedMissionId];

            if (!ClientConfiguration.Instance.ModMode &&
                (!Updater.IsFileNonexistantOrOriginal(mission.Scenario) || AreFilesModified()))
            {
                // Confront the user by showing the cheater screen
                _missionToLaunch = mission;
                _cheaterWindow.Enable();
                return;
            }

            var result = LaunchCheck();

            if (!string.IsNullOrEmpty(result))
            {
                XNAMessageBox.Show(WindowManager, "Info".L10N("UI:Main:Info"), result);
                return;
            }

            LaunchMission(mission);
        }

        private string LaunchCheck()
        {
            //if (_chkModify.Checked)
                if (_cmbGame.SelectedItem == null)
                    return "Please select a game".L10N("UI:Main:SelectGame");
            return string.Empty;
        }

        protected List<string> GetDeleteFile(string oldGame)
        {
            if (oldGame == null || oldGame == "")
                return null;

            List<string> deleteFile = new List<string>();

            foreach (string file in Directory.GetFiles(oldGame))
            {
                deleteFile.Add(Path.GetFileName(file));

            }

            return deleteFile;
        }

        private bool AreFilesModified()
        {
            foreach (string filePath in _filesToCheck)
            {
                if (!Updater.IsFileNonexistantOrOriginal(filePath))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Called when the user wants to proceed to the Path despite having
        /// being called a cheater.
        /// </summary>
        private void CheaterWindow_YesClicked(object sender, EventArgs e)
        {
            LaunchMission(_missionToLaunch);
        }

        private string 战役临时目录 = SafePath.CombineFilePath(ProgramConstants.GamePath, "Resources\\MissionCache\\");

        /// <summary>
        /// Starts a singleplayer Path.
        /// </summary>
        /// 

        private void LaunchMission(Mission mission)
        {
            if (_lbxCampaignList.SelectedIndex == -1 || _lbxCampaignList.SelectedIndex >= _screenMissions.Count) return;
            Logger.Log("About to write spawn.ini.");

            var spawnIni = new IniFile();
            var mod = ((Mod)_cmbGame.SelectedItem.Tag);

            try
            {
                if (!Directory.Exists(战役临时目录))
                    Directory.CreateDirectory(战役临时目录);
                else if (Directory.GetFiles(战役临时目录).Length > 0)
                {
                    Directory.Delete(战役临时目录, true);
                    Directory.CreateDirectory(战役临时目录);
                }
            }
            catch { }

            if (_gameOptionsPanel.Visible)
            {
                var mapName = SafePath.CombineFilePath(ProgramConstants.GamePath, Path.Combine(mission.Path, mission.Scenario));
                if (!File.Exists(mapName)) return;

                 void ProcessMission(Mission m)
        {
            if (string.IsNullOrEmpty(m.Scenario)) return;

            var mapIni = new IniFile(
                SafePath.CombineFilePath(m.MPack.FilePath, m.Scenario),
                Encoding.GetEncoding("GBK"));

            if (mapIni.GetSections().Count == 0)
            {
                File.Copy(
                    SafePath.CombineFilePath(m.MPack.FilePath, m.Scenario),
                    SafePath.CombineFilePath(战役临时目录, m.Scenario));
            }
            else
            {
                // ===== Header 修复 =====
                if (!mapIni.SectionExists("Header"))
                    mapIni.AddSection("Header");
                if (mapIni.GetValue("Header", "NumberStartingPoints", string.Empty) == string.Empty)
                    mapIni.SetValue("Header", "NumberStartingPoints", 0);

                // ===== General 修复 =====
                if (!mapIni.SectionExists("General"))
                    mapIni.AddSection("General");
                if (mapIni.GetIntValue("General", "MaximumQueuedObjects", 0) == 0)
                    mapIni.SetIntValue("General", "MaximumQueuedObjects", 100);

                // ===== mod 特殊处理 =====
                if (mod.md == string.Empty)
                {
                    mapIni.SetIntValue("MindControl", "Damage", 1);
                    mapIni.SetIntValue("SuperMindControl", "Damage", 1);
                }

                        if (!mapIni.SectionExists("AlliedOccupyW"))

                            mapIni.AddSection("AlliedOccupyW")
                                    .SetValue("Damage", 30)
                                    .SetValue("ROF", 15)
                                    .SetValue("Range", 7)
                                    .SetValue("Projectile", "InvisibleHigh")
                                    .SetValue("Warhead", "SSAB")
                                    .SetValue("Report", "AlliedOccupiedAttack")
                                    .SetValue("OccupantAnim", "UCFLASH")
                                    ;  
                        if (!mapIni.SectionExists("SovietOccupyW"))
                            mapIni.AddSection("SovietOccupyW")
                                .SetValue("Damage", 20)
                                .SetValue("ROF", 20)
                                .SetValue("Range", 7)
                                .SetValue("Projectile", "InvisibleHigh")
                                .SetValue("Warhead", "SSAB")
                                .SetValue("Report", "SovietOccupiedAttack")
                                .SetValue("OccupantAnim", "UCFLASH")
                                ;

                        // ===== 难度 ini 合并 =====
                        var difficultyIni = new Rampastring.Tools.IniFile(
                    SafePath.CombineFilePath(ProgramConstants.GamePath, DifficultyIniPaths[_trbDifficultySelector.Value]));
                IniFile.ConsolidateIniFiles(mapIni, difficultyIni);
                IniFile.ConsolidateIniFiles(mapIni, new IniFile("Client/custom_rules_all.ini"));
                // IniFile.ConsolidateIniFiles(mapIni, new IniFile("Resources/SkinRulesmd.ini"));

                // ===== 应用复选框配置 =====
                foreach (GameLobbyCheckBox chkBox in CheckBoxes)
                {
                    chkBox.ApplySpawnINICode(spawnIni);
                    chkBox.ApplyMapCode(mapIni, null);
                }

                // ===== 应用下拉框配置 =====
                foreach (GameLobbyDropDown dd in DropDowns)
                {
                    dd.ApplySpawnIniCode(spawnIni);
                    dd.ApplyMapCode(mapIni, null);
                }

                // ===== 金币数配置 =====
                if (_cmbCredits.SelectedItem != null && _cmbCredits.SelectedItem.Text != string.Empty)
                    Credits(mapIni, int.Parse(_cmbCredits.SelectedItem.Text) / 100);


                // ===== 保存文件 =====
                mapIni.WriteIniFile(
                    SafePath.CombineFilePath(战役临时目录, m.Scenario),
                    Encoding.GetEncoding("GBK"));
            }
        }

        // 1. 先处理当前任务（同步，保证立即可用）
        ProcessMission(mission);

                // 2. 后台异步处理同一任务包内的其他任务
                Task.Run(() =>
                {
                    foreach (var m in mission.MPack.Missions)
                    {
                        if (m == mission) continue; // 跳过当前任务
                        try
                        {
                            ProcessMission(m);
                        }
                        catch (Exception ex)
                        {
                            // 建议写日志，别影响主流程
                            Console.WriteLine($"处理任务 {m.Scenario} 出错: {ex.Message}");
                        }
                    }
                });

                UserINISettings.Instance.CampaignDefaultGameSpeed.Value = 6 - _cmbGameSpeed.SelectedIndex;
                UserINISettings.Instance.Difficulty.Value = _trbDifficultySelector.Value;
                UserINISettings.Instance.SaveSettings();

                
                //Mix.PackToMix(战役临时目录, Path.Combine(mission.MPack.FilePath, ProgramConstants.MISSION_MIX));
            }

            #region 切换文件

            string newMission = mission.Path;

            var newGame = mod.FilePath;

            #endregion

            #region 写入新设置
            WindowManager.progress.Report("正在写入新设置");

            var settings = new IniSection("Settings");

            //写入新游戏
            settings.SetValue("Game", newGame);

            settings.SetValue("Mission", newMission);

            //if(_chkExtension.Checked)
            //    settings.SetValue("Ra2Mode", mod.md != "md");
            //else//这里不知为何一定得写False，即使是用原版玩，用True会弹窗
                settings.SetValue("Ra2Mode", false);
            settings.SetValue("chkSatellite", CheckBoxes?.Find(chk => chk.Name == "chkSatellite")?.Checked ?? false);
            settings.SetValue("Scenario", mission.Scenario);
            settings.SetValue("CampaignID", mission.Index);
            settings.SetValue("IsSinglePlayer", true);
            settings.SetValue("SidebarHack", ClientConfiguration.Instance.SidebarHack);
            settings.SetValue("Side", mission.Side);
            settings.SetValue("BuildOffAlly", mission.BuildOffAlly);
            settings.SetValue("DifficultyModeHuman", (mission.PlayerAlwaysOnNormalDifficulty ? "1" : _trbDifficultySelector.Value.ToString()));
            settings.SetValue("DifficultyModeComputer", GetComputerDifficulty());
            settings.SetValue("chkTerrain", false);

            spawnIni.AddSection(settings);

            UserINISettings.Instance.Difficulty.Value = _trbDifficultySelector.Value;

            #endregion

            SaveSettings();

        //    ((MainMenuDarkeningPanel)Parent).Hide();

            string difficultyName = DifficultyNames[_trbDifficultySelector.Value];

            _discordHandler.UpdatePresence(mission.GUIName, difficultyName, mission.IconPath, true);

            GameProcessLogic.GameProcessExited += GameProcessExited_Callback;

            GameProcessLogic.StartGameProcess(WindowManager, spawnIni);
        }

        //private void 保存配置(IniFile settings)
        //{
            
        //};

        private int GetComputerDifficulty() =>
            Math.Abs(_trbDifficultySelector.Value - 2);

        private void GameProcessExited_Callback()
        {
            WindowManager.AddCallback(new Action(GameProcessExited), null);
        }

        protected virtual void GameProcessExited()
        {
            GameProcessLogic.GameProcessExited -= GameProcessExited_Callback;
         
            _discordHandler.UpdatePresence();
        }

        public void ScreenMission()
        {

            _screenMissions.Clear();
            _lbxCampaignList.Clear();

            foreach (var mission in _missions)
            {
               
                if (_ddDifficulty.SelectedItem.Tag != null && mission.Difficulty != (string)_ddDifficulty.SelectedItem.Tag)
                    continue;
               
                // 筛选阵营
                if (_ddSide.SelectedItem.Tag != null && mission.IconPath != (string)_ddSide.SelectedItem.Tag)
                    continue;
               
                // 筛选任务包
                if (_ddMissionPack.SelectedItem.Tag != null && mission.MPack != (MissionPack)_ddMissionPack.SelectedItem.Tag)
                    continue;
               
                _screenMissions.Add(mission);

                var item = new XNAListBoxItem();
                item.Text = mission.GUIName.L10N("UI:MissionName:" + mission.SectionName);
                item.Tag = mission.SectionName;
                if (!mission.Enabled)
                {
                    item.TextColor = UISettings.ActiveSettings.DisabledItemColor;
                }
                else if (string.IsNullOrEmpty(mission.Scenario))
                {
                    item.TextColor = AssetLoader.GetColorFromString(
                        ClientConfiguration.Instance.ListBoxHeaderColor);

                    item.IsHeader = true;
                    item.Selectable = false;
                    item.TextColor = Color.DodgerBlue;
                }
                else
                {
                    //item.TextColor = lbCampaignList.DefaultItemColor;
                    if (mission.Difficulty == "困难")
                        item.TextColor = Color.Red;
                    else if (mission.Difficulty == "简单")
                        item.TextColor = Color.Green;
                    else if (mission.Difficulty == "极难")
                        item.TextColor = Color.Black;
                    else
                        item.TextColor = Color.AliceBlue;
                }
                var mod = Mod.Mods.Find(mod => mod.ID == mission?.MPack?.DefaultMod);

                 var iconPath = Path.Combine(ProgramConstants.GamePath, "Resources", mission.IconPath + "icon.png");
                if (mod != null)
                {
                    var modIconPath = Path.Combine(mod.FilePath,"Resources", mission.IconPath) + "icon.png";
                    if (File.Exists(modIconPath))
                        iconPath = modIconPath;
                } 

                if (File.Exists(iconPath))
                    item.Texture = AssetLoader.LoadTexture(iconPath);

                _lbxCampaignList.AddItem(item);
            }

            _lbxCampaignList.TopIndex = -1;
        }

        public void ReadMissionList()
        {
            Mod.ReLoad();
            MissionPack.ReLoad();
            
            _missions.Clear();
         
            _ddSide.SelectedIndexChanged -= DDDifficultySelectedIndexChanged;
            _ddDifficulty.SelectedIndexChanged -= DDDifficultySelectedIndexChanged;
            _ddMissionPack.SelectedIndexChanged -= DDDifficultySelectedIndexChanged;

            _ddSide.Items.Clear();
            _ddDifficulty.Items.Clear();
            _ddMissionPack.Items.Clear();

            _ddDifficulty.AddItem(new XNADropDownItem() { Text = "Difficulty of screening".L10N("UI:Main:DifficultyScreening") });
            _ddSide.AddItem(new XNADropDownItem() { Text = "Filter the factions".L10N("UI:Main:FilterFactions") });
            _ddMissionPack.AddItem(new XNADropDownItem() { Text = "Select the mission packs".L10N("UI:Main:SelectMissionPacks") });

            // Mod.Clear();
            string path = @"Maps/Cp";

            var files = Directory.GetFiles(path, "Battle*.ini");

            foreach (var file in files)
            {
                 ParseBattleIni(file);
            }

            

            //if (Missions.oldSaves == 0)
            //    ParseBattleIni("INI/" + ClientConfiguration.Instance.BattleFSFileName);

            _difficultyList = _difficultyList.ToArray().GroupBy(p => p).Select(p => p.Key).ToList();
            _sideList = _sideList.ToArray().GroupBy(p => p).Select(p => p.Key).ToList();

            foreach (string diff in _difficultyList)
            {
                _ddDifficulty.AddItem(new XNADropDownItem() { Text = diff.L10N("UI:Campaign:" + diff), Tag = diff });
          
            }

            foreach (string side in _sideList)
            {
                _ddSide.AddItem(new XNADropDownItem() { Text = side.L10N("UI:Campaign:" + side), Tag = side });
            }

            foreach (var missionPack in MissionPack.MissionPacks)
            {
               
                _ddMissionPack.AddItem(new XNADropDownItem() { Text = missionPack.Description, Tag = missionPack });
            }

            _ddSide.SelectedIndex = 0;
            _ddDifficulty.SelectedIndex = 0;
            _ddMissionPack.SelectedIndex = 0;

            _ddSide.SelectedIndexChanged += DDDifficultySelectedIndexChanged;
            _ddDifficulty.SelectedIndexChanged += DDDifficultySelectedIndexChanged;
            _ddMissionPack.SelectedIndexChanged += DDDifficultySelectedIndexChanged;

            ScreenMission();

            _lbxCampaignList.SelectedIndex = -1;
        }

        /// <summary>
        /// Parses a Battle(E).ini file. Returns true if succesful (file found), otherwise false.
        /// </summary>
        /// <param name="path">The path of the file, relative to the game directory.</param>
        /// <returns>True if succesful, otherwise false.</returns>
        private void ParseBattleIni(string path)
        {

            Logger.Log("解析客户端任务注册文件 " + path);

            FileInfo battleIniFileInfo = SafePath.GetFile(ProgramConstants.GamePath, path);
            if (!battleIniFileInfo.Exists)
            {
                Logger.Log("文件 " + path + " 未找到. 忽略.");
                return;
            }

            //if (Missions.oldSaves > 0)
            //{
            //    throw new InvalidOperationException("Loading multiple Battle*.ini files is not supported anymore.");
            //}

            // 读取任务ini
            var battleIni = new IniFile(battleIniFileInfo.FullName);

            // 读取任务
            List<string> battleKeys = battleIni.GetSectionKeys("Battles");

            if (battleKeys == null)
                return; // File exists but [Battles] doesn't


            for (int i = 0; i < battleKeys.Count; i++)
            {
                string battleEntry = battleKeys[i];
                string battleSection = battleIni.GetValue("Battles", battleEntry, "NOT FOUND");

                if (!battleIni.SectionExists(battleSection))
                    continue;

                var mission = new Mission(battleIni, battleSection, i);

                // 筛选难度
                if (mission.Difficulty != string.Empty)
                    _difficultyList.Add(mission.Difficulty);
                if (mission.IconPath != string.Empty)
                    _sideList.Add(mission.IconPath);

                _missions.Add(mission);
            }

            Logger.Log("完成解析 " + path + ".");
        }

        public void SwitchOn()
        {
            Enable();
        }

        public void SwitchOff()
        {
            Disable();
        }

        public string GetSwitchName()
        {
            return "Skirmish Lobby".L10N("UI:Main:SkirmishLobby");
        }

        private string GetFixedFormatText(string strDes)
        {
            string strTmp = strDes.Replace("\r\n\r\n\r\n","\r\n").Replace("。", "。\r\n").Replace("!", "!\r\n");
            string[] strArr = strTmp.Split("\r\n");
            StringBuilder sBuff = new StringBuilder();
            for (int i = 0; i < strArr.Length; i++)
            {
                if (strArr[i].Length > 39)
                {
                    var lstStr = SplitLongString(strArr[i]);
                    foreach(string str in lstStr)
                    {
                        sBuff.Append(str + "\r\n");
                    }
                }
                else
                    sBuff.Append(strArr[i] + "\r\n");
            }
            strTmp = sBuff.ToString();
            sBuff.Clear();
            return strTmp;
        }

        private List<string> SplitLongString(string strText, int nSize = 39)
        {
            int nLen = strText.Length;
            int nCount = (int)Math.Ceiling(nLen * 1.0 / nSize);
            List<string> lstStrs = new List<string>();
            for (int i = 0; i < nCount; i++)
            {
                if(i < (nCount- 1))
                    lstStrs.Add(strText.Substring(i * nSize, nSize));
                else
                    lstStrs.Add(strText.Substring(i * nSize));
            }
            return lstStrs;
        }
    }
}

﻿using System;
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
using Google.Protobuf;

namespace Ra2Client.DXGUI.Generic
{
    public class CampaignSelector : INItializableXNAWindow
    {
        private const int DefaultWidth = 650;
        private const int DefaultHeight = 600;

        private static readonly string[] DifficultyNames = { "Easy", "Medium", "Hard" };

        private static readonly string[] DifficultyIniPaths =
        {
            "INI/MapCode/Difficulty Easy.ini",
            "INI/MapCode/Difficulty Medium.ini", 
            "INI/MapCode/Difficulty Hard.ini"
        };

        public event Action ReLoadMissionList;

        public CampaignSelector(WindowManager windowManager, DiscordHandler discordHandler) : base(windowManager)
        {
            this._discordHandler = discordHandler;
        }

        //public static CampaignSelector GetInstance(WindowManager windowManager, DiscordHandler discordHandler)
        //{
        //    return _instance ??= new CampaignSelector(windowManager, discordHandler);
        //}

        private DiscordHandler _discordHandler;

        private List<Mission> _missions = new();
        private List<Mission> _screenMissions = new();
        private XNAListBox _lbxCampaignList;
        private XNALabel _lblScreen;
        private XNADropDown _ddDifficulty;
        private XNADropDown _ddSide;
        private XNADropDown _ddMissionPack;
        private GameLobbyDropDown _cmbCredits;
        private XNAClientButton _btnLaunch;
        private XNATextBlock _tbMissionDescription;
        private XNATrackbar _trbDifficultySelector;

        Tuple<int, List<string>> _extension;
        public List<GameLobbyCheckBox> CheckBoxes = [];
        public List<GameLobbyDropDown> DropDowns = new();

        private XNAButton _mapPreviewBox;

        public Rectangle MapPreviewBoxPosition { get; private set; }

        private Rectangle MapPreviewBoxAspectPosition { get; set; }

        private XNALabel _lblGame;
        private GameLobbyDropDown _cmbGame;
        private GameLobbyDropDown _cmbGameSpeed;
        private XNAClientCheckBox _chkExtension;
        private XNAListBox _lbxInforBox;
        private XNAPanel _gameOptionsPanel;
        private XNACheckBox _chkModify;
        private XNAClientRatingBox _ratingBox;
        private XNAClientButton _btnRatingDone;
        private XNALabel _lblRatingResult;
        private ModManager _modManager;

        private CheaterWindow _cheaterWindow;

        private List<string> _difficultyList = new();
        private List<string> _sideList = new();

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
        private int _missionIndex = -1;
        private int count = 0;

        public event EventHandler Exited;

        public override void Initialize()
        {
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
            _modManager.MyEvent += (_, _) => ReadMissionList();
            //modManager.EnabledChanged += CampaignSelector_EnabledChanged;

            _lblScreen = new XNALabel(WindowManager);
            _lblScreen.Name = "lblScreen";
            _lblScreen.Text = "筛选:";
            _lblScreen.ClientRectangle = new Rectangle(10, 35, 0, 0);

            _ddDifficulty = new XNADropDown(WindowManager);
            _ddDifficulty.Name = nameof(_ddDifficulty);
            _ddDifficulty.ClientRectangle = new Rectangle(10, 60, 100, 25);

            _ddSide = new XNADropDown(WindowManager);
            _ddSide.Name = nameof(_ddSide);
            _ddSide.ClientRectangle = new Rectangle(_ddDifficulty.X + _ddDifficulty.Width + 5, _ddDifficulty.Y, _ddDifficulty.Width, _ddDifficulty.Height);

            _ddMissionPack = new XNADropDown(WindowManager);
            _ddMissionPack.Name = nameof(_ddMissionPack);
            _ddMissionPack.ClientRectangle = new Rectangle(_ddDifficulty.X, _ddDifficulty.Y + _ddDifficulty.Height + 5, _ddSide.X + _ddSide.Width - _ddDifficulty.X, _ddDifficulty.Height);

            var lblMissionDescriptionHeader = new XNALabel(WindowManager);
            lblMissionDescriptionHeader.Name = "lblMissionDescriptionHeader";
            lblMissionDescriptionHeader.FontIndex = 1;
            lblMissionDescriptionHeader.ClientRectangle = new Rectangle(
                _lbxCampaignList.Right + 12,
                lblSelectCampaign.Y, 0, 0);
            lblMissionDescriptionHeader.Text = "MISSION DESCRIPTION:".L10N("UI:Main:MissionDescription");

            _tbMissionDescription = new XNATextBlock(WindowManager);
            _tbMissionDescription.Name = "tbMissionDescription";
            _tbMissionDescription.ClientRectangle = new Rectangle(
                lblMissionDescriptionHeader.X,
                lblMissionDescriptionHeader.Bottom + 6,
                600, 300);
            _tbMissionDescription.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            _tbMissionDescription.Alpha = 1.0f;
            _tbMissionDescription.FontIndex = 1;
            _tbMissionDescription.BackgroundTexture = AssetLoader.CreateTexture(AssetLoader.GetColorFromString(ClientConfiguration.Instance.AltUIBackgroundColor),
                _tbMissionDescription.Width, _tbMissionDescription.Height);

            _chkModify = new XNAClientCheckBox(WindowManager);
            _chkModify.Name = nameof(_chkModify);
            _chkModify.ClientRectangle = new Rectangle(_tbMissionDescription.X + _tbMissionDescription.Width + 10, _tbMissionDescription.Y + 10, 0, 0);
            _chkModify.Text = "开启修改";
            _chkModify.CheckedChanged += ChkModify_CheckedChanged;
            _chkModify.Checked = false;
            _chkModify.Visible = false;

            _chkExtension = new XNAClientCheckBox(WindowManager);
            _chkExtension.Name = nameof(_chkExtension);
            _chkExtension.ClientRectangle = new Rectangle(_chkModify.X, _chkModify.Y + 30, 0, 0);
            _chkExtension.Text = "启用扩展平台";
            _chkExtension.CheckedChanged += ChkExtension_SelectedChanged;
            //var  ToolTip = new ToolTip(WindowManager, chkExtension) { Text = "启用合适的扩展平台（Ares,Phobos等）。"};
            AddChild(_chkExtension);
            _chkExtension.Visible = false;

            var lblModify = new XNALabel(WindowManager);
            lblModify.Name = nameof(lblModify);
            lblModify.Text = "注：某些修改可能会破坏战役流程。";
            lblModify.ClientRectangle = new Rectangle(_chkModify.X + 100, _chkModify.Y, 0, 0);

            var lblDifficultyLevel = new XNALabel(WindowManager);
            lblDifficultyLevel.Name = "lblDifficultyLevel";
            lblDifficultyLevel.Text = "DIFFICULTY LEVEL".L10N("UI:Main:DifficultyLevel");
            lblDifficultyLevel.FontIndex = 1;
            Vector2 textSize = Renderer.GetTextDimensions(lblDifficultyLevel.Text, lblDifficultyLevel.FontIndex);
            lblDifficultyLevel.ClientRectangle = new Rectangle(
                _tbMissionDescription.X + (_tbMissionDescription.Width - (int)textSize.X),
                _tbMissionDescription.Bottom, (int)textSize.X, (int)textSize.Y);

            _trbDifficultySelector = new XNATrackbar(WindowManager);
            _trbDifficultySelector.Name = "trbDifficultySelector";
            _trbDifficultySelector.ClientRectangle = new Rectangle(
                _tbMissionDescription.X, lblDifficultyLevel.Bottom + 6,
                _tbMissionDescription.Width - 255, 35);
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
                Text = "刷新",
                SelectAction = () => ReadMissionList()
            });
            _campaignMenu.AddItem(new XNAContextMenuItem
            {
                Text = "删除这组任务",
                SelectAction = DelConf
            });
            _campaignMenu.AddItem(new XNAContextMenuItem
            {
                Text = "任务包管理器",
                SelectAction = () => ModManagerEnabled(2)
            });
            _campaignMenu.AddItem(new XNAContextMenuItem
            {
                Text = "导入任务包",
                SelectAction = () =>
                {
                    ModManagerEnabled(2);
                    _modManager.BtnNew.OnLeftClick();

                }
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
                    ModManagerEnabled(0);
                    _modManager.BtnNew.OnLeftClick();
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
                _tbMissionDescription.X + (_tbMissionDescription.Width - (int)textSize.X) / 2,
                lblEasy.Y, (int)textSize.X, (int)textSize.Y);

            var lblHard = new XNALabel(WindowManager);
            lblHard.Name = "lblHard";
            lblHard.FontIndex = 1;
            lblHard.Text = "HARD".L10N("UI:Main:DifficultyHard");
            lblHard.ClientRectangle = new Rectangle(
                _tbMissionDescription.Right - lblHard.Width,
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
            _ratingBox.Text = "评分";
            _ratingBox.ClientRectangle = new Rectangle(_lbxCampaignList.X, _lbxCampaignList.Y + _lbxCampaignList.Height + 150, 0, 0);
            _ratingBox.CheckedChanged += RatingBox_CheckedChanged;
            _ratingBox.Visible = false;
            AddChild(_ratingBox);

            _btnRatingDone = new XNAClientButton(WindowManager);
            _btnRatingDone.Name = nameof(_btnRatingDone);
            _btnRatingDone.Text = "打分";
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
            lblalter.Text = "这个任务有以下改动: ";

            AddChild(lblSelectCampaign);
            AddChild(lblMissionDescriptionHeader);
            AddChild(_lbxCampaignList);
            AddChild(_lblScreen);
            AddChild(_ddDifficulty);
            AddChild(_ddSide);
            AddChild(_lbxInforBox);
            AddChild(_ddMissionPack);
            AddChild(_chkModify);
            AddChild(lblalter);
            AddChild(lblModify);
            AddChild(_tbMissionDescription);
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

            UserINISettings.Instance.ReLoadMissionList += ReadMissionList;

            //ReadDrop();


            _lblGame = FindChild<XNALabel>("lblGame");
            _cmbGame = FindChild<GameLobbyDropDown>("cmbGame");
            // lbCampaignList.SelectedIndex = 1;
            //    LbxCampaignListSelectedIndexChanged(lbCampaignList, new EventArgs());
            _cmbGame.SelectedIndexChanged += CmbGame_SelectedChanged;
            // CmbGame_SelectedChanged(cmbGame, new EventArgs());

            _cmbGame.RightClick += (_, _) => _modMenu.Open(GetCursorPoint());
            _gameOptionsPanel = FindChild<XNAPanel>("GameOptionsPanel");
            _gameOptionsPanel.Visible = false;
            _mapPreviewBox = FindChild<XNAButton>("mapPreviewBox");
            MapPreviewBoxPosition = _mapPreviewBox.ClientRectangle;
            _mapPreviewBox.LeftClick += MapPreviewBox_LeftClick;
            _cmbGameSpeed = FindChild<GameLobbyDropDown>("cmbGameSpeed");

            _cmbCredits = FindChild<GameLobbyDropDown>("cmbCredits");

            _lbxInforBox.ClientRectangle = new Rectangle(_gameOptionsPanel.X, _mapPreviewBox.Y + 25, 345, _mapPreviewBox.Height - 185);
            _lbxInforBox.FontIndex = 1;
            _lbxInforBox.LineHeight = 20;
            _lbxInforBox.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;


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

            LoadSettings();


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

        private async void BtnRatingDone_LeftClick(object sender, EventArgs e)
        {
            if (-1 == _scoreLevel)
            {
                XNAMessageBox.Show(WindowManager, "信息", "您是否还没有打分呢！");
                return;
            }

            string missionName = _screenMissions[_lbxCampaignList.SelectedIndex].SectionName;
            var ini = new IniFile(ProgramConstants.GamePath + SETTINGS_PATH);
            if (!ini.SectionExists(missionName))
                ini.AddSection(missionName);

            int mark = ini.GetValue(missionName, "Mark", -1);
            if (-1 != mark)
                XNAMessageBox.Show(WindowManager, "信息", "这个战役您已经打过分啦！");
            else
            {
               _ = Task.Run(async() =>
                {
                    await UploadScore(missionName, _scoreLevel);

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

        private void ChkExtension_SelectedChanged(object sender, EventArgs e)
        {
            //if (UserINISettings.Instance.ChkExtensionIsFirst.Value)
            //{
            //    XNAMessageBox.Show(WindowManager, "扩展平台", "打开后会启用Ares与Phobos.\n" +
            //        "部分功能只有在开启扩展平台后才能使用。如shift连点器，部分皮肤（建造预览，飞机尾迹等等），同时也会修复一些原版的bug（特性）。" +
            //        "\n然而有些地图或任务使用了原版的特性，(如林德拉克计划)必须不开启扩展才能正常游玩。");

            //    UserINISettings.Instance.ChkExtensionIsFirst.Value = false;
            //    UserINISettings.Instance.SaveSettings();
            //}

            if (_lbxCampaignList.SelectedIndex != -1 && _lbxCampaignList.SelectedItem != null)
            {
                _chkExtension.Visible = true;
                ExtensionChange();
            }
        }

        /// <summary>
        /// 返回当前可使用扩展平台情况
        /// </summary>
        /// <returns>键：0表示必须使用扩展平台,1表示可用可不用</returns>
        private void GetUseExtension()
        {
            //  Tuple<int, List<string>> Extension = new Dictionary<int, List<string>>();
            List<string> list;


            // 优先级 任务>任务包>Mod

            if (_cmbGame.SelectedItem.Tag is not Mod mod)
                return;

            Mission mission = _screenMissions[_lbxCampaignList.SelectedIndex];

            if (mission.MuExtension || mod.ExtensionOn)
            {  //如果任务，任务包，Mod 有必须使用扩展的
               // Extension.Add(0, null);
                _extension = new Tuple<int, List<string>>(0, null);
                return;
            }

            ////如果都不是空那就取交集
            //if(!string.IsNullOrEmpty(mission.Extension) && !string.IsNullOrEmpty(mod.Extension))
            //{
            //    var missionExtensions = mission.Extension.Split(',').ToList();
            //    var modExtensions = mod.Extension.Split(',').ToList();
            //    list = missionExtensions.Intersect(modExtensions).ToList();
            //}
            //else
            //{
            //    //如果都是空那就没有可用的扩展。
            //    if (string.IsNullOrEmpty(mission.Extension) && string.IsNullOrEmpty(mod.Extension))
            //        list = null;
            //    else if (!string.IsNullOrEmpty(mission.Extension))
            //        list = mission.Extension.Split(',').ToList();
            //    else if(!string.IsNullOrEmpty(mod.Extension))
            //        list = mod.Extension.Split(',').ToList();
            //}

            _extension = new Tuple<int, List<string>>(1, null);
        }

        public void ExtensionChange()
        {

            // 获取扩展使用情况
            GetUseExtension();
   
            if (_extension == null)
            {
                _chkExtension.AllowChecking = false;
                _chkExtension.Checked = false;
            }
            // 刷新扩展按钮
            else if (_extension.Item1 == 0)
            {
                _chkExtension.AllowChecking = false;
                _chkExtension.Checked = true;

            }
            else if (_extension.Item1 == 1)
            {
                //  _chkExtension.Checked = false;
            }


            //根据扩展按钮刷新其他控件
            foreach (var chk in CheckBoxes)
            {
                if (_chkExtension.Checked) // 扩展被选中，说明当前有可使用的扩展
                {
                    if (string.IsNullOrEmpty(chk.Extension))
                    {
                        chk.AllowChecking = true;
                        continue;
                    }

                    foreach (var e in chk.Extension.Split(','))
                    {

                        if (_extension == null || _extension.Item2 != null && _extension.Item2.Find(ex => ex == e) == null)  // 当该单选框所需要的扩展不在当前可用扩展中时
                        {

                            chk.Checked = false;
                            chk.AllowChecking = false;
                            break;
                        }
                        chk.AllowChecking = true;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(chk.Extension))
                    {
                        chk.Checked = false;
                        chk.AllowChecking = false;
                    }
                    else
                    {
                        chk.AllowChecking = true;
                    }
                }
            }

            foreach (var dd in DropDowns)
            {
                if (_chkExtension.Checked)
                {
                    if (string.IsNullOrEmpty(dd.Extension))
                    {
                        dd.AllowDropDown = true;
                        continue;
                    }
                    foreach (var e in dd.Extension.Split(','))
                    {

                        if (_extension.Item2 != null && _extension.Item2.Find(ex => ex == e) == null)
                        {

                            dd.SelectedIndex = dd.defaultIndex;
                            dd.AllowDropDown = false;
                            break;
                        }

                        dd.AllowDropDown = true;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(dd.Extension))
                    {
                        dd.SelectedIndex = dd.defaultIndex;
                        dd.AllowDropDown = false;
                    }
                    else
                    {
                        dd.AllowDropDown = true;
                    }
                }
            }


        }

        private void CmbGame_SelectedChanged(object sender, EventArgs e)
        {
            if (_cmbGame.SelectedItem == null || _cmbGame.SelectedItem == null)
                return;

            if (_lbxCampaignList.SelectedIndex == -1 || _lbxCampaignList.SelectedIndex >= _screenMissions.Count) return;

            Task.Run(() => { GetMissionInfo(true); });
            

            ChkModify_CheckedChanged(null, null);

            if (((Mod)(_cmbGame.SelectedItem.Tag)).md == "md" && !_screenMissions[_lbxCampaignList.SelectedIndex].YR)
            {
                _chkExtension.Checked = true;
                _chkExtension.AllowChecking = false;
            }
            else
            {
                _chkExtension.AllowChecking = true;
            }

            base.OnSelectedChanged();

        }


        private void ChkModify_CheckedChanged(object sender, EventArgs e)
        {
            if (_lbxCampaignList.SelectedIndex == -1 || _lbxCampaignList.SelectedIndex >= _screenMissions.Count) return;


            //if (UserINISettings.Instance.ChkModifyIsFirst.Value)
            //{
            //    XNAMessageBox.Show(WindowManager, "开启修改", "开启后若勾选下面的游戏选项则会修改地图.\n" +
            //        "这些功能可以提供一些额外的体验，不过由于本质对地图进行了修改，所以有可能会导致地图触发流程出现问题。并且开启修改后无法自动进行下一关。\n");

            //    UserINISettings.Instance.ChkModifyIsFirst.Value = false;
            //    UserINISettings.Instance.SaveSettings();
            //}

            _gameOptionsPanel.Visible = _chkModify.Checked;

            //bool isYR = _screenMissions[_lbxCampaignList.SelectedIndex].YR;
            //bool isSelectedNotNull = _cmbGame.SelectedItem != null;
            //bool isModifyChecked = _chkModify.Checked;
            //bool isMd = ((Mod)(_cmbGame.SelectedItem.Tag)).md == "md";

            //if ((isSelectedNotNull && isYR && !isModifyChecked) || (isMd && isYR))
            //{
            //    _chkExtension.Checked = true;
            //    _chkExtension.AllowChecking = false;
            //}
            //else
            //{
            //    _chkExtension.AllowChecking = true;
            //}

        }

        /// <summary>
        /// 异步获取任务信息
        /// </summary>
        /// <param name="modChange"> 是否忽视缓存 </param>
        private async void GetMissionInfo(bool modChange)
        {
            if (_lbxCampaignList.SelectedIndex == -1 || _lbxCampaignList.SelectedIndex >= _screenMissions.Count) return;

            _lbxInforBox.Clear();

            Mission mission = _screenMissions[_lbxCampaignList.SelectedIndex];

            if (_cmbGame.SelectedItem == null || _cmbGame.SelectedItem.Tag is not Mod mod)
                return;

            string missionInfo = string.Empty;
            if (!modChange)
                missionInfo = mission.MissionInfo;
            Rulesmd rulesmd = null;
            if (mod.rules != string.Empty)
                rulesmd = new(mod.rules, mod.ID);

            if (string.IsNullOrEmpty(missionInfo) || modChange) // 如果不在内存中
            {
                var infoini = new IniFile(Path.Combine(ProgramConstants.GamePath, "Resources/missioninfo.ini"));
                if (!modChange)
                {

                    if (infoini.GetSection(mission.SectionName) != null)
                    {
                        missionInfo = infoini.GetValue(mission.SectionName, "info", string.Empty);
                    }
                    else { missionInfo = string.Empty; }
                }
                if (string.IsNullOrEmpty(missionInfo) && rulesmd != null || modChange) // 如果不在缓存中
                {
                    //解析
                    string mapPath = Path.Combine(mission.Path, mission.Scenario);
                    var iniFile = new IniFile(mapPath);

                    var csfPath = Path.Combine(ProgramConstants.GamePath, mission.Path, "ra2md.csf");
                    if (!File.Exists(csfPath))
                        csfPath = Path.Combine(ProgramConstants.GamePath, mod.FilePath, "ra2md.csf");
                    Dictionary<string, string> csf = new CSF(csfPath).GetCsfDictionary();

                    if (csf != null && rulesmd != null) // 若csf解析成功
                    {
                        List<string> allSession = iniFile.GetSections();

                        foreach (string session in allSession)
                        {

                            string info = new GameObject(session, csf, iniFile.GetSection(session), rulesmd).GetInfo();
                            if (!string.IsNullOrEmpty(info))
                            {
                                missionInfo += info + "@";
                            }

                        }
                    }

                    infoini.SetValue(mission.SectionName, "info", missionInfo);
                    infoini.WriteIniFile();
                }
            }

            mission.MissionInfo = missionInfo;

            foreach (string info in missionInfo!.Split("@"))
            {
                _lbxInforBox.AddItem(info);
            }

        }

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
                    campaignSettingsIni.SetValue("GameOptions", dd.Name, dd.UserSelectedIndex + "");
                }

                foreach (GameLobbyCheckBox cb in CheckBoxes)
                {
                    campaignSettingsIni.SetValue("GameOptions", cb.Name, cb.Checked.ToString());
                }

                campaignSettingsIni.SetValue("GameOptions", "chkExtension", _chkExtension.Checked);
                campaignSettingsIni.SetValue("GameOptions", "chkModify", _chkModify.Checked);

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

                _chkExtension.Checked = campaignSettingsIni.GetValue("GameOptions", "chkExtension", true);
                _chkModify.Checked = campaignSettingsIni.GetValue("GameOptions", "chkModify", false);

            }
            //if (ClientConfiguration.Instance.SaveSkirmishGameOptions)
            //{
            if (campaignSettingsIni.SectionExists("GameOptions"))
            {
                foreach (GameLobbyDropDown dd in DropDowns)
                {
                    dd.UserSelectedIndex = campaignSettingsIni.GetValue("GameOptions", dd.Name, dd.UserSelectedIndex);

                    if (dd.UserSelectedIndex > -1 && dd.UserSelectedIndex < dd.Items.Count)
                        dd.SelectedIndex = dd.UserSelectedIndex;
                }

                foreach (GameLobbyCheckBox cb in CheckBoxes)
                {
                    cb.Checked = campaignSettingsIni.GetValue("GameOptions", cb.Name, cb.Checked);
                }
            }
            //}
        }

        protected virtual void DelConf()
        {
            ModManagerEnabled(2);

            var missionPack = _screenMissions[_lbxCampaignList.SelectedIndex].MPack;
            if (missionPack == null) return;

            var index = _modManager.ListBoxModAi.Items.FindIndex(m => ((MissionPack)m.Tag).ID == missionPack.ID);
            if (index == -1) return;
            _modManager.ListBoxModAi.SelectedIndex = index;
            _modManager.BtnDel.OnLeftClick();
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

            modManager.DDModAI.SelectedIndex = 0;
            modManager.Enable();
            modManager.EnabledChanged += (_, _) =>
            {
                DarkeningPanel.RemoveControl(dp, WindowManager, modManager);
                if (_chkExtension.AllowChecking != false)
                    _chkExtension.Checked = false;
            };

            _modManager.DDModAI.SelectedIndex = index;

        }

        private async Task updateMark(string name)
        {
            //显示远程总分数
            try
            {
                //var uri = "https://dln1.yra2.com/Client/Scores/rating_index.php";
                //Dictionary<string, string> dic = new Dictionary<string, string>();
                //dic.Add("id", name);
                //dic.Add("op", "query");
                //dic.Add("key", "r2e0u2n3i1o1n");
                //var response = await WebHelper.HttpGet(uri, dic);
                //if (!string.IsNullOrEmpty(response))
                //{
                //    RatingObejct ratingObj = (RatingObejct)JsonSerializer.Deserialize(response, typeof(RatingObejct));
                //    int nTotalNum = int.Parse(ratingObj.total);
                //    if (nTotalNum > 0)
                //        _lblRatingResult.Text = string.Format("任务评分:{0:F1}分（参与人数:{1}）", float.Parse(ratingObj.score), nTotalNum);
                //    else
                //        _lblRatingResult.Text = "快来抢占您的第一个评分^_^";
                //}

                var score = NetWorkINISettings.Get<ClientCore.Entity.Score>($"score/getScore?name={name}").GetAwaiter().GetResult().Item1;
                if (score != null)
                    _lblRatingResult.Text = string.Format("任务评分:{0:F1}分（参与人数:{1}）", score.score, score.total);
                else
                    _lblRatingResult.Text = "快来抢占您的第一个评分^_^";
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
        }

        private void RatingBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_lbxCampaignList.SelectedIndex == -1 || _lbxCampaignList.SelectedIndex >= _screenMissions.Count) return;

            XNAClientRatingBox ratingBox = (XNAClientRatingBox)sender;
            if (null != ratingBox)
            {
                string name = _screenMissions[_lbxCampaignList.SelectedIndex].SectionName;
                _scoreLevel = ratingBox.CheckedIndex + 1;
#if DEBUG
                Console.WriteLine("任务：{0},评分：{1}", name, _scoreLevel);
#endif
            }
        }

        private async Task UploadScore(string strName, int strScore)
        {
            //var uri = "https://dln1.yra2.com/Client/Scores/rating_index.php";
            //Dictionary<string, string> dic = new Dictionary<string, string>();
            //dic.Add("id", strName);
            //dic.Add("op", "upload");
            //dic.Add("score", strScore);
            //dic.Add("key", "r2e0u2n3i1o1n");
            //var response = await WebHelper.HttpGet(uri, dic);
            //if (!string.IsNullOrEmpty(response))
            //{
            //    Console.WriteLine(response);
            //}
            //return true;

            var score = new ClientCore.Entity.Score()
            {
                name = strName,
                score = strScore,
                total = 1
            };

          
            await NetWorkINISettings.Post<bool?>($"score/updateScore", score);
        }

        private void LbxCampaignListRightClick(object sender, EventArgs e)
        {
            if (_lbxCampaignList.HoveredIndex < 0 || _lbxCampaignList.HoveredIndex >= _lbxCampaignList.Items.Count)
                return;

            // if (string.IsNullOrEmpty(_screenMissions[_lbxCampaignList.HoveredIndex].Scenario))
            // {

            _lbxCampaignList.SelectedIndex = _lbxCampaignList.HoveredIndex;

            //if (!lbCampaignList.Items.Any(i => i.VisibilityChecker == null || i.VisibilityChecker()))
            //    return;

            //    toggleFavoriteItem.Text = GameModeMap.IsFavorite ? "Remove Favorite".L10N("UI:Main:RemoveFavorite") : "Add Favorite".L10N("UI:Main:AddFavorite");


            _campaignMenu.Open(GetCursorPoint());
            // }
        }

        private void DDDifficultySelectedIndexChanged(object sender, EventArgs e)
        {
            ScreenMission();
        }

        private async void LbxCampaignListSelectedIndexChanged(object sender, EventArgs e)
        {

            if (_lbxCampaignList.SelectedIndex == -1 || _lbxCampaignList.SelectedIndex >= _screenMissions.Count)
            {
                _tbMissionDescription.Text = string.Empty;
                _btnLaunch.AllowClick = false;
                _chkModify.Visible = false;
                _chkExtension.Visible = false;
                return;
            }
            Mission mission = _screenMissions[_lbxCampaignList.SelectedIndex];
            _tbMissionDescription.Text = mission.GUIDescription;
            // 如果不是任务
            if (string.IsNullOrEmpty(mission.Scenario))
            {
                //tbMissionDescription.Text = Path.D;
                _btnLaunch.AllowClick = false;
                return;
            }

            //如果任务没启用
            if (!mission.Enabled)
            {
                _btnLaunch.AllowClick = false;
                return;
            }

            _chkModify.Visible = true;
            _chkExtension.Visible = true;

            _missionIndex = _lbxCampaignList.SelectedIndex;
            //改变


            if (File.Exists(Path.Combine(ProgramConstants.GamePath, mission.Path, mission.Scenario)))
            { // 如果地图文件不在mix里,那么可以尝试分析和修改

                _chkModify.AllowChecking = true;
            }
            else
            {
                _chkModify.Checked = false;
                _chkModify.AllowChecking = false;
            }

            _mapPreviewBox.Visible = false;

            //重新加载Mod选择器
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

            if (_cmbGame.SelectedIndex == -1 || _cmbGame.SelectedItem == null)
                _cmbGame.SelectedIndex = 0;

            CmbGame_SelectedChanged(_cmbGame, null);

            _btnLaunch.AllowClick = true;

            _ = Task.Run(async () =>
            {
                //获取任务解析
                //  GetMissionInfo(false);

                if (!string.IsNullOrEmpty(mission.Scenario))
                {
                    string img = Path.Combine(ProgramConstants.GamePath, mission.Path,
                        mission.Scenario[..mission.Scenario.LastIndexOf('.')] + ".png");
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

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Enabled = false;

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
                XNAMessageBox.Show(WindowManager, "信息", result);
                return;
            }

            LaunchMission(mission);
        }

        private string LaunchCheck()
        {
            if (_chkModify.Checked)
                if (_cmbGame.SelectedItem == null)
                    return "请选择游戏";
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

        private Dictionary<string, Dictionary<string, int>> FileHash = [];
        private Dictionary<string, string> FilePaths = [];



        /// <summary>
        /// Starts a singleplayer Path.
        /// </summary>
        /// 

        private void LaunchMission(Mission mission)
        {
            if (_lbxCampaignList.SelectedIndex == -1 || _lbxCampaignList.SelectedIndex >= _screenMissions.Count) return;
            Logger.Log("About to write spawn.ini.");

            FileInfo spawnerSettingsFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SPAWNER_SETTINGS);

            var spawnIni = new IniFile(spawnerSettingsFile.FullName);

            spawnerSettingsFile.Delete();

            #region 切换文件

            string oldMain = spawnIni.GetValue("Settings", "Main", string.Empty);

            string newMain;
            string newGame;
            string newExtension = string.Empty;

            bool 加载音乐 = true;

            string newMission = mission.Path;
            //string newReName = "Mod&AI/Extension/rename_YR";
            //if(!mission.YR && ((Mod)_cmbGame.SelectedItem.Tag).md == string.Empty)
            //    newReName = "Mod&AI/Extension/rename_RA2";
            string newAi = "Mod&AI/AI/Other";
            
            Mod mod;

            if (_chkModify.Checked) {

                mod = ((Mod)_cmbGame.SelectedItem.Tag);

                 newMain = mission.YR ?"Mod&AI/Main/YR_YR":
                        (mod.md == "md" ?"Mod&AI/Main/RA2_YR":"Mod&AI/Main/RA2_RA2");

                newGame = mod.FilePath;
                if (_chkExtension.Checked)
                {
                    newExtension = mod.Extension;
                }
            }
            else {
               mod = Mod.Mods.Find(m => m.ID == _screenMissions[_lbxCampaignList.SelectedIndex].DefaultMod);
                newMain = mission.YR ? "Mod&AI/Main/YR_YR" :
                        (mod.md == "md" ? "Mod&AI/Main/RA2_YR" : "Mod&AI/Main/RA2_RA2");
                newGame = mod.FilePath;
                if (_chkExtension.Checked)
                {
                    newExtension = mod.Extension;
                }
            }
            
            
            bool 检测文件是否修改()
            {
                string oldGame = spawnIni.GetValue("Settings", "Game", string.Empty);

                string oldExtension = spawnIni.GetValue("Settings", "Extension", string.Empty);

                string oldMission = spawnIni.GetValue("Settings", "Mission", string.Empty);

                string oldAi = spawnIni.GetValue("Settings", "AI", string.Empty);

                if (oldGame != newGame && File.Exists($"{newGame}\\thememd.mix"))
                    加载音乐 = false;

                if (oldMain != newMain || oldGame != newGame || oldAi != newAi || oldMission != newMission || oldExtension != newExtension) return true;

                foreach(var fileType in FilePaths)
                {
                    if (!FileHash.TryGetValue(fileType.Key, out var value)) return true;
                    foreach(var file in Directory.GetFiles(fileType.Value))
                    {
                        if(!value.TryGetValue(file, out var hash)) return true;
                        var newHash = new FileInfo(file).GetHashCode();
                        if (hash != newHash) return true;
                    }
                }

                return false;
            }

            try
            {
               
                if (检测文件是否修改())
                {

                    GameOptionsPanel.清除缓存();

                    FileHelper.CopyDirectory(newGame, "./");
                    
                    foreach (var extension in newExtension.Split(","))
                    {
                        string directoryPath = $"Mod&AI/Extension/{extension}"; // 默认路径
                        if (extension.Contains("Ares"))
                        {
                            // 当extension为"Ares"，Child设置为"Ares3"，否则为extension本身
                            string extensionChild = extension == "Ares" ? "Ares3" : extension;
                            directoryPath = $"Mod&AI/Extension/Ares/{extensionChild}";
                        }
                        else if (extension.Contains("Phobos"))
                        {
                            // 当extension为"Phobos"，Child设置为"Phobos36"，否则为extension本身
                            string extensionChild = extension == "Phobos" ? "Phobos36" : extension;
                            directoryPath = $"Mod&AI/Extension/Phobos/{extensionChild}";
                        }
                        FileHelper.CopyDirectory(directoryPath, "./");
                    }

                    FileHelper.CopyDirectory(newAi, "./");

                    FileHelper.CopyDirectory(newMission, "./");

                    FileHelper.CopyDirectory(newMain, "./");

                    FilePaths["Game"] = newGame;
                    FilePaths["Main"] = newMain;
                    FilePaths["Mission"] = newMission;
                    FilePaths["AI"] = newAi;
                //    FilePaths["Extension"] = directoryPath;

                    foreach(var keyValue in FilePaths)
                    {
                        if (!FileHash.ContainsKey(keyValue.Key))
                            FileHash.Add(keyValue.Key, []);
                        foreach (var fileName in Directory.GetFiles(keyValue.Value))
                        {
                            var file = new FileInfo(fileName);
                            FileHash[keyValue.Key][fileName] = file.GetHashCode();
                        }
                    }
                }

                if(加载音乐)
                    Mix.PackToMix($"{ProgramConstants.GamePath}Resources/thememd/", "./thememd.mix");

                if (File.Exists("ra2md.csf"))
                {
                    var d = new CSF("ra2md.csf").GetCsfDictionary();
                    if (d != null)
                    {
                        foreach (var item in UserINISettings.Instance.MusicNameDictionary.Keys)
                        {
                            if (d.ContainsKey(item))
                            {
                                d[item] = UserINISettings.Instance.MusicNameDictionary[item];
                            }
                            else
                            {
                                d.Add(item, UserINISettings.Instance.MusicNameDictionary[item]);
                            }

                        }
                        CSF.WriteCSF(d, "ra2md.csf");
                    }
                 }
                }
            catch (FileLockedException ex)
            {
                XNAMessageBox.Show(WindowManager, "错误", ex.Message);
                return;
            }

            #endregion

            #region 写入新设置
            spawnIni = new IniFile(spawnerSettingsFile.FullName);
            var settings = new IniSection("Settings");

            settings.SetValue("Main", newMain);
            //写入新游戏
            settings.SetValue("Game", newGame);
            //写入新扩展
            settings.SetValue("Extension", newExtension);
            //写入新AI
            settings.SetValue("AI", newAi);

            settings.SetValue("Mission", newMission);

            
            //settings.SetValue("Ra2Mode", mod.md != "md");
            if(_chkExtension.Checked)
                settings.SetValue("Ra2Mode", mod.md != "md");
            else//这里不知为何一定得写False，即使是用原版玩，用True会弹窗
                settings.SetValue("Ra2Mode", false);

            if (_chkModify.Checked)
                settings.SetValue("Scenario", "spawnmap.ini");
            else
                settings.SetValue("Scenario",  mission.Scenario);

            settings.SetValue("CampaignID", mission.Index);
            settings.SetValue("IsSinglePlayer", true);
            settings.SetValue("SidebarHack", ClientConfiguration.Instance.SidebarHack);
            settings.SetValue("Side", mission.Side);
            settings.SetValue("BuildOffAlly", mission.BuildOffAlly);
            settings.SetValue("DifficultyModeHuman", (mission.PlayerAlwaysOnNormalDifficulty ? "1" : _trbDifficultySelector.Value.ToString()));
            settings.SetValue("DifficultyModeComputer", GetComputerDifficulty());
            spawnIni.AddSection(settings);

            UserINISettings.Instance.Difficulty.Value = _trbDifficultySelector.Value;

            //   spawnStreamWriter.WriteLine("DifficultyModeHuman=" + (Path.PlayerAlwaysOnNormalDifficulty ? "1" : trbDifficultySelector.Value.ToString()));
            //  spawnStreamWriter.WriteLine("DifficultyModeComputer=" + GetComputerDifficulty());

            var difficultyIni = new Rampastring.Tools.IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, DifficultyIniPaths[_trbDifficultySelector.Value]));
            string difficultyName = DifficultyNames[_trbDifficultySelector.Value];

            //  spawnStreamWriter.WriteLine();
            //  spawnStreamWriter.WriteLine();
            //  spawnStreamWriter.WriteLine();

            //如果开启修改
            if (_chkModify.Checked)
            {

                var mapIni = new Rampastring.Tools.IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, Path.Combine(mission.Path, mission.Scenario)));

                if (!_chkExtension.Checked && ((Mod)_cmbGame.SelectedItem.Tag).md == string.Empty)
                {
                    Rampastring.Tools.IniFile.ConsolidateIniFiles(mapIni, new Rampastring.Tools.IniFile("Resources/rules_repair_ra2.ini"));
                    Rampastring.Tools.IniFile.ConsolidateIniFiles(mapIni, new Rampastring.Tools.IniFile("Resources/repair_rules_ra2.ini"));
                   
                }

                if (!mapIni.SectionExists("General"))
                    mapIni.AddSection("General");
                if (mapIni.GetIntValue("General", "MaximumQueuedObjects", 0) == 0)
                    mapIni.SetIntValue("General", "MaximumQueuedObjects", 100);

                Rampastring.Tools.IniFile.ConsolidateIniFiles(mapIni, difficultyIni);
                Rampastring.Tools.IniFile.ConsolidateIniFiles(mapIni, new Rampastring.Tools.IniFile("Client/custom_rules_all.ini"));
                Rampastring.Tools.IniFile.ConsolidateIniFiles(mapIni, new Rampastring.Tools.IniFile("Resources/SkinRulesmd.ini"));

                foreach (GameLobbyCheckBox chkBox in CheckBoxes)
                {
                    chkBox.ApplySpawnINICode(spawnIni);
                    chkBox.ApplyMapCode(mapIni, null);
                }

                foreach (GameLobbyDropDown dd in DropDowns)
                {
                    dd.ApplySpawnIniCode(spawnIni);
                    dd.ApplyMapCode(mapIni, null);
                }
                if(_cmbCredits.SelectedItem != null)
                    Credits(mapIni, int.Parse(_cmbCredits.SelectedItem.Text)/100);

                mapIni.WriteIniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "spawnmap.ini"));

                if (((Mod)_cmbGame.SelectedItem.Tag).md == "md" && !mission.YR)
                {
                    var inifile = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "spawnmap.ini"));
                        inifile.RenameSection("Countries", "YBCountry");
                    inifile.WriteIniFile();
                }
            }
            
            spawnIni.WriteIniFile();
            UserINISettings.Instance.CampaignDefaultGameSpeed.Value = 6 - _cmbGameSpeed.SelectedIndex;
            UserINISettings.Instance.Difficulty.Value = _trbDifficultySelector.Value;
            UserINISettings.Instance.SaveSettings();
            #endregion

            SaveSettings();

            ((MainMenuDarkeningPanel)Parent).Hide();

            _discordHandler.UpdatePresence(mission.GUIName, difficultyName, mission.IconPath, true);


          
            oldSaves = Directory.GetFiles($"{ProgramConstants.GamePath}Saved Games");



            GameProcessLogic.GameProcessExited += GameProcessExited_Callback;

            GameProcessLogic.StartGameProcess(WindowManager);
        }

        private string[] oldSaves;

        private int GetComputerDifficulty() =>
            Math.Abs(_trbDifficultySelector.Value - 2);

        private void GameProcessExited_Callback()
        {
            var newSaves = Directory.GetFiles($"{ProgramConstants.GamePath}Saved Games");

            if (oldSaves.Length < newSaves.Length)
            {

                var iniFile = new IniFile($"{ProgramConstants.GamePath}Saved Games/Save.ini");
                var spawn = new IniFile($"{ProgramConstants.GamePath}spawn.ini");
                var game = spawn.GetValue("Settings", "Game", string.Empty);
                var main = spawn.GetValue("Settings", "Main", string.Empty);
                var mission = spawn.GetValue("Settings", "Mission", string.Empty);
                var extension = spawn.GetValue("Settings", "Extension", string.Empty);
                var ra2Mode = spawn.GetValue("Settings", "RA2Mode", false);
                var YR_to_RA2 = spawn.GetValue("Settings", "YR_to_RA2", false);
                // 找到在 newSaves 中但不在 oldSaves 中的文件
                var addedFiles = newSaves.Where(newFile => !oldSaves.Contains(newFile)).ToArray();

                foreach (var fileFullPath in addedFiles)
                {
                    string fileName = Path.GetFileName(fileFullPath);

                    iniFile.SetValue(fileName, "Game", game);
                    iniFile.SetValue(fileName, "Extension", extension);
                    iniFile.SetValue(fileName, "Main", main);
                    iniFile.SetValue(fileName, "Mission", mission);
                    iniFile.SetValue(fileName, "RA2Mode", YR_to_RA2);
                }
                iniFile.WriteIniFile();
            }


            WindowManager.AddCallback(new Action(GameProcessExited), null);
        }

        protected virtual void GameProcessExited()
        {
            GameProcessLogic.GameProcessExited -= GameProcessExited_Callback;
            // Logger.Log("GameProcessExited: Updating Discord Presence.");
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

                if (!string.IsNullOrEmpty(mission.IconPath))
                    item.Texture = AssetLoader.LoadTexture(mission.IconPath + "icon.png");

                _lbxCampaignList.AddItem(item);
            }
        }

        public void ReadMissionList()
        {

            MissionPack.MissionPacks.Clear();
            _missions.Clear();

            _ddSide.SelectedIndexChanged -= DDDifficultySelectedIndexChanged;
            _ddDifficulty.SelectedIndexChanged -= DDDifficultySelectedIndexChanged;
            _ddMissionPack.SelectedIndexChanged -= DDDifficultySelectedIndexChanged;

            _ddSide.Items.Clear();
            _ddDifficulty.Items.Clear();
            _ddMissionPack.Items.Clear();

            _ddDifficulty.AddItem(new XNADropDownItem() { Text = "筛选难度" });
            _ddSide.AddItem(new XNADropDownItem() { Text = "筛选阵营" });
            _ddMissionPack.AddItem(new XNADropDownItem() { Text = "选择任务包" });

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

            // 读取任务包
           

            if (battleIni.SectionExists("MissionPack"))
            {
                foreach (var s in battleIni.GetSectionKeys("MissionPack"))
                {
                    string value = battleIni.GetValue("MissionPack", s, string.Empty);
                    if (battleIni.SectionExists(value))
                    {
                        MissionPack missionPack = new MissionPack(battleIni, value);
                        MissionPack.MissionPacks.Add(missionPack);
                    }
                }
            }

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

    }
}

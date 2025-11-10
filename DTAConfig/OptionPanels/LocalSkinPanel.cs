using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ClientCore;
using ClientCore.Settings;
using ClientGUI;
using Localization;
using Localization.Tools;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAConfig.OptionPanels
{
    class LocalSkinPanel : XNAOptionsPanel
    {

        public LocalSkinPanel(WindowManager windowManager, UserINISettings iniSettings)
            : base(windowManager, iniSettings)
        {
        }

        private XNAListBox NameBox;
        private XNAClientDropDown DdSkin;
        private XNAClientDropDown DdScreen;
        private List<string> SkinName;
        private List<string[]> AllSkin;
        private XNAButton btnImage;
        private XNALabel lblselect;
        private XNALabel lblScreen;
        private XNAClientButton btnDefault;

        public StringListSetting Skin;

        private List<XNAClientDropDown> DropDowns = new List<XNAClientDropDown>();

        private XNALabel lblAllText;

        private LoadOrSaveSkinOptionPresetWindow loadOrSaveSkinOptionPresetWindow;

        protected XNAClientButton BtnSaveLoadSkinOptions { get; set; }

        private XNAContextMenu loadSaveSkinOptionsMenu { get; set; }
        public bool DisableSkinOptionUpdateBroadcast { get; private set; }

        private IniFile settingsIni;

        private string selectName;

        private string Folder;
        private string[] Options;
        private string[] Image;
        private string[] Types;
        public override void Initialize()
        {
            base.Initialize();

            settingsIni = new IniFile(ClientConfiguration.Instance.SkinIniName);
            Skin = new StringListSetting(settingsIni, "Skin", "Skin", new List<string>());

            btnImage = new XNAButton(WindowManager);
            btnImage.ClientRectangle = new Rectangle(190, 80, 400, 250);

            SkinName = GetSkinName("All");

            var info = GetAIISkin();
            if (info.Count > 0)
                selectName = info[0][5];

            DdScreen = new XNAClientDropDown(WindowManager);
            DdScreen.ClientRectangle = new Rectangle(150, 15, 100, 10);

            Name = "LocalSkinPanel";

            Types = GetTypes();

            lblAllText = new XNALabel(WindowManager);
            lblAllText.ClientRectangle = new Rectangle(420, 50, 0, 0);

            NameBox = new XNAListBox(WindowManager);
            NameBox.ClientRectangle = new Rectangle(30, 50, 100, 300);
            NameBox.FontIndex = 1;
            NameBox.LineHeight = 20;
            NameBox.SelectedIndexChanged += NameBox_SelectedChanged;
            XNADropDownItem item = new XNADropDownItem();
            item.Text = "All".L10N("UI:SkinType:All");
            item.Tag = "All";
            DdScreen.AddItem(item);
            for (int i = 0; i < Types.Length; i++)
            {
                XNADropDownItem item1 = new XNADropDownItem();
                item1.Text = Types[i].L10N("UI:SkinType:" + Types[i]);
                item1.Tag = Types[i];
                DdScreen.AddItem(item1);
            }


            for (int i = 0; i < SkinName.Count; i++)
            {
                string[] SelectSkin = GetSkinByName(SkinName[i]);
                DdSkin = new XNAClientDropDown(WindowManager);
                DdSkin.Tag = SelectSkin[5];
                Options = SelectSkin[2].Split(',');
                Folder = SelectSkin[1];
                Image = SelectSkin[4].Split(',');


                DdSkin.ClientRectangle = new Rectangle(250, 50, 100, 15);
                DdSkin.SelectedIndex = GetSkinBy(SelectSkin[5], "Select");
                DdSkin.Disable();

                DropDowns.Add(DdSkin);
                DdSkin.SelectedIndexChanged += DdSkin_SelectedChanged;
                AddChild(DdSkin);
                for (int j = 0; j < Options.Length; j++)
                    DdSkin.AddItem(Options[j].L10N("UI:SkinOpt:" + Options[j]));

                DdSkin.Name = SelectSkin[5];
            }


            DdScreen.SelectedIndexChanged += DdScreen_SelectedChanged;
            DdScreen.SelectedIndex = 0;

            NameBox.SelectedIndex = 0;
            lblScreen = new XNALabel(WindowManager)
            {
                Text = "Screening:".L10N("UI:Skin:Screening"),
                ClientRectangle = new Rectangle(30, 15, 0, 0)
            };


            lblselect = new XNALabel(WindowManager)
            {
                Text = "At present：".L10N("UI:Skin:At"),
                ClientRectangle = new Rectangle(150, 50, 0, 0)
            };

            btnDefault = new XNAClientButton(WindowManager);
            btnDefault.Name = nameof(btnDefault);
            btnDefault.IdleTexture = AssetLoader.LoadTexture("92pxbtn.png");
            btnDefault.HoverTexture = AssetLoader.LoadTexture("92pxbtn_c.png");
            btnDefault.Text = "Restore all defaults".L10N("UI:Skin:AllDef");
            btnDefault.ClientRectangle = new Rectangle(450, 15, 130, 25);
            btnDefault.LeftClick += btnDefaultLeftClick;

            AddChild(NameBox);
            AddChild(lblselect);
            AddChild(lblScreen);
            AddChild(DdScreen);
            AddChild(btnDefault);
            AddChild(btnImage);
            AddChild(lblAllText);
            InitializeSkinOptionPresetUI();
        }

        private void InitializeSkinOptionPresetUI()
        {
            BtnSaveLoadSkinOptions = new XNAClientButton(WindowManager);
            BtnSaveLoadSkinOptions.Name = nameof(BtnSaveLoadSkinOptions);
            BtnSaveLoadSkinOptions.IdleTexture = AssetLoader.LoadTexture("SkinoptionsButton.png");
            BtnSaveLoadSkinOptions.HoverTexture = AssetLoader.LoadTexture("SkinoptionsButton_c.png");
            BtnSaveLoadSkinOptions.ClientRectangle = new Rectangle(600, 16, 25, 25);

            loadOrSaveSkinOptionPresetWindow = new LoadOrSaveSkinOptionPresetWindow(WindowManager);
            loadOrSaveSkinOptionPresetWindow.Name = nameof(loadOrSaveSkinOptionPresetWindow);
            loadOrSaveSkinOptionPresetWindow.PresetLoaded += (sender, s) => HandleSkinOptionPresetLoadCommand(s);
            loadOrSaveSkinOptionPresetWindow.PresetSaved += (sender, s) => HandleSkinOptionPresetSaveCommand(s);
            loadOrSaveSkinOptionPresetWindow.Disable();

            var loadConfigMenuItem = new XNAContextMenuItem()
            {
                Text = "ReLoad".L10N("UI:Main:ButtonLoad"),
                SelectAction = () => loadOrSaveSkinOptionPresetWindow.Show(true)
            };
            var saveConfigMenuItem = new XNAContextMenuItem()
            {
                Text = "Save".L10N("UI:Main:ButtonSave"),
                SelectAction = () => loadOrSaveSkinOptionPresetWindow.Show(false)
            };

            loadSaveSkinOptionsMenu = new XNAContextMenu(WindowManager);
            loadSaveSkinOptionsMenu.Name = nameof(loadSaveSkinOptionsMenu);
            loadSaveSkinOptionsMenu.ClientRectangle = new Rectangle(0, 0, 75, 0);
            loadSaveSkinOptionsMenu.Items.Add(loadConfigMenuItem);
            loadSaveSkinOptionsMenu.Items.Add(saveConfigMenuItem);

            BtnSaveLoadSkinOptions.LeftClick += (sender, args) =>
                loadSaveSkinOptionsMenu.Open(GetCursorPoint());

            AddChild(BtnSaveLoadSkinOptions);
            AddChild(loadSaveSkinOptionsMenu);
            AddChild(loadOrSaveSkinOptionPresetWindow);
            
        }

        public bool LoadSkinOptionPreset(string name)
        {
            SkinOptionPreset preset = SkinOptionPresets.Instance.GetPreset(name);
            if (preset == null)
                return false;

            DisableSkinOptionUpdateBroadcast = true;

            var dropDownValues = preset.GetSkinValues();
            foreach (var kvp in dropDownValues)
            {
                XNAClientDropDown dropDown = DropDowns.Find(d => d.Name == kvp.Key);
                if (dropDown != null && dropDown.AllowDropDown)
                    dropDown.SelectedIndex = kvp.Value;
            }

            DisableSkinOptionUpdateBroadcast = false;

            return true;
        }

        protected void HandleSkinOptionPresetSaveCommand(SkinOptionPresetEventArgs e) => HandleSkinOptionPresetSaveCommand(e.PresetName);

        protected void HandleSkinOptionPresetSaveCommand(string presetName)
        {
            string error = AddSkinOptionPreset(presetName);
            if (!string.IsNullOrEmpty(error))
                Logger.Log(error);
        }

        protected void HandleSkinOptionPresetLoadCommand(SkinOptionPresetEventArgs e) => HandleSkinOptionPresetLoadCommand(e.PresetName);

        protected void HandleSkinOptionPresetLoadCommand(string presetName)
        {
            if (LoadSkinOptionPreset(presetName))
                Logger.Log("Game option preset loaded succesfully.".L10N("UI:Main:PresetLoaded"));
            else
                Logger.Log(string.Format("Preset {0} not found!".L10N("UI:Main:PresetNotFound"), presetName));
        }

        protected string AddSkinOptionPreset(string name)
        {
            string error = SkinOptionPreset.IsNameValid(name);
            if (!string.IsNullOrEmpty(error))
                return error;

            var preset = new SkinOptionPreset(name);

            foreach (XNADropDown dropDown in DropDowns)
            {
                preset.AddskinValue(dropDown.Name, dropDown.SelectedIndex);
            }

            SkinOptionPresets.Instance.AddPreset(preset);
            return null;
        }

        public override void Load()
        {
            base.Load();
            OptionsWindow.UseSkin = false;
            foreach (XNAClientDropDown dd in DropDowns)
            {
                dd.SelectedIndex = int.Parse(GetAIISkin().Find(s => s[5] == dd.Name)[3]);
                if (dd.SelectedIndex != 0)
                    OptionsWindow.UseSkin = true;
            }

        }

        public override bool Save()
        {

            File.WriteAllText("Resources\\SkinRulesmd.ini", ";皮肤Rules" + Environment.NewLine);
            File.WriteAllText("Resources\\SkinArtmd.ini", ";皮肤Art" + Environment.NewLine);
            if (!Directory.Exists("./Resources/SkinCashe"))
                Directory.CreateDirectory("./Resources/SkinCashe");

            DirectoryInfo directoryInfo = new DirectoryInfo("./Resources/SkinCashe");

            foreach (var file in directoryInfo.GetFiles())
            {
                file.Delete();
            }

            if (File.Exists($"./Resources/{ProgramConstants.SKIN_MIX}"))
                File.Delete($"./Resources/{ProgramConstants.SKIN_MIX}");

            List<string[]> Rules = new List<string[]>();
            List<string[]> Art = new List<string[]>();

            foreach (XNAClientDropDown dd in DropDowns)
            {
                //  List<string> file = new List<string>();

                string[] file = GetSkin((string)dd.Tag);
                List<string> del = new List<string>();
                int oldselect = int.Parse(file[3]);
                if (file != null)
                {
             

                    if (file[7] != "" && file[7].Split('|')[dd.SelectedIndex] != "")
                    {
                        Rules.Add(File.ReadAllLines(file[1] + file[10].Split(',')[dd.SelectedIndex] + "/" + file[7].Split('|')[dd.SelectedIndex], Encoding.UTF8));
                    }
                    if (file[8] != "" && file[8].Split('|')[dd.SelectedIndex] != "")
                    {
                        Art.Add(File.ReadAllLines(file[1] + file[10].Split(',')[dd.SelectedIndex] + "/" + file[8].Split('|')[dd.SelectedIndex], Encoding.UTF8));
                    }

                    FileHelper.CopyDirectory(file[1] + file[10].Split(',')[dd.SelectedIndex], "./Resources/SkinCashe");

                }

                SetSkinIndex(dd.Name, dd.SelectedIndex);
                settingsIni.WriteIniFile();
            }

            //PackToMix(ProgramConstants.游戏目录 + "Skin\\", "aaa.mix");

            for (int i = 0; i < Rules.Count; i++)
            {
                File.AppendAllLines("Resources\\SkinRulesmd.ini", Rules[i]);
            }
            for (int i = 0; i < Art.Count; i++)
            {
                File.AppendAllLines("Resources\\SkinArtmd.ini", Art[i]);
            }

            string[] mixFiles = Directory.GetFiles("./Resources/SkinCashe", "*.mix");



            if (mixFiles.Length > 0)
            {
                foreach (string mixFile in mixFiles)
                {
                    string fileName = Path.GetFileName(mixFile);
                    string destinationPath = Path.Combine("./", fileName);

                    File.Move(mixFile, destinationPath, true);
                    //  Console.WriteLine($"已将文件 {fileName} 移动到目标目录。");
                }
            }

            string mixFilePath = Path.Combine("./Resources/SkinCashe", "uimd.ini");

            // 检查文件是否存在
            if (File.Exists(mixFilePath))
            {
                string fileName = Path.GetFileName(mixFilePath);
                string destinationPath = Path.Combine("./", fileName);

                // 移动文件
                File.Move(mixFilePath, destinationPath, true);
                // Console.WriteLine($"已将文件 {fileName} 移动到目标目录。");
            }


            Mix.PackToMix("./Resources/SkinCashe", $"./Resources/{ProgramConstants.SKIN_MIX}");

            if (File.Exists($"./{ProgramConstants.SKIN_MIX}"))
                File.Delete($"./{ProgramConstants.SKIN_MIX}");

            File.Move($"./Resources/{ProgramConstants.SKIN_MIX}", $"./{ProgramConstants.SKIN_MIX}");
            //   iniSettings.SaveSettings();

            OptionsWindow.UseSkin = false;
            foreach (XNAClientDropDown dd in DropDowns)
            {
                if (dd.SelectedIndex != 0)
                {
                    OptionsWindow.UseSkin = true;
                    break;
                }
            }

            return false;
        }

        private void btnDefaultLeftClick(object sender, EventArgs e)
        {
            XNAMessageBox messageBox = new XNAMessageBox(WindowManager, "Restore default".L10N("UI:Main:Default"), "Are you sure you want to restore all skin effects to default?".L10N("UI:Main:AllDefault"), XNAMessageBoxButtons.YesNo);
            messageBox.Show();
            messageBox.YesClickedAction += Default_YesClicked;
        }

        private void Default_YesClicked(XNAMessageBox messageBox)
        {

            foreach (XNAClientDropDown dd in DropDowns)
            {
                dd.SelectedIndex = 0;
            }
            XNAMessageBox Box = new XNAMessageBox(WindowManager, "Restore default".L10N("UI:Main:Default"), "Operation successful, if regret can click cancel.".L10N("UI:Skin:Successful"), XNAMessageBoxButtons.OK);
            Box.Show();
        }


        public void DdScreen_SelectedChanged(object sender, EventArgs e)
        {
            NameBox.Items.Clear();
            //NameBox.Clear();

            SkinName = GetSkinName((string)DdScreen.SelectedItem.Tag);

            for (int i = 0; i < SkinName.Count; i++)
            {
                XNAListBoxItem item = new XNAListBoxItem();
                item.Tag = GetSkinByName(SkinName[i])[5];
                item.Text = SkinName[i].L10N("UI:SkinName:" + item.Tag);
                NameBox.AddItem(item);
            }

            NameBox.SelectedIndex = 0;
            NameBox.SelectedIndexChanged += NameBox_SelectedChanged;
            NameBox.OnSelectedChanged();

            NameBox_SelectedChanged(sender, e);
        }

        public void NameBox_SelectedChanged(object sender, EventArgs e)
        {
            if (NameBox.SelectedItem == null)
            {

                return;
            }
            //  List<string> file = new List<string>();
            Tag = (string)NameBox.SelectedItem.Tag;

            string[] SelectSkin = GetSkin((string)Tag);
            Folder = SelectSkin[1];
            Image = SelectSkin[4].Split(',');
            //设置描述语句
            if (SelectSkin[9] != "")
                lblAllText.Text = SelectSkin[9].L10N("UI:SkinAllText:" + Tag);
            else
                lblAllText.Text = SelectSkin[0].L10N("UI:SkinText:" + Tag);

            //隐藏上一个选项，显示下一个选项
            DropDowns.Find(p => p.Name == selectName).Disable();
            DropDowns.Find(p => p.Name == (string)NameBox.SelectedItem.Tag).Enable();

            //标记当前选项
            selectName = (string)NameBox.SelectedItem.Tag;


            DdSkin_SelectedChanged(DropDowns.Find(p => p.Name == (string)NameBox.SelectedItem.Tag), e);

        }

        public void DdSkin_SelectedChanged(object sender, EventArgs e)
        {
            btnImage.IdleTexture = AssetLoader.LoadTexture(Folder + Image[DropDowns.Find(p => p.Name == (string)NameBox.SelectedItem.Tag).SelectedIndex]);
        }

        public string[] GetSkin(string Name)
        {
            AllSkin = GetAIISkin();

            for (int i = 0; i < AllSkin.Count; i++)
            {
                if (AllSkin[i][5] == Name)
                    return AllSkin[i];
            }
            return null;
        }
        public string[] GetSkinByName(string Name)
        {
            AllSkin = GetAIISkin();

            for (int i = 0; i < AllSkin.Count; i++)
            {
                if (AllSkin[i][0] == Name)
                    return AllSkin[i];
            }
            return null;
        }


        public string[] GetTypes()
        {
            List<string> SkinList = Skin.Value;

            List<string> Types = new List<string>();
            for (int i = 0; i < SkinList.Count; i++)
            {
                foreach (string type in new StringSetting(settingsIni, SkinList[i], "Type", "").Value.Split(','))
                    Types.Add(type);
            }
            return Types.ToArray().GroupBy(p => p).Select(p => p.Key).ToArray();
        }

        public int GetSkinBy(string name, string m)
        {

            return new IntSetting(settingsIni, name, m, 0).Value;
        }

        public List<string> GetSkinName(string Types)
        {
            List<string> SkinList = Skin.Value;

            List<string> SkinName = new List<string>();

            for (int i = 0; i < SkinList.Count; i++)
            {

                string s = new StringSetting(settingsIni, SkinList[i], "Type", "").Value;
                if (Types == "All" || s.IndexOf(Types) != -1)
                    SkinName.Add(new StringSetting(settingsIni, SkinList[i], "Text", "").Value);
            }
            return SkinName;
        }


        public List<string[]> GetAIISkin()
        {
            List<string> SkinList = Skin.Value;

            List<string[]> AllSkin = new List<string[]>();

            for (int i = 0; i < SkinList.Count; i++)
            {
                string[] skin = new string[11];
                skin[0] = new StringSetting(settingsIni, SkinList[i], "Text", "").Value.ToString();
                skin[1] = new StringSetting(settingsIni, SkinList[i], "Folder", "").Value.ToString();
                skin[2] = new StringSetting(settingsIni, SkinList[i], "Options", "").Value.ToString();
                skin[3] = new StringSetting(settingsIni, SkinList[i], "Select", "").Value.ToString();
                skin[4] = new StringSetting(settingsIni, SkinList[i], "Image", "").Value.ToString();
                skin[5] = SkinList[i];
                skin[6] = new StringSetting(settingsIni, SkinList[i], "Delete", "").Value.ToString();
                skin[7] = new StringSetting(settingsIni, SkinList[i], "RulesIni", "").Value.ToString();
                skin[8] = new StringSetting(settingsIni, SkinList[i], "ArtIni", "").Value.ToString();
                skin[9] = new StringSetting(settingsIni, SkinList[i], "AllText", "").Value.ToString();
                skin[10] = new StringSetting(settingsIni, SkinList[i], "Index", "").Value.ToString();
                AllSkin.Add(skin);
            }
            return AllSkin;
        }

        public List<string> GetSkinIni(string types)
        {
            List<string> SkinList = Skin.Value;
            List<string> rules = new List<string>();
            for (int i = 0; i < SkinList.Count; i++)
            {
                rules.Add(new StringSetting(settingsIni, SkinList[i], types, "").Value);
            }
            return rules;
        }

        public void SetSkinIndex(string name, int value)
        {
            settingsIni.SetIntValue(name, "Select", value);
        }
    }


    public class LoadOrSaveSkinOptionPresetWindow : XNAWindow
    {


        private bool _isLoad;

        private readonly XNALabel lblHeader;

        private readonly XNADropDownItem ddiCreatePresetItem;

        private readonly XNADropDownItem ddiSelectPresetItem;

        private readonly XNAClientButton btnLoadSave;

        private readonly XNAClientButton btnDelete;

        private readonly XNAClientDropDown ddPresetSelect;

        private readonly XNALabel lblNewPresetName;

        private readonly XNATextBox tbNewPresetName;

        public EventHandler<SkinOptionPresetEventArgs> PresetLoaded;

        public EventHandler<SkinOptionPresetEventArgs> PresetSaved;

        public LoadOrSaveSkinOptionPresetWindow(WindowManager windowManager) : base(windowManager)
        {
            ClientRectangle = new Rectangle(0, 0, 325, 185);

            var margin = 10;

            lblHeader = new XNALabel(WindowManager);
            lblHeader.Name = nameof(lblHeader);
            lblHeader.FontIndex = 1;
            lblHeader.ClientRectangle = new Rectangle(
                margin, margin,
                150, 22
            );

            var lblPresetName = new XNALabel(WindowManager);
            lblPresetName.Name = nameof(lblPresetName);
            lblPresetName.Text = "Preset Name".L10N("UI:Main:PresetName");
            lblPresetName.ClientRectangle = new Rectangle(
                margin, lblHeader.Bottom + margin,
                150, 18
            );

            ddiCreatePresetItem = new XNADropDownItem();
            ddiCreatePresetItem.Text = "[Create New]".L10N("UI:Main:CreateNewPreset");

            ddiSelectPresetItem = new XNADropDownItem();
            ddiSelectPresetItem.Text = "[Select Preset]".L10N("UI:Main:SelectPreset");
            ddiSelectPresetItem.Selectable = false;

            ddPresetSelect = new XNAClientDropDown(WindowManager);
            ddPresetSelect.Name = nameof(ddPresetSelect);
            ddPresetSelect.ClientRectangle = new Rectangle(
                10, lblPresetName.Bottom + 2,
                150, 22
            );
            ddPresetSelect.SelectedIndexChanged += DropDownPresetSelect_SelectedIndexChanged;

            lblNewPresetName = new XNALabel(WindowManager);
            lblNewPresetName.Name = nameof(lblNewPresetName);
            lblNewPresetName.Text = "New Preset Name".L10N("UI:Main:NewPresetName");
            lblNewPresetName.ClientRectangle = new Rectangle(
                margin, ddPresetSelect.Bottom + margin,
                150, 18
            );

            tbNewPresetName = new XNATextBox(WindowManager);
            tbNewPresetName.Name = nameof(tbNewPresetName);
            tbNewPresetName.ClientRectangle = new Rectangle(
                10, lblNewPresetName.Bottom + 2,
                150, 22
            );
            tbNewPresetName.TextChanged += (sender, args) => RefreshButtons();

            btnLoadSave = new XNAClientButton(WindowManager);
            btnLoadSave.Name = nameof(btnLoadSave);
            btnLoadSave.LeftClick += BtnLoadSave_LeftClick;
            btnLoadSave.ClientRectangle = new Rectangle(
            margin,
                Height - UIDesignConstants.BUTTON_HEIGHT - margin,
                UIDesignConstants.BUTTON_WIDTH_92,
                UIDesignConstants.BUTTON_HEIGHT
            );

            btnDelete = new XNAClientButton(WindowManager);
            btnDelete.Name = nameof(btnDelete);
            btnDelete.Text = "Delete".L10N("UI:Main:ButtonDelete");
            btnDelete.LeftClick += BtnDelete_LeftClick;
            btnDelete.ClientRectangle = new Rectangle(
                btnLoadSave.Right + margin,
                btnLoadSave.Y,
                UIDesignConstants.BUTTON_WIDTH_92,
                UIDesignConstants.BUTTON_HEIGHT
            );

            var btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Text = "Cancel".L10N("UI:Main:ButtonCancel");
            btnCancel.ClientRectangle = new Rectangle(
                btnDelete.Right + margin,
                btnLoadSave.Y,
                UIDesignConstants.BUTTON_WIDTH_92,
                UIDesignConstants.BUTTON_HEIGHT
            );
            btnCancel.LeftClick += (sender, args) => Disable();

            AddChild(lblHeader);
            AddChild(lblPresetName);
            AddChild(ddPresetSelect);
            AddChild(lblNewPresetName);
            AddChild(tbNewPresetName);
            AddChild(btnLoadSave);
            AddChild(btnDelete);
            AddChild(btnCancel);

            Disable();
        }

        public override void Initialize()
        {
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 255), 1, 1);

            base.Initialize();
        }

        /// <summary>
        /// Show the window.
        /// </summary>
        /// <param name="isLoad">The "mode" for the window: load vs save.</param>
        public void Show(bool isLoad)
        {
            _isLoad = isLoad;
            lblHeader.Text = _isLoad ? "ReLoad Preset".L10N("UI:Main:LoadPreset") : "Save Preset".L10N("UI:Main:SavePreset");
            btnLoadSave.Text = _isLoad ? "ReLoad".L10N("UI:Main:ButtonLoad") : "Save".L10N("UI:Main:ButtonSave");

            if (_isLoad)
                ShowLoad();
            else
                ShowSave();

            RefreshButtons();
            CenterOnParent();
            Enable();
        }

        /// <summary>
        /// Callback when the Preset drop down selection has changed
        /// </summary>
        private void DropDownPresetSelect_SelectedIndexChanged(object sender, EventArgs eventArgs)
        {
            if (!_isLoad)
                DropDownPresetSelect_SelectedIndexChanged_IsSave();

            RefreshButtons();
        }

        /// <summary>
        /// Callback when the Preset drop down selection has changed during "save" mode
        /// </summary>
        private void DropDownPresetSelect_SelectedIndexChanged_IsSave()
        {
            if (IsCreatePresetSelected)
            {
                // show the field to specify a new name when "create" option is selected in drop down
                tbNewPresetName.Enable();
                lblNewPresetName.Enable();
            }
            else
            {
                // hide the field to specify a new name when an existing preset is selected
                tbNewPresetName.Disable();
                lblNewPresetName.Disable();
            }
        }

        /// <summary>
        /// Refresh the state of the load/save button
        /// </summary>
        private void RefreshButtons()
        {
            if (_isLoad)
                btnLoadSave.Enabled = !IsSelectPresetSelected;
            else
                btnLoadSave.Enabled = !IsCreatePresetSelected || !IsNewPresetNameFieldEmpty;

            btnDelete.Enabled = !IsCreatePresetSelected && !IsSelectPresetSelected;
        }

        private bool IsCreatePresetSelected => ddPresetSelect.SelectedItem == ddiCreatePresetItem;
        private bool IsSelectPresetSelected => ddPresetSelect.SelectedItem == ddiSelectPresetItem;
        private bool IsNewPresetNameFieldEmpty => string.IsNullOrWhiteSpace(tbNewPresetName.Text);

        /// <summary>
        /// Populate the preset drop down from saved presets
        /// </summary>
        private void LoadPresets()
        {
            ddPresetSelect.Items.Clear();
            ddPresetSelect.Items.Add(_isLoad ? ddiSelectPresetItem : ddiCreatePresetItem);
            ddPresetSelect.SelectedIndex = 0;

            ddPresetSelect.Items.AddRange(SkinOptionPresets.Instance
                .GetPresetNames()
                .OrderBy(name => name)
                .Select(name => new XNADropDownItem()
                {
                    Text = name
                }));
        }

        /// <summary>
        /// Show the current window in the "load" mode context
        /// </summary>
        private void ShowLoad()
        {
            LoadPresets();

            // do not show fields to specify a preset name during "load" mode
            lblNewPresetName.Disable();
            tbNewPresetName.Disable();
        }

        /// <summary>
        /// Show the current window in the "save" mode context
        /// </summary>
        private void ShowSave()
        {
            LoadPresets();

            // show fields to specify a preset name during "save" mode
            lblNewPresetName.Enable();
            tbNewPresetName.Enable();
            tbNewPresetName.Text = string.Empty;
        }

        private void BtnLoadSave_LeftClick(object sender, EventArgs e)
        {
            var selectedItem = ddPresetSelect.Items[ddPresetSelect.SelectedIndex];
            if (_isLoad)
            {
                PresetLoaded?.Invoke(this, new SkinOptionPresetEventArgs(selectedItem.Text));
            }
            else
            {
                var presetName = IsCreatePresetSelected ? tbNewPresetName.Text : selectedItem.Text;
                PresetSaved?.Invoke(this, new SkinOptionPresetEventArgs(presetName));
            }

            Disable();
        }

        private void BtnDelete_LeftClick(object sender, EventArgs e)
        {
            var selectedItem = ddPresetSelect.Items[ddPresetSelect.SelectedIndex];
            var messageBox = XNAMessageBox.ShowYesNoDialog(WindowManager,
                "Confirm Preset Delete".L10N("UI:Main:ConfirmPresetDeleteTitle"),
                "Are you sure you want to delete this preset?".L10N("UI:Main:ConfirmPresetDeleteText") + "\n\n" + selectedItem.Text);
            messageBox.YesClickedAction = box =>
            {
                SkinOptionPresets.Instance.DeletePreset(selectedItem.Text);
                ddPresetSelect.Items.Remove(selectedItem);
                ddPresetSelect.SelectedIndex = 0;
            };
        }
    }

    /// <summary>
    /// A single game option preset.
    /// </summary>
    public class SkinOptionPreset
    {
        public SkinOptionPreset(string profileName)
        {
            ProfileName = profileName;

            if (ProfileName.Contains('[') || ProfileName.Contains(']'))
                throw new ArgumentException("Game option preset name cannot contain the [] characters.");
        }

        /// <summary>
        /// Checks if a specific name is valid for the name of a game option preset.
        /// Returns null if the name is valid, an error message otherwise.
        /// </summary>
        public static string IsNameValid(string name)
        {
            if (name.Contains('[') || name.Contains(']'))
                return "Game option preset name cannot contain the [] characters.";

            return null;
        }

        public string ProfileName { get; }


        private Dictionary<string, int> skinValues = new Dictionary<string, int>();

        private void AddValues<T>(IniSection section, string keyName, Dictionary<string, T> dictionary, Converter<string, T> converter)
        {
            string[] valueStrings = section.GetValue(keyName,
                string.Empty).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string value in valueStrings)
            {
                string[] splitValue = value.Split(':');
                if (splitValue.Length != 2)
                {
                    Logger.Log($"Failed to parse game option preset value ({ProfileName}, {keyName})");
                    continue;
                }

                dictionary.Add(splitValue[0], converter(splitValue[1]));
            }
        }


        public void AddskinValue(string skinValue, int value)
        {
            skinValues.Add(skinValue, value);
        }

        public Dictionary<string, int> GetSkinValues() => new Dictionary<string, int>(skinValues);

        public void Read(IniSection section)
        {
            // Syntax example:
            // CheckBoxValues=chkCrates:1,chkShortGame:1,chkFastResourceGrowth:0,.... (0 = unchecked, 1 = checked)
            // DropDownValues=ddTechLevel:7,ddStartingCredits:5,... (the number is the selected option index)

            AddValues(section, "SkinValues", skinValues, s => Conversions.IntFromString(s, 0));
        }

        public void Write(IniSection section)
        {
            section.SetValue("SkinValues", string.Join(",",
                skinValues.Select(s => $"{s.Key}:{s.Value.ToString()}")));
        }
    }

    /// <summary>
    /// Handles game option presets.
    /// </summary>
    public class SkinOptionPresets
    {
        private const string IniFileName = "SkinOptionsPresets.ini";
        private const string PresetDefinitionsSectionName = "Presets";

        private SkinOptionPresets() { }

        private static SkinOptionPresets _instance;
        public static SkinOptionPresets Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SkinOptionPresets();

                return _instance;
            }
        }

        private IniFile SkinOptionPresetsIni;
        private Dictionary<string, SkinOptionPreset> presets;

        public SkinOptionPreset GetPreset(string name)
        {
            LoadIniIfNotInitialized();

            if (presets.TryGetValue(name, out SkinOptionPreset value))
                return value;

            return null;
        }

        public List<string> GetPresetNames()
        {
            LoadIniIfNotInitialized();

            return presets.Keys
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .ToList();
        }

        public void AddPreset(SkinOptionPreset preset)
        {
            LoadIniIfNotInitialized();

            presets[preset.ProfileName] = preset;
            WriteIni();
        }

        public void DeletePreset(string name)
        {
            LoadIniIfNotInitialized();

            if (!presets.ContainsKey(name))
                return;

            presets.Remove(name);
            WriteIni();
        }

        private void LoadIniIfNotInitialized()
        {
            if (SkinOptionPresetsIni == null)
                LoadIni();
        }

        private void LoadIni()
        {
            SkinOptionPresetsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, IniFileName));
            presets = new Dictionary<string, SkinOptionPreset>();

            var presetsDefinitions = SkinOptionPresetsIni.GetSection(PresetDefinitionsSectionName);
            if (presetsDefinitions == null)
                return;

            foreach (var kvp in presetsDefinitions.Keys)
            {
                if (!presets.ContainsKey(kvp.Key))
                {
                    var presetSection = SkinOptionPresetsIni.GetSection(kvp.Key);
                    if (presetSection == null)
                        continue;

                    var preset = new SkinOptionPreset(kvp.Key);
                    preset.Read(presetSection);
                    presets[kvp.Key] = preset;
                }
            }
        }

        private void WriteIni()
        {
            SkinOptionPresetsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, IniFileName));
            int i = 0;
            var definitionsSection = new IniSection(PresetDefinitionsSectionName);
            SkinOptionPresetsIni.AddSection(definitionsSection);
            foreach (var kvp in presets)
            {
                definitionsSection.SetValue(i.ToString(), kvp.Value.ProfileName);
                var presetSection = new IniSection(kvp.Value.ProfileName);
                kvp.Value.Write(presetSection);
                SkinOptionPresetsIni.AddSection(presetSection);
                i++;
            }

            SkinOptionPresetsIni.WriteIniFile();
        }
    }

    public class SkinOptionPresetEventArgs : EventArgs
    {
        public string PresetName { get; }

        public SkinOptionPresetEventArgs(string presetName)
        {
            PresetName = presetName;
        }
    }
}
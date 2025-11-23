using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using ClientCore;
using ClientGUI;
using Localization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAConfig.OptionPanels
{
    class DisplayOptionsPanel : XNAOptionsPanel
    {
        private const int DRAG_DISTANCE_DEFAULT = 4;
        private const int ORIGINAL_RESOLUTION_WIDTH = 640;
        private const string RENDERERS_INI = "Renderers.ini";

        public DisplayOptionsPanel(WindowManager windowManager, UserINISettings iniSettings)
            : base(windowManager, iniSettings)
        {
        }

        private XNAClientCheckBox chkRandom_wallpaper;
        private XNAClientDropDown ddIngameResolution;
        private XNAClientDropDown ddDetailLevel;
        private XNAClientDropDown ddRenderer;
        private XNAClientCheckBox chkWindowedMode;
        private XNAClientCheckBox chkBorderlessWindowedMode;
        private XNAClientCheckBox chk跳过启动动画;
        private XNAClientCheckBox chkBackBufferInVRAM;
        private XNAClientPreferredItemDropDown ddClientResolution;
        private XNAClientCheckBox chkBorderlessClient;
        private XNAClientDropDown ddClientTheme;
        private XNAClientDropDown ddLanguage;
        private XNAClientDropDown ddStart;
        private XNAClientCheckBox chkCustomIngameResolution;
        protected XNATextBox tbIngameResolutionX;
        protected XNATextBox tbIngameResolutionY;
        protected XNALabel lblCustomIngameResolution;


        private List<DirectDrawWrapper> renderers;

        private string defaultRenderer;
        private DirectDrawWrapper selectedRenderer = null;


        public override void Initialize()
        {
            base.Initialize();

            Name = "DisplayOptionsPanel";

            // 游戏分辨率
            var lblIngameResolution = new XNALabel(WindowManager);
            lblIngameResolution.Name = nameof(lblIngameResolution);
            lblIngameResolution.ClientRectangle = new Rectangle(12, 14, 0, 0);
            lblIngameResolution.Text = "In-game Resolution:".L10N("UI:DTAConfig:InGameResolution");
            AddChild(lblIngameResolution);
            
            tbIngameResolutionX = new XNATextBox(WindowManager);
            tbIngameResolutionX.Name = "tbIngameResolutionX";
            tbIngameResolutionX.ClientRectangle = new Rectangle(
                lblIngameResolution.Right + 3,
                lblIngameResolution.Y - 2, 69, 22);
            tbIngameResolutionX.Disable();
            AddChild(tbIngameResolutionX);

            lblCustomIngameResolution = new XNALabel(WindowManager);
            lblCustomIngameResolution.Name = nameof(lblCustomIngameResolution);
            lblCustomIngameResolution.ClientRectangle = new Rectangle(lblIngameResolution.Right + 80, lblIngameResolution.Y , 0, 0);
            lblCustomIngameResolution.Text = "x";
            lblCustomIngameResolution.Disable();
            AddChild(lblCustomIngameResolution);

            tbIngameResolutionY = new XNATextBox(WindowManager);
            tbIngameResolutionY.Name = "tbIngameResolutionY";
            tbIngameResolutionY.ClientRectangle = new Rectangle(
                lblCustomIngameResolution.Right + 7,
                lblIngameResolution.Y - 2, 69, 22);
            tbIngameResolutionY.Disable();
            AddChild(tbIngameResolutionY);

            ddIngameResolution = new XNAClientDropDown(WindowManager);
            ddIngameResolution.Name = nameof(ddIngameResolution);
            ddIngameResolution.ClientRectangle = new Rectangle(lblIngameResolution.Right + 12,lblIngameResolution.Y - 2, 120, 19);
            AddChild(ddIngameResolution);

            var clientConfig = ClientConfiguration.Instance;
            var resolutions = GetResolutions(clientConfig.MinimumIngameWidth,
                clientConfig.MinimumIngameHeight,
                clientConfig.MaximumIngameWidth, clientConfig.MaximumIngameHeight);
            resolutions.Sort();
            foreach (var res in resolutions)
                ddIngameResolution.AddItem(res.ToString());
                
            chkCustomIngameResolution = new XNAClientCheckBox(WindowManager);
            chkCustomIngameResolution.Name = nameof(chkCustomIngameResolution);
            chkCustomIngameResolution.ClientRectangle = new Rectangle(lblIngameResolution.X,
                ddIngameResolution.Bottom + 10, 0, 0);
            chkCustomIngameResolution.Text = "Customize the game resolution".L10N("UI:DTAConfig:CustomizethegameResolution");
            chkCustomIngameResolution.CheckedChanged += ChkCustomIngameResolution_CheckedChanged;
            chkCustomIngameResolution.Checked = false;
            AddChild(chkCustomIngameResolution);

            // 客户端分辨率
            var lblClientResolution = new XNALabel(WindowManager);
            lblClientResolution.Name = nameof(lblClientResolution);
            lblClientResolution.ClientRectangle = new Rectangle(ddIngameResolution.Right + 120, ddIngameResolution.Top, 0, 0);
            lblClientResolution.Text = "Client Resolution:".L10N("UI:DTAConfig:ClientResolution");
            AddChild(lblClientResolution);
            
            ddClientResolution = new XNAClientPreferredItemDropDown(WindowManager);
            ddClientResolution.Name = nameof(ddClientResolution);
            ddClientResolution.ClientRectangle = new Rectangle(lblClientResolution.Right + 12,lblClientResolution.Y - 2, 160, ddIngameResolution.Height);
            ddClientResolution.AllowDropDown = false;
            ddClientResolution.PreferredItemLabel = "(recommended)".L10N("UI:DTAConfig:Recommended");
            AddChild(ddClientResolution);

            // Add "optimal" client resolutions for windowed mode
            // if they're not supported in fullscreen mode
            AddResolutionIfFitting(1024, 600, resolutions);
            AddResolutionIfFitting(1024, 720, resolutions);
            AddResolutionIfFitting(1280, 600, resolutions);
            AddResolutionIfFitting(1280, 720, resolutions);
            AddResolutionIfFitting(1280, 768, resolutions);
            resolutions.Sort();
            foreach (var res in resolutions)
            {
                var item = new XNADropDownItem();
                item.Text = res.ToString();
                item.Tag = res.ToString();
                ddClientResolution.AddItem(item);
            }

            int width = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            int height = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            resolutions = GetResolutions(1280, 768, width, height);

            // So we add the optimal resolutions to the list, sort it and then find
            // out the optimal resolution index - it's inefficient, but works

            string[] recommendedResolutions = clientConfig.RecommendedResolutions;

            foreach (string resolution in recommendedResolutions)
            {
                string trimmedresolution = resolution.Trim();
                int index = resolutions.FindIndex(res => res.ToString() == trimmedresolution);
                if (index > -1)
                    ddClientResolution.PreferredItemIndexes.Add(index);
            }

            // 画面精细度
            var lblDetailLevel = new XNALabel(WindowManager);
            lblDetailLevel.Name = nameof(lblDetailLevel);
            lblDetailLevel.ClientRectangle = new Rectangle(lblIngameResolution.X, lblIngameResolution.Bottom + 48, 0, 0);
            lblDetailLevel.Text = "Detail Level:".L10N("UI:DTAConfig:DetailLevel");
            AddChild(lblDetailLevel);

            ddDetailLevel = new XNAClientDropDown(WindowManager);
            ddDetailLevel.Name = nameof(ddDetailLevel);
            ddDetailLevel.ClientRectangle = new Rectangle(lblDetailLevel.Right + 12, lblDetailLevel.Y - 2, ddIngameResolution.Width, ddIngameResolution.Height);
            ddDetailLevel.AddItem("Low".L10N("UI:DTAConfig:DetailLevelLow"));
            ddDetailLevel.AddItem("Medium".L10N("UI:DTAConfig:DetailLevelMedium"));
            ddDetailLevel.AddItem("High".L10N("UI:DTAConfig:DetailLevelHigh"));
            AddChild(ddDetailLevel);

            // 渲染器(ddraw)
            var lblRenderer = new XNALabel(WindowManager);
            lblRenderer.Name = nameof(lblRenderer);
            lblRenderer.ClientRectangle = new Rectangle(ddDetailLevel.Right + 120, ddDetailLevel.Top, 0, 0);
            lblRenderer.Text = "Renderer:".L10N("UI:DTAConfig:Renderer");
            AddChild(lblRenderer);
            
            ddRenderer = new XNAClientDropDown(WindowManager);
            ddRenderer.Name = nameof(ddRenderer);
            ddRenderer.ClientRectangle = new Rectangle(lblRenderer.Right + 12, lblRenderer.Y - 2, 175, ddDetailLevel.Height);
            GetRenderers();
            AddChild(ddRenderer);
            
            var localOS = ClientConfiguration.Instance.GetOperatingSystemVersion();
            foreach (var renderer in renderers)
            {
                if (renderer.IsCompatibleWithOS(localOS) && !renderer.Hidden)
                {
                    ddRenderer.AddItem(new XNADropDownItem()
                    {
                        Text = renderer.UIName,
                        Tag = renderer
                    });
                }
            }

            // 窗口模式
            chkWindowedMode = new XNAClientCheckBox(WindowManager);
            chkWindowedMode.Name = nameof(chkWindowedMode);
            chkWindowedMode.ClientRectangle = new Rectangle(lblDetailLevel.X, lblDetailLevel.Bottom + 16, 0, 0);
            chkWindowedMode.Text = "Windowed Mode".L10N("UI:DTAConfig:WindowedMode");
            chkWindowedMode.CheckedChanged += ChkWindowedMode_CheckedChanged;
            AddChild(chkWindowedMode);
           
            // 无窗口模式
            chkBorderlessWindowedMode = new XNAClientCheckBox(WindowManager);
            chkBorderlessWindowedMode.Name = nameof(chkBorderlessWindowedMode);
            chkBorderlessWindowedMode.ClientRectangle = new Rectangle(lblRenderer.X, lblRenderer.Bottom + 16, 0, 0);
            chkBorderlessWindowedMode.Text = "Borderless Windowed Mode".L10N("UI:DTAConfig:BorderlessWindowedMode");
            chkBorderlessWindowedMode.AllowChecking = false;
            AddChild(chkBorderlessWindowedMode);

     

            // 客户端全屏
            chkBorderlessClient = new XNAClientCheckBox(WindowManager);
            chkBorderlessClient.Name = nameof(chkBorderlessClient);
            chkBorderlessClient.ClientRectangle = new Rectangle(lblClientResolution.X, lblClientResolution.Bottom + 16, 0, 0);
            chkBorderlessClient.Text = "Fullscreen Client".L10N("UI:DTAConfig:FullscreenClient");
            chkBorderlessClient.CheckedChanged += ChkBorderlessMenu_CheckedChanged;
            chkBorderlessClient.Checked = true;
            AddChild(chkBorderlessClient);

            // 语言
            var lblLanguage = new XNALabel(WindowManager);
            lblLanguage.Name = nameof(lblLanguage);
            lblLanguage.ClientRectangle = new Rectangle(chkBorderlessWindowedMode.X, chkBorderlessWindowedMode.Bottom + 16, 0, 0);
            lblLanguage.Text = "Language:".L10N("UI:Main:Language");
            AddChild(lblLanguage);
            
            ddLanguage = new XNAClientDropDown(WindowManager);
            ddLanguage.Name = nameof(ddLanguage);
            ddLanguage.ClientRectangle = new Rectangle(lblLanguage.Right + 12, lblLanguage.Y - 2, 160, ddRenderer.Height);
            AddChild(ddLanguage);
            
            int languageCount = ClientConfiguration.Instance.LanguageCount;
            if (languageCount == 0)
            {
                lblLanguage.Visible = false;
                ddLanguage.Visible = false;
            }
            for (int i = 0; i < languageCount; i++)
            {
                XNADropDownItem item1 = new XNADropDownItem();
                item1.Text = ClientConfiguration.Instance.GetLanguageInfoFromIndex(i)[0].L10N("UI:Language:" + ClientConfiguration.Instance.GetLanguageInfoFromIndex(i)[0]);
                item1.Tag = ClientConfiguration.Instance.GetLanguageInfoFromIndex(i)[0];
                ddLanguage.AddItem(item1);
            }

            // 主题
            var lblClientTheme = new XNALabel(WindowManager);
            lblClientTheme.Name = nameof(lblClientTheme);
            lblClientTheme.ClientRectangle = new Rectangle(lblLanguage.X, lblLanguage.Bottom + 16, 0, 0);
            lblClientTheme.Text = "Client Theme:".L10N("UI:DTAConfig:ClientTheme");
            AddChild(lblClientTheme);
            
            ddClientTheme = new XNAClientDropDown(WindowManager);
            ddClientTheme.Name = nameof(ddClientTheme);
            ddClientTheme.ClientRectangle = new Rectangle(lblClientTheme.Right + 12, lblClientTheme.Top - 2, ddClientResolution.Width, ddRenderer.Height);
            AddChild(ddClientTheme);

            int themeCount = ClientConfiguration.Instance.ThemeCount;
            for (int i = 0; i < themeCount; i++)
            {
                XNADropDownItem item1 = new XNADropDownItem();
                item1.Text = ClientConfiguration.Instance.GetThemeInfoFromIndex(i)[0].L10N("UI:Themes:" + ClientConfiguration.Instance.GetThemeInfoFromIndex(i)[0]);
                item1.Tag = ClientConfiguration.Instance.GetThemeInfoFromIndex(i)[1];
                // Console.WriteLine(item1.Tag.ToString());

                if (Directory.Exists("Resources\\" + ClientConfiguration.Instance.GetThemeInfoFromIndex(i)[1]))
                    ddClientTheme.AddItem(item1);
            }

            // 随机启动封面
            chkRandom_wallpaper = new XNAClientCheckBox(WindowManager);
            chkRandom_wallpaper.Name = nameof(chkRandom_wallpaper);
            chkRandom_wallpaper.ClientRectangle = new Rectangle(chkWindowedMode.X, chkWindowedMode.Bottom + 16, 0, 0);
            chkRandom_wallpaper.Text = "Random start cover".L10N("UI:Main:RanWall");
            chkRandom_wallpaper.Checked = false;
            AddChild(chkRandom_wallpaper);

            chk跳过启动动画 = new XNAClientCheckBox(WindowManager)
            {
                Name = nameof(chk跳过启动动画),
                ClientRectangle = new Rectangle(chkBorderlessWindowedMode.X, chkRandom_wallpaper.Y, 0, 0),
                Text = "跳过启动动画"
            };

            AddChild(chk跳过启动动画);

            // 随机启动封面->壁纸or视频
            var lblStart = new XNALabel(WindowManager);
            lblStart.Name = nameof(lblStart);
            lblStart.Text = "Load:".L10N("UI:Main:Load");
            lblStart.ClientRectangle = new Rectangle(chkRandom_wallpaper.Right + 50, chkRandom_wallpaper.Y, 0, 0);
            lblStart.Visible = false;
            AddChild(lblStart);

            ddStart = new XNAClientDropDown(WindowManager);
            ddStart.Name = nameof(ddStart);
            ddStart.ClientRectangle = new Rectangle(lblStart.Right + 30, lblStart.Y - 2, 60, ddRenderer.Height);
            ddStart.AddItem("Image".L10N("UI:Main:Image"));
            ddStart.AddItem("Video".L10N("UI:Main:Video"));
            ddStart.SelectedChanged += DdStart_SelectedChanged;
            ddStart.Visible = false;
            AddChild(ddStart);

            var btnOpen = new XNAButton(WindowManager);
            btnOpen.Name = nameof(btnOpen);
            btnOpen.Text = "Open the location".L10N("UI:Main:Openthelocation");
            btnOpen.ClientRectangle = new Rectangle(ddStart.Right + 30, lblStart.Y, 50, ddRenderer.Height);
            btnOpen.LeftClick += BtnOpen_LeftClick;
            AddChild(btnOpen);
            btnOpen.Visible = false;

            // 双缓冲模式
            chkBackBufferInVRAM = new XNAClientCheckBox(WindowManager);
            chkBackBufferInVRAM.Name = nameof(chkBackBufferInVRAM);
            chkBackBufferInVRAM.ClientRectangle = new Rectangle(chkRandom_wallpaper.X, chkRandom_wallpaper.Bottom + 16, 0, 0);
            chkBackBufferInVRAM.Text = ("Back Buffer in Video Memory" + Environment.NewLine +
                "(lower performance, but is" + Environment.NewLine + "necessary on some systems)").L10N("UI:DTAConfig:BackBuffer");
            AddChild(chkBackBufferInVRAM);
        }

        private void BtnOpen_LeftClick(object sender, EventArgs e)
        {
            if (ddStart.SelectedIndex == 0)
                Process.Start("explorer", $"{ProgramConstants.GamePath}Resources\\{ddClientTheme.SelectedItem.Tag}Wallpaper\\");
            else if(File.Exists($"{ProgramConstants.GamePath}Resources\\{UserINISettings.Instance.ClientTheme}loading.mp4"))
                Process.Start("explorer", $"/select,{ProgramConstants.GamePath}Resources\\{UserINISettings.Instance.ClientTheme}loading.mp4");
            else
                Process.Start("explorer", $"/select,{ProgramConstants.GamePath}Resources\\loading.mp4");
        }

        private void DdStart_SelectedChanged(object sender, EventArgs e)
        {
            if (ddStart.SelectedIndex != 0)
            {
                chkRandom_wallpaper.Enabled = false;
            }
            else
                chkRandom_wallpaper.Enabled = true;
        }

        /// <summary>
        /// Adds a screen resolution to a list of resolutions if it fits on the screen.
        /// Checks if the resolution already exists before adding it.
        /// </summary>
        /// <param name="width">The width of the new resolution.</param>
        /// <param name="height">The height of the new resolution.</param>
        /// <param name="resolutions">A list of screen resolutions.</param>
        private void AddResolutionIfFitting(int width, int height, List<ScreenResolution> resolutions)
        {
            if (resolutions.Find(res => res.Width == width && res.Height == height) != null)
                return;

            int currentWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            int currentHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

            if (currentWidth >= width && currentHeight >= height)
            {
                resolutions.Add(new ScreenResolution(width, height));
            }
        }

        private void GetRenderers()
        {
            renderers = new List<DirectDrawWrapper>();

            var renderersIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), RENDERERS_INI));

            var keys = renderersIni.GetSectionKeys("Renderers");
            if (keys == null)
                throw new ClientConfigurationException("[Renderers] not found from Renderers.ini!");

            foreach (string key in keys)
            {
                string internalName = renderersIni.GetStringValue("Renderers", key, string.Empty);

                var ddWrapper = new DirectDrawWrapper(internalName, renderersIni);
                renderers.Add(ddWrapper);
            }

            OSVersion osVersion = ClientConfiguration.Instance.GetOperatingSystemVersion();

            defaultRenderer = renderersIni.GetStringValue("DefaultRenderer", osVersion.ToString(), string.Empty);

            if (defaultRenderer == null)
                throw new ClientConfigurationException("Invalid or missing default renderer for operating system: " + osVersion);

            string renderer = UserINISettings.Instance.Renderer;

            selectedRenderer = renderers.Find(r => r.InternalName == renderer);

            if (selectedRenderer == null)
                selectedRenderer = renderers.Find(r => r.InternalName == defaultRenderer);

            if (selectedRenderer == null)
                throw new ClientConfigurationException("Missing renderer: " + renderer);

            GameProcessLogic.UseQres = selectedRenderer.UseQres;
            GameProcessLogic.SingleCoreAffinity = selectedRenderer.SingleCoreAffinity;
        }

        private void ChkCustomIngameResolution_CheckedChanged(object sender, EventArgs e)
        {
            if (chkCustomIngameResolution.Checked)
            {
                ddIngameResolution.AllowDropDown = false;
                ddIngameResolution.Disable();
                lblCustomIngameResolution.Enable();
                tbIngameResolutionX.Enable();
                tbIngameResolutionX.Text = UserINISettings.Instance.IngameScreenWidth.Value.ToString();
                tbIngameResolutionY.Enable();
                tbIngameResolutionY.Text = UserINISettings.Instance.IngameScreenHeight.Value.ToString();
            }
            else
            {
                lblCustomIngameResolution.Disable();
                tbIngameResolutionX.Disable();
                tbIngameResolutionY.Disable();
                ddIngameResolution.AllowDropDown = true;
                ddIngameResolution.Enable();
            }
        }

        private void ChkBorderlessMenu_CheckedChanged(object sender, EventArgs e)
        {
            if (chkBorderlessClient.Checked)
            {
                ddClientResolution.AllowDropDown = false;
                string nativeRes = Screen.PrimaryScreen.Bounds.Width + "x" + Screen.PrimaryScreen.Bounds.Height;

                int nativeResIndex = ddClientResolution.Items.FindIndex(i => (string)i.Tag == nativeRes);
                if (nativeResIndex > -1)
                    ddClientResolution.SelectedIndex = nativeResIndex;
            }
            else
            {
                ddClientResolution.AllowDropDown = true;
                if (ddClientResolution.PreferredItemIndexes.Count > 0)
                {
                    int optimalWindowedResIndex = ddClientResolution.PreferredItemIndexes[0];
                    ddClientResolution.SelectedIndex = optimalWindowedResIndex;
                }
            }
        }

        private void ChkWindowedMode_CheckedChanged(object sender, EventArgs e)
        {
            if (chkWindowedMode.Checked)
            {
                chkBorderlessWindowedMode.AllowChecking = true;
                return;
            }

            chkBorderlessWindowedMode.AllowChecking = false;
            chkBorderlessWindowedMode.Checked = false;
        }

        /// <summary>
        /// Loads the user's preferred renderer.
        /// </summary>
        private void LoadRenderer()
        {
            int index = ddRenderer.Items.FindIndex(
                           r => ((DirectDrawWrapper)r.Tag).InternalName == selectedRenderer.InternalName);

            if (index < 0 && selectedRenderer.Hidden)
            {
                ddRenderer.AddItem(new XNADropDownItem()
                {
                    Text = selectedRenderer.UIName,
                    Tag = selectedRenderer
                });
                index = ddRenderer.Items.Count - 1;
            }

            ddRenderer.SelectedIndex = index;
        }

        public override void Load()
        {
            base.Load();

            LoadRenderer();
            ddDetailLevel.SelectedIndex = UserINISettings.Instance.DetailLevel;

            
            //string currentRes = UserINISettings.Instance.IngameScreenWidth.Value +
            //    "x" + UserINISettings.Instance.IngameScreenHeight.Value;

            //int index = ddIngameResolution.Items.FindIndex(i => i.Text == currentRes);

            //ddIngameResolution.SelectedIndex = index > -1 ? index : 0;
            
            if (IniSettings.CustonIngameResolution.Value)
            {
                chkCustomIngameResolution.Checked = true;

                int closestIndex = 0;
                int minDiff = int.MaxValue;
                for (int i = 0; i < ddIngameResolution.Items.Count; i++)
                {
                    int x = int.Parse(ddIngameResolution.Items[i].Text.Split('x')[0]);
                    int y = int.Parse(ddIngameResolution.Items[i].Text.Split('x')[1]);
                    int diff = Math.Abs(UserINISettings.Instance.IngameScreenWidth.Value + UserINISettings.Instance.IngameScreenHeight.Value - x - y);
                    if (diff < minDiff)
                    {
                        minDiff = diff;
                        closestIndex = i;
                    }
                }
                ddIngameResolution.SelectedIndex = closestIndex;
            }
            else
            {
                string currentRes = UserINISettings.Instance.IngameScreenWidth.Value +
                    "x" + UserINISettings.Instance.IngameScreenHeight.Value;

                int index = ddIngameResolution.Items.FindIndex(i => i.Text == currentRes);

                ddIngameResolution.SelectedIndex = index > -1 ? index : 0;
            }

            // Wonder what this "Win8CompatMode" actually does...
            // Disabling it used to be TS-DDRAW only, but it was never enabled after 
            // you had tried TS-DDRAW once, so most players probably have it always
            // disabled anyway
            IniSettings.Win8CompatMode.Value = "No";

            var renderer = (DirectDrawWrapper)ddRenderer.SelectedItem.Tag;

            if (renderer.UsesCustomWindowedOption())
            {
                // For renderers that have their own windowed mode implementation
                // enabled through their own config INI file
                // (for example DxWnd and CnC-DDRAW)

                IniFile rendererSettingsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "Resources\\Render", renderer.InternalName, renderer.ConfigFileName));

                chkWindowedMode.Checked = rendererSettingsIni.GetBooleanValue(renderer.WindowedModeSection,
                    renderer.WindowedModeKey, false);

                if (!string.IsNullOrEmpty(renderer.BorderlessWindowedModeKey))
                {
                    bool setting = rendererSettingsIni.GetBooleanValue(renderer.WindowedModeSection,
                        renderer.BorderlessWindowedModeKey, false);
                    chkBorderlessWindowedMode.Checked = renderer.IsBorderlessWindowedModeKeyReversed ? !setting : setting;
                }
                else
                {
                    chkBorderlessWindowedMode.Checked = UserINISettings.Instance.BorderlessWindowedMode;
                }
            }
            else
            {
                chkWindowedMode.Checked = UserINISettings.Instance.WindowedMode;
                chkBorderlessWindowedMode.Checked = UserINISettings.Instance.BorderlessWindowedMode;
            }

            // 随机壁纸
            chkRandom_wallpaper.Checked = UserINISettings.Instance.Random_wallpaper;
            chk跳过启动动画.Checked = UserINISettings.Instance.跳过启动动画;
            ddStart.SelectedIndex = UserINISettings.Instance.video_wallpaper ? 1 : 0;
            int selectedLanguageIndex = ddLanguage.Items.FindIndex(
                ddi => (string)ddi.Tag == UserINISettings.Instance.Language);
            ddLanguage.SelectedIndex = selectedLanguageIndex > -1 ? selectedLanguageIndex : 0;

            string currentClientRes = IniSettings.ClientResolutionX.Value + "x" + IniSettings.ClientResolutionY.Value;

            int clientResIndex = ddClientResolution.Items.FindIndex(i => (string)i.Tag == currentClientRes);

            ddClientResolution.SelectedIndex = clientResIndex > -1 ? clientResIndex : 0;

            chkBorderlessClient.Checked = UserINISettings.Instance.BorderlessWindowedClient;

            int selectedThemeIndex = ddClientTheme.Items.FindIndex(
                ddi => (string)ddi.Tag == UserINISettings.Instance.ClientTheme);
            ddClientTheme.SelectedIndex = selectedThemeIndex > -1 ? selectedThemeIndex : 0;
            chkBackBufferInVRAM.Checked = UserINISettings.Instance.BackBufferInVRAM;
        }

        public override bool Save()
        {
            bool restartRequired = base.Save();

            IniSettings.DetailLevel.Value = ddDetailLevel.SelectedIndex;

            //string[] resolution = ddIngameResolution.SelectedItem.Text.Split('x');

            //int[] ingameRes = [int.Parse(resolution[0]), int.Parse(resolution[1])];
            
            int[] ingameRes = new int[2];
            if (chkCustomIngameResolution.Checked)
            {
                ingameRes = new int[2] { int.Parse(tbIngameResolutionX.Text), int.Parse(tbIngameResolutionY.Text) };
                IniSettings.CustonIngameResolution.Value = true;
            }
            else
            {
                IniSettings.CustonIngameResolution.Value = false;
                string[] resolution = ddIngameResolution.SelectedItem.Text.Split('x');
                ingameRes = new int[2] { int.Parse(resolution[0]), int.Parse(resolution[1]) };
            }

            IniSettings.IngameScreenWidth.Value = ingameRes[0];
            IniSettings.IngameScreenHeight.Value = ingameRes[1];

            //同步RF和MD配置文件配置分辨率不一致问题 By 彼得兔 2024/01/06
            //var mdIniFile = new IniFile(SafePath.CombineFilePath(ProgramConstants.游戏目录, "RA2RF.ini"));
            //var videoSec = mdIniFile.GetSection("Video");
            //videoSec.SetIntValue("ScreenWidth", IniSettings.IngameScreenWidth.Value);
            //videoSec.SetIntValue("ScreenHeight", IniSettings.IngameScreenHeight.Value);
            //mdIniFile.WriteIniFile();

            // Calculate drag selection distance, scale it with resolution width
            int dragDistance = ingameRes[0] / ORIGINAL_RESOLUTION_WIDTH * DRAG_DISTANCE_DEFAULT;
            IniSettings.DragDistance.Value = dragDistance;

            DirectDrawWrapper originalRenderer = selectedRenderer;
            selectedRenderer = (DirectDrawWrapper)ddRenderer.SelectedItem.Tag;

            IniSettings.WindowedMode.Value = chkWindowedMode.Checked &&
                !selectedRenderer.UsesCustomWindowedOption();

            IniSettings.BorderlessWindowedMode.Value = chkBorderlessWindowedMode.Checked &&
                string.IsNullOrEmpty(selectedRenderer.BorderlessWindowedModeKey);

            string[] clientResolution = ((string)ddClientResolution.SelectedItem.Tag).Split('x');

            int[] clientRes = new int[2] { int.Parse(clientResolution[0]), int.Parse(clientResolution[1]) };

            if (clientRes[0] != IniSettings.ClientResolutionX.Value ||
                clientRes[1] != IniSettings.ClientResolutionY.Value)
                restartRequired = true;

            IniSettings.ClientResolutionX.Value = clientRes[0];
            IniSettings.ClientResolutionY.Value = clientRes[1];

            IniSettings.video_wallpaper.Value = ddStart.SelectedIndex == 0 ? false : true;

            if (IniSettings.BorderlessWindowedClient.Value != chkBorderlessClient.Checked)
                restartRequired = true;

            IniSettings.BorderlessWindowedClient.Value = chkBorderlessClient.Checked;

            if (UserINISettings.Instance.Language != "")
            {
                string language = ClientConfiguration.Instance.GetLanguagePath(UserINISettings.Instance.Language);
                if (language == null)
                {
                    language = ClientConfiguration.Instance.GetLanguageInfoFromIndex(0)[1];
                }
                else
                {
                    language = ClientConfiguration.Instance.GetLanguageInfoFromIndex(ddLanguage.SelectedIndex)[1];
                }

                if (IniSettings.Language != (string)ddLanguage.SelectedItem.Tag)
                {

                    File.Delete(ProgramConstants.GamePath + "cameo..mix");
                    File.Delete(ProgramConstants.GamePath + "cameomd.mix");
                    File.Delete(ProgramConstants.GamePath + "ra2md.csf");
                    FileHelper.CopyDirectory(language, "./");
                    restartRequired = true;
                    
                }
                IniSettings.Language.Value = (string)ddLanguage.SelectedItem.Tag;
            }

            if (IniSettings.ClientTheme != (string)ddClientTheme.SelectedItem.Tag)
            {

                restartRequired = true;
            }

            IniSettings.ClientTheme.Value = (string)ddClientTheme.SelectedItem.Tag;

            //随机壁纸
            IniSettings.Random_wallpaper.Value = chkRandom_wallpaper.Checked;
            IniSettings.BackBufferInVRAM.Value = chkBackBufferInVRAM.Checked;

            IniSettings.跳过启动动画.Value = chk跳过启动动画.Checked;

            if (selectedRenderer != originalRenderer ||
                !SafePath.GetFile(ProgramConstants.GamePath, selectedRenderer.ConfigFileName).Exists)
            {
                foreach (var renderer in renderers)
                {
                    if (renderer != selectedRenderer)
                        renderer.Clean();
                }
            }

            selectedRenderer.Apply();

            GameProcessLogic.UseQres = selectedRenderer.UseQres;
            GameProcessLogic.SingleCoreAffinity = selectedRenderer.SingleCoreAffinity;

            if (selectedRenderer.UsesCustomWindowedOption())
            {
                IniFile rendererSettingsIni = new IniFile(SafePath.CombineFilePath(Path.Combine(ProgramConstants.GamePath, "Resources\\Render", selectedRenderer.InternalName), selectedRenderer.ConfigFileName));

                rendererSettingsIni.SetBooleanValue(selectedRenderer.WindowedModeSection,
                    selectedRenderer.WindowedModeKey, chkWindowedMode.Checked);

                if (!string.IsNullOrEmpty(selectedRenderer.BorderlessWindowedModeKey))
                {
                    bool borderlessModeIniValue = chkBorderlessWindowedMode.Checked;
                    if (selectedRenderer.IsBorderlessWindowedModeKeyReversed)
                        borderlessModeIniValue = !borderlessModeIniValue;

                    rendererSettingsIni.SetBooleanValue(selectedRenderer.WindowedModeSection,
                        selectedRenderer.BorderlessWindowedModeKey, borderlessModeIniValue);
                }

                rendererSettingsIni.WriteIniFile();
            }

            IniSettings.Renderer.Value = selectedRenderer.InternalName;
            return restartRequired;
        }

        private List<ScreenResolution> GetResolutions(int minWidth, int minHeight, int maxWidth, int maxHeight)
        {
            var screenResolutions = new List<ScreenResolution>();

            foreach (DisplayMode dm in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
            {
                if (dm.Width < minWidth || dm.Height < minHeight || dm.Width > maxWidth || dm.Height > maxHeight)
                    continue;

                var resolution = new ScreenResolution(dm.Width, dm.Height);

                if (screenResolutions.Find(res => res.Equals(resolution)) != null)
                    continue;

                screenResolutions.Add(resolution);
            }

            return screenResolutions;
        }

        /// <summary>
        /// A single screen resolution.
        /// </summary>
        sealed class ScreenResolution : IComparable<ScreenResolution>
        {
            public ScreenResolution(int width, int height)
            {
                Width = width;
                Height = height;
            }

            /// <summary>
            /// The width of the resolution in pixels.
            /// </summary>
            public int Width { get; set; }

            /// <summary>
            /// The height of the resolution in pixels.
            /// </summary>
            public int Height { get; set; }

            public override string ToString()
            {
                return Width + "x" + Height;
            }

            public int CompareTo(ScreenResolution res2)
            {
                if (this.Width < res2.Width)
                    return -1;
                else if (this.Width > res2.Width)
                    return 1;
                else // equal
                {
                    if (this.Height < res2.Height)
                        return -1;
                    else if (this.Height > res2.Height)
                        return 1;
                    else return 0;
                }
            }

            public override bool Equals(object obj)
            {
                var resolution = obj as ScreenResolution;

                if (resolution == null)
                    return false;

                return CompareTo(resolution) == 0;
            }

            public override int GetHashCode()
            {
                return new { Width, Height }.GetHashCode();
            }
        }
    }
}

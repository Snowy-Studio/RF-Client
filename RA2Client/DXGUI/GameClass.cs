using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.CnCNet5;
using ClientCore.Settings;
using ClientGUI;
using ClientGUI.IME;
using DTAConfig;
using DTAConfig.Settings;
using DTAConfig.OptionPanels;
using Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Ra2Client.Domain;
using Ra2Client.DXGUI.Generic;
using Ra2Client.Domain.Multiplayer;
using Ra2Client.Domain.Multiplayer.CnCNet;
using Ra2Client.DXGUI.Multiplayer;
using Ra2Client.DXGUI.Multiplayer.CnCNet;
using Ra2Client.DXGUI.Multiplayer.GameLobby;
using Ra2Client.Online;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace Ra2Client.DXGUI
{
    /// <summary>
    /// The main class for the game. Sets up asset search paths
    /// and initializes components.
    /// </summary>
    public class GameClass : Game
    {
        private HotKey hotKey = null;
        private const string GAME_TITLE = "Red Alert 2 Reunion the Future";

        public GameClass()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.SynchronizeWithVerticalRetrace = false;
            graphics.HardwareModeSwitch = false;
            content = new ContentManager(Services);

            
        }

        ~GameClass() 
        {
            if (null != hotKey)
            {
                hotKey.OnHotkey -= HotKey_OnHotkey;
                hotKey.Clear();
            }
        }

        private static GraphicsDeviceManager graphics;
        ContentManager content;

       public void ChangeTiTle(string s = "")
        {
            string windowTitle = ClientConfiguration.Instance.WindowTitle;
            // 在主线程上执行 UI 更新逻辑
           
                Window.Title = $"{windowTitle} ver{Updater.GameVersion} {s}";
        }

        protected override void Initialize()
        {
            Logger.Log("加载客户端UI。");

            ChangeTiTle();

            
            base.Initialize();

            AssetLoader.Initialize(GraphicsDevice, content);
            AssetLoader.AssetSearchPaths.Add(ProgramConstants.GetResourcePath());
            AssetLoader.AssetSearchPaths.Add(ProgramConstants.GetBaseResourcePath());
            AssetLoader.AssetSearchPaths.Add(ProgramConstants.GamePath);

            try
            {
                Texture2D texture = new Texture2D(GraphicsDevice, 100, 100, false, SurfaceFormat.Color);
                Color[] colorArray = new Color[100 * 100];
                texture.SetData(colorArray);

                _ = AssetLoader.LoadTextureUncached("checkBoxClear.png");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("DeviceRemoved"))
                {
                    Logger.Log($"Creating texture on startup failed! Creating .dxfail file and re-launching client launcher.");

                    DirectoryInfo clientDirectory = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath);

                    if (!clientDirectory.Exists)
                        clientDirectory.Create();

                    // Create startup failure file that the launcher can check for this error
                    // and handle it by redirecting the user to another version instead

                    File.WriteAllBytes(SafePath.CombineFilePath(clientDirectory.FullName, ".dxfail"), new byte[] { 1 });

                    string launcherExe = ClientConfiguration.Instance.LauncherExe;
                    if (string.IsNullOrEmpty(launcherExe))
                    {
                        // LauncherExe is unspecified, just throw the exception forward
                        // because we can't handle it

                        Logger.Log("No LauncherExe= specified in ClientDefinitions.ini! " +
                            "Forwarding exception to regular exception handler.");

                        throw;
                    }
                    else
                    {
                        Logger.Log("Starting " + launcherExe + " and exiting.");

                        Process.Start(SafePath.CombineFilePath(ProgramConstants.GamePath, launcherExe));
                        Environment.Exit(1);
                    }
                }
            }

            InitializeUISettings();



            WindowManager wm = new(this, graphics);
            wm.Initialize(content, ProgramConstants.GetBaseResourcePath());

            IMEHandler imeHandler = IMEHandler.Create(this);

            if (UserINISettings.Instance.IMEEnabled.Value)
            {
                wm.IMEHandler = imeHandler;

                wm.WindowSizeChangedByUser += (sender, e) =>
                {
                    imeHandler.SetIMETextInputRectangle(wm);
                };
            }

            WindowManager.标题改变 += ChangeTiTle;
            // ClientConfiguration.标题改变 += ChangeTiTle;

            //注册隐藏全局快捷键
            if (null == hotKey)
            {
                hotKey = new HotKey(Window.Handle);
                hotKey.OnHotkey += HotKey_OnHotkey;
                // 注册全局快捷键: 隐藏窗口 (Alt+H)
                bool bRet = hotKey.Regist((uint)(HotKey.HotkeyModifiers.Alt), (uint)System.Windows.Forms.Keys.H);
                if (!bRet)
                    Logger.Log("Regist Hidden Key Error, Please Change To Other Key");

                // 注册全局快捷键: 显示窗口 (Alt+J)
                bRet = hotKey.Regist((uint)(HotKey.HotkeyModifiers.Alt), (uint)System.Windows.Forms.Keys.J);
                if (!bRet)
                    Logger.Log("Regist Show Key Error, Please Change To Other Key");

                WindowControl.WindowHandle = Window.Handle;
            }

            ProgramConstants.DisplayErrorAction = (title, error, exit) =>
            {
                new XNAMessageBox(wm, title, error, XNAMessageBoxButtons.OK)
                {
                    OKClickedAction = _ =>
                    {
                        if (exit)
                            Environment.Exit(1);
                    }
                }.Show();
            };

            SetGraphicsMode(wm);

            wm.SetIcon(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "clienticon.ico"));
            wm.SetControlBox(true);

            wm.Cursor.Textures =
            [
                AssetLoader.LoadTexture("cursor.png"),
                AssetLoader.LoadTexture("waitCursor.png")
            ];

            FileInfo primaryNativeCursorPath = SafePath.GetFile(ProgramConstants.GetResourcePath(), "cursor.cur");
            FileInfo alternativeNativeCursorPath = SafePath.GetFile(ProgramConstants.GetBaseResourcePath(), "cursor.cur");

            if (primaryNativeCursorPath.Exists)
                wm.Cursor.LoadNativeCursor(primaryNativeCursorPath.FullName);
            else if (alternativeNativeCursorPath.Exists)
                wm.Cursor.LoadNativeCursor(alternativeNativeCursorPath.FullName);

            Components.Add(wm);

            string playerName = UserINISettings.Instance.PlayerName.Value.Trim();

            if (UserINISettings.Instance.AutoRemoveUnderscoresFromName)
            {
                while (playerName.EndsWith("_"))
                    playerName = playerName.Substring(0, playerName.Length - 1);
            }

            if (string.IsNullOrEmpty(playerName))
            {
                playerName = Environment.UserName;

                playerName = playerName.Substring(playerName.IndexOf("\\") + 1);
            }

            playerName = Renderer.GetSafeString(NameValidator.GetValidOfflineName(playerName), 0);

            ProgramConstants.PLAYERNAME = playerName;
            UserINISettings.Instance.PlayerName.Value = playerName;

            IServiceProvider serviceProvider = BuildServiceProvider(wm);
            LoadingScreen ls = serviceProvider.GetService<LoadingScreen>();
            wm.AddAndInitializeControl(ls);
            ls.ClientRectangle = new Rectangle((wm.RenderResolutionX - ls.Width) / 2,
               (wm.RenderResolutionY - ls.Height) / 2, ls.Width, ls.Height);
            Exiting += (sender, e) =>
            {
                LocalHttpServer.Stop();
                ShiftClickAutoClicker.Instance.Stop();

            };
            Task.Run(() =>
            {
                LocalHttpServer.Start(wm);
            });
        }

        private void HotKey_OnHotkey(int nHotkeyId)
        {
            uint nShow = 1, nMinMax = 6; 
            if(nHotkeyId == hotKey[0])
            {
                nShow = 0;
                nMinMax = 6;
            }
            else if(nHotkeyId == hotKey[1])
            {
                nShow = 1;
                nMinMax = 4;
            }

            IntPtr? p = WindowControl.FindWindow(GAME_TITLE);
            if (null != p)
            {
                WindowControl.DisplayWindow(p.Value, nMinMax); //Min Or Max Window
                WindowControl.DisplayWindow(p.Value, nShow); //Hidden Game
            }

            WindowControl.DisplayWindow(nShow); //Hidden Client
        }

        private static Random GetRandom()
        {
            var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            byte[] intBytes = new byte[sizeof(int)];
            rng.GetBytes(intBytes);
            int seed = BitConverter.ToInt32(intBytes, 0);
            return new Random(seed);
        }

        private IServiceProvider BuildServiceProvider(WindowManager windowManager)
        {
            // Create host - this allows for things like DependencyInjection
            IHost host = Host.CreateDefaultBuilder()
                .ConfigureServices((_, services) =>
                    {
                        // services (or service-like)
                        services
                            .AddSingleton<ServiceProvider>()
                            .AddSingleton(windowManager)
                            .AddSingleton(GraphicsDevice)
                            .AddSingleton<GameCollection>()
                            .AddSingleton<CnCNetUserData>()
                            .AddSingleton<CnCNetManager>()
                            .AddSingleton<TunnelHandler>()
                            .AddSingleton<DiscordHandler>()
                            .AddSingleton<PrivateMessageHandler>()
                            .AddSingleton<MapLoader>()
                            .AddSingleton<Random>(GetRandom());

                        // singleton xna controls - same instance on each request
                        services
                            .AddSingletonXnaControl<LoadingScreen>()
                            .AddSingletonXnaControl<TopBar>()
                            .AddSingletonXnaControl<OptionsWindow>()
                            .AddSingletonXnaControl<PrivateMessagingWindow>()
                            .AddSingletonXnaControl<ModManager>()
                            .AddSingletonXnaControl<PrivateMessagingPanel>()
                            .AddSingletonXnaControl<LANLobby>()
                            .AddSingletonXnaControl<CnCNetGameLobby>()
                            .AddSingletonXnaControl<CnCNetGameLoadingLobby>()
                            .AddSingletonXnaControl<CnCNetLobby>()
                            .AddSingletonXnaControl<GameInProgressWindow>()
                            .AddSingletonXnaControl<SkirmishLobby>()
                            .AddSingletonXnaControl<MainMenu>()
                            .AddSingletonXnaControl<MapPreviewBox>()
                            .AddSingletonXnaControl<GameLaunchButton>()
                            .AddSingletonXnaControl<PlayerExtraOptionsPanel>();

                        // transient xna controls - new instance on each request
                        services
                            .AddTransientXnaControl<XNAControl>()
                            .AddTransientXnaControl<XNAButton>()
                            .AddTransientXnaControl<XNAClientButton>()
                            .AddTransientXnaControl<XNAClientCheckBox>()
                            .AddTransientXnaControl<XNAClientDropDown>()
                            .AddTransientXnaControl<XNALinkButton>()
                            .AddTransientXnaControl<XNAExtraPanel>()
                            .AddTransientXnaControl<XNACheckBox>()
                            .AddTransientXnaControl<XNADropDown>()
                            .AddTransientXnaControl<XNALabel>()
                            .AddTransientXnaControl<XNALinkLabel>()
                            .AddTransientXnaControl<XNAListBox>()
                            .AddTransientXnaControl<XNAMultiColumnListBox>()
                            .AddTransientXnaControl<XNAPanel>()
                            .AddTransientXnaControl<XNAProgressBar>()
                            .AddTransientXnaControl<XNASuggestionTextBox>()
                            .AddTransientXnaControl<XNATextBox>()
                            .AddTransientXnaControl<XNATrackbar>()
                            .AddTransientXnaControl<XNAChatTextBox>()
                            .AddTransientXnaControl<ChatListBox>()
                            .AddTransientXnaControl<GameLobbyCheckBox>()
                            .AddTransientXnaControl<GameLobbyDropDown>()
                            .AddTransientXnaControl<SettingCheckBox>()
                            .AddTransientXnaControl<SettingDropDown>()
                            .AddTransientXnaControl<FileSettingCheckBox>()
                            .AddTransientXnaControl<FileSettingDropDown>();
                    }
                )
                .Build();

            return host.Services.GetService<IServiceProvider>();
        }

        private void InitializeUISettings()
        {
            UISettings settings = new UISettings();

            settings.AltColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.AltUIColor);
            settings.SubtleTextColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.UIHintTextColor);
            settings.ButtonTextColor = settings.AltColor;

            settings.ButtonHoverColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.ButtonHoverColor);
            settings.TextColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.UILabelColor);
            //settings.WindowBorderColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.WindowBorderColor);
            settings.PanelBorderColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.PanelBorderColor);
            settings.BackgroundColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.AltUIBackgroundColor);
            settings.FocusColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.ListBoxFocusColor);
            settings.DisabledItemColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.DisabledButtonColor);

            settings.DefaultAlphaRate = ClientConfiguration.Instance.DefaultAlphaRate;
            settings.CheckBoxAlphaRate = ClientConfiguration.Instance.CheckBoxAlphaRate;
            settings.IndicatorAlphaRate = ClientConfiguration.Instance.IndicatorAlphaRate;

            settings.CheckBoxClearTexture = AssetLoader.LoadTexture("checkBoxClear.png");
            settings.CheckBoxCheckedTexture = AssetLoader.LoadTexture("checkBoxChecked.png");
            settings.CheckBoxDisabledClearTexture = AssetLoader.LoadTexture("checkBoxClearD.png");
            settings.CheckBoxDisabledCheckedTexture = AssetLoader.LoadTexture("checkBoxCheckedD.png");

            settings.RatingBoxCheckedTexture = AssetLoader.LoadTexture("ratingBoxChecked.png");
            settings.RatingBoxClearTexture = AssetLoader.LoadTexture("ratingBoxClear.png");

            XNAPlayerSlotIndicator.LoadTextures();

            UISettings.ActiveSettings = settings;
        }

        /// <summary>
        /// Sets the client's graphics mode.
        
        /// </summary>
        /// <param name="wm">The window manager</param>
        public static void SetGraphicsMode(WindowManager wm)
        {
            var clientConfiguration = ClientConfiguration.Instance;

            int windowWidth = UserINISettings.Instance.ClientResolutionX;
            int windowHeight = UserINISettings.Instance.ClientResolutionY;

            bool borderlessWindowedClient = UserINISettings.Instance.BorderlessWindowedClient;
            int currentWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            int currentHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

            if (currentWidth >= windowWidth && currentHeight >= windowHeight)
            {
                if (!wm.InitGraphicsMode(windowWidth, windowHeight, false))
                    throw new GraphicsModeInitializationException("Setting graphics mode failed!".L10N("UI:Main:SettingGraphicModeFailed") + " " + windowWidth + "x" + windowHeight);
            }
            else
            {
                if (!wm.InitGraphicsMode(1024, 600, false))
                    throw new GraphicsModeInitializationException("Setting default graphics mode failed!".L10N("UI:Main:SettingDefaultGraphicModeFailed"));
            }

            int renderResolutionX = 0;
            int renderResolutionY = 0;

            int initialXRes = Math.Max(windowWidth, clientConfiguration.MinimumRenderWidth);
            initialXRes = Math.Min(initialXRes, clientConfiguration.MaximumRenderWidth);

            int initialYRes = Math.Max(windowHeight, clientConfiguration.MinimumRenderHeight);
            initialYRes = Math.Min(initialYRes, clientConfiguration.MaximumRenderHeight);

            double xRatio = (windowWidth) / (double)initialXRes;
            double yRatio = (windowHeight) / (double)initialYRes;

            double ratio = xRatio > yRatio ? yRatio : xRatio;

            if ((windowWidth == 1366 || windowWidth == 1360) && windowHeight == 768)
            {
                renderResolutionX = windowWidth;
                renderResolutionY = windowHeight;
            }

            if (ratio > 1.0)
            {
                // Check whether we could sharp-scale our client window
                for (int i = 2; i < 10; i++)
                {
                    int sharpScaleRenderResX = windowWidth / i;
                    int sharpScaleRenderResY = windowHeight / i;

                    if (sharpScaleRenderResX >= clientConfiguration.MinimumRenderWidth &&
                        sharpScaleRenderResX <= clientConfiguration.MaximumRenderWidth &&
                        sharpScaleRenderResY >= clientConfiguration.MinimumRenderHeight &&
                        sharpScaleRenderResY <= clientConfiguration.MaximumRenderHeight)
                    {
                        renderResolutionX = sharpScaleRenderResX;
                        renderResolutionY = sharpScaleRenderResY;
                        break;
                    }
                }
            }

            if (renderResolutionX == 0 || renderResolutionY == 0)
            {
                renderResolutionX = initialXRes;
                renderResolutionY = initialYRes;

                if (ratio == xRatio)
                    renderResolutionY = (int)(windowHeight / ratio);
            }

            wm.SetBorderlessMode(borderlessWindowedClient);

            if (borderlessWindowedClient)
            {
                graphics.IsFullScreen = true;
                graphics.ApplyChanges();
            }

            wm.CenterOnScreen();
            wm.SetRenderResolution(renderResolutionX, renderResolutionY);
        }
    }

    /// <summary>
    /// An exception that is thrown when initializing display / graphics mode fails.
    /// </summary>
    class GraphicsModeInitializationException : Exception
    {
        public GraphicsModeInitializationException(string message) : base(message)
        {
        }
    }
}
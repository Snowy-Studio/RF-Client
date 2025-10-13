using System;
using System.Linq;
using System.Reflection.Metadata;
using ClientCore;
using ClientCore.Extensions;
using ClientGUI;
using Localization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;


namespace DTAConfig.OptionPanels
{
    public class MinesweeperGame : XNAWindow
    {
        public MinesweeperGame(WindowManager windowManager) : base(windowManager) { }
        private SpriteBatch spriteBatch;

        private int TileSize = 21; // 方块大小
        private double _TileSize = 21;

        // 默认的行数、列数和地雷数量
        private int DefaultRows = 25;
        private int DefaultColumns = 38;
        private int DefaultMineCount = 96;

        private int MinRows = 16; // 行数下限
        private int MaxRows = 25; // 行数上限
        private int MinColumns = 16; // 列数下限
        private int MaxColumns = 38; // 列数上限
        private int MinMineCount = 10; // 地雷下限
        private int MaxMineCount = 192; // 地雷上限


        private int FlagNum = 0; //可用旗帜数 逆推剩余雷数用 可以为负数 旗帜数量可以大于地雷数

        private new int Alpha = 5; //透明度


        private XNATrackbar trb_input1;
        private XNATrackbar trb_input2;
        private XNATrackbar trb_input3;
        private XNATrackbar trbAlphaTrack;

        private XNALabel lblAlpha; //透明度文字


        private XNALabel lblinput1; //透明度文字

        private XNALabel lblinput2; //透明度文字

        private XNALabel lblinput3; //透明度文字

        public static int Rows = 25; // 行数
        public static int Columns = 38; // 列数
        private int MineCount = 96; // 地雷数量

        //private XNALabel lblTime; //计时
        // private XNALabel lblRemaining; //剩余块数

        private Texture2D[] numberTextures = new Texture2D[17];//格子纹理


        private Texture2D[] digitTextures = new Texture2D[11]; //计时器纹理

        private Texture2D[] buttonTextures = new Texture2D[8]; //笑脸 哭脸




        //private Texture2D[] counterTextures = new Texture2D[11]; //计数器纹理 未来与计时器合并

        // private Texture2D tileTexture;

        //private SoundEffect clickSound;

        // private SoundEffect explosionSound;

        private EnhancedSoundEffect clickSound;

        private EnhancedSoundEffect explosionSound;

        private EnhancedSoundEffect rightclickSound;



        private int digitWidth = 10; // Width of each digit in pixels
        private int digitHeight = 15; // Height of each digit in pixels
        private int digitSpacing = 0; // Spacing between digits


        private XNAClientButton _btnresult;  //笑脸

        //Texture2D BackgroundTexture1;

        private bool _leftButtonWasPressed = false;
        private bool _rightButtonWasPressed = false;
        //鼠标防止连击 按下后加一个标记
        private bool _timerRunning;//计时器
        private TimeSpan _elapsedTime;//计时器

        private bool sureWindows = false;//重开确认对话框


        private XNAClientButton _btnok; //确定
        private XNAClientButton _btnok2; //确定透明度
        private XNAClientButton btnQuit; //退出

        private Rectangle destRect2;//格子后的背景

        //private bool _first = true;//判断是不是第一次进入
        //第一次进入是首次初始化 后续点击开始是另外的初始化
        //不这样做第一次完会导致没有雷
        //注意放置的雷会在点击后清空 然后重新放置
        //找到问题在哪了 

        private bool firstClick = true;//首次点击 点击后放雷 同时判定游戏开始

        private bool _gameOver;//判定游戏结束
        private bool _victory;//判定胜利
        private int _uncoveredCount;


        private int Alphahs = 128;



        //private readonly string[] trbAlphaTrackNames = Enumerable.Range(0, 10).Select(i => i.ToString()).ToArray();

        // 输入框1 - 行数
        // 输入框2 - 列数
        // 输入框3 - 地雷数


        //private readonly string[] trbinput1Names = Enumerable.Range(0, 26).Select(i => i.ToString()).ToArray();

        //private readonly string[] trbinput2Names = Enumerable.Range(0, 39).Select(i => i.ToString()).ToArray();

        private readonly string[] trbinput3Names = Enumerable.Range(0, 193).Select(i => i.ToString()).ToArray();

        public int windowWidth = UserINISettings.Instance.ClientResolutionX;
        public int windowHeight = UserINISettings.Instance.ClientResolutionY;

        //private Tile[,] tiles;
        private Tile[,] _tiles;

        private enum TileState
        {
            Covered,            //揭开    
            Uncovered,          //未揭开
            Flagged,            //标记旗帜
            MaybeMine           //也许是旗帜
        }

        private class Tile
        {
            public bool HasMine { get; set; }
            public TileState State { get; set; }
            public int NeighborMines { get; set; }

            // 新增属性
            public bool IsRevealed { get; set; }
            public int AdjacentMines { get; set; }
            public Rectangle Bounds { get; set; }
            public Texture2D Texture { get; set; }
            public bool IsClicked { get; set; }
            public float ClickAnimationTimer { get; set; }
            public float ClickAnimationDuration { get; set; }
            public Color OriginalColor { get; set; }
            public Color ClickedColor { get; set; }
            public bool IsMineClicked { get; set; }
            public float ClickMineAnimationTimer { get; set; }
            public float ClickMineAnimationDuration { get; set; }
        }

        public override void Initialize()
        {


            spriteBatch = new SpriteBatch(GraphicsDevice);

            // 加载音效文件
            clickSound = new EnhancedSoundEffect("click_sound.wav");//点击
            explosionSound = new EnhancedSoundEffect("explosion_sound.wav");//雷
            rightclickSound = new EnhancedSoundEffect("joingame.wav");//右键

            numberTextures[0] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/0.png"); //空格 周围没有雷
            numberTextures[1] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/slocindicator1.png");//1-8计数
            numberTextures[2] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/slocindicator2.png");//1-8计数
            numberTextures[3] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/slocindicator3.png");//1-8计数
            numberTextures[4] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/slocindicator4.png");//1-8计数
            numberTextures[5] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/slocindicator5.png");//1-8计数
            numberTextures[6] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/slocindicator6.png");//1-8计数
            numberTextures[7] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/slocindicator7.png");//1-8计数
            numberTextures[8] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/slocindicator8.png");//1-8计数
            numberTextures[9] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/statusWarning.png"); //旗帜
            numberTextures[10] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/statusUnavailable.png");//未揭开
            numberTextures[11] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/totalmines.png"); //地雷
            numberTextures[12] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/questionMark_c.png");//也许是旗帜
            numberTextures[13] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/background1.png");//格子下方的背景
            numberTextures[14] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/background2.png");//格子下方的背景 胜利后切换
            numberTextures[15] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/background3.png");//格子下方的背景 失败后切换
            numberTextures[16] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/statusError.png");//点到的雷

            //计数
            digitTextures[0] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/Digits/0.png");
            digitTextures[1] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/Digits/1.png");
            digitTextures[2] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/Digits/2.png");
            digitTextures[3] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/Digits/3.png");
            digitTextures[4] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/Digits/4.png");
            digitTextures[5] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/Digits/5.png");
            digitTextures[6] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/Digits/6.png");
            digitTextures[7] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/Digits/7.png");
            digitTextures[8] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/Digits/8.png");
            digitTextures[9] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/Digits/9.png");
            digitTextures[10] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/Digits/mh.png");

            //笑脸 哭脸
            buttonTextures[0] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/button.png");//平时
            buttonTextures[1] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/button0.png");//按下
            buttonTextures[2] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/crying.png");//失败
            buttonTextures[3] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/smiley.png");//胜利

            buttonTextures[4] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/NotrackbarButton.png");//开始后滑块背景
            buttonTextures[5] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/OktrackbarButton.png");////可点击滑块背景

            buttonTextures[6] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/XtrackbarButton.png");//开始后滑块
            buttonTextures[7] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/trackbarButton.png");//可点击滑块

            //counterTextures[0] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/Digits/0.png");
            //counterTextures[1] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/Digits/1.png");
            //counterTextures[2] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/Digits/2.png");
            //counterTextures[3] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/Digits/3.png");
            //counterTextures[4] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/Digits/4.png");
            //counterTextures[5] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/Digits/5.png");
            //counterTextures[6] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/Digits/6.png");
            //counterTextures[7] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/Digits/7.png");
            //counterTextures[8] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/Digits/8.png");
            //counterTextures[9] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/Digits/9.png");
            //counterTextures[10] = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/Digits/mh.png");



            _TileSize = WindowManager.ScaleRatio * TileSize;
            //Console.WriteLine(_TileSize);
            //base.Initialize();
            Name = "MinesweeperGameWindow";

            BackgroundTexture = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/background.png");  // 加载背景   
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.CENTERED;
            //背景画布居中
            //不重要了 现在是一个透明图片

            DrawBorders = false;
            FlagNum = MineCount;//可用旗帜数 = 雷数

            //行数
            trb_input1 = new XNATrackbar(WindowManager);
            trb_input1.Name = "trbinput1";
            trb_input1.MinValue = 16;
            trb_input1.MaxValue = 25;
            trb_input1.Value = 25;
            trb_input1.ClientRectangle = new Rectangle(85, 620, 100, 20);// 调整位置和大小
            trb_input1.ValueChanged += trb_input1_ValueChanged;
            trb_input1.Text = $"{Rows}";
            AddChild(trb_input1);
            trb_input1.Enabled = true;

            //列数
            trb_input2 = new XNATrackbar(WindowManager);
            trb_input2.Name = "trbinput2";
            trb_input2.MinValue = 16;
            trb_input2.MaxValue = 38;
            trb_input2.Value = 38;
            trb_input2.ClientRectangle = new Rectangle(245, 620, 100, 20);// 调整位置和大小
            trb_input2.ValueChanged += trb_input2_ValueChanged;
            trb_input2.Text = $"{Columns}";
            AddChild(trb_input2);
            trb_input2.Enabled = true;

            //地雷数
            trb_input3 = new XNATrackbar(WindowManager);
            trb_input3.Name = "trbinput3";
            trb_input3.MinValue = 10;
            trb_input3.MaxValue = 192;
            trb_input3.Value = 96;
            trb_input3.ClientRectangle = new Rectangle(405, 620, 100, 20);// 调整位置和大小
            trb_input3.ValueChanged += trb_input3_ValueChanged;
            trb_input3.Text = $"{MineCount}";
            AddChild(trb_input3);
            trb_input3.Enabled = true;

            trbAlphaTrack = new XNATrackbar(WindowManager);
            trbAlphaTrack.Name = "trbAlphaSelector";
            trbAlphaTrack.MinValue = 0;
            trbAlphaTrack.MaxValue = 10;
            trbAlphaTrack.Value = 5;
            trbAlphaTrack.ClientRectangle = new Rectangle(580, 620, 100, 20);// 调整位置和大小
            trbAlphaTrack.ValueChanged += AlphaTrack_ValueChanged;
            AddChild(trbAlphaTrack);

            lblinput1 = new XNALabel(WindowManager);
            lblinput1.Name = "lblinput1：";
            lblinput1.Text = $"{Rows}";
            lblinput1.ClientRectangle = new Rectangle(65, 620, 30, 20);
            AddChild(lblinput1);

            lblinput2 = new XNALabel(WindowManager);
            lblinput2.Name = "lblinput2：";
            lblinput2.Text = $"{Columns}";
            lblinput2.ClientRectangle = new Rectangle(225, 620, 30, 20);
            AddChild(lblinput2);

            lblinput3 = new XNALabel(WindowManager);
            lblinput3.Name = "lblinput3：";
            lblinput3.Text = $"{MineCount}";
            lblinput3.ClientRectangle = new Rectangle(382, 620, 30, 20);
            AddChild(lblinput3);

            lblAlpha = new XNALabel(WindowManager);
            lblAlpha.Name = "lblAlpha：";
            lblAlpha.Text = $"{Alpha}";
            lblAlpha.ClientRectangle = new Rectangle(560, 620, 30, 20);
            AddChild(lblAlpha);

            btnQuit = new XNAClientButton(WindowManager);
            btnQuit.Name = nameof(btnQuit);
            btnQuit.ClientRectangle = new Rectangle(705, 618, UIDesignConstants.BUTTON_WIDTH_75, UIDesignConstants.BUTTON_HEIGHT);
            btnQuit.Text = "Jump to the edit user window".L10N("UI:Main:JumpEditUserWindow");
            btnQuit.LeftClick += btnQuit_LeftClick;
            AddChild(btnQuit);

            _tiles = new Tile[Columns, Rows];

            _gameOver = false;
            _victory = false;
            _uncoveredCount = 0;

            _btnresult = new XNAClientButton(WindowManager);
            _btnresult.Name = "result";
            _btnresult.ClientRectangle = new Rectangle(Columns * TileSize / 2 - 20 + 20, 25, 40, 40);
            _btnresult.LeftClick += BtnResult_LeftClick;  // 返回按钮点击事件.
            _btnresult.HoverTexture = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/button.png");  // 加载背景纹理
            _btnresult.IdleTexture = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/Content/button.png");  // 加载背景纹理                
            // _btnresult.Enabled = false;

            AddChild(_btnresult);

            CenterOnParent();

            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    _tiles[x, y] = new Tile()
                    {
                        HasMine = false,
                        IsRevealed = false,
                        AdjacentMines = 0,
                        Texture = numberTextures[10],

                        //Texture = tileTexture3,

                        Bounds = new Rectangle(
                           (int)(X - _TileSize * 19) + x * TileSize,
                           (int)(Y - _TileSize * 11.5) + y * TileSize,
                            TileSize,
                            TileSize
                            ),
                        IsClicked = false,
                        ClickAnimationTimer = 0.0f,//单击动画计时器
                        ClickAnimationDuration = 0.4f, //动画时长

                        //点到雷
                        IsMineClicked = false,
                        ClickMineAnimationTimer = 0.0f,//单击动画计时器
                        ClickMineAnimationDuration = 0.4f, //动画时长

                        OriginalColor = new Color(0, 0, 0, 0),//点击前的颜色
                        //ClickedColor = Color.Gray,//点击后的颜色
                        ClickedColor = new Color(255, 255, 255, 255)//点击后的颜色
                    };
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            MouseState mouseState = Mouse.GetState();
            if (!_gameOver && !_victory && WindowManager.HasFocus && !sureWindows)
            //以及没有在重开确认窗口
            //游戏未结束并且程序有焦点
            //没有焦点时不响应点击且停止计时
            {
                if (_timerRunning && !firstClick && WindowManager.HasFocus)
                {
                    _elapsedTime += gameTime.ElapsedGameTime;
                }

                //处理左键点击事件
                if (mouseState.LeftButton == ButtonState.Pressed && !_leftButtonWasPressed)
                {
                    _leftButtonWasPressed = true;


                    double mousex = WindowManager.ScaleRatio * Cursor.Location.X;
                    double mousey = WindowManager.ScaleRatio * Cursor.Location.Y;

                    int newmMouseStateX = (int)((mousex - X * WindowManager.ScaleRatio) / _TileSize - 1);
                    int newmMouseStateY = (int)((mousey - Y * WindowManager.ScaleRatio) / _TileSize - 4);

                    //-0到-1之间的数被舍弃为0了 需要修正
                    if ((mousex - X * WindowManager.ScaleRatio) / _TileSize - 1 < 0)
                    {
                        newmMouseStateX = -1;
                    }

                    if ((mousey - Y * WindowManager.ScaleRatio) / _TileSize - 4 < 0)
                    {
                        newmMouseStateY = -1;
                    }

                    Console.WriteLine($"窗体X坐标:{X}");
                    Console.WriteLine($"窗体Y坐标:{Y}");
                    Console.WriteLine($"X坐标:{mousex - X * WindowManager.ScaleRatio}");
                    Console.WriteLine($"Y坐标:{mousey - Y * WindowManager.ScaleRatio}");
                    Console.WriteLine($"格子大小:{Math.Ceiling(_TileSize)}");
                    Console.WriteLine($"格子坐标起点X:{_TileSize * 1 + X * WindowManager.ScaleRatio}");
                    Console.WriteLine($"格子坐标起点Y:{_TileSize * 4 + Y * WindowManager.ScaleRatio}");
                    Console.WriteLine($"左键点击格子X:{(mousex - X * WindowManager.ScaleRatio) / _TileSize - 1}");
                    Console.WriteLine($"左键点击格子Y:{(mousey - Y * WindowManager.ScaleRatio) / _TileSize - 4}");
                    Console.WriteLine(newmMouseStateX);
                    Console.WriteLine(newmMouseStateY);

                    //判断点击在格子区域
                    if (newmMouseStateX >= 0 && newmMouseStateY >= 0 && newmMouseStateX < Columns && newmMouseStateY < Rows)
                    {

                        _btnresult.HoverTexture = buttonTextures[1];
                        _btnresult.IdleTexture = buttonTextures[1];

                        if (trb_input1.Enabled == true || trb_input2.Enabled == true || trb_input3.Enabled == true)
                        {
                            //_btnok.Enabled = false;//游戏开始了 不能重设地雷数
                            trb_input1.Enabled = false;
                            trb_input2.Enabled = false;
                            trb_input3.Enabled = false;
                            trb_input1.BackgroundTexture = buttonTextures[4];
                            trb_input2.BackgroundTexture = buttonTextures[4];
                            trb_input3.BackgroundTexture = buttonTextures[4];
                            trb_input1.ButtonTexture = buttonTextures[6];
                            trb_input2.ButtonTexture = buttonTextures[6];
                            trb_input3.ButtonTexture = buttonTextures[6];
                        }

                        Tile tile = _tiles[newmMouseStateX, newmMouseStateY];

                        if (tile.State == TileState.Covered)
                        {

                            if (firstClick)
                            {
                                // Randomly place mines again
                                Random random = new Random();
                                ////Console.WriteLine("初始化放置地雷");
                                int minesToPlace = MineCount;

                                clickSound.Play();
                                firstClick = false;

                                //Console.WriteLine("清空地图");
                                for (int x = 0; x < Columns; x++)
                                {
                                    for (int y = 0; y < Rows; y++)
                                    {
                                        //_tiles[x, y].State = TileState.Covered; // 初始状态
                                        _tiles[x, y].HasMine = false; //清空地雷
                                        _tiles[x, y].NeighborMines = 0; //清空邻格地雷数量
                                                                        //这里其实只清空地雷就行
                                    }
                                }
                                //随机放置地雷

                                while (minesToPlace > 0)
                                {
                                    int x = random.Next(Columns);
                                    int y = random.Next(Rows);

                                    if (x == newmMouseStateX && y == newmMouseStateY)
                                    {
                                        continue;
                                    }

                                    //避开四角
                                    //if ((x == 0 && y == 0) ||
                                    //    (x == Columns - 1 && y == 0) ||
                                    //    (x == 0 && y == Rows - 1) ||
                                    //    (x == Columns - 1 && y == Rows - 1)
                                    //   )
                                    //{
                                    //    continue;
                                    //}

                                    if (!_tiles[x, y].HasMine)
                                    {
                                        _tiles[x, y].HasMine = true;
                                        minesToPlace--;
                                    }
                                }
                                // 重新计算邻格地雷数量
                                for (int x = 0; x < Columns; x++)
                                {
                                    for (int y = 0; y < Rows; y++)
                                    {
                                        if (!_tiles[x, y].HasMine)
                                        {
                                            _tiles[x, y].NeighborMines = CountNeighborMines(x, y);
                                        }
                                    }
                                }

                                UncoverTile(newmMouseStateX, newmMouseStateY);

                                tile.IsClicked = true;
                                tile.ClickAnimationTimer = tile.ClickAnimationDuration; //开始点击动画


                            }
                            else if (tile.HasMine)
                            {
                                // 点击到地雷
                                explosionSound.Play();
                                _gameOver = true;
                                Console.WriteLine("点到雷");
                                //lblRemaining.Text = "再接再厉！";
                                firstClick = false;//避免出错 这一句还是加上

                                //Texture2D tileTexture3 = AssetLoader.LoadTexture($"Resources/{UserINISettings.Instance.ClientTheme}/statusError.png");
                                tile.Texture = numberTextures[16];
                                tile.IsMineClicked = true;
                                tile.ClickMineAnimationTimer = tile.ClickMineAnimationDuration; //开始点击动画

                            }
                            else
                            {
                                clickSound.Play();
                                Console.WriteLine("揭开块");
                                // 揭开方块
                                UncoverTile(newmMouseStateX, newmMouseStateY);
                                tile.IsClicked = true;
                                tile.ClickAnimationTimer = tile.ClickAnimationDuration; //开始点击动画
                                firstClick = false;
                            }
                        }

                        //计算剩余块数
                        if (_gameOver)
                        {
                            _btnresult.HoverTexture = buttonTextures[2];
                            _btnresult.IdleTexture = buttonTextures[2];
                            // _btnok.Enabled = true;
                            trb_input1.Enabled = true;
                            trb_input2.Enabled = true;
                            trb_input3.Enabled = true;
                            trb_input1.BackgroundTexture = buttonTextures[5];
                            trb_input2.BackgroundTexture = buttonTextures[5];
                            trb_input3.BackgroundTexture = buttonTextures[5];
                            trb_input1.ButtonTexture = buttonTextures[7];
                            trb_input2.ButtonTexture = buttonTextures[7];
                            trb_input3.ButtonTexture = buttonTextures[7];
                            XNAMessageBox messageBox = new XNAMessageBox(WindowManager, "Game Over".L10N("UI:Main:GameOver"),
                                $"游戏结束,请再接再厉TwT.\n剩余格子: {(Rows * Columns - MineCount - _uncoveredCount)}", XNAMessageBoxButtons.OK);
                            messageBox.Show();
                        }
                        else if (_victory)
                        {
                            _btnresult.HoverTexture = buttonTextures[3];
                            _btnresult.IdleTexture = buttonTextures[3];
                            //_btnok.Enabled = true;
                            trb_input1.Enabled = true;
                            trb_input2.Enabled = true;
                            trb_input3.Enabled = true;
                            trb_input1.BackgroundTexture = buttonTextures[5];
                            trb_input2.BackgroundTexture = buttonTextures[5];
                            trb_input3.BackgroundTexture = buttonTextures[5];
                            trb_input1.ButtonTexture = buttonTextures[7];
                            trb_input2.ButtonTexture = buttonTextures[7];
                            trb_input3.ButtonTexture = buttonTextures[7];
                            //string elapsedTimeString = _elapsedTime.ToString(@"hh\:mm\:ss");
                            string elapsedTimeString = $"{(int)_elapsedTime.TotalHours} 时 {(int)_elapsedTime.TotalMinutes % 60} 分 {_elapsedTime.Seconds} 秒";
                            XNAMessageBox messageBox = new XNAMessageBox(WindowManager, "Game Victory".L10N("UI:Main:GameVictory"),
                                $"恭喜,您获胜了OvO.\n行数: {Rows}, 列数: {Columns}, 地雷数: {MineCount}, \n耗费时间: {elapsedTimeString}", XNAMessageBoxButtons.OK);
                            messageBox.Show();
                        }
                    }
                    //else
                    //{
                    //    //Console.WriteLine("格子外点击");
                    //    //Disable();
                    //}
                }

                else if (mouseState.LeftButton == ButtonState.Released && _leftButtonWasPressed == true)
                {
                    //Console.WriteLine("释放左键");
                    _leftButtonWasPressed = false; // 在释放时重置bool
                    _btnresult.HoverTexture = buttonTextures[0];
                    _btnresult.IdleTexture = buttonTextures[0];
                }

                // 处理右键点击事件
                if (mouseState.RightButton == ButtonState.Pressed && !_rightButtonWasPressed)
                {
                    _rightButtonWasPressed = true; // Set flag to true upon initial press

                    double mousex = WindowManager.ScaleRatio * Cursor.Location.X;
                    double mousey = WindowManager.ScaleRatio * Cursor.Location.Y;

                    int newmMouseStateX = (int)((mousex - X * WindowManager.ScaleRatio) / _TileSize - 1);
                    int newmMouseStateY = (int)((mousey - Y * WindowManager.ScaleRatio) / _TileSize - 4);

                    //-0到-1之间的数被舍弃为0了 需要修正
                    if ((mousex - X * WindowManager.ScaleRatio) / _TileSize - 1 < 0)
                    {
                        newmMouseStateX = -1;
                    }

                    if ((mousey - Y * WindowManager.ScaleRatio) / _TileSize - 4 < 0)
                    {
                        newmMouseStateY = -1;
                    }

                    Console.WriteLine($"X坐标:{mousex - X * WindowManager.ScaleRatio}");
                    Console.WriteLine($"Y坐标:{mousey - Y * WindowManager.ScaleRatio}");
                    Console.WriteLine($"格子大小:{Math.Ceiling(_TileSize)}");
                    Console.WriteLine($"格子坐标起点X:{_TileSize * 1 + X * WindowManager.ScaleRatio}");
                    Console.WriteLine($"格子坐标起点Y:{_TileSize * 4 + Y * WindowManager.ScaleRatio}");
                    Console.WriteLine($"左键点击格子X:{(mousex - X * WindowManager.ScaleRatio) / _TileSize - 1}");
                    Console.WriteLine($"左键点击格子Y:{(mousey - Y * WindowManager.ScaleRatio) / _TileSize - 4}");
                    Console.WriteLine(newmMouseStateX);
                    Console.WriteLine(newmMouseStateY);

                    //判断点击在格子区域
                    if (newmMouseStateX >= 0 && newmMouseStateY >= 0 && newmMouseStateX < Columns && newmMouseStateY < Rows)
                    {
                        if (trb_input1.Enabled == true || trb_input2.Enabled == true || trb_input3.Enabled == true)
                        {
                            trb_input1.Enabled = false;
                            trb_input2.Enabled = false;
                            trb_input3.Enabled = false;
                            trb_input1.BackgroundTexture = buttonTextures[4];
                            trb_input2.BackgroundTexture = buttonTextures[4];
                            trb_input3.BackgroundTexture = buttonTextures[4];
                            trb_input1.ButtonTexture = buttonTextures[6];
                            trb_input2.ButtonTexture = buttonTextures[6];
                            trb_input3.ButtonTexture = buttonTextures[6];
                        }

                        Tile tile = _tiles[newmMouseStateX, newmMouseStateY];
                        if (tile.State == TileState.Covered)
                        {
                            rightclickSound.Play();
                            // 切换状态：覆盖 -> 标记
                            tile.State = TileState.Flagged;
                            FlagNum--;
                        }
                        else if (tile.State == TileState.Flagged)
                        {
                            rightclickSound.Play();
                            // 切换状态：标记 -> 可能为地雷
                            tile.State = TileState.MaybeMine;
                            FlagNum++; // 因为从标记状态切换到可能为地雷状态，所以增加剩余地雷数
                        }
                        else if (tile.State == TileState.MaybeMine)
                        {
                            rightclickSound.Play();
                            // 切换状态：可能为地雷 -> 覆盖
                            tile.State = TileState.Covered;
                            // FlagNum不变，因为从可能为地雷状态切换到覆盖状态，不影响剩余地雷数
                        }
                    }
                }
                else if (mouseState.RightButton == ButtonState.Released && _rightButtonWasPressed == true)
                {
                    //Console.WriteLine("释放右键");                    
                    _rightButtonWasPressed = false; // 在释放时重置bool
                }

                if (!firstClick && !_timerRunning)
                {
                    // lblTime.Text = "Time:".L10N("Client:Main:MinesweeperTime") + _elapsedTime.ToString(@"mm\:ss");
                    _elapsedTime = TimeSpan.Zero;
                    _timerRunning = true;
                    Console.WriteLine("重置计时");
                }

                foreach (var tile in _tiles)
                {

                    if (_gameOver || _victory)
                    {
                        tile.ClickAnimationTimer = 0;
                        tile.IsClicked = false; // 结束动画
                    }
                    else if (firstClick)
                    {
                        tile.ClickMineAnimationTimer = 0;
                        tile.IsMineClicked = false; // 直接结束点到地雷的动画
                        tile.Texture = numberTextures[10];
                    }
                    else if (tile.IsClicked)
                    {
                        tile.ClickAnimationTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                        if (tile.ClickAnimationTimer < 0)
                        {
                            tile.ClickAnimationTimer = 0;
                            tile.IsClicked = false; // 结束动画
                                                    //Console.WriteLine("结束");
                        }
                    }
                }
            }
            base.Update(gameTime);
        }

        private void AlphaTrack_ValueChanged(object sender, EventArgs e)
        {
            lblAlpha.Text = trbinput3Names[trbAlphaTrack.Value];
            int.TryParse(lblAlpha.Text, out int AlphaInput);
            Alpha = MathHelper.Clamp(AlphaInput, 0, 10);
            // Alphahs = (int)Math.Round(Alpha * 255 / 10.0);
            Alphahs = Alpha * 20 + 55;
            Console.WriteLine($"当前透明度：{Alphahs}");
            //保护值不溢出
            if (Alphahs < 0)
            {
                Console.WriteLine("透明度过低");
                Alphahs = 0;
            }
            else if (Alphahs > 255)
            {
                Console.WriteLine("透明度过高");
                Alphahs = 255;
            }
        }

        private void trb_input1_ValueChanged(object sender, EventArgs e)
        {
            lblinput1.Text = trbinput3Names[trb_input1.Value];
            int.TryParse(lblinput1.Text, out int rowsInput);
            Rows = (int)MathHelper.Clamp(rowsInput, MinRows, MaxRows);
            InitializeGame();
        }

        private void trb_input2_ValueChanged(object sender, EventArgs e)
        {
            lblinput2.Text = trbinput3Names[trb_input2.Value];
            int.TryParse(lblinput2.Text, out int ColumnsInput);
            Columns = (int)MathHelper.Clamp(ColumnsInput, MinColumns, MaxColumns);
            InitializeGame();

        }
        private void trb_input3_ValueChanged(object sender, EventArgs e)
        {
            trb_input3.Text = $"{MineCount}";
            lblinput3.Text = trbinput3Names[trb_input3.Value];
            int.TryParse(lblinput3.Text, out int MineCountsInput);
            MineCount = (int)MathHelper.Clamp(MineCountsInput, MinMineCount, MaxMineCount);
            InitializeGame();
        }

        private void btnQuit_LeftClick(object sender, EventArgs e)
        {
            sureWindows = true;
            if (!firstClick && !_gameOver && !_victory)
            {
                XNAMessageBox messageBox = new XNAMessageBox(WindowManager, "Exit the Game Confirm".L10N("UI:Main:QuitWindow"),
                    "The game is ongoing. Confirm your exit?".L10N("UI:Main:QuitWindowTips"), XNAMessageBoxButtons.YesNo);
                messageBox.Show();
                messageBox.YesClickedAction += Quit_YesClicked;
                messageBox.NoClickedAction += Result_NoClicked;
            }
            else
            {
                InitializeGame();
                Disable();
                sureWindows = false;
            }

        }

        private void Quit_YesClicked(XNAMessageBox messageBox)
        {
            InitializeGame();
            Disable();
            sureWindows = false;
        }

        private void BtnResult_LeftClick(object sender, EventArgs e)
        {
            sureWindows = true;

            if (firstClick || _gameOver || _victory)
            {
                // 重新初始化游戏状态和数据
                InitializeGame();
                Console.WriteLine("重新开始");
                // 重新初始化游戏设置和游戏
            }
            else
            {
                Console.WriteLine("这一局还没有完成呢!");
                XNAMessageBox messageBox = new XNAMessageBox(WindowManager, "Resume Confirm".L10N("UI:Main:ResetWindow"),
                    "The game is ongoing. Confirm to start over?".L10N("UI:Main:ResetWindowTips"), XNAMessageBoxButtons.YesNo);
                messageBox.Show();
                messageBox.YesClickedAction += Result_YesClicked;
                messageBox.NoClickedAction += Result_NoClicked;
            }
        }

        private void Result_YesClicked(XNAMessageBox messageBox)
        {
            Console.WriteLine("重新开始!");
            sureWindows = false;
            InitializeGame();
        }

        //放弃退出和放弃重置都是这个
        private void Result_NoClicked(XNAMessageBox messageBox)
        {
            //    //不明确这几句是否必要
            //    //var parent = (DarkeningPanel)messageBox.Parent;
            //    ////parent.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            //    //parent.Hide();
            //    //parent.Hidden += Parent_Hidden;
            //    sureWindows = false;
            //    Console.WriteLine("不要重开！");
            sureWindows = false;
        }

        //private static void Parent_Hidden(object sender, EventArgs e)
        //{
        //    var darkeningPanel = (DarkeningPanel)sender;
        //    darkeningPanel.WindowManager.RemoveControl(darkeningPanel);
        //    darkeningPanel.Hidden -= Parent_Hidden;
        //}

        //每次结束后初始化游戏
        private void InitializeGame()
        {
            _gameOver = false;
            _victory = false;
            _uncoveredCount = 0;
            FlagNum = MineCount;
            firstClick = true;
            _leftButtonWasPressed = false; // 在释放时重置bool
            _rightButtonWasPressed = false;
            sureWindows = false;//重开确认

            _timerRunning = false;
            _elapsedTime = TimeSpan.Zero;

            trb_input1.Enabled = true;
            trb_input2.Enabled = true;
            trb_input3.Enabled = true;
            trb_input1.BackgroundTexture = buttonTextures[5];
            trb_input2.BackgroundTexture = buttonTextures[5];
            trb_input3.BackgroundTexture = buttonTextures[5];
            trb_input1.ButtonTexture = buttonTextures[7];
            trb_input2.ButtonTexture = buttonTextures[7];
            trb_input3.ButtonTexture = buttonTextures[7];

            // 重置按钮样式
            _btnresult.HoverTexture = buttonTextures[0];  // 加载背景纹理
            _btnresult.IdleTexture = buttonTextures[0];   // 加载背景纹理

            //重置所有格子
            //初始化的时候清空一次
            //点击左键的时候还清空了一次雷
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    _tiles[x, y].State = TileState.Covered; //揭开、标记的、带标记的收起
                    _tiles[x, y].HasMine = false;           //移除所有的雷
                    _tiles[x, y].NeighborMines = 0;         //重置格子上的计数
                                                            //以上三条把所有变化的格子重置了
                }
            }

        }
        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin();
            GraphicsDevice.Clear(new Color(0, 0, 0, Alphahs));
            //背板透明度
            //如果想要减少行列数之后居中的话 这里要改
            int NewColumns = Columns;
            //如果想要减少行列数之后居中的话 这里要改
            //改过之后可能还要继续完善鼠标坐标和格子的计算
            int NewRows = Rows;

            //底下绘制一层 做格子的背景
            Rectangle destRect2 = new Rectangle(
            X + TileSize,
            Y + 1 * TileSize,
            TileSize * 38,
            TileSize * 28);

            if (_victory)
            {
                //Console.WriteLine("win");
                spriteBatch.Draw(numberTextures[14], destRect2, new Color(Alphahs, Alphahs, Alphahs, Alphahs));
                //底下绘制一层 做格子的背景
            }
            else if (_gameOver)
            {
                //Console.WriteLine("lost");
                spriteBatch.Draw(numberTextures[15], destRect2, new Color(Alphahs, Alphahs, Alphahs, Alphahs));
                //赢了就换一个图像
            }
            else
            {
                spriteBatch.Draw(numberTextures[13], destRect2, new Color(Alphahs, Alphahs, Alphahs, Alphahs));
                //底下绘制一层 做格子的背景
            }

            //绘制每个方块
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    Rectangle destRect = new Rectangle(
                    X + (x + 1 + Columns - NewColumns) * TileSize,
                    Y + (3 + 1 + y + Rows - NewRows) * TileSize,
                    TileSize,
                    TileSize);
                    //偏移量加一格 给边框的位置

                    spriteBatch.Draw(numberTextures[0], destRect, new Color(255, 255, 255, 255));
                    //如果直接放到背景上 可以节约性能 但是会导致行列缩小后 格子还在

                    Tile tile = _tiles[x, y];


                    // 根据是否点击了方块，调整颜色
                    Color color = tile.IsClicked ? tile.ClickedColor : tile.OriginalColor;
                    //Color color2 = tile.IsMineClicked ? tile.ClickedColor : tile.OriginalColor;

                    //点击动画

                    if (_gameOver && tile.HasMine)
                    {
                        spriteBatch.Draw(numberTextures[11], destRect, new Color(255, 255, 255, 255));
                    }
                    else
                    {
                        if (tile.State == TileState.Covered)
                        {
                            spriteBatch.Draw(numberTextures[10], destRect, new Color(255, 255, 255, 255));
                        }
                        else if (tile.State == TileState.Uncovered)
                        {
                            if (tile.HasMine)
                            {
                                spriteBatch.Draw(numberTextures[11], destRect, new Color(255, 255, 255, 255));
                                //如果是地雷 则显示雷
                            }
                            else
                            {
                                spriteBatch.Draw(numberTextures[tile.NeighborMines], destRect, new Color(255, 255, 255, 255));
                                //如果不是地雷 则显示周围有几个雷
                            }
                        }
                        else if (tile.State == TileState.Flagged)
                        {
                            spriteBatch.Draw(numberTextures[9], destRect, new Color(255, 255, 255, 255));
                        }
                        else if (tile.State == TileState.MaybeMine)
                        {
                            spriteBatch.Draw(numberTextures[12], destRect, new Color(255, 255, 255, 255));
                        }

                    }

                    // 如果正在进行点击动画，插值颜色
                    if (tile.IsClicked && tile.ClickAnimationTimer > 0)
                    {
                        float t = tile.ClickAnimationTimer / tile.ClickAnimationDuration;
                        color = Color.Lerp(tile.OriginalColor, tile.ClickedColor, t);
                    }
                    else if (tile.IsMineClicked && tile.ClickMineAnimationTimer > 0)
                    {
                        float t = tile.ClickMineAnimationTimer / tile.ClickMineAnimationDuration;
                        color = Color.Lerp(tile.OriginalColor, tile.ClickedColor, t);
                    }
                    spriteBatch.Draw(tile.Texture, tile.Bounds, color);
                }
            }

            // 绘制还需打开的空格数
            DrawMineCounter((Rows * Columns - MineCount - _uncoveredCount), new Vector2(X + 13.5F * TileSize, Y + 2.25F * TileSize));

            //旗帜数地雷数
            DrawMineCounter(FlagNum, new Vector2(X + 15.45F * TileSize, Y + 2.25F * TileSize));

            //绘制时钟
            DrawDigitalClock(_elapsedTime, new Vector2(X + 23.2F * TileSize, Y + 2.25F * TileSize));

            spriteBatch.End();
            base.Draw(gameTime);
        }

        // 揭开方块，并递归揭开周围没有地雷的方块
        private void UncoverTile(int x, int y)
        {
            if (x < 0 || x >= Columns || y < 0 || y >= Rows || _tiles[x, y].State != TileState.Covered)
            {
                return;
            }

            _tiles[x, y].State = TileState.Uncovered;
            _tiles[x, y].IsClicked = true;
            _tiles[x, y].ClickAnimationTimer = _tiles[x, y].ClickAnimationDuration; // 开始动画

            _uncoveredCount++;

            if (_tiles[x, y].NeighborMines == 0)
            {
                UncoverTile(x - 1, y - 1);
                UncoverTile(x, y - 1);
                UncoverTile(x + 1, y - 1);
                UncoverTile(x - 1, y);
                UncoverTile(x + 1, y);
                UncoverTile(x - 1, y + 1);
                UncoverTile(x, y + 1);
                UncoverTile(x + 1, y + 1);
            }

            if (_uncoveredCount == Rows * Columns - MineCount)
            {
                _victory = true;
                //胜利判定
            }
        }

        // 计算指定方块周围的地雷数量
        private int CountNeighborMines(int x, int y)
        {
            int count = 0;

            for (int i = Math.Max(0, x - 1); i <= Math.Min(x + 1, Columns - 1); i++)
            {
                for (int j = Math.Max(0, y - 1); j <= Math.Min(y + 1, Rows - 1); j++)
                {
                    if (_tiles[i, j].HasMine)
                    {
                        count++;
                    }
                }
            }
            return count;
        }


        // 绘制时钟
        private void DrawDigitalClock(TimeSpan time, Vector2 position)
        {
            string timeString = $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";

            int currentX = (int)position.X;

            foreach (char digit in timeString)
            {
                if (digit == ':')
                {
                    // Draw colon (assuming 2 pixels wide for spacing)
                    spriteBatch.Draw(digitTextures[10], new Rectangle(currentX, (int)position.Y + 2, 4, 11), Color.White);
                    currentX += 4 + digitSpacing;
                    //冒号右边的距离+4
                }
                else
                {
                    // Draw digits
                    int digitIndex = digit - '0'; // Convert char to digit index
                    spriteBatch.Draw(digitTextures[digitIndex], new Rectangle(currentX, (int)position.Y, digitWidth, digitHeight), Color.White);
                    currentX += digitWidth + digitSpacing;
                }
            }
        }

        private void DrawMineCounter(int count, Vector2 position)
        {
            string countString = count.ToString("D3"); // Display count as three digits (e.g., "001", "012", "123")

            int currentX = (int)position.X;

            foreach (char digit in countString)
            {
                // Draw digits
                int digitIndex = digit - '0'; // Convert char to digit index
                spriteBatch.Draw(digitTextures[digitIndex], new Rectangle(currentX, (int)position.Y, digitWidth, digitHeight), Color.White);

                currentX += digitWidth + digitSpacing - 2;
            }
        }

    }
}

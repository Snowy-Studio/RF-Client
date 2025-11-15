using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.Entity;
using ClientCore.Settings;
using ClientGUI;
using Localization;
using Localization.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAConfig.OptionPanels
{
    public class 地图库 : XNAWindow
    {
        private static 地图库 _instance;
        private XNASuggestionTextBox searchBox;
        private XNADropDown ddType;
        private XNAMultiColumnListBox mapPanel;
        private string[] types;
        private static readonly object _lock = new();
        private int _当前页数 = 1;
        private int _总页数 = 1;

        private int _大小 = 30;

        private XNALabel lblPage;
        private XNAContextMenu _menu;

        private int _reloadToken = 0; // 用于标记本次加载

        // 缓存已安装地图ID，减少磁盘IO
        private HashSet<string> _installedMapIds = new();

        public int 当前页数
        {
            get => _当前页数;
            set
            {
                if (_当前页数 != value)
                {
                    _当前页数 = value;
                    lblPage.Text = $"{_当前页数} / {_总页数}";
                }
            }
        }

        public int 总页数
        {
            get => _总页数;
            set
            {
                if (_总页数 != value)
                {
                    _总页数 = value;
                    lblPage.Text = $"{_当前页数} / {_总页数}";
                }
            }
        }

        // 私有构造函数，防止外部实例化
        private 地图库(WindowManager windowManager) : base(windowManager)
        {
        }

        // 单例访问点
        public static 地图库 GetInstance(WindowManager windowManager)
        {
            需要刷新 = false;

            if (_instance == null)
            {
                lock (_lock) // 线程安全
                {
                    _instance ??= new 地图库(windowManager);
                    _instance.Initialize();
                    DarkeningPanel.AddAndInitializeWithControl(windowManager, _instance);
                }
            }
            return _instance;
        }

        public static bool 需要刷新 = false;

        private List<XNAClientButton> buttons = [];
        private List<Maps> Maps = [];

        public override void Initialize()
        {
            Name = "地图库";
            ClientRectangle = new Rectangle(0, 0, 1000, 700);
            CenterOnParent();

            // 标题
            var titleLabel = new XNALabel(WindowManager)
            {
                Text = "地图库",
                ClientRectangle = new Rectangle(450, 20, 100, 30)
            };

            // 搜索框
            searchBox = new XNASuggestionTextBox(WindowManager)
            {
                ClientRectangle = new Rectangle(50, 60, 200, 25),
                Suggestion = "搜索地图名..."
            };

            var btnSearch = new XNAClientButton(WindowManager)
            {
                Text = "搜索",
                ClientRectangle = new Rectangle(260, 60, UIDesignConstants.BUTTON_WIDTH_75, UIDesignConstants.BUTTON_HEIGHT)
            };
            btnSearch.LeftClick += (sender, args) => { Reload(); };

            ddType = new XNADropDown(WindowManager)
            {
                ClientRectangle = new Rectangle(380, 60, 150, 25),
                Text = "地图类型"
            };

            var 上传地图 = new XNAClientButton(WindowManager)
            {
                Text = "上传地图",
                ClientRectangle = new Rectangle(ddType.Right + 25,ddType.Y,UIDesignConstants.BUTTON_WIDTH_92,UIDesignConstants.BUTTON_HEIGHT)
            };
            上传地图.LeftClick += (_, _) => { FunExtensions.OpenUrl("https://creator.yra2.com/workshop/submit/upload?token=" + UserINISettings.Instance.Token); };

            // 地图列表容器
            mapPanel = new XNAMultiColumnListBox(WindowManager)
            {
                ClientRectangle = new Rectangle(50, 100, 900, 540),
                // BackgroundColor = Color.Black * 0.5f // 半透明背景
            };

            mapPanel.DoubleLeftClick += 查看地图详细信息;

            mapPanel.LineHeight = 30;
            // mapPanel.AddColumn("预览图", 200);
            mapPanel.AddColumn("地图名", 200);
            mapPanel.AddColumn("作者", 160);
            mapPanel.AddColumn("地图类型", 100);
            mapPanel.AddColumn("下载次数", 80);
            mapPanel.AddColumn("评分", 80);
            mapPanel.AddColumn("介绍", 100);
            mapPanel.AddColumn("状态", 80);
            mapPanel.AddColumn("安装", 100);

            mapPanel.RightClick += (sender, args) =>
            {
                _menu.Open(GetCursorPoint());
            };

            _menu = new XNAContextMenu(WindowManager);
            _menu.Name = nameof(_menu);
            _menu.Width = 100;

            _menu.AddItem(new XNAContextMenuItem
            {
                Text = "刷新",
                SelectAction = Reload
            });

            AddChild(_menu);

            types = NetWorkINISettings.Get<string>("dict/getValue?section=map&key=type").Result.Item1?.Split(',') ?? [];

            ddType.AddItem("所有");
            foreach (var type in types)
            {
                ddType.AddItem(type);
            }
            ddType.SelectedIndexChanged += DdType_SelectedIndexChanged;
            ddType.SelectedIndex = 0;

            var btnLeft = new XNAButton(WindowManager)
            {
                ClientRectangle = new Rectangle(mapPanel.X, mapPanel.Bottom + 10, 20, 20),
                HoverTexture = AssetLoader.LoadTexture("left.png"),
                IdleTexture = AssetLoader.LoadTexture("left.png"),
            };
            btnLeft.LeftClick += BtnLeft_LeftClick;

            lblPage = new XNALabel(WindowManager)
            {
                Text = "1 / 1",
                ClientRectangle = new Rectangle(btnLeft.Right + 10, btnLeft.Y, 0, 0)
            };

            var btnRight = new XNAButton(WindowManager)
            {
                ClientRectangle = new Rectangle(btnLeft.Right + 60, btnLeft.Y, 20, 20),
                HoverTexture = AssetLoader.LoadTexture("right.png"),
                IdleTexture = AssetLoader.LoadTexture("right.png"),
            };
            btnRight.LeftClick += BtnRight_LeftClick;

            // 关闭按钮
            var closeButton = new XNAClientButton(WindowManager)
            {
                Text = "关闭",
                X = 820,
                Y = 655,
                // ClientRectangle = new Rectangle(870, 620, 120, 40)
            };
            closeButton.LeftClick += (sender, args) =>
            {
                if (需要刷新)
                {
                    UserINISettings.Instance.重新加载地图和任务包?.Invoke(null,null);
                }
                Disable();
            };

            // 添加控件
            AddChild(titleLabel);
            AddChild(searchBox);
            // AddChild(refreshButton);
            AddChild(mapPanel);
            // AddChild(detailButton);
            AddChild(closeButton);
            AddChild(btnSearch);
            AddChild(ddType);
            AddChild(上传地图);
            AddChild(btnLeft);
            AddChild(btnRight);
            AddChild(lblPage);

            base.Initialize();

            // 初始化已安装地图ID缓存
            RefreshInstalledMapIds();
        }

        // 刷新已安装地图ID缓存
        private void RefreshInstalledMapIds()
        {
            if (!Directory.Exists(ProgramConstants.MAP_PATH))
            {
                _installedMapIds.Clear();
                return;
            }
            _installedMapIds = Directory.GetFiles(ProgramConstants.MAP_PATH, "*.map")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Select(idStr => idStr)
                .Where(id => id != "-1")
                .ToHashSet();
        }

        private void 查看地图详细信息(object sender, EventArgs e)
        {
            mapPanel.SelectedIndex = mapPanel.HoveredIndex;
        }

        private void BtnRight_LeftClick(object sender, EventArgs e)
        {

            if (当前页数 < 总页数)
            {
                当前页数++;
                Reload();
            }
        }

        private void BtnLeft_LeftClick(object sender, EventArgs e)
        {
            if (当前页数 > 1)
            {
                当前页数--;
                Reload();
            }
        }

        private void 查看地图(int i)
        {
            mapPanel.SelectedIndex = i;

            var map = Maps[mapPanel.SelectedIndex];

            var w = new 地图详细信息界面(WindowManager, map.id, types, _installedMapIds.Contains(map.id));
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, w);
            w.EnabledChanged += (_, _) =>
            {
                // 重新刷新已安装地图ID缓存
                RefreshInstalledMapIds();
                Reload();
            };
        }

        private void DdType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Reload();
        }

        private async void Reload()
        {
            // 增加加载token
            int currentToken = ++_reloadToken;

            // 立即清空当前显示
            mapPanel.ClearItems();
            buttons.ForEach(b => mapPanel.RemoveChild(b));
            buttons.Clear();
            Maps = [];

            // 显示加载中提示
            var loadingLabel = new XNALabel(WindowManager)
            {
                Text = "地图列表加载中...",
                ClientRectangle = new Rectangle(450, 350, 200, 40)
            };
            AddChild(loadingLabel);

            try
            {
                var search = searchBox.Text.Trim() == "搜索地图名..." ? string.Empty : searchBox.Text.Trim();
                var ts = ddType.SelectedIndex == 0 ? string.Empty : $"{ddType.SelectedIndex - 1}";

                var r = await NetWorkINISettings.Get<Page<Maps>>($"map/getRelMapsByPage?search={search}&types={ts}&maxPlayers=&pageNum={当前页数}&pageSize={_大小}");

                // 如果token已变，说明有新加载请求，放弃本次加载
                if (currentToken != _reloadToken)
                    return;

                if (r.Item1 == null)
                {
                    XNAMessageBox.Show(WindowManager, "错误", r.Item2);
                    return;
                }

                总页数 = (int)r.Item1.pages;

                // 重新生成按钮和列表
                for (int i = 0; i < _大小; i++)
                {
                    var btn = new XNAClientButton(WindowManager);
                    btn.Width = 75;
                    btn.X = 802;
                    btn.Y = 25 + i * mapPanel.LineHeight;
                    btn.Text = "查看";
                    btn.Tag = i;
                    btn.Visible = false;
                    btn.LeftClick += (_, _) => { 查看地图((int)(btn.Tag)); };

                    buttons.Add(btn);
                    mapPanel.AddChild(btn);
                }

                Maps = r.Item1?.records ?? [];

                for (int i = 0; i < Maps.Count; i++)
                {
                    var map = Maps[i];

                    List<XNAListBoxItem> items = [];

                    var is安装 = _installedMapIds.Contains(map.id);
                    items.Add(new XNAListBoxItem(map.name));
                    items.Add(new XNAListBoxItem(map.author));
                    items.Add(new XNAListBoxItem(types[map.type]));
                    items.Add(new XNAListBoxItem(map.downCount.ToString()));
                    items.Add(new XNAListBoxItem(map.score.ToString()));
                    items.Add(new XNAListBoxItem(map.description));
                    items.Add(new XNAListBoxItem(is安装 ? "已安装" : "未安装"));
                    items.Add(new XNAListBoxItem(string.Empty));
                    mapPanel.AddItem(items);

                    buttons[i].Visible = true;
                }
            }
            finally
            {
                RemoveChild(loadingLabel);
            }
        }

        public class 地图详细信息界面 : XNAWindow
        {
            private Maps map;
            private string[] types;
            private bool is下载;
            private XNAClientButton 下载按钮;
            private int _scoreLevel = -1;
            private readonly string SETTINGS_PATH = "Client\\MapSettings.ini";
            private XNAClientRatingBox _ratingBox;
            private XNAClientButton _btnRatingDone;
            private string mapID;
            private CancellationTokenSource _cts;

            public 地图详细信息界面(WindowManager windowManager, string mapID, string[] types, bool is下载 = true) : base(windowManager)
            {
                this.mapID = mapID;
                this.types = types;
                this.is下载 = is下载;
            }

            public override void Initialize()
            {
                ClientRectangle = new Rectangle(0, 0, 550, 400);

                // 先显示加载中
                var loadingLabel = new XNALabel(WindowManager)
                {
                    Text = "地图详情信息加载中,预计需要1-10秒左右...",
                    ClientRectangle = new Rectangle(140, 180, 270, 40)
                };
                AddChild(loadingLabel);

                int btnWidth = 100;
                int btnHeight = 30;
                int btnX = (ClientRectangle.Width - btnWidth) / 2;
                int btnY = loadingLabel.ClientRectangle.Y + loadingLabel.ClientRectangle.Height + 20;

                var cancelButton = new XNAClientButton(WindowManager)
                {
                    Text = "取消加载",
                    ClientRectangle = new Rectangle(btnX, btnY, btnWidth, btnHeight),
                    IdleTexture = AssetLoader.LoadTexture("75pxbtn.png"),
                    HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png")
                };
                cancelButton.LeftClick += (sender, args) =>
                {
                    _cts?.Cancel();
                    Disable();
                };
                AddChild(cancelButton);

                base.Initialize();
                CenterOnParent();

                _cts = new CancellationTokenSource();

                // 传递cancelToken和cancelButton
                _ = LoadMapAndPreviewAsync(loadingLabel, cancelButton, _cts.Token);
            }

            private async Task LoadMapAndPreviewAsync(XNALabel loadingLabel, XNAClientButton cancelButton, CancellationToken cancelToken)
            {
                try
                {
                    var resultTask = NetWorkINISettings.Get<Maps>($"map/getMapInfo?id={mapID}");
                    var completedTask = await Task.WhenAny(resultTask, Task.Delay(-1, cancelToken));
                    if (cancelToken.IsCancellationRequested)
                        return;

                    var result = await resultTask;
                    map = result.Item1;
                    if (map == null)
                    {
                        RemoveChild(loadingLabel);
                        RemoveChild(cancelButton);
                        var errorLabel = new XNALabel(WindowManager)
                        {
                            Text = "加载失败",
                            ClientRectangle = new Rectangle(270, 180, 200, 40)
                        };
                        AddChild(errorLabel);

                        var closeButton = new XNAClientButton(WindowManager)
                        {
                            Text = "关闭",
                            ClientRectangle = new Rectangle(260, 230, 100, 30),
                            IdleTexture = AssetLoader.LoadTexture("75pxbtn.png"),
                            HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png")
                        };
                        closeButton.LeftClick += (sender, args) => { Disable(); };
                        AddChild(closeButton);

                        return;
                    }

                    // base64图片异步解码，支持取消
                    Texture2D PreviewTexture = null;
                    //if (!string.IsNullOrEmpty(map.base64))
                    //{
                    //    try
                    //    {
                    //        PreviewTexture = await Task.Run(() =>
                    //        {
                    //            cancelToken.ThrowIfCancellationRequested();
                    //            return AssetLoader.LoadTexture(map.img);
                    //        }, cancelToken);
                    //    }
                    //    catch (OperationCanceledException)
                    //    {
                    //        return;
                    //    }
                    //    catch
                    //    {
                    //        PreviewTexture = null;
                    //    }
                    //}

                    PreviewTexture = await AssetLoader.LoadTextureFromUrl(NetWorkINISettings.Address + map.img);

                    // UI更新需在主线程
                    WindowManager.AddCallback(new Action(() =>
                    {
                        RemoveChild(loadingLabel);
                        RemoveChild(cancelButton);
                        BuildDetailUI(PreviewTexture);
                    }));
                }
                catch (OperationCanceledException)
                {
                    // 被取消，直接关闭窗口
                    WindowManager.AddCallback(new Action(() =>
                    {
                        Disable();
                    }));
                }
            }

            private void BuildDetailUI(Texture2D PreviewTexture)
            {
                var 地图预览图 = new XNATextBlock(WindowManager)
                {
                    BackgroundTexture = PreviewTexture,
                    ClientRectangle = new Rectangle(10, 10, 256, 153)
                };

                var 地图名 = new XNALabel(WindowManager)
                {
                    Text = "地图名称：",
                    ClientRectangle = new Rectangle(地图预览图.Right + 20, 地图预览图.Y, 0, 0)
                };

                var 地图名内容 = new XNALabel(WindowManager)
                {
                    Text = map?.name ?? string.Empty,
                    ClientRectangle = new Rectangle(380, 10, 0, 0)
                };

                var 地图作者 = new XNALabel(WindowManager)
                {
                    Text = "地图作者：",
                    ClientRectangle = new Rectangle(地图名.X, 地图名.Bottom + 25, 0, 0)
                };

                var 地图作者内容 = new XNALabel(WindowManager)
                {
                    Text = map?.author ?? string.Empty,
                    ClientRectangle = new Rectangle(地图名内容.X, 地图名内容.Bottom + 25, 0, 0)
                };

                var 地图类型 = new XNALabel(WindowManager)
                {
                    Text = "地图类型：",
                    ClientRectangle = new Rectangle(地图作者.X, 地图作者.Bottom + 25, 0, 0)
                };

                var 地图类型内容 = new XNALabel(WindowManager)
                {
                    Text = (map != null && types != null && map.type >= 0 && map.type < types.Length) ? types[map.type] : string.Empty,
                    ClientRectangle = new Rectangle(地图名内容.X, 地图作者内容.Bottom + 25, 0, 0)
                };

                var 地图评分 = new XNALabel(WindowManager)
                {
                    Text = "地图评分：",
                    ClientRectangle = new Rectangle(地图类型.X, 地图类型.Bottom + 25, 0, 0)
                };

                var 地图评分内容 = new XNALabel(WindowManager)
                {
                    Text = map?.score.ToString() ?? string.Empty,
                    ClientRectangle = new Rectangle(地图类型内容.X, 地图类型内容.Bottom + 25, 0, 0)
                };

                var 下载次数 = new XNALabel(WindowManager)
                {
                    Text = "下载次数：",
                    ClientRectangle = new Rectangle(地图评分.X, 地图评分.Bottom + 25, 0, 0)
                };

                var 下载次数内容 = new XNALabel(WindowManager)
                {
                    Text = map?.downCount.ToString() ?? string.Empty,
                    ClientRectangle = new Rectangle(地图评分内容.X, 地图评分内容.Bottom + 25, 0, 0)
                };

                //_ratingBox = new XNAClientRatingBox(WindowManager)
                //{
                //    ClientRectangle = new Rectangle(下载次数.X, 下载次数内容.Bottom + 25, 100, 30),
                //    Text = "打分".L10N("UI:Main:Rating"),
                //};
                //_ratingBox.CheckedChanged += RatingBox_CheckedChanged;

                //_btnRatingDone = new XNAClientButton(WindowManager)
                //{
                //    Name = nameof(_btnRatingDone),
                //    Text = "打分".L10N("UI:Main:Rating"),
                //    ClientRectangle = new Rectangle(Right - 100, _ratingBox.Y, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
                //};

                //_btnRatingDone.LeftClick += BtnRatingDone_LeftClick;

                var 地图介绍 = new XNATextBlock(WindowManager)
                {
                    Text = InsertLineBreaks(map?.description ?? string.Empty, 35),
                    ClientRectangle = new Rectangle(地图预览图.X, 地图预览图.Bottom + 15, 530, 180),
                    FontIndex = 3
                };

                下载按钮 = new XNAClientButton(WindowManager)
                {
                    ClientRectangle = new Rectangle(地图介绍.X, 地图介绍.Bottom + 10, 100, 30),
                    IdleTexture = AssetLoader.LoadTexture("75pxbtn.png"),
                    HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png")
                };

                下载按钮.LeftClick += 安装;

                if (!is下载)
                {
                    下载按钮.Text = "安装";
                    下载按钮.LeftClick += 安装;
                }
                else
                {
                    下载按钮.Text = "删除";
                    下载按钮.LeftClick += 删除;
                }

                var 关闭按钮 = new XNAClientButton(WindowManager)
                {
                    Text = "关闭",
                    ClientRectangle = new Rectangle(ClientRectangle.Width - 110, 下载按钮.Y, 100, 30),
                    IdleTexture = AssetLoader.LoadTexture("75pxbtn.png"),
                    HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png")
                };

                关闭按钮.LeftClick += (sender, args) =>
                {
                    Disable();
                };

                AddChild(地图预览图);
                AddChild(地图名);
                AddChild(地图名内容);
                AddChild(地图作者);
                AddChild(地图作者内容);
                AddChild(地图类型);
                AddChild(地图类型内容);
                AddChild(地图评分);
                AddChild(地图评分内容);
                AddChild(下载次数);
                //AddChild(_ratingBox);
                //AddChild(_btnRatingDone);
                AddChild(下载次数内容);
                AddChild(地图介绍);
                AddChild(下载按钮);
                AddChild(关闭按钮);
            }

            string InsertLineBreaks(string text, int maxCharsPerLine)
            {
                if (string.IsNullOrEmpty(text)) return text;

                var sb = new StringBuilder();
                int count = 0;
                foreach (var ch in text)
                {
                    sb.Append(ch);
                    count++;
                    if (count >= maxCharsPerLine)
                    {
                        sb.Append('\n'); // 插入换行
                        count = 0;
                    }
                }
                return sb.ToString();
            }

            private void BtnRatingDone_LeftClick(object sender, EventArgs e)
            {
                if (-1 == _scoreLevel)
                {
                    XNAMessageBox.Show(WindowManager, "Info".L10N("UI:Main:Info"), "You haven't scored it yet!".L10N("UI:Main:NotScored"));
                    return;
                }

                string missionName = map.id.ToString();
                var missionPack = string.Empty;
                var brief = string.Empty;
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

                        updateMark(missionName);
                    });

                }
            }

            private void updateMark(string missionName)
            {
                _ = Task.Run(() =>
                {

                });
            }

            private void RatingBox_CheckedChanged(object sender, EventArgs e)
            {
                // if (_lbxCampaignList.SelectedIndex == -1 || _lbxCampaignList.SelectedIndex >= _screenMissions.Count) return;

                XNAClientRatingBox ratingBox = (XNAClientRatingBox)sender;
                if (null != ratingBox)
                {
                    string name = map.id.ToString();
                    _scoreLevel = ratingBox.CheckedIndex + 1;
                    CDebugView.OutputDebugInfo("Mission: {0}, Rating: {1}".L10N("UI:Main:Rating2"), name, _scoreLevel);
                }
            }

            private Task UploadScore(string strName, string missionPack, string brief, int strScore)
            {
                return Task.CompletedTask;
            }

            private void 安装(object sender, EventArgs e)
            {
                下载按钮.Enabled = false;
                try
                {
                    if (!Directory.Exists(ProgramConstants.MAP_PATH))
                        Directory.CreateDirectory(ProgramConstants.MAP_PATH);
                    File.WriteAllText(Path.Combine(ProgramConstants.MAP_PATH, $"{map.id}.map"), map.file);

                var mapIni = new IniFile("Maps\\Multi\\MPMapsMapLibrary.ini");
                var sectionName = "Maps/Multi/MapLibrary/" + map.id;
                mapIni.SetValue(sectionName, "Description", $"[{map.maxPlayers}]{map.name}");
                mapIni.SetValue(sectionName, "GameModes", "常规作战,地图库");
                mapIni.SetValue(sectionName, "Author", map.author);
                mapIni.SetValue(sectionName, "Briefing", map.description);

                    var rules = map.rules?.Split(';') ?? [];
                    for (int i = 1; i <= rules.Length; i++)
                        mapIni.SetValue(sectionName, $"Rule{i}", rules[i - 1]);


                    var enemyHouses = map.enemyHouse?.Split(';') ?? [];
                    for (int i = 1; i <= enemyHouses.Length; i++)
                        mapIni.SetValue(sectionName, $"EnemyHouse{i}", enemyHouses[i - 1]);

                    var allyHouses = map.allyHouse?.Split(';') ?? [];
                    for (int i = 1; i <= allyHouses.Length; i++)
                        mapIni.SetValue(sectionName, $"AllyHouse{i}", allyHouses[i - 1]);

                    mapIni.WriteIniFile();

                    地图库.需要刷新 = true;
                    下载按钮.Text = "删除";
                    下载按钮.LeftClick -= 删除;
                    下载按钮.LeftClick -= 安装;
                    下载按钮.LeftClick += 删除;
                }
                catch (Exception ex)
                {
                    XNAMessageBox.Show(WindowManager, "错误", ex.Message);
                }

                下载按钮.Enabled = true;
            }

            private void 删除(object sender, EventArgs e)
            {
                下载按钮.Enabled = false;
                try
                {
                    File.Delete(Path.Combine(ProgramConstants.MAP_PATH, $"{map.id}.map"));
                    地图库.需要刷新 = true;
                    下载按钮.Text = "安装";
                    下载按钮.LeftClick -= 删除;
                    下载按钮.LeftClick -= 安装;
                    下载按钮.LeftClick += 安装;
                }
                catch (Exception ex)
                {
                    XNAMessageBox.Show(WindowManager, "错误", ex.Message);
                }

                下载按钮.Enabled = true;
            }
        }
    }
}
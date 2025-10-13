using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using ClientCore;
using ClientGUI;
using DTAConfig;
using Ra2Client.Domain.Multiplayer;
using Localization;
using Microsoft.VisualBasic.Logging;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using Rampastring.Tools;
using RandomMapGenerator;

namespace Ra2Client.DXGUI.Multiplayer.GameLobby
{
    class 生成随机地图窗口(WindowManager windowManager, MapLoader mapLoader) : XNAWindow(windowManager)
    {
        private const int OPTIONHEIGHT = 85;

        private XNALabel lblTitle;

        private XNALabel lblClimate;        // 气候
        private XNAClientDropDown ddClimate;

        private XNALabel lblPeople;         // 人数
        private XNAClientDropDown ddPeople;

        private XNAClientCheckBox cbDamage; // 建筑物损伤

        private XNALabel lblSize;
        private XNAClientDropDown ddSize;
        private XNAClientButton btnGenerate;
        private XNAClientButton btnCancel;
        private XNAClientButton btnSave;
        private XNATextBlock tbPreview;

        private XNALabel lblStatus;

        private bool Stop = false;

        private bool isSave;

        private string[] People;

        private string Damage = string.Empty;

        public MapLoader MapLoader = mapLoader;

        public override void Initialize()
        {

            Name = "生成随机地图窗口";
            CenterOnParent();
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
            ClientRectangle = new Rectangle(240, 100, 800, 500);
            BackgroundTexture = AssetLoader.LoadTexture("hotkeyconfigbg.png");

            base.Initialize();

            lblTitle = new XNALabel(WindowManager);
            lblTitle.ClientRectangle = new Rectangle(350, 40, 40, 20);
            lblTitle.Name = "lblTitle";
            lblTitle.CenterOnParentHorizontally();
            lblTitle.Text = "Generate random map".L10N("UI:Main:GenRanMap");

            lblStatus = new XNALabel(WindowManager);
            lblStatus.ClientRectangle = new Rectangle(360, 420, 40, 20);

            btnGenerate = new XNAClientButton(WindowManager);
            btnGenerate.Name = "btnGenerate";
            btnGenerate.ClientRectangle = new Rectangle(350, 460, 100, 20);
            btnGenerate.Text = "Generate".L10N("UI:Main:Generate");
            btnGenerate.IdleTexture = AssetLoader.LoadTexture("92pxbtn.png");
            btnGenerate.HoverTexture = AssetLoader.LoadTexture("92pxbtn_c.png");
            btnGenerate.LeftClick += btnGenerat_LeftClick;


            btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = "btnCancel";
            btnCancel.ClientRectangle = new Rectangle(660, 460, 100, 20);
            btnCancel.Text = "Cancel".L10N("UI:Main:ButtonCancel");
            btnCancel.IdleTexture = AssetLoader.LoadTexture("92pxbtn.png");
            btnCancel.HoverTexture = AssetLoader.LoadTexture("92pxbtn_c.png");
            btnCancel.LeftClick += btnCancel_LeftClick;

            btnSave = new XNAClientButton(WindowManager);
            btnSave.Name = "btnSave";
            btnSave.ClientRectangle = new Rectangle(40, 460, 100, 20);
            btnSave.Text = "Save".L10N("UI:Main:ButtonSave");
            btnSave.IdleTexture = AssetLoader.LoadTexture("92pxbtn.png");
            btnSave.HoverTexture = AssetLoader.LoadTexture("92pxbtn_c.png");
            btnSave.Enabled = false;
            btnSave.LeftClick += btnSave_LeftClick;

            lblClimate = new XNALabel(WindowManager);
            lblClimate.ClientRectangle = new Rectangle(40, OPTIONHEIGHT + 2, 40, 20);
            lblClimate.Text = "Climatic".L10N("UI:Main:Climatic");

            ddClimate = new XNAClientDropDown(WindowManager);
            ddClimate.ClientRectangle = new Rectangle(lblClimate.X + 75, OPTIONHEIGHT + 2, 80, 20);
            XNADropDownItem Desert = new XNADropDownItem();
            Desert.Text = "DESERT".L10N("UI:Main:DESERT");
            Desert.Tag = "DESERT";
            XNADropDownItem Newurban = new XNADropDownItem();
            Newurban.Text = "NEWURBAN".L10N("UI:Main:NEWURBAN"); ;
            Newurban.Tag = "NEWURBAN";
            XNADropDownItem Temperate = new XNADropDownItem();
            Temperate.Text = "TEMPERATE".L10N("UI:Main:TEMPERATE"); ;
            Temperate.Tag = "TEMPERATE";
            XNADropDownItem Temperate_Islands = new XNADropDownItem();
            Temperate_Islands.Text = "Islands".L10N("UI:Main:Islands"); ;
            Temperate_Islands.Tag = "TEMPERATE_Islands";

            tbPreview = new XNATextBlock(WindowManager);
            tbPreview.ClientRectangle = new Rectangle(100, 150, 600, 250);
            tbPreview.BackgroundTexture = AssetLoader.LoadTexture("gamecreationoptionsbg.png");
            tbPreview.Name = "tbPreview";
            tbPreview.DrawBorders = false;


            ddClimate.AddItem("Random".L10N("UI:Main:Random"));
            ddClimate.AddItem(Temperate);
            ddClimate.AddItem(Temperate_Islands);
            ddClimate.AddItem(Newurban);
            ddClimate.AddItem(Desert);
            ddClimate.SelectedIndex = 0;

            lblPeople = new XNALabel(WindowManager);
            lblPeople.ClientRectangle = new Rectangle(ddClimate.X + 100, OPTIONHEIGHT + 2, 40, 20);
            lblPeople.Text = "Number".L10N("UI:Main:Number");

            ddPeople = new XNAClientDropDown(WindowManager);
            ddPeople.ClientRectangle = new Rectangle(lblPeople.X + 75, OPTIONHEIGHT, 80, 20);
            ddPeople.AddItem("Random".L10N("UI:Main:Random"));


            for (int i = 2; i <= 8; i++)
            {
                ddPeople.AddItem(i.ToString());
            }
            ddPeople.SelectedIndex = 0;

            lblSize = new XNALabel(WindowManager);
            lblSize.ClientRectangle = new Rectangle(ddPeople.X + 100, OPTIONHEIGHT + 2, 40, 20);
            lblSize.Text = "Size".L10N("UI:Main:Size");

            ddSize = new XNAClientDropDown(WindowManager);
            ddSize.ClientRectangle = new Rectangle(lblSize.X + 75, OPTIONHEIGHT, 80, 20);
            ddSize.AddItem("small".L10N("UI:Main:small"));
            ddSize.AddItem("medium".L10N("UI:Main:medium"));
            ddSize.AddItem("big".L10N("UI:Main:big"));
            ddSize.AddItem("Very big".L10N("UI:Main:Verybig"));
            ddSize.SelectedIndex = 1;


            cbDamage = new XNAClientCheckBox(WindowManager);
            cbDamage.ClientRectangle = new Rectangle(ddSize.X + 120, OPTIONHEIGHT, 40, 20);
            cbDamage.Text = "Random building damage".L10N("UI:Main:RanBuildDamage");


            //thread.Abort()
            AddChild(lblTitle);
            AddChild(lblStatus);
            AddChild(tbPreview);

            AddChild(lblClimate);
            AddChild(ddClimate);

            AddChild(lblPeople);
            AddChild(ddPeople);

            AddChild(lblSize);
            AddChild(ddSize);

            AddChild(cbDamage);
            AddChild(btnGenerate);
            AddChild(btnCancel);
            AddChild(btnSave);
        }

        public string GetIsSave()
        {
            tbPreview.BackgroundTexture = AssetLoader.LoadTexture("gamecreationoptionsbg.png");
            return isSave ? lastName:string.Empty;
        }

        private void btnCancel_LeftClick(object sender, EventArgs e)
        {
            if (tbPreview.Visible)
            {
                try
                {
                    if (File.Exists($"{lastName}.png"))
                        File.Delete($"{lastName}.png");
                    if (File.Exists($"{lastName}.map"))
                        File.Delete($"{lastName}.map");
                }
                catch(Exception ex)
                {
                    Logger.Log($"删除随机地图失败，原因：{ex}");
                }
            }

            tbPreview.BackgroundTexture = AssetLoader.LoadTexture("gamecreationoptionsbg.png");
            tbPreview.Visible = false;
            btnSave.Enabled = false;
            isSave = false;
            Disable();
        }

        private void btnSave_LeftClick(object sender, EventArgs e)
        {
            tbPreview.BackgroundTexture = AssetLoader.LoadTexture("gamecreationoptionsbg.png");
            isSave = true;
            Disable();
        }

        private string lastName;

        private void btnGenerat_LeftClick(object sender, EventArgs e)
        {

            tbPreview.BackgroundTexture = AssetLoader.LoadTextureUncached("gamecreationoptionsbg.png");
            var t = new Thread(() =>
            {
                WindowManager.progress.Report("正在生成随机地图...");
                //string strCmdText;
                Random r = new Random();
                string Generate = (string)ddClimate.SelectedItem.Tag;
                if (ddClimate.SelectedIndex == 0)
                {
                    Generate = (string)ddClimate.Items[r.Next(1, 5)].Tag;
                }

                int sizex = 35 * (ddSize.SelectedIndex + 1) + r.Next(30, 50);
                int sizey = 35 * (ddSize.SelectedIndex + 1) + r.Next(30, 50);

                People = GetPeople(ddPeople.SelectedItem.Text);

                var option = new Options(){
                    Width = sizex,
                    Height = sizey,
                    NW = int.Parse(People[0]),
                    SE = int.Parse(People[1]),
                    NE = int.Parse(People[2]),
                    SW = int.Parse(People[3]),
                    S = int.Parse(People[4]),
                    W = int.Parse(People[5]),
                    E = int.Parse(People[6]),
                    N = int.Parse(People[7]),
                    DamangedBuilding = cbDamage.Checked,
                    Type = Generate,
                    Gamemode = "Ares",
                    输出目录 = ProgramConstants.GamePath + "Maps\\Multi\\Custom\\",
                };

                try
                {
                    随机地图生成.RunOptions(option);

                    RenderImage.RenderOneImage($"Maps/Multi/Custom/随机地图.map");
                }
                catch (Exception ex)
                {
                    lblStatus.Text = "error".L10N("UI:Main:error");
                }
                Stop = true;

            });
            t.Start();

            Thread t2 = new Thread(() =>
            {
                btnGenerate.Enabled = false;
                btnSave.Enabled = false;
                btnCancel.Enabled = false;
                // lblStatus.ClientRectangle = new Rectangle(100, 420, 300, 20);
                string[] TextList = { "Is dispersing civilians.".L10N("UI:Main:GenText1"), "Ore being mined".L10N("UI:Main:GenText2"), "The base construction vehicles are being loaded".L10N("UI:Main:GenText3"), "Ammunition being examined".L10N("UI:Main:GenText4"), "Bobosa is being distributed for mobilization".L10N("UI:Main:GenText5"), "Getting the Phantom tank familiar with the environment".L10N("UI:Main:GenText6"), "The police dogs are being calmed".L10N("UI:Main:GenText7"), "Catching dolphins".L10N("UI:Main:GenText8"), "Bargaining with the logistics".L10N("UI:Main:GenText9"), "The transport plane is being refuelled".L10N("UI:Main:GenText10"), "We're sinking the submarine".L10N("UI:Main:GenText11"), "The building is being painted".L10N("UI:Main:GenText12") };

                while (!Stop)
                {
                    Thread.Sleep(300);
                    var r = new Random();
                    lblStatus.Text = TextList[r.Next(TextList.Length)];
                }
                if (Stop)
                {
                    var png = new FileInfo("Maps/Multi/Custom/随机地图.jpg");

                    var map = new FileInfo("Maps/Multi/Custom/随机地图.map");
                    try
                    {

                        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                        lastName = $"Maps/Multi/Custom/随机地图{timestamp}";

                        // 构建文件名
                        string pngName = $"{lastName}.jpg";

                        string mapName = $"{lastName}.map";

                        // 移动文件
                        png.MoveTo(pngName);
                        map.MoveTo(mapName);

                        // 加载背景纹理
                        tbPreview.BackgroundTexture = AssetLoader.LoadTextureUncached(pngName);
                        lblStatus.Text = "completed".L10N("UI:Main:completed");
                        Stop = false;
                    }
                    catch
                    {
                        lblStatus.Text = "error".L10N("UI:Main:error");
                        btnGenerate.Enabled = true;
                        btnCancel.Enabled = true;
                       Stop = false;
                        return;
                    }
                    WindowManager.progress.Report(string.Empty);
                }
                btnGenerate.Enabled = true;
                btnSave.Enabled = true;
                tbPreview.Visible = true;
                btnCancel.Enabled = true;
            });
            t2.Start();
        }


        private string[] GetPeople(string Peoples)
        {
            int[] p = { 0, 0, 0, 0, 0, 0, 0, 0 };
            int Current;
            var r = new Random();
            if (Peoples == "Random".L10N("UI:Main:Random"))
                Current = r.Next(2, 8);
            else
                Current = int.Parse(Peoples);

            while (Current > 0)
            {

                p[r.Next(8)]++;

                Current--;
            }
            return string.Join(",", p).Split(',');
        }

    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClientCore.Entity;
using ClientCore.Settings;
using ClientGUI;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAConfig
{
    public class ThankWindow : XNAWindow
    {
        public XNAListBox lblThankList;

        public ThankWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        public override void Initialize()
        {
            Name = "ThankWindow";
            ClientRectangle = new Rectangle(0, 0, 334, 453);

            var lblCheater = new XNALabel(WindowManager);
            lblCheater.Name = "lblCheater";
            lblCheater.ClientRectangle = new Rectangle(0, 0, 0, 0);
            lblCheater.FontIndex = 1;
            lblCheater.Text = "Thanks".L10N("UI:DTAConfig:ButtonThanks");

            lblThankList = new XNAListBox(WindowManager);
            lblThankList.Name = nameof(lblThankList);
            lblThankList.ClientRectangle = new Rectangle(15, lblCheater.Y + 35, 300, 370);
            lblThankList.FontIndex = 1;
            lblThankList.LineHeight = 30;

            var btnYes = new XNAClientButton(WindowManager);
            btnYes.Name = "btnYes";
            btnYes.ClientRectangle = new Rectangle((Width - UIDesignConstants.BUTTON_WIDTH_92) / 2,
                Height - 35, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
            btnYes.Text = "Yes".L10N("UI:Main:Yes");
            btnYes.LeftClick += BtnYes_LeftClick;

            AddChild(lblThankList);
            AddChild(lblCheater);
            AddChild(btnYes);

            lblCheater.CenterOnParent();
            lblCheater.ClientRectangle = new Rectangle(lblCheater.X, 12,
                lblCheater.Width, lblCheater.Height);

            Task.Run(SynchronizedList);

            base.Initialize();
        }

        private async void SynchronizedList()
        {
           var r = await NetWorkINISettings.Get<List<Thank>>("thanks/getAllThanks");
            if (r.Item1 != null)
            {
                lblThankList.Clear();
                foreach (var item in r.Item1)
                {
                    lblThankList.AddItem($"{item.content} By {item.author}");
                }
            }
            else
                LoadDefaultList();
        }


        private void LoadDefaultList()
        {
            List<string> lstItems =
            [
                "CNC平台：CNCNet",
                "游戏平台：Ares，Phobos",
                "随机地图：韩方序",
                "重置战役：MadHQ",
                "地图：MadHQ",
                "二次元主题：Blue623",
                "地图编辑器：FA2SP制作组",
                "中文语音包：蚂蚁制作组",
                "(皮肤)雷达样式2：=Star=",
                "(皮肤)血条：雷德克里莫",
                "(皮肤)水面箱子样式2：雷德克里莫",
                "(皮肤)超时空动画样式2：ppap11404",
                "(皮肤)无畏级战舰样式2：snmiglight",
                "(皮肤)恐怖机器人样式2：BoundaryHand",
                "(皮肤)基洛夫空艇样式2：布加迪",
                "(皮肤)入侵者战机样式2：Creator",
                "(皮肤)黑鹰战机样式2：Creator",
                "(皮肤)建造UI：Aaron_Kka",
                "(皮肤)盟军高科样式2：雷德克里莫",
                "(皮肤)盟军重工样式2：雷德克里莫",
                "(皮肤)盟军电厂样式2：雷德克里莫",
                "(皮肤)盟军矿场样式2：雷德克里莫",
                "(皮肤)盟军兵营样式2：雷德克里莫 lalalayuan77",
                "(皮肤)苏军高科样式2：雷德克里莫",
                "(皮肤)苏军重工样式2：雷德克里莫",
                "(皮肤)苏军电厂样式2：雷德克里莫",
                "(皮肤)苏军矿场样式2：雷德克里莫",
                "(皮肤)苏军兵营样式2：雷德克里莫",
                "(皮肤)苏军雷达样式3：雷德克里莫",
                "(皮肤)苏军基地样式2：xuetianyi",
                "(皮肤)盟军基地样式2：ruanhuhu,qwqwq",
                "(皮肤)盟军基地样式3：xuetianyi",
                "(皮肤)灯光：雷德克里莫",
                "(皮肤)脑车样式2：cyanideT",
                "(皮肤)城市地形：凌..",
                "(皮肤)防空炮样式2：HG_SCIPCION deathreaperz",
                "(皮肤)爱国者样式2：HG_SCIPCION deathreaperz",
                "(皮肤)哨戒炮样式2：HG_SCIPCION deathreaperz",
                "(皮肤)神盾样式2：13220379104",
                "(皮肤)维修样式2：Foehn焚风",
                "(皮肤)狂风坦克样式2：1437",
                "(皮肤)武装直升机样式2：167784",
                "(皮肤)雷鸣潜艇样式2：Enterprise-企业",
                "(皮肤)指针样式2：雷德克里莫",
                "冷场AI：韩方序",
                "马王神AI：MammamiaMadman",
                "尤里版新共辉：达达利亚",
                "尤里共和国：核武将军",
                "日语语音：高级复读兽响子",
                "简体中文游戏：手柄",
                "地图：达达利亚",
                "bug修复：ruanhuhu,边缘星2020",
                "汉化：黑色圣杯",
                "服务器赞助: 精武止戈 Peng",
            ];

            lblThankList.Clear();
            lstItems.ForEach(lblThankList.AddItem);
      
        }



        private void BtnYes_LeftClick(object sender, EventArgs e)
        {
            Disable();
        }
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClientCore;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;

namespace ClientGUI
{
   public class YRPathWindow(WindowManager windowManager) : XNAWindow(windowManager)
    {
        public override void Initialize()
        {

            var firstLabel = new XNALabel(WindowManager)
            {
                ClientRectangle = new Rectangle(20, 10, 0, 0),
                Text = "选择纯净尤里的复仇目录\n（这个对话框通常不会存在，如果你没有修改游戏文件，那也可能是安装目录太长了）"
            };


            BackgroundTexture = AssetLoader.LoadTexture("msgboxform.png");
            var tbxName = new XNASuggestionTextBox(WindowManager)
            {
                ClientRectangle = new Rectangle(20, 50, 350, 25),
                Name = "nameTextBox",
                Suggestion = "点击以选择纯净尤里的复仇目录",

            };

            tbxName.LeftClick += (_, _) =>
            {
                using var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
                folderDialog.Description = "请选择纯净尤里的复仇目录";
                folderDialog.ShowNewFolderButton = false;

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    tbxName.Text = folderDialog.SelectedPath;
                }
            };

            var btnConfirm = new XNAClientButton(WindowManager)
            {
                ClientRectangle = new Rectangle(20, 90, UIDesignConstants.BUTTON_WIDTH_160, UIDesignConstants.BUTTON_HEIGHT),
                Text = "确认"
            };
            btnConfirm.LeftClick += (sender, e) =>
            {
                if (!ProgramConstants.判断目录是否为纯净尤复(tbxName.Text))
                {
                    XNAMessageBox.Show(windowManager, "提示", "您选择的目录不是纯净尤复目录，请重新选择");
                    return;
                }


                UserINISettings.Instance.YRPath.Value = tbxName.Text;
                UserINISettings.Instance.SaveSettings();
                Disable();

            };

            ClientRectangle = new Rectangle(0, 0, tbxName.Right + 24, btnConfirm.Y + 40);


            base.Initialize();

            AddChild(firstLabel);
            AddChild(tbxName);
            AddChild(btnConfirm);

            WindowManager.CenterControlOnScreen(this);

        }
        public void Show()
        {
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, this);
        }
    }
}

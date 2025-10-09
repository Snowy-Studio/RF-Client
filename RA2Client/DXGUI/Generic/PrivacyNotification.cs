using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System.Windows.Forms;
using ClientCore;
using ClientGUI;
using System;

namespace Ra2Client.DXGUI.Generic
{
    /// <summary>
    /// A notification that asks the user to accept the CnCNet privacy policy.
    /// </summary>
    public class PrivacyNotification : XNAWindow
    {
        private Timer _timer;
        private int CountSec = 10;

        private XNALabel lblDescription;
        private XNALabel lblMoreInformation;
        private XNALabel lblTermsAndConditions;
        private XNALabel lblPrivacyPolicy;
        private XNALabel lblExplanation;
        private XNAClientButton btnOK;
        public delegate void BoilerLogHandler();
        public event BoilerLogHandler BoilerEventLog;
        public PrivacyNotification(WindowManager windowManager) : base(windowManager)
        {
            //DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
            _timer = new Timer();
        }

        public override void Initialize()
        {
            Name = nameof(PrivacyNotification);
            ClientRectangle = new Rectangle(0, 0, 800, 300);
            BackgroundTexture = AssetLoader.LoadTexture("updatequerybg.png");

            lblDescription = new XNALabel(WindowManager);
            lblDescription.Name = nameof(lblDescription);
            lblDescription.ClientRectangle = new Rectangle(75, 15, 0, 0);
            lblDescription.Text = Renderer.FixText("使用此客户端即代表您同意CnCNet条款和CnCNet隐私政策,隐私相关设置可在客户端中设置.",
                lblDescription.FontIndex, WindowManager.RenderResolutionX - (UIDesignConstants.EMPTY_SPACE_SIDES * 2)).Text;
            AddChild(lblDescription);

            lblMoreInformation = new XNALabel(WindowManager);
            lblMoreInformation.Name = nameof(lblMoreInformation);
            lblMoreInformation.ClientRectangle = new Rectangle(75, 40, 0, 0);
            lblMoreInformation.Text = "更多信息:";
            AddChild(lblMoreInformation);

            lblTermsAndConditions = new XNALinkLabel(WindowManager);
            lblTermsAndConditions.Name = nameof(lblTermsAndConditions);
            lblTermsAndConditions.ClientRectangle = new Rectangle(150, 40, 0, 0);
            lblTermsAndConditions.Text = "https://cncnet.org/terms-and-conditions";
            lblTermsAndConditions.LeftClick += (s, e) => ProcessLauncher.StartShellProcess(lblTermsAndConditions.Text);
            AddChild(lblTermsAndConditions);

            lblPrivacyPolicy = new XNALinkLabel(WindowManager);
            lblPrivacyPolicy.Name = nameof(lblPrivacyPolicy);
            lblPrivacyPolicy.ClientRectangle = new Rectangle(lblTermsAndConditions.Right + 10, lblTermsAndConditions.Y, 0, 0);
            lblPrivacyPolicy.Text = "https://cncnet.org/privacy-policy";
            lblPrivacyPolicy.LeftClick += (s, e) => ProcessLauncher.StartShellProcess(lblPrivacyPolicy.Text);
            AddChild(lblPrivacyPolicy);

            lblExplanation = new XNALabel(WindowManager);
            lblExplanation.Name = nameof(lblExplanation);
            lblExplanation.ClientRectangle = new Rectangle(75, 80, 0, 0);
            lblExplanation.Text = "重聚未来官网: www.yra2.com (备用地址: www.ra2yr.com)\r\n\r\n" +
                                  "根据相关法律法规,您有权知道我们需要收集的信息,这包含您的设备标识符以及您的设备信息。\r\n\r\n" +
                                  "此Mod完全免费,从下载到安装以及后续技术支持等不存在任何付费情况。\r\n\r\n" +
                                  "如果您喜欢这个Mod,可以到官网的赞助界面请作者喝杯奶茶。\r\n\r\n" +
                                  "请勿相信在任何第三方不明渠道购买和下载此Mod,谨防上当受骗！\r\n\r\n" +
                                  "注意: 如果您在使用时遇到问题,请一定要确保是最新版本,已停止维护的旧版本出现问题概不负责！！！";
            lblExplanation.TextColor = UISettings.ActiveSettings.SubtleTextColor;
            AddChild(lblExplanation);

            btnOK = new XNAClientButton(WindowManager);
            btnOK.Name = nameof(btnOK);
            btnOK.ClientRectangle = new Rectangle(320, 270, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnOK.Text = "10s";
            btnOK.Enabled = false;
            AddChild(btnOK);
            btnOK.LeftClick += (s, e) =>
            {
                UserINISettings.Instance.PrivacyPolicyAccepted.Value = true;
                UserINISettings.Instance.SaveSettings();
                
                Disable();
                BoilerEventLog?.Invoke();
                WindowManager.progress.Report(string.Empty);
            };

            base.Initialize();
            CenterOnParent();

            _timer.Tick += TimerCallback;
            _timer.Interval = 1000;
            _timer.Start();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Alpha <= 0.0)
                Disable();
        }
#nullable enable
        private void TimerCallback(object? sender, EventArgs e)
        {
            btnOK.Text = string.Format("请阅读以上内容({0})", CountSec);
            CountSec--;
            if(0 > CountSec)
            {
                _timer.Stop();
                CountSec = 0;
                btnOK.Text = "已阅读并同意喵~";
                btnOK.Enabled = true;
            }
        }
#nullable restore
    }
}

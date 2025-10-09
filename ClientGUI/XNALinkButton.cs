using System;
using ClientCore;
using Rampastring.Tools;
using Rampastring.XNAUI;

namespace ClientGUI
{
    public class XNALinkButton : XNAClientButton
    {
        public XNALinkButton(WindowManager windowManager) : base(windowManager)
        {
        }

        public string URL { get; set; }

        private ToolTip toolTip;

        private void CreateToolTip()
        {
            if (toolTip == null)
                toolTip = new ToolTip(WindowManager, this);
        }

        public override void Initialize()
        {
            base.Initialize();

            CreateToolTip();
        }

        public override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            if (key == "URL")
            {
                URL = value;
                return;
            }
            else if (key == "ToolTip")
            {
                CreateToolTip();
                toolTip.Text = value.Replace("@", Environment.NewLine);
                return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        public override void OnLeftClick()
        {
            if(URL!=null)
                ProcessLauncher.StartShellProcess(URL);

            base.OnLeftClick();
        }
    }
}
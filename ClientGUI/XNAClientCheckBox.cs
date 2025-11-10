using System;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI
{
    public class XNAClientCheckBox : XNACheckBox
    {
        private ToolTip ToolTip { get; set; }
        private string _toolTipText { get; set; }

        public XNAClientCheckBox(WindowManager windowManager) : base(windowManager)
        {
        }

        private void CreateToolTip()
        {
            ToolTip ??= new ToolTip(WindowManager, this);
        }

        public override void Initialize()
        {
            CheckSoundEffect = new EnhancedSoundEffect("checkbox.wav");

            base.Initialize();

            CreateToolTip();
            SetToolTipText(_toolTipText);
        }

        public void SetToolTipText(string text)
        {
            _toolTipText = text ?? string.Empty;
            if (ToolTip != null)
                ToolTip.Text = _toolTipText;
        }

        public override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            if (key == "ToolTip")
            {
                CreateToolTip();
                SetToolTipText(value.Replace("@", Environment.NewLine));
                return;
            }
            if (key == "$ToolTip")
            {
                CreateToolTip();
                SetToolTipText(value.Replace("@", Environment.NewLine));
                return;
            }
            base.ParseControlINIAttribute(iniFile, key, value);
        }
    }
}

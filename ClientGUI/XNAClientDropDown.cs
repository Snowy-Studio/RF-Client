using System;
using Localization;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI
{
    public class XNAClientDropDown : XNADropDown
    {
        public ToolTip ToolTip { get; set; }
        private string _toolTipText { get; set; }

        public XNAClientDropDown(WindowManager windowManager) : base(windowManager)
        {
        }

        private void CreateToolTip()
        {
            ToolTip ??= new ToolTip(WindowManager, this);
        }

        public override void Initialize()
        {
            ClickSoundEffect = new EnhancedSoundEffect("dropdown.wav");

            

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

        public override void OnMouseLeftDown()
        {
            base.OnMouseLeftDown();
            UpdateToolTipBlock();
        }

        protected override void CloseDropDown()
        {
            base.CloseDropDown();
            UpdateToolTipBlock();
        }

        protected void UpdateToolTipBlock()
        {
            if (DropDownState == DropDownState.CLOSED)
                ToolTip.Blocked = false;
            else
                ToolTip.Blocked = true;
        }
    }
}

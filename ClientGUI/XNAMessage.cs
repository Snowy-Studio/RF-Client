using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI
{
    public class XNAMessage : XNAWindow
    {
        private XNALabel lblCaption;
        private XNALabel lblDescription;

        public XNAMessage(WindowManager windowManager) : base(windowManager)
        {
            lblCaption = new XNALabel(WindowManager)
            {
                Text = "",
                ClientRectangle = new Rectangle(12, 9, 0, 0)
            };

            lblDescription = new XNALabel(WindowManager)
            {
                Text = "",
                ClientRectangle = new Rectangle(12, 39, 0, 0)
            };

            AddChild(lblCaption);
            AddChild(lblDescription);
        }

        private string _caption;
        public string caption
        {
            set => lblCaption.Text = value;
            get => _caption;
        }

        private string _description;
        public string description
        {
            set => UpdateLabelText(ref _description, lblDescription, value, true);
            get => _description;
        }

        private void UpdateLabelText(ref string field, XNALabel label, string value, bool updateSize = false)
        {
            field = value;
            label.Text = value;
            if (updateSize)
            {
                Vector2 textDimensions = Renderer.GetTextDimensions(value, label.FontIndex);
                ClientRectangle = new Rectangle(0, 0, (int)textDimensions.X + 24, (int)textDimensions.Y + 81);
                WindowManager.CenterControlOnScreen(this);
            }
        }

        public override void Initialize()
        {
            Name = "MessageBox";
           
            base.Initialize();
        }

        public void Show()
        {
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, this);
        }
    }

}

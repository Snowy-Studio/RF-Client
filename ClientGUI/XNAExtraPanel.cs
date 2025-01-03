﻿using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI
{
    /// <summary>
    /// An "extra panel" for modders that automatically
    /// changes its size to match the texture size.
    /// </summary>
    public class XNAExtraPanel : XNAPanel
    {
        public XNAExtraPanel(WindowManager windowManager) : base(windowManager)
        {
            InputEnabled = false;
            DrawBorders = false;
        }

        public override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            if (key == "BackgroundTexture")
            {
                BackgroundTexture = AssetLoader.LoadTexture(value);

                if (new Point(Width, Height) == Point.Zero)
                {
                    ClientRectangle = new Rectangle(X, Y,
                        BackgroundTexture.Width, BackgroundTexture.Height);
                }

                return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }
    }
}

﻿using Microsoft.Xna.Framework;
using System;

namespace Rampastring.XNAUI.XNAControls;

public class MouseEventArgs : EventArgs
{
    public MouseEventArgs(Point relativeLocation)
    {
        RelativeLocation = relativeLocation;
    }

    /// <summary>
    /// The point of the mouse cursor relative to the control.
    /// </summary>
    public Point RelativeLocation { get; set; }
}

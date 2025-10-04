using Microsoft.Xna.Framework;
using System;

namespace Rampastring.XNAUI.XNAControls;

public class ControlEventArgs : EventArgs
{
    public ControlEventArgs(XNAControl control) => Control = control;

    public XNAControl Control { get; }
}
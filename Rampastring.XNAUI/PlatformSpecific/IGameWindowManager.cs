﻿using System;
using System.Windows.Forms;

namespace Rampastring.XNAUI.PlatformSpecific;

internal interface IGameWindowManager
{
    event EventHandler GameWindowClosing;
    event EventHandler ClientSizeChanged;

    void AllowClosing();
#if !NETFRAMEWORK
    [System.Runtime.Versioning.SupportedOSPlatform("windows5.1.2600")]
#endif
    void FlashWindow();
    IntPtr GetWindowHandle();
    void HideWindow();
    void MaximizeWindow();
    void MinimizeWindow();
    void PreventClosing();
    void SetControlBox(bool value);
    void SetIcon(string path);
    void ShowWindow();
    int GetWindowWidth();
    int GetWindowHeight();
    void SetFormBorderStyle(FormBorderStyle borderStyle);
    bool HasFocus();
    void CenterOnScreen();
    void SetBorderlessMode(bool value);
}
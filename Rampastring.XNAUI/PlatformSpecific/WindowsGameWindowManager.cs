using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;

namespace Rampastring.XNAUI.PlatformSpecific;

/// <summary>
/// Manages the game window on Windows.
/// </summary>
internal class WindowsGameWindowManager : IGameWindowManager
{
    public WindowsGameWindowManager(Game game)
    {
        this.game = game;

        game.Window.ClientSizeChanged += GameForm_ClientSizeChanged;

        gameForm = (Form)Control.FromHandle(game.Window.Handle);

        if (gameForm != null)
        {
            gameForm.FormClosing += GameForm_FormClosing_Event;
            gameForm.ResizeEnd += GameForm_ClientSizeChanged;
        }

    }



    private Form gameForm;

    private bool closingPrevented = false;

    public event EventHandler GameWindowClosing;


    public event EventHandler ClientSizeChanged;

    private Game game;


    private void GameForm_FormClosing_Event(object sender, FormClosingEventArgs e)
    {
        GameWindowClosing?.Invoke(this, EventArgs.Empty);
    }


    private void GameForm_ClientSizeChanged(object sender, EventArgs e)
    {

        if (gameForm == null)
            return;

        ClientSizeChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Centers the game window on the screen.
    /// </summary>
    public void CenterOnScreen()
    {

        if (gameForm == null) 
            return;

        var screen = System.Windows.Forms.Screen.FromHandle(gameForm.Handle);
        int currentWidth = screen.Bounds.Width;
        int currentHeight = screen.Bounds.Height;
        int x = (currentWidth - game.Window.ClientBounds.Width) / 2;
        int y = (currentHeight - game.Window.ClientBounds.Height) / 2;
        int screenX = screen.Bounds.X;
        int screenY = screen.Bounds.Y;


#if XNA
        gameForm.DesktopLocation = new System.Drawing.Point(x, y);
#else
        game.Window.Position = new Microsoft.Xna.Framework.Point(screenX + x, screenY + y);
#endif
    }

    /// <summary>
    /// Enables or disables borderless windowed mode.
    /// </summary>
    /// <param name="value">A boolean that determines whether borderless 
    /// windowed mode should be enabled.</param>
    public void SetBorderlessMode(bool value)
    {
#if !XNA
        game.Window.IsBorderless = value;
#else
        if (value)
            gameForm.FormBorderStyle = FormBorderStyle.None;
        else
            gameForm.FormBorderStyle = FormBorderStyle.FixedSingle;
#endif
    }


    /// <summary>
    /// Minimizes the game window.
    /// </summary>
    public void MinimizeWindow()
    {
        if (gameForm == null)
            return;

        gameForm.WindowState = FormWindowState.Minimized;
    }

    /// <summary>
    /// Maximizes the game window.
    /// </summary>
    public void MaximizeWindow()
    {
        if (gameForm == null)
            return;

        gameForm.WindowState = FormWindowState.Normal;
    }

    /// <summary>
    /// Hides the game window.
    /// </summary>
    public void HideWindow()
    {
        if (gameForm == null)
            return;

        gameForm.Hide();
    }

    /// <summary>
    /// Shows the game window.
    /// </summary>
    public void ShowWindow()
    {
        if (gameForm == null)
            return;

        gameForm.Show();
    }

    /// <summary>
    /// Flashes the game window on the taskbar.
    /// </summary>
#if NET5_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatform("windows5.1.2600")]
#endif
    public void FlashWindow()
    {
        if (gameForm == null)
            return;

        WindowFlasher.FlashWindowEx(gameForm.Handle);
    }

    /// <summary>
    /// Sets the icon of the game window to an icon that exists on a specific
    /// file path.
    /// </summary>
    /// <param name="path">The path to the icon file.</param>
    public void SetIcon(string path)
    {
        if (gameForm == null)
            return;

        gameForm.Icon = Icon.ExtractAssociatedIcon(SafePath.GetFile(path).FullName);
    }

    /// <summary>
    /// Returns the IntPtr handle of the game window on Windows.
    /// On other platforms, returns IntPtr.Zero.
    /// </summary>
    public IntPtr GetWindowHandle()
    {
        if (gameForm == null)
            return IntPtr.Zero;

        return gameForm.Handle;
    }

    /// <summary>
    /// Enables or disables the "maximize box" for the game form.
    /// </summary>
    public void SetMaximizeBox(bool value)
    {
        if (gameForm != null)
            gameForm.MaximizeBox = value;
    }

    /// <summary>
    /// Enables or disables the "control box" (minimize/maximize/close buttons) for the game form.
    /// </summary>
    /// <param name="value">True to enable the control box, false to disable it.</param>
    public void SetControlBox(bool value)
    {
        if (gameForm == null)
            return;

        gameForm.ControlBox = value;
    }

    /// <summary>
    /// Prevents the user from closing the game form by Alt-F4.
    /// </summary>
    public void PreventClosing()
    {
        if (gameForm == null)
            return;

        if (!closingPrevented)
            gameForm.FormClosing += GameForm_FormClosing;
        closingPrevented = true;
    }

    private void GameForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        e.Cancel = true;
    }

    /// <summary>
    /// Allows the user to close the game form by Alt-F4.
    /// </summary>
    public void AllowClosing()
    {
        if (gameForm == null)
            return;

        gameForm.FormClosing -= GameForm_FormClosing;
        closingPrevented = false;
    }

    public bool HasFocus()
    {
        if (gameForm == null)
            return game.IsActive;

        return Form.ActiveForm != null;
    }

    public int GetWindowWidth() => gameForm.Width;

    public int GetWindowHeight() => gameForm.Height;

    public void SetFormBorderStyle(FormBorderStyle borderStyle)
    {
#if !XNA
        if (borderStyle != FormBorderStyle.None && game.Window.IsBorderless)
            throw new ArgumentException("Cannot set form border style when game window has been set to borderless!");
#endif

        if (gameForm != null)
            gameForm.FormBorderStyle = borderStyle;
    }

    //public bool HasFocus()
    //{
    //    return game.IsActive;
    //}

}
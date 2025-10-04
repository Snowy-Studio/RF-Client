using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Rampastring.XNAUI.XNAControls;

/// <summary>
/// A vertical scroll bar that can be utilized for various other controls.
/// </summary>
/// <remarks>
/// See also the sibling <see cref="XNAHorizontalScrollBar"/> class.
/// </remarks>
public class XNAScrollBar : XNAControl
{
    private const int MIN_BUTTON_HEIGHT = 10;

    /// <summary>
    /// Creates a new scroll bar.
    /// </summary>
    /// <param name="windowManager">The game window manager.</param>
    public XNAScrollBar(WindowManager windowManager) : base(windowManager)
    {
        ExclusiveInputCapture = true;

        var scrollUpTexture = AssetLoader.LoadTexture("sbUpArrow.png");

        btnScrollUp = new XNAButton(WindowManager);
        btnScrollUp.Name = nameof(btnScrollUp);
        btnScrollUp.ClientRectangle = new Rectangle(0, 0, scrollUpTexture.Width, scrollUpTexture.Height);
        btnScrollUp.IdleTexture = scrollUpTexture;
        if (AssetLoader.AssetExists("sbUpArrowHovered.png"))
            btnScrollUp.HoverTexture = AssetLoader.LoadTexture("sbUpArrowHovered.png");

        var scrollDownTexture = AssetLoader.LoadTexture("sbDownArrow.png");

        btnScrollDown = new XNAButton(WindowManager);
        btnScrollDown.Name = nameof(btnScrollDown);
        btnScrollDown.ClientRectangle = new Rectangle(0, Height - scrollDownTexture.Height,
            scrollDownTexture.Width, scrollDownTexture.Height);
        btnScrollDown.IdleTexture = scrollDownTexture;
        if (AssetLoader.AssetExists("sbDownArrowHovered.png"))
            btnScrollDown.HoverTexture = AssetLoader.LoadTexture("sbDownArrowHovered.png");

        ClientRectangleUpdated += XNAScrollBar_ClientRectangleUpdated;
    }

    private void XNAScrollBar_ClientRectangleUpdated(object sender, EventArgs e)
    {
        btnScrollDown.ClientRectangle = new Rectangle(0,
            Height - btnScrollDown.Height,
            btnScrollDown.Width, btnScrollDown.Height);
        Refresh();
    }

    /// <summary>
    /// Raised when the scroll bar is scrolled. 
    /// </summary>
    public event EventHandler Scrolled;

    /// <summary>
    /// Raised when the scroll bar is scrolled and it reaches its lowest value.
    /// </summary>
    public event EventHandler ScrolledToBottom;

    /// <summary>
    /// The height of the entire scrollable area.
    /// For example in a list box, the sum of the height of its items.
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// The number of pixels that the scrollable parent control
    /// is able to display at once.
    /// </summary>
    public int DisplayedPixelCount { get; set; }

    /// <summary>
    /// The scroll bar's current position.
    /// The parent of the scroll-bar has to keep the scrollbar up-to-date when the 
    /// view of the parent changes.
    /// </summary>
    public int ViewTop { get; set; }

    /// <summary>
    /// How many pixels to scroll at once.
    /// </summary>
    public int ScrollStep { get; set; } = 10;

    /// <summary>
    /// Returns the width of the scroll bar.
    /// </summary>
    public int ScrollWidth
    {
        get { return btnScrollUp.IdleTexture.Width; }
    }

    private int thumbHeight;

    private int scrollablePixels;

    private int buttonMinY = 0;

    private int buttonMaxY = 0;

    private int buttonY = 0;

    private XNAButton btnScrollUp;

    private XNAButton btnScrollDown;

    private Texture2D background;
    private Texture2D thumbMiddle;
    private Texture2D thumbTop;
    private Texture2D thumbBottom;

    private bool isHeldDown = false;

    public override void Initialize()
    {
        base.Initialize();

        AddChild(btnScrollUp);
        AddChild(btnScrollDown);

        btnScrollUp.LeftClick += BtnScrollUp_LeftClick;
        btnScrollDown.LeftClick += BtnScrollDown_LeftClick;

        background = AssetLoader.LoadTexture("sbBackground.png");
        thumbMiddle = AssetLoader.LoadTexture("sbMiddle.png");
        thumbTop = AssetLoader.LoadTexture("sbThumbTop.png");
        thumbBottom = AssetLoader.LoadTexture("sbThumbBottom.png");
    }

    public override void Kill()
    {
        // These textures are cached, don't allow the buttons to dispose them
        btnScrollDown.IdleTexture = null;
        btnScrollDown.HoverTexture = null;
        btnScrollUp.IdleTexture = null;
        btnScrollUp.HoverTexture = null;

        base.Kill();
    }

    /// <summary>
    /// Scrolls up when the user presses on the "scroll up" arrow.
    /// </summary>
    private void BtnScrollUp_LeftClick(object sender, EventArgs e)
    {
        if (ViewTop > 0)
        {
            ViewTop -= ScrollStep;
            if (ViewTop < 0)
                ViewTop = 0;
        }

        RefreshButtonY();

        Scrolled?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Scrolls down when the user presses on the "scroll down" arrow.
    /// </summary>
    private void BtnScrollDown_LeftClick(object sender, EventArgs e)
    {
        int nonDisplayedLines = Length - DisplayedPixelCount;

        if (ViewTop < nonDisplayedLines)
            ViewTop = Math.Min(ViewTop + ScrollStep, nonDisplayedLines);

        RefreshButtonY();

        Scrolled?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Returns a bool that tells whether there's enough items in a list for 
    /// the scrollbar to be drawn.
    /// </summary>
    public bool IsDrawn()
    {
        return scrollablePixels > 0;
    }

    /// <summary>
    /// Refreshes the scroll bar's thumb size.
    /// </summary>
    public void Refresh()
    {
        int height = Height -
            btnScrollUp.Height - btnScrollDown.Height;

        int nonDisplayedLines = Length - DisplayedPixelCount;

        if (nonDisplayedLines <= 0)
        {
            thumbHeight = height;
            scrollablePixels = 0;
            btnScrollDown.Disable();
            btnScrollUp.Disable();
        }
        else
        {
            thumbHeight = Math.Max(height - (int)(height * nonDisplayedLines / (double)Length),
                MIN_BUTTON_HEIGHT);

            scrollablePixels = height - thumbHeight;

            btnScrollDown.Enable();
            btnScrollUp.Enable();
        }

        buttonMinY = btnScrollUp.Bottom + thumbHeight / 2;
        buttonMaxY = Height - btnScrollDown.Height - (thumbHeight / 2);

        RefreshButtonY();
    }

    /// <summary>
    /// Scrolls the scrollbar when it's clicked on.
    /// </summary>
    public override void OnLeftClick(InputEventArgs inputEventArgs)
    {
        if (IsDrawn())
        {
            inputEventArgs.Handled = true;
            base.OnLeftClick(inputEventArgs);

            Scroll();
        }
    }

    /// <summary>
    /// Scrolls the scrollbar if the user presses the mouse left button
    /// while moving the cursor over the scrollbar.
    /// </summary>
    public override void OnMouseMove()
    {
        base.OnMouseMove();

        if (Cursor.LeftDown)
        {
            Scroll();
            isHeldDown = true;
            WindowManager.SelectedControl = this;
        }
    }

    private void Scroll()
    {
        if (!btnScrollDown.Enabled || !btnScrollUp.Enabled) return;


        var point = GetCursorPoint();

        if (point.Y < btnScrollUp.Height
            || point.Y > btnScrollDown.Y)
        {
            return;
        }


        if (point.Y <= buttonMinY || DisplayedPixelCount >= Length)
        {
            ViewTop = 0;
            RefreshButtonY();
            Scrolled?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (point.Y >= buttonMaxY)
        {
            

            ViewTop = Length - DisplayedPixelCount;
            RefreshButtonY();
            
            Scrolled?.Invoke(this, EventArgs.Empty);
            if (ScrolledToBottom == null) return;
            ScrolledToBottom?.Invoke(this, EventArgs.Empty);
            return;
        }

        double difference = buttonMaxY - buttonMinY;

        double location = point.Y - buttonMinY;

        int nonDisplayedLines = Length - DisplayedPixelCount;

        ViewTop = (int)(location / difference * nonDisplayedLines);
        RefreshButtonY();

        Scrolled?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Updates the top item index of the scroll bar,
    /// and the vertical position of the scroll bar's thumb.
    /// </summary>
    public void RefreshButtonY(int viewTop)
    {
        ViewTop = viewTop;
        RefreshButtonY();
    }

    /// <summary>
    /// Updates the vertical position of the scroll bar's thumb.
    /// </summary>
    public void RefreshButtonY()
    {
        int nonDisplayedLines = Length - DisplayedPixelCount;

        if (nonDisplayedLines <= 0)
        {
            buttonY = btnScrollUp.RenderRectangle().Bottom;
            return;
        }

        buttonY = Math.Min(
            buttonMinY + (int)(((ViewTop / (double)nonDisplayedLines) * scrollablePixels) - thumbHeight / 2),
            Height - btnScrollDown.Height - thumbHeight);
    }

    /// <summary>
    /// Updates the scroll bar's logic each frame.
    /// Makes it possible to drag the scrollbar thumb even if the cursor
    /// leaves the scroll bar's surface.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (isHeldDown)
        {
            if (!Cursor.LeftDown)
            {
                isHeldDown = false;
                WindowManager.SelectedControl = null;
            }
            else
            {
                Scroll();
            }
        }
    }

    /// <summary>
    /// Draws the scroll bar.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    public override void Draw(GameTime gameTime)
    {
        if (scrollablePixels > 0)
        {
            DrawTexture(background, new Rectangle(0, 0, Width, Height), Color.White);

            DrawTexture(thumbTop, new Rectangle(0, buttonY, ScrollWidth, thumbTop.Height), RemapColor);
            DrawTexture(thumbBottom, new Rectangle(0,
                buttonY + thumbHeight - thumbBottom.Height, ScrollWidth, thumbBottom.Height), Color.White);
            DrawTexture(thumbMiddle, new Rectangle(0,
                buttonY + thumbTop.Height, ScrollWidth, thumbHeight - thumbTop.Height - thumbBottom.Height), Color.White);
        }

        base.Draw(gameTime);
    }
}

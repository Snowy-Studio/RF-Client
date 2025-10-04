using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace Rampastring.XNAUI.XNAControls;

/// <summary>
/// A horizontal scroll bar that can be utilized for various other controls.
/// </summary>
/// /// <remarks>
/// See also the sibling <see cref="XNAScrollBar"/> class.
/// </remarks>
public class XNAHorizontalScrollBar : XNAControl
{
    private const int MIN_BUTTON_WIDTH = 10;

    /// <summary>
    /// Creates a new scroll bar.
    /// </summary>
    /// <param name="windowManager">The game window manager.</param>
    public XNAHorizontalScrollBar(WindowManager windowManager) : base(windowManager)
    {
        ExclusiveInputCapture = true;

        var scrollLeftTexture = AssetLoader.LoadTexture("hsbLeftArrow.png");

        btnScrollLeft = new XNAButton(WindowManager);
        btnScrollLeft.Name = nameof(btnScrollLeft);
        btnScrollLeft.ClientRectangle = new Rectangle(0, 0, scrollLeftTexture.Width, scrollLeftTexture.Height);
        btnScrollLeft.IdleTexture = scrollLeftTexture;
        if (AssetLoader.AssetExists("hsbLeftArrowHovered.png"))
            btnScrollLeft.HoverTexture = AssetLoader.LoadTexture("hsbLeftArrowHovered.png");

        var scrollRightTexture = AssetLoader.LoadTexture("hsbRightArrow.png");

        btnScrollRight = new XNAButton(WindowManager);
        btnScrollRight.Name = nameof(btnScrollRight);
        btnScrollRight.ClientRectangle = new Rectangle(Width - scrollRightTexture.Width, 0,
            scrollRightTexture.Width, scrollRightTexture.Height);
        btnScrollRight.IdleTexture = scrollRightTexture;
        if (AssetLoader.AssetExists("hsbRightArrowHovered.png"))
            btnScrollRight.HoverTexture = AssetLoader.LoadTexture("hsbRightArrowHovered.png");

        ClientRectangleUpdated += XNAScrollBar_ClientRectangleUpdated;
    }

    private void XNAScrollBar_ClientRectangleUpdated(object sender, EventArgs e)
    {
        btnScrollRight.ClientRectangle = new Rectangle(Width - btnScrollRight.Width, 0,
            btnScrollRight.Width, btnScrollRight.Height);
        Refresh();
    }

    /// <summary>
    /// Raised when the scroll bar is scrolled. 
    /// </summary>
    public event EventHandler Scrolled;

    /// <summary>
    /// Raised when the scroll bar is scrolled and it reaches its rightmost value.
    /// </summary>
    public event EventHandler ScrolledToRight;

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
    public int ViewLeft { get; set; }

    /// <summary>
    /// How many pixels to scroll at once.
    /// </summary>
    public int ScrollStep { get; set; } = 10;

    /// <summary>
    /// Returns the height of the scroll bar.
    /// </summary>
    public int ScrollHeight
    {
        get { return btnScrollLeft.IdleTexture.Height; }
    }

    private int thumbWidth;

    private int scrollablePixels;

    private int buttonMinX = 0;

    private int buttonMaxX = 0;

    private int buttonX = 0;

    private XNAButton btnScrollLeft;

    private XNAButton btnScrollRight;

    private Texture2D background;
    private Texture2D thumbMiddle;
    private Texture2D thumbLeft;
    private Texture2D thumbRight;

    private bool isHeldDown = false;

    public override void Initialize()
    {
        base.Initialize();

        AddChild(btnScrollLeft);
        AddChild(btnScrollRight);

        btnScrollLeft.LeftClick += BtnScrollLeft_LeftClick;
        btnScrollRight.LeftClick += BtnScrollRight_LeftClick;

        background = AssetLoader.LoadTexture("hsbBackground.png");
        thumbMiddle = AssetLoader.LoadTexture("hsbMiddle.png");
        thumbLeft = AssetLoader.LoadTexture("hsbThumbLeft.png");
        thumbRight = AssetLoader.LoadTexture("hsbThumbRight.png");
    }
    
    public override void Kill()
    {
        // These textures are cached, don't allow the buttons to dispose them
        btnScrollRight.IdleTexture = null;
        btnScrollRight.HoverTexture = null;
        btnScrollLeft.IdleTexture = null;
        btnScrollLeft.HoverTexture = null;

        base.Kill();
    }

    /// <summary>
    /// Scrolls up when the user presses on the "scroll left" arrow.
    /// </summary>
    private void BtnScrollLeft_LeftClick(object sender, EventArgs e)
    {
        if (ViewLeft > 0)
        {
            ViewLeft -= ScrollStep;
            if (ViewLeft < 0)
                ViewLeft = 0;
        }

        RefreshButtonX();

        Scrolled?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Scrolls down when the user presses on the "scroll down" arrow.
    /// </summary>
    private void BtnScrollRight_LeftClick(object sender, EventArgs e)
    {
        int nonDisplayedLines = Length - DisplayedPixelCount;

        if (ViewLeft < nonDisplayedLines)
            ViewLeft = Math.Min(ViewLeft + ScrollStep, nonDisplayedLines);

        RefreshButtonX();

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
        int width = Width -
            btnScrollLeft.Width - btnScrollRight.Width;

        int nonDisplayedColumns = Length - DisplayedPixelCount;

        if (nonDisplayedColumns <= 0)
        {
            thumbWidth = width;
            scrollablePixels = 0;
            btnScrollRight.Disable();
            btnScrollLeft.Disable();
        }
        else
        {
            thumbWidth = Math.Max(width - (int)(width * nonDisplayedColumns / (double)Length),
                MIN_BUTTON_WIDTH);

            scrollablePixels = width - thumbWidth;

            btnScrollRight.Enable();
            btnScrollLeft.Enable();
        }

        buttonMinX = btnScrollLeft.Right + thumbWidth / 2;
        buttonMaxX = Width - btnScrollRight.Width - (thumbWidth / 2);

        RefreshButtonX();
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
        var point = GetCursorPoint();

        if (point.X < btnScrollLeft.Width
            || point.X > btnScrollRight.X)
        {
            return;
        }
        
        if (point.X <= buttonMinX || DisplayedPixelCount >= Length)
        {
            ViewLeft = 0;
            RefreshButtonX();
            Scrolled?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (point.X >= buttonMaxX)
        {
            ViewLeft = Length - DisplayedPixelCount;
            RefreshButtonX();
            Scrolled?.Invoke(this, EventArgs.Empty);
            ScrolledToRight?.Invoke(this, EventArgs.Empty);
            return;
        }

        double difference = buttonMaxX - buttonMinX;

        double location = point.X - buttonMinX;

        int nonDisplayedLines = Length - DisplayedPixelCount;

        ViewLeft = (int)(location / difference * nonDisplayedLines);
        RefreshButtonX();

        Scrolled?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Updates the top item index of the scroll bar,
    /// and the horizontal position of the scroll bar's thumb.
    /// </summary>
    public void RefreshButtonX(int viewLeft)
    {
        ViewLeft = viewLeft;
        RefreshButtonX();
    }

    /// <summary>
    /// Updates the horizontal position of the scroll bar's thumb.
    /// </summary>
    public void RefreshButtonX()
    {
        int nonDisplayedColumns = Length - DisplayedPixelCount;

        if (nonDisplayedColumns <= 0)
        {
            buttonX = btnScrollLeft.RenderRectangle().Right;
            return;
        }

        buttonX = Math.Min(
            buttonMinX + (int)(((ViewLeft / (double)nonDisplayedColumns) * scrollablePixels) - thumbWidth / 2),
            Width - btnScrollRight.Width - thumbWidth);
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

            DrawTexture(thumbLeft, new Rectangle(buttonX, 0, thumbLeft.Width, ScrollHeight), RemapColor);
            DrawTexture(thumbRight, new Rectangle(buttonX + thumbWidth - thumbRight.Width,
                0, thumbRight.Width, ScrollHeight), Color.White);
            DrawTexture(thumbMiddle, new Rectangle(buttonX + thumbLeft.Width,
                0, thumbWidth - thumbLeft.Width - thumbRight.Width, ScrollHeight), Color.White);
        }

        base.Draw(gameTime);
    }
}
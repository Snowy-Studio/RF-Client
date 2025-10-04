using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Globalization;
using System.Linq;
using Rampastring.Tools;
using Rampastring.XNAUI.Extensions;
using static Rampastring.XNAUI.Extensions.MathExtensions;
using static Rampastring.XNAUI.Extensions.PointExtensions;
using static Rampastring.XNAUI.Extensions.RectangleExtensions;

namespace Rampastring.XNAUI.XNAControls;

/// <summary>
/// A panel that allows scrolling over arbitrarily placed child controls that overflow the size of the panel itself.
/// </summary>
public class XNAScrollPanel : XNAPanel
{
    protected const string CORNER_TEXTURE_FILENAME = "spCornerPanel.png";
    
    protected XNAHorizontalScrollBar HorizontalScrollBar;
    protected XNAScrollBar VerticalScrollBar;
    protected XNAPanel ContentPanel;
    protected XNAPanel CornerPanel;
    
    private TimeSpan _scrollKeyTime = TimeSpan.Zero;
    private TimeSpan _timeSinceLastScroll = TimeSpan.Zero;
    private bool _isScrollingQuickly = false;

    /// <summary>
    /// Raised when the scroll panel content is scrolled.
    /// </summary>
    public event EventHandler ViewPositionChanged;
    
    #region Properties
    
    #region Configurable properties
    
    /// <summary>
    /// Whether to allow scrolling with arrow keys.
    /// </summary>
    public bool AllowKeyboardInput { get; set; } = true;

    private (bool X, bool Y) _allowScroll = (true, true);
    
    /// <summary>
    /// Whether to allow scrolling on X or Y axes.
    /// </summary>
    public (bool X, bool Y) AllowScroll
    {
        get => _allowScroll;
        set
        {
            if (_allowScroll == value)
                return;
            
            _allowScroll = value;
            RecalculateScrollbars();
        }
    }

    /// <summary>
    /// How fast this control is scrolled?
    /// </summary>
    public int ScrollStep
    {
        get => HorizontalScrollBar.ScrollStep;
        set => HorizontalScrollBar.ScrollStep 
            = VerticalScrollBar.ScrollStep 
                = value;
    }
    
    private Point _overscrollMargin;

    /// <summary>
    /// How much can the content be "overscrolled", used for the purposes of inner panel size calculation.
    /// </summary>
    public Point OverscrollMargin
    {
        get => _overscrollMargin;
        set
        {
            value = new(Math.Max(0, value.X), Math.Max(0, value.Y));
            
            if (_overscrollMargin == value)
                return;
            
            ContentSize = ContentSize.Add(value).Subtract(_overscrollMargin);
            _overscrollMargin = value;
        }
    }
    
    private bool _drawBorders;

    public override bool DrawBorders
    {
        get => _drawBorders;
        set
        {
            if (_drawBorders == value)
                return;
            
            _drawBorders = value;
            RecalculateScrollbars();
        }
    }

    #endregion
    
    /// <summary>
    /// Size of the inner panel that contains all the content.
    /// </summary>
    public Point ContentSize
    {
        get => ContentPanel.ClientRectangle.GetSize();
        private set
        {
            ContentPanel.ClientRectangle = ContentPanel.ClientRectangle.WithSize(value);
            
            HorizontalScrollBar.Length = value.X;
            VerticalScrollBar.Length = value.Y;
            
            RecalculateScrollbars();
        }
    }
    
    /// <summary>
    /// The physical offset of the <see cref="ContentPanel"/>.
    /// </summary>
    protected Point CurrentContentPanelPosition
    {
        get => ContentPanel.ClientRectangle.Location;
        set
        {
            value = new Point
            {
                X = AllowScroll.X
                    ? Clamp(value.X, Math.Min(0, -(ContentSize.X - ViewSize.X)), 0)
                    : ContentPanel.X,
                Y = AllowScroll.Y
                    ? Clamp(value.Y, Math.Min(0, -(ContentSize.Y - ViewSize.Y)), 0)
                    : ContentPanel.Y,
            };

            if (value == ContentPanel.ClientRectangle.Location)
                return;

            ContentPanel.ClientRectangle = ContentPanel.ClientRectangle with { Location = value };
            ViewPositionChanged?.Invoke(this, EventArgs.Empty);
            VerticalScrollBar.RefreshButtonY(-value.Y);
            HorizontalScrollBar.RefreshButtonX(-value.X);
        }
    }
    
    /// <summary>
    /// The size of content that can be displayed by the control at once.
    /// </summary>
    public Point ViewSize => new()
        {
            X = VerticalScrollBar.Visible ? VerticalScrollBar.X : Width,
            Y = HorizontalScrollBar.Visible ? HorizontalScrollBar.Y : Height,
        };
    
    /// <summary>
    /// Location of the viewport over the <see cref="ContentPanel"/>.
    /// </summary>
    public Point CurrentViewPosition
    {
        get => new(-CurrentContentPanelPosition.X, -CurrentContentPanelPosition.Y);
        set => CurrentContentPanelPosition = new(-value.X, -value.Y);
    }

    /// <summary>
    /// The viewport area over the <see cref="ContentPanel"/>.
    /// </summary>
    public Rectangle CurrentViewRectangle => FromLocationAndSize(CurrentViewPosition, ViewSize);
    
    /// <summary>
    /// Indicates whether the control can be scrolled 
    /// </summary>
    public (bool X, bool Y) CanScroll => (
        AllowScroll.X && ViewSize.X < ContentSize.X,
        AllowScroll.Y && ViewSize.Y < ContentSize.Y);

    #endregion
    
    public XNAScrollPanel(WindowManager windowManager) : base(windowManager)
    {
        DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;  // so controls can be clipped
        
        HorizontalScrollBar = new XNAHorizontalScrollBar(WindowManager);
        VerticalScrollBar = new XNAScrollBar(WindowManager);
        
        ContentPanel = new XNAPanel(WindowManager)
        {
            DrawBorders = false,
        };
        CornerPanel = new XNAPanel(WindowManager)
        {
            DrawBorders = false,
            Alpha = 1.0f,
        };

        NameChanged += XNAScrollPanel_NameChanged;
        
        ClientRectangleUpdated += XNAScrollPanel_ClientRectangleUpdated;
    }

    public override void Initialize()
    {
        base.Initialize();
        
        ContentPanel.ChildAdded += ContentPanel_ChildAdded;
        ContentPanel.ChildRemoved += ContentPanel_ChildRemoved;
        
        HorizontalScrollBar.Scrolled += HorizontalScrollBar_Scrolled;
        HorizontalScrollBar.MouseScrolledHorizontally += HorizontalScrollBar_MouseScrolledHorizontally;
        // additional handler for users without horizontal scroll
        HorizontalScrollBar.MouseScrolled += HorizontalScrollBar_MouseScrolled;
        
        VerticalScrollBar.Scrolled += VerticalScrollBar_Scrolled;
        VerticalScrollBar.MouseScrolled += VerticalScrollBar_MouseScrolled;
        
        CornerPanel.BackgroundTexture = AssetLoader.AssetExists(CORNER_TEXTURE_FILENAME)
            ? AssetLoader.LoadTexture(CORNER_TEXTURE_FILENAME)
            : AssetLoader.CreateTexture(Color.Black, 2, 2);
        CornerPanel.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
        
        ComposeControls();
    }

    protected virtual void ComposeControls()
    {
        AddChild(ContentPanel);
        AddChild(HorizontalScrollBar);
        AddChild(VerticalScrollBar);
        AddChild(CornerPanel);
    }

    public override void Kill()
    {
        ContentPanel.ChildAdded -= ContentPanel_ChildAdded;
        ContentPanel.ChildRemoved -= ContentPanel_ChildRemoved;

        // this is needed because some of the children may be removed after ChildRemoved
        // handler was removed, thus the subscription won't be removed otherwise
        foreach (var child in ContentPanel.Children)
            child.ClientRectangleUpdated -= ChildControl_ClientRectangleUpdated;

        // don't allow to dispose the cached generic texture
        if (CornerPanel.BackgroundTexture.Name == CORNER_TEXTURE_FILENAME)
            CornerPanel.BackgroundTexture = null;

        base.Kill();
    }

    public override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "AllowKeyboardInput":
                AllowKeyboardInput = Conversions.BooleanFromString(value, true);
                return;
            case "AllowScroll":
                string[] arr = value.Split(',');
                AllowScroll = (
                    Conversions.BooleanFromString(arr[0], true),
                    Conversions.BooleanFromString(arr[1], true));
                return;
            case "AllowScrollX":
                AllowScroll = AllowScroll with { X = Conversions.BooleanFromString(value, true) };
                return;
            case "AllowScrollY":
                AllowScroll = AllowScroll with { Y = Conversions.BooleanFromString(value, true) };
                return;
            case "ScrollStep":
                ScrollStep = int.Parse(value);
                return;
            case "OverscrollMargin":
                string[] size = value.Split(',');
                OverscrollMargin = new(
                    int.Parse(size[0], CultureInfo.InvariantCulture),
                    int.Parse(size[1], CultureInfo.InvariantCulture));
                return;
            case "OverscrollMarginX":
                OverscrollMargin = OverscrollMargin with { X = int.Parse(value, CultureInfo.InvariantCulture) };
                return;
            case "OverscrollMarginY":
                OverscrollMargin = OverscrollMargin with { Y = int.Parse(value, CultureInfo.InvariantCulture) };
                return;
            case "DrawBorders":  // overtaking this one since behavior needs to be adjusted
                DrawBorders = Conversions.BooleanFromString(value, true);
                RecalculateScrollbars();
                return;
            case "Padding":  // padding is invalid for this control
                return;
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    #region Control behavior / Handlers
    
    private void XNAScrollPanel_NameChanged(object sender, EventArgs e)
    {
        HorizontalScrollBar.Name = $"{Name}_HorizontalScrollBar";
        VerticalScrollBar.Name = $"{Name}_VerticalScrollBar";
        ContentPanel.Name = $"{Name}_ContentPanel";
        CornerPanel.Name = $"{Name}_CornerPanel";
    }
    
    #region Recalculation handlers
    
    private void XNAScrollPanel_ClientRectangleUpdated(object sender, EventArgs e)
        => RecalculateScrollbars();

    private void ContentPanel_ChildAdded(object o, ControlEventArgs e)
    {
        if (e.Control.ClientRectangle != Rectangle.Empty)
            RecalculateContentSize();

        e.Control.ClientRectangleUpdated += ChildControl_ClientRectangleUpdated;
    }
    
    void ChildControl_ClientRectangleUpdated(object sender, EventArgs args)
        => RecalculateContentSize();

    private void ContentPanel_ChildRemoved(object o, ControlEventArgs e)
    {
        RecalculateContentSize();

        e.Control.ClientRectangleUpdated -= ChildControl_ClientRectangleUpdated;
    }

    #endregion

    #region Scroll handlers

    private void HorizontalScrollBar_Scrolled(object sender, EventArgs e)
        => CurrentViewPosition = CurrentViewPosition with { X = HorizontalScrollBar.ViewLeft };

    private void VerticalScrollBar_Scrolled(object sender, EventArgs e)
        => CurrentViewPosition = CurrentViewPosition with { Y = VerticalScrollBar.ViewTop };
    
    private void HorizontalScrollBar_MouseScrolledHorizontally(object sender, InputEventArgs inputEventArgs)
    {
        inputEventArgs.Handled = true;
        
        CurrentViewPosition = CurrentViewPosition with { X = CurrentViewPosition.X - Cursor.HorizontalScrollWheelValue * ScrollStep };
    }

    private void HorizontalScrollBar_MouseScrolled(object sender, InputEventArgs inputEventArgs)
    {
        inputEventArgs.Handled = true;
        
        CurrentViewPosition = CurrentViewPosition with { X = CurrentViewPosition.X - Cursor.ScrollWheelValue * ScrollStep };
    }

    private void VerticalScrollBar_MouseScrolled(object sender, InputEventArgs inputEventArgs)
    {
        inputEventArgs.Handled = true;
        
        CurrentViewPosition = CurrentViewPosition with { Y = CurrentViewPosition.Y - Cursor.ScrollWheelValue * ScrollStep };
    }

    public override void OnMouseScrolled(InputEventArgs inputEventArgs)
    {
        inputEventArgs.Handled = true;
        
        // scroll horizontally if no vertical scroll needed to ease the life of users without horizontal scroll
        // or if shift is held
        if (!CanScroll.Y || Keyboard.IsShiftHeldDown())
            CurrentViewPosition = CurrentViewPosition with { X = CurrentViewPosition.X - Cursor.ScrollWheelValue * ScrollStep };
        else
            CurrentViewPosition = CurrentViewPosition with { Y = CurrentViewPosition.Y - Cursor.ScrollWheelValue * ScrollStep };
        
        base.OnMouseScrolled(inputEventArgs);
    }
    
    public override void OnMouseScrolledHorizontally(InputEventArgs inputEventArgs)
    {
        inputEventArgs.Handled = true;
        
        CurrentViewPosition = CurrentViewPosition with { X = CurrentViewPosition.X - Cursor.HorizontalScrollWheelValue * ScrollStep };
        
        base.OnMouseScrolled(inputEventArgs);
    }

    #endregion
    
    public override void Update(GameTime gameTime)
    {
        if (IsActive && AllowKeyboardInput)
        {
            bool up = Keyboard.IsKeyHeldDown(Keys.Up);
            bool down = Keyboard.IsKeyHeldDown(Keys.Down);
            bool left = Keyboard.IsKeyHeldDown(Keys.Left);
            bool right = Keyboard.IsKeyHeldDown(Keys.Right);
            
            bool scrollingHorizontally = left != right;
            bool scrollingVertically = up != down;
            bool anyValidInput = scrollingHorizontally || scrollingVertically;
            
            if (scrollingVertically)
                HandleScrollKeyDown(gameTime, up ? ScrollUp : ScrollDown);
            
            if (scrollingHorizontally)
                HandleScrollKeyDown(gameTime, left ? ScrollLeft : ScrollRight);

            if (!anyValidInput)
            {
                _isScrollingQuickly = false;
                _timeSinceLastScroll = TimeSpan.Zero;
                _scrollKeyTime = TimeSpan.Zero;
            }
        }

        base.Update(gameTime);
    }

    protected virtual void HandleScrollKeyDown(GameTime gameTime, Action action)
    {
        if (_scrollKeyTime.Equals(TimeSpan.Zero))
            action();

        WindowManager.SelectedControl = this;

        _scrollKeyTime += gameTime.ElapsedGameTime;

        if (_isScrollingQuickly)
        {
            _timeSinceLastScroll += gameTime.ElapsedGameTime;

            if (_timeSinceLastScroll > TimeSpan.FromSeconds(XNAUIConstants.KEYBOARD_SCROLL_REPEAT_TIME))
            {
                _timeSinceLastScroll = TimeSpan.Zero;
                action();
            }
        }

        if (_scrollKeyTime > TimeSpan.FromSeconds(XNAUIConstants.KEYBOARD_FAST_SCROLL_TRIGGER_TIME) && !_isScrollingQuickly)
        {
            _isScrollingQuickly = true;
            _timeSinceLastScroll = TimeSpan.Zero;
        }
    }
    
    #endregion

    #region Recalculation methods
    
    /// <summary>
    /// Readjusts the inner panel size using coords of its children.
    /// </summary>
    protected virtual void RecalculateContentSize()
    {
        // TODO profile and perhaps optimize this via sorted array of max control sizes
        Point contentSize = ContentPanel.Children
            .Select(c => new Point(c.Right, c.Bottom))
            .Aggregate(Point.Zero, (accumulated, next) 
                => new Point(Math.Max(accumulated.X, next.X), Math.Max(accumulated.Y, next.Y)));

        ContentSize = contentSize.Add(OverscrollMargin);
    }

    /// <summary>
    /// Readjusts the scrollbar sizes, max values etc.
    /// </summary>
    protected virtual void RecalculateScrollbars()
    {
        if (Width == 0 || Height == 0)
            return;
        
        int border = DrawBorders ? 1 : 0;
        int border2x = border * 2;

        #region Visibility calculations

        Point viewSizeNoScrollbars = ClientRectangle.GetSize();
        Point viewSizeWithScrollbars = new()
        {
            X = Width - VerticalScrollBar.ScrollWidth - border,
            Y = Height - HorizontalScrollBar.ScrollHeight - border,
        };

        // soft means only overflows if the scrollbar-bound area is overflown
        (bool X, bool Y) isOverflowingSoft = new()
        {
            X = ContentSize.X > viewSizeWithScrollbars.X,
            Y = ContentSize.Y > viewSizeWithScrollbars.Y,
        };
        
        // hard means whole control area (when scrollbars are hidden) is overflown
        (bool X, bool Y) isOverflowingHard = new()
        {
            X = ContentSize.X > viewSizeNoScrollbars.X,
            Y = ContentSize.Y > viewSizeNoScrollbars.Y,
        };

        // X means the need for vertical scrollbar, Y - horizontal
        // this complexity here handles the cases when addition of
        // a scrollbar makes another coord overflow
        (bool X, bool Y) scrollbarVisible = new()
        {
            X = AllowScroll.X && (isOverflowingHard.X || isOverflowingSoft.X && isOverflowingHard.Y),
            Y = AllowScroll.Y && (isOverflowingHard.Y || isOverflowingSoft.Y && isOverflowingHard.X),
        };
        
        (HorizontalScrollBar.Visible, VerticalScrollBar.Visible) = scrollbarVisible;
        CornerPanel.Visible = scrollbarVisible.X && scrollbarVisible.Y;

        #endregion
        
        HorizontalScrollBar.ClientRectangle = new()
        {
            X = border,
            Y = Height - HorizontalScrollBar.ScrollHeight - border,
            Width = Width - border2x - (scrollbarVisible.Y ? VerticalScrollBar.ScrollWidth : 0),
            Height = HorizontalScrollBar.ScrollHeight,
        };

        VerticalScrollBar.ClientRectangle = new()
        {
            X = Width - VerticalScrollBar.ScrollWidth - border,
            Y = border,
            Width = VerticalScrollBar.ScrollWidth,
            Height = Height - border2x - (scrollbarVisible.X ? HorizontalScrollBar.ScrollHeight : 0),
        };

        // keep in mind that CurrentViewSize call here relies on correct placement of scrollbars
        CornerPanel.ClientRectangle = FromLocationAndSize(ViewSize,
            ClientRectangle.GetSize().Subtract(ViewSize).Subtract(FromInt(border))
        );
        
        HorizontalScrollBar.DisplayedPixelCount = ViewSize.X;
        VerticalScrollBar.DisplayedPixelCount = ViewSize.Y;
        
        HorizontalScrollBar.Refresh();
        VerticalScrollBar.Refresh();
    }
    
    #endregion

    #region Scroll methods

    /// <summary>
    /// Scrolls to the specified rectangle.
    /// </summary>
    /// <remarks>
    /// If the rectangle is bigger than the viewport - scrolls to its top/left bounds.
    /// </remarks>
    /// <param name="rect">The rectangle (in local coordinates) to scroll to.</param>
    public virtual void ScrollTo(Rectangle rect)
    {
        // Currently the calculations below make the viewport shift the minimal amount needed to
        // fully show the rectangle we're scrolling to.
        
        // Math.Min calls inside the calculation are responsible for handling controls bigger
        // than the viewport (basically makes the viewport snap to top left corner, or top/left
        // sides separately if it overflows on only one side).
        
        // As an alternative implementation you might want to have the viewport scroll to
        // the closest part of the control. To do that - remove the aforementioned Math.Min
        // calls and simply flip min and max values (or flip places of CurrentViewRectangle
        // and rect in the calculation) on the axes where the rect is bigger than CurrentViewRectangle.
        
        // Another implementation to consider here is that when the rectangle to scroll to could be centered
        // within the viewport. This is a more niche implementation though, and it could be argued that
        // it would be better if the descendants of this control implement it instead for their specific
        // use cases.
        
        CurrentViewPosition = new()
        {
            X = Clamp(value: CurrentViewRectangle.X,
                min: Math.Min(rect.X + rect.Width - CurrentViewRectangle.Width, rect.X),
                max: rect.X),
            Y = Clamp(value: CurrentViewRectangle.Y,
                min: Math.Min(rect.Y + rect.Height - CurrentViewRectangle.Height, rect.Y),
                max: rect.Y),
        };
    }

    /// <summary>
    /// Scrolls to the specified point.
    /// </summary>
    /// <param name="point">The point (in local coordinates) to scroll to.</param>
    public void ScrollTo(Point point) => ScrollTo(rect: FromLocationAndSize(point, Point.Zero));
    
    /// <summary>
    /// Scrolls to the specified child control.
    /// </summary>
    /// <param name="control">The child control of <see cref="ContentPanel"/> to scroll to.</param>
    public void ScrollToChildControl(XNAControl control) => ScrollTo(control.ClientRectangle);
    
    private void ScrollUp() => CurrentViewPosition = CurrentViewPosition with { Y = CurrentViewPosition.Y - ScrollStep };

    private void ScrollDown() => CurrentViewPosition = CurrentViewPosition with { Y = CurrentViewPosition.Y + ScrollStep };

    private void ScrollLeft() => CurrentViewPosition = CurrentViewPosition with { X = CurrentViewPosition.X - ScrollStep };

    private void ScrollRight() => CurrentViewPosition = CurrentViewPosition with { X = CurrentViewPosition.X + ScrollStep };
    
    public void ScrollToTop() => CurrentViewPosition = CurrentViewPosition with { Y = 0 };

    public void ScrollToBottom() => CurrentViewPosition = CurrentViewPosition with { Y = ContentSize.Y - ViewSize.Y };

    public void ScrollToLeft() => CurrentViewPosition = CurrentViewPosition with { X = 0 };

    public void ScrollToRight() => CurrentViewPosition = CurrentViewPosition with { X = ContentSize.X - ViewSize.X };

    public void ScrollToBegin()
    {
        ScrollToTop();
        ScrollToLeft();
    }
    
    public void ScrollToEnd()
    {
        ScrollToBottom();
        ScrollToRight();
    }
    
    #endregion
}

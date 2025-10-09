using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Rampastring.XNAUI.XNAControls;

public class ContextMenuItemSelectedEventArgs : EventArgs
{
    public ContextMenuItemSelectedEventArgs(int itemIndex)
    {
        ItemIndex = itemIndex;
    }

    public int ItemIndex { get; }
}

/// <summary>
/// A context menu item.
/// </summary>
public class XNAContextMenuItem
{
    /// <summary>
    /// The text of the context menu item.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// A function for generating the hint text of the context menu item.
    /// Drawn in the end of the item.
    /// </summary>
    public Func<string> HintTextGenerator { get; set; }

    /// <summary>
    /// Cached hint text of the context menu item.
    /// </summary>
    internal string HintText { get; set; }

    /// <summary>
    /// Determines whether the context menu item is enabled
    /// (can be clicked on).
    /// </summary>
    public bool Selectable { get; set; } = true;

    /// <summary>
    /// Determines whether the context menu item is visible.
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// The height of the context menu item.
    /// If null, the common item height is used.
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// The font index of the context menu item.
    /// If null, the common font index is used.
    /// </summary>
    public int? FontIndex { get; set; }

    /// <summary>
    /// The texture of the context menu item.
    /// </summary>
    public Texture2D Texture { get; set; }

    /// <summary>
    /// The background color of the context menu item.
    /// If null, the common background color is used.
    /// </summary>
    public Color? BackgroundColor { get; set; }

    /// <summary>
    /// The color of the context menu item's text.
    /// If null, the common text color is used.
    /// </summary>
    public Color? TextColor { get; set; }

    /// <summary>
    /// The method that is called when the item is selected.
    /// </summary>
    public Action SelectAction { get; set; }

    /// <summary>
    /// When the context menu is shown, this function is called
    /// to determine whether this item should be selectable. 
    /// If null, the value of the Enabled property is not changed.
    /// </summary>
    public Func<bool> SelectableChecker { get; set; }

    /// <summary>
    /// When the context menu is shown, this function is called
    /// to determine whether this item should be visible.
    /// If null, the value of the Visible property is not changed.
    /// </summary>
    public Func<bool> VisibilityChecker { get; set; }

    /// <summary>
    /// The Y position of the item's text.
    /// </summary>
    public float TextY { get; set; }
}

/// <summary>
/// A context menu.
/// </summary>
public class XNAContextMenu : XNAControl
{
    protected const int BORDER_WIDTH = 1;
    protected const int TEXTURE_PADDING = 1;

    /// <summary>
    /// Creates a new context menu.
    /// </summary>
    /// <param name="windowManager">The WindowManager associated with this context menu.</param>
    public XNAContextMenu(WindowManager windowManager) : base(windowManager)
    {
        Height = BORDER_WIDTH * 2;
        DisabledItemColor = Color.Gray;
        Disable();
    }

    public event EventHandler<ContextMenuItemSelectedEventArgs> OptionSelected;

    public int ItemHeight { get; set; } = 17;

    public List<XNAContextMenuItem> Items = new List<XNAContextMenuItem>();

    private Color? _borderColor;

    public Color BorderColor
    {
        get => _borderColor ?? UISettings.ActiveSettings.PanelBorderColor;
        set => _borderColor = value;
    }

    private Color? _focusColor;

    public Color FocusColor
    {
        get => _focusColor ?? UISettings.ActiveSettings.FocusColor;
        set => _focusColor = value;
    }

    private Color? _backColor;

    public Color BackColor
    {
        get => _backColor ?? UISettings.ActiveSettings.BackgroundColor;
        set => _backColor = value;
    }

    private Color? _itemColor;

    public Color ItemColor
    {
        get => _itemColor ?? UISettings.ActiveSettings.AltColor;
        set => _itemColor = value;
    }

    private Color? _disabledItemColor;

    public Color DisabledItemColor
    {
        get => _disabledItemColor ?? UISettings.ActiveSettings.DisabledItemColor;
        set => _disabledItemColor = value;
    }

    public int FontIndex { get; set; }
    public int HintFontIndex { get; set; }

    public int TextHorizontalPadding { get; set; } = 1;
    public int TextVerticalPadding { get; set; } = 1;

    /// <summary>
    /// The index of the context menu item that 
    /// the user's cursor is hovering on. -1 for none.
    /// </summary>
    public int HoveredIndex { get; private set; } = -1;

    private bool leftClickHandled = false;
    private bool openedOnThisFrame = false;

    #region AddItem methods

    /// <summary>
    /// Adds an item into the context menu.
    /// </summary>
    /// <param name="item">The item.</param>
    public void AddItem(XNAContextMenuItem item)
    {
        Items.Add(item);
    }

    /// <summary>
    /// Generates and adds an item with the specified text into the context menu.
    /// </summary>
    /// <param name="text">The text of the item.</param>
    public void AddItem(string text)
    {
        var item = new XNAContextMenuItem();
        item.Text = text;

        AddItem(item);
    }

    public void AddItem(string text, Action selectAction, Func<bool> selectableChecker = null, Func<bool> visibilityChecker = null, Texture2D texture = null, Func<string> hintTextGenerator = null)
    {
        var item = new XNAContextMenuItem()
        {
            Text = text,
            SelectAction = selectAction,
            SelectableChecker = selectableChecker,
            VisibilityChecker = visibilityChecker,
            Texture = texture,
            HintTextGenerator = hintTextGenerator
        };

        AddItem(item);

        if (Enabled)
        {
            Height += GetItemHeight(item);
        }
    }

    #endregion

    public void Open(Point point)
    {
        X = point.X;
        Y = point.Y;

        int height = BORDER_WIDTH * 2;

        for (int i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            if (item.VisibilityChecker != null)
                item.Visible = item.VisibilityChecker();

            if (item.Visible)
            {
                int itemHeight = GetItemHeight(item);
                height += itemHeight;
                item.TextY = (itemHeight - Renderer.GetTextDimensions(item.Text, GetItemFontIndex(item)).Y) / 2;
            }

            if (item.SelectableChecker != null)
                item.Selectable = item.SelectableChecker();

            if (item.HintTextGenerator != null)
            {
                item.HintText = item.HintTextGenerator();
            }
            else
            {
                item.HintText = null;
            }
        }

        Height = height;

        Enable();

        if (!Detached)
            Detach();

        Point windowPoint = GetWindowPoint();
        if (windowPoint.X + Width > WindowManager.RenderResolutionX)
            X -= Width;

        if (windowPoint.Y + Height > WindowManager.RenderResolutionY)
        {
            int screenTopPointY = point.Y - windowPoint.Y;

            if (Height > WindowManager.RenderResolutionY)
            {
                // The context menu is too big for the screen.
                // Open it so that the last item is visible at the bottom of the screen.
                Y = WindowManager.RenderResolutionY + screenTopPointY - Height;
            }
            else
            {
                if (Y - Height < screenTopPointY)
                {
                    // We cannot open up fully above our location, but not also fully below.
                    // Stick to the bottom of the screen so that the entire context menu is visible.
                    Y = screenTopPointY + (WindowManager.RenderResolutionY - Height);
                }
                else
                {
                    // We have enough space to fully open above.
                    Y -= Height;
                }
            }
        }

        openedOnThisFrame = true;
    }

    public void ClearItems()
    {
        Items.Clear();
        Height = 2;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (InputEnabled)
        {
            // Hide the drop-down if the left mouse button is clicked while the
            // cursor isn't on this control
            if (Cursor.LeftClicked && !leftClickHandled && !openedOnThisFrame)
                OnLeftClick();

            leftClickHandled = false;
            openedOnThisFrame = false;

            // Update hovered index

            int itemIndexOnCursor = GetItemIndexOnCursor();

            if (itemIndexOnCursor > -1 && Items[itemIndexOnCursor].Selectable)
                HoveredIndex = itemIndexOnCursor;
            else
                HoveredIndex = -1;
        }
    }

    public override void OnLeftClick()
    {
        base.OnLeftClick();

        leftClickHandled = true;

        int itemIndexOnCursor = GetItemIndexOnCursor();

        if (itemIndexOnCursor > -1)
        {
            if (Items[itemIndexOnCursor].Selectable)
            {
                Items[itemIndexOnCursor].SelectAction?.Invoke();
                OptionSelected?.Invoke(this, new ContextMenuItemSelectedEventArgs(itemIndexOnCursor));

                if (Detached)
                    Attach();
                Disable();
            }

            return;
        }

        Attach();
        Disable();
    }

    /// <summary>
    /// Returns the index of the item that the cursor currently points to.
    /// </summary>
    private int GetItemIndexOnCursor()
    {
        Point p = GetCursorPoint();

        Rectangle rect = GetWindowRectangle();

        if (p.X < 0 || p.X > Width ||
            p.Y > Height ||
            p.Y < 0)
        {
            return -1;
        }

        int y = p.Y;
        int currentHeight = BORDER_WIDTH;

        for (int i = 0; i < Items.Count; i++)
        {
            var item = Items[i];

            if (!item.Visible)
                continue;

            int itemHeight = GetItemHeight(item);
            if (y >= currentHeight && y <= currentHeight + itemHeight)
                return i;

            currentHeight += itemHeight;
        }

        return -1;
    }

    /// <summary>
    /// Gets the height of a context menu item.
    /// </summary>
    /// <param name="item">The item.</param>
    protected int GetItemHeight(XNAContextMenuItem item) =>
         item.Height ?? ItemHeight;

    /// <summary>
    /// Gets the index of an item's font.
    /// </summary>
    /// <param name="item">The item.</param>
    protected int GetItemFontIndex(XNAContextMenuItem item) =>
        item.FontIndex ?? FontIndex;

    protected Color GetItemTextColor(XNAContextMenuItem item) =>
        item.TextColor ?? ItemColor;

    public override void Draw(GameTime gameTime)
    {
        //Renderer.FillRectangle(new Rectangle(wr.X + 1, wr.Y + 1, wr.Width - 2, wr.Height - 2), BackColor);
        DrawRectangle(new Rectangle(0, 0, Width, Height), BorderColor);

        int y = BORDER_WIDTH;

        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i].Visible)
                y += DrawItem(i, new Point(BORDER_WIDTH, y));
        }

        base.Draw(gameTime);
    }

    /// <summary>
    /// Draws a single context menu item.
    /// Returns the height of the item.
    /// This can be overriden in derived classes to customize the drawing code.
    /// </summary>
    /// <param name="index">The index of the item to draw.</param>
    /// <param name="point">The point (relative to the control) where to draw the item.</param>
    /// <returns>The height of the item that was drawn.</returns>
    protected virtual int DrawItem(int index, Point point)
    {
        XNAContextMenuItem item = Items[index];

        int itemHeight = GetItemHeight(item);

        if (HoveredIndex == index)
        {
            FillRectangle(new Rectangle(point.X, point.Y, Width - BORDER_WIDTH * 2, itemHeight), FocusColor);
        }
        else
        {
            FillRectangle(new Rectangle(point.X, point.Y, Width - BORDER_WIDTH * 2, itemHeight), BackColor);
        }

        int textX = point.X + TextHorizontalPadding;
        if (item.Texture != null)
        {
            Renderer.DrawTexture(item.Texture, new Rectangle(point.X + TEXTURE_PADDING, point.Y + TEXTURE_PADDING,
                item.Texture.Width, item.Texture.Height), Color.White);
            textX += item.Texture.Width + TEXTURE_PADDING * 2;
        }

        Color textColor = item.Selectable ? GetItemTextColor(item) : DisabledItemColor;

        DrawStringWithShadow(item.Text, FontIndex, new Vector2(textX, point.Y + TextVerticalPadding), textColor);
        if (item.HintText != null)
        {
            int hintTextX = Width - TextHorizontalPadding - (int)Renderer.GetTextDimensions(item.HintText, HintFontIndex).X;
            DrawStringWithShadow(item.HintText, HintFontIndex, new Vector2(hintTextX, point.Y + TextVerticalPadding), textColor);
        }

        return itemHeight;
    }
}

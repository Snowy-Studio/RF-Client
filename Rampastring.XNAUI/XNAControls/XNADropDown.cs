using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;

namespace Rampastring.XNAUI.XNAControls;

public enum DropDownState
{
    CLOSED,
    OPENED_DOWN,
    OPENED_UP
}

/// <summary>
/// A drop-down control.
/// </summary>
public class XNADropDown : XNAControl
{
    /// <summary>
    /// Creates a new drop-down control.
    /// </summary>
    /// <param name="windowManager">The WindowManager associated with this control.</param>
    public XNADropDown(WindowManager windowManager) : base(windowManager)
    {
        Height = ItemHeight + 2;
    }

    public delegate void SelectedIndexChangedEventHandler(object sender, EventArgs e);
    public event SelectedIndexChangedEventHandler SelectedIndexChanged;

    /// <summary>
    /// Raised when the user re-selects an already selected drop-down item.
    /// </summary>
    public event EventHandler IndexReselected;

    /// <summary>
    /// The height of drop-down items.
    /// </summary>
    public int ItemHeight { get; set; } = 17;

    public List<XNADropDownItem> Items = new List<XNADropDownItem>();

    /// <summary>
    /// Gets or sets the dropped-down status of the drop-down control.
    /// </summary>
    public DropDownState DropDownState { get; private set; }

    private bool _allowDropDown = true;

    /// <summary>
    /// Controls whether the drop-down control can be dropped down.
    /// </summary>
    public bool AllowDropDown
    {
        get { return _allowDropDown; }
        set
        {
            _allowDropDown = value;
            if (!_allowDropDown && DropDownState != DropDownState.CLOSED)
            {
                CloseDropDown();
            }
        }
    }

    private int _selectedIndex = -1;

    /// <summary>
    /// Gets or sets the selected index of the drop-down control.
    /// </summary>
    public int SelectedIndex
    {
        get { return _selectedIndex; }
        set
        {
            int oldSelectedIndex = _selectedIndex;

            _selectedIndex = value;

            if (value != oldSelectedIndex)
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            else
                IndexReselected?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gets the currently selected item of the drop-down control.
    /// </summary>
    public XNADropDownItem SelectedItem
    {
        get
        {
            if (SelectedIndex < 0 || SelectedIndex >= Items.Count)
                return null;

            return Items[SelectedIndex];
        }
    }

    public int FontIndex { get; set; }

    private Color? _borderColor;

    public Color BorderColor
    {
        get
        {
            return _borderColor ?? UISettings.ActiveSettings.PanelBorderColor;
        }
        set { _borderColor = value; }
    }

    private Color? _focusColor;

    public Color FocusColor
    {
        get
        {
            return _focusColor ?? UISettings.ActiveSettings.FocusColor;
        }
        set { _focusColor = value; }
    }

    private Color? _backColor;

    public Color BackColor
    {
        get
        {
            return _backColor ?? UISettings.ActiveSettings.BackgroundColor;
        }
        set { _backColor = value; }
    }

    private Color? _textColor;

    public Color TextColor
    {
        get
        {
            return _textColor ?? UISettings.ActiveSettings.AltColor;
        }
        set { _textColor = value; }
    }

    private Color? _disabledItemColor;

    public Color DisabledItemColor
    {
        get
        {
            return _disabledItemColor ?? UISettings.ActiveSettings.DisabledItemColor;
        }
        set { _disabledItemColor = value; }
    }

    /// <summary>
    /// If set, the drop-down is opened upwards rather than downwards.
    /// </summary>
    public bool OpenUp { get; set; }

    public Texture2D DropDownTexture { get; set; }
    public Texture2D DropDownOpenTexture { get; set; }

    public EnhancedSoundEffect ClickSoundEffect { get; set; }

    private int hoveredIndex = 0;
    private bool clickedAfterOpen = false;

    #region AddItem methods

    /// <summary>
    /// Adds an item into the drop-down.
    /// </summary>
    /// <param name="item">The item.</param>
    public XNADropDown AddItem(XNADropDownItem item)
    {
        Items.Add(item);
        return this;
    }

    /// <summary>
    /// Generates and adds an item with the specified text into the drop-down.
    /// </summary>
    /// <param name="text">The text of the item.</param>
    public void AddItem(string text)
    {
        var item = new XNADropDownItem();
        item.Text = text;

        Items.Add(item);

    }

    public void AddItem(string text,object tag)
    {
        var item = new XNADropDownItem();
        item.Text = text;
        item.Tag = tag;

        Items.Add(item);

    }

    public XNADropDown AddItem(string[] texts)
    {
        foreach (var item in texts)
        {
            AddItem(item);
        }
        return this;
    }

    /// <summary>
    /// Generates and adds an item with the specified text and texture
    /// into the drop-down.
    /// </summary>
    /// <param name="text">The text of the item.</param>
    /// <param name="texture">The item's texture.</param>
    public XNADropDown AddItem(string text, Texture2D texture)
    {
        var item = new XNADropDownItem();
        item.Text = text;
        item.Texture = texture;

        Items.Add(item);
        return this;
    }

    /// <summary>
    /// Generates and adds an item with the specified text
    /// and text color into the drop-down control.
    /// </summary>
    /// <param name="text">The text of the item.</param>
    /// <param name="color">The color of the item's text.</param>
    public XNADropDown AddItem(string text, Color color)
    {
        var item = new XNADropDownItem();
        item.Text = text;
        item.TextColor = color;

        Items.Add(item);
        return this;
    }

    #endregion

    public override void Initialize()
    {
        base.Initialize();

        DropDownTexture = AssetLoader.LoadTexture("comboBoxArrow.png");
        DropDownOpenTexture = AssetLoader.LoadTexture("openedComboBoxArrow.png");

        Height = DropDownTexture.Height;
    }

    public override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "OpenUp":
                OpenUp = Conversions.BooleanFromString(value, OpenUp);
                return;
            case "DropDownTexture":
                DropDownTexture = AssetLoader.LoadTextureUncached(value);
                return;
            case "DropDownOpenTexture":
                DropDownOpenTexture = AssetLoader.LoadTextureUncached(value);
                return;
            case "ItemHeight":
                ItemHeight = Conversions.IntFromString(value, ItemHeight);
                return;
            case "ClickSoundEffect":
                ClickSoundEffect = new EnhancedSoundEffect(value);
                return;
            case "FontIndex":
                FontIndex = Conversions.IntFromString(value, FontIndex);
                return;
            case "BorderColor":
                BorderColor = AssetLoader.GetRGBAColorFromString(value);
                return;
            case "FocusColor":
                FocusColor = AssetLoader.GetRGBAColorFromString(value);
                return;
            case "BackColor":
                BackColor = AssetLoader.GetRGBAColorFromString(value);
                return;
            case "DisabledItemColor":
                DisabledItemColor = AssetLoader.GetColorFromString(value);
                return;
        }

        if (key.StartsWith("Option", StringComparison.InvariantCulture))
        {
            AddItem(value);
            return;
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    /// <summary>
    /// Gets the text color of a drop-down item.
    /// </summary>
    /// <param name="item">The item.</param>
    protected Color GetItemTextColor(XNADropDownItem item) =>
        item.TextColor ?? TextColor;

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (DropDownState != DropDownState.CLOSED)
        {
            if (!IsActive && Cursor.LeftPressedDown)
            {
                hoveredIndex = -1;
                CloseDropDown();
                return;
            }

            // Update hovered index

            int itemIndexOnCursor = GetItemIndexOnCursor();

            if (itemIndexOnCursor > -1 && Items[itemIndexOnCursor].Selectable)
                hoveredIndex = itemIndexOnCursor;
            else
                hoveredIndex = -1;
        }
    }

    public override void OnMouseLeftDown()
    {
        base.OnMouseLeftDown();

        if (!AllowDropDown)
            return;

        if (DropDownState != DropDownState.CLOSED)
            return;

        ClickSoundEffect?.Play();

        clickedAfterOpen = false;

        if (!OpenUp)
        {
            DropDownState = DropDownState.OPENED_DOWN;
            Height = DropDownTexture.Height + 2 + ItemHeight * Items.Count;
        }
        else
        {
            DropDownState = DropDownState.OPENED_UP;
            Y -= 1 + ItemHeight * Items.Count;
            Height = DropDownTexture.Height + 1 + ItemHeight * Items.Count;
        }

        Detach();
        hoveredIndex = -1;
    }

    public override void OnLeftClick()
    {
        base.OnLeftClick();

        if (DropDownState == DropDownState.CLOSED)
        {
            return;
        }

        int itemIndexOnCursor = GetItemIndexOnCursor();

        if (itemIndexOnCursor == -1 && !clickedAfterOpen)
        {
            clickedAfterOpen = true;
            return;
        }

        if (itemIndexOnCursor > -1)
        {
            if (Items[itemIndexOnCursor].Selectable)
                SelectedIndex = itemIndexOnCursor;
            else
                return;
        }

        ClickSoundEffect?.Play();

        CloseDropDown();
    }

    protected virtual void CloseDropDown()
    {
        if (DropDownState == DropDownState.OPENED_UP)
        {
            Y = Bottom - DropDownTexture.Height;
        }

        Height = DropDownTexture.Height;
        DropDownState = DropDownState.CLOSED;
        Attach();
    }

    public override void OnMouseScrolled()
    {
        if (!AllowDropDown)
            return;

        if (Cursor.ScrollWheelValue < 0)
        {
            if (SelectedIndex >= Items.Count - 1)
                return;

            if (Items[SelectedIndex + 1].Selectable)
                SelectedIndex++;
        }

        if (Cursor.ScrollWheelValue > 0)
        {
            if (SelectedIndex < 1 || SelectedIndex > Items.Count - 1)
                return;

            if (Items[SelectedIndex - 1].Selectable)
                SelectedIndex--;
        }

        base.OnMouseScrolled();
    }

    /// <summary>
    /// Returns the index of the item that the cursor currently points to.
    /// </summary>
    private int GetItemIndexOnCursor()
    {
        Point p = GetCursorPoint();

        if (p.X < 0 || p.X > Width ||
            p.Y > Height ||
            p.Y < 0)
        {
            return -2;
        }

        int itemIndex;

        if (DropDownState == DropDownState.OPENED_DOWN)
        {
            if (p.Y < DropDownTexture.Height + 1)
                return -1;

            int y = p.Y - DropDownTexture.Height - 1;
            itemIndex = y / ItemHeight;
        }
        else // if (DropDownState == DropDownState.DROPPED_UP)
        {
            if (p.Y > ClientRectangle.Height - DropDownTexture.Height - 1)
                return -1;

            itemIndex = (p.Y - 1) / ItemHeight;
        }

        if (itemIndex < Items.Count && itemIndex > -1)
        {
            return itemIndex;
        }

        return -1;
    }

    /// <summary>
    /// Draws the drop-down.
    /// </summary>
    public override void Draw(GameTime gameTime)
    {
        Rectangle dropDownRect;
        if (DropDownState == DropDownState.CLOSED)
            dropDownRect = new Rectangle(0, 0, Width, Height);
        else if (DropDownState == DropDownState.OPENED_DOWN)
            dropDownRect = new Rectangle(0, 0, Width, DropDownTexture.Height);
        else
            dropDownRect = new Rectangle(0, Height - DropDownTexture.Height, Width, DropDownTexture.Height);

        FillRectangle(new Rectangle(dropDownRect.X + 1, dropDownRect.Y + 1,
            dropDownRect.Width - 2, dropDownRect.Height - 2), BackColor);
        DrawRectangle(dropDownRect, BorderColor);

        if (SelectedIndex > -1 && SelectedIndex < Items.Count)
        {
            XNADropDownItem item = Items[SelectedIndex];

            int textX = 3;
            if (item.Texture != null)
            {
                DrawTexture(item.Texture,
                    new Rectangle(1, dropDownRect.Y + 2,
                    item.Texture.Width, item.Texture.Height), Color.White);
                textX += item.Texture.Width + 1;
            }

            if (item.Text != null)
            {
                DrawStringWithShadow(item.Text, FontIndex,
                    new Vector2(textX, dropDownRect.Y + 2), GetItemTextColor(item));
            }
        }

        if (AllowDropDown)
        {
            var ddRectangle = new Rectangle(Width - DropDownTexture.Width,
                dropDownRect.Y, DropDownTexture.Width, DropDownTexture.Height);

            if (DropDownState != DropDownState.CLOSED)
            {
                DrawTexture(DropDownOpenTexture,
                    ddRectangle, RemapColor);

                Rectangle listRectangle;

                if (DropDownState == DropDownState.OPENED_DOWN)
                    listRectangle = new Rectangle(0, DropDownTexture.Height, Width, Height - DropDownTexture.Height);
                else
                    listRectangle = new Rectangle(0, 0, Width, Height - DropDownTexture.Height);

                DrawRectangle(listRectangle, BorderColor);

                for (int i = 0; i < Items.Count; i++)
                {
                    int y = listRectangle.Y + 1 + i * ItemHeight;
                    DrawItem(i, y);
                }
            }
            else
            {
                DrawTexture(DropDownTexture, ddRectangle, RemapColor);
            }
        }

        base.Draw(gameTime);
    }

    /// <summary>
    /// Draws a single drop-down item.
    /// This can be overridden in derived classes to customize the drawing code.
    /// </summary>
    /// <param name="index">The index of the item to be drawn.</param>
    /// <param name="y">The Y coordinate of the item's top border.</param>
    protected virtual void DrawItem(int index, int y)
    {
        XNADropDownItem item = Items[index];

        if (hoveredIndex == index)
        {
            FillRectangle(new Rectangle(1, y, Width - 2, ItemHeight), FocusColor);
        }
        else
        {
            FillRectangle(new Rectangle(1, y, Width - 2, ItemHeight), BackColor);
        }

        int textX = 2;
        if (item.Texture != null)
        {
            DrawTexture(item.Texture, new Rectangle(1, y + 1, item.Texture.Width, item.Texture.Height), Color.White);
            textX += item.Texture.Width + 1;
        }

        Color textColor;

        if (item.Selectable)
            textColor = GetItemTextColor(item);
        else
            textColor = DisabledItemColor;

        if (item.Text != null)
            DrawStringWithShadow(item.Text, FontIndex, new Vector2(textX, y + 1), textColor);
    }
}

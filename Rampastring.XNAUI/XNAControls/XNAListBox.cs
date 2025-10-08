using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#if WINFORMS
using Rampastring.XNAUI.Input;
using TextCopy;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using Rampastring.Tools;
using System.Globalization;

namespace Rampastring.XNAUI.XNAControls;

/// <summary>
/// A list box.
/// </summary>
public class XNAListBox : XNAPanel
{
    private const int MARGIN = 2;
    private const int ITEM_TEXT_TEXTURE_MARGIN = 2;

    /// <summary>
    /// Creates a new list box instance.
    /// </summary>
    /// <param name="windowManager"></param>
    public XNAListBox(WindowManager windowManager) : base(windowManager)
    {
        DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
        _lineHeight = UISettings.ActiveSettings.ListBoxDefaultItemHeight.GetValueOrDefault((int)Renderer.MeasureString("Test String @", FontIndex).Y - 1);
        ScrollBar = new XNAScrollBar(WindowManager);
        ScrollBar.Name = "XNAListBoxScrollBar";
        ScrollBar.ScrollStep = LineHeight;
        ClientRectangleUpdated += XNAListBox_ClientRectangleUpdated;
    }

    private void XNAListBox_ClientRectangleUpdated(object sender, EventArgs e)
    {
        if (ScrollBar != null)
        {
            ScrollBar.ClientRectangle = new Rectangle(Width - ScrollBar.ScrollWidth - 1,
                1, ScrollBar.ScrollWidth, Height - 2);
            ScrollBar.DisplayedPixelCount = Height - MARGIN * 2;
            ScrollBar.Refresh();
        }
    }

    public event EventHandler HoveredIndexChanged;
    public event EventHandler SelectedIndexChanged;
    public event EventHandler TopIndexChanged;

    #region Public members

    /// <summary>
    /// Returns the list of items in the list box.
    /// If you manipulate the list directly, call
    /// RefreshScrollbar afterwards.
    /// !!! DO NOT remove items directly, use <see cref="Clear"/> or
    /// one of the <see cref="RemoveItem(int)"/> overloads instead
    /// or you risk leaking memory.
    /// TODO change to ObservableCollection?
    /// </summary>
    public List<XNAListBoxItem> Items = new List<XNAListBoxItem>();

    private Color? _focusColor;

    public Color FocusColor
    {
        get => _focusColor ?? UISettings.ActiveSettings.FocusColor;
        set { _focusColor = value; }
    }

    private Color? _defaultItemColor;

    public Color DefaultItemColor
    {
        get => _defaultItemColor ?? UISettings.ActiveSettings.AltColor;
        set { _defaultItemColor = value; }
    }

    private int _lineHeight;

    /// <summary>
    /// Gets or sets the height of a single line of text in the list box.
    /// </summary>
    public int LineHeight
    {
        get => _lineHeight;
        set { _lineHeight = value; ScrollBar.ScrollStep = value; }
    }

    public int FontIndex { get; set; }

    /// <summary>
    /// If set to false, only the first line will be displayed from items
    /// that are long enough to cover more than one line. Changing this
    /// only affects new items in the list box; existing items are not
    /// truncated!
    /// </summary>
    public bool AllowMultiLineItems { get; set; } = true;

    /// <summary>
    /// If set to true, the user is able to scroll the listbox items
    /// by using keyboard keys.
    /// </summary>
    public bool AllowKeyboardInput { get; set; } = true;

    /// <summary>
    /// Gets or sets the distance between the text of a list box item
    /// and the list box border in pixels.
    /// </summary>
    public int TextBorderDistance { get; set; } = 3;

    private int _viewTop;

    public int ViewTop
    {
        get => _viewTop;
        set
        {
            if (value != _viewTop)
            {
                if (_viewTop < 0)
                    _viewTop = 0;
                else
                    _viewTop = value;
                TopIndexChanged?.Invoke(this, EventArgs.Empty);
                ScrollBar.RefreshButtonY(_viewTop);
            }
        }
    }

    public int TopIndex
    {
        get
        {
            int h = 0;

            for (int i = 0; i < Items.Count; i++)
            {
                h += Items[i].TextLines.Count * LineHeight;
                if (h > ViewTop)
                    return i;
            }

            return Items.Count;
        }
        set
        {
            int h = 0;

            for (int i = 0; i < value && i < Items.Count; i++)
            {
                h += Items[i].TextLines.Count * LineHeight;
            }

            ViewTop = h;
        }
    }

    public int LastIndex
    {
        get
        {
            int height = 1 - ViewTop % LineHeight;

            for (int i = TopIndex; i < Items.Count; i++)
            {
                XNAListBoxItem lbItem = Items[i];

                height += lbItem.TextLines.Count * LineHeight;

                if (height >= Height)
                    return i;
            }

            return Items.Count - 1;
        }
        set
        {
            int requiredHeight = MARGIN;

            for (int i = 0; i < Items.Count; i++)
            {
                XNAListBoxItem lbItem = Items[i];

                requiredHeight += lbItem.TextLines.Count * LineHeight;

                if (i == value)
                {
                    break;
                }
            }

            ViewTop = requiredHeight - Height;
        }
    }

    public float ItemAlphaRate { get; set; } = 0.01f;

    private int selectedIndex = -1;
    public int SelectedIndex
    {
        get { return selectedIndex; }
        set
        {
            int oldSelectedIndex = selectedIndex;

            selectedIndex = value;

            if (value != oldSelectedIndex && SelectedIndexChanged != null)
                SelectedIndexChanged(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gets the currently selected list box item.
    /// </summary>
    public XNAListBoxItem SelectedItem
    {
        get
        {
            if (SelectedIndex < 0 || SelectedIndex >= Items.Count)
                return null;

            return Items[SelectedIndex];
        }
    }

    private int hoveredIndex = -1;
    public int HoveredIndex
    {
        get
        {
            return hoveredIndex;
        }
        set
        {
            int oldHoveredIndex = hoveredIndex;

            hoveredIndex = value;

            if (value != oldHoveredIndex && HoveredIndexChanged != null)
                HoveredIndexChanged(this, EventArgs.Empty);
        }
    }

    public XNAListBoxItem HoveredItem
    {
        get
        {
            if (HoveredIndex < 0 || HoveredIndex >= Items.Count)
                return null;

            return Items[HoveredIndex];
        }
    }

    /// <summary>
    /// Returns the number of text lines that can fit on the list box at a time.
    /// </summary>
    public int NumberOfLinesOnList
    {
        get { return (ClientRectangle.Height - 4) / LineHeight; }
    }

    private bool _enableScrollbar = true;

    /// <summary>
    /// Controls whether the integrated listbox scrollbar is used.
    /// </summary>
    public bool EnableScrollbar
    {
        get { return _enableScrollbar; }
        set
        {
            _enableScrollbar = value;

            ScrollBar.Visible = _enableScrollbar;
            ScrollBar.Enabled = _enableScrollbar;
        }
    }

    /// <summary>
    /// Gets or sets a bool that determines whether the user is able to un-select
    /// the currently selected listbox item by right-clicking on the list box.
    /// </summary>
    public bool AllowRightClickUnselect { get; set; } = true;

    /// <summary>
    /// Controls whether the highlighted background of the selected item should
    /// be drawn under the scrollbar area.
    /// </summary>
    public bool DrawSelectionUnderScrollbar { get; set; } = false;

    #endregion

    protected XNAScrollBar ScrollBar;

    private TimeSpan scrollKeyTime = TimeSpan.Zero;
    private TimeSpan timeSinceLastScroll = TimeSpan.Zero;
    private bool isScrollingQuickly = false;
    private bool selectedIndexChanged = false;

    public override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "EnableScrollbar":
                EnableScrollbar = Conversions.BooleanFromString(value, true);
                return;
            case "DrawSelectionUnderScrollbar":
                DrawSelectionUnderScrollbar = Conversions.BooleanFromString(value, true);
                return;
            case nameof(AllowMultiLineItems):
                AllowMultiLineItems = Conversions.BooleanFromString(value, AllowMultiLineItems);
                return;
            case nameof(AllowRightClickUnselect):
                AllowRightClickUnselect = Conversions.BooleanFromString(value, AllowRightClickUnselect);
                return;
            case nameof(FontIndex):
                FontIndex = Conversions.IntFromString(value, FontIndex);
                return;
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    public void Clear()
    {
        Items.ForEach(item => item.TextChanged -= ListBoxItem_TextChanged);
        Items.Clear();
        RefreshScrollbar();
    }

    /// <summary>
    /// Adds a selectable item to the list box with the default item color.
    /// </summary>
    /// <param name="text">The text of the item.</param>
    public void AddItem(string text)
    {
        AddItem(text, null, true);
    }

    public void AddItem(string text,object tag)
    {
        AddItem(text, null, true,null,tag);
    }

    public void AddItem(string text, bool selectable)
    {
        AddItem(text, null, selectable);
    }

    public void AddItem(string text, Texture2D texture)
    {
        AddItem(text, texture, true, null);
    }

    public void AddItem(string text, Color textColor)
    {
        AddItem(text, textColor, true);
    }

    public void AddItem(string text, Color? textColor, bool selectable)
    {
        AddItem(text, null, selectable, textColor);
    }

    public void AddItem(string text, Texture2D texture, bool selectable, Color? textColor = null,object tag = null)
    {
        var item = new XNAListBoxItem();
        if (textColor.HasValue)
            item.TextColor = textColor.Value;
        item.Text = text;
        item.Tag = tag;
        item.Texture = texture;
        item.Selectable = selectable;
        AddItem(item);
    }

    /// <summary>
    /// Adds an item into the list box.
    /// </summary>
    /// <param name="listBoxItem">The item to add.</param>
    public void AddItem(XNAListBoxItem listBoxItem)
    {
        CheckItemTextForWordWrapAndExcessSize(listBoxItem);

        Items.Add(listBoxItem);
        RefreshScrollbar();

        listBoxItem.TextChanged += ListBoxItem_TextChanged;
    }

    private void ListBoxItem_TextChanged(object sender, EventArgs e) =>
        CheckItemTextForWordWrapAndExcessSize((XNAListBoxItem)sender);

    private void CheckItemTextForWordWrapAndExcessSize(XNAListBoxItem listBoxItem)
    {
        int width = Width - TextBorderDistance * 2;
        if (EnableScrollbar)
        {
            width -= ScrollBar.Width;
        }

        if (listBoxItem.Texture != null)
        {
            int textureHeight = listBoxItem.Texture.Height;
            int textureWidth = listBoxItem.Texture.Width;

            if (textureHeight > LineHeight)
            {
                double scaleRatio = textureHeight / (double)LineHeight;
                textureWidth = (int)(textureWidth / scaleRatio);
            }

            width -= textureWidth + ITEM_TEXT_TEXTURE_MARGIN;
        }

        // Apply word wrap if needed
        List<string> textLines = Renderer.GetFixedTextLines(listBoxItem.Text, FontIndex, width);
        if (textLines.Count == 0)
            textLines.Add(string.Empty);
        listBoxItem.TextLines = textLines;

        if (textLines.Count > 1 && !AllowMultiLineItems)
            textLines.RemoveRange(1, textLines.Count - 1);

        if (textLines.Count == 1)
        {
            Vector2 textSize = Renderer.GetTextDimensions(textLines[0], FontIndex);
            listBoxItem.TextYPadding = (LineHeight - (int)textSize.Y) / 2;

            if (listBoxItem.IsHeader)
            {
                listBoxItem.TextXPadding = (width - (int)textSize.X) / 2;
            }
        }
    }

    /// <summary>
    /// Removes the item at the specified index of the list box.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    public void RemoveItem(int index)
    {
        var item = Items[index];
        item.TextChanged -= ListBoxItem_TextChanged;
        Items.RemoveAt(index);
    }

    /// <summary>
    /// Removes the first item of the list box that fills the given condition.
    /// Returns a bool that tells whether an item filling the condition
    /// was found (and removed) from the list.
    /// </summary>
    public bool RemoveItem(Predicate<XNAListBoxItem> condition)
    {
        int index = Items.FindIndex(condition);
        if (index > -1)
        {
            RemoveItem(index);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes the given item at the specified index of the list box.
    /// Returns a bool that tells whether the item was found (and removed)
    /// from the list.
    /// </summary>
    public bool RemoveItem(XNAListBoxItem item)
    {
        int index = Items.IndexOf(item);
        if (index > -1)
        {
            RemoveItem(index);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Refreshes the scroll bar's status. Call when list box items are modified.
    /// </summary>
    public void RefreshScrollbar()
    {
        ScrollBar.Length = GetTotalLineCount() * LineHeight;
        ScrollBar.DisplayedPixelCount = Height - MARGIN * 2;
        ScrollBar.Refresh();
    }

    /// <summary>
    /// Returns the total amount of lines in all list box items combined.
    /// </summary>
    private int GetTotalLineCount()
    {
        int lineCount = 0;

        foreach (XNAListBoxItem item in Items)
        {
            if (item.Visible)
                lineCount += item.TextLines.Count;
        }

        return lineCount;
    }

    /// <summary>
    /// Checks whether the list box is scrolled so that
    /// the last item in the list is entirely visible.
    /// </summary>
    public bool IsScrolledToBottom()
    {
        return ViewTop + Height >= GetTotalLineCount() * LineHeight;
    }

    /// <summary>
    /// Scrolls the list box so that the last item is entirely visible.
    /// </summary>
    public void ScrollToBottom()
    {
        if (GetTotalLineCount() <= NumberOfLinesOnList)
        {
            TopIndex = 0;
            return;
        }

        ViewTop = GetTotalLineCount() * LineHeight - Height + MARGIN * 2;
    }

    /// <summary>
    /// Scrolls the list box so that the selected element is made visible, if it is not visible already.
    /// </summary>
    public void ScrollToSelectedElement()
    {
        int totalHeight = 0;
        int itemY = 0;

        for (int i = 0; i < Items.Count; i++)
        {
            int elementHeight = Items[i].TextLines.Count * LineHeight;

            totalHeight += elementHeight;

            if (i < SelectedIndex)
                itemY += elementHeight;
        }

        const int listBoxMargin = 2;

        if (ViewTop > itemY + LineHeight)
            ViewTop = itemY;
        else if (ViewTop + Height <= itemY)
            ViewTop = Math.Min(itemY, totalHeight - Height + (listBoxMargin * 2));
    }

    /// <summary>
    /// Initializes the list box.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

#if WINFORMS
        Keyboard.OnKeyPressed += Keyboard_OnKeyPressed;
#endif

#if !XNA
        Game.Window.TextInput += Window_TextInput;
#else
        KeyboardEventInput.CharEntered += KeyboardEventInput_CharEntered;
#endif

        ScrollBar.ClientRectangle = new Rectangle(Width - ScrollBar.ScrollWidth - 1,
            1, ScrollBar.ScrollWidth, Height - 2);
        ScrollBar.Scrolled += ScrollBar_Scrolled;
        AddChild(ScrollBar);
        ScrollBar.Refresh();

        ParentChanged += Parent_ClientRectangleUpdated;

        if (Parent != null)
            Parent.ClientRectangleUpdated += Parent_ClientRectangleUpdated;
    }

    public override void Kill()
    {
#if WINFORMS
        Keyboard.OnKeyPressed -= Keyboard_OnKeyPressed;
#endif

#if !XNA
        Game.Window.TextInput -= Window_TextInput;
#else
        KeyboardEventInput.CharEntered -= KeyboardEventInput_CharEntered;
#endif

        ParentChanged -= Parent_ClientRectangleUpdated;

        if (Parent != null)
            Parent.ClientRectangleUpdated -= Parent_ClientRectangleUpdated;

        base.Kill();
    }

    private void Parent_ClientRectangleUpdated(object sender, EventArgs e)
    {
        ScrollBar.Refresh();
    }

    /// <summary>
    /// Returns the width of the list box's scroll bar.
    /// </summary>
    public int GetScrollBarWidth()
    {
        return ScrollBar.Width;
    }

    private void ScrollBar_Scrolled(object sender, EventArgs e)
    {
        ViewTop = ScrollBar.ViewTop;
    }

#if WINFORMS
    /// <summary>
    /// Allows copying items to the clipboard using Ctrl + C.
    /// </summary>
    private void Keyboard_OnKeyPressed(object sender, KeyPressEventArgs e)
    {
        if (!IsActive || !Enabled || SelectedItem == null)
            return;

        if (e.PressedKey == Keys.C && Keyboard.IsCtrlHeldDown())
            ClipboardService.SetText(SelectedItem.Text);
    }
#endif

#if XNA
    private void KeyboardEventInput_CharEntered(object sender, KeyboardEventArgs e)
    {
        HandleCharInput(e.Character);
    }
#else
    private void Window_TextInput(object sender, TextInputEventArgs e)
    {
        HandleCharInput(e.Character);
    }
#endif

    /// <summary>
    /// Allows the user to select items by selecting the list box and then
    /// pressing the first letter of the item's text.
    /// </summary>
    /// <param name="character">The entered character.</param>
    private void HandleCharInput(char character)
    {
        if (WindowManager.SelectedControl != this || !Enabled || (Parent != null && !Parent.Enabled) || !WindowManager.HasFocus || !AllowKeyboardInput)
            return;

        string charString = character.ToString();

        for (int i = SelectedIndex + 1; i < Items.Count; i++)
        {
            var item = Items[i];

            if (!item.Selectable)
                return;

            if (item.TextLines.Count == 0)
                return;

            if (item.TextLines[0].StartsWith(charString, true, CultureInfo.CurrentCulture))
            {
                SelectedIndex = i;

                int lastIndex = LastIndex;

                if (lastIndex < SelectedIndex)
                    TopIndex += SelectedIndex - lastIndex;

                break;
            }
        }
    }

    public override void Update(GameTime gameTime)
    {
        var itemsCopy = Items.ToList();
        foreach (XNAListBoxItem lbItem in itemsCopy)
        {
            if (lbItem.Alpha < 1.0f)
                lbItem.Alpha += ItemAlphaRate;
        }

        if (IsActive && AllowKeyboardInput)
        {
            if (Keyboard.IsKeyHeldDown(Keys.Up))
            {
                HandleScrollKeyDown(gameTime, ScrollUp);
            }
            else if (Keyboard.IsKeyHeldDown(Keys.Down))
            {
                HandleScrollKeyDown(gameTime, ScrollDown);
            }
            else
            {
                isScrollingQuickly = false;
                timeSinceLastScroll = TimeSpan.Zero;
                scrollKeyTime = TimeSpan.Zero;
            }
        }

        base.Update(gameTime);
    }

    /// <summary>
    /// Handles input from scrolling keys.
    /// </summary>
    /// <param name="gameTime">GameTime.</param>
    /// <param name="action">The action to execute when scrolling.</param>
    private void HandleScrollKeyDown(GameTime gameTime, Action action)
    {
        if (scrollKeyTime.Equals(TimeSpan.Zero))
            action();

        WindowManager.SelectedControl = this;

        scrollKeyTime += gameTime.ElapsedGameTime;

        if (isScrollingQuickly)
        {
            timeSinceLastScroll += gameTime.ElapsedGameTime;

            if (timeSinceLastScroll > TimeSpan.FromSeconds(XNAUIConstants.KEYBOARD_SCROLL_REPEAT_TIME))
            {
                timeSinceLastScroll = TimeSpan.Zero;
                action();
            }
        }

        if (scrollKeyTime > TimeSpan.FromSeconds(XNAUIConstants.KEYBOARD_FAST_SCROLL_TRIGGER_TIME) && !isScrollingQuickly)
        {
            isScrollingQuickly = true;
            timeSinceLastScroll = TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Handles scrolling by holding down the up arrow key.
    /// </summary>
    private void ScrollUp()
    {
        if (SelectedIndex >= Items.Count)
        {
            SelectedIndex = Items.Count - 1;
            return;
        }

        for (int i = SelectedIndex - 1; i > -1; i--)
        {
            if (!Items[i].Visible)
                continue;

            if (Items[i].Selectable)
            {
                SelectedIndex = i;
                if (TopIndex > i)
                    TopIndex = i;

                ScrollBar.RefreshButtonY(ViewTop);
                return;
            }
        }

        ScrollBar.RefreshButtonY(ViewTop);
    }

    /// <summary>
    /// Handles scrolling by holding down the down arrow key.
    /// </summary>
    private void ScrollDown()
    {
        int scrollLineCount = 1;
        for (int i = SelectedIndex + 1; i < Items.Count; i++)
        {
            if (!Items[i].Visible)
                continue;

            if (Items[i].Selectable)
            {
                SelectedIndex = i;
                while (LastIndex < i)
                    TopIndex++;

                ScrollBar.RefreshButtonY(ViewTop);
                return;
            }

            scrollLineCount++;
        }

        ScrollBar.RefreshButtonY(ViewTop);
    }

    /// <summary>
    /// Handles input from a scroll wheel.
    /// </summary>
    public override void OnMouseScrolled(InputEventArgs inputEventArgs)
    {
        inputEventArgs.Handled = true;

        if (GetTotalLineCount() <= NumberOfLinesOnList)
        {
            TopIndex = 0;
            return;
        }

        ViewTop -= Cursor.ScrollWheelValue * ScrollBar.ScrollStep;

        if (ViewTop < 0)
        {
            TopIndex = 0;
            return;
        }

        if (IsScrolledToBottom())
        {
            // Show as many items above the last item as possible
            ScrollToBottom();
        }

        base.OnMouseScrolled(inputEventArgs);
    }

    /// <summary>
    /// Updates the hovered item index while the cursor is on this control.
    /// </summary>
    public override void OnMouseOnControl()
    {
        base.OnMouseOnControl();

        int itemIndex = GetItemIndexOnCursor(GetCursorPoint());
        HoveredIndex = itemIndex;
    }

    /// <summary>
    /// Clears the selection on right-click.
    /// </summary>
    public override void OnRightClick(InputEventArgs inputEventArgs)
    {
        if (AllowRightClickUnselect)
        {
            inputEventArgs.Handled = true;
            SelectedIndex = -1;
        }

        base.OnRightClick(inputEventArgs);
    }

    /// <summary>
    /// Selects an item when the user left-clicks on this control.
    /// </summary>
    public override void OnMouseLeftDown(InputEventArgs inputEventArgs)
    {
        inputEventArgs.Handled = true;
        int itemIndex = GetItemIndexOnCursor(GetCursorPoint());

        if (itemIndex == -1)
            return;

        selectedIndexChanged = false;

        if (Items[itemIndex].Selectable && itemIndex != SelectedIndex)
        {
            selectedIndexChanged = true;
            SelectedIndex = itemIndex;
        }

        base.OnMouseLeftDown(inputEventArgs);
    }

    public override void OnDoubleLeftClick(InputEventArgs inputEventArgs)
    {
        // We don't want to send a "double left click" message if the user
        // is just quickly changing the selected index
        if (!selectedIndexChanged)
            base.OnDoubleLeftClick(inputEventArgs);
    }

    /// <summary>
    /// Updates the hovered index when the mouse cursor leaves this control's client rectangle.
    /// </summary>
    public override void OnMouseLeave()
    {
        base.OnMouseLeave();

        HoveredIndex = -1;
    }

    /// <summary>
    /// Returns the index of the list box item that the cursor is
    /// currently pointing at, or -1 if the cursor doesn't point at any item
    /// of this list box.
    /// </summary>
    /// <param name="mouseLocation">The location of the cursor relative 
    /// to this control.</param>
    private int GetItemIndexOnCursor(Point mouseLocation)
    {
        if (mouseLocation.X < 0)
            return -1;

        if (EnableScrollbar)
        {
            if (mouseLocation.X > Width - ScrollBar.ScrollWidth)
                return -1;
        }
        else if (mouseLocation.X > Width)
        {
            return -1;
        }

        var drawInfo = GetTopIndexAndDrawOffset();
        int height = MARGIN + drawInfo.YDrawOffset;

        for (int i = drawInfo.TopIndex; i < Items.Count; i++)
        {
            XNAListBoxItem lbItem = Items[i];

            if (!lbItem.Visible)
                continue;

            height += lbItem.TextLines.Count * LineHeight;

            if (height > mouseLocation.Y)
            {
                return i;
            }

            if (height > Height)
            {
                return -1;
            }
        }

        return -1;
    }

    private struct ListBoxItemDrawInfo
    {
        public ListBoxItemDrawInfo(int topIndex, int yDrawOffset)
        {
            TopIndex = topIndex;
            YDrawOffset = yDrawOffset;
        }

        public int TopIndex;
        public int YDrawOffset;
    }

    private ListBoxItemDrawInfo GetTopIndexAndDrawOffset()
    {
        int h = 0;
        for (int i = 0; i < Items.Count; i++)
        {
            int heightIncrease = Items[i].TextLines.Count * LineHeight;
            if (h + heightIncrease > ViewTop)
                return new ListBoxItemDrawInfo(i, h - ViewTop);
            h += heightIncrease;
        }

        return new ListBoxItemDrawInfo(Items.Count, 0);
    }

    protected virtual void DrawListBoxItem(int index, int y)
    {
        XNAListBoxItem lbItem = Items[index];

        int x = TextBorderDistance;

        if (index == SelectedIndex)
        {
            int drawnWidth;

            if (DrawSelectionUnderScrollbar || !ScrollBar.IsDrawn() || !EnableScrollbar)
            {
                drawnWidth = Width - 2;
            }
            else
            {
                drawnWidth = Width - 2 - ScrollBar.Width;
            }

            FillRectangle(new Rectangle(1, y, drawnWidth,
                lbItem.TextLines.Count * LineHeight),
                FocusColor);
        }

        if (lbItem.Texture != null)
        {
            int textureHeight = lbItem.Texture.Height;
            int textureWidth = lbItem.Texture.Width;
            int textureYPosition = 0;

            if (lbItem.Texture.Height > LineHeight)
            {
                double scaleRatio = textureHeight / (double)LineHeight;
                textureHeight = LineHeight;
                textureWidth = (int)(textureWidth / scaleRatio);
            }
            else
            {
                textureYPosition = (LineHeight - textureHeight) / 2;
            }

            DrawTexture(lbItem.Texture,
                new Rectangle(x, y + textureYPosition,
                textureWidth, textureHeight), Color.White);

            x += textureWidth + ITEM_TEXT_TEXTURE_MARGIN;
        }

        x += lbItem.TextXPadding;

        for (int j = 0; j < lbItem.TextLines.Count; j++)
        {
            DrawStringWithShadow(lbItem.TextLines[j], FontIndex,
                new Vector2(x, y + j * LineHeight + lbItem.TextYPadding),
                lbItem.TextColor);
        }
    }

    /// <summary>
    /// Draws the list box and its items.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    public override void Draw(GameTime gameTime)
    {
        DrawPanel();

        var drawInfo = GetTopIndexAndDrawOffset();
        int height = MARGIN + drawInfo.YDrawOffset;

        for (int i = drawInfo.TopIndex; i < Items.Count; i++)
        {
            XNAListBoxItem lbItem = Items[i];

            if (!lbItem.Visible)
                continue;

            DrawListBoxItem(i, height);

            height += lbItem.TextLines.Count * LineHeight;

            if (height > Height)
                break;
        }

        if (DrawBorders)
            DrawPanelBorders();

        DrawChildren(gameTime);
    }
}
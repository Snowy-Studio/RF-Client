using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;

namespace Rampastring.XNAUI.XNAControls;

/// <summary>
/// A control that has multiple tabs, of which only one can be selected at a time.
/// </summary>
public class XNATabControl : XNAControl
{
    public XNATabControl(WindowManager windowManager) : base(windowManager)
    {
    }

    public delegate void SelectedIndexChangedEventHandler(object sender, EventArgs e);
    public event SelectedIndexChangedEventHandler SelectedIndexChanged;

    private int _selectedTab = 0;
    public int SelectedTab
    {
        get { return _selectedTab; }
        set
        {
            if (_selectedTab == value)
                return;

            _selectedTab = value;

            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public int FontIndex { get; set; }

    public bool DisposeTexturesOnTabRemove { get; set; }

    private Color? _textColor;

    public Color TextColor
    {
        get => _textColor ?? UISettings.ActiveSettings.AltColor;
        set { _textColor = value; }
    }

    private Color? _textColorDisabled;

    public Color TextColorDisabled
    {
        get => _textColorDisabled ?? UISettings.ActiveSettings.DisabledItemColor;
        set { _textColorDisabled = value; }
    }

    private List<Tab> Tabs = new List<Tab>();

    public EnhancedSoundEffect ClickSound { get; set; }

    public override void Initialize()
    {
        base.Initialize();
    }

    public bool IsSelectable(int index)
    {
        return Tabs[index].Selectable;
    }

    public void MakeSelectable(int index)
    {
        Tabs[index].Selectable = true;
    }

    public void MakeUnselectable(int index)
    {
        Tabs[index].Selectable = false;
    }

    public void RemoveTab(int index)
    {
        if (DisposeTexturesOnTabRemove)
        {
            Tabs[index].DefaultTexture.Dispose();
            Tabs[index].PressedTexture.Dispose();
        }

        Tabs.RemoveAt(index);
    }

    public void RemoveTab(string text)
    {
        int index = Tabs.FindIndex(t => t.Text == text);

        Tabs.RemoveAt(index);
    }

    public void AddTab(string text, Texture2D defaultTexture, Texture2D pressedTexture)
    {
        AddTab(text, defaultTexture, pressedTexture, true);
    }

    public void AddTab(string text, Texture2D defaultTexture, Texture2D pressedTexture, bool selectable)
    {
        var tab = new Tab(text, defaultTexture, pressedTexture, selectable);
        Tabs.Add(tab);

        Vector2 textSize = Renderer.GetTextDimensions(text, FontIndex);
        tab.TextXPosition = (defaultTexture.Width - (int)textSize.X) / 2;
        tab.TextYPosition = (defaultTexture.Height - (int)textSize.Y) / 2;

        Width += defaultTexture.Width;
        Height = defaultTexture.Height;
    }

    public override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "RemapColor":
            case "TextColor":
                TextColor = AssetLoader.GetColorFromString(value);
                return;
            case "TextColorDisabled":
                TextColorDisabled = AssetLoader.GetColorFromString(value);
                return;
        }

        if (key.StartsWith("RemoveTabIndex", StringComparison.InvariantCulture))
        {
            int index = int.Parse(key.Substring(14), CultureInfo.InvariantCulture);

            if (Conversions.BooleanFromString(value, false))
                RemoveTab(index);
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    public override void OnLeftClick()
    {
        base.OnLeftClick();

        Point p = GetCursorPoint();

        int w = 0;
        int i = 0;
        foreach (Tab tab in Tabs)
        {
            w += tab.DefaultTexture.Width;

            if (p.X < w)
            {
                if (tab.Selectable)
                {
                    ClickSound?.Play();

                    SelectedTab = i;
                }

                return;
            }

            i++;
        }
    }

    public override void Draw(GameTime gameTime)
    {
        int x = 0;

        for (int i = 0; i < Tabs.Count; i++)
        {
            Tab tab = Tabs[i];

            Texture2D texture = i == SelectedTab ? tab.PressedTexture : tab.DefaultTexture;

            DrawTexture(texture, new Point(x, 0), RemapColor);

            DrawStringWithShadow(tab.Text, FontIndex,
                new Vector2(x + tab.TextXPosition, tab.TextYPosition),
                tab.Selectable && Enabled ? TextColor : TextColorDisabled);

            x += tab.DefaultTexture.Width;
        }
    }
}

internal class Tab
{
    public Tab() { }

    public Tab(string text, Texture2D defaultTexture, Texture2D pressedTexture, bool selectable)
    {
        Text = text;
        DefaultTexture = defaultTexture;
        PressedTexture = pressedTexture;
        Selectable = selectable;
    }

    public Texture2D DefaultTexture { get; set; }

    public Texture2D PressedTexture { get; set; }

    public string Text { get; set; }

    public bool Selectable { get; set; }

    public int TextXPosition { get; set; }

    public int TextYPosition { get; set; }
}

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Rampastring.XNAUI.XNAControls;

public class XNAListBoxItem
{
    public XNAListBoxItem() { }

    public XNAListBoxItem(string text)
    {
        Text = text;
    }

    public XNAListBoxItem(string text, Color textColor)
    {
        Text = text;
        TextColor = textColor;
    }

    public event EventHandler TextChanged;

    private Color? _textColor;

    public Color TextColor
    {
        get
        {
            if (_textColor.HasValue)
                return _textColor.Value;

            if (!Selectable)
                return UISettings.ActiveSettings.DisabledItemColor;

            return UISettings.ActiveSettings.AltColor;
        }
        set { _textColor = value; }
    }

    private Color? _backgroundColor;

    public Color BackgroundColor
    {
        get
        {
            return _backgroundColor ?? UISettings.ActiveSettings.BackgroundColor;
        }
        set { _backgroundColor = value; }
    }

    public Texture2D Texture { get; set; }

    public bool IsHeader { get; set; }

    private string _text;

    /// <summary>
    /// The text of the list box item prior to its parsing by the list box.
    /// If this is modified when the item belongs to a <see cref="XNAListBox"/>, the ListBox
    /// will re-parse it and save the result <see cref="TextLines"/> to support multi-line
    /// items and potentially cut the text if it's too long.
    /// </summary>
    public string Text
    {
        get => _text;
        set { _text = value; TextChanged?.Invoke(this, EventArgs.Empty); }
    }

    /// <summary>
    /// Stores optional custom data associated with the list box item.
    /// </summary>
    public object Tag { get; set; }

    public int TextYPadding { get; set; }

    public int TextXPadding { get; set; }
    public bool Selectable { get; set; } = true;

    private float alpha = 0.0f;
    public float Alpha
    {
        get { return alpha; }
        set
        {
            if (value < 0.0f)
                alpha = 0.0f;
            else if (value > 1.0f)
                alpha = 1.0f;
            else
                alpha = value;
        }
    }

    /// <summary>
    /// Whether this list box item is visible.
    /// Invisible list box items are not drawn and cannot be selected.
    /// </summary>
    public bool Visible { get; set; } = true;

    public List<string> TextLines;
}

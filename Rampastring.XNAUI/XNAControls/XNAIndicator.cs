using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;

namespace Rampastring.XNAUI.XNAControls;

/// <summary>
/// An indicator with dynamic textures pool.
/// </summary>
/// <typeparam name="T">The type of the enum that specifies the possible indicator states.</typeparam>
public class XNAIndicator<T> : XNAControl where T : Enum
{
    private const int TEXT_PADDING_DEFAULT = 5;

    /// <summary>
    /// Creates a new indicator.
    /// </summary>
    /// <param name="windowManager">The window manager.</param>
    public XNAIndicator(WindowManager windowManager) : base(windowManager) =>
        AlphaRate = UISettings.ActiveSettings.IndicatorAlphaRate * 2.0;

    public XNAIndicator(WindowManager windowManager, Dictionary<T, Texture2D> textures) : this(windowManager) =>
        Textures = textures;

    /// <summary>
    /// Contains a pool of textures for indicator.
    /// </summary>
    public Dictionary<T, Texture2D> Textures { get; set; }

    private Texture2D _oldTexture = null;
    private Texture2D _currentTexture = null;

    /// <summary>
    /// Determines the currently displayed texture.
    /// </summary>
    protected virtual Texture2D CurrentTexture
    {
        get => _currentTexture;
        set
        {
            if (value != _currentTexture)
            {
                _oldTexture = _currentTexture;
                _currentTexture = value;
                textureAlpha = 0.0;
            }
        }
    }

    /// <summary>
    /// The index of the text font.
    /// </summary>
    public int FontIndex { get; set; }

    /// <summary>
    /// The space, in pixels, between the indicator and its text.
    /// </summary>
    public int TextPadding { get; set; } = TEXT_PADDING_DEFAULT;

    private Color? _idleColor;

    /// <summary>
    /// The color of the indicator's text when it's not hovered on.
    /// </summary>
    public Color IdleColor
    {
        get => _idleColor ?? UISettings.ActiveSettings.TextColor;
        set => _idleColor = value;
    }

    private Color? _highlightColor;

    /// <summary>
    /// The color of the indicator's text when it's hovered on.
    /// </summary>
    public Color HighlightColor
    {
        get => _highlightColor ?? UISettings.ActiveSettings.AltColor;
        set => _highlightColor = value;
    }

    public double AlphaRate { get; set; }

    /// <summary>
    /// Gets or sets the text of the indicator.
    /// </summary>
    public override string Text
    {
        get => base.Text;

        set
        {
            base.Text = value;
            SetTextPositionAndSize();
        }
    }

    /// <summary>
    /// The Y coordinate of the indicator text
    /// relative to the location of the indicator.
    /// </summary>
    protected int TextLocationY { get; set; }

    private double textureAlpha = 1.0;

    public override void Initialize()
    {
        if (Textures == null || Textures.Count == 0)
            throw new InvalidOperationException($"{nameof(XNAIndicator<T>)}: No textures specified!");

        if (_oldTexture == null)
            _oldTexture = Textures[default];

        if (_currentTexture == null)
            _currentTexture = Textures[default];

        SetTextPositionAndSize();

        base.Initialize();
    }

    /// <summary>
    /// Switches the texture of the indicator.
    /// </summary>
    /// <param name="key">The enum texture key.</param>
    public virtual void SwitchTexture(T key)
    {
        if (Textures.ContainsKey(key))
            CurrentTexture = Textures[key];
        else
            Logger.Log($"{nameof(XNAIndicator<T>)}: Tried to switch to non-existing texture {key} at indicator {Name}!");
    }

    public override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "FontIndex":
                FontIndex = Conversions.IntFromString(value, 0);
                return;
            case "HighlightColor":
                HighlightColor = AssetLoader.GetColorFromString(value);
                return;
            case "AlphaRate":
                AlphaRate = Conversions.DoubleFromString(value, AlphaRate);
                return;
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    /// <summary>
    /// Updates the size of the indicator and the vertical position of its text.
    /// </summary>
    protected virtual void SetTextPositionAndSize()
    {
        if (Textures == null || Textures.Count == 0)
            return;

        var enumerator = Textures.Values.GetEnumerator();
        enumerator.MoveNext();
        Texture2D texture2D = enumerator.Current;

        if (!string.IsNullOrEmpty(Text))
        {
            Vector2 textDimensions = Renderer.GetTextDimensions(Text, FontIndex);

            TextLocationY = (texture2D.Height - (int)textDimensions.Y) / 2 - 1;

            Width = (int)textDimensions.X + TEXT_PADDING_DEFAULT + texture2D.Width;
            Height = Math.Max((int)textDimensions.Y, texture2D.Height);
        }
        else
        {
            Width = texture2D.Width;
            Height = texture2D.Height;
        }
    }

    /// <summary>
    /// Updates the indicator's alpha each frame.
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        double alphaRate = AlphaRate * (gameTime.ElapsedGameTime.TotalMilliseconds / 10.0);
        textureAlpha = Math.Min(textureAlpha + alphaRate, 1.0);

        base.Update(gameTime);
    }

    /// <summary>
    /// Draws the indicator.
    /// </summary>
    public override void Draw(GameTime gameTime)
    {
        int indicatorYPosition = 0;
        int textYPosition = TextLocationY;

        if (TextLocationY < 0)
        {
            // If the text is higher than the indicator texture
            // (textLocationY < 0), let's draw the text at the top of 
            // the client rectangle and the indicator in the middle of 
            // the text. This is necessary for input to work properly.
            indicatorYPosition -= TextLocationY;
            textYPosition = 0;
        }

        if (!string.IsNullOrEmpty(Text))
        {
            Color textColor = IsActive ? HighlightColor : IdleColor;

            DrawStringWithShadow(Text, FontIndex,
                new Vector2(CurrentTexture.Width + TextPadding, textYPosition),
                textColor);
        }

        // Don't draw old texture if new one is fully opaque
        if (textureAlpha < 1.0)
        {
            DrawTexture(_oldTexture,
                new Rectangle(0, indicatorYPosition,
                _oldTexture.Width, _oldTexture.Height),
                Color.White);
        }

        // Don't draw new texture if it's fully invisible
        if (textureAlpha > 0.0)
        {
            DrawTexture(_currentTexture,
                new Rectangle(0, indicatorYPosition,
                _currentTexture.Width, _currentTexture.Height),
                Color.White * (float)textureAlpha);
        }

        base.Draw(gameTime);
    }
}

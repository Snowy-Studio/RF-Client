using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Rampastring.Tools;
using System;

namespace Rampastring.XNAUI.XNAControls;

/// <summary>
/// A basic button.
/// </summary>
public class XNAButton : XNAControl
{
    public XNAButton(WindowManager windowManager) : base(windowManager)
    {
        AlphaRate = UISettings.ActiveSettings.DefaultAlphaRate;
    }

    public Texture2D IdleTexture { get; set; }

    public Texture2D HoverTexture { get; set; }

    public EnhancedSoundEffect HoverSoundEffect { get; set; }

    public EnhancedSoundEffect ClickSoundEffect { get; set; }

    public float AlphaRate { get; set; }
    public float IdleTextureAlpha { get; private set; } = 1.0f;
    public float HoverTextureAlpha { get; private set; } = 0.0f;

    public Keys HotKey { get; set; }

    public int FontIndex { get; set; }

    public float TextShadowDistance { get; set; } = UISettings.ActiveSettings.TextShadowDistance;

    private bool _allowClick = true;
    public bool AllowClick
    {
        get => _allowClick;
        set
        {
            _allowClick = value;
            if (_allowClick && cursorOnControl)
                AnimationMode = ButtonAnimationMode.HIGHLIGHT;
            else
                AnimationMode = ButtonAnimationMode.RETURN;
        }
    }

    private string _text = String.Empty;
    public override string Text
    {
        get { return _text; }
        set
        {
            _text = value;
            if (AdaptiveText)
            {
                CalculateTextPosition();
            }
        }
    }

    public int TextXPosition { get; set; }
    public int TextYPosition { get; set; }

    private Color? _textColorIdle;

    public Color TextColorIdle
    {
        get => _textColorIdle ?? UISettings.ActiveSettings.ButtonTextColor;
        set
        {
            _textColorIdle = value;

            if (!IsActive)
                textColor = value;
        }
    }

    private Color? _textColorHover;

    public Color TextColorHover
    {
        get => _textColorHover ?? UISettings.ActiveSettings.ButtonHoverColor;
        set => _textColorHover = value;
    }

    private Color? _textColorDisabled;

    public Color TextColorDisabled
    {
        get => _textColorDisabled ?? UISettings.ActiveSettings.DisabledItemColor;
        set => _textColorDisabled = value;
    }

    public bool AdaptiveText { get; set; } = true;

    /// <summary>
    /// The current color of the button's text.
    /// </summary>
    private Color textColor = Color.White;

    private ButtonAnimationMode AnimationMode { get; set; }

    private bool cursorOnControl = false;

    public override void OnMouseEnter()
    {
        base.OnMouseEnter();

        cursorOnControl = true;

        if (Cursor.LeftDown)
            return;

        textColor = TextColorHover;

        if (!AllowClick)
            return;

        HoverSoundEffect?.Play();

        if (HoverTexture != null)
        {
            IdleTextureAlpha = 0.5f;
            HoverTextureAlpha = 0.75f;
            AnimationMode = ButtonAnimationMode.HIGHLIGHT;
        }
    }

    public override void OnMouseLeave()
    {
        base.OnMouseLeave();

        cursorOnControl = false;
        textColor = TextColorIdle;

        if (!AllowClick)
            return;

        if (HoverTexture != null)
        {
            IdleTextureAlpha = 0.75f;
            HoverTextureAlpha = 0.5f;
            AnimationMode = ButtonAnimationMode.RETURN;
        }
    }

    public override void OnLeftClick()
    {
        if (!AllowClick)
            return;

        ClickSoundEffect?.Play();

        base.OnLeftClick();
    }

    public override void Initialize()
    {
        base.Initialize();

        if (IdleTexture != null && Width == 0 && Height == 0)
        {
            ClientRectangle = new Rectangle(X, Y,
                IdleTexture.Width, IdleTexture.Height);
        }

        textColor = TextColorIdle;
    }

    protected override void OnClientRectangleUpdated()
    {
        if (AdaptiveText)
        {
            CalculateTextPosition();
        }

        base.OnClientRectangleUpdated();
    }

    private void CalculateTextPosition()
    {
        Vector2 textSize = Renderer.GetTextDimensions(_text, FontIndex);

        if (textSize.X < Width)
        {
            TextXPosition = (int)((Width - textSize.X) / 2);
        }
        else if (textSize.X > Width)
        {
            TextXPosition = (int)((textSize.X - Width) / -2);
        }

        if (textSize.Y < Height)
        {
            TextYPosition = (int)((Height - textSize.Y) / 2);
        }
        else if (textSize.Y > Height)
        {
            TextYPosition = Convert.ToInt32((textSize.Y - Height) / -2);
        }
    }

    public override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "TextColorIdle":
                TextColorIdle = AssetLoader.GetColorFromString(value);
                textColor = TextColorIdle;
                return;
            case "TextColorHover":
                TextColorHover = AssetLoader.GetColorFromString(value);
                return;
            case "HoverSoundEffect":
                HoverSoundEffect = new EnhancedSoundEffect(value);
                return;
            case "ClickSoundEffect":
                ClickSoundEffect = new EnhancedSoundEffect(value);
                return;
            case "AdaptiveText":
                AdaptiveText = Conversions.BooleanFromString(value, true);
                return;
            case "AlphaRate":
                AlphaRate = Conversions.FloatFromString(value, 0.01f);
                return;
            case "FontIndex":
                FontIndex = Conversions.IntFromString(value, 0);
                if (AdaptiveText)
                    CalculateTextPosition();
                return;
            case "IdleTexture":
                IdleTexture = AssetLoader.LoadTexture(value);
                ClientRectangle = new Rectangle(X, Y,
                    IdleTexture.Width, IdleTexture.Height);
                if (AdaptiveText)
                    CalculateTextPosition();
                return;
            case "HoverTexture":
                HoverTexture = AssetLoader.LoadTexture(value);
                return;
            case "TextShadowDistance":
                TextShadowDistance = Conversions.FloatFromString(value, TextShadowDistance);
                return;
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    public override void Kill()
    {
        base.Kill();

        if (IdleTexture != null)
            IdleTexture.Dispose();

        if (HoverTexture != null)
            HoverTexture.Dispose();

        if (HoverSoundEffect != null)
            HoverSoundEffect.Dispose();

        if (ClickSoundEffect != null)
            ClickSoundEffect.Dispose();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        float alphaRate = AlphaRate * (float)(gameTime.ElapsedGameTime.TotalMilliseconds / 10.0);

        if (AnimationMode == ButtonAnimationMode.HIGHLIGHT)
        {
            IdleTextureAlpha -= alphaRate;
            if (IdleTextureAlpha < 0.0f)
            {
                IdleTextureAlpha = 0.0f;
            }

            HoverTextureAlpha += alphaRate;
            if (HoverTextureAlpha >= 1.0f)
            {
                HoverTextureAlpha = 1.0f;
            }
        }
        else
        {
            HoverTextureAlpha -= alphaRate;
            if (HoverTextureAlpha < 0.0f)
            {
                HoverTextureAlpha = 0.0f;
            }

            IdleTextureAlpha += alphaRate;
            if (IdleTextureAlpha >= 1.0f)
            {
                IdleTextureAlpha = 1.0f;
            }
        }

        if (Parent != null && Parent.IsActive && Keyboard.PressedKeys.Contains(HotKey))
            OnLeftClick();
    }

    public override void Draw(GameTime gameTime)
    {
        if (IdleTexture != null)
        {
            if (IdleTextureAlpha > 0f)
            {
                DrawTexture(IdleTexture, new Rectangle(0, 0, Width, Height),
                    RemapColor * IdleTextureAlpha * Alpha);
            }

            if (HoverTexture != null && HoverTextureAlpha > 0f)
            {
                DrawTexture(HoverTexture, new Rectangle(0, 0, Width, Height),
                    RemapColor * HoverTextureAlpha * Alpha);
            }
        }

        var textPosition = new Vector2(TextXPosition, TextYPosition);

        if (!Enabled || !AllowClick)
            DrawStringWithShadow(_text, FontIndex, textPosition, TextColorDisabled, 1.0f, TextShadowDistance);
        else
            DrawStringWithShadow(_text, FontIndex, textPosition, textColor, 1.0f, TextShadowDistance);

        base.Draw(gameTime);
    }
}

internal enum ButtonAnimationMode
{
    NONE,
    HIGHLIGHT,
    RETURN
}

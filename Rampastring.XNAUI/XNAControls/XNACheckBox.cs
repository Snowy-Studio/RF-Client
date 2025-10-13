using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;

namespace Rampastring.XNAUI.XNAControls;

/// <summary>
/// A check-box.
/// </summary>
public class XNACheckBox : XNAControl
{
    private const int TEXT_PADDING_DEFAULT = 5;

    /// <summary>
    /// Creates a new check box.
    /// </summary>
    /// <param name="windowManager">The window manager.</param>
    public XNACheckBox(WindowManager windowManager) : base(windowManager)
    {
        AlphaRate = UISettings.ActiveSettings.CheckBoxAlphaRate * 2.0;
    }

    public event EventHandler CheckedChanged;

    public Texture2D CheckedTexture { get; set; }
    public Texture2D ClearTexture { get; set; }

    public Texture2D DisabledCheckedTexture { get; set; }
    public Texture2D DisabledClearTexture { get; set; }

    /// <summary>
    /// The sound effect that is played when the check box is clicked on.
    /// </summary>
    public EnhancedSoundEffect CheckSoundEffect { get; set; }

    /// <summary>
    /// The sound effect that is played when the cursor enters the check box's area.
    /// </summary>
    public EnhancedSoundEffect HoverSoundEffect { get; set; }

    private bool _checked = false;

    /// <summary>
    /// Determines whether the check box is currently checked.
    /// </summary>
    public bool Checked
    {
        get { return _checked; }
        set
        {
            bool originalValue = _checked;
            _checked = value;

            if (_checked != originalValue)
                CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Determines whether the user can (un)check the box by clicking on it.
    /// </summary>
    public bool AllowChecking { get; set; } = true;

    /// <summary>
    /// The index of the text font.
    /// </summary>
    public int FontIndex { get; set; }

    /// <summary>
    /// The space, in pixels, between the check box and its text.
    /// </summary>
    public int TextPadding { get; set; } = TEXT_PADDING_DEFAULT;

    private Color? _idleColor;

    /// <summary>
    /// The color of the check box's text when it's not hovered on.
    /// </summary>
    public Color IdleColor
    {
        get => _idleColor ?? UISettings.ActiveSettings.TextColor;
        set { _idleColor = value; }
    }

    private Color? _highlightColor;

    /// <summary>
    /// The color of the check box's text when it's hovered on.
    /// </summary>
    public Color HighlightColor
    {
        get => _highlightColor ?? UISettings.ActiveSettings.AltColor;
        set
        { _highlightColor = value; }
    }

    public double AlphaRate { get; set; }

    /// <summary>
    /// Gets or sets the text of the check box.
    /// </summary>
    public override string Text
    {
        get
        {
            return base.Text;
        }

        set
        {
            base.Text = value;
            SetTextPositionAndSize();
        }
    }

    /// <summary>
    /// The Y coordinate of the check box text
    /// relative to the location of the check box.
    /// </summary>
    protected int TextLocationY { get; set; }


    private double checkedAlpha = 0.0;


    public override void Initialize()
    {
        if (CheckedTexture == null)
            CheckedTexture = UISettings.ActiveSettings.CheckBoxCheckedTexture;

        if (ClearTexture == null)
            ClearTexture = UISettings.ActiveSettings.CheckBoxClearTexture;

        if (DisabledCheckedTexture == null)
            DisabledCheckedTexture = UISettings.ActiveSettings.CheckBoxDisabledCheckedTexture;

        if (DisabledClearTexture == null)
            DisabledClearTexture = UISettings.ActiveSettings.CheckBoxDisabledClearTexture;

        SetTextPositionAndSize();

        if (Checked)
        {
            checkedAlpha = 1.0;
        }

        base.Initialize();
    }

    public override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "FontIndex":
                FontIndex = Conversions.IntFromString(value, 0);
                return;
            case "IdleColor":
                IdleColor = AssetLoader.GetColorFromString(value);
                return;
            case "HighlightColor":
                HighlightColor = AssetLoader.GetColorFromString(value);
                return;
            case "AlphaRate":
                AlphaRate = Conversions.DoubleFromString(value, AlphaRate);
                return;
            case "AllowChecking":
                AllowChecking = Conversions.BooleanFromString(value, true);
                return;
            case "Checked":
                Checked = Conversions.BooleanFromString(value, true);
                return;
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    /// <summary>
    /// Updates the size of the check box and the vertical position of its text.
    /// </summary>
    protected virtual void SetTextPositionAndSize()
    {
        if (CheckedTexture == null)
            return;

        if (!string.IsNullOrEmpty(Text))
        {
            Vector2 textDimensions = Renderer.GetTextDimensions(Text, FontIndex);

            TextLocationY = (CheckedTexture.Height - (int)textDimensions.Y) / 2 - 1;

            Width = (int)textDimensions.X + TEXT_PADDING_DEFAULT + CheckedTexture.Width;
            Height = Math.Max((int)textDimensions.Y, CheckedTexture.Height);
        }
        else
        {
            Width = CheckedTexture.Width;
            Height = CheckedTexture.Height;
        }
    }

    public override void OnMouseEnter()
    {
        if (AllowChecking)
        {
            HoverSoundEffect?.Play();
        }

        base.OnMouseEnter();
    }

    /// <summary>
    /// Handles left mouse button clicks on the check box.
    /// </summary>
    public override void OnLeftClick()
    {
        if (AllowChecking)
        {
            Checked = !Checked;
            CheckSoundEffect?.Play();
        }

        base.OnLeftClick();
    }

    /// <summary>
    /// Updates the check box's alpha each frame.
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        double alphaRate = AlphaRate * (gameTime.ElapsedGameTime.TotalMilliseconds / 10.0);

        if (Checked)
        {
            checkedAlpha = Math.Min(checkedAlpha + alphaRate, 1.0);
        }
        else
        {
            checkedAlpha = Math.Max(0.0, checkedAlpha - alphaRate);
        }

        base.Update(gameTime);
    }

    /// <summary>
    /// Draws the check box.
    /// </summary>
    public override void Draw(GameTime gameTime)
    {
        Texture2D clearTexture;
        Texture2D checkedTexture;

        if (AllowChecking)
        {
            clearTexture = ClearTexture;
            checkedTexture = CheckedTexture;
        }
        else
        {
            clearTexture = DisabledClearTexture;
            checkedTexture = DisabledCheckedTexture;
        }

        int checkBoxYPosition = 0;
        int textYPosition = TextLocationY;

        if (TextLocationY < 0)
        {
            // If the text is higher than the checkbox texture (textLocationY < 0), 
            // let's draw the text at the top of the client
            // rectangle and the check-box in the middle of the text.
            // This is necessary for input to work properly.
            checkBoxYPosition -= TextLocationY;
            textYPosition = 0;
        }

        if (!string.IsNullOrEmpty(Text))
        {
            Color textColor;
            if (!AllowChecking)
                textColor = Color.Gray;
            else
                textColor = IsActive ? HighlightColor : IdleColor;

            DrawStringWithShadow(Text, FontIndex,
                new Vector2(checkedTexture.Width + TextPadding, textYPosition),
                textColor, 1.0f, UISettings.ActiveSettings.TextShadowDistance);
        }

        // Might not be worth it to save one draw-call per frame with a confusing
        // if-else routine, but oh well
        if (checkedAlpha == 0.0)
        {
            DrawTexture(clearTexture,
                new Rectangle(0, checkBoxYPosition,
                clearTexture.Width, clearTexture.Height), Color.White);
        }
        else if (checkedAlpha == 1.0)
        {
            DrawTexture(checkedTexture,
                new Rectangle(0, checkBoxYPosition,
                clearTexture.Width, clearTexture.Height),
                Color.White);
        }
        else
        {
            DrawTexture(clearTexture,
                new Rectangle(0, checkBoxYPosition,
                clearTexture.Width, clearTexture.Height), Color.White);

            DrawTexture(checkedTexture,
                new Rectangle(0, checkBoxYPosition,
                clearTexture.Width, clearTexture.Height),
                Color.White * (float)checkedAlpha);
        }

        base.Draw(gameTime);
    }
}

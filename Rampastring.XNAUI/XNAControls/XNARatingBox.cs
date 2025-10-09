using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using SharpDX.Direct2D1.Effects;

namespace Rampastring.XNAUI.XNAControls;

/// <summary>
/// A rating-box.
/// </summary>
public class XNARatingBox : XNAControl
{
    private const int TEXT_PADDING_DEFAULT = 5;

    private const int START_NUMBER = 5;

    /// <summary>
    /// Creates a new check box.
    /// </summary>
    /// <param name="windowManager">The window manager.</param>
    public XNARatingBox(WindowManager windowManager) : base(windowManager)
    {
        AlphaRate = UISettings.ActiveSettings.CheckBoxAlphaRate * 2.0;
        StarTextures = new List<Texture2D>();
    }

    public event EventHandler CheckedChanged;

    private List<Texture2D> StarTextures = null;


    /// <summary>
    /// The sound effect that is played when the check box is clicked on.
    /// </summary>
    public EnhancedSoundEffect CheckSoundEffect { get; set; }

    /// <summary>
    /// The sound effect that is played when the cursor enters the check box's area.
    /// </summary>
    public EnhancedSoundEffect HoverSoundEffect { get; set; }

    private int _checkedIndex = -1;

    /// <summary>
    /// Determines whether the check box is currently checked.
    /// </summary>
    public int CheckedIndex
    {
        get => _checkedIndex;
        set
        {
            int originalValue = _checkedIndex;
            _checkedIndex = value;
            if (_checkedIndex != originalValue || -1 == originalValue)
                ChangeTexture(value);
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
            //SetTextPositionAndSize();
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
        for (int i = 0; i < START_NUMBER; i++)
        {
            StarTextures.Add(UISettings.ActiveSettings.RatingBoxClearTexture);
        }

        SetTextPositionAndSize();

        if (-1 != CheckedIndex)
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
                bool bChked = Conversions.BooleanFromString(value, true);
                CheckedIndex = bChked ? 0 : -1;
                return;
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    /// <summary>
    /// Updates the size of the check box and the vertical position of its text.
    /// </summary>
    protected virtual void SetTextPositionAndSize()
    {
        if (StarTextures == null)
            return;

        if (!string.IsNullOrEmpty(Text))
        {
            Vector2 textDimensions = Renderer.GetTextDimensions(Text, FontIndex);
            
            TextLocationY = (int)(StarTextures[0].Height - textDimensions.Y) / 2 + 2;

            Width = (int)textDimensions.X + TEXT_PADDING_DEFAULT + StarTextures[0].Width * StarTextures.Count;
            Height = Math.Max((int)textDimensions.Y, StarTextures[0].Height);
        }
        else
        {
            Width = StarTextures[0].Width * StarTextures.Count;
            Height = StarTextures[0].Height;
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
            Rectangle rectangle = GetWindowRectangle();
            Point curPt = Cursor.Location;
            if (rectangle.Contains(curPt))
            {
                int nCurIndex = -1;
                int nRectLeft = rectangle.Left;
                for (int i = 0; i < StarTextures.Count; i++)
                {
                    rectangle.Width = StarTextures[i].Width;
                    rectangle.X = nRectLeft + i * StarTextures[i].Width;
                    if (rectangle.Contains(curPt))
                    {
                        nCurIndex = i;
                        break;
                    }
                }

                if (-1 != nCurIndex)
                {
                    CheckSoundEffect?.Play();
                    CheckedIndex = nCurIndex;
                    CheckedChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        
        base.OnLeftClick();
    }

    private void ChangeTexture(int nIndex)
    {
        if (-1 == nIndex)
        {
            for (int i = 0; i < StarTextures.Count; i++)
            {
                StarTextures[i] = UISettings.ActiveSettings.RatingBoxClearTexture;
            }
        }
        else
        {
            for (int i = 0; i < StarTextures.Count; i++)
            {
                if (i <= nIndex)
                    StarTextures[i] = UISettings.ActiveSettings.RatingBoxCheckedTexture;
                else
                    StarTextures[i] = UISettings.ActiveSettings.RatingBoxClearTexture;
            }
        }
       
        switch(nIndex)
        {
            case 0:
                Text = "很差";
                break;
            case 1:
                Text = "差";
                break;
            case 2:
                Text = "一般";
                break;
            case 3:
                Text = "好";
                break;
            case 4:
                Text = "很好";
                break;
            default:
                Text = "请打分";
                break;
        }
    }

    /// <summary>
    /// Updates the check box's alpha each frame.
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        double alphaRate = AlphaRate * (gameTime.ElapsedGameTime.TotalMilliseconds / 10.0);

        if (-1 != CheckedIndex)
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
                new Vector2(StarTextures[0].Width * StarTextures.Count + TextPadding, textYPosition),
                textColor, 1.0f, UISettings.ActiveSettings.TextShadowDistance);
        }

        // Might not be worth it to save one draw-call per frame with a confusing
        // if-else routine, but oh well
        if (checkedAlpha == 0.0)
        {
            for(int i = 0; i < StarTextures.Count; i++)
            {
                Texture2D texture = StarTextures[i];
                DrawTexture(texture,
                new Rectangle(0 + i * texture.Width, checkBoxYPosition,
                texture.Width, texture.Height), Color.White);
            }
        }
        else if (checkedAlpha == 1.0)
        {
            for (int i = 0; i < StarTextures.Count; i++)
            {
                Texture2D texture = StarTextures[i];
                DrawTexture(texture,
                new Rectangle(0 + i * texture.Width, checkBoxYPosition,
                texture.Width, texture.Height), Color.White);
            }
        }
        else
        {
            for (int i = 0; i < StarTextures.Count; i++)
            {
                Texture2D texture = StarTextures[i];
                DrawTexture(texture,
                new Rectangle(0 + i * texture.Width, checkBoxYPosition,
                texture.Width, texture.Height), Color.White * (float)checkedAlpha);
            }
        }

        base.Draw(gameTime);
    }
}

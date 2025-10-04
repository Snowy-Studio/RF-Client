using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Rampastring.XNAUI;

/// <summary>
/// A class that contains UI-related settings, like default UI colors.
/// </summary>
public class UISettings
{
    /// <summary>
    /// The currently active UI settings.
    /// </summary>
    public static UISettings ActiveSettings { get; set; }

    public float TextShadowDistance { get; set; } = 1.0f;
    public Color TextShadowColor { get; set; } = Color.Black;

    public Color TextColor { get; set; } = new Color(196, 196, 196);

    public Color SubtleTextColor { get; set; } = Color.Gray;

    public Color AltColor { get; set; } = Color.White;

    public Color ButtonTextColor { get; set; } = new Color(196, 196, 196);

    public Color DisabledItemColor { get; set; } = Color.Gray;

    public Color ButtonHoverColor { get; set; } = Color.White;

    public Color BackgroundColor { get; set; } = Color.Black;

    public Color FocusColor { get; set; } = new Color(64, 64, 64);

    public Color PanelBackgroundColor { get; set; } = new Color(32, 32, 32);

    public Color PanelBorderColor { get; set; } = new Color(196, 196, 196);

    public Texture2D CheckBoxCheckedTexture { get; set; }

    public Texture2D CheckBoxClearTexture { get; set; }

    public Texture2D CheckBoxDisabledCheckedTexture { get; set; }

    public Texture2D CheckBoxDisabledClearTexture { get; set; }
    public int? DropDownDefaultItemHeight { get; set; }
    public int? ListBoxDefaultItemHeight { get; set; }
    public int? ContextMenuDefaultItemHeight { get; set; }
    public int? TextBoxDefaultHeight { get; set; }

    public Texture2D RatingBoxCheckedTexture { get; set; }

    public Texture2D RatingBoxClearTexture { get; set; }

    public float DefaultAlphaRate = 0.005f;

    public float CheckBoxAlphaRate = 0.05f;

    public float IndicatorAlphaRate = 0.05f;
}

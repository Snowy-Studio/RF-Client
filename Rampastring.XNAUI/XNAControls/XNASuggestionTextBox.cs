using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Rampastring.Tools;

namespace Rampastring.XNAUI.XNAControls;

/// <summary>
/// A text box that displays a "suggestion" text when it's not active.
/// </summary>
public class XNASuggestionTextBox : XNATextBox
{
    public XNASuggestionTextBox(WindowManager windowManager) : base(windowManager)
    {
    }

    private string _suggestion = string.Empty;

    public string Suggestion
    {
        get => _suggestion;
        set
        {
            bool isTextDefault = Text == Suggestion;
            _suggestion = value;
            if (isTextDefault)
            {
                Text = _suggestion;
            }
        }
    }

    private Color? _suggestedTextColor;

    public Color SuggestedTextColor
    {
        get => _suggestedTextColor ?? UISettings.ActiveSettings.SubtleTextColor;
        set => _suggestedTextColor = value;
    }

    public override void Initialize()
    {
        base.Initialize();

        Text = Suggestion ?? string.Empty;
    }

    public override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        if (key == "Suggestion")
        {
            Suggestion = value;
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    public override Color TextColor
    {
        get => WindowManager.SelectedControl == this ? base.TextColor : SuggestedTextColor;
        set => base.TextColor = value;
    }

    public override void OnSelectedChanged()
    {
        base.OnSelectedChanged();

        if (WindowManager.SelectedControl == this)
        {
            if (Text == Suggestion)
                Text = string.Empty;
        }
        else
        {
            if (string.IsNullOrEmpty(Text))
                Text = Suggestion ?? string.Empty;
        }
    }
}

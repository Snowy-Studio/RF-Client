using System;
using Microsoft.Xna.Framework;

namespace Rampastring.XNAUI.XNAControls;

public class XNAProgressBar : XNAControl
{
    public XNAProgressBar(WindowManager windowManager) : base(windowManager)
    {
    }

    public int BorderWidth { get; set; } = 1;

    private Color? _borderColor;

    public Color BorderColor
    {
        get => _borderColor ?? UISettings.ActiveSettings.PanelBorderColor;
        set => _borderColor = value;
    }

    private Color? _filledColor;

    public Color FilledColor
    {
        get => _filledColor ?? UISettings.ActiveSettings.AltColor;
        set => _filledColor = value;
    }

    private Color? _unfilledColor;

    public Color UnfilledColor
    {
        get => _unfilledColor ?? new Color(FilledColor.R / 3, FilledColor.G / 3, FilledColor.B / 3, FilledColor.A);
        set => _unfilledColor = value;
    }

    public int Maximum { get; set; }

    public bool SmoothBackwardTransition { get; set; }

    public bool SmoothForwardTransition { get; set; }

    public int SmoothTransitionRate { get; set; } = 1;

    private int _value = 0;

    public int Value
    {
        get { return _value; }
        set
        {
            if (value > Maximum)
                _value = Maximum;
            else
                _value = value;
        }
    }

    private int _shownValue = 0;

    public override void Update(GameTime gameTime)
    {
        if (_shownValue < _value)
        {
            if (SmoothForwardTransition)
                _shownValue = Math.Min(_shownValue + SmoothTransitionRate, _value);
            else
                _shownValue = _value;
        }
        else if (_shownValue > _value)
        {
            if (SmoothBackwardTransition)
                _shownValue = Math.Max(0, _shownValue - SmoothTransitionRate);
            else
                _shownValue = _value;
        }
    }

    public override void Draw(GameTime gameTime)
    {
        Rectangle wrect = RenderRectangle();

        for (int i = 0; i < BorderWidth; i++)
        {
            var rect = new Rectangle(wrect.X + i, wrect.Y + i,
                wrect.Width - i, wrect.Height - i);

            Renderer.DrawRectangle(rect, BorderColor);
        }

        int filledWidth = (int)((_shownValue / (double)Maximum) * (Width - BorderWidth * 2));

        var filledRect = new Rectangle(wrect.X + BorderWidth, wrect.Y + BorderWidth,
            filledWidth, wrect.Height - BorderWidth * 2);

        Renderer.FillRectangle(filledRect, FilledColor);

        var unfilledRect = new Rectangle(wrect.X + BorderWidth + filledWidth, wrect.Y + BorderWidth,
            wrect.Width - filledWidth - BorderWidth * 2, wrect.Height - BorderWidth * 2);

        Renderer.FillRectangle(unfilledRect, UnfilledColor);

        base.Draw(gameTime);
    }
}

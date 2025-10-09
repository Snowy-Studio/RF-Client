using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using System;

namespace Rampastring.XNAUI.XNAControls;

public class XNATrackbar : XNAPanel
{
    public XNATrackbar(WindowManager windowManager) : base(windowManager)
    {
    }

    public event EventHandler ValueChanged;

    public int MinValue { get; set; }

    public int MaxValue { get; set; }

    private int value = 0;
    public int Value
    {
        get { return value; }
        set
        {
            int oldValue = this.value;

            if (value > MaxValue)
                this.value = MaxValue;
            else if (value < MinValue)
                this.value = MinValue;
            else
                this.value = value;

            if (oldValue != this.value)
                ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public EnhancedSoundEffect ClickSound { get; set; }

    public Texture2D ButtonTexture { get; set; }

    private bool isHeldDown = false;

    public override void Initialize()
    {
        base.Initialize();

        if (ButtonTexture == null)
            ButtonTexture = AssetLoader.LoadTexture("trackbarButton.png");

        if (Height == 0)
            Height = ButtonTexture.Height;
    }

    public override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "MinValue":
                MinValue = iniFile.GetIntValue(Name, "MinValue", 0);
                return;
            case "MaxValue":
                MaxValue = iniFile.GetIntValue(Name, "MaxValue", 10);
                return;
            case "Value":
                Value = iniFile.GetIntValue(Name, "Value", 0);
                return;
            case "ClickSound":
                ClickSound = new EnhancedSoundEffect(value);
                return;
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    /// <summary>
    /// Scrolls the scrollbar if the user presses the mouse left button
    /// while moving the cursor over the scrollbar.
    /// </summary>
    public override void OnMouseOnControl()
    {
        base.OnMouseOnControl();

        if (Cursor.LeftPressedDown)
        {
            isHeldDown = true;
            // It's fair to assume that dragged trackbars are selected
            WindowManager.SelectedControl = this;
        }
    }

    public override void OnLeftClick()
    {
        isHeldDown = true;
        Scroll();

        base.OnLeftClick();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (isHeldDown)
        {
            if (!Cursor.LeftDown)
            {
                isHeldDown = false;
                return;
            }

            Scroll();
        }
    }

    private void Scroll()
    {
        int xOffset = GetCursorPoint().X;

        int tabCount = MaxValue - MinValue + 1;

        double pixelsPerTab = Width / (double)tabCount;

        int currentTab = 0;

        for (int i = 0; i <= tabCount; i++)
        {
            if (i * pixelsPerTab < xOffset)
            {
                currentTab = i;
            }
            else
            {
                int newValue = currentTab + MinValue;

                if (Value != newValue)
                    ClickSound?.Play();

                Value = newValue;

                return;
            }
        }

        if (Value != MaxValue)
            ClickSound?.Play();

        Value = MaxValue;
    }

    public override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);

        int tabIndex = Value - MinValue;

        int tabCount = MaxValue - MinValue;

        double pixelsPerTab = (Width - ButtonTexture.Width) / (double)tabCount;

        double tabLocationX = tabIndex * pixelsPerTab;

        //if (tabIndex == 0)
        //    tabLocationX += ButtonTexture.Width / 2;
        //else if (tabIndex == tabCount)
        //    tabLocationX -= ButtonTexture.Width / 2;

        DrawTexture(ButtonTexture,
            new Rectangle((int)(tabLocationX), 0, ButtonTexture.Width, Height),
            Color.White);
    }
}

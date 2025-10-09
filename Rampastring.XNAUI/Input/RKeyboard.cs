using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rampastring.XNAUI.Input;

/// <summary>
/// A class for handling the keyboard.
/// </summary>
public class RKeyboard : GameComponent
{
    public RKeyboard(Game game)
        : base(game)
    {
        PressedKeys = new List<Keys>();
        KeyboardState = Keyboard.GetState();
    }

    public delegate void KeyPressedEventHandler(object sender, KeyPressEventArgs e);
    public event KeyPressedEventHandler OnKeyPressed;

    public KeyboardState KeyboardState;
    private Keys[] DownKeys = new Keys[0];

    public List<Keys> PressedKeys;

    public override void Update(GameTime gameTime)
    {
        KeyboardState = Keyboard.GetState();
        PressedKeys.Clear();

        foreach (Keys key in DownKeys)
        {
            if (key == Keys.None)
                continue; // Work-around a MonoGame bug in OGL mode

            if (KeyboardState.IsKeyUp(key))
            {
                DoKeyPress(key);
                PressedKeys.Add(key);
            }
        }

        DownKeys = KeyboardState.GetPressedKeys();
    }

    private void DoKeyPress(Keys key)
    {
        if (OnKeyPressed != null)
        {
            Delegate[] delegates = OnKeyPressed.GetInvocationList();
            var args = new KeyPressEventArgs(key);
            for (int i = 0; i < delegates.Length; i++)
            {
                delegates[i].DynamicInvoke(this, args);
                if (args.Handled)
                    return;
            }
        }
    }

    public bool IsKeyHeldDown(Keys key)
    {
        return DownKeys.Contains(key);
    }

    public bool IsCtrlHeldDown()
    {
        return IsKeyHeldDown(Keys.RightControl) || IsKeyHeldDown(Keys.LeftControl);
    }

    public bool IsShiftHeldDown()
    {
        return IsKeyHeldDown(Keys.RightShift) || IsKeyHeldDown(Keys.LeftShift);
    }

    public bool IsAltHeldDown()
    {
        return IsKeyHeldDown(Keys.RightAlt) || IsKeyHeldDown(Keys.LeftAlt);
    }
}

public class KeyPressEventArgs : EventArgs
{
    public KeyPressEventArgs(Keys key)
    {
        PressedKey = key;
    }

    public Keys PressedKey { get; set; }

    // If set, the key press event won't be forwarded on to following subscribers.
    public bool Handled { get; set; }
}

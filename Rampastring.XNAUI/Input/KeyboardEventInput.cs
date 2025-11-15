using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.Xna.Framework;

namespace Rampastring.XNAUI.Input
{
    /// <summary>
    /// Handles text input. XNA does not have a built-in system for text input.
    /// Note: MonoGame has a way of detecting text input,
    /// via the Game.Window.TextInput event:
    /// http://www.gamedev.net/topic/457783-xna-getting-text-from-keyboard/
    /// </summary>
    public static class KeyboardEventInput
    {
        public delegate void CharEnteredHandler(object sender, KeyboardEventArgs e);

        /// <summary>
        /// Event raised when a character has been entered.
        /// </summary>
        public static event CharEnteredHandler CharEntered;

        ///// <summary>
        ///// Event raised when a key has been pressed down. May fire multiple times due to keyboard repeat.
        ///// </summary>
        //public static event KeyEventHandler KeyDown;

        ///// <summary>
        ///// Event raised when a key has been released.
        ///// </summary>
        //public static event KeyEventHandler KeyUp;

        private static bool initialized;
        private static IntPtr prevWndProc;
        private static WNDPROC hookProcDelegate;

        // private static HIMC hIMC;

        /// <summary>
        /// Initialize the TextInput with the given GameWindow.
        /// </summary>
        /// <param name="window">The XNA window to which text input should be linked.</param>
#if NET5_0_OR_GREATER
        [System.Runtime.Versioning.SupportedOSPlatform("windows5.1.2600")]
#endif
        public static void Initialize(GameWindow window)
        {
            if (initialized)
                throw new InvalidOperationException("KeyboardEventInput.Initialize can only be called once!");

            hookProcDelegate = HookProc;

            if (Environment.Is64BitProcess)
            {
                nint result = PInvoke.SetWindowLongPtr((HWND)window.Handle, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(hookProcDelegate));

                if (result == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                prevWndProc = result;
            }
            else
            {
                var result = (IntPtr)PInvoke.SetWindowLong((HWND)window.Handle, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, (int)Marshal.GetFunctionPointerForDelegate(hookProcDelegate));

                if (result == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                prevWndProc = result;
            }

            // hIMC = PInvoke.ImmGetContext((HWND)window.Handle);

            initialized = true;
        }

        private static LRESULT HookProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
        {
            Delegate xnaDelegate = Marshal.GetDelegateForFunctionPointer(prevWndProc, typeof(Action));
            var wndProcDelegate = (WndProcDelegate)Delegate.CreateDelegate(typeof(WndProcDelegate), xnaDelegate.Target, xnaDelegate.Method, false);
            var returnCode = (LRESULT)PInvoke.CallWindowProc(wndProcDelegate, hWnd, msg, (nint)(nuint)wParam, lParam);

            switch (msg)
            {
                case PInvoke.WM_GETDLGCODE:
                    returnCode = (LRESULT)((nint)returnCode | (nint)PInvoke.DLGC_WANTALLKEYS);
                    break;

                //case WM_KEYDOWN:
                //    if (KeyDown != null)
                //        KeyDown(null, new KeyEventArgs((Keys)wParam));
                //    break;

                //case WM_KEYUP:
                //    if (KeyUp != null)
                //        KeyUp(null, new KeyEventArgs((Keys)wParam));
                //    break;

                case PInvoke.WM_CHAR:
                    CharEntered?.Invoke(null, new KeyboardEventArgs((char)wParam.Value, (int)lParam.Value));
                    break;

                // case PInvoke.WM_IME_SETCONTEXT:
                //     if (wParam == 1)
                //         PInvoke.ImmAssociateContext(hWnd, hIMC);
                //     break;

                // case PInvoke.WM_INPUTLANGCHANGE:
                //     PInvoke.ImmAssociateContext(hWnd, hIMC);
                //     returnCode = (LRESULT)1;
                //     break;
            }

            return returnCode;
        }
    }
}
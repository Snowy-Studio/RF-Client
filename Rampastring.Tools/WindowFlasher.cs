using System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Rampastring.Tools;

public static class WindowFlasher
{
    /// <summary>
    /// Flashes a form's window in the taskbar.
    /// </summary>
    /// <param name="windowHandle">The handle of the window to flash.</param>
    /// <returns>The return value fo FlashWindowEx.</returns>
#if NET5_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatform("windows5.1.2600")]
#endif
    public static bool FlashWindowEx(IntPtr windowHandle)
    {
        int cbSize;

        unsafe
        {
            cbSize = sizeof(FLASHWINFO);
        }

        var pfwi = new FLASHWINFO
        {
            cbSize = (uint)cbSize,
            hwnd = (HWND)windowHandle,
            dwFlags = FLASHWINFO_FLAGS.FLASHW_ALL | FLASHWINFO_FLAGS.FLASHW_TIMERNOFG,
            uCount = uint.MaxValue,
            dwTimeout = 0u
        };

        BOOL result = PInvoke.FlashWindowEx(pfwi);

        return result.Value != 0;
    }
}
using ClientCore;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace ClientGUI
{
    public sealed class ShiftClickAutoClicker
    {
        private static readonly Lazy<ShiftClickAutoClicker> _instance = new(() => new ShiftClickAutoClicker());
        public static ShiftClickAutoClicker Instance => _instance.Value;

        private Thread _monitorThread;
        private bool _monitoring = false;
        private bool _wasLeftDown = false;

        private const int VK_SHIFT = 0x10;
        private const int VK_LBUTTON = 0x01;

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        private ShiftClickAutoClicker() { }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        private readonly int[] _blockedKeys =
        {
            0x5A, // Z键
            0x11, // Ctrl键
            0x12, // Alt键
        };

        /// <summary>
        /// 启动监听
        /// </summary>
        public void Start()
        {
            if (_monitoring) return;

            _monitoring = true;
            _monitorThread = new Thread(MonitorLoop)
            {
                IsBackground = true
            };
            _monitorThread.Start();

            Console.WriteLine("ShiftClickAutoClicker 已开始监听...");
        }

        /// <summary>
        /// 停止监听
        /// </summary>
        public void Stop()
        {
            _monitoring = false;
            _monitorThread?.Join();
            Console.WriteLine("ShiftClickAutoClicker 已停止监听。");
        }

        private bool IsBlockedKeyDown()
        {
            foreach (var key in _blockedKeys)
            {
                if ((GetAsyncKeyState(key) & 0x8000) != 0)
                {
                    return true;
                }
            }
            return false;
        }

        private void MonitorLoop()
        {
            while (_monitoring)
            {
                bool shiftDown = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;
                bool leftDown = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;

                if (shiftDown && leftDown && !_wasLeftDown && !IsBlockedKeyDown())
                {
                    if (IsInBuildBarArea()) // 只在建造栏区域触发
                    {
                        AutoClick(UserINISettings.Instance.连点数量 - 1);
                    }
                }

                _wasLeftDown = leftDown;
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// 判断当前鼠标是否位于红警2（gamemd.exe）窗口的建造栏区域
        /// 这里使用 buildBarWidth = width/11 + width/33 的计算（即 1/11 + 1/33）
        /// </summary>
        private bool IsInBuildBarArea()
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return false;

            GetWindowThreadProcessId(hwnd, out uint pid);
            try
            {
                Process proc = Process.GetProcessById((int)pid);
                if (!proc.ProcessName.Equals("gamemd-spawn", StringComparison.OrdinalIgnoreCase))
                    return false; // 前台不是红警2
            }
            catch
            {
                return false;
            }

            if (!GetWindowRect(hwnd, out RECT rect)) return false;
            if (!GetCursorPos(out POINT cursor)) return false;

            int windowWidth = rect.Right - rect.Left;

            // 使用整数像素计算建造栏宽度：width/11 + width/33 = (3/33 + 1/33) = 4/33
            int buildBarWidth = 160;
            int buildBarLeft = rect.Right - buildBarWidth;

            bool inBuildBar = cursor.X >= buildBarLeft &&
                              cursor.X <= rect.Right &&
                              cursor.Y >= rect.Top &&
                              cursor.Y <= rect.Bottom;

            return inBuildBar;
        }

        private void AutoClick(int count)
        {
            for (int i = 0; i < count; i++)
            {
                mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                Thread.Sleep(50);
            }
        }
    }
}

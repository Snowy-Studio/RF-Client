using Rampastring.XNAUI;

namespace ClientGUI
{
    public class XNAWindowSingleton(WindowManager windowManager) : XNAWindow(windowManager)
    {
        private static XNAWindowSingleton Instance = null;

        public static XNAWindowSingleton GetInstance(WindowManager windowManager)
        {
            Instance ??= new XNAWindowSingleton(windowManager);
            return Instance;
        }


    }
}

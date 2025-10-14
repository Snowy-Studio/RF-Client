using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

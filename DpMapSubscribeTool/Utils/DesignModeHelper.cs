using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DpMapSubscribeTool.Utils
{
    internal static class DesignModeHelper
    {
        public static bool IsDesignMode => Avalonia.Controls.Design.IsDesignMode;

        public static void CheckOnlyForDesignMode()
        {
            if (!IsDesignMode)
                throw new Exception("Only use in DesignMode.");
        }
    }
}

using System;
using System.Diagnostics;
using Avalonia.Controls;

namespace DpMapSubscribeTool.Utils;

public static class DesignModeHelper
{
    public static bool IsDesignMode => Design.IsDesignMode;

    public static void CheckOnlyForDesignMode()
    {
        if (!IsDesignMode)
            throw new Exception("Only use in DesignMode.");
    }
}
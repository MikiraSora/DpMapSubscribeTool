using System;
using Avalonia.Controls;

namespace DpMapSubscribeTool.Utils;

public static class DesignModeHelper
{
    public static bool IsDesignMode => Design.IsDesignMode;

    /// <summary>
    ///     it will throw exception if current env is NOT in design mode
    /// </summary>
    /// <exception cref="Exception"></exception>
    public static void CheckOnlyForDesignMode()
    {
        if (!IsDesignMode)
            throw new Exception("Only use in DesignMode.");
    }
}
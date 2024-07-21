using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace DpMapSubscribeTool;

public static class LoggerEx
{
    public static void LogInformationEx(this ILogger logger, string content,
        [CallerMemberName] string callerName = default)
    {
#if DEBUG
        if (callerName is null)
            return;
#endif
        var newContent = $"{callerName}() {content}";
        logger.LogInformation(newContent);
    }

    public static void LogDebugEx(this ILogger logger, string content,
        [CallerMemberName] string callerName = default)
    {
#if DEBUG
        if (callerName is null)
            return;
#endif
        var newContent = $"{callerName}() {content}";
        logger.LogDebug(newContent);
    }

    public static void LogWarningEx(this ILogger logger, string content,
        [CallerMemberName] string callerName = default)
    {
#if DEBUG
        if (callerName is null)
            return;
#endif
        var newContent = $"{callerName}() {content}";
        logger.LogWarning(newContent);
    }

    public static void LogTraceEx(this ILogger logger, string content,
        [CallerMemberName] string callerName = default)
    {
#if DEBUG
        if (callerName is null)
            return;
#endif
        var newContent = $"{callerName}() {content}";
        logger.LogTrace(newContent);
    }

    public static void LogCriticalEx(this ILogger logger, string content,
        [CallerMemberName] string callerName = default)
    {
#if DEBUG
        if (callerName is null)
            return;
#endif
        var newContent = $"{callerName}() {content}";
        logger.LogCritical(newContent);
    }

    public static void LogErrorEx(this ILogger logger, string content,
        [CallerMemberName] string callerName = default)
    {
#if DEBUG
        if (callerName is null)
            return;
#endif
        var newContent = $"{callerName}() {content}";
        logger.LogError(newContent);
    }

    public static void LogErrorEx(this ILogger logger, Exception e, string content,
        [CallerMemberName] string callerName = default)
    {
#if DEBUG
        if (callerName is null)
            return;
#endif
        var newContent = $"{callerName}() {content}";
        logger.LogError(e, newContent);
    }
}
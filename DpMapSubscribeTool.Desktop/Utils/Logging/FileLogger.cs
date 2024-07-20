using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace DpMapSubscribeTool.Desktop.Utils.Logging;

public class FileLogger : ILogger
{
    private readonly object _lock = new();
    private readonly string categoryName;
    private readonly string[] filePathList;
    private readonly string simpliedCategoryName;
    private readonly DateTime startTime;

    public FileLogger(string categoryName, string[] filePathList, DateTime startTime)
    {
        this.categoryName = categoryName;
        this.filePathList = filePathList;
        this.startTime = startTime;
        simpliedCategoryName = this.categoryName.Split(".").LastOrDefault();
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
        Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var now = DateTime.Now;

        var levelStr = logLevel switch
        {
            LogLevel.Information => "Info",
            _ => logLevel.ToString()
        };

        var overDays = (int) (now - startTime).TotalDays;
        var overDaysStr = overDays > 0 ? $"+{overDays}d " : string.Empty;
        var eventIdStr = eventId == 0 ? string.Empty : eventId.ToString();
        var threadId = Thread.CurrentThread.ManagedThreadId;
        var threadIdStr = threadId switch
        {
            1 => string.Empty,
            _ => threadId.ToString()
        };

        var logRecord =
            $"{overDaysStr}{now:HH:mm:ss.fff} {levelStr}:{eventIdStr}:{threadIdStr} [{simpliedCategoryName}] {formatter(state, exception)}{Environment.NewLine}";

        lock (_lock)
        {
            foreach (var filePath in filePathList)
                File.AppendAllText(filePath, logRecord);
        }
    }
}
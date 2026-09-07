using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public enum LogLevel
{
    Info,
    Warning,
    Error,
}

static class Logger
{
    public static event Action<string, LogLevel> OnLog;

    public static void Log(Object value)
    {
        string msg = value?.ToString() ?? "";
        Console.WriteLine(msg);
        OnLog?.Invoke(msg, LogLevel.Info);
    }

    public static void Error(Object value)
    {
        string msg = value?.ToString() ?? "";
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(msg);
        Console.ForegroundColor = ConsoleColor.White;
        OnLog?.Invoke(msg, LogLevel.Error);
    }

    public static void Warning(Object value)
    {
        string msg = value?.ToString() ?? "";
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(msg);
        Console.ForegroundColor = ConsoleColor.White;
        OnLog?.Invoke(msg, LogLevel.Warning);
    }

    public static void LogCls(Object value)
    {
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(value.ToString() + " ", Console.BufferWidth);
    }
}

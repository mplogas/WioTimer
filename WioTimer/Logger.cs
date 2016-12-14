using System;

namespace WioTimer
{
    public class Logger
    {
        public Logger(){}

        public static void Write(Severity severity, string message)
        {
            Console.WriteLine($"{DateTime.Now} - {Enum.GetName(typeof(Severity), severity)}: {message}");
        }
    }

    public enum Severity
    {
        Info,
        Warning,
        Error
    }
}
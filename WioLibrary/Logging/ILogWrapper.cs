using System;

namespace WioLibrary.Logging
{
    public interface ILogWrapper
    {
        void Write(LogSeverity severity, string message);
        void Write(LogSeverity severity, Exception exception);
    }

    public enum LogSeverity
    {
        Debug,
        Warn,
        Error
    }
}
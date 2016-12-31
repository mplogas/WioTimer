using System;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace WioLibrary.Logging
{
    public class LogWrapper : ILogWrapper
    {
        private Logger logger;

        public LogWrapper(string filePath)
        {
            var config = new LoggingConfiguration();

            var consoleTarget = new ColoredConsoleTarget {Layout = @"${date:format=HH\:mm\:ss} ${logger} ${message}"};
            config.AddTarget("console", consoleTarget);
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, consoleTarget));

            if (!string.IsNullOrEmpty(filePath))
            {
                var fileTarget = new FileTarget
                {
                    FileName = filePath,
                    Layout = @"${date:format=HH\:mm\:ss} ${logger} ${message}"
                };

                config.AddTarget("file", fileTarget);
                config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, fileTarget));
            }

            LogManager.Configuration = config;
            logger = LogManager.GetLogger("WioLibrary");
        }

        public LogWrapper():this(string.Empty) { }

        public void Write(LogSeverity severity, string message)
        {
            if (logger == null)
                throw new InvalidOperationException("Logger not initialized yet. Call Initialize() first!");

            switch (severity)
            {
                case LogSeverity.Warn:
                    logger.Warn(message);
                    break;
                case LogSeverity.Error:
                    logger.Error(message);
                    break;
                default:
                    logger.Debug(message);
                    break;
            }
        }

        public void Write(LogSeverity severity, Exception exception)
        {
            if (logger == null)
                throw new InvalidOperationException("Logger not initialized yet. Call Initialize() first!");

            switch (severity)
            {
                case LogSeverity.Warn:
                    logger.Warn(exception);
                    break;
                case LogSeverity.Error:
                    logger.Error(exception);
                    break;
                default:
                    logger.Debug(exception);
                    break;
            }
        }
    }

    
}
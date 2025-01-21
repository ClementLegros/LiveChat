using NLog;
using NLog.Config;
using System;
using System.Diagnostics;
using System.IO;
using System.Excepts;
using System.Runtime.CompilerServices;

namespace CalSup.Utilities
{
    public static class Logger
    {
        // A Logger dispenser for the current assembly (Remember to call Flush on application exit)
        public static LogFactory Instance => _instance.Value;

        private static Lazy<LogFactory> _instance = new Lazy<LogFactory>(BuildLogFactory);

        public static ILogger logger = Instance.GetCurrentClassLogger();

        // https://stackoverflow.com/questions/1348643/how-performant-is-stackframe/1348853#answer-28015917
        public static void Debug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = null)
        {
#if DEBUG
            logger.Debug($"{GetDefaultLogInfo(memberName, filePath)} {message}");
#endif
        }

        public static void Enter([CallerMemberName] string memberName = "", [CallerFilePath] string filePath = null)
        {
            logger.Info($"{GetDefaultLogInfo(memberName, filePath)} {"Enter"}");
        }

        public static void Enter(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = null)
        {
            logger.Info($"{GetDefaultLogInfo(memberName, filePath)} Enter {message}");
        }

        public static void Leave([CallerMemberName] string memberName = "", [CallerFilePath] string filePath = null)
        {
            logger.Info($"{GetDefaultLogInfo(memberName, filePath)} {"Leave"}");
        }

        public static void Leave(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = null)
        {
            logger.Info($"{GetDefaultLogInfo(memberName, filePath)} Leave {message}");
        }

        public static void Error(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = null)
        {
            logger.Error($"{GetDefaultLogInfo(memberName, filePath)} {message}");
        }

        public static void Error(string message, Exception ex, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = null)
        {
            logger.Error(ex, $"{GetDefaultLogInfo(memberName, filePath)} {message}");
        }

#if DEBUG
        public static void StackTrace() => new StackTrace(true)
                ?.GetFrames()
                .ForEachTry(f => f.HasSource())
                .ForEachTry(f => logger.Error($"    {f?.GetMethod()?.ToString()} {f?.GetFileName()?.ToString()} {f?.GetFileLineNumber()}"));
#else
        public static void StackTrace() => new StackTrace(true) { }
#endif

        public static void Info(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = null)
        {
            logger.Info($"{GetDefaultLogInfo(memberName, filePath)} {message}");
        }

        public static void Warning(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = null)
        {
            logger.Warn($"{GetDefaultLogInfo(memberName, filePath)} {message}");
        }

        private static string GetDefaultLogInfo(string memberName, string filePath)
        {
            return $"{Environment.UserName} {Path.GetFileNameWithoutExtension(filePath)} {memberName}";
        }

        private static LogFactory BuildLogFactory()
        {
            // look up how to find app name
            string appName = AppEnvironment.AppName;

            string configFilePath = Path.ChangeExtension(appName, ".nlog");

            // It will create an empty file, logging won't work. Should we put a default logger in the file ?
            if (!File.Exists(configFilePath))
            {
                File.Create(configFilePath).Close();
            }

            LogFactory logFactory = new LogFactory();

            logFactory.Configuration = new XmlLoggingConfiguration(configFilePath, true, logFactory);

            return logFactory;
        }
    }
}

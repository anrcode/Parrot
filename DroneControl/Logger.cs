using System;
using System.Collections.Generic;
using System.Text;

namespace Parrot.DroneControl
{
    public enum LogScope
    {
        Info,
        Debug,
        Warning,
        Error
    }

    public abstract class Logger
    {
        private static StringBuilder _log = new StringBuilder();

        public static void Log(LogScope scope, string message)
        {
            //if (scope != LogScope.Debug)
            //{
                Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " " + scope.ToString() + ": " + message);
                _log.AppendLine(DateTime.Now.ToString("HH:mm:ss.fff") + " " + scope.ToString() + ": " + message);
            //}
        }

        public static void Log(LogScope scope, Exception ex)
        {
            //if (scope != LogScope.Debug)
            //{
                Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " " + scope.ToString() + ": " + ex.Message);
                _log.AppendLine(DateTime.Now.ToString("HH:mm:ss.fff") + " " + scope.ToString() + ": " + ex.Message);
            //}
        }

        public static string RetrieveLogContents()
        {
            return _log.ToString();
        }
    }
}

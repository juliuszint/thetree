using System;

namespace derbaum
{
    public enum LogLevel
    {
        Warning,
        Error,
        Info
    }

    public static class BaumEnvironment
    {
        public static void Log(LogLevel level, string message)
        {
            var levelString = level.ToString().PadRight(8);
            Console.WriteLine($"{level}: {message}");
        }
    }
}

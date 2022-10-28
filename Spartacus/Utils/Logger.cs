using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Spartacus
{
    class Logger
    {
        public static bool IsVerbose = false;

        public static bool IsDebug = false;

        public static string ConsoleLogFile = "";

        public static void Verbose(string message, bool newLine = true, bool showTime = true)
        {
            if (!IsVerbose)
            {
                return;
            }

            Write(message, newLine, showTime);
        }

        public static void Debug(string message, bool newLine = true, bool showTime = true)
        {
            if (!IsDebug)
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.Blue;
            Write("[DEBUG] " + message, newLine, showTime);
            Console.ResetColor();
        }

        public static void Error(string message, bool newLine = true, bool showTime = true)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Write(message, newLine, showTime);
            Console.ResetColor();
        }

        public static void Info(string message, bool newLine = true, bool showTime = true)
        {
            Write(message, newLine, showTime);
        }

        public static void Warning(string message, bool newLine = true, bool showTime = true)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Write(message, newLine, showTime);
            Console.ResetColor();
        }

        public static void Success(string message, bool newLine = true, bool showTime = true)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Write(message, newLine, showTime);
            Console.ResetColor();
        }

        protected static string FormatString(string message)
        {
            return $"[{DateTime.Now:HH:mm:ss}] {message}";
        }

        protected static void Write(string message, bool newLine = true, bool showTime = true)
        {
            message = showTime ? FormatString(message) : message;
            message += newLine ? Environment.NewLine : "";
            Console.Write(message);
            WriteToLogFile(message);
        }

        protected static void WriteToLogFile(string message)
        {
            // Write to file too.
            if (ConsoleLogFile == "")
            {
                ConsoleLogFile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\spartacus.log";
                if (!File.Exists(ConsoleLogFile))
                {
                    File.Create(ConsoleLogFile).Dispose();
                }
            }

            using (StreamWriter w = File.AppendText(ConsoleLogFile))
            {
                w.Write(message);
            }
        }
    }
}

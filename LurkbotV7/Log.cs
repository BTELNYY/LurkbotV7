using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LurkbotV7
{
    public class Log
    {
        public static void Message(LogLevel level, string message)
        {
#if !DEBUG
            if (level == LogLevel.Debug)
            {
                return;
            }
#endif
            switch (level)
            {
                case LogLevel.Critical:
                    Critical(message);
                    break;
                case LogLevel.Error:
                    Error(message);
                    break;
                case LogLevel.Warning:
                    Warning(message);
                    break;
                case LogLevel.Debug:
                    Debug(message);
                    break;
                case LogLevel.Verbose:
                    Verbose(message);
                    break;
            }
        }

        public static void WriteLineColor(string msg, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        public static void Success(string msg)
        {
            if (!Program.Config.EnableLogging)
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            string date = DateTime.Now.ToString("dd-MM-yyyy");
            string time = DateTime.Now.ToString("hh\\:mm\\:ss");
            string file = Path.Combine(Program.Config.LogPath, date + ".log");
            if (Program.Config.ShowLogsInConsole)
            {
                Console.WriteLine("[" + time + $" SUCCESS]: " + msg);
            }
            StreamWriter sw = new StreamWriter(file, append: true);
            sw.Write("[" + time + $" SUCCESS]: " + msg + "\n");
            sw.Close();
            Console.ResetColor();
        }

        public static void Error(string msg)
        {
            if (!Program.Config.EnableLogging)
            {
                return;
            }
            StackTrace trace = new StackTrace();
            Console.ForegroundColor = ConsoleColor.Red;
            string date = DateTime.Now.ToString("dd-MM-yyyy");
            string time = DateTime.Now.ToString("hh\\:mm\\:ss");
            string file = Path.Combine(Program.Config.LogPath, date + ".log");
            if (Program.Config.ShowLogsInConsole)
            {
                Console.WriteLine($"[" + time + $" ERROR]: " + msg);
            }
            StreamWriter sw = new StreamWriter(file, append: true);
            sw.Write("[" + time + $" ERROR]: " + msg + "\n");
            sw.Close();
            Console.ResetColor();
            Debug("Error stack trace: \n" + trace.ToString());
        }

        public static void Fatal(string msg)
        {
            if (!Program.Config.EnableLogging)
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.Red;
            string date = DateTime.Now.ToString("dd-MM-yyyy");
            string time = DateTime.Now.ToString("hh\\:mm\\:ss");
            string file = Path.Combine(Program.Config.LogPath, date + ".log");
            if (Program.Config.ShowLogsInConsole)
            {
                Console.WriteLine("[" + time + $" FATAL ERROR]: " + msg);
            }
            StreamWriter sw = new StreamWriter(file, append: true);
            sw.Write("[" + time + $" FATAL ERROR]: " + msg + "\n");
            sw.Close();
            Console.ResetColor();
        }

        public static void Warning(string msg)
        {
            if (!Program.Config.EnableLogging)
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            string date = DateTime.Now.ToString("dd-MM-yyyy");
            string time = DateTime.Now.ToString("hh\\:mm\\:ss");
            string file = Path.Combine(Program.Config.LogPath, date + ".log");
            if (Program.Config.ShowLogsInConsole)
            {
                Console.WriteLine("[" + time + $" WARNING]: " + msg);
            }
            StreamWriter sw = new StreamWriter(file, append: true);
            sw.Write("[" + time + $" WARNING]: " + msg + "\n");
            sw.Close();
            Console.ResetColor();
        }

        public static void Info(string msg)
        {
            if (!Program.Config.EnableLogging)
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.White;
            string date = DateTime.Now.ToString("dd-MM-yyyy");
            string time = DateTime.Now.ToString("hh\\:mm\\:ss");
            string file = Path.Combine(Program.Config.LogPath, date + ".log");
            if (Program.Config.ShowLogsInConsole)
            {
                Console.WriteLine("[" + time + $" INFO]: " + msg);
            }
            StreamWriter sw = new StreamWriter(file, append: true);
            sw.Write("[" + time + $" INFO]: " + msg + "\n");
            sw.Close();
            Console.ResetColor();
        }

        public static void Debug(string msg)
        {
#if !DEBUG
return;
#endif
            if (!Program.Config.EnableLogging)
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.Gray;
            string date = DateTime.Now.ToString("dd-MM-yyyy");
            string time = DateTime.Now.ToString("hh\\:mm\\:ss");
            string file = Path.Combine(Program.Config.LogPath, date + ".log");
            if (Program.Config.ShowLogsInConsole)
            {
                Console.WriteLine("[" + time + $" DEBUG]: " + msg);
            }
            StreamWriter sw = new StreamWriter(file, append: true);
            sw.Write("[" + time + $" DEBUG]: " + msg + "\n");
            sw.Close();
            Console.ResetColor();
        }

        public static void Verbose(string msg)
        {
            if (!Program.Config.EnableLogging)
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.Gray;
            string date = DateTime.Now.ToString("dd-MM-yyyy");
            string time = DateTime.Now.ToString("hh\\:mm\\:ss");
            string file = Path.Combine(Program.Config.LogPath, date + ".log");
            if (Program.Config.ShowLogsInConsole)
            {
                Console.WriteLine("[" + time + $" VERBOSE]: " + msg);
            }
            StreamWriter sw = new StreamWriter(file, append: true);
            sw.Write("[" + time + $" VERBOSE]: " + msg + "\n");
            sw.Close();
            Console.ResetColor();
        }

        public static void Critical(string msg)
        {
            if (!Program.Config.EnableLogging)
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            string date = DateTime.Now.ToString("dd-MM-yyyy");
            string time = DateTime.Now.ToString("hh\\:mm\\:ss");
            string file = Path.Combine(Program.Config.LogPath, date + ".log");
            if (Program.Config.ShowLogsInConsole)
            {
                Console.WriteLine("[" + time + $" CRITICAL]: " + msg);
            }
            StreamWriter sw = new StreamWriter(file, append: true);
            sw.Write("[" + time + $" CRITICAL]: " + msg + "\n");
            sw.Close();
            Console.ResetColor();
        }
    }

    public enum LogLevel
    {
        Critical,
        Error,
        Warning,
        Info,
        Verbose,
        Debug,
    }
}

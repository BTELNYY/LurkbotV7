using System.Diagnostics;

namespace LurkbotV7
{
    public class Log
    {
        public static object streamLock = new();

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
                case LogLevel.Success:
                    Success(message);
                    break;
                case LogLevel.Info:
                    Info(message);
                    break;
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
            string message = "[" + time + $" SUCCESS]: " + msg;
            if (Program.Config.ShowLogsInConsole)
            {
                Console.WriteLine(message);
            }
            _ = Task.Run(async () =>
            {
                StreamWriter sw = new(file, append: true);
                await sw.WriteLineAsync(message);
                sw.Close();
            });
            Console.ResetColor();
        }

        public static void Error(string msg)
        {
            if (!Program.Config.EnableLogging)
            {
                return;
            }
            StackTrace trace = new();
            Console.ForegroundColor = ConsoleColor.Red;
            string date = DateTime.Now.ToString("dd-MM-yyyy");
            string time = DateTime.Now.ToString("hh\\:mm\\:ss");
            string file = Path.Combine(Program.Config.LogPath, date + ".log");
            string message = $"[" + time + $" ERROR]: " + msg;
            if (Program.Config.ShowLogsInConsole)
            {
                Console.WriteLine(message);
            }
            _ = Task.Run(async () =>
            {
                StreamWriter sw = new(file, append: true);
                await sw.WriteLineAsync(message);
                sw.Close();
            });
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
            string message = "[" + time + $" FATAL ERROR]: " + msg;
            if (Program.Config.ShowLogsInConsole)
            {
                Console.WriteLine(message);
            }
            _ = Task.Run(async () =>
            {
                StreamWriter sw = new(file, append: true);
                await sw.WriteLineAsync(message);
                sw.Close();
            });
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
            string message = "[" + time + $" WARNING]: " + msg;
            if (Program.Config.ShowLogsInConsole)
            {
                Console.WriteLine(message);
            }
            _ = Task.Run(async () =>
            {
                StreamWriter sw = new(file, append: true);
                await sw.WriteLineAsync(message);
                sw.Close();
            });
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
            string message = "[" + time + $" INFO]: " + msg;
            if (Program.Config.ShowLogsInConsole)
            {
                Console.WriteLine(message);
            }
            _ = Task.Run(async () =>
            {
                StreamWriter sw = new(file, append: true);
                await sw.WriteLineAsync(message);
                sw.Close();
            });
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
            string message = "[" + time + $" DEBUG]: " + msg;
            if (Program.Config.ShowLogsInConsole)
            {
                Console.WriteLine(message);
            }
            _ = Task.Run(async () =>
            {
                StreamWriter sw = new(file, append: true);
                await sw.WriteLineAsync(message);
                sw.Close();
            });
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
            string message = "[" + time + $" VERBOSE]: " + msg;
            if (Program.Config.ShowLogsInConsole)
            {
                Console.WriteLine(message);
            }
            _ = Task.Run(async () =>
            {
                StreamWriter sw = new(file, append: true);
                await sw.WriteLineAsync(message);
                sw.Close();
            });
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
            string message = "[" + time + $" CRITICAL]: " + msg;
            if (Program.Config.ShowLogsInConsole)
            {
                Console.WriteLine(message);
            }
            _ = Task.Run(async () =>
            {
                StreamWriter sw = new(file, append: true);
                await sw.WriteLineAsync(message);
                sw.Close();
            });
            Console.ResetColor();
        }
    }

    public enum LogLevel
    {
        Critical = 0,
        Error = 1,
        Warning = 2,
        Info = 3,
        Verbose = 4,
        Debug = 5,
        Success = 6,
    }
}

using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace AntennaCalibrator.Utilis
{
    internal class SetupLogging
    {
        private static readonly LoggingLevelSwitch _levelSwitch = new LoggingLevelSwitch(LogEventLevel.Verbose);

        private static readonly AnsiConsoleTheme _theme = new AnsiConsoleTheme(new Dictionary<ConsoleThemeStyle, string>
        {
            [ConsoleThemeStyle.Text] = "\u001b[37m",        // Bianco
            [ConsoleThemeStyle.SecondaryText] = "\u001b[90m", // Grigio
            [ConsoleThemeStyle.LevelDebug] = "\u001b[34m", // Blu
            [ConsoleThemeStyle.LevelVerbose] = "\u001b[36m",  // Azzurro
            [ConsoleThemeStyle.LevelInformation] = "\u001b[32m", // Verde
            [ConsoleThemeStyle.LevelWarning] = "\u001b[33m", // Giallo
            [ConsoleThemeStyle.LevelError] = "\u001b[31m", // Rosso
            [ConsoleThemeStyle.LevelFatal] = "\u001b[35m", // Magenta
        });

        private static readonly string _outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} ({Level:u4}) {ThreadName}] > {Message}{NewLine}{Exception}";

        public static ILogger CreateThreadLogger(string logFilePath, string threadName)
        {
            logFilePath = Path.Combine(logFilePath, "logs");

            return new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.WithProperty("ThreadName", threadName)
                .WriteTo.Console(levelSwitch: _levelSwitch,
                    outputTemplate: _outputTemplate,
                    theme: _theme)
                .WriteTo.File(Path.Combine(logFilePath, threadName, $"{threadName}-.log"),
                    restrictedToMinimumLevel: LogEventLevel.Verbose,
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: _outputTemplate)
                .CreateLogger();
        }

        public static void SetLogLevel(LogEventLevel level)
        {
            _levelSwitch.MinimumLevel = level;
        }
    }
}

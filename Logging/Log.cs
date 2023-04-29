using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CSUtil.Logging
{
    public static class Log
    {
        static bool showDebugConsole = false;
        static bool showConsle = false;
        static Log()
        {
#if DEBUG
            showDebugConsole = true;
#else
            showConsole = true;
#endif
        }

        public static void ConfigureLogs(bool showDebug, bool showConsole)
        {
            Log.showDebugConsole = showDebug;
            Log.showConsle = showConsole;
        }

        public static void Normal(object data, bool newLine = true, ConsoleColor color = ConsoleColor.White, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = -1)
        {
            string str = $"{filePath}:{lineNumber} |{callerName}| {data}";
            if (newLine)
                str += "\n";

            if(showDebugConsole)
            {
                System.Diagnostics.Debug.Write(str);
            }
            
            if(showConsle)
            {
                var bClr = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(str);
                Console.ForegroundColor = bClr;
            }
        }

        public static void Warning(object data, bool newLine = true, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = -1)
        {
            Normal(data, newLine, ConsoleColor.Yellow, callerName, filePath, lineNumber);
        }

        public static void Error(object data, bool newLine = true, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = -1)
        {
            Normal(data, newLine, ConsoleColor.Red, callerName, filePath, lineNumber);
        }

        public static void FatalError(object data, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = -1)
        {
            Normal("[FATAL ERROR] " + data, true, ConsoleColor.DarkRed, callerName, filePath, lineNumber);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CSUtil.Logging
{
    public static class Log
    {
        public static void Normal(object data, bool newLine = true, ConsoleColor color = ConsoleColor.White, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = -1)
        {
            string str = $"{filePath}:{lineNumber} |{callerName}| {data}";
            if (newLine)
                str += "\n";
#if DEBUG
            System.Diagnostics.Debug.Write(str);
#else

            var bClr = Console.ForegroundColor;
            Console.ForegroundColor = color;
            /*Console.Write(filePath + "[" + callerName + "()");
            if (lineNumber >= 0)
                Console.Write(":" + lineNumber);
            Console.Write("]");
            Console.Write(data);*/
            Console.ForegroundColor = bClr;
#endif
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

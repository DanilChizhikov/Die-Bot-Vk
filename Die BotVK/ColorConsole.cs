using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Die_BotVK
{
    public class ColorConsole
    {
        public void ColorWriteLine(string message, ConsoleColor FirstColor, ConsoleColor EndColor)
        {
            Console.ForegroundColor = FirstColor;
            Console.WriteLine(message);
            Console.ForegroundColor = EndColor;
        }

        public void ColorWrite(string message, ConsoleColor FirstColor, ConsoleColor EndColor)
        {
            Console.ForegroundColor = FirstColor;
            Console.Write(message);
            Console.ForegroundColor = EndColor;
        }
    }
}

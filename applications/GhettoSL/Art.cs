using System;
using System.Collections.Generic;
using System.Text;

namespace ghetto
{
    partial class GhettoSL
    {
        void IntroArt()
        {
            Console.ForegroundColor = System.ConsoleColor.DarkCyan;
            Console.WriteLine("\r\n================================================================");
            Console.WriteLine("================================================================");
            Console.ForegroundColor = System.ConsoleColor.Cyan;
            Console.WriteLine("           __                             ________              ");
            Console.WriteLine("          |  |                           /        \\             ");
            Console.WriteLine("          |  |                          |   ____   |__          ");
            Console.WriteLine("  ________|  |                          |  |    |__|  |         ");
            Console.WriteLine(" |   ___   | |    _______  _            |  |      |   |         ");
            Console.WriteLine(" |  |   |  | +___|       || |___    ____|_ +______|   |         ");
            Console.WriteLine(" |  |   |  |      | ___  |.  ___|. |      |        |  |         ");
            Console.WriteLine(" |  +___|  |  __  ||   | || |__| |_|   .  |_____   |  |         ");
            Console.WriteLine(" |______   | |  | |+___| || |__   __| ||  |     |  |  |         ");
            Console.WriteLine("  _     |  | |  | | _____|| |  | | |  ||  |.____|  |  |_______  ");
            Console.WriteLine(" | |____|  | |  | ||_____ | |  | | |  `   |        |          | ");
            Console.WriteLine(" |_________|_|  |_|______||_|  |_| |______|_______/+__________| ");
            Console.ForegroundColor = System.ConsoleColor.DarkGray;
            Console.WriteLine("                                   (c) 2006 obsoleet industries ");
            Console.ForegroundColor = System.ConsoleColor.DarkCyan;
            Console.WriteLine("================================================================");
            Console.WriteLine("================================================================\r\n");
            Console.ForegroundColor = System.ConsoleColor.Gray;
        }

        void HeaderHelp()
        {
            Console.ForegroundColor = System.ConsoleColor.DarkCyan; Console.Write("\r\n-=");
            Console.ForegroundColor = System.ConsoleColor.Cyan; Console.Write("[");
            Console.ForegroundColor = System.ConsoleColor.White; Console.Write(" Commands ");
            Console.ForegroundColor = System.ConsoleColor.Cyan; Console.Write("]");
            Console.ForegroundColor = System.ConsoleColor.DarkCyan; Console.Write("=----------------------------------------------------------------\r\n");
            Console.ForegroundColor = System.ConsoleColor.Gray;
        }

        void HeaderWho()
        {
            Console.ForegroundColor = System.ConsoleColor.DarkCyan; Console.Write("\r\n-=");
            Console.ForegroundColor = System.ConsoleColor.Cyan; Console.Write("[");
            Console.ForegroundColor = System.ConsoleColor.White; Console.Write(" Nearby Avatars ");
            Console.ForegroundColor = System.ConsoleColor.Cyan; Console.Write("]");
            Console.ForegroundColor = System.ConsoleColor.DarkCyan; Console.Write("=----------------------------------------------------------\r\n");
            Console.ForegroundColor = System.ConsoleColor.Gray;
        }
        void Footer()
        {
            Console.ForegroundColor = System.ConsoleColor.DarkCyan;
            Console.WriteLine("-------------------------------------------------------------------------------\r\n");
            Console.ForegroundColor = System.ConsoleColor.Gray;
        }
    }
}

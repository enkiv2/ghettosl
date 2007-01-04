using libsecondlife;
using System;
using System.Collections.Generic;
using System.Text;

namespace ghetto
{
    partial class GhettoSL
    {
        void IntroArt(int imageNumber)
        {
            switch (imageNumber)
            {
                case 1:
                    {
                        Console.ForegroundColor = System.ConsoleColor.DarkCyan;
                        Console.WriteLine("\r\n════════════════════════════════════════════════\r\n");
                        Console.ForegroundColor = System.ConsoleColor.Cyan;
                        Console.WriteLine("          ┌──┐                                                   ");
                        Console.WriteLine("          │  │                                                   ");
                        Console.WriteLine(" ┌────────┴┐ │      ┌─────────────┐                              ");
                        Console.WriteLine(" │         │ │   ┌──┴────┐        │┌───────┐                     ");
                        Console.WriteLine(" │  ┌───┐  │ │   │       ├┐  ┌────┘│       │                     ");
                        Console.WriteLine(" │  │   │  │ └───┴┐┌───┐ ││  │ ┌─┐ │   .   │                     ");
                        Console.WriteLine(" │  └───┘  │ ┌──┐ │└───┘ ││  ├─┘ └─┴┐ ││   │                     ");
                        Console.WriteLine(" └───────┐ │ │  │ │      ││  ├─┐ ┌─┬┘ ││   │                     ");
                        Console.WriteLine("  ┌─┐    │ │ │  │ │┌─────┘│  │ │ │ │  ││   │                     ");
                        Console.WriteLine("  │ └────┘ │ │  │ │└─────┐│  │ │ │ │  `    │ SL                  ");
                        Console.WriteLine("  └────────┘─┘  └─┘──────┘└──┘ └─┘ └───────┘                     ");
                        Console.ForegroundColor = System.ConsoleColor.DarkCyan;
                        Console.WriteLine("\r\n════════════════════════════════════════════════\r\n");
                        Console.ForegroundColor = System.ConsoleColor.Gray;
                        break;
                    }
                case 2:
                    {
                        Console.ForegroundColor = System.ConsoleColor.Cyan;
                        Console.BackgroundColor = System.ConsoleColor.Cyan;
                        Console.Write("\r\n■■■■■■■■■■");
                        Console.ForegroundColor = System.ConsoleColor.DarkCyan;
                        Console.BackgroundColor = System.ConsoleColor.Black;
                        Console.Write("┌──┐");
                        Console.ForegroundColor = System.ConsoleColor.Cyan;
                        Console.BackgroundColor = System.ConsoleColor.Cyan;
                        Console.Write("■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■\r\n");
                        Console.ForegroundColor = System.ConsoleColor.DarkCyan;
                        Console.BackgroundColor = System.ConsoleColor.Black;
                        Console.WriteLine("░░░░░░░░░░│  │░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░");
                        Console.WriteLine("░┌────────┴┐ │░░░░░░┌─────────────┐░░░░░░░░░░░░░░░░░░░░░░░░");
                        Console.WriteLine("▒│         │ │▒▒▒┌──┴────┐        │┌───────┐▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒");
                        Console.WriteLine("▒│  ┌───┐  │ │▒▒▒│       ├┐  ┌────┘│       │▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒");
                        Console.WriteLine("▓│  │ ☻ │  │ └───┴┐┌───┐ ││  │▓┌─┐▓│  ╒    │▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓");
                        Console.WriteLine("─│  └───┘  │ ┌──┐ │└───┘ ││  ├─┘ └─┴┐ ││   │───-─- ☼ -─-───");
                        Console.WriteLine("▓└───────┐ │ │▓▓│ │      ││  ├─┐ ┌─┬┘ ││   │▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓");
                        Console.WriteLine("▒▒┌─┐    │ │ │▒▒│ │┌─────┘│  │▒│ │▒│  ││   │▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒");
                        Console.WriteLine("▒▒│ └────┘ │ │▒▒│ │└─────┐│  │▒│ │▒│   ╛   │►SL◄▒▒▒▒▒▒▒▒▒▒▒");
                        Console.WriteLine("░░└────────┘─┘░░└─┘──────┘└──┘░└─┘░└───────┘░░░░░░░░░░░░░░░");
                        Console.ForegroundColor = System.ConsoleColor.Cyan;
                        Console.BackgroundColor = System.ConsoleColor.Cyan;
                        Console.WriteLine("■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■\r\n");
                        Console.ForegroundColor = System.ConsoleColor.Gray;
                        Console.BackgroundColor = System.ConsoleColor.Black;
                        break;
                    }
            }
        }


        void HeaderHelp()
        {
            Console.ForegroundColor = System.ConsoleColor.DarkCyan; Console.Write("\r\n-=");
            Console.ForegroundColor = System.ConsoleColor.Cyan; Console.Write("[");
            Console.ForegroundColor = System.ConsoleColor.White; Console.Write(" Commands ");
            Console.ForegroundColor = System.ConsoleColor.Cyan; Console.Write("]");
            Console.ForegroundColor = System.ConsoleColor.DarkCyan; Console.Write("=-────────────────────────────────────────────--──────────--──--·\r\n");
            Console.ForegroundColor = System.ConsoleColor.Gray;
        }

        void HeaderWho()
        {
            Console.ForegroundColor = System.ConsoleColor.DarkCyan; Console.Write("\r\n-=");
            Console.ForegroundColor = System.ConsoleColor.Cyan; Console.Write("[");
            Console.ForegroundColor = System.ConsoleColor.White; Console.Write(" Nearby Avatars ");
            Console.ForegroundColor = System.ConsoleColor.Cyan; Console.Write("]");
            Console.ForegroundColor = System.ConsoleColor.DarkCyan; Console.Write("=-──────────────────────────────────────--──────────--──--·\r\n");
            Console.ForegroundColor = System.ConsoleColor.Gray;
        }

        void Footer()
        {
            Console.ForegroundColor = System.ConsoleColor.DarkCyan; //32 to 111
            Console.WriteLine("-────────────────────────────────────────────────────────────-──────────--──--·\r\n");
            Console.ForegroundColor = System.ConsoleColor.Gray;
        }

        void ShowStats()
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("─────────-──────────--───--·\r\n");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(" Uptime : ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(Duration(Helpers.GetUnixTime() - Session.StartTime) + "\r\n");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(" Earned : L$");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(Session.MoneyReceived + "\r\n");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(" Spent  : L$");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(Session.MoneySpent + "\r\n");

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("──────────-──────────--──--·\r\n");
            Console.ForegroundColor = ConsoleColor.Gray;
        }

    }
}

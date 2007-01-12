/*
* Copyright (c) 2006, obsoleet industries
* All rights reserved.
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions are met:
*
*     * Redistributions of source code must retain the above copyright
*       notice, this list of conditions and the following disclaimer.
*     * Redistributions in binary form must reproduce the above copyright
*       notice, this list of conditions and the following disclaimer in the
*       documentation and/or other materials provided with the distribution.
*     * Neither the name of obsoleet industries nor the names of its
*       contributors may be used to endorse or promote products derived from
*       this software without specific prior written permission.
*
* THIS SOFTWARE IS PROVIDED BY THE REGENTS AND CONTRIBUTORS "AS IS" AND ANY
* EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
* WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
* DISCLAIMED. IN NO EVENT SHALL THE REGENTS AND CONTRIBUTORS BE LIABLE FOR ANY
* DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
* (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
* LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
* ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
* (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
* SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using libsecondlife;
using libsecondlife.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ghetto
{
    //FIXME - FUNCTIONS IN THIS CLASS SHOULD ONLY ACCEPT STRINGS/VALUES,
    //        AND SHOULD HAVE OUTPUT SPLIT INTO START,RESULT,END DISPLAY FUNCTIONS

    static class Display
    {

        /// <summary>
        /// Pad a string with spaces to fit the specified length
        /// </summary>
        public static string Pad(string str, int length)
        {
            string pad = str;
            while (pad.Length < length) pad += " ";
            return pad;
        }

        /// <summary>
        /// Generic info message
        /// </summary>
        public static void InfoResponse(uint sessionNum, string message)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("({0}) {1}", sessionNum, message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        /// <summary>
        /// Generic error message
        /// </summary>
        public static void Error(uint sessionNum, string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("({0}) {1}", sessionNum, message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        /// <summary>
        /// Generic "teleporting" message
        /// </summary>
        public static void Teleporting(uint sessionNum, string simName)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("({0}) Teleporting to {1}...", sessionNum, simName);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        /// <summary>
        /// Displayed upon successful login
        /// </summary>
        public static void Connected(uint sessionNum)
        {
            GhettoSL.UserSession Session = Interface.Sessions[sessionNum];
            Console.Title = Session.Name + " - GhettoSL";

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("* CONNECTED");
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        /// <summary>
        /// Displayed when objects or avatars chat in public
        /// </summary>
        public static void Chat(string fromName, string message,  bool meAction, byte chatType, byte sourceType)
        {
            string volume = "";
            if (chatType == (int)MainAvatar.ChatType.Whisper) volume = "whisper";
            else if (chatType == (int)MainAvatar.ChatType.Shout) volume = "shout";

            if (sourceType == 1) Console.ForegroundColor = ConsoleColor.White;
            else Console.ForegroundColor = ConsoleColor.DarkCyan;

            if (meAction) Console.WriteLine("({0}ed) {1} {2}", volume, fromName, message);
            else if (volume == "") Console.WriteLine("{0}: {1}", fromName, message);
            else Console.WriteLine("{0} {1}s: {2}", fromName, volume, message);

            Console.ForegroundColor = ConsoleColor.Gray;
        }

        /// <summary>
        /// Displayed on Instant Message
        /// </summary>
        public static void InstantMessage(uint sessionNum, byte dialog, string fromName, string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("({0}) {1}: {2}", sessionNum, fromName, message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        /// <summary>
        /// Displayed on AlertMessage
        /// </summary>
        public static void AlertMessage(uint sessionNum, string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("({0}) {1}", sessionNum, message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        /// <summary>
        /// Dsplayed on MoneyBalanceReply
        /// </summary>
        public static void Balance(uint sessionNum, int balance, int changedAmount, string name, string desc)
        {
            if (name != "")
            {
                if (changedAmount > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("({0}) {1} paid you L${2}", sessionNum, name, changedAmount);
                    Console.WriteLine("({0}) Balance: L${1}", sessionNum, balance);
                    
                }
                else if (changedAmount < 0)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("({0}) You paid {1} L${2}", sessionNum, name, -changedAmount);
                    Console.WriteLine("({0}) Balance: L${1}", sessionNum, balance);
                }
            }
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        /// <summary>
        /// Shown when /s or /session is called with no arguments
        /// </summary>
        public static void SessionList()
        {
            foreach (KeyValuePair<uint, GhettoSL.UserSession> c in Interface.Sessions)
            {
                if (c.Value.Client.Network.Connected) Console.ForegroundColor = ConsoleColor.Cyan;
                else Console.ForegroundColor = ConsoleColor.Red; //not connected
                Console.Write(c.Key);
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write("  " + c.Value.Name + Environment.NewLine);
            }
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        /// <summary>
        /// Convert an ugly float vector to a pretty integer one
        /// </summary>
        public static string VectorString(LLVector3 vector)
        {
            return "<" + (int)vector.X + "," + (int)vector.Y + "," + (int)vector.Z + ">";
        }

        /// <summary>
        /// Shown when /who is called
        /// </summary>
        public static void Who(uint sessionNum)
        {
            GhettoSL.UserSession Session = Interface.Sessions[Interface.CurrentSession];

            Console.ForegroundColor = System.ConsoleColor.DarkCyan; Console.Write("\r\n-=");
            Console.ForegroundColor = System.ConsoleColor.Cyan; Console.Write("[");
            Console.ForegroundColor = System.ConsoleColor.White; Console.Write(" Nearby Avatars ");
            Console.ForegroundColor = System.ConsoleColor.Cyan; Console.Write("]");
            Console.ForegroundColor = System.ConsoleColor.DarkCyan; Console.Write("=-──────────────────────────────────────--──────────--──--·\r\n");
            Console.ForegroundColor = System.ConsoleColor.Gray;

            foreach (Avatar av in Interface.Sessions[sessionNum].Avatars.SimLocalAvatars().Values)
            {
                string pos;
                string prefix;
                string dist;
                if (av.SittingOn > 0) {
                    prefix = "~";
                    pos = "<???>";
                    dist = "(??)";
                }
                else {
                    prefix = "";
                    pos = VectorString(av.Position);
                    dist = "(" + (int)Helpers.VecDist(av.Position, Session.Client.Self.Position) + "m)";
                }
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write(Pad(prefix, 1));
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(Pad(av.Name,22));
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(Pad(dist, 7));
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write(Pad(pos, 14));
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(av.ID);
                Console.Write(Environment.NewLine);
            }
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("────────────────────────────────────-──────────────────────────────────--───--·");
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        /// <summary>
        /// Shown when /stats is called
        /// </summary>
        public static void Stats(uint sessionNum)
        {
            GhettoSL.UserSession Session = Interface.Sessions[Interface.CurrentSession];

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("───────────────────-──────────--───--·");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(" Name   : ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(Session.Client.Self.FirstName + " " + Session.Client.Self.LastName + Environment.NewLine);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(" Region : ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(Session.Client.Network.CurrentSim.Region.Name + Environment.NewLine);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(" Uptime : ");
            Console.ForegroundColor = ConsoleColor.White;
            uint elapsed = Helpers.GetUnixTime() - Session.StartTime;
            Console.Write(Duration(elapsed) + Environment.NewLine);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(" Earned : ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("L$" + Session.MoneyReceived + Environment.NewLine);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(" Spent  : ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("L$" + Session.MoneySpent + Environment.NewLine);

            if (Session.Client.Self.SittingOn > 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(" Seat   : ");
                Console.ForegroundColor = ConsoleColor.Gray;
                uint sittingOn = Session.Client.Self.SittingOn;
                Console.Write(sittingOn);
                if (Session.Prims.ContainsKey(sittingOn))
                {
                    Console.Write(" - " + Session.Prims[sittingOn].ID + Environment.NewLine);
                    if (Session.Prims[sittingOn].Text != "")
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write(" Text   : ");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write(Session.Prims[sittingOn].Text + Environment.NewLine);
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("────────────────────-──────────--──--·" + Environment.NewLine);
            Console.ForegroundColor = ConsoleColor.Gray;
        }



        /// <summary>
        /// "The sun is shining in..."
        /// </summary>
        public static string RPGWeather(GhettoSL.UserSession Session)
        {
            LLVector3 SunDirection = Session.Client.Grid.SunDirection;

            //Console.WriteLine("Sun: " + SunDirection.X + " " + SunDirection.Y + " " + SunDirection.Z);
            string response = "";
            if (SunDirection.Z > 0.9) response = "It is midday";
            else if (SunDirection.Z > 0.5) response = "The sun is shining";
            else if (SunDirection.Z > 0.1)
            {
                if (SunDirection.X > 0) response = "It is still morning";
                else response = "It is late afternoon";
            }
            else if (SunDirection.Z > 0)
            {
                if (SunDirection.X > 0) response = "The sun is rising";
                else response = "The sun is setting";
            }
            else if (SunDirection.Z < -0.9) response = "It is the middle of the night";
            else if (SunDirection.Z < -0.5) response = "The moon lingers overhead";
            else if (SunDirection.Z < -0.1) response = "It is nighttime";
            else if (SunDirection.Z < 0)
            {
                if (SunDirection.X > 0) response = "It is not yet dawn";
                else response = "The night is still young";
            }
            return response + " in " + Session.Client.Network.CurrentSim.Region.Name + ".";
        }


        /// <summary>
        /// Convert seconds to weeks, days, hours, minutes, seconds
        /// </summary>
        public static string Duration(uint seconds)
        {
            string d = "";
            uint remaining = seconds;

            if (remaining >= 31556926)
            {
                d += (int)(remaining / 31556926) + "yr ";
                remaining %= 31556926;
            }
            if (remaining >= 2629744)
            {
                d += (int)(remaining / 2629744) + "mo ";
                remaining %= 2629744;
            }
            if (remaining >= 604800)
            {
                d += (int)(remaining / 604800) + "wks ";
                remaining %= 604800;
            }
            if (remaining >= 86400)
            {
                d += (int)(remaining / 86400) + "days ";
                remaining %= 86400;
            }
            if (remaining >= 3600)
            {
                d += (int)(remaining / 3600) + "hrs ";
                remaining %= 3600;
            }
            if (remaining >= 60)
            {
                d += (int)(remaining / 60) + "mins ";
                remaining %= 60;
            }
            if (remaining >= 0)
            {
                d += remaining + "secs";
            }

            return d;
        }


        public static void IntroArt(int imageNumber)
        {
            switch (imageNumber)
            {
                case 1:
                    {
                        Console.ForegroundColor = System.ConsoleColor.DarkCyan;
                        Console.WriteLine(Environment.NewLine + "════════════════════════════════════════════════" + Environment.NewLine);
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
                        Console.WriteLine(Environment.NewLine + "════════════════════════════════════════════════" + Environment.NewLine);
                        Console.ForegroundColor = System.ConsoleColor.Gray;
                        break;
                    }
                case 2:
                    {
                        Console.ForegroundColor = System.ConsoleColor.Cyan;
                        Console.BackgroundColor = System.ConsoleColor.Cyan;
                        Console.Write(Environment.NewLine + "■■■■■■■■■■");
                        Console.ForegroundColor = System.ConsoleColor.DarkCyan;
                        Console.BackgroundColor = System.ConsoleColor.Black;
                        Console.Write("┌──┐");
                        Console.ForegroundColor = System.ConsoleColor.Cyan;
                        Console.BackgroundColor = System.ConsoleColor.Cyan;
                        Console.Write("■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■" + Environment.NewLine);
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
                        Console.WriteLine("■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■" + Environment.NewLine);
                        Console.ForegroundColor = System.ConsoleColor.Gray;
                        Console.BackgroundColor = System.ConsoleColor.Black;
                        break;
                    }
            }
        }


        public static void Help(string helpTopic)
        {

            string topic = "";
            string result = "";
            if (helpTopic != null) topic = helpTopic.ToLower();

            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict = HelpSystem.HelpDict;

            if (topic == "")
            {
                HeaderHelp();
                foreach (KeyValuePair<string, string> pair in dict)
                {
                    string spaces = "";
                    for (int sp = pair.Key.Length; sp < 24; sp++) spaces += " ";
                    Console.ForegroundColor = System.ConsoleColor.White;
                    Console.Write(" " + pair.Key + spaces);
                    Console.ForegroundColor = System.ConsoleColor.Gray;
                    Console.Write(pair.Value + Environment.NewLine);
                }
                Footer();
            }
            else
            {
                Console.ForegroundColor = System.ConsoleColor.DarkGray;
                if (HelpSystem.HelpDict.TryGetValue(topic, out result)) Console.WriteLine(topic + " - " + result);
                else Console.WriteLine("No help available for that topic. Type /help for a list of commands.");
                Console.ForegroundColor = System.ConsoleColor.Gray;
            }

        }

        public static void HeaderHelp()
        {
            Console.ForegroundColor = System.ConsoleColor.DarkCyan; Console.Write("\r\n-=");
            Console.ForegroundColor = System.ConsoleColor.Cyan; Console.Write("[");
            Console.ForegroundColor = System.ConsoleColor.White; Console.Write(" Commands ");
            Console.ForegroundColor = System.ConsoleColor.Cyan; Console.Write("]");
            Console.ForegroundColor = System.ConsoleColor.DarkCyan; Console.Write("=-───────────────────────────────────────--───────────────--──--·\r\n");
            Console.ForegroundColor = System.ConsoleColor.Gray;
        }

        public static void Footer()
        {
            Console.ForegroundColor = System.ConsoleColor.DarkCyan; //32 to 111
            Console.WriteLine("-────────────────────────────────────────────────────────────-──────────--──--·\r\n");
            Console.ForegroundColor = System.ConsoleColor.Gray;
        }


    }
}

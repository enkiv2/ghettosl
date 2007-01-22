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
            SetColor(ConsoleColor.Blue);
            Console.WriteLine("({0}) {1}", sessionNum, message);
            SetColor(ConsoleColor.Gray);
        }

        /// <summary>
        /// Generic error message
        /// </summary>
        public static void Error(uint sessionNum, string message)
        {
            SetColor(ConsoleColor.Red);
            Console.WriteLine("({0}) {1}", sessionNum, message);
            SetColor(ConsoleColor.Gray);
        }

        /// <summary>
        /// Generic "teleporting" message
        /// </summary>
        public static void Teleporting(uint sessionNum, string simName)
        {
            SetColor(ConsoleColor.Magenta);
            Console.WriteLine("({0}) Teleporting to {1}...", sessionNum, simName);
            SetColor(ConsoleColor.Gray);
        }

        /// <summary>
        /// Displayed on teleport finish
        /// </summary>
        public static void TeleportFinished(uint sessionNum, string simName)
        {
            Display.SetColor(System.ConsoleColor.Magenta);
            Display.Teleporting(sessionNum, "Arrived in " + simName + ".");
            Display.SetColor(System.ConsoleColor.Gray);
        }

        /// <summary>
        /// Displayed upon successful login
        /// </summary>
        public static void Connected(uint sessionNum, string name)
        {
            Console.Title = name + " - GhettoSL";

            Display.SetColor(ConsoleColor.White);
            Console.WriteLine("({0}) CONNECTED", sessionNum);
            Display.SetColor(ConsoleColor.Gray);
        }

        /// <summary>
        /// Displayed on disconnect from sim (including after teleport)
        /// </summary>
        public static void Disconnected(uint sessionNum, string reason)
        {
            Display.SetColor(System.ConsoleColor.Red);
            Console.WriteLine("({0}) DISCONNECTED FROM SIM: {1}", sessionNum, reason);
            Display.SetColor(System.ConsoleColor.Gray);
        }

        /// <summary>
        /// Displayed when objects or avatars chat in public
        /// </summary>
        public static void Chat(uint sessionNum, string fromName, string message, bool meAction, byte chatType, byte sourceType)
        {
            string volume = "";
            if (chatType == (int)MainAvatar.ChatType.Whisper) volume = "whisper";
            else if (chatType == (int)MainAvatar.ChatType.Shout) volume = "shout";

            if (sourceType == 1) Display.SetColor(ConsoleColor.White);
            else Display.SetColor(ConsoleColor.DarkCyan);

            if (meAction) Console.WriteLine("({0}) ({1}ed) {2} {3}", sessionNum, volume, fromName, message);
            else if (volume == "") Console.WriteLine("({0}) {1}: {2}", sessionNum, fromName, message);
            else Console.WriteLine("({0}) {1} {2}s: {3}", sessionNum, fromName, volume, message);

            Display.SetColor(ConsoleColor.Gray);
        }

        /// <summary>
        /// Displayed on Instant Message
        /// </summary>
        public static void InstantMessage(uint sessionNum, bool objectNotAgent, byte dialog, string fromName, string message)
        {
            string type = "";
            if (dialog != 1) type = " (" + dialog.ToString() + ")";
            if (objectNotAgent) Display.SetColor(ConsoleColor.DarkCyan);
            else Display.SetColor(ConsoleColor.Cyan);
            Console.WriteLine("({0}) {1}{2}: {3}", sessionNum, fromName, type, message);
            Display.SetColor(ConsoleColor.Gray);
        }

        /// <summary>
        /// Displayed on AlertMessage
        /// </summary>
        public static void AlertMessage(uint sessionNum, string message)
        {
            SetColor(ConsoleColor.Cyan);
            Console.WriteLine("({0}) {1}", sessionNum, message);
            SetColor(ConsoleColor.Gray);
        }

        /// <summary>
        /// Dsplayed on MoneyBalanceReply
        /// </summary>
        public static void Balance(uint sessionNum, int balance, int changedAmount, string name, string desc)
        {
            if (name != "")
            {
                if (changedAmount < 0)
                {
                    Display.SetColor(ConsoleColor.Magenta);
                    Console.WriteLine("({0}) You paid {1} L${2}", sessionNum, name, -changedAmount);
                    Console.WriteLine("({0}) Balance: L${1}", sessionNum, balance);
                    return;
                }

                if (changedAmount > 0)
                {
                    SetColor(ConsoleColor.Green);
                    Console.WriteLine("({0}) {1} paid you L${2}", sessionNum, name, changedAmount);
                }

            }
            Console.WriteLine("({0}) Balance: L${1}", sessionNum, balance);
            SetColor(ConsoleColor.Gray);
        }

        /// <summary>
        /// Shown when /s or /session is called with no arguments
        /// FIXME - move ghettosl dependencies out of Display
        /// </summary>
        public static void SessionList()
        {
            /// FIXME - move ghettosl dependencies out of Display
            foreach (KeyValuePair<uint, GhettoSL.UserSession> c in Interface.Sessions)
            {
                if (c.Value.Client.Network.Connected) SetColor(ConsoleColor.Cyan);
                else SetColor(ConsoleColor.Red); //not connected
                Console.Write(" " + Pad(c.Key.ToString(),3));
                SetColor(ConsoleColor.DarkCyan);
                Console.Write(c.Value.Name + Environment.NewLine);
            }
            SetColor(ConsoleColor.Gray);
        }
        /// FIXME - move ghettosl dependencies out of Display
        public static void EventList(uint sessionNum)
        {
            GhettoSL.UserSession Session = Interface.Sessions[sessionNum];
            if (Session.ScriptEvents.Count == 0)
            {
                Display.InfoResponse(sessionNum, "No events are active for this session.");
                return;
            }
            foreach (KeyValuePair<string, ScriptSystem.ScriptEvent> c in Session.ScriptEvents)
            {
                SetColor(ConsoleColor.DarkCyan);
                Console.Write(Pad(" " + c.Key, 10));
                SetColor(ConsoleColor.Cyan);
                Console.Write(Pad(" " + c.Value.EventType.ToString(), 10));
                SetColor(ConsoleColor.Gray);
                Console.Write(" " + c.Value.Command + Environment.NewLine);
            }
        }

        /// <summary>
        /// Convert an ugly float vector to a pretty integer one
        /// </summary>
        public static string VectorString(LLVector3 vector)
        {
            return "<" + (int)vector.X + "," + (int)vector.Y + "," + (int)vector.Z + ">";
        }


        /// <summary>
        /// Shown when /re is sent with no arguments
        /// FIXME - move ghettosl dependencies out of Display
        /// </summary>
        public static void IMSessions(uint sessionNum)
        {
            GhettoSL.UserSession Session = Interface.Sessions[sessionNum];
            foreach (GhettoSL.IMSession im in Session.IMSessions.Values)
            {
                SetColor(ConsoleColor.Cyan);
                Console.WriteLine(im.Name);
            }
            SetColor(ConsoleColor.Gray);
        }


        /// <summary>
        /// Shown when /who is called
        /// </summary>
        public static void Who(uint sessionNum)
        {
            GhettoSL.UserSession Session = Interface.Sessions[Interface.CurrentSession];

            SetColor(System.ConsoleColor.DarkCyan); Console.Write(Environment.NewLine + "-=");
            SetColor(System.ConsoleColor.Cyan); Console.Write("[");
            SetColor(System.ConsoleColor.White); Console.Write(" Nearby Avatars ");
            SetColor(System.ConsoleColor.Cyan); Console.Write("]");
            SetColor(System.ConsoleColor.DarkCyan); Console.Write("=-──────────────────────────────────────--──────────--──--·" + Environment.NewLine);
            SetColor(System.ConsoleColor.Gray);

            LLVector3 myPos;
            if (Session.Client.Self.SittingOn > 0) myPos = Session.Prims[Session.Client.Self.SittingOn].Position;
            else myPos = Session.Client.Self.Position;

            foreach (Avatar av in Session.Avatars.SimLocalAvatars().Values)
            {
                string pos;
                string prefix;
                string dist;

                if (av.SittingOn > 0)
                {
                    prefix = "~";

                    if (!Session.Prims.ContainsKey(av.SittingOn))
                    {
                        pos = "<???>";
                        dist = "(??)";
                    }
                    else
                    {
                        LLVector3 avPos = Session.Prims[av.SittingOn].Position;
                        avPos.X += av.Position.X;
                        avPos.Y += av.Position.Y;
                        avPos.Z += av.Position.Z;
                        dist = "(" + (int)Helpers.VecDist(avPos, myPos) + "m)";
                        pos = VectorString(avPos);
                    }

                }

                else
                {
                    prefix = "";
                    pos = VectorString(av.Position);
                    dist = "(" + (int)Helpers.VecDist(av.Position, myPos) + "m)";
                }
                SetColor(ConsoleColor.DarkCyan);
                Console.Write(Pad(prefix, 1));
                SetColor(ConsoleColor.Cyan);
                Console.Write(Pad(av.Name,22));
                SetColor(ConsoleColor.White);
                Console.Write(Pad(dist, 7));
                SetColor(ConsoleColor.DarkCyan);
                Console.Write(Pad(pos, 14));
                SetColor(ConsoleColor.DarkGray);
                Console.Write(av.ID);
                Console.Write(Environment.NewLine);
            }
            SetColor(ConsoleColor.DarkCyan);
            Console.WriteLine("────────────────────────────────────-──────────────────────────────────--───--·");
            SetColor(ConsoleColor.Gray);
        }

        /// <summary>
        /// Shown when /stats is called
        /// </summary>
        public static void Stats(uint sessionNum)
        {
            GhettoSL.UserSession Session = Interface.Sessions[Interface.CurrentSession];

            SetColor(ConsoleColor.DarkCyan);
            Console.WriteLine("───────────────────-──────────--───--·");

            SetColor(ConsoleColor.Cyan);
            Console.Write(" Name   : ");
            SetColor(ConsoleColor.White);
            Console.Write(Session.Name + Environment.NewLine);

            SetColor(ConsoleColor.Cyan);
            Console.Write(" Region : ");
            if (Session.Client.Network.Connected  == true)
            {
                SetColor(ConsoleColor.White);
                Console.Write(Session.Client.Network.CurrentSim.Region.Name);
                SetColor(ConsoleColor.Gray);
                Console.Write(" " + VectorString(Session.Client.Self.Position) + Environment.NewLine);
            }
            else
            {
                SetColor(ConsoleColor.Red);
                Console.Write("not connected" + Environment.NewLine);
            }
            SetColor(ConsoleColor.Cyan);
            Console.Write(" Uptime : ");
            SetColor(ConsoleColor.White);
            uint elapsed = Helpers.GetUnixTime() - Session.StartTime;
            Console.Write(Duration(elapsed) + Environment.NewLine);

            SetColor(ConsoleColor.Cyan);
            Console.Write(" Earned : ");
            SetColor(ConsoleColor.Green);
            Console.Write("L$" + Session.MoneyReceived + Environment.NewLine);

            SetColor(ConsoleColor.Cyan);
            Console.Write(" Spent  : ");
            SetColor(ConsoleColor.Red);
            Console.Write("L$" + Session.MoneySpent + Environment.NewLine);

            if (Session.Client.Self.SittingOn > 0)
            {
                SetColor(ConsoleColor.Cyan);
                Console.Write(" Seat   : ");
                SetColor(ConsoleColor.Gray);
                uint sittingOn = Session.Client.Self.SittingOn;
                Console.Write(sittingOn);
                if (Session.Prims.ContainsKey(sittingOn))
                {
                    Console.Write(" - " + Session.Prims[sittingOn].ID + Environment.NewLine);
                    if (Session.Prims[sittingOn].Text != "")
                    {
                        SetColor(ConsoleColor.Cyan);
                        Console.Write(" Text   : ");
                        SetColor(ConsoleColor.Gray);
                        Console.Write(Session.Prims[sittingOn].Text + Environment.NewLine);
                    }
                }
            }

            SetColor(ConsoleColor.DarkCyan);
            Console.WriteLine("────────────────────-──────────--──--·" + Environment.NewLine);
            SetColor(ConsoleColor.Gray);
        }



        /// <summary>
        /// "The sun is shining in..."
        /// </summary>
        public static string RPGWeather(uint sessionNum, string regionName, LLVector3 SunDirection)
        {
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
            return response + " in " + regionName + ".";
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
                        SetColor(System.ConsoleColor.DarkCyan);
                        Console.WriteLine(Environment.NewLine + "════════════════════════════════════════════════" + Environment.NewLine);
                        SetColor(System.ConsoleColor.Cyan);
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
                        SetColor(System.ConsoleColor.DarkCyan);
                        Console.WriteLine(Environment.NewLine + "════════════════════════════════════════════════" + Environment.NewLine);
                        SetColor(System.ConsoleColor.Gray);
                        break;
                    }
                case 2:
                    {
                        SetColor(System.ConsoleColor.Cyan);
                        SetColorBG(System.ConsoleColor.Cyan);
                        Console.Write(Environment.NewLine + "■■■■■■■■■■");
                        SetColor(System.ConsoleColor.DarkCyan);
                        SetColorBG(System.ConsoleColor.Black);
                        Console.Write("┌──┐");
                        SetColor(System.ConsoleColor.Cyan);
                        SetColorBG(System.ConsoleColor.Cyan);
                        Console.Write("■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■" + Environment.NewLine);
                        SetColor(System.ConsoleColor.DarkCyan);
                        SetColorBG(System.ConsoleColor.Black);
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
                        SetColor(System.ConsoleColor.Cyan);
                        SetColorBG(System.ConsoleColor.Cyan);
                        Console.WriteLine("■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■" + Environment.NewLine);
                        SetColor(System.ConsoleColor.Gray);
                        SetColorBG(System.ConsoleColor.Black);
                        break;
                    }
            }
        }


        public static void Help(string helpTopic)
        {

            string topic = "";
            string result = "";
            if (helpTopic != null) topic = helpTopic.ToLower();

            Dictionary<string, string>  HelpDict = new Dictionary<string, string>();

            HelpDict.Add("anim <uuid>", "Start the specified animation");
            HelpDict.Add("camp <text>", "Find a chair with text matching the specified string");
            HelpDict.Add("clear", "Clear the console display");
            HelpDict.Add("fly", "Enable flying");
            HelpDict.Add("*follow <name|off>", "Follow the specified avatar, or \"off\" to disable");
            HelpDict.Add("go <X> <Y> [Z]", "Move to the specified coordinates using autopilot");
            HelpDict.Add("land", "Disable flying");
            HelpDict.Add("listen", "Listen to local chat (on by default)");
            HelpDict.Add("look", "Displays time and region sun direction");
            HelpDict.Add("quiet", "Stop listening to local chat");
            HelpDict.Add("re [name] [message]", "List IM sessions or reply by partial name match");
            HelpDict.Add("relog", "Log out and back in");
            HelpDict.Add("ride <name>", "Sit on the same object as the specified name");
            HelpDict.Add("run", "Enable running");
            HelpDict.Add("session <#> [command]", "Switch or send a command to another session");
            HelpDict.Add("script <scriptName>", "Execute the specified script file");
            HelpDict.Add("shout <message>", "Shout the specified message to users within 100m");
            HelpDict.Add("sit <uuid>", "Sit on the specified UUID");
            HelpDict.Add("sitg", "Sit on the ground at current location");
            HelpDict.Add("stand", "Stand while seated on an object or on the ground");
            HelpDict.Add("stopanim <uuid>", "Stop the specified animation");
            HelpDict.Add("teleport <sim> [x y z]", "Teleports to the specified destination");
            HelpDict.Add("touch <uuid>", "Touch the specified object");
            HelpDict.Add("touchid <localID>", "Touch the specified object LocalID");
            HelpDict.Add("updates <on|off>", "Toggles AgentUpdate timer (on by default)");
            HelpDict.Add("walk", "Disable running");
            HelpDict.Add("whisper", "Whisper the specified message to users within 5m");
            HelpDict.Add("who", "List avatars within viewing range");           

            if (topic == "")
            {
                HeaderHelp();
                foreach (KeyValuePair<string, string> pair in HelpDict)
                {
                    SetColor(System.ConsoleColor.White);
                    Console.Write(" " + Pad(pair.Key,24));
                    SetColor(System.ConsoleColor.Gray);
                    Console.Write(pair.Value + Environment.NewLine);
                }
                Footer();
            }
            else
            {
                SetColor(System.ConsoleColor.DarkGray);
                if (HelpDict.TryGetValue(topic, out result)) Console.WriteLine(topic + " - " + result);
                else
                {
                    foreach (KeyValuePair<string, string> pair in HelpDict)
                    {
                        if (pair.Key.Length < helpTopic.Length) continue; //too short to be a match
                        if (pair.Key.Substring(0, helpTopic.Length).ToLower() == helpTopic.ToLower())
                        {
                            SetColor(System.ConsoleColor.White);
                            Console.Write(" " + Pad(pair.Key, 24));
                            SetColor(System.ConsoleColor.Gray);
                            Console.Write(pair.Value + Environment.NewLine);
                            return;
                        }
                    }
                    Console.WriteLine("No help available for that topic. Type /help for a list of commands.");
                }
                SetColor(System.ConsoleColor.Gray);
            }

        }

        public static void HeaderHelp()
        {
            SetColor(System.ConsoleColor.DarkCyan); Console.Write(Environment.NewLine + "-=");
            SetColor(System.ConsoleColor.Cyan); Console.Write("[");
            SetColor(System.ConsoleColor.White); Console.Write(" Commands ");
            SetColor(System.ConsoleColor.Cyan); Console.Write("]");
            SetColor(System.ConsoleColor.DarkCyan); Console.Write("=-───────────────────────────────────────--───────────────--──--·" + Environment.NewLine);
            SetColor(System.ConsoleColor.Gray);
        }

        public static void Footer()
        {
            SetColor(System.ConsoleColor.DarkCyan); //32 to 111
            Console.WriteLine("-────────────────────────────────────────────────────────────-──────────--──--·" + Environment.NewLine);
            SetColor(System.ConsoleColor.Gray);
        }

        public static void SetColor(System.ConsoleColor color)
        {
            if (!Interface.NoColor) Console.ForegroundColor = color;
        }

        public static void SetColorBG(System.ConsoleColor color)
        {
            if (!Interface.NoColor) Console.BackgroundColor = color;
        }

    }
}

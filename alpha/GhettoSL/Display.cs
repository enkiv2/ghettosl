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

using OpenMetaverse;
//using OpenMetaverse.Utilities;
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
            SetColor(ConsoleColor.DarkBlue);
            Console.Write(SessionPrefix(sessionNum));
            SetColor(ConsoleColor.Blue);
            Console.Write("{0} " + Environment.NewLine, message);
            SetColor(ConsoleColor.Gray);
        }

        public static void Health(uint sessionNum, float health)
        {
            SetColor(ConsoleColor.DarkRed);
            Console.Write(SessionPrefix(sessionNum));
            SetColor(ConsoleColor.Red);
            Console.Write("Health: " + health + "%" + Environment.NewLine);
            SetColor(ConsoleColor.Gray);
        }

        public static string SessionPrefix(uint sessionNum)
        {
            if (sessionNum == 0) return " *  ";
            else return "(" + sessionNum + ") ";
        }

        /// <summary>
        /// Generic error message
        /// </summary>
        public static void Error(uint sessionNum, string message)
        {
            SetColor(ConsoleColor.DarkRed);
            Console.Write(SessionPrefix(sessionNum));
            SetColor(ConsoleColor.Red);
            Console.Write("Error: " + message + Environment.NewLine);
            SetColor(ConsoleColor.Gray);
        }

        /// <summary>
        /// Friend is online
        /// </summary>
        public static void FriendOnline(uint sessionNum, FriendInfo friend)
        {
            string name = "(unknown)";
            if (friend.Name == "") name = friend.Name;
            SetColor(ConsoleColor.DarkYellow);
            Console.Write(SessionPrefix(sessionNum));
            SetColor(ConsoleColor.Yellow);
            Console.Write(name + " is online." + Environment.NewLine);
            SetColor(ConsoleColor.Gray);
        }

        /// <summary>
        /// Friend is offline
        /// </summary>
        public static void FriendOffline(uint sessionNum, FriendInfo friend)
        {
            string name = "(unknown)";
            if (friend.Name == "") name = friend.Name;
            SetColor(ConsoleColor.DarkYellow);
            Console.Write(SessionPrefix(sessionNum));
            SetColor(ConsoleColor.Yellow);
            Console.Write(name + " is offline." + Environment.NewLine);
            SetColor(ConsoleColor.Gray);
        }

        /// <summary>
        /// Friendship offered
        /// </summary>
        public static void FriendshipOffered(uint sessionNum, UUID agentID, string agentName, UUID imSessionID)
        {
            SetColor(ConsoleColor.DarkCyan);
            Console.Write(SessionPrefix(sessionNum));
            SetColor(ConsoleColor.Cyan);
            Console.WriteLine(agentName + " has offered to be your friend.");
            SetColor(ConsoleColor.Gray);
        }

        /// <summary>
        /// Response to your friendship offer
        /// </summary>
        public static void FriendshipResponse(uint sessionNum, UUID agentID, string agentName, bool accepted)
        {
            SetColor(ConsoleColor.Cyan);
            if (accepted) Console.WriteLine(agentName + " accepted your friendship request.");
            else Console.WriteLine(agentName + " refused your friendship request.");
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


        public static void SendMessage(uint sessionNum, int channel, UUID id, string message)
        {
            //GhettoSL.UserSession Session = Interface.Sessions[sessionNum];
            SetColor(ConsoleColor.Cyan);
            Console.WriteLine("({0}) -> ({1}) {2}", sessionNum, channel, message );
            SetColor(ConsoleColor.Gray);
        }

        /// <summary>
        /// Displayed upon sim-change
        /// </summary>
        public static void SimChanged(uint sessionNum, Simulator previousSim, Simulator newSim)
        {
            SetColor(ConsoleColor.DarkMagenta);
            Console.Write(SessionPrefix(sessionNum));
            SetColor(ConsoleColor.Magenta);
            if (previousSim != null && newSim != null) Console.Write("Moved from {0} to {1}." + Environment.NewLine, previousSim.Name, newSim.Name);
            else if (previousSim == null && newSim != null) Console.Write("Entered region {0}." + Environment.NewLine, newSim.Name);
            SetColor(ConsoleColor.Gray);
        }


        /// <summary>
        /// Displayed on teleport finish
        /// </summary>
        public static void TeleportFinished(uint sessionNum)
        {
            GhettoSL.UserSession Session = Interface.Sessions[sessionNum];
            Display.SetColor(System.ConsoleColor.Magenta);
            Console.WriteLine("({0}) Teleported to {1}", sessionNum, Session.Client.Network.CurrentSim.Name);
            Display.SetColor(System.ConsoleColor.Gray);
        }

        /// <summary>
        /// Displayed upon successful login
        /// </summary>
        public static void Connected(uint sessionNum, string name)
        {
            GhettoSL.UserSession Session = Interface.Sessions[sessionNum];
            Display.SetColor(ConsoleColor.Gray);
            Console.Write("({0}) ", sessionNum);
            Display.SetColor(ConsoleColor.White);
            Console.Write("CONNECTED" + Environment.NewLine, sessionNum);
            Display.SetColor(ConsoleColor.Gray);
        }

        /// <summary>
        /// Displayed on disconnect from sim (including after teleport)
        /// </summary>
        public static void Disconnected(uint sessionNum, NetworkManager.DisconnectType reason, string message)
        {
            Display.SetColor(System.ConsoleColor.Red);
            Console.WriteLine("({0}) DISCONNECTED: {1} ({2})", sessionNum, reason, message);
            Display.SetColor(System.ConsoleColor.Gray);
        }

        /// <summary>
        /// Displayed when objects or avatars chat in public
        /// </summary>
        public static void Chat(uint sessionNum, string fromName, string message, bool meAction, ChatType type, ChatSourceType sourceType)
        {
            string volume = "";
            if (type == ChatType.Whisper) volume = "whisper";
            else if (type == ChatType.Shout) volume = "shout";
            else if (type != ChatType.Normal) volume = type.ToString();

            ConsoleColor nameColor;
            ConsoleColor textColor;

            if (sourceType == ChatSourceType.Agent)
            {
                nameColor = ConsoleColor.Cyan;
                textColor = ConsoleColor.Gray;
            }
            else if (sourceType == ChatSourceType.Object)
            {
                nameColor = ConsoleColor.Green;
                textColor = ConsoleColor.DarkCyan;
            }
            else
            {
                nameColor = ConsoleColor.Magenta;
                textColor = ConsoleColor.DarkMagenta;
            }

            SetColor(ConsoleColor.DarkCyan); Console.Write("({0}) ", sessionNum);
            SetColor(nameColor); 

            if (meAction)
            {
                Console.WriteLine(fromName + " " + message);
            }
            else
            {
                if (volume == "") Console.Write(fromName);
                else Console.Write("{0} {1}s", fromName, volume);
                SetColor(ConsoleColor.DarkCyan); Console.Write(": ", sessionNum);
                SetColor(textColor); Console.Write(message + Environment.NewLine);
            }

            SetColor(ConsoleColor.Gray);
        }

        /// <summary>
        /// Displayed on Instant Message
        /// FIXME - ditch objectNotAgent
        /// </summary>
        public static void InstantMessage(uint sessionNum, InstantMessageDialog dialog, string fromName, string message)
        {
            string type = "";
            type = " (" + dialog.ToString() + ")";
            if (dialog == InstantMessageDialog.MessageFromObject) SetColor(ConsoleColor.DarkCyan);
            else SetColor(ConsoleColor.Cyan);
            Console.WriteLine("({0}) {1}{2}: {3}", sessionNum, fromName, type, message);
            SetColor(ConsoleColor.Gray);
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
            if (name != null)
            {
                if (changedAmount < 0)
                {
                    Display.SetColor(ConsoleColor.Magenta);
                    Console.WriteLine("({0}) You paid {1} L${2}", sessionNum, name, -changedAmount);
                }
                else if (changedAmount > 0)
                {
                    SetColor(ConsoleColor.Green);
                    Console.WriteLine("({0}) {1} paid you L${2}", sessionNum, name, changedAmount);
                }
            }
            else
            {
                SetColor(ConsoleColor.Green);
            }
            Console.WriteLine("({0}) Balance: L${1}", sessionNum, balance);
            SetColor(ConsoleColor.Gray);
        }


        /// <summary>
        /// Displayed when a script pops up a dialog
        /// </summary>
        public static void ScriptDialog(uint sessionNum, string message, string objectName, UUID imageID, UUID objectID, string firstName, string lastName, int chatChannel, List<string> buttons)
        {
            SetColor(ConsoleColor.Cyan);
            Console.WriteLine("({0}) Script dialog from \"{1}\" owned by {2} {3}:", sessionNum, objectName, firstName, lastName);
            SetColor(ConsoleColor.DarkCyan);
            Console.WriteLine("Channel : " + chatChannel);
            Console.WriteLine("UUID    : " + objectID);
            Console.WriteLine("Message : " + message);
            Console.WriteLine("Buttons : " + String.Join(", ", buttons.ToArray()));
            SetColor(ConsoleColor.Gray);
        }

        public static void SitChanged(uint sessionNum, Avatar av, uint sittingOn)
        {
            if (av.LocalID != Interface.Sessions[sessionNum].Client.Self.LocalID) return;
            Display.SetColor(ConsoleColor.Blue);
            if (sittingOn > 0) Console.WriteLine("({0}) You take a seat.", sessionNum);
            else Console.WriteLine("({0}) You stand up.", sessionNum);
            Display.SetColor(ConsoleColor.Gray);
        }

        public static void InventoryItemReceived(uint sessionNum, UUID fromAgentID, string fromAgentName, uint parentEstateID, UUID regionID, Vector3 position, DateTime timestamp, InventoryBase item)
        {
            SetColor(ConsoleColor.Cyan);
            if (item is InventoryItem)
            {
                InventoryItem objItem = (InventoryItem)item;
                Console.WriteLine("({0}) {1} gave you an item named {2} ({3}).", sessionNum, fromAgentID, objItem.Name, objItem.Description);
            }
            else if (item is InventoryFolder)
            {
                InventoryFolder objFolder = (InventoryFolder)item;
                Console.WriteLine("({0}) {1} gave you a folder named {2} ({3} items).", sessionNum, fromAgentID, objFolder.Name, objFolder.DescendentCount);
            }
            SetColor(ConsoleColor.Gray);
        }

        public static void LogMessage(uint sessionNum, string message, Helpers.LogLevel level)
        {
            Display.SetColor(ConsoleColor.DarkGray);
            Console.WriteLine("({0}) {1}: {2}", sessionNum, level.ToString().ToUpper(), message);
            Display.SetColor(ConsoleColor.Gray);
        }

        /// <summary>
        /// Shown when /s or /session is called with no arguments
        /// </summary>
        public static void SessionList()
        {
            /// FIXME - move ghettosl dependencies out of Display
            foreach (KeyValuePair<uint, GhettoSL.UserSession> c in Interface.Sessions)
            {
                string prefix = " ";
                if (c.Value.Client.Self.SittingOn > 0) prefix = "~";
                Display.SetColor(ConsoleColor.DarkCyan);
                Console.Write(prefix);

                if (c.Value.Client.Network.Connected) SetColor(ConsoleColor.Cyan);
                else SetColor(ConsoleColor.Red); //not connected
                Console.Write(Pad(c.Key.ToString(),3));

                SetColor(ConsoleColor.DarkCyan);
                Console.Write(Display.Pad(c.Value.Name, 21) + " ");

                SetColor(ConsoleColor.Cyan);
                Console.Write("L$" + Display.Pad(c.Value.Balance.ToString(), 6) + " ");

                SetColor(ConsoleColor.DarkCyan);
                if (c.Value.Client.Network.Connected) Console.Write(Display.Pad(c.Value.Client.Network.CurrentSim.Name, 17));
                else Console.Write(Display.Pad("not connected",17));

                string myPos = "";
                if (c.Value.Client.Network.Connected)
                {
                    myPos = VectorString(c.Value.Client.Self.SimPosition);
                }
                SetColor(ConsoleColor.Cyan);
                Console.Write(" " + myPos);
                Console.Write(Environment.NewLine);
            }
            SetColor(ConsoleColor.Gray);
        }

        public static void EventList(uint sessionNum)
        {
            Console.WriteLine("FIXME");
            GhettoSL.UserSession Session = Interface.Sessions[sessionNum];
            //{
            //    Display.InfoResponse(sessionNum, "No events are active for this session.");
            //    return;
            //}
            //foreach (KeyValuePair<string, ScriptSystem.ScriptEvent> c in Session.ScriptEvents)
            //{
            //    SetColor(ConsoleColor.DarkCyan);
            //    Console.Write(Pad(" " + c.Key, 10));
            //    SetColor(ConsoleColor.Cyan);
            //    Console.Write(Pad(" " + c.Value.EventType.ToString(), 10));
            //    SetColor(ConsoleColor.Gray);
            //    Console.Write(" " + c.Value.Command + Environment.NewLine);
            //}
        }

        public static void GroupList(uint sessionNum)
        {
            GhettoSL.UserSession Session = Interface.Sessions[sessionNum];
            if (Session.Groups.Count == 0)
            {
                Display.InfoResponse(sessionNum, "You do not appear to be in any groups");
            }
            else
            {
                foreach (KeyValuePair<UUID, OpenMetaverse.Group> g in Session.Groups)
                {
                    Console.WriteLine(g.Key + " (" + g.Value.GroupMembershipCount + ") " + g.Value.Name);
                }
            }
        }


        /// <summary>
        /// Convert an ugly float vector to a pretty integer one
        /// </summary>
        public static string VectorString(Vector3 vector)
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
            lock (Session.IMSessions)
            {
                foreach (GhettoSL.IMSession im in Session.IMSessions.Values)
                {
                    SetColor(ConsoleColor.Cyan);
                    Console.WriteLine(im.Name);
                }
            }
            SetColor(ConsoleColor.Gray);
        }


        public static void GroupRoles(Dictionary<UUID, GroupRole> roles)
        {
            foreach (GroupRole role in roles.Values)
            {
                Console.WriteLine(role.ID + " " + role.Name + " \"" + role.Title + "\"");
            }
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

            Vector3 myPos = Session.Client.Self.SimPosition;

            lock (Session.Avatars.Values)
            {
                foreach (Avatar av in Session.Avatars.Values)
                {
                    string pos;
                    string prefix;
                    string dist;

                    if (av.ParentID > 0)
                    {
                        prefix = "~";

                        if (!Session.Prims.ContainsKey(av.ParentID))
                        {
                            pos = "<???>";
                            dist = "(??)";
                        }
                        else
                        {
                            Vector3 avPos = Session.Prims[av.ParentID].Position;
                            avPos.X += av.Position.X;
                            avPos.Y += av.Position.Y;
                            avPos.Z += av.Position.Z;
                            if (myPos != Vector3.Zero) dist = "(" + (int)Vector3.Distance(avPos, myPos) + "m)";
                            else dist = "(??)";
                            pos = VectorString(avPos);
                        }

                    }

                    else
                    {
                        prefix = "";
                        pos = VectorString(av.Position);
                        dist = "(" + (int)Vector3.Distance(av.Position, myPos) + "m)";
                    }
                    SetColor(ConsoleColor.DarkCyan);
                    Console.Write(Pad(prefix, 1));
                    SetColor(ConsoleColor.Cyan);
                    Console.Write(Pad(av.Name, 22));
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
        }

        public static void ScriptList()
        {
            foreach (ScriptSystem.UserScript s in Interface.Scripts.Values)
            {
                SetColor(ConsoleColor.Cyan);
                Console.Write(" " + Pad(s.ScriptName,15) + " ");
                SetColor(ConsoleColor.Blue);
                Console.Write(s.Aliases.Count + " Alias(es), " + s.Events.Count + " Event(s)" + Environment.NewLine);
            }
            InfoResponse(0, Interface.Scripts.Count + " script(s) loaded.");
        }

        /// <summary>
        /// Shown when /stats is called
        /// </summary>
        public static void Stats(uint sessionNum)
        {
            GhettoSL.UserSession Session = Interface.Sessions[sessionNum];

            SetColor(ConsoleColor.DarkCyan);
            Console.WriteLine("──────────────────────────-──────────--───--·");

            SetColor(ConsoleColor.Cyan);
            Console.Write(" Name    : ");
            SetColor(ConsoleColor.White);
            Console.Write(Session.Name + Environment.NewLine);

            SetColor(ConsoleColor.Cyan);
            Console.Write(" UUID    : ");
            SetColor(ConsoleColor.Gray);
            Console.Write(Session.Client.Self.AgentID + Environment.NewLine);

            SetColor(ConsoleColor.Cyan);
            Console.Write(" Region  : ");
            if (Session.Client.Network.Connected  == true)
            {
                SetColor(ConsoleColor.White);
                Console.Write(Session.Client.Network.CurrentSim.Name);
                SetColor(ConsoleColor.Gray);
                //Console.Write(" " + Session.Client.Network.CurrentSim.Handle);
                Console.Write(" " + VectorString(Session.Client.Self.SimPosition) + Environment.NewLine);
            }
            else
            {
                SetColor(ConsoleColor.Red);
                Console.Write("not connected" + Environment.NewLine);
            }
            SetColor(ConsoleColor.Cyan);
            Console.Write(" Uptime  : ");
            SetColor(ConsoleColor.White);
            uint elapsed = Utils.GetUnixTime() - Session.StartTime;
            Console.Write(Duration(elapsed) + Environment.NewLine);

            SetColor(ConsoleColor.Cyan);
            Console.Write(" Balance : ");
            SetColor(ConsoleColor.Green);
            Console.Write("L$" + Session.Balance + Environment.NewLine);

            SetColor(ConsoleColor.Cyan);
            Console.Write(" Earned  : ");
            SetColor(ConsoleColor.Gray);
            Console.Write("L$" + Session.MoneyReceived + Environment.NewLine);

            SetColor(ConsoleColor.Cyan);
            Console.Write(" Spent   : ");
            SetColor(ConsoleColor.Gray);
            Console.Write("L$" + Session.MoneySpent + Environment.NewLine);

            if (Session.Client.Self.SittingOn > 0)
            {
                SetColor(ConsoleColor.Cyan);
                Console.Write(" Seat    : ");
                SetColor(ConsoleColor.Gray);
                uint sittingOn = Session.Client.Self.SittingOn;
                Console.Write(sittingOn);
                if (Session.Prims.ContainsKey(sittingOn))
                {
                    Console.Write(" - " + Session.Prims[sittingOn].ID + Environment.NewLine);
                    if (Session.Prims[sittingOn].Text != "")
                    {
                        SetColor(ConsoleColor.Cyan);
                        Console.Write(" Text    : ");
                        SetColor(ConsoleColor.Gray);
                        Console.Write(Session.Prims[sittingOn].Text);
                    }
                }
                else
                {
                    SetColor(ConsoleColor.DarkGray);
                    Console.Write(" object info missing");
                }
                Console.Write(Environment.NewLine);
            }

            SetColor(ConsoleColor.DarkCyan);
            Console.WriteLine("────────────────────────-─────────────--──--·");
            SetColor(ConsoleColor.Gray);
        }



        /// <summary>
        /// "The sun is shining in..."
        /// </summary>
        public static string RPGWeather(uint sessionNum, string regionName, Vector3 SunDirection)
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
            HelpDict.Add("answer <text>", "Answer the last-received script dialog");
            HelpDict.Add("brot <x,y,z>", "rotate your avatar's body (Z affects direction)");
            HelpDict.Add("clear", "Clear the console display");
            HelpDict.Add("delete <inventoryID>", "Delete the specified inventory item");
            HelpDict.Add("dir <path>", "View inventory folder contents");
            HelpDict.Add("ekick <userID>", "Estate kick (requires manager access)");
            HelpDict.Add("ekill <userID>", "Teleport user home (requires manager access)");
            HelpDict.Add("eban <userID>", "Estate ban (requires manager access)");
            HelpDict.Add("eunban <userID>", "Estate unban (requires manager access)");
            HelpDict.Add("fly [on|off]", "Toggles avatar flying");
            HelpDict.Add("follow <name|off|on>", "Follow the specified avatar, or toggle following");
            HelpDict.Add("friend [-r] <userID>", "Request friendship, or remove an existing friend");
            HelpDict.Add("go <x,y,z>", "Move to the specified coordinates using autopilot");
            HelpDict.Add("groups", "Lists information about groups you are in");
            HelpDict.Add("home", "Teleport to your home location");
            HelpDict.Add("hrot <x,y,z>", "rotate your avatar's head (does this do anything?)");
            HelpDict.Add("land", "Disable flying");
            HelpDict.Add("listen", "Same as /quiet off");
            HelpDict.Add("lure <userID> [reason]", "Send a teleport lure request");
            HelpDict.Add("look [text]", "Display info about area, or searches by prim text");
            HelpDict.Add("particles", "Show the ID and location of prims with particles");
            HelpDict.Add("pay <amount> <uuid>", "Pay the specified avatar or object");
            HelpDict.Add("paybytext <amt> <text>", "Pay an object with text regex-matching <text>");
            HelpDict.Add("payme <amount>", "Pay the specified amount to $masterid, if defined");
            HelpDict.Add("quiet [on|off]", "Toggle the display of object/avatar chat");
            HelpDict.Add("re [name] [message]", "List IM sessions or reply by partial name match");
            HelpDict.Add("relog", "Log out and back in");
            HelpDict.Add("ride <name>", "Sit on the same object as the specified name");
            HelpDict.Add("roles <groupID>", "List role information for the specified group ID");
            HelpDict.Add("rotto <position>", "Rotate your avatar's body to face toward <position>");
            HelpDict.Add("run [on|off]", "Toggles avatar running");
            HelpDict.Add("session <#> [command]", "Switch or send a command to another session");
            HelpDict.Add("sethome", "Sets your home position to the current location");
            HelpDict.Add("script <scriptName>", "Execute the specified script file");
            HelpDict.Add("shout <message>", "Shout the specified message to users within 100m");
            HelpDict.Add("simmessage <message>", "Send a message to the sim (requires manager access)");
            HelpDict.Add("sit <uuid>", "Sit on the specified UUID");
            HelpDict.Add("sitbytext <text>", "Find a chair with text regex-matching <text>");
            HelpDict.Add("sitg", "Sit on the ground at current location");
            HelpDict.Add("stand", "Stand while seated on an object or on the ground");
            HelpDict.Add("stopanim <uuid>", "Stop the specified animation");
            HelpDict.Add("teleport \"sim\" <x,y,z>", "Teleport to the specified destination");
            HelpDict.Add("timer [-m] <label> [off] [reps] [interval] [command]", "");
            HelpDict.Add("touch <uuid>", "Touch the specified object");
            HelpDict.Add("touchbytext <text>", "Touch an object with text regex-matching <text>");
            HelpDict.Add("touchid <localID>", "Touch the specified object LocalID");
            HelpDict.Add("updates <on|off>", "Toggle AgentUpdate timer (on by default)");
            HelpDict.Add("walk", "Disable running");
            HelpDict.Add("wear <itemid> [point]", "Attach an object from inventory after /dir");
            HelpDict.Add("whisper", "Whisper the specified message to users within 5m");
            HelpDict.Add("whois", "Look up an avatar by name");
            HelpDict.Add("who", "List avatars within viewing range");

            Dictionary<string, string> ScriptingDict = new Dictionary<string, string>();
            ScriptingDict.Add("inc <%var> [amount]", "Increases %var by 1 or any other number/%var");
            ScriptingDict.Add("set <%var> <value>", "Used in scripts to define or change a variable");
            ScriptingDict.Add("settarget <position>", "Sets a location for $distance to compare against");
            ScriptingDict.Add("settime", "Sets script-time for $elapsed to compare against");
            ScriptingDict.Add("return", "Exits the current routine");

            if (topic == "")
            {
                HeaderHelp();
                foreach (KeyValuePair<string, string> pair in HelpDict)
                {
                    SetColor(System.ConsoleColor.White);
                    Console.Write(" " + Pad(pair.Key,25));
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
                    bool match = false;
                    foreach (KeyValuePair<string, string> pair in HelpDict)
                    {
                        if (pair.Key.Length < helpTopic.Length) continue; //too short to be a match
                        if (pair.Key.Substring(0, helpTopic.Length).ToLower() == helpTopic.ToLower())
                        {
                            SetColor(System.ConsoleColor.White);
                            Console.Write(" " + Pad(pair.Key, 24));
                            SetColor(System.ConsoleColor.Gray);
                            Console.Write(pair.Value + Environment.NewLine);
                            match = true;
                        }
                    }
                    if (!match) Console.WriteLine("No help available for \"" + topic + "\". Type /help for a list of commands.");
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
            Console.WriteLine("-────────────────────────────────────────────────────────────-──────────--──--·");
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

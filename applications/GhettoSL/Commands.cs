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
using libsecondlife.Packets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ghetto
{
    partial class GhettoSL
    {
        void ParseCommand(bool console, string commandString, string fromAgentName, LLUUID fromAgentID, LLUUID imSessionID)
        {
            if (commandString.Length == 0) return;
            char[] splitChar = { ' ' };
            string[] msg = commandString.Split(splitChar);
            if (msg[0] == null || msg[0] == "") return;

            string command = msg[0].ToLower();
            string response = "";

            //Store command arguments in "details" variable
            string details = null;
            int i = 1;
            if (command == "re" || command == "im") i++;
            while (i < msg.Length)
            {
                details += msg[i];
                if (i + 1 < msg.Length) details += " ";
                i++;
            }

            switch (command)
            {
                case "account":
                    {
                        string action = msg[1].ToLower();
                        if (action == "add")
                        {
                            if (msg.Length < 5)
                            {
                                response = "Usage: /account <list|add|del> [FirstName] [LastName] [Password] [options]";
                            }
                            else
                            {
                                //FIXME - Add account to list.. this command might get renamed now too

                            }
                        }
                        else if (action == "del" || action == "remove")
                        {

                        }
                        else
                        {
                            response = "Usage: /account <list|add|del> [FirstName] [LastName] [Password] [options]";
                        }
                        break;
                    }
                case "anim":
                    {
                        SendAgentAnimation((LLUUID)details, true);
                        break;
                    }
                case "stopanim":
                    {
                        SendAgentAnimation((LLUUID)details, false);
                        break;
                    }
                case "backflip":
                    {
                        SendAgentAnimation((LLUUID)"c4ca6188-9127-4f31-0158-23c4e2f93304", true); //backflip
                        Thread.Sleep(500);
                        Client.Self.Status.Controls.FinishAnim = true;
                        Client.Self.Status.SendUpdate();
                        Thread.Sleep(500);
                        Client.Self.Status.Controls.FinishAnim = false;
                        Client.Self.Status.SendUpdate();
                        break;
                    }
                case "balance":
                    {
                        Client.Self.RequestBalance();
                        break;
                    }
                case "camp":
                    {
                        uint localID = FindObjectByText(details.ToLower());
                        if (localID > 0)
                        {
                            response = "Match found. Camping...";
                            Client.Self.RequestSit(Session.Prims[localID].ID, new LLVector3(0, 0, 0));
                            Client.Self.Sit();
                            Client.Self.Status.Controls.FinishAnim = false;
                            Client.Self.Status.Controls.Fly = false;
                            Client.Self.Status.Controls.StandUp = false;
                            Client.Self.Status.SendUpdate();
                        }
                        else response = "No matching objects found.";
                        break;
                    }
                case "clear":
                    {
                        Console.Clear();
                        break;
                    }
                case "clone":
                    {
                        if (msg.Length != 3) return;
                        if (Clone(details)) response = "Cloning...";
                        else response = "Error: Avatar not found";
                        break;
                    }
                case "die":
                    {
                        logout = true;
                        response = "Shutting down...";
                        break;
                    }
                case "quit":
                    {
                        logout = true;
                        response = "Shutting down...";
                        break;
                    }
                case "drag":
                    {
                        LLUUID findID = (LLUUID)msg[1];
                        foreach (PrimObject prim in Session.Prims.Values)
                        {
                            if (prim.ID != findID) continue;
                            LLVector3 targetPos = new LLVector3(prim.Position.X, prim.Position.Y, prim.Position.Z + 10);
                            Client.Self.Grab(prim.LocalID);
                            Client.Self.GrabUpdate(prim.ID, targetPos);
                            Client.Self.DeGrab(prim.LocalID);
                            response = "DRAGGED OBJECT " + prim.LocalID + " TO " + targetPos;
                            break;
                        }
                        if (response == "") response = "NO OBJECT FOUND MATCHING " + findID;
                        break;
                    }
                case "events":
                    {
                        foreach (KeyValuePair<string, ScriptEvent> pair in Session.Script.Events)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write(pair.Key);
                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                            Console.Write(" " + pair.Value.Command);

                            Console.Write("\r\n");
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }
                        break;
                    }
                case "face":
                    {
                        //FIXME - Face toward the specified object uuid
                        LLUUID findID = (LLUUID)msg[1];
                        foreach (PrimObject prim in Session.Prims.Values)
                        {
                            if (prim.ID != findID) continue;
                            LLVector3 targetPos = new LLVector3(prim.Position.X, prim.Position.Y, prim.Position.Z + 10);
                            LLQuaternion between = Helpers.RotBetween(Client.Self.Position, prim.Position);
                            response = "FACING " + targetPos + " " + between;
                            break;
                        }
                        if (response == "") response = "NO OBJECT FOUND MATCHING " + findID;
                        break;
                    }
                case "follow":
                    {
                        if (msg.Length == 2 && msg[1].ToLower() == "off")
                        {
                            Session.Settings.FollowName = null;
                            response = "Stopped following";
                        }
                        else if (msg.Length > 1)
                        {
                            if (Follow(details)) response = "Following " + Session.Settings.FollowName + "...";
                            else
                            {
                                response = "Error: Avatar not found";
                                Session.Settings.FollowName = null;
                            }
                        }
                    }
                    break;
                case "fly":
                    {
                        Client.Self.Status.Controls.Fly = true;
                        Client.Self.Status.SendUpdate();
                        break;
                    }
                case "fwd":
                    {
                        if (details == null) response = "Usage: /fwd <seconds>";
                        else MoveAvatar((int)(1000 * float.Parse("0"+details)),true,false,false,false,false,false);
                        break;
                    }
                case "back":
                    {
                        if (details == null) response = "Usage: /back <seconds>";
                        else MoveAvatar((int)(1000 * float.Parse("0" + details)), false, true, false, false, false, false);
                        break;
                    }
                case "left":
                    {
                        if (details == null) response = "Usage: /left <seconds>";
                        else MoveAvatar((int)(1000 * float.Parse("0" + details)), false, false, true, false, false, false);
                        break;
                    }
                case "right":
                    {
                        if (details == null) response = "Usage: /right <seconds>";
                        else MoveAvatar((int)(1000 * float.Parse("0" + details)), false, false, false, true, false, false);
                        break;
                    }
                case "goto":
                    {
                        if (msg.Length < 3) response = "Usage: /goto <X> <Y> [Z]";
                        else
                        {
                            ulong x = (ulong)Session.RegionX + ulong.Parse(msg[1]);
                            ulong y = (ulong)Session.RegionY + ulong.Parse(msg[2]);
                            float z = Client.Self.Position.Z;
                            if (msg.Length > 3) z = float.Parse(msg[3]);
                            Client.Self.AutoPilot(x, y, z);
                        }
                        break;
                    }
                case "help":
                    {
                        if (!console) response = "Help is only available from the console.";
                        else Help(details);
                        break;
                    }
                case "im":
                    {
                        if (msg.Length > 2)
                        {
                            Client.Self.InstantMessage(new LLUUID(msg[1]), details);
                            response = "Message sent.";
                        }
                        break;
                    }
                case "inventory":
                    {
                        if (msg.Length > 2)
                        {
                            FetchInventoryPacket p = new FetchInventoryPacket();
                            p.AgentData.AgentID = Client.Network.AgentID;
                            p.AgentData.SessionID = Client.Network.SessionID;
                            FetchInventoryPacket.InventoryDataBlock data = new FetchInventoryPacket.InventoryDataBlock();
                            data.ItemID = (LLUUID)msg[1];
                            data.OwnerID = Client.Network.AgentID;
                            Client.Network.SendPacket(p);
                        }

                        break;
                    }
                case "land":
                    {
                        Client.Self.Status.Controls.Fly = false;
                        Client.Self.Status.SendUpdate();
                        break;
                    }
                case "listen":
                    {
                        Session.Settings.Quiet = false;
                        response = "Displaying object/avatar chat.";
                        break;
                    }
                case "me":
                    {
                        Client.Self.Chat("/me " + details, 0, MainAvatar.ChatType.Normal);
                        break;
                    }
                case "pay":
                    {
                        int amount;
                        if (msg.Length < 3) return;
                        if (!int.TryParse(msg[1], out amount)) return;
                        Client.Self.GiveMoney((LLUUID)msg[2], amount, "");
                        response = "Payment sent to " + msg[2] + ".";
                        break;
                    }
                case "payme":
                    {
                        if (console) Client.Self.GiveMoney(Session.Settings.MasterID, int.Parse(msg[1]), "");
                        else Client.Self.GiveMoney(fromAgentID, int.Parse(msg[1]), "");
                        response = "Payment sent.";
                        break;
                    }
                case "ping":
                    {
                        Client.Self.InstantMessage(fromAgentID, "pong", imSessionID);
                        break;
                    }
                case "quiet":
                    {
                        Session.Settings.Quiet = true;
                        response = "Stopped listening to chat.";
                        break;
                    }
                case "re":
                    {
                        if (msg.Length == 1)
                        {
                            int count = Session.IMSession.Count;
                            response = count + " active IM session";
                            if (count != 1) response += "s";
                            foreach (Avatar av in Session.IMSession.Values)
                            {
                                response += "\n" + av.LocalID + ". " + av.Name;
                            }
                        }
                        else if (msg.Length > 2)
                        {
                            int windowNum;
                            if (!int.TryParse(msg[1], out windowNum) || windowNum < 1 || windowNum > Session.IMSession.Count)
                            {
                                response = "Invalid IM window number";
                            }
                            else
                            {
                                Client.Self.InstantMessage(Session.IMSession[(uint)windowNum].ID, details, Session.IMSession[(uint)windowNum].ProfileProperties.Partner);
                                response = "Message sent.";
                            }
                        }
                        break;
                    }
                case "relog":
                    {
                        response = "Relogging...";
                        logout = false;
                        Client.Network.Logout();
                        break;
                    }
                case "ride":
                    {
                        if (!RideWith(details)) Console.WriteLine(TimeStamp() + "No avatars found matching \"{0}\".", details);
                        break;
                    }
                case "run":
                    {
                        Client.Self.SetAlwaysRun(true);
                        response = "Running enabled";
                        break;
                    }
                case "say":
                    {
                        Client.Self.Chat(details, 0, MainAvatar.ChatType.Normal);
                        break;
                    }
                case "script":
                    {
                        if (msg.Length > 1) LoadScript(msg[1] + ".script");
                        else
                        {
                            if (Session.Script.Lines.Length == 0) response = "No script loaded";
                            else
                            {
                                for (int lnum = 0; lnum < Session.Script.Lines.Length; lnum++)
                                {
                                    string lstring = "" + (lnum + 1);
                                    while (lstring.Length < 3) lstring += " ";
                                    Console.ForegroundColor = System.ConsoleColor.Gray;
                                    Console.Write("{0}: ", lstring);
                                    if (lnum < Session.Script.CurrentStep) Console.ForegroundColor = System.ConsoleColor.DarkCyan;
                                    else if (lnum > Session.Script.CurrentStep) Console.ForegroundColor = System.ConsoleColor.Cyan;
                                    else
                                    {
                                        Console.ForegroundColor = System.ConsoleColor.White;
                                        uint elapsed = Helpers.GetUnixTime() - Session.Script.SleepingSince;
                                        uint remaining = (uint)(Session.Script.SleepTimer.Interval / 1000) - elapsed;
                                        if (Session.Script.SleepTimer.Enabled == true) response = "Time remaining at current step: " + remaining;
                                    }
                                    Console.WriteLine(Session.Script.Lines[lnum]);                                    
                                }
                                Console.ForegroundColor = System.ConsoleColor.Gray;
                                Console.WriteLine("");
                            }
                        }
                        break;
                    }
                case "shout":
                    {
                        Client.Self.Chat(details, 0, MainAvatar.ChatType.Shout);
                        break;
                    }
                case "stalk":
                    {
                        OnMapStalk += new MapStalkDelegate(GhettoSL_OnMapStalk);
                        Stalk(new LLUUID(msg[1]));
                        break;
                    }
                case "stats":
                    {
                        ShowStats();
                        break;
                    }
                case "teleport":
                    {
                        if (msg.Length < 2) return;
                        string simName;
                        LLVector3 tPos;
                        if (msg.Length >= 5)
                        {
                            simName = String.Join(" ", msg, 1, msg.Length - 4);
                            float x = float.Parse(msg[msg.Length - 3]);
                            float y = float.Parse(msg[msg.Length - 2]);
                            float z = float.Parse(msg[msg.Length - 1]);
                            tPos = new LLVector3(x, y, z);
                        }
                        else
                        {
                            simName = details;
                            tPos = new LLVector3(128, 128, 0);
                        }
                        if (console)
                        {
                            Console.ForegroundColor = System.ConsoleColor.Magenta;
                            Console.WriteLine(TimeStamp() + "Teleporting to {0}...", simName);
                            Console.ForegroundColor = System.ConsoleColor.Gray;
                        }
                        else Client.Self.InstantMessage(fromAgentID, "Teleporting to {0}...", simName);
                        Client.Self.Teleport(simName, tPos);
                        break;
                    }
                case "sit":
                    {

                        if (msg.Length < 2) return;
                        Client.Self.RequestSit((LLUUID)details, new LLVector3());
                        Client.Self.Sit();
                        Client.Self.Status.Controls.FinishAnim = false;
                        Client.Self.Status.Controls.Fly = false;
                        Client.Self.Status.Controls.SitOnGround = false;
                        Client.Self.Status.Controls.StandUp = false;
                        Client.Self.Status.SendUpdate();
                        break;
                    }
                case "sitg":
                    {
                        Client.Self.Status.Controls.StandUp = false;
                        Client.Self.Status.Controls.SitOnGround = true;
                        Client.Self.Status.SendUpdate();
                        break;
                    }
                case "stand":
                    {
                        Client.Self.Status.Controls.SitOnGround = false;
                        Client.Self.Status.Controls.StandUp = true;
                        Client.Self.Status.SendUpdate();
                        Client.Self.Status.Controls.StandUp = false;
                        Client.Self.Status.SendUpdate();
                        //SendAgentAnimation((LLUUID)"2408fe9e-df1d-1d7d-f4ff-1384fa7b350f", true); //stand
                        break;
                    }
                case "time":
                    {
                        response = RPGWeather();
                        break;
                    }
                case "touch":
                    {
                        LLUUID findID = (LLUUID)msg[1];
                        foreach (PrimObject prim in Session.Prims.Values)
                        {
                            if (prim.ID != findID) continue;
                            Client.Self.Touch(prim.LocalID);
                            response = "Touched object " + prim.LocalID;
                            break;
                        }
                        if (response == "") response = "Object not found: " + findID;
                        break;
                    }
                case "touchid":
                    {
                        Client.Self.Touch(uint.Parse(msg[1]));
                        break;
                    }
                case "touchspy": //FIXME!!!
                    {
                        //display the localID of prims selected by other users
                        if (msg.Length < 2) return;
                        else if (msg[1] == "on") Client.Network.RegisterCallback(PacketType.ObjectSelect, new NetworkManager.PacketCallback(OnObjectSelect));
                        else if (msg[1] == "off") Client.Network.UnregisterCallback(PacketType.ObjectSelect, new NetworkManager.PacketCallback(OnObjectSelect));
                        else return;
                        response = "Selection spying " + msg[1].ToUpper();
                        break;
                    }
                case "tp": //FIXME!!!
                    {
                        //send me a tp when I ask for one
                        StartLurePacket p = new StartLurePacket();
                        p.AgentData.AgentID = Client.Network.AgentID;
                        p.AgentData.SessionID = Client.Network.SessionID;
                        string invite = "Join me in " + Client.Network.CurrentSim.Region.Name + "!";
                        p.Info.Message = Helpers.StringToField(invite);
                        p.TargetData[0].TargetID = fromAgentID;
                        p.Info.LureType = 4;
                        Client.Network.SendPacket(p);
                        break;
                    }
                case "updates":
                    {
                        if (msg.Length < 2) response = "Usage: /updates <on|off>";
                        else if (details == "on") {
                            Session.Settings.SendUpdates = true;
                            Client.Self.Status.UpdateTimer.Start();
                            response = "Update timer ON";
                        }
                        else if (details == "off")
                        {
                            Session.Settings.SendUpdates = false;
                            Client.Self.Status.UpdateTimer.Stop();
                            response = "Update timer OFF";
                        }
                        break;
                    }
                case "walk":
                    {
                        Client.Self.SetAlwaysRun(false);
                        response = "Running disabled";
                        break;
                    }
                case "whisper":
                    {
                        Client.Self.Chat(details, 0, MainAvatar.ChatType.Whisper);
                        break;
                    }
                case "who":
                    {
                        if (Session.Avatars.Count == 0)
                        {
                            response = "No one is around";
                        }
                        else
                        {
                            HeaderWho();
                            string spaces;
                            lock (Session.Avatars)
                            {
                                LLVector3 myPos = Client.Self.Position;
                                if (Client.Self.SittingOn > 0)
                                {
                                    if (Session.Prims.ContainsKey(Client.Self.SittingOn))
                                    {
                                        myPos.X += Session.Prims[Client.Self.SittingOn].Position.X;
                                        myPos.Y += Session.Prims[Client.Self.SittingOn].Position.Y;
                                        myPos.Z += Session.Prims[Client.Self.SittingOn].Position.Z;
                                    }
                                    else myPos = new LLVector3(0f,0f,0f);
                                }

                                foreach (Avatar a in Session.Avatars.Values)
                                {

                                    LLVector3 avPos;
                                    string pos;

                                    if (a.SittingOn < 1)
                                    {
                                        avPos = a.Position;
                                        pos = " <" + (int)avPos.X + "," + (int)avPos.Y + "," + (int)avPos.Z + ">";
                                    }
                                    else if (!Session.Prims.ContainsKey(a.SittingOn) || !Session.Prims.ContainsKey(Client.Self.SittingOn))
                                    {
                                        avPos = new LLVector3(0f, 0f, 0f);
                                        pos = " <???>";
                                    }
                                    else
                                    {
                                        avPos = Session.Prims[a.SittingOn].Position + a.Position;
                                        pos = " <" + (int)avPos.X + "," + (int)avPos.Y + "," + (int)avPos.Z + ">";
                                    }

                                    if (a.SittingOn > 0)
                                    {
                                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                                        Console.Write("~");
                                    }
                                    else
                                    {
                                        Console.Write(" ");
                                    }

                                    spaces = ""; for (int sc = a.Name.Length; sc < 20; sc++) spaces += " ";
                                    Console.ForegroundColor = ConsoleColor.White;
                                    Console.Write(a.Name + spaces);

                                    string dist = "(" + (int)Helpers.VecDist(Client.Self.Position, avPos) + "m)";
                                    spaces = ""; for (int sc = dist.Length; sc < 6; sc++) spaces += " ";
                                    Console.ForegroundColor = ConsoleColor.Gray;
                                    Console.Write(" " + dist + spaces);

                                    spaces = ""; for (int sc = pos.Length; sc < 14; sc++) spaces += " ";
                                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                                    Console.Write(" " + pos + spaces);

                                    Console.ForegroundColor = ConsoleColor.DarkGray;
                                    Console.Write(" " + a.ID + "\r\n");
                                }
                            }
                            Footer();
                        }
                        break;
                    }
            }
            if (response == "") return;
            else if (console)
            {
                Console.ForegroundColor = System.ConsoleColor.Blue;
                Console.WriteLine(TimeStamp() + response);
                Console.ForegroundColor = System.ConsoleColor.Gray;
            }
            else Client.Self.InstantMessage(fromAgentID, response, imSessionID);
        }

        void GhettoSL_OnMapStalk(LLUUID stalked, Location location)
        {
            Console.WriteLine("Stalking " + stalked + " to " + location);
            TeleportToLocation(location);
        }

    }
}

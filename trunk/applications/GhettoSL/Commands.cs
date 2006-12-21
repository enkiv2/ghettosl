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
        void ParseCommand(bool console, string message, string fromAgentName, LLUUID fromAgentID, LLUUID imSessionID)
        {
            if (message.Length == 0) return;
            char[] splitChar = { ' ' };
            string[] msg = message.Split(splitChar);
            if (msg[0] == null || msg[0] == "") return;

            string command = msg[0].ToLower();
            if (command.Substring(0, 1) == "/") command = command.Substring(1);
            else if (console)
            {
                Client.Self.Chat(message, 0, MainAvatar.ChatType.Normal);
                return;
            }

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
                case "button1":
                    {
                        Client.Self.Touch(9835045);
                        break;
                    }
                case "button2":
                    {
                        Client.Self.Touch(9835044);
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
                case "camp":
                    {
                        uint localID = FindObjectByText(details.ToLower());
                        if (localID > 0)
                        {
                            response = "Match found. Camping...";
                            Client.Self.RequestSit(prims[localID].ID, new LLVector3(0, 0, 0));
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
                        foreach (PrimObject prim in prims.Values)
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
                case "face":
                    {
                        LLUUID findID = (LLUUID)msg[1];
                        foreach (PrimObject prim in prims.Values)
                        {
                            if (prim.ID != findID) continue;
                            LLVector3 targetPos = new LLVector3(prim.Position.X, prim.Position.Y, prim.Position.Z + 10);
                            LLQuaternion between = Helpers.RotBetween(Client.Self.Position, prim.Position);
                            response = "FACING " + targetPos + " " + between;
                            //FIXME!!!

                            break;
                        }
                        if (response == "") response = "NO OBJECT FOUND MATCHING " + findID;
                        break;
                    }
                case "follow":
                    {
                        if (msg.Length == 2 && msg[1].ToLower() == "off")
                        {
                            followName = null;
                            response = "Stopped following";
                        }
                        else if (msg.Length > 1)
                        {
                            if (Follow(details)) response = "Following " + followName + "...";
                            else
                            {
                                response = "Error: Avatar not found";
                                followName = null;
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
                            ulong x = (ulong)regionX + ulong.Parse(msg[1]);
                            ulong y = (ulong)regionY + ulong.Parse(msg[2]);
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
                            Client.Self.InstantMessage((LLUUID)msg[1], msg[2]);
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
                        quiet = false;
                        Client.Self.OnChat += new ChatCallback(OnChatEvent);
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
                        Client.Self.GiveMoney((LLUUID)msg[2], int.Parse(msg[1]), "");
                        response = "Payment sent to " + msg[2] + ".";
                        break;
                    }
                case "payme":
                    {
                        if (console) Client.Self.GiveMoney(masterID, int.Parse(msg[1]), "");
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
                        quiet = true;
                        response = "Stopped listening to chat.";
                        Client.Self.OnChat -= new ChatCallback(OnChatEvent);
                        break;
                    }
                case "re":
                    {
                        if (msg.Length == 1)
                        {
                            int count = imWindows.Count;
                            response = count + " active IM session";
                            if (count != 1) response += "s";
                            foreach (Avatar av in imWindows.Values)
                            {
                                response += "\n" + av.LocalID + ". " + av.Name;
                            }
                        }
                        else if (msg.Length == 2)
                        {
                            int isNumeric;
                            if (!int.TryParse(msg[1], out isNumeric))
                            {
                                response = "Invalid IM window number";
                            }
                            else
                            {
                                uint index = (uint)(-1 + int.Parse(msg[1]));
                                if (index < 0 || index >= imWindows.Count) response = "Invalid IM window number";
                                else
                                {
                                    Client.Self.InstantMessage(imWindows[index].ID, details, imWindows[index].PartnerID);
                                    response = "Message sent.";
                                }
                            }
                        }
                        break;
                    }
                case "relog":
                    {
                        response = "Relogging...";
                        Client.Network.Logout();
                        Thread.Sleep(3000);
                        while (!Login()) Thread.Sleep(5000);
                        break;
                    }
                case "ride":
                    {
                        if (!RideWith(details)) Console.WriteLine("* No avatars found matching \"{0}\".", details);
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
                        if (msg.Length > 0) LoadScript(msg[1] + ".script");
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
                            Console.WriteLine("* Teleporting to {0}...", simName);
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
                        foreach (PrimObject prim in prims.Values)
                        {
                            if (prim.ID != findID) continue;
                            Client.Self.Touch(prim.LocalID);
                            response = "TOUCHED OBJECT " + prim.LocalID;
                            break;
                        }
                        if (response == "") response = "NO OBJECT FOUND MATCHING " + findID;
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
                            sendUpdates = true;
                            Client.Self.Status.UpdateTimer.Start();
                            response = "Update timer ON";
                        }
                        else if (details == "off")
                        {
                            sendUpdates = false;
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
                        if (avatars.Count == 0)
                        {
                            response = "No one is around";
                        }
                        else
                        {
                            HeaderWho();
                            string spaces;
                            foreach (Avatar a in avatars.Values)
                            {
                                spaces = ""; for (int sc = a.Name.Length; sc < 19; sc++) spaces += " ";
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.Write(" "+a.Name + spaces);
                                
                                string dist = "(" + (int)Helpers.VecDist(Client.Self.Position, a.Position) + "m)";
                                spaces = ""; for (int sc = dist.Length; sc < 6; sc++) spaces += " ";
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.Write(" " + dist + spaces);

                                
                                string pos = " <" + (int)a.Position.X + "," + (int)a.Position.Y +"," + (int)a.Position.Z+">";
                                spaces = ""; for (int sc = pos.Length; sc < 14; sc++) spaces += " ";
                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                Console.Write(" " + pos + spaces);

                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                Console.Write(" " + a.ID + "\r\n");
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
                Console.WriteLine(TimeStamp() + "* " + response);
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

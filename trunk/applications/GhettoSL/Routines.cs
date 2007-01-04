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
using libsecondlife.AssetSystem;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ghetto
{
    partial class GhettoSL
    {

        void AcknowledgePayment(string agentName, int amount)
        {
            LLUUID agentID = new LLUUID();
            string msg = "Unable to locate " + agentName + ". Payment: L$" + amount;
            foreach (Avatar av in Session.Avatars.Values)
            {
                if (av.Name != agentName) continue;
                agentID = av.ID;

                //uncomment to whisper payment info on a secret channel
                //Client.Self.Chat(av.Name+", "+av.ID+", "+balance, 8414263, MainAvatar.ChatType.Whisper);

                msg = "Found avatar " + av.Name + ": " + av.ID;
                break;
            }
            if (amount > 0)
            {
                Session.MoneyReceived += amount;
                Console.WriteLine(TimeStamp() + msg);
            }
            else
            {
                Session.MoneySpent -= amount;
            }
            foreach (KeyValuePair<string, ScriptEvent> pair in Session.Script.Events)
            {
                if (pair.Value.Type == (int)EventTypes.GetMoney && amount > 0)
                {
                    string[] cmdScript = { ParseScriptVariables(pair.Value.Command, agentName, agentID, amount, "") };
                    ParseScriptLine(cmdScript, 0);
                }
                else if (pair.Value.Type == (int)EventTypes.GiveMoney && amount < 0)
                {
                    string[] cmdScript = { ParseScriptVariables(pair.Value.Command, agentName, agentID, amount, "") };
                    ParseScriptLine(cmdScript, 0);
                }
            }

        }

        void CreateMessageWindow(LLUUID fromAgentID, string fromAgentName, byte dialog, LLUUID imSessionID)
        {
            //check for existing session
            foreach (Avatar av in Session.IMSession.Values)
            {
                if (av.ID == fromAgentID) return;
            }
            //create new session
            Avatar newAvatar = new Avatar();
            newAvatar.ID = fromAgentID;
            newAvatar.Name = fromAgentName;
            newAvatar.ProfileProperties.Partner = imSessionID; //hack - imSessionID, not PartnerID
            newAvatar.LocalID = (uint)(Session.IMSession.Count + 1); //hack - windowID, not LocalID
            Session.IMSession.Add((uint)Session.IMSession.Count, newAvatar);
            Console.WriteLine(TimeStamp() + "Created IM window {0} for user {1}.", newAvatar.LocalID, newAvatar.Name);
        }

        uint FindObjectByText(string textValue)
        {
            Session.Settings.CampChairMatchText = textValue;
            uint localID = 0;
            foreach (PrimObject prim in Session.Prims.Values)
            {
                int len = Session.Settings.CampChairMatchText.Length;
                string match = prim.Text.Replace("\n", ""); //Strip newlines
                if (match.Length < len) continue; //Text is too short to be a match
                else if (match.Substring(0, len).ToLower() == Session.Settings.CampChairMatchText)
                {
                    localID = prim.LocalID;
                    break;
                }
            }
            return localID;
        }

        bool Follow(string name)
        {
            string findName = name.ToLower();
            foreach (Avatar av in Session.Avatars.Values)
            {
                if (av.Name.Length < findName.Length) continue; //Name is too short to be a match
                else if (av.Name.ToLower().Substring(0, findName.Length) == findName)
                {
                    Session.Settings.FollowName = av.Name;
                    if (Helpers.VecDist(av.Position, Client.Self.Position) > 4)
                    {
                        //GridRegion region = Client.Network.CurrentSim.Region.GridRegionData;
                        Client.Self.AutoPilot((ulong)(av.Position.X + Session.RegionX), (ulong)(av.Position.Y + Session.RegionY), av.Position.Z);
                        Thread.Sleep(100);
                        Client.Self.Status.SendUpdate();
                    }
                    else
                    {
                        Client.Self.Status.SendUpdate();
                    }
                    return true;
                }
            }
            return false;
        }

        void InitializeCamera()
        {
            Client.Self.Status.Camera.Far = 96.0f;
            Client.Self.Status.Camera.CameraAtAxis = new LLVector3(0, 0, 0);
            Client.Self.Status.Camera.CameraCenter = new LLVector3(0, 0, 0);
            Client.Self.Status.Camera.CameraLeftAxis = new LLVector3(0, 0, 0);
            Client.Self.Status.Camera.CameraUpAxis = new LLVector3(0, 0, 0);
            Client.Self.Status.Camera.HeadRotation = new LLQuaternion(0, 0, 0, 1);
            Client.Self.Status.Camera.BodyRotation = new LLQuaternion(0, 0, 0, 1);
        }

        void InitializeUserSession(UserSession session)
        {
            Session = session;
            Session.IMSession = new Dictionary<uint, Avatar>();
            Session.LastAppearance = new AgentSetAppearancePacket();
            Session.Appearances = new Dictionary<LLUUID, AvatarAppearancePacket>();
            Session.Avatars = new Dictionary<uint, Avatar>();
            Session.Friends = new Dictionary<LLUUID, Avatar>();
            Session.Prims = new Dictionary<uint, PrimObject>();
            Session.StartTime = Helpers.GetUnixTime();
            Session.Script.Events = new Dictionary<string, ScriptEvent>();
            Session.Settings.SendUpdates = true;
        }


        bool Login()
        {
            Console.Title = "GhettoSL - Logging in...";
            Console.ForegroundColor = System.ConsoleColor.White;
            Console.WriteLine(TimeStamp() + "Logging in as " + Session.Settings.FirstName + " " + Session.Settings.LastName + "...");
            Console.ForegroundColor = System.ConsoleColor.Gray;

            //Attempt to log in
            if (!Client.Network.Login(Session.Settings.FirstName, Session.Settings.LastName, Session.Settings.Password, "GhettoSL", "ghetto@obsoleet.com"))
            {
                Console.WriteLine("Login failed.");
                return false;
            }

            Console.Title = Client.Self.FirstName + " " + Client.Self.LastName + " - GhettoSL";

            //Succeeded - Wait for simulator name or disconnection
            Simulator sim = Client.Network.CurrentSim;
            while (Client.Network.Connected && (!sim.Connected || sim.Region.Name == "" || Client.Grid.SunDirection.X == 0))
            {
                Thread.Sleep(100);
            }

            //Halt if disconnected
            if (!Client.Network.Connected) return false;

            //We are in!
            Console.ForegroundColor = System.ConsoleColor.White;
            Console.WriteLine(TimeStamp() + RPGWeather());
            Console.WriteLine(TimeStamp() + "Location: " + Client.Self.Position);
            Console.ForegroundColor = System.ConsoleColor.Gray;

            //Fix the "bot squat" animation
            Client.Self.Status.SendUpdate();

            return true;
        }


        void MoveAvatar(int time, bool fwd, bool back, bool left, bool right, bool up, bool down)
        {
            Client.Self.Status.Controls.AtPos = fwd;
            Client.Self.Status.Controls.AtNeg = back;
            Client.Self.Status.Controls.LeftPos = left;
            Client.Self.Status.Controls.LeftNeg = right;
            Client.Self.Status.Controls.UpPos = up;
            Client.Self.Status.Controls.UpNeg = down;
            Client.Self.Status.SendUpdate();
            Thread.Sleep(time);
            Client.Self.Status.Controls.AtPos = false;
            Client.Self.Status.Controls.AtNeg = false;
            Client.Self.Status.Controls.LeftPos = false;
            Client.Self.Status.Controls.LeftNeg = false;
            Client.Self.Status.Controls.UpPos = false;
            Client.Self.Status.Controls.UpNeg = false;
            Client.Self.Status.SendUpdate();
        }


        void SendAgentAnimation(LLUUID animID, bool start)
        {
            AgentAnimationPacket p = new AgentAnimationPacket();
            AgentAnimationPacket.AnimationListBlock[] animList = new AgentAnimationPacket.AnimationListBlock[1];
            animList[0] = new AgentAnimationPacket.AnimationListBlock();
            animList[0].AnimID = animID;
            animList[0].StartAnim = true;
            p.AnimationList = animList;
            p.AgentData.AgentID = Client.Network.AgentID;
            p.AgentData.SessionID = Client.Network.SessionID;
            Client.Network.SendPacket(p);
        }


        bool RideWith(string name)
        {
            string findName = name.ToLower();
            foreach (Avatar av in Session.Avatars.Values)
            {

                if (av.Name.ToLower().Substring(0, findName.Length) == findName)
                {
                    if (av.SittingOn > 0)
                    {
                        Console.WriteLine(TimeStamp() + "Riding with " + av.Name + ".");
                        Client.Self.RequestSit(Session.Prims[av.SittingOn].ID, new LLVector3(0, 0, 0));
                        Client.Self.Sit();
                        return true;
                    }
                    else
                    {
                        Console.WriteLine(TimeStamp() + av.Name + " is not sitting.");
                        return false;
                    }
                }
            }
            return false;
        }

        void UpdateAppearance()
        {
            AppearanceManager aManager;
            aManager = new AppearanceManager(Client);
            aManager.SendAgentSetAppearance();
        }

        string Duration(uint seconds)
        {
            string d = "";
            uint remaining = seconds;
            uint years = remaining % 31556926;
            if (years > 0)
            {
                d += years + "y ";
                remaining -= years * 31556926;
            }
            uint months = remaining % 2629744;
            if (months > 0)
            {
                d += months + "m ";
                remaining -= months * 2629744;
            }
            uint weeks = remaining % 604800;
            if (weeks > 0)
            {
                d += weeks + "w ";
                remaining -= weeks % 604800;
            }
            uint days = remaining % 86400;
            if (days > 0)
            {
                d += days + "d ";
                remaining -= days * 86400;
            }
            uint hours = remaining % 3600;
            if (hours > 0)
            {
                d += hours + "h ";
                remaining -= hours * 3600;
            }
            uint minutes = remaining % 60;
            if (hours > 0)
            {
                d += minutes + "m ";
                remaining -= minutes * 60;
            }
            d += remaining + "s";

            return d;
        }

    }
}

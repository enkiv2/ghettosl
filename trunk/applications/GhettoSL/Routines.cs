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


        void AcknowledgePayment(string agentName, int amount)
        {
            foreach (Avatar av in avatars.Values)
            {
                if (av.Name != agentName) continue;
                Console.WriteLine("* RECEIVED PAYMENT FROM " + av.ID);
                //uncomment to whisper payment info on a secret channel
                //Client.Self.Chat(av.Name+", "+av.ID+", "+amount, 8414263, MainAvatar.ChatType.Whisper);
                return;
            }
            Console.WriteLine("* RECEIVED UNIDENTIFIABLE PAYMENT FROM " + agentName + ": L$" + amount);
        }


        void CreateMessageWindow(LLUUID fromAgentID, string fromAgentName, byte dialog, LLUUID imSessionID)
        {
            bool hasWindow = false;
            foreach (Avatar av in imWindows.Values)
            {
                if (av.ID == fromAgentID)
                {
                    hasWindow = true;
                    break;
                }
            }
            Avatar newAvatar = new Avatar();
            newAvatar.ID = fromAgentID;
            newAvatar.Name = fromAgentName;
            newAvatar.PartnerID = imSessionID; //hack
            newAvatar.LocalID = (uint)(imWindows.Count + 1); //hack
            if (!hasWindow) imWindows.Add((uint)imWindows.Count, newAvatar);
        }


        bool Follow(string name)
        {
            string findName = name.ToLower();
            foreach (Avatar av in avatars.Values)
            {
                if (av.Name.Length < findName.Length) continue; //Name is too short to be a match
                else if (av.Name.ToLower().Substring(0, findName.Length) == findName)
                {
                    followName = av.Name;
                    if (Helpers.VecDist(av.Position, Client.Self.Position) > 4)
                    {
                        //GridRegion region = Client.Network.CurrentSim.Region.GridRegionData;
                        Client.Self.AutoPilot((ulong)(av.Position.X + regionX), (ulong)(av.Position.Y + regionY), av.Position.Z);
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
            foreach (Avatar av in avatars.Values)
            {

                if (av.Name.ToLower().Substring(0, findName.Length) == findName)
                {
                    if (av.SittingOn > 0)
                    {
                        Console.WriteLine("* Riding with " + av.Name + ".");
                        Client.Self.RequestSit(prims[av.SittingOn].ID, new LLVector3(0, 0, 0));
                        Client.Self.Sit();
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("* " + av.Name + " is not sitting.");
                        return false;
                    }
                }
            }
            return false;
        }

    }
}

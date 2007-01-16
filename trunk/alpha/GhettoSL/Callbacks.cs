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

namespace ghetto
{
    public class CallbackManager
    {

        public GhettoSL.UserSession Session;

        public CallbackManager(GhettoSL.UserSession session)
        {
            Session = session;
            Session.Client.Network.OnConnected += new NetworkManager.ConnectedCallback(Network_OnConnected);
            Session.Client.Network.OnSimDisconnected += new NetworkManager.SimDisconnectCallback(Network_OnSimDisconnected);
            Session.Client.Objects.OnNewPrim += new ObjectManager.NewPrimCallback(Objects_OnNewPrim);
            Session.Client.Objects.OnObjectKilled += new ObjectManager.KillObjectCallback(Objects_OnObjectKilled);
            Session.Client.Objects.OnPrimMoved += new ObjectManager.PrimMovedCallback(Objects_OnPrimMoved);
            Session.Client.Self.OnChat += new MainAvatar.ChatCallback(Self_OnChat);
            Session.Client.Self.OnInstantMessage += new MainAvatar.InstantMessageCallback(Self_OnInstantMessage);

            Session.Client.Network.RegisterCallback(PacketType.AlertMessage, new NetworkManager.PacketCallback(Callback_AlertMessage));
            Session.Client.Network.RegisterCallback(PacketType.MoneyBalanceReply, new NetworkManager.PacketCallback(Callback_MoneyBalanceReply));
            Session.Client.Network.RegisterCallback(PacketType.TeleportFinish, new NetworkManager.PacketCallback(Callback_TeleportFinish));
        }

        void Callback_AlertMessage(Packet packet, Simulator sim)
        {
            AlertMessagePacket p = (AlertMessagePacket)packet;
            Display.AlertMessage(Session.SessionNumber, Helpers.FieldToString(p.AlertData.Message));
        }

        void Callback_MoneyBalanceReply(Packet packet, Simulator simulator)
        {
            MoneyBalanceReplyPacket reply = (MoneyBalanceReplyPacket)packet;
            string desc = Helpers.FieldToString(reply.MoneyData.Description);
            string name = "";
            int amount = 0;

            Session.Balance = reply.MoneyData.MoneyBalance;

            char[] splitChar = { ' ' };
            string[] msg = desc.Split(splitChar);

            if (msg.Length <= 4 || msg[4].Length < 4 || !int.TryParse(msg[4].Substring(2, msg[4].Length - 3), out amount))
            {
                if (desc.Length > 0) Display.Error(Session.SessionNumber, "Unexpected MoneyDataBlock.Description:" + desc);
            }

            else if (msg[0] + " " + msg[1] == "You paid")
            {
                Session.MoneySpent += amount;
                name = msg[2] + " " + msg[3];
                amount *= -1;
            }
            else if (msg[2] + " " + msg[3] == "paid you")
            {
                Session.MoneyReceived += amount;
                name = msg[0] + " " + msg[1];
            }

            Display.Balance(Session.SessionNumber, reply.MoneyData.MoneyBalance, amount, name, desc);
        }

        void Callback_TeleportFinish(Packet packet, Simulator sim)
        {
            //FIXME - add scripted TeleportFinish event check
            TeleportFinishPacket p = (TeleportFinishPacket)packet;
            Display.TeleportFinished(Session.SessionNumber, sim.Region.Name);
            Session.UpdateAppearance();
        }

        void Network_OnConnected(object sender)
        {
            //FIXME - add scripted Connect event check


            Display.Connected(Session.SessionNumber);

            Session.Settings.StartRegion = "";

            Session.UpdateAppearance();

            Session.Client.Self.Status.UpdateTimer.Start();

            Session.Client.Grid.AddEstateSims();

            //Retrieve offline IMs
            //FIXME - Add Client.Self.RetrieveInstantMessages() to core
            RetrieveInstantMessagesPacket p = new RetrieveInstantMessagesPacket();
            p.AgentData.AgentID = Session.Client.Network.AgentID;
            p.AgentData.SessionID = Session.Client.Network.SessionID;
            Session.Client.Network.SendPacket(p);
        }

        void Network_OnSimDisconnected(Simulator simulator, NetworkManager.DisconnectType reason)
        {
            Display.Disconnected(Session.SessionNumber, reason.ToString());
        }

        void Objects_OnPrimMoved(Simulator simulator, PrimUpdate prim, ulong regionHandle, ushort timeDilation)
        {
            lock (Session.Prims)
            {
                if (Session.Prims.ContainsKey(prim.LocalID))
                {
                    Session.Prims[prim.LocalID].Position = prim.Position;
                    Session.Prims[prim.LocalID].Rotation = prim.Rotation;
                }
            }
        }

        void Objects_OnNewPrim(Simulator simulator, PrimObject prim, ulong regionHandle, ushort timeDilation)
        {
            lock (Session.Prims)
            {
                Session.Prims[prim.LocalID] = prim;
            }
        }

        void Objects_OnObjectKilled(Simulator simulator, uint objectID)
        {
            lock (Session.Prims)
            {
                if (Session.Prims.ContainsKey(objectID))
                    Session.Prims.Remove(objectID);
            }
        }

        void Self_OnChat(string message, byte audible, byte chatType, byte sourceType, string fromName, LLUUID id, LLUUID ownerid, LLVector3 position)
        {
            if (chatType > 3 || audible < 1) return;
            //FIXME - add script chat event check



            if (!Session.Settings.DisplayChat) return;

            char[] splitChar = { ' ' };
            string[] msg = message.Split(splitChar);

            bool action;
            if (msg[0].ToLower() == "/me")
            {
                action = true;
                message = String.Join(" ", msg, 1, msg.Length - 1);
            }
            else action = false;
            
            Display.Chat(fromName, message, action, chatType, sourceType);
        }


        void Self_OnInstantMessage(LLUUID fromID, string fromName, LLUUID toAgentID, uint parentEstateID, LLUUID regionID, LLVector3 position, byte dialog, bool groupIM, LLUUID imSessionID, DateTime timestamp, string message, byte offline, byte[] binaryBucket)
        {
            //FIXME - add script im event check



            if (dialog == (int)MainAvatar.InstantMessageDialog.RequestTeleport)
            {
                if (fromID == Session.Settings.MasterID || (message.Length > 0 && message == Session.Settings.PassPhrase))
                {
                    Session.Client.Self.TeleportLureRespond(fromID, true);
                }
            }
            else if (dialog == (int)MainAvatar.InstantMessageDialog.MessageFromObject)
            {
                Display.InstantMessage(Session.SessionNumber, true, dialog, fromName, message);
            }
            else
            {
                if (!Session.IMSessions.ContainsKey(fromID))
                {
                    Session.IMSessions.Add(fromID, new GhettoSL.IMSession(imSessionID, fromName));
                }
                Display.InstantMessage(Session.SessionNumber, false, dialog, fromName, message);
                if (fromID == Session.Settings.MasterID) ScriptSystem.ParseCommand(Session.SessionNumber, message, false, true);
            }
        }

    }
}
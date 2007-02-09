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
            Session.Client.Inventory.OnInventoryItemReceived += new libsecondlife.InventorySystem.InventoryManager.On_InventoryItemReceived(Inventory_OnInventoryItemReceived);
            Session.Client.Network.OnConnected += new NetworkManager.ConnectedCallback(Network_OnConnected);
            Session.Client.Network.OnCurrentSimChanged += new NetworkManager.CurrentSimChangedCallback(Network_OnCurrentSimChanged);
            //Session.Client.Network.OnSimDisconnected += new NetworkManager.SimDisconnectCallback(Network_OnSimDisconnected);

            Session.Client.Network.OnDisconnected += new NetworkManager.DisconnectCallback(Network_OnDisconnected);

            Session.Client.Objects.OnNewAvatar += new ObjectManager.NewAvatarCallback(Objects_OnNewAvatar);

            Session.Client.Objects.OnAvatarSitChanged += new ObjectManager.AvatarSitChanged(Objects_OnAvatarSitChanged);
            Session.Client.Objects.OnNewPrim += new ObjectManager.NewPrimCallback(Objects_OnNewPrim);
            Session.Client.Objects.OnObjectKilled += new ObjectManager.KillObjectCallback(Objects_OnObjectKilled);
            Session.Client.Objects.OnObjectUpdated += new ObjectManager.ObjectUpdatedCallback(Objects_OnObjectUpdated);
            session.Client.OnLogMessage += new SecondLife.LogCallback(Client_OnLogMessage);
            Session.Client.Self.OnChat += new MainAvatar.ChatCallback(Self_OnChat);
            Session.Client.Self.OnScriptDialog += new MainAvatar.ScriptDialogCallback(Self_OnScriptDialog);
            Session.Client.Self.OnInstantMessage += new MainAvatar.InstantMessageCallback(Self_OnInstantMessage);

            Session.Client.Network.RegisterCallback(PacketType.AlertMessage, new NetworkManager.PacketCallback(Callback_AlertMessage));
            Session.Client.Network.RegisterCallback(PacketType.MoneyBalanceReply, new NetworkManager.PacketCallback(Callback_MoneyBalanceReply));


            //Session.Client.Network.RegisterCallback(PacketType.TeleportFinish, new NetworkManager.PacketCallback(Callback_TeleportFinish));
            Session.Client.Self.OnTeleport += new MainAvatar.TeleportCallback(Self_OnTeleport);
        }

        void Objects_OnNewAvatar(Simulator simulator, Avatar avatar, ulong regionHandle, ushort timeDilation)
        {
            lock (Session.Avatars)
            {
                if (!Session.Avatars.ContainsKey(avatar.LocalID)) Session.Avatars.Add(avatar.LocalID, avatar);
                else Session.Avatars[avatar.LocalID] = avatar;
            }
        }

        void Network_OnDisconnected(NetworkManager.DisconnectType reason, string message)
        {
            Display.Disconnected(Session.SessionNumber, reason, message);
            Session.Avatars = new Dictionary<uint, Avatar>();
        }

        void Self_OnTeleport(string message, MainAvatar.TeleportStatus status, MainAvatar.TeleportFlags flags)
        {
            if (status == MainAvatar.TeleportStatus.Finished)
            {
                Display.TeleportFinished(Session.SessionNumber);
                Session.Prims = new Dictionary<uint, Primitive>();
                Session.Avatars = new Dictionary<uint, Avatar>();
                Session.UpdateAppearance(); //probably never needed
                Dictionary<string, string> identifiers = new Dictionary<string, string>();
                identifiers.Add("$message", message);
                ScriptSystem.TriggerEvents(Session.SessionNumber, ScriptSystem.EventTypes.TeleportFinish, identifiers);
            }
            else Console.WriteLine("tp: " + status + " - " + message + " - " + flags);
        }

        void Objects_OnObjectUpdated(Simulator simulator, ObjectUpdate update, ulong regionHandle, ushort timeDilation)
        {
            lock (Session.Prims)
            {
                if (Session.Prims.ContainsKey(update.LocalID))
                {
                    Session.Prims[update.LocalID].Position = update.Position;
                    Session.Prims[update.LocalID].Rotation = update.Rotation;
                }
            }
            lock (Session.Avatars)
            {
                if (Session.Avatars.ContainsKey(update.LocalID))
                {
                    Session.Avatars[update.LocalID].Position = update.Position;
                    Session.Avatars[update.LocalID].Rotation = update.Rotation;
                }
            }
        }

        void Client_OnLogMessage(string message, Helpers.LogLevel level)
        {
            Display.LogMessage(Session.SessionNumber, message, level);
        }

        void Objects_OnAvatarSitChanged(Simulator simulator, uint sittingOn)
        {
            Display.SitChanged(Session.SessionNumber, sittingOn);
            if (sittingOn > 0) ScriptSystem.TriggerEvents(Session.SessionNumber, ScriptSystem.EventTypes.Sit, null);
            else ScriptSystem.TriggerEvents(Session.SessionNumber, ScriptSystem.EventTypes.Unsit, null);
        }

        void Network_OnCurrentSimChanged(Simulator PreviousSimulator)
        {
            //FIXME - add event?
            Display.SimChanged(Session.SessionNumber, PreviousSimulator, Session.Client.Network.CurrentSim);
            Session.Avatars = new Dictionary<uint, Avatar>();
        }

        void Inventory_OnInventoryItemReceived(LLUUID fromAgentID, string fromAgentName, uint parentEstateID, LLUUID regionID, LLVector3 position, DateTime timestamp, libsecondlife.InventorySystem.InventoryItem item)
        {
            Display.InventoryItemReceived(Session.SessionNumber, fromAgentID, fromAgentName, parentEstateID, regionID, position, timestamp, item);
            Dictionary<string,string> identifiers = new Dictionary<string,string>();
            identifiers.Add("$name", fromAgentName);
            identifiers.Add("$id", fromAgentID.ToString());
            identifiers.Add("$item", item.Name);
            identifiers.Add("$itemid", item.ItemID.ToString());
            ScriptSystem.TriggerEvents(Session.SessionNumber, ScriptSystem.EventTypes.GetItem, identifiers);
        }

        void Self_OnScriptDialog(string message, string objectName, LLUUID imageID, LLUUID objectID, string firstName, string lastName, int chatChannel, List<string> buttons)
        {
            Session.LastDialogID = objectID;
            Session.LastDialogChannel = chatChannel;
            Display.ScriptDialog(Session.SessionNumber, message, objectName, imageID, objectID, firstName, lastName, chatChannel, buttons);

            Dictionary<string, string> identifiers = new Dictionary<string, string>();
            identifiers.Add("$name", objectName);
            identifiers.Add("$id", objectID.ToString());
            identifiers.Add("$channel", chatChannel.ToString());
            identifiers.Add("$message", message);
            ScriptSystem.TriggerEvents(Session.SessionNumber, ScriptSystem.EventTypes.ScriptDialog, identifiers);
        }

        void Callback_AlertMessage(Packet packet, Simulator sim)
        {
            AlertMessagePacket p = (AlertMessagePacket)packet;
            Display.AlertMessage(Session.SessionNumber, Helpers.FieldToString(p.AlertData.Message));
        }

        void Callback_MoneyBalanceReply(Packet packet, Simulator sim)
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
                else Display.Balance(Session.SessionNumber, Session.Balance, 0, null, null);
                return;
            }

            if (msg[0] + " " + msg[1] == "You paid")
            {
                Session.MoneySpent += amount;
                name = msg[2] + " " + msg[3];

                Dictionary<string, string> identifiers = new Dictionary<string, string>();
                identifiers.Add("$name", name);
                identifiers.Add("$amount", amount.ToString());

                amount *= -1; //you paid
                Display.Balance(Session.SessionNumber, reply.MoneyData.MoneyBalance, amount, name, desc);

                ScriptSystem.TriggerEvents(Session.SessionNumber, ScriptSystem.EventTypes.GiveMoney, identifiers);
            }

            else if (msg[2] + " " + msg[3] == "paid you")
            {
                Session.MoneyReceived += amount;
                name = msg[0] + " " + msg[1];

                Display.Balance(Session.SessionNumber, reply.MoneyData.MoneyBalance, amount, name, desc);

                Dictionary<string, string> identifiers = new Dictionary<string, string>();
                identifiers.Add("$name", name);
                identifiers.Add("$amount", amount.ToString());

                ScriptSystem.TriggerEvents(Session.SessionNumber, ScriptSystem.EventTypes.GetMoney, identifiers);
            }

        }

        /*
        void Callback_TeleportFinish(Packet packet, Simulator sim)
        {
            TeleportFinishPacket p = (TeleportFinishPacket)packet;
            Display.TeleportFinished(Session.SessionNumber, sim.Region.Name);
            Session.Prims = new Dictionary<uint, Primitive>();
            Session.UpdateAppearance(); //probably never needed

            Dictionary<string, string> identifiers = new Dictionary<string, string>();
            identifiers.Add("$region", sim.Region.Name);
            identifiers.Add("$newregion", Session.Client.Network.CurrentSim.Region.Name);
            ScriptSystem.TriggerEvents(Session.SessionNumber, ScriptSystem.EventTypes.TeleportFinish, identifiers);

        }
         * */

        void Network_OnConnected(object sender)
        {
            //change our settings to use the correct caps from now on
            Session.Settings.FirstName = Session.Client.Self.FirstName;
            Session.Settings.LastName = Session.Client.Self.LastName;

            Display.Connected(Session.SessionNumber, Session.Name);

            Session.Client.Self.RequestBalance();

            Session.UpdateAppearance();

            Session.Client.Groups.BeginGetCurrentGroups(new GroupManager.CurrentGroupsCallback(GroupsUpdatedHandler));

            Session.Client.Grid.RequestEstateSims(GridManager.MapLayerType.Terrain);

            Session.Client.Self.Status.UpdateTimer.Start();

            //Retrieve offline IMs
            //FIXME - Add Client.Self.RetrieveInstantMessages() to core
            RetrieveInstantMessagesPacket p = new RetrieveInstantMessagesPacket();
            p.AgentData.AgentID = Session.Client.Network.AgentID;
            p.AgentData.SessionID = Session.Client.Network.SessionID;
            Session.Client.Network.SendPacket(p);

            ScriptSystem.TriggerEvents(Session.SessionNumber, ScriptSystem.EventTypes.Connect, null);

        }

        //void Network_OnSimDisconnected(Simulator simulator, NetworkManager.DisconnectType reason)
        //{
        //    Display.Disconnected(Session.SessionNumber, reason.ToString());
        //    ScriptSystem.TriggerEvents(Session.SessionNumber, ScriptSystem.EventTypes.Disconnect, null);
        //}

        void GroupsUpdatedHandler(Dictionary<LLUUID, libsecondlife.Group> groups)
        {
            Session.Groups = groups;
        }

        void Objects_OnNewPrim(Simulator simulator, Primitive prim, ulong regionHandle, ushort timeDilation)
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
            lock (Session.Avatars)
            {
                if (Session.Avatars.ContainsKey(objectID))
                {
                    if (Session.Avatars[objectID].Name == Session.FollowName) Session.FollowTimer.Stop();
                    Session.Avatars.Remove(objectID);
                }
            }
        }

        void Self_OnChat(string message, byte audible, byte chatType, byte sourceType, string fromName, LLUUID id, LLUUID ownerid, LLVector3 position)
        {
            if (chatType > 3 || audible < 1) return;

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
            
            Display.Chat(Session.SessionNumber, fromName, message, action, chatType, sourceType);

            if (id == Session.Client.Network.AgentID) return;

            Dictionary<string, string> identifiers = new Dictionary<string, string>();
            identifiers.Add("$name", fromName);
            identifiers.Add("$message", message);
            identifiers.Add("$id", id.ToString());
            identifiers.Add("$ownerid", ownerid.ToString());
            identifiers.Add("$ctype", chatType.ToString());
            identifiers.Add("$stype", sourceType.ToString());
            ScriptSystem.TriggerEvents(Session.SessionNumber, ScriptSystem.EventTypes.Chat, identifiers);
        }


        void Self_OnInstantMessage(LLUUID fromID, string fromName, LLUUID toAgentID, uint parentEstateID, LLUUID regionID, LLVector3 position, byte dialog, bool groupIM, LLUUID imSessionID, DateTime timestamp, string message, byte offline, byte[] binaryBucket)
        {

            if (dialog == (int)MainAvatar.InstantMessageDialog.RequestTeleport)
            {
                Display.InfoResponse(Session.SessionNumber, "Teleport lure received from " + fromName + ": " + message);
                if (fromID == Session.Settings.MasterID || (message.Length > 0 && message == Session.Settings.PassPhrase))
                {
                    Session.Client.Self.TeleportLureRespond(fromID, true);
                }
            }
            else if (dialog == (int)MainAvatar.InstantMessageDialog.MessageFromObject)
            {
                Display.InstantMessage(Session.SessionNumber, true, dialog, fromName, message);
            }
            else if (dialog == (int)MainAvatar.InstantMessageDialog.InventoryOffered)
            {
                //handled by Inventory_OnInventoryItemReceived
            }
            else
            {
                if (!Session.IMSessions.ContainsKey(fromID))
                {
                    Session.IMSessions.Add(fromID, new GhettoSL.IMSession(imSessionID, fromName));
                }
                Display.InstantMessage(Session.SessionNumber, false, dialog, fromName, message);
                if (fromID == Session.Settings.MasterID) ScriptSystem.ParseCommand(Session.SessionNumber, "", message, false, true);
            }

            Dictionary<string, string> identifiers = new Dictionary<string, string>();
            identifiers.Add("$name", fromName);
            identifiers.Add("$message", message);
            identifiers.Add("$id", fromID.ToString());
            identifiers.Add("$dialog", dialog.ToString());
            ScriptSystem.TriggerEvents(Session.SessionNumber, ScriptSystem.EventTypes.IM, identifiers);

        }
    }
}
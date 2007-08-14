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
            Session.Client.Avatars.OnAvatarNames += new AvatarManager.AvatarNamesCallback(Avatars_OnAvatarNames);
            Session.Client.Directory.OnDirPeopleReply += new DirectoryManager.DirPeopleReplyCallback(Directory_OnDirPeopleReply);
            Session.Client.Friends.OnFriendOnline += new FriendsManager.FriendOnlineEvent(Friends_OnFriendOnline);
            Session.Client.Friends.OnFriendOffline += new FriendsManager.FriendOfflineEvent(Friends_OnFriendOffline);
            Session.Client.Friends.OnFriendshipOffered += new FriendsManager.FriendshipOfferedEvent(Friends_OnFriendshipOffered);
            Session.Client.Friends.OnFriendshipResponse += new FriendsManager.FriendshipResponseEvent(Friends_OnFriendshipResponse);
            Session.Client.Inventory.OnInventoryObjectReceived += new InventoryManager.InventoryObjectReceived(Inventory_OnInventoryObjectReceived);
            Session.Client.Network.OnConnected += new NetworkManager.ConnectedCallback(Network_OnConnected);
            Session.Client.Network.OnCurrentSimChanged += new NetworkManager.CurrentSimChangedCallback(Network_OnCurrentSimChanged);
            //Session.Client.Network.OnSimDisconnected += new NetworkManager.SimDisconnectCallback(Network_OnSimDisconnected);

            Session.Client.Network.OnDisconnected += new NetworkManager.DisconnectedCallback(Network_OnDisconnected);
            Session.Client.Objects.OnNewAvatar += new ObjectManager.NewAvatarCallback(Objects_OnNewAvatar);
            Session.Client.Objects.OnAvatarSitChanged += new ObjectManager.AvatarSitChanged(Objects_OnAvatarSitChanged);
            Session.Client.Objects.OnNewPrim += new ObjectManager.NewPrimCallback(Objects_OnNewPrim);
            Session.Client.Objects.OnObjectKilled += new ObjectManager.KillObjectCallback(Objects_OnObjectKilled);
            Session.Client.Objects.OnObjectUpdated += new ObjectManager.ObjectUpdatedCallback(Objects_OnObjectUpdated);
            session.Client.OnLogMessage += new SecondLife.LogCallback(Client_OnLogMessage);
            Session.Client.Self.OnChat += new MainAvatar.ChatCallback(Self_OnChat);
            Session.Client.Self.OnScriptDialog += new MainAvatar.ScriptDialogCallback(Self_OnScriptDialog);
            Session.Client.Self.OnScriptQuestion += new MainAvatar.ScriptQuestionCallback(Self_OnScriptQuestion);
            Session.Client.Self.OnInstantMessage += new MainAvatar.InstantMessageCallback(Self_OnInstantMessage);

            Session.Client.Groups.OnCurrentGroups += new GroupManager.CurrentGroupsCallback(Groups_OnCurrentGroups);
            Session.Client.Groups.OnGroupRoles += new GroupManager.GroupRolesCallback(Groups_OnGroupRoles);

            Session.Client.Network.RegisterCallback(PacketType.AgentAnimation, new NetworkManager.PacketCallback(Callback_AgentAnimation)); //DEBUG?

            Session.Client.Network.RegisterCallback(PacketType.AlertMessage, new NetworkManager.PacketCallback(Callback_AlertMessage));
            Session.Client.Network.RegisterCallback(PacketType.MoneyBalanceReply, new NetworkManager.PacketCallback(Callback_MoneyBalanceReply));
            Session.Client.Network.RegisterCallback(PacketType.HealthMessage, new NetworkManager.PacketCallback(Callback_HealthMessage));

            //Session.Client.Network.RegisterCallback(PacketType.TeleportFinish, new NetworkManager.PacketCallback(Callback_TeleportFinish));
            Session.Client.Self.OnTeleport += new MainAvatar.TeleportCallback(Self_OnTeleport);

            Interface.HTTPServer.OnHTTPRequest += new HTTPServer.OnHTTPRequestCallback(HTTPServer_OnHTTPRequest);            
        }

        void HTTPServer_OnHTTPRequest(string method, string path, string host, string userAgent, string contentType, Dictionary<string, string> getVars)
        {
            Console.WriteLine(method + " " + path);
            Console.WriteLine("Host: " + host);
            Console.WriteLine("User-agent: " + userAgent);
            foreach (KeyValuePair<string, string> var in getVars)
            {
                Console.WriteLine(var.Key + " = " + var.Value);
            }
        }

        void Friends_OnFriendshipOffered(LLUUID agentID, string agentName, LLUUID imSessionID)
        {
            Display.FriendshipOffered(Session.SessionNumber, agentID, agentName, imSessionID);
        }

        void Friends_OnFriendshipResponse(LLUUID agentID, string agentName, bool accepted)
        {
            Display.FriendshipResponse(Session.SessionNumber, agentID, agentName, accepted);
        }

        void Directory_OnDirPeopleReply(LLUUID queryID, List<DirectoryManager.AgentSearchData> matchedPeople)
        {
            foreach (DirectoryManager.AgentSearchData av in matchedPeople)
            {
                string name = av.FirstName + " " + av.LastName;
                Console.WriteLine(name.PadRight(20) + " " + av.AgentID.ToStringHyphenated());
            }
        }

        void Estate_OnGetTopColliders(int objectCount, List<EstateTools.EstateTask> Tasks)
        {
            //FIXME - Send results to Display class
            foreach (EstateTools.EstateTask task in Tasks)
            {
                if (task.Score > 0.1) Console.WriteLine(Math.Round(task.Score, 5) + " - " + task.OwnerName.PadRight(20) + " - " + task.TaskName);
            }
        }

        void Estate_OnGetTopScripts(int objectCount, List<EstateTools.EstateTask> Tasks)
        {
            //FIXME - Send results to Display class
            foreach (EstateTools.EstateTask task in Tasks)
            {
                if (task.Score > 0.1) Console.WriteLine(Math.Round(task.Score, 5) + " - " + task.OwnerName.PadRight(20) + " - " + task.TaskName);
            }
        }

        void Friends_OnFriendOffline(FriendsManager.FriendInfo friend)
        {
            Display.FriendOffline(Session.SessionNumber, friend);
        }

        void Friends_OnFriendOnline(FriendsManager.FriendInfo friend)
        {
            Display.FriendOnline(Session.SessionNumber, friend);
        }

        void Self_OnScriptQuestion(LLUUID taskID, LLUUID itemID, string objectName, string objectOwner, MainAvatar.ScriptPermission questions)
        {
            //FIXME - move to display
            Console.WriteLine(objectName + " owned by " + objectOwner + " has requested the following permissions: " + questions.ToString());
        }

        void Groups_OnGroupRoles(Dictionary<LLUUID, GroupRole> roles)
        {
            Display.GroupRoles(roles);
        }

        void Groups_OnCurrentGroups(Dictionary<LLUUID, Group> groups)
        {
            Session.Groups = groups;
        }

        void Self_OnInstantMessage(LLUUID fromAgentID, string fromAgentName, LLUUID toAgentID, uint parentEstateID, LLUUID regionID, LLVector3 position, MainAvatar.InstantMessageDialog dialog, bool groupIM, LLUUID imSessionID, DateTime timestamp, string message, MainAvatar.InstantMessageOnline offline, byte[] binaryBucket, Simulator simulator)
        {

            if (dialog == MainAvatar.InstantMessageDialog.RequestTeleport)
            {
                Display.InfoResponse(Session.SessionNumber, "Teleport lure received from " + fromAgentID + ": " + message);
                if (fromAgentID == Session.Settings.MasterID || (message.Length > 0 && message == Session.Settings.PassPhrase))
                {
                    Session.Client.Self.TeleportLureRespond(fromAgentID, true);
                }
            }
            else if (dialog == MainAvatar.InstantMessageDialog.MessageFromObject)
            {
                Display.InstantMessage(Session.SessionNumber, dialog, fromAgentName, message);
            }
            else if (dialog == MainAvatar.InstantMessageDialog.TaskInventoryOffered)
            {
                Console.WriteLine("POSSIBLE SPAM: " + fromAgentName + " gave you: " + message);
                Session.Client.Avatars.RequestAvatarName(fromAgentID);
            }
            else
            {
                if (!Session.IMSessions.ContainsKey(fromAgentID))
                {
                    Session.IMSessions.Add(fromAgentID, new GhettoSL.IMSession(imSessionID, fromAgentName));
                }
                Display.InstantMessage(Session.SessionNumber, dialog, fromAgentName, message);
                if (fromAgentID == Session.Settings.MasterID) Parse.Command(Session.SessionNumber, "", message, false, true);
            }

            Dictionary<string, string> identifiers = new Dictionary<string, string>();
            identifiers.Add("$name", fromAgentName);
            identifiers.Add("$message", message);
            identifiers.Add("$id", fromAgentID.ToStringHyphenated());
            identifiers.Add("$dialog", dialog.ToString());
            identifiers.Add("$pos", position.ToString());
            ScriptSystem.TriggerEvents(Session.SessionNumber, ScriptSystem.EventTypes.IM, identifiers);

        }

        void Avatars_OnAvatarNames(Dictionary<LLUUID, string> names)
        {
            foreach (KeyValuePair<LLUUID, string> pair in names)
            {
                Console.WriteLine(pair.Key.ToStringHyphenated() + " = " + pair.Value);
            }
        }

        void Self_OnChat(string message, MainAvatar.ChatAudibleLevel audible, MainAvatar.ChatType type, MainAvatar.ChatSourceType sourceType, string fromName, LLUUID id, LLUUID ownerid, LLVector3 position)
        {
            if (type == MainAvatar.ChatType.StartTyping || type == MainAvatar.ChatType.StopTyping || audible != MainAvatar.ChatAudibleLevel.Fully) return;

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

            Display.Chat(Session.SessionNumber, fromName, message, action, type, sourceType);

            if (id == Session.Client.Network.AgentID) return;

            Dictionary<string, string> identifiers = new Dictionary<string, string>();
            identifiers.Add("$name", fromName);
            identifiers.Add("$message", message);
            identifiers.Add("$id", id.ToStringHyphenated());
            identifiers.Add("$ownerid", ownerid.ToStringHyphenated());
            identifiers.Add("$ctype", type.ToString());
            identifiers.Add("$stype", sourceType.ToString());
            identifiers.Add("$pos", position.ToString());

            for (int i = 0; i < msg.Length; i++)
                identifiers.Add("$" + (i + 1), msg[i]);

            ScriptSystem.TriggerEvents(Session.SessionNumber, ScriptSystem.EventTypes.Chat, identifiers);
        }

        void Objects_OnNewAvatar(Simulator simulator, Avatar avatar, ulong regionHandle, ushort timeDilation)
        {
            //Console.WriteLine("NEW AVATAR:" + avatar.Name); //DEBUG
            lock (Session.Avatars)
            {
                if (!Session.Avatars.ContainsKey(avatar.LocalID)) Session.Avatars.Add(avatar.LocalID, avatar);
                else Session.Avatars[avatar.LocalID] = avatar;
            }
            //GC.Collect();
            //GC.WaitForPendingFinalizers();
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
            else Console.WriteLine("tp: " + status + " - " + message + " - " + flags); //debug
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
            if (Session.Debug > 0) Display.LogMessage(Session.SessionNumber, message, level);
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
            if (Session.SessionNumber == Interface.CurrentSession)
            {
                Console.Title = Session.Client.Self.FirstName + " " + Session.Client.Self.LastName + " @ " + Session.Client.Network.CurrentSim.Name + " - GhettoSL";
            }
            Display.SimChanged(Session.SessionNumber, PreviousSimulator, Session.Client.Network.CurrentSim);
            Session.Avatars = new Dictionary<uint, Avatar>();
            Session.Client.Network.CurrentSim.Estate.OnGetTopScripts += new EstateTools.GetTopScriptsReply(Estate_OnGetTopScripts);
            Session.Client.Network.CurrentSim.Estate.OnGetTopColliders += new EstateTools.GetTopCollidersReply(Estate_OnGetTopColliders);
        }

        bool Inventory_OnInventoryObjectReceived(LLUUID fromAgentID, string fromAgentName, uint parentEstateID, LLUUID regionID, LLVector3 position, DateTime timestamp, AssetType type, LLUUID objectID, bool fromTask)
        {
            InventoryBase obj = Session.Client.Inventory.Store[objectID];
            Display.InventoryItemReceived(Session.SessionNumber, fromAgentID, fromAgentName, parentEstateID, regionID, position, timestamp, obj);
            Dictionary<string, string> identifiers = new Dictionary<string, string>();
            identifiers.Add("$name", fromAgentName);
            identifiers.Add("$id", fromAgentID.ToStringHyphenated());
            identifiers.Add("$item", obj.Name);
            identifiers.Add("$itemid", objectID.ToStringHyphenated());
            identifiers.Add("$type", type.ToString());
            ScriptSystem.TriggerEvents(Session.SessionNumber, ScriptSystem.EventTypes.GetItem, identifiers);
            return true;
        }

        void Self_OnScriptDialog(string message, string objectName, LLUUID imageID, LLUUID objectID, string firstName, string lastName, int chatChannel, List<string> buttons)
        {
            Session.LastDialogID = objectID;
            Session.LastDialogChannel = chatChannel;
            Display.ScriptDialog(Session.SessionNumber, message, objectName, imageID, objectID, firstName, lastName, chatChannel, buttons);

            Dictionary<string, string> identifiers = new Dictionary<string, string>();
            identifiers.Add("$name", objectName);
            identifiers.Add("$id", objectID.ToStringHyphenated());
            identifiers.Add("$channel", chatChannel.ToString());
            identifiers.Add("$message", message);

            string[] splitSpace = { " " };
            string[] msg = message.Split(splitSpace, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < msg.Length; i++)
                identifiers.Add("$" + (i + 1), msg[i]);

            ScriptSystem.TriggerEvents(Session.SessionNumber, ScriptSystem.EventTypes.ScriptDialog, identifiers);
        }

        void Callback_AgentAnimation(Packet packet, Simulator sim)
        {
            AgentAnimationPacket reply = (AgentAnimationPacket)packet;
            //FIXME - add Animation event?
            if (reply.AgentData.AgentID != Session.Client.Network.AgentID) return;

            foreach (AgentAnimationPacket.AnimationListBlock b in reply.AnimationList)
            {
                Console.WriteLine("anim: " + b.AnimID + " : " + b.StartAnim); //DEBUG
            }
        }

        void Callback_AlertMessage(Packet packet, Simulator sim)
        {
            AlertMessagePacket p = (AlertMessagePacket)packet;
            string message = Helpers.FieldToUTF8String(p.AlertData.Message);
            Display.AlertMessage(Session.SessionNumber, message);
        }

        void Callback_HealthMessage(Packet packet, Simulator sim)
        {
            HealthMessagePacket reply = (HealthMessagePacket)packet;
            Display.Health(Session.SessionNumber, reply.HealthData.Health);
        }

        void Callback_MoneyBalanceReply(Packet packet, Simulator sim)
        {
            MoneyBalanceReplyPacket reply = (MoneyBalanceReplyPacket)packet;
            string desc = Helpers.FieldToUTF8String(reply.MoneyData.Description);
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

            //Session.Client.Groups.BeginGetCurrentGroups(new GroupManager.CurrentGroupsCallback(GroupsUpdatedHandler));

            //Session.Client.Grid.RequestEstateSims(GridManager.MapLayerType.Terrain); //FIXME - get new function name frm jh?

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
    }
 
}
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
using OpenMetaverse.Packets;
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
            Session.Client.Network.OnLogin += new NetworkManager.LoginCallback(Network_OnLogin);
            Session.Client.Network.OnConnected += new NetworkManager.ConnectedCallback(Network_OnConnected);
            Session.Client.Network.OnCurrentSimChanged += new NetworkManager.CurrentSimChangedCallback(Network_OnCurrentSimChanged);
            //Session.Client.Network.OnSimDisconnected += new NetworkManager.SimDisconnectCallback(Network_OnSimDisconnected);

            Session.Client.Network.OnDisconnected += new NetworkManager.DisconnectedCallback(Network_OnDisconnected);
            Session.Client.Objects.OnNewAvatar += new ObjectManager.NewAvatarCallback(Objects_OnNewAvatar);
            Session.Client.Objects.OnAvatarSitChanged += new ObjectManager.AvatarSitChanged(Objects_OnAvatarSitChanged);
            Session.Client.Objects.OnNewPrim += new ObjectManager.NewPrimCallback(Objects_OnNewPrim);
            Session.Client.Objects.OnObjectKilled += new ObjectManager.KillObjectCallback(Objects_OnObjectKilled);
            Session.Client.Objects.OnObjectUpdated += new ObjectManager.ObjectUpdatedCallback(Objects_OnObjectUpdated);
            Session.Client.Self.OnChat += new AgentManager.ChatCallback(Self_OnChat);
            Session.Client.Self.OnScriptDialog += new AgentManager.ScriptDialogCallback(Self_OnScriptDialog);
            Session.Client.Self.OnScriptQuestion += new AgentManager.ScriptQuestionCallback(Self_OnScriptQuestion);
            Session.Client.Self.OnInstantMessage += new AgentManager.InstantMessageCallback(Self_OnInstantMessage);

            Session.Client.Groups.OnCurrentGroups += new GroupManager.CurrentGroupsCallback(Groups_OnCurrentGroups);
            Session.Client.Groups.OnGroupRoles += new GroupManager.GroupRolesCallback(Groups_OnGroupRoles);

            Session.Client.Network.RegisterCallback(PacketType.AgentAnimation, new NetworkManager.PacketCallback(Callback_AgentAnimation)); //DEBUG?

            Session.Client.Network.RegisterCallback(PacketType.AlertMessage, new NetworkManager.PacketCallback(Callback_AlertMessage));
            Session.Client.Network.RegisterCallback(PacketType.MoneyBalanceReply, new NetworkManager.PacketCallback(Callback_MoneyBalanceReply));
            Session.Client.Network.RegisterCallback(PacketType.HealthMessage, new NetworkManager.PacketCallback(Callback_HealthMessage));

            //Session.Client.Network.RegisterCallback(PacketType.TeleportFinish, new NetworkManager.PacketCallback(Callback_TeleportFinish));
            Session.Client.Self.OnTeleport += new AgentManager.TeleportCallback(Self_OnTeleport);

            Interface.HTTPServer.OnHTTPRequest += new HTTPServer.OnHTTPRequestCallback(HTTPServer_OnHTTPRequest);            
        }

        void Network_OnLogin(LoginStatus login, string message)
        {
            if (login == LoginStatus.Failed)
            {
                Display.Error(Session.SessionNumber, "Login failed (" + message + ")");
            }
            else if (login == LoginStatus.Success)
            {
                Display.InfoResponse(Session.SessionNumber, "Connected!");
            }
            else Display.InfoResponse(Session.SessionNumber, message);
        }

        void HTTPServer_OnHTTPRequest(string method, string path, string host, string userAgent, string contentType, int contentLength, Dictionary<string, string> getVars)
        {
            Console.WriteLine(method + " " + path);
            Console.WriteLine("Host: " + host);
            Console.WriteLine("User-agent: " + userAgent);
            foreach (KeyValuePair<string, string> var in getVars)
            {
                Console.WriteLine(var.Key + " = " + var.Value);
            }
        }

        void Friends_OnFriendshipOffered(UUID agentID, string agentName, UUID imSessionID)
        {
            Display.FriendshipOffered(Session.SessionNumber, agentID, agentName, imSessionID);
        }

        void Friends_OnFriendshipResponse(UUID agentID, string agentName, bool accepted)
        {
            Display.FriendshipResponse(Session.SessionNumber, agentID, agentName, accepted);
        }

        void Directory_OnDirPeopleReply(UUID queryID, List<DirectoryManager.AgentSearchData> matchedPeople)
        {
            foreach (DirectoryManager.AgentSearchData av in matchedPeople)
            {
                string name = av.FirstName + " " + av.LastName;
                Console.WriteLine(name.PadRight(20) + " " + av.AgentID.ToString());
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

        void Friends_OnFriendOffline(FriendInfo friend)
        {
            Display.FriendOffline(Session.SessionNumber, friend);
        }

        void Friends_OnFriendOnline(FriendInfo friend)
        {
            Display.FriendOnline(Session.SessionNumber, friend);
        }

        void Self_OnScriptQuestion(Simulator simulator, UUID taskID, UUID itemID, string objectName, string objectOwner, ScriptPermission questions)
        {
            //FIXME - move to display
            Console.WriteLine(objectName + " owned by " + objectOwner + " has requested the following permissions: " + questions.ToString());
        }

        void Groups_OnGroupRoles(Dictionary<UUID, GroupRole> roles)
        {
            Display.GroupRoles(roles);
        }

        void Groups_OnCurrentGroups(Dictionary<UUID, Group> groups)
        {
            Session.Groups = groups;
        }


        void Self_OnInstantMessage(InstantMessage im, Simulator simulator)
        {
            if (im.Dialog == InstantMessageDialog.RequestTeleport)
            {
                Display.InfoResponse(Session.SessionNumber, "Teleport lure received from " + im.FromAgentID + ": " + im.Message);
                if (im.FromAgentID == Session.Settings.MasterID || (im.Message.Length > 0 && im.Message == Session.Settings.PassPhrase))
                {
                    Session.Client.Self.TeleportLureRespond(im.FromAgentID, true);
                }
            }
            else if (im.Dialog == InstantMessageDialog.MessageFromObject)
            {
                Display.InstantMessage(Session.SessionNumber, im.Dialog, im.FromAgentName, im.Message);
                if (Session.Debug > 0) Display.InfoResponse(Session.SessionNumber, "Owner UUID: " + im.FromAgentID.ToString());
            }
            else if (im.Dialog == InstantMessageDialog.TaskInventoryOffered)
            {
                Console.WriteLine("POSSIBLE SPAM: " + im.FromAgentName + " gave you: " + im.Message);
                Session.Client.Avatars.RequestAvatarName(im.FromAgentID);
            }
            else
            {
                if (!Session.IMSessions.ContainsKey(im.FromAgentID))
                {
                    Session.IMSessions.Add(im.FromAgentID, new GhettoSL.IMSession(im.IMSessionID, im.FromAgentName));
                }
                Display.InstantMessage(Session.SessionNumber, im.Dialog, im.FromAgentName, im.Message);
                if (im.FromAgentID == Session.Settings.MasterID) Parse.Command(Session.SessionNumber, "", im.Message, false, true);
            }

            Dictionary<string, string> identifiers = new Dictionary<string, string>();
            identifiers.Add("$name", im.FromAgentName);
            identifiers.Add("$message", im.Message);
            identifiers.Add("$id", im.FromAgentID.ToString());
            identifiers.Add("$dialog", im.Dialog.ToString());
            identifiers.Add("$pos", im.Position.ToString());
            ScriptSystem.TriggerEvents(Session.SessionNumber, ScriptSystem.EventTypes.IM, identifiers);

        }

        void Avatars_OnAvatarNames(Dictionary<UUID, string> names)
        {
            foreach (KeyValuePair<UUID, string> pair in names)
            {
                Console.WriteLine(pair.Key.ToString() + " = " + pair.Value);
            }
        }

        void Self_OnChat(string message, ChatAudibleLevel audible, ChatType type, ChatSourceType sourceType, string fromName, UUID id, UUID ownerid, Vector3 position)
        {
            if (type == ChatType.StartTyping || type == ChatType.StopTyping || audible != ChatAudibleLevel.Fully) return;

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

            if (id == Session.Client.Self.AgentID) return;

            Dictionary<string, string> identifiers = new Dictionary<string, string>();
            identifiers.Add("$name", fromName);
            identifiers.Add("$message", message);
            identifiers.Add("$id", id.ToString());
            identifiers.Add("$ownerid", ownerid.ToString());
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

        void Self_OnTeleport(string message, AgentManager.TeleportStatus status, AgentManager.TeleportFlags flags)
        {
            if (status == AgentManager.TeleportStatus.Finished)
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
                    Avatar av = Session.Avatars[update.LocalID];
                    av.Position = update.Position;
                    av.Rotation = update.Rotation;
                    Dictionary<string, string> identifiers = new Dictionary<string,string>();
                    identifiers.Add("$name", av.Name);
                    identifiers.Add("$id", av.ID.ToString());
                    identifiers.Add("$pos", av.Position.ToString());
                    ScriptSystem.TriggerEvents(Session.SessionNumber, ScriptSystem.EventTypes.Update, identifiers);
                }
            }
        }

        void Objects_OnAvatarSitChanged(Simulator sim, Avatar av, uint localid, uint oldSeatID)
        {
            Display.SitChanged(Session.SessionNumber, av, localid);
            if (localid > 0) ScriptSystem.TriggerEvents(Session.SessionNumber, ScriptSystem.EventTypes.Sit, null);
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

        bool Inventory_OnInventoryObjectReceived(UUID fromAgentID, string fromAgentName, uint parentEstateID, UUID regionID, Vector3 position, DateTime timestamp, AssetType type, UUID objectID, bool fromTask)
        {
            InventoryBase obj = Session.Client.Inventory.Store[objectID];
            Display.InventoryItemReceived(Session.SessionNumber, fromAgentID, fromAgentName, parentEstateID, regionID, position, timestamp, obj);
            Dictionary<string, string> identifiers = new Dictionary<string, string>();
            identifiers.Add("$name", fromAgentName);
            identifiers.Add("$id", fromAgentID.ToString());
            identifiers.Add("$item", obj.Name);
            identifiers.Add("$itemid", objectID.ToString());
            identifiers.Add("$type", type.ToString());
            ScriptSystem.TriggerEvents(Session.SessionNumber, ScriptSystem.EventTypes.GetItem, identifiers);
            return true;
        }

        void Self_OnScriptDialog(string message, string objectName, UUID imageID, UUID objectID, string firstName, string lastName, int chatChannel, List<string> buttons)
        {
            Session.LastDialogID = objectID;
            Session.LastDialogChannel = chatChannel;
            Display.ScriptDialog(Session.SessionNumber, message, objectName, imageID, objectID, firstName, lastName, chatChannel, buttons);

            Dictionary<string, string> identifiers = new Dictionary<string, string>();
            identifiers.Add("$name", objectName);
            identifiers.Add("$id", objectID.ToString());
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
            if (reply.AgentData.AgentID != Session.Client.Self.AgentID) return;

            foreach (AgentAnimationPacket.AnimationListBlock b in reply.AnimationList)
            {
                Console.WriteLine("anim: " + b.AnimID + " : " + b.StartAnim); //DEBUG
            }
        }

        void Callback_AlertMessage(Packet packet, Simulator sim)
        {
            AlertMessagePacket p = (AlertMessagePacket)packet;
            string message = Utils.BytesToString(p.AlertData.Message);
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
            string desc = Utils.BytesToString(reply.MoneyData.Description);
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

            //Retrieve offline IMs
            //FIXME - Add Client.Self.RetrieveInstantMessages() to core
            Session.Client.Self.RetrieveInstantMessages();

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
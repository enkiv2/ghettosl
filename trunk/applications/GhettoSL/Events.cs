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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using libsecondlife;
using libsecondlife.Packets;

namespace ghetto
{
    partial class GhettoSL
    {
        void InitializeCallbacks()
        {
            Client.Network.RegisterCallback(PacketType.AvatarAppearance, new NetworkManager.PacketCallback(OnAppearance));
            Client.Network.RegisterCallback(PacketType.FetchInventoryReply, new NetworkManager.PacketCallback(OnFetchInventoryReply));
            Client.Network.RegisterCallback(PacketType.MoneyBalanceReply, new NetworkManager.PacketCallback(OnMoneyBalanceReply));
            Client.Network.RegisterCallback(PacketType.ObjectUpdate, new NetworkManager.PacketCallback(OnObjectUpdateEvent));
            Client.Network.RegisterCallback(PacketType.RequestFriendship, new NetworkManager.PacketCallback(OnRequestFriendship));
            Client.Network.RegisterCallback(PacketType.TeleportFinish, new NetworkManager.PacketCallback(OnTeleportFinish));
            Client.Network.RegisterCallback(PacketType.AlertMessage, new NetworkManager.PacketCallback(OnAlertMessage));
            Client.Network.RegisterCallback(PacketType.DirGroupsReply, new NetworkManager.PacketCallback(OnDirGroupsReply));


            //Mapstalk - FIXME - Jesse added this... I don't think it works.
            Client.Network.RegisterCallback(PacketType.FindAgent, new NetworkManager.PacketCallback(FindAgentCallback));

            //Sim Crossing - FIXME - Neither of these seems to trigger
            Client.Network.RegisterCallback(PacketType.CrossedRegion, new NetworkManager.PacketCallback(OnCrossedRegion));
            Client.Network.RegisterCallback(PacketType.AgentToNewRegion, new NetworkManager.PacketCallback(OnAgentToNewRegion));

            Client.Network.OnConnected += new NetworkManager.ConnectedCallback(OnConnectedEvent);
            Client.Network.OnSimDisconnected += new NetworkManager.SimDisconnectCallback(OnSimDisconnectEvent);
            Client.Objects.OnAvatarMoved += new ObjectManager.AvatarMovedCallback(OnAvatarMovedEvent);
            Client.Objects.OnNewAvatar += new ObjectManager.NewAvatarCallback(OnNewAvatarEvent);
            Client.Objects.OnNewPrim += new ObjectManager.NewPrimCallback(OnNewPrimEvent);
            Client.Objects.OnObjectKilled += new ObjectManager.KillObjectCallback(OnObjectKilledEvent);
            Client.Objects.OnPrimMoved += new ObjectManager.PrimMovedCallback(OnPrimMovedEvent);
            Client.Avatars.OnFriendNotification += new AvatarManager.FriendNotificationCallback(OnFriendNotificationEvent);
            Client.Self.OnChat += new MainAvatar.ChatCallback(OnChatEvent);
            Client.Self.OnInstantMessage += new MainAvatar.InstantMessageCallback(OnInstantMessageEvent);
            Client.Self.OnScriptDialog += new MainAvatar.ScriptDialogCallback(OnScriptDialogEvent);

        }

        void OnScriptDialogEvent(string message, string objectName, LLUUID imageID, LLUUID objectID, string firstName, string lastName, int chatChannel, List<string> buttons)
        {
            string[] btn = buttons.ToArray();
            string buttonList = "[" + String.Join("] [", btn) + "]";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(TimeStamp() + "Dialog from " + objectName);
            Console.WriteLine(TimeStamp() + "Owner: " + firstName + " " + lastName);
            Console.WriteLine(TimeStamp() + "Object: " + objectID);
            Console.WriteLine(TimeStamp() + "Channel: " + chatChannel);
            Console.WriteLine(TimeStamp() + "Message: " + message);
            Console.WriteLine(TimeStamp() + "Choices: " + buttonList);
            Console.ForegroundColor = ConsoleColor.Gray;
        }        

        void OnConnectedEvent(object sender)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(TimeStamp() + "CONNECTED");
            Console.ForegroundColor = ConsoleColor.Gray;

            //Load "Avatar Name.appearance" if the file exists
            //string appearanceFile = Client.Self.FirstName + " " + Client.Self.LastName + ".appearance";
            //if (File.Exists(appearanceFile)) LoadAppearance(appearanceFile);

            //Load stored appearance from asset server
            UpdateAppearance();

            //Enable agent updates
            if (Session.Settings.SendUpdates) Client.Self.Status.UpdateTimer.Start();

            //Needed for finding lots of sims
            Client.Grid.AddEstateSims();

            //Retrieve offline IMs
            //FIXME - Add Client.Self.RetrieveInstantMessages() TO CORE!
            RetrieveInstantMessagesPacket p = new RetrieveInstantMessagesPacket();
            p.AgentData.AgentID = Client.Network.AgentID;
            p.AgentData.SessionID = Client.Network.SessionID;
            Client.Network.SendPacket(p);
        }



        void OnSimDisconnectEvent(Simulator sim, NetworkManager.DisconnectType type)
        {
            Console.ForegroundColor = System.ConsoleColor.Red;
            Console.WriteLine(TimeStamp() + "DISCONNECTED FROM SIM: " + type.ToString());
            Console.ForegroundColor = System.ConsoleColor.Gray;
            if (logout) return;

            return; //FIXME - Log back in after disconnect

            Client.Network.Logout();
            do Thread.Sleep(5000);
            while (!Login());
        }

        

        void OnAlertMessage(Packet packet, Simulator sim)
        {
            AlertMessagePacket p = (AlertMessagePacket)packet;

            Console.ForegroundColor = System.ConsoleColor.Cyan;
            Console.WriteLine(TimeStamp() + Helpers.FieldToString(p.AlertData.Message));
            Console.ForegroundColor = System.ConsoleColor.Gray;
        }

        //FIXME - CROSS INTO NEW REGIONS - Neither of these triggers when pushed across a border
        void OnCrossedRegion(Packet packet, Simulator sim)
        {
            CrossedRegionPacket reply = (CrossedRegionPacket)packet;
            Console.WriteLine(TimeStamp() + "CROSSED TO NEW REGION: " + reply.RegionData.RegionHandle);
            Client.Network.SendPacket(new DisableSimulatorPacket());
            EnableSimulatorPacket p = new EnableSimulatorPacket();
            p.SimulatorInfo.IP = reply.RegionData.SimIP;
            p.SimulatorInfo.Port = reply.RegionData.SimPort;
            p.SimulatorInfo.Handle = reply.RegionData.RegionHandle;
        }
        void OnAgentToNewRegion(Packet packet, Simulator sim)
        {
            AgentToNewRegionPacket reply = (AgentToNewRegionPacket)packet;
            Console.WriteLine(TimeStamp() + "CROSSED TO NEW REGION: " + reply.RegionData.Handle);
            Client.Network.SendPacket(new DisableSimulatorPacket());
            EnableSimulatorPacket p = new EnableSimulatorPacket();
            p.SimulatorInfo.IP = reply.RegionData.IP;
            p.SimulatorInfo.Port = reply.RegionData.Port;
            p.SimulatorInfo.Handle = reply.RegionData.Handle;
        }

        void OnDirGroupsReply(Packet packet, Simulator sim)
        {
            DirGroupsReplyPacket reply = (DirGroupsReplyPacket)packet;
            DirGroupsReplyPacket.QueryRepliesBlock[] groups = reply.QueryReplies;
            foreach (DirGroupsReplyPacket.QueryRepliesBlock g in groups)
            {
                Console.WriteLine(g.OpenEnrollment + " " + g.MembershipFee + " " + g.Members + " " + g.GroupName);
            }
        }

        void OnChatEvent(string message, byte audible, byte chatType, byte sourceType, string name, LLUUID fromAgentID, LLUUID ownerID, LLVector3 position)
        {
            string lowerMessage = message.ToLower();
            foreach(KeyValuePair<string, ScriptEvent> pair in Session.Script.Events)
            {
                if (pair.Value.Type != (int)EventTypes.Chat) continue;
                if (pair.Value.Text.ToLower() != lowerMessage) continue;
                string[] cmdScript = { ParseScriptVariables(pair.Value.Command, name, fromAgentID, 0, message) };
                ParseScriptLine(cmdScript, 0);
            }
            if (Session.Settings.Quiet || chatType > 3 || audible < 1) return;
            char[] splitChar = { ' ' };
            string[] msg = message.Split(splitChar);

            if (sourceType == 1) Console.ForegroundColor = System.ConsoleColor.White;
            else Console.ForegroundColor = System.ConsoleColor.DarkCyan;

            string prefix = "";
            if (msg[0].ToLower() != "/me")
            {
                if (chatType == (int)MainAvatar.ChatType.Shout) prefix = " shouts";
                else if (chatType == (int)MainAvatar.ChatType.Whisper) prefix = " whispers";
                Console.WriteLine(TimeStamp() + "{0}{1}: {2}", name, prefix, message);
            }
            else
            {
                if (chatType == (int)MainAvatar.ChatType.Shout) prefix = "(shouted) ";
                else if (chatType == (int)MainAvatar.ChatType.Whisper) prefix = "(whispered) ";
                message = String.Join(" ", msg, 1, msg.Length - 1);
                Console.WriteLine(TimeStamp() + "{0}{1} {2}", prefix, name, message);
            }
            Console.ForegroundColor = System.ConsoleColor.Gray;
        }



        void OnInstantMessageEvent(LLUUID fromAgentID, string fromAgentName, LLUUID toAgentID, uint parentEstateID, LLUUID regionID, LLVector3 position, byte dialog, bool groupIM, LLUUID imSessionID, DateTime timestamp, string message, byte offline, byte[] binaryBucket)
        {
            string lowerMessage = message.ToLower();
            foreach (KeyValuePair<string, ScriptEvent> pair in Session.Script.Events)
            {
                if (pair.Value.Type == (int)EventTypes.IM && pair.Value.Text.ToLower() == lowerMessage)
                {
                    string[] cmdScript = { pair.Value.Command };
                    ParseScriptLine(cmdScript, 0);
                }
            }

            //Teleport request
            if (dialog == (int)MainAvatar.InstantMessageDialog.RequestTeleport && (fromAgentID == Session.Settings.MasterID || message == Session.Settings.PassPhrase))
            {
                Console.ForegroundColor = System.ConsoleColor.Magenta;
                Console.WriteLine(TimeStamp() + "Accepting teleport request from {0} ({1})", fromAgentName, message);
                Console.ForegroundColor = System.ConsoleColor.Gray;
                Client.Self.TeleportLureRespond(fromAgentID, true);
                return;
            }
            //Receive object
            else if (dialog == (int)MainAvatar.InstantMessageDialog.GiveInventory)
            {
                Console.ForegroundColor = System.ConsoleColor.Cyan;
                Console.WriteLine(TimeStamp() + fromAgentName + " gave you an object named \"" + message + "\"");
                Console.ForegroundColor = System.ConsoleColor.Gray;
                return;
            }
            //Receive notecard
            else if (dialog == (int)MainAvatar.InstantMessageDialog.GiveNotecard)
            {
                Console.ForegroundColor = System.ConsoleColor.Cyan;
                Console.WriteLine(TimeStamp() + "{0} gave you a notecard named \"{1}\"", fromAgentName, message);
                Console.ForegroundColor = System.ConsoleColor.Gray;
                return;
            }

            if (!Session.Settings.Quiet)
            {
                CreateMessageWindow(fromAgentID, fromAgentName, dialog, imSessionID);
                //Display IM in console
                Console.ForegroundColor = System.ConsoleColor.Cyan;
                Console.WriteLine(TimeStamp() + "(IM|d={0}) <{1}>: {2}", dialog, fromAgentName, message);
                Console.ForegroundColor = System.ConsoleColor.Gray;
            }

            //Parse commands from masterID only
            if (offline > 0 || fromAgentID != Session.Settings.MasterID) return;

            //Remember IM session
            Session.MasterIMSession = imSessionID;
            string command;
            if (message.Substring(0, 1) == "/") command = message.Substring(1);
            else command = message;
            ParseCommand(false, message, fromAgentName, fromAgentID, imSessionID);
        }

        //Friend notification
        //FIXME - Get agent name by uuid
        void OnFriendNotificationEvent(LLUUID friendID, bool online)
        {
            Console.ForegroundColor = System.ConsoleColor.Gray;
            Console.BackgroundColor = System.ConsoleColor.DarkBlue;
            Console.WriteLine(" {0} is online ({1}) ", friendID, online);
            Console.BackgroundColor = System.ConsoleColor.Black;
        }

        void OnMoneyBalanceReply(Packet packet, Simulator simulator)
        {
            MoneyBalanceReplyPacket reply = (MoneyBalanceReplyPacket)packet;
            string desc = Helpers.FieldToString(reply.MoneyData.Description);
            int changeAmount = reply.MoneyData.MoneyBalance - Session.Balance;
            Session.Balance = reply.MoneyData.MoneyBalance;

            char[] splitChar = { ' ' };
            string[] msg = desc.Split(splitChar);
            if (msg.Length == 5)
            {
                if (!int.TryParse(msg[4].Substring(2, msg[4].Length - 3), out changeAmount))
                    Console.WriteLine(TimeStamp() + "UNEXPECTED PAYMENT MESSAGE");
                else if (msg[0] + " " + msg[1] == "You paid")
                    AcknowledgePayment(msg[2] + " " + msg[3], changeAmount * -1);
                else if (msg[2] + " " + msg[3] == "paid you")
                    AcknowledgePayment(msg[0] + " " + msg[1], changeAmount);
                else
                    Console.WriteLine(TimeStamp() + "UNEXPECTED PAYMENT MESSAGE");
            }
                
            if (desc.Length > 1)
            {
                Console.ForegroundColor = System.ConsoleColor.Cyan;
                Console.WriteLine(TimeStamp() + desc);
            }
            Console.ForegroundColor = System.ConsoleColor.Green;
            Console.WriteLine(TimeStamp() + "Balance: L$" + Session.Balance);
            Console.ForegroundColor = System.ConsoleColor.Gray;
        }



        void OnTeleportFinish(Packet packet, Simulator simulator)
        {
            Session.Avatars = new Dictionary<uint,Avatar>(); //wipe old avatar list
            Console.WriteLine(TimeStamp() + "FINISHED TELEPORT TO REGION AT " + Session.RegionX + ", " + Session.RegionY);
            TeleportFinishPacket reply = (TeleportFinishPacket)packet;
            Session.RegionX = (int)(reply.Info.RegionHandle >> 32);
            Session.RegionY = (int)(reply.Info.RegionHandle & 0xFFFFFFFF);
            if (reply.Info.AgentID != Client.Network.AgentID) return;
            if (Session.LastAppearance.AgentData.SerialNum > 0) Client.Network.SendPacket(Session.LastAppearance);
            Client.Self.Status.SendUpdate();
        }


        //FIXME - Don't even look at this part, it needs serious rewriting
        void OnAvatarMovedEvent(Simulator simulator, AvatarUpdate avatar, ulong regionHandle, ushort timeDilation)
        {
            Avatar test;
            if (!Session.Avatars.TryGetValue(avatar.LocalID, out test)) return;
            lock (Session.Avatars)
            {
                string name = Session.Avatars[avatar.LocalID].Name;
                //if (Session.Avatars[avatar.LocalID].ID == Client.Network.AgentID)
                //{
                //this is a temp hack to update region corner X/Y any time any av moves (not just the follow target)
                Session.RegionX = (int)(regionHandle >> 32);
                Session.RegionY = (int)(regionHandle & 0xFFFFFFFF);
                //}
                if (Session.Avatars[avatar.LocalID].Name == Session.Settings.FollowName)
                {
                    Session.Avatars[avatar.LocalID].Position = avatar.Position;
                    Session.Avatars[avatar.LocalID].Rotation = avatar.Rotation;
                    if (!Follow(name))
                    {
                        Client.Self.Status.SendUpdate();
                    }
                }
            }
        }



        void OnNewAvatarEvent(Simulator simulator, Avatar avatar, ulong regionHandle, ushort timeDilation)
        {
            lock (Session.Avatars)
            {
                Session.Avatars[avatar.LocalID] = avatar;
            }
        }
        void OnAppearance(Packet packet, Simulator simulator)
        {
            AvatarAppearancePacket appearance = (AvatarAppearancePacket)packet;
            lock (Session.Appearances)
            {
                Session.Appearances[appearance.Sender.ID] = appearance;
            }
        }
        void OnNewPrimEvent(Simulator simulator, PrimObject prim, ulong regionHandle, ushort timeDilation)
        {
            lock (Session.Prims)
            {
                Session.Prims[prim.LocalID] = prim;
            }
        }
        void OnObjectKilledEvent(Simulator simulator, uint objectID)
        {
            lock (Session.Prims)
            {
                if (Session.Prims.ContainsKey(objectID))
                    Session.Prims.Remove(objectID);
            }
            lock (Session.Avatars)
            {
                if (Session.Avatars.ContainsKey(objectID))
                    Session.Avatars.Remove(objectID);
            }
        }
        void OnPrimMovedEvent(Simulator simulator, PrimUpdate prim, ulong regionHandle, ushort timeDilation)
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


        void OnFetchInventoryReply(Packet packet, Simulator sim)
        {
            //FIXME - Fetch inventory item data
            FetchInventoryReplyPacket reply = (FetchInventoryReplyPacket)packet;
            FetchInventoryReplyPacket.InventoryDataBlock obj = new FetchInventoryReplyPacket.InventoryDataBlock();

            Console.WriteLine("Inventory info: " + obj.Name);

            //FIXME - Rez object from inventory
            RezObjectPacket p = new RezObjectPacket();
            p.AgentData.AgentID = Client.Network.AgentID;
            p.AgentData.SessionID = Client.Network.SessionID;
            p.InventoryData.BaseMask = obj.BaseMask;
            p.InventoryData.CRC = obj.CRC;
            p.InventoryData.CreationDate = obj.CreationDate;
            p.InventoryData.CreatorID = obj.CreatorID;
            p.InventoryData.Description = obj.Description;
            p.InventoryData.EveryoneMask = obj.EveryoneMask;
            p.InventoryData.Flags = obj.Flags;
            p.InventoryData.FolderID = obj.FolderID;
            p.InventoryData.GroupID = obj.GroupID;
            p.InventoryData.GroupMask = obj.GroupMask;
            p.InventoryData.GroupOwned = obj.GroupOwned;
            p.InventoryData.InvType = obj.InvType;
            p.InventoryData.ItemID = obj.ItemID;
            p.InventoryData.Name = obj.Name;
            p.InventoryData.NextOwnerMask = obj.NextOwnerMask;
            p.InventoryData.OwnerID = obj.OwnerID;
            p.InventoryData.OwnerMask = obj.OwnerMask;
            p.InventoryData.SalePrice = obj.SalePrice;
            p.InventoryData.SaleType = obj.SaleType;
            //p.InventoryData.TransactionID = ?
            p.InventoryData.Type = obj.Type;

            LLVector3 rezPos = Client.Self.Position;
            rezPos.X++;
            rezPos.Y++;
            p.RezData.RayEnd = rezPos;
            p.RezData.RayStart = Client.Self.Position;
            p.RezData.RayEndIsIntersection = false;
            p.RezData.RezSelected = false;
            p.RezData.RemoveItem = false;
            p.RezData.BypassRaycast = 1;

            //Client.Network.SendPacket(p);

        }



        void OnObjectSelect(Packet packet, Simulator sim)
        {
            ObjectSelectPacket reply = (ObjectSelectPacket)packet;
            //FIXME -- Detect object a user is grabbing
            //if (reply.AgentData.AgentID != masterID) return;
            Console.WriteLine(TimeStamp() + "Touched/grabbed object " + reply.ObjectData[0].ObjectLocalID);
        }

        //FRIEND REQUESTS (FIXME!!!) #########################################
        void OnRequestFriendship(Packet packet, Simulator simulator)
        {
            RequestFriendshipPacket reply = (RequestFriendshipPacket)packet;
            if (reply.AgentData.AgentID != Session.Settings.MasterID) return;
            AcceptFriendshipPacket p = new AcceptFriendshipPacket();
            p.AgentData.AgentID = Client.Network.AgentID;
            p.AgentData.SessionID = Client.Network.SessionID;
            p.TransactionBlock.TransactionID = reply.AgentBlock.TransactionID;
            AcceptFriendshipPacket.FolderDataBlock[] folder = new AcceptFriendshipPacket.FolderDataBlock[1];
            folder[0].FolderID = reply.AgentBlock.FolderID;
            p.FolderData = folder;
            Client.Network.SendPacket(p);
        }
        //END OF FRIEND REQUESTS ##############################################


        //AUTO-CAMP OBJECT-FINDING STUFF ######################################
        void OnObjectUpdateEvent(Packet packet, Simulator sim)
        {
            ObjectUpdatePacket p = (ObjectUpdatePacket)packet;
            foreach (ObjectUpdatePacket.ObjectDataBlock obj in p.ObjectData)
            {
                //FIXME!!! Update prim text
                //if (prims[obj.ID])
                //    prims[obj.ID].Text = Helpers.FieldToString(obj.Text);
            }
        }
        //END OF AUTO-CAMP STUFF ##############################################



    }
}

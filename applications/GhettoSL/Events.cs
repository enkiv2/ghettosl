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
            //Mapstalk
            Client.Network.RegisterCallback(PacketType.FindAgent, new NetworkManager.PacketCallback(FindAgentCallback));
            Client.Network.OnConnected += new NetworkManager.ConnectedCallback(OnConnectedEvent);
            Client.Network.OnSimDisconnected += new NetworkManager.SimDisconnectCallback(OnSimDisconnectEvent);
            Client.Objects.OnAvatarMoved += new ObjectManager.AvatarMovedCallback(OnAvatarMovedEvent);
            Client.Objects.OnNewAvatar += new ObjectManager.NewAvatarCallback(OnNewAvatarEvent);
            Client.Objects.OnNewPrim += new ObjectManager.NewPrimCallback(OnNewPrimEvent);
            Client.Objects.OnObjectKilled += new ObjectManager.KillObjectCallback(OnObjectKilledEvent);
            Client.Objects.OnPrimMoved += new ObjectManager.PrimMovedCallback(OnPrimMovedEvent);
            Client.Avatars.OnFriendNotification += new AvatarManager.FriendNotificationCallback(OnFriendNotificationEvent);
            Client.Self.OnInstantMessage += new MainAvatar.InstantMessageCallback(OnInstantMessageEvent);
        }

        

        void OnConnectedEvent(object sender)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(TimeStamp() + "* CONNECTED");
            Console.ForegroundColor = ConsoleColor.Gray;

            string appearanceFile = Client.Self.FirstName + " " + Client.Self.LastName + ".appearance";
            if (File.Exists(appearanceFile)) LoadAppearance(appearanceFile);

            if (sendUpdates) Client.Self.Status.UpdateTimer.Start();

            Client.Grid.AddEstateSims();

            //FIXME!!! - ADD Client.Self.RetrieveInstantMessages() TO CORE!
            //Retrieve offline IMs
            RetrieveInstantMessagesPacket p = new RetrieveInstantMessagesPacket();
            p.AgentData.AgentID = Client.Network.AgentID;
            p.AgentData.SessionID = Client.Network.SessionID;
            Client.Network.SendPacket(p);
        }



        void OnSimDisconnectEvent(Simulator sim, NetworkManager.DisconnectType type)
        {
            Console.ForegroundColor = System.ConsoleColor.Red;
            Console.WriteLine("* DISCONNECTED FROM SIM: " + type.ToString());
            Console.ForegroundColor = System.ConsoleColor.Gray;
            //FIXME - reconnect on disconnect
            //if (logout) return;
            //Client.Network.Logout();
            //do Thread.Sleep(5000);
            //while (!Login());
        }

        

        void OnAlertMessage(Packet packet, Simulator sim)
        {
            AlertMessagePacket p = (AlertMessagePacket)packet;

            Console.ForegroundColor = System.ConsoleColor.Cyan;
            Console.WriteLine(TimeStamp() + "* " + Helpers.FieldToString(p.AlertData.Message));
            Console.ForegroundColor = System.ConsoleColor.Gray;
        }


        
        void OnChatEvent(string message, byte audible, byte chatType, byte sourceType, string name, LLUUID fromAgentID, LLUUID ownerID, LLVector3 position)
        {
            string lowerMessage = message.ToLower();
            foreach(KeyValuePair<int, Event> pair in scriptEvents)
            {
                if (pair.Value.Type == (int)EventTypes.Chat && pair.Value.Text.ToLower() == lowerMessage)
                {
                    string[] cmdScript = { ParseScriptVariables(pair.Value.Command, name, fromAgentID, 0, message) };
                    ParseScriptLine(cmdScript, 0);
                }
            }
            if (quiet || chatType > 3 || audible < 1) return;
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
                Console.WriteLine(TimeStamp() + "* {0}{1} {2}", prefix, name, message);
            }
            Console.ForegroundColor = System.ConsoleColor.Gray;
        }



        void OnInstantMessageEvent(LLUUID fromAgentID, string fromAgentName, LLUUID toAgentID, uint parentEstateID, LLUUID regionID, LLVector3 position, byte dialog, bool groupIM, LLUUID imSessionID, DateTime timestamp, string message, byte offline, byte[] binaryBucket)
        {
            string lowerMessage = message.ToLower();
            foreach (KeyValuePair<int, Event> pair in scriptEvents)
            {
                if (pair.Value.Type == (int)EventTypes.IM && pair.Value.Text.ToLower() == lowerMessage)
                {
                    string[] cmdScript = { pair.Value.Command };
                    ParseScriptLine(cmdScript, 0);
                }
            }

            //Teleport request
            if (dialog == (int)MainAvatar.InstantMessageDialog.RequestTeleport && (fromAgentID == masterID || message == passPhrase))
            {
                Console.ForegroundColor = System.ConsoleColor.Magenta;
                Console.WriteLine("* Accepting teleport request from {0} ({1})", fromAgentName, message);
                Console.ForegroundColor = System.ConsoleColor.Gray;
                Client.Self.TeleportLureRespond(fromAgentID, true);
                return;
            }
            //Receive object
            else if (dialog == (int)MainAvatar.InstantMessageDialog.GiveInventory)
            {
                Console.ForegroundColor = System.ConsoleColor.Cyan;
                Console.WriteLine(TimeStamp() + "* " + fromAgentName + " gave you an object named \"" + message + "\"");
                Console.ForegroundColor = System.ConsoleColor.Gray;
                return;
            }
            //Receive notecard
            else if (dialog == (int)MainAvatar.InstantMessageDialog.GiveNotecard)
            {
                Console.ForegroundColor = System.ConsoleColor.Cyan;
                Console.WriteLine(TimeStamp() + "* {0} gave you a notecard named \"{1}\"", fromAgentName, message);
                Console.ForegroundColor = System.ConsoleColor.Gray;
                return;
            }

            if (!quiet)
            {
                CreateMessageWindow(fromAgentID, fromAgentName, dialog, imSessionID);
                //Display IM in console
                Console.ForegroundColor = System.ConsoleColor.Cyan;
                Console.WriteLine(TimeStamp() + "(IM|d={0}) <{1}>: {2}", dialog, fromAgentName, message);
                Console.ForegroundColor = System.ConsoleColor.Gray;
            }

            //Parse commands from masterID only
            if (offline > 0 || fromAgentID != masterID) return;

            //Remember IM session
            masterIMSessionID = imSessionID;
            string command;
            if (message.Substring(0, 1) == "/") command = message.Substring(1);
            else command = message;
            ParseCommand(false, message, fromAgentName, fromAgentID, imSessionID);
        }


        void OnFriendNotificationEvent(LLUUID friendID, bool online)
        {
            Console.ForegroundColor = System.ConsoleColor.Gray;
            Console.BackgroundColor = System.ConsoleColor.DarkBlue;
            //string name1 = Client.Avatars.GetAvatarName(friendID);
            //string name2 = Client.Avatars.GetAvatarName(friendID);
            //Console.WriteLine(" {0} is online ({1}) ", name1, online);
            //Console.WriteLine(" {0} is online ({1}) ", name2, online);
            //if (online) Console.WriteLine(" {0} is online ({1}) ", friendID, online);
            //else Console.WriteLine(" {0} is offline ({1}) ", friendID, online);
            //FIXME


            Console.BackgroundColor = System.ConsoleColor.Black;

            //FIXME!!!
            //Client.Avatars.BeginGetAvatarName(friendID, new AvatarManager.AgentNamesCallback(AgentNamesHandler));
        }

        //void AgentNamesHandler(Dictionary<LLUUID, string> agentNames)
        //{
        // lock(Friends)
        //    {
        //        foreach (KeyValuePair<LLUUID, string> agent in agentNames)
        //        {
        //        //FIXME!!!
        //            //Friends[agent.Key] = agent.Value;
        //            Console.WriteLine("AgentNames: key={0}, name={1}", agent.Key, agent.Value);
        //        }
        //    }
        //}



        void OnMoneyBalanceReply(Packet packet, Simulator simulator)
        {
            MoneyBalanceReplyPacket reply = (MoneyBalanceReplyPacket)packet;
            string desc = Helpers.FieldToString(reply.MoneyData.Description);
            int changeAmount = reply.MoneyData.MoneyBalance - currentBalance;
            currentBalance = reply.MoneyData.MoneyBalance;

            char[] splitChar = { ' ' };
            string[] msg = desc.Split(splitChar);
            if (msg.Length == 5)
            {
                if (!int.TryParse(msg[4].Substring(2, msg[4].Length - 3), out changeAmount))
                    Console.WriteLine("* UNEXPECTED PAYMENT MESSAGE");
                else if (msg[0] + " " + msg[1] == "You paid")
                    AcknowledgePayment(msg[2] + " " + msg[3], changeAmount * -1);
                else if (msg[2] + " " + msg[3] == "paid you")
                    AcknowledgePayment(msg[0] + " " + msg[1], changeAmount);
                else
                    Console.WriteLine("* UNEXPECTED PAYMENT MESSAGE");
            }
                
            if (desc.Length > 1)
            {
                Console.ForegroundColor = System.ConsoleColor.Cyan;
                Console.WriteLine(TimeStamp() + "* " + desc);
            }
            Console.ForegroundColor = System.ConsoleColor.Green;
            Console.WriteLine(TimeStamp() + "* Balance: L$" + currentBalance);
            Console.ForegroundColor = System.ConsoleColor.Gray;
        }



        void OnTeleportFinish(Packet packet, Simulator simulator)
        {
            Console.WriteLine(TimeStamp() + "* FINISHED TELEPORT TO REGION AT " + regionX + ", " + regionY);
            TeleportFinishPacket reply = (TeleportFinishPacket)packet;
            regionX = (int)(reply.Info.RegionHandle >> 32);
            regionY = (int)(reply.Info.RegionHandle & 0xFFFFFFFF);
            if (reply.Info.AgentID != Client.Network.AgentID) return;
            if (lastAppearance.AgentData.SerialNum > 0) Client.Network.SendPacket(lastAppearance);
            Client.Self.Status.SendUpdate();
        }



        void OnAvatarMovedEvent(Simulator simulator, AvatarUpdate avatar, ulong regionHandle, ushort timeDilation)
        {
            Avatar test;
            if (!avatars.TryGetValue(avatar.LocalID, out test)) return;
            lock (avatars)
            {
                string name = avatars[avatar.LocalID].Name;
                //if (avatars[avatar.LocalID].ID == Client.Network.AgentID)
                //{
                //this is a temp hack to update region corner X/Y any time any av moves (not just the follow target)
                regionX = (int)(regionHandle >> 32);
                regionY = (int)(regionHandle & 0xFFFFFFFF);
                //}
                if (avatars[avatar.LocalID].Name == followName)
                {
                    avatars[avatar.LocalID].Position = avatar.Position;
                    avatars[avatar.LocalID].Rotation = avatar.Rotation;
                    if (!Follow(name))
                    {
                        Client.Self.Status.SendUpdate();
                    }
                }
            }
        }



        void OnNewAvatarEvent(Simulator simulator, Avatar avatar, ulong regionHandle, ushort timeDilation)
        {
            lock (avatars)
            {
                avatars[avatar.LocalID] = avatar;
            }
        }
        void OnAppearance(Packet packet, Simulator simulator)
        {
            AvatarAppearancePacket appearance = (AvatarAppearancePacket)packet;
            lock (appearances)
            {
                appearances[appearance.Sender.ID] = appearance;
            }
        }
        void OnNewPrimEvent(Simulator simulator, PrimObject prim, ulong regionHandle, ushort timeDilation)
        {
            lock (prims)
            {
                prims[prim.LocalID] = prim;
            }
        }
        void OnObjectKilledEvent(Simulator simulator, uint objectID)
        {
            lock (prims)
            {
                if (prims.ContainsKey(objectID))
                    prims.Remove(objectID);
            }
            lock (avatars)
            {
                if (avatars.ContainsKey(objectID))
                    avatars.Remove(objectID);
            }
        }
        void OnPrimMovedEvent(Simulator simulator, PrimUpdate prim, ulong regionHandle, ushort timeDilation)
        {
            lock (prims)
            {
                if (prims.ContainsKey(prim.LocalID))
                {
                    prims[prim.LocalID].Position = prim.Position;
                    prims[prim.LocalID].Rotation = prim.Rotation;
                }
            }
        }


        void OnFetchInventoryReply(Packet packet, Simulator sim)
        {
            //fetch inventory item data - FIXME!!!
            FetchInventoryReplyPacket reply = (FetchInventoryReplyPacket)packet;
            FetchInventoryReplyPacket.InventoryDataBlock obj = new FetchInventoryReplyPacket.InventoryDataBlock();

            Console.WriteLine("Inventory info: " + obj.Name);

            //rez object from inventory - FIXME!!!
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
            //FIXME -- DETECT WHAT masterID IS GRABBING
            //if (reply.AgentData.AgentID == masterID)
            //{
            Console.WriteLine("* Touched/grabbed object " + reply.ObjectData[0].ObjectLocalID);
            //}
        }

        //FRIEND REQUESTS (FIXME!!!) #########################################
        void OnRequestFriendship(Packet packet, Simulator simulator)
        {
            RequestFriendshipPacket reply = (RequestFriendshipPacket)packet;
            if (reply.AgentData.AgentID != masterID) return;
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

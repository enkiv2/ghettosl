using System;
using System.Collections.Generic;
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

            Client.Network.OnConnected += new NetworkManager.ConnectedCallback(OnConnectedEvent);
            Client.Network.OnSimDisconnected += new NetworkManager.SimDisconnectCallback(OnSimDisconnectEvent);
            Client.Objects.OnAvatarMoved += new ObjectManager.AvatarMovedCallback(OnAvatarMovedEvent);
            Client.Objects.OnNewAvatar += new ObjectManager.NewAvatarCallback(OnNewAvatarEvent);
            Client.Objects.OnNewPrim += new ObjectManager.NewPrimCallback(OnNewPrimEvent);
            Client.Objects.OnObjectKilled += new ObjectManager.KillObjectCallback(OnObjectKilledEvent);
            Client.Objects.OnPrimMoved += new ObjectManager.PrimMovedCallback(OnPrimMovedEvent);
            Client.Avatars.OnFriendNotification += new AvatarManager.FriendNotificationCallback(OnFriendNotificationEvent);
            Client.Self.OnInstantMessage += new InstantMessageCallback(OnInstantMessageEvent);
            Client.Self.OnTeleport += new TeleportCallback(OnTeleportEvent);
        }


        void OnConnectedEvent(object sender)
        {
            Console.WriteLine("* CONNECTED");
            //Retrieve offline IMs
            Client.Grid.AddEstateSims();
            //FIXME!!! - ADD Client.Self.RetrieveInstantMessages() TO CORE!
            RetrieveInstantMessagesPacket p = new RetrieveInstantMessagesPacket();
            p.AgentData.AgentID = Client.Network.AgentID;
            p.AgentData.SessionID = Client.Network.SessionID;
            Client.Network.SendPacket(p);
        }



        void OnSimDisconnectEvent(Simulator sim, NetworkManager.DisconnectType type)
        {
            Console.WriteLine("* DISCONNECTED FROM SIM: " + type.ToString());
            if (logout || sim.IPEndPoint != Client.Network.CurrentSim.IPEndPoint) return;
            Client.Network.Logout();
            do Thread.Sleep(5000);
            while (!Login());
        }

        

        void OnAlertMessage(Packet packet, Simulator sim)
        {
            AlertMessagePacket p = (AlertMessagePacket)packet;
            Console.WriteLine(TimeStamp() + "* " + Helpers.FieldToString(p.AlertData.Message));
        }


        
        void OnChatEvent(string message, byte audible, byte chatType, byte sourceType, string name, LLUUID fromAgentID, LLUUID ownerID, LLVector3 position)
        {
            if (chatType > 3 || audible < 1) return;
            char[] splitChar = { ' ' };
            string[] msg = message.Split(splitChar);
            if (msg[0].ToLower() != "/me")
                Console.WriteLine(TimeStamp() + "(type " + chatType + ") " + name + ": " + message);
            else
            {
                message = String.Join(" ", msg, 1, msg.Length - 1);
                Console.WriteLine(TimeStamp() + "(type " + chatType + ") * " + name + " " + message);
            }
        }



        void OnInstantMessageEvent(LLUUID fromAgentID, string fromAgentName, LLUUID toAgentID, uint parentEstateID, LLUUID regionID, LLVector3 position, byte dialog, bool groupIM, LLUUID imSessionID, DateTime timestamp, string message, byte offline, byte[] binaryBucket)
        {
            //Teleport requests (dialog set to 22)
            if (dialog == (int)InstantMessageDialog.RequestTeleport && (fromAgentID == masterID || message == passPhrase))
            {
                Console.WriteLine("* Accepting teleport request from " + fromAgentName + " (" + message + ")");
                Client.Self.TeleportLureRespond(fromAgentID, true);
                return;
            }
            //Receive inventory
            else if (dialog == (int)InstantMessageDialog.GiveInventory)
            {
                Console.WriteLine(TimeStamp() + "* " + fromAgentName + " gave you an object named \"" + message + "\"");
                return;
            }

            CreateMessageWindow(fromAgentID, fromAgentName, dialog, imSessionID);

            //Display IM in console
            Console.WriteLine(TimeStamp() + "(dialog " + dialog + ") <" + fromAgentName + ">: " + message);

            //Parse commands from masterID only
            if (offline > 0 || fromAgentID != masterID) return;

            //Remember IM session
            masterIMSessionID = imSessionID;
            ParseCommand(false, message, fromAgentName, fromAgentID, imSessionID);
        }


        void OnFriendNotificationEvent(LLUUID friendID, bool online)
        {
            if (online) Console.WriteLine("* ONLINE: {0}", friendID);
            else Console.WriteLine("* OFFLINE: {0}", friendID);
            //FIXME!!!
            Client.Avatars.BeginGetAvatarName(friendID, new AvatarManager.AgentNamesCallback(AgentNamesHandler));
        }
        void AgentNamesHandler(Dictionary<LLUUID, string> agentNames)
        {
            foreach (KeyValuePair<LLUUID, string> agent in agentNames)
            {
                //FIXME!!!
                //Friends[agent.Key].Name = agent.Value;
                //Friends[agent.Key].ID = agent.Key;
                Console.WriteLine("agent: {0} {1}", agent.Key, agent.Value);
            }
        }



        void OnMoneyBalanceReply(Packet packet, Simulator simulator)
        {
            MoneyBalanceReplyPacket reply = (MoneyBalanceReplyPacket)packet;
            string desc = Helpers.FieldToString(reply.MoneyData.Description);
            int changeAmount = reply.MoneyData.MoneyBalance - currentBalance;
            currentBalance = reply.MoneyData.MoneyBalance;

            char[] splitChar = { ' ' };
            string[] msg = desc.Split(splitChar);
            if (msg.Length > 3 && msg[2] + " " + msg[3] == "paid you")
                AcknowledgePayment(msg[0] + " " + msg[1], changeAmount);

            if (desc.Length > 1) Console.WriteLine("* " + desc);
            Console.WriteLine(TimeStamp() + "* Balance: L$" + currentBalance);
        }



        void OnTeleportEvent(Simulator sim, string message, TeleportStatus status)
        {
            Console.WriteLine("* TELEPORT (" + status.ToString() + "): " + message);
        }



        void OnTeleportFinish(Packet packet, Simulator simulator)
        {

            TeleportFinishPacket reply = (TeleportFinishPacket)packet;
            Console.WriteLine("* FINISHED TELEPORT TO REGION " + regionX + "," + regionY);
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


    }
}

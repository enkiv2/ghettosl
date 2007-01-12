using libsecondlife;
using libsecondlife.Packets;
using System;
using System.Collections.Generic;
using System.Text;

namespace ghetto
{
    class EventManager
    {

        public GhettoSL.UserSession Session;

        public EventManager(GhettoSL.UserSession session)
        {
            Session = session;
            Session.Client.Network.OnConnected += new NetworkManager.ConnectedCallback(Network_OnConnected);
            Session.Client.Network.OnSimDisconnected += new NetworkManager.SimDisconnectCallback(Network_OnSimDisconnected);
            Session.Client.Objects.OnNewPrim += new ObjectManager.NewPrimCallback(Objects_OnNewPrim);
            Session.Client.Objects.OnObjectKilled += new ObjectManager.KillObjectCallback(Objects_OnObjectKilled);
            Session.Client.Objects.OnPrimMoved += new ObjectManager.PrimMovedCallback(Objects_OnPrimMoved);
            Session.Client.Self.OnChat += new MainAvatar.ChatCallback(Self_OnChat);

            Session.Client.Network.RegisterCallback(PacketType.AlertMessage, new NetworkManager.PacketCallback(Callback_AlertMessage));
            Session.Client.Network.RegisterCallback(PacketType.MoneyBalanceReply, new NetworkManager.PacketCallback(Callback_MoneyBalanceReply));
        }

        void Callback_AlertMessage(Packet packet, Simulator sim)
        {
            AlertMessagePacket p = (AlertMessagePacket)packet;
            Console.ForegroundColor = System.ConsoleColor.Cyan;
            Display.AlertMessage(Session.SessionNumber, Helpers.FieldToString(p.AlertData.Message));
            Console.ForegroundColor = System.ConsoleColor.Gray;
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
            if (msg.Length <= 4 || !int.TryParse(msg[4].Substring(1, msg[4].Length - 2), out amount))
            {
                Display.Error(Session.SessionNumber, "Unexpected MoneyBalanceReplyPacket.MoneyDataBlock.Description");
                return;
            }

            if (msg[0] + " " + msg[1] == "You paid")
            {
                name = msg[2] + " " + msg[3];
                amount *= -1;
            }
            else if (msg[2] + " " + msg[3] == "paid you")
            {
                name = msg[0] + " " + msg[1];
            }

            Display.Balance(Session.SessionNumber, reply.MoneyData.MoneyBalance, amount, name, desc);
        }


        void Network_OnConnected(object sender)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("* CONNECTED");
            Console.ForegroundColor = ConsoleColor.Gray;

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
            Console.ForegroundColor = System.ConsoleColor.Red;
            Console.WriteLine("* DISCONNECTED FROM SIM: " + reason.ToString());
            Console.ForegroundColor = System.ConsoleColor.Gray;
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

    }
}
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
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Xml.Serialization;
using libsecondlife;
using libsecondlife.Packets;

namespace ghetto
{
    class GhettoSL
    {
        //GLOBAL VARIABLES ####################################################
        SecondLife Client = new SecondLife();
        Dictionary<uint, Avatar> avatars;
        Dictionary<uint, PrimObject> prims;
        Dictionary<uint, Avatar> imWindows;
        Dictionary<LLUUID, Avatar> Friends;
        Dictionary<LLUUID, AvatarAppearancePacket> appearances;
        AgentSetAppearancePacket lastAppearance = new AgentSetAppearancePacket();
        static bool logout = false;
        public string firstName;
        public string lastName;
        public string password;
        public string passPhrase;
        public LLUUID masterID;
        LLUUID masterIMSessionID;
        string followName;
        uint controls = 0;
        int currentBalance;
        int regionX;
        int regionY;

        //Variables used by Auto-Camp
        string campChairTextMatch;
        //END OF GLOBAL VARIABLES #############################################


        //BEGIN MAIN VOID #####################################################
        static void Main(string[] args)
        {
            //Make sure command line arguments are valid
            string[] commandLineArguments = args;
            if (args.Length < 5)
            {
                Console.WriteLine("Usage: GhettoSL <firstName> <lastName> <password> <passPhrase> <masterID> [quiet] [scriptFile]");
                return;
            }
            bool quiet = false;
            string scriptFile = "";

            if (args.Length > 5 && args[5].ToLower() == "quiet") quiet = true;
            if (args.Length > 6) scriptFile = args[6];

            GhettoSL ghetto = new GhettoSL(args[0], args[1], args[2], args[3], new LLUUID(args[4]), quiet,scriptFile);
        }
        //END OF MAIN VOID ####################################################


        //GHETTOSL VOID ######################################################
        public GhettoSL(string first, string last, string pass, string phrase, LLUUID master, bool quiet,string scriptFile)
        {
            //RotBetween Test
            //LLVector3 a = new LLVector3(1, 0, 0);
            //LLVector3 b = new LLVector3(0, 0, 1);
            //Console.WriteLine("RotBetween: " + Helpers.RotBetween(Helpers.VecNorm(a), Helpers.VecNorm(b)));
            //Console.ReadLine();
            //return;

            firstName = first;
            lastName = last;
            password = pass;
            passPhrase = phrase;
            masterID = master;
            avatars = new Dictionary<uint, Avatar>();
            Friends = new Dictionary<LLUUID, Avatar>();
            prims = new Dictionary<uint, PrimObject>();
            appearances = new Dictionary<LLUUID, AvatarAppearancePacket>();
            imWindows = new Dictionary<uint, Avatar>();

            Client.Debug = false;

            //Add callbacks for events

            Client.Network.RegisterCallback(PacketType.AvatarAppearance, new NetworkManager.PacketCallback(OnAppearance));
            Client.Network.RegisterCallback(PacketType.FetchInventoryReply, new NetworkManager.PacketCallback(OnFetchInventoryReply));
            Client.Network.RegisterCallback(PacketType.MoneyBalanceReply, new NetworkManager.PacketCallback(OnMoneyBalanceReply));
            Client.Network.RegisterCallback(PacketType.ObjectUpdate, new NetworkManager.PacketCallback(OnObjectUpdateEvent));
            Client.Network.RegisterCallback(PacketType.RequestFriendship, new NetworkManager.PacketCallback(OnRequestFriendship));
            Client.Network.RegisterCallback(PacketType.TeleportFinish, new NetworkManager.PacketCallback(OnTeleportFinish));
            
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
            
            if (!quiet) Client.Self.OnChat += new ChatCallback(OnChatEvent);

            //Attempt to login, and exit if failed
            while (!Login()) Thread.Sleep(5000);

            //Run script
            if (scriptFile != "") LoadScript(scriptFile);

            //Accept commands
            do
            {
                ParseCommand(true, Console.ReadLine(), Client.Self.FirstName + " " + Client.Self.LastName, new LLUUID(), new LLUUID());
            }
            while (!logout);

            Client.Network.Logout();
        }
        //END OF GHETTOSL VOID ###############################################


        //ON SIM CONNECT ######################################################
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
        //END OF ON CONNECT ###################################################


        //ON SIM DISCONNECT ###################################################
        void OnSimDisconnectEvent(Simulator sim, NetworkManager.DisconnectType type)
        {
            Console.WriteLine("* DISCONNECTED FROM SIM: " + type.ToString());
            if (logout || sim.IPEndPoint != Client.Network.CurrentSim.IPEndPoint) return;
            Client.Network.Logout();
            do Thread.Sleep(5000);
            while (!Login());
        }
        //END OF SIM DISCONNECT ###############################################


        //LOGIN SEQUENCE ######################################################
        bool Login()
        {
            Console.WriteLine("Logging in as " + firstName + " " + lastName + "...");

            //Attempt to log in
            if (!Client.Network.Login(firstName, lastName, password, "GhettoSL", "ghetto@obsoleet.com"))
            {
                Console.WriteLine("Login failed.");
                return false;
            }

            //Succeeded - Wait for simulator name or disconnection
            Simulator sim = Client.Network.CurrentSim;
            while (Client.Network.Connected && (!sim.Connected || sim.Region.Name == "" || Client.Grid.SunDirection.X == 0))
            {
                Thread.Sleep(100);
            }

            //Halt if disconnected
            if (!Client.Network.Connected) return false;

            //We are in!
            Console.WriteLine(RPGWeather());
            Console.WriteLine("Location: <" + Client.Self.Position + ">");

            //Fix the "bot squat" animation
            controls = 0;
            SendAgentUpdate();

            return true;
        }
        //END OF LOGIN ########################################################


        //CHAT FROM SIMULATOR (USERS AND OBJECTS) #############################
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
        //END OF CHAT FROM SIMULATOR ##########################################


        //INSTANT MESSAGE STUFF ###############################################
        void OnInstantMessageEvent(LLUUID fromAgentID, string fromAgentName, LLUUID toAgentID, uint parentEstateID, LLUUID regionID, LLVector3 position, byte dialog, bool groupIM, LLUUID imSessionID, DateTime timestamp, string message, byte offline, byte[] binaryBucket)
        {
            //Teleport requests (dialog set to 22)
            if (dialog == 22 && (fromAgentID == masterID || message == passPhrase))
            {
                Console.WriteLine("* Accepting teleport request from " + fromAgentName + " (" + message + ")");
                Client.Self.TeleportLureRespond(fromAgentID, true);
                return;
            }
            //Receive inventory
            else if (dialog == 4)
            {
                Console.WriteLine(TimeStamp()+"* " + fromAgentName + " gave you an object named \"" + message+"\"");
                return;
            }

            CreateMessageWindow(fromAgentID, fromAgentName, dialog, imSessionID);

            //Display IM in console
            Console.WriteLine(TimeStamp()+"(dialog " + dialog + ") <" + fromAgentName + ">: " + message);

            //Parse commands from masterID only
            if (offline  > 0 || fromAgentID != masterID) return;

            //Remember IM session
            masterIMSessionID = imSessionID;
            ParseCommand(false,message, fromAgentName, fromAgentID, imSessionID);
        }
        //END OF INSTANT MESSAGE STUFF ########################################


        //IM WINDOW CREATION ##################################################
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
        //END OF IM WINDOW CREATION ###########################################


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

        //ON FRIEND NOTIFICATION ###############################################
        void OnFriendNotificationEvent(LLUUID friendID, bool online)
        {
            if (online) Console.WriteLine("* ONLINE: {0}", friendID);
            else Console.WriteLine("* OFFLINE: {0}", friendID);
            //FIXME!!!
            Client.Avatars.BeginGetAvatarName(friendID, new AvatarManager.AgentNamesCallback(AgentNamesHandler));
        }
        void AgentNamesHandler(Dictionary <LLUUID,string> agentNames)
        {
            foreach (KeyValuePair<LLUUID, string> agent in agentNames)
            {
                //FIXME!!!
                //Friends[agent.Key].Name = agent.Value;
                //Friends[agent.Key].ID = agent.Key;
                Console.WriteLine("agent: {0} {1}", agent.Key, agent.Value);
            }
        }
        //END OF FRIEND NOTIFICATION ##########################################


        //ON TELEPORT FINISH ##################################################
        void OnTeleportEvent(string message, TeleportStatus status)
        {
            Console.WriteLine("* TELEPORT ("+status.ToString()+"): " + message);
        }
        void OnTeleportFinish(Packet packet, Simulator simulator)
        {
            TeleportFinishPacket reply = (TeleportFinishPacket)packet;
            Console.WriteLine("* FINISHED TELEPORT TO REGION " + regionX+","+regionY);
            if (reply.Info.AgentID != Client.Network.AgentID) return;
            if (lastAppearance.AgentData.SerialNum > 0) Client.Network.SendPacket(lastAppearance);
            controls = 0;
            SendAgentUpdate();
        }
        //END OF TELEPORT FINISH ##############################################


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


        //MONEY BALANCE STUFF #################################################
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
            Console.WriteLine(TimeStamp()+"* Balance: L$" + currentBalance);
        }

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
        //END OF MONEY BALANCE ################################################


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


        //AGENT UPDATE ########################################################
        void SendAgentUpdate()
        {
            AgentUpdatePacket p = new AgentUpdatePacket();
            p.AgentData.Far = 96.0f;
            p.AgentData.CameraAtAxis = new LLVector3(0,0,0);
            p.AgentData.CameraCenter = new LLVector3(0,0,0);
            p.AgentData.CameraLeftAxis = new LLVector3(0,0,0);
            p.AgentData.CameraUpAxis = new LLVector3(0,0,0);
            p.AgentData.HeadRotation = new LLQuaternion(0, 0, 0, 1); ;
            p.AgentData.BodyRotation = new LLQuaternion(0, 0, 0, 1); ;
            p.AgentData.AgentID = Client.Network.AgentID;
            p.AgentData.SessionID = Client.Network.SessionID;
            p.AgentData.ControlFlags = controls;
            Client.Network.SendPacket(p);
        }
        //END OF AGENT UPDATE #################################################


        //AGENT ANIMATION #####################################################
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
        //END OF AGENT ANIMATION ##############################################


        //AGENT SIT ###########################################################
        void Sit(LLUUID targetID, LLVector3 offset)
        {
            AgentRequestSitPacket p = new AgentRequestSitPacket();
            p.AgentData.AgentID = Client.Network.AgentID;
            p.AgentData.SessionID = Client.Network.SessionID;
            p.TargetObject.TargetID = targetID;
            p.TargetObject.Offset = offset;
            Client.Network.SendPacket(p);

            AgentSitPacket sit = new AgentSitPacket();
            sit.AgentData.AgentID = Client.Network.AgentID;
            sit.AgentData.SessionID = Client.Network.SessionID;
            Client.Network.SendPacket(sit);

            controls = 0;
            SendAgentUpdate();
        }
        //END OF AGENT SIT ####################################################


        //COMMAND PARSING #####################################################
        void ParseCommand(bool console, string message, string fromAgentName, LLUUID fromAgentID, LLUUID imSessionID)
        {
            if (message.Length == 0) return;
            char[] splitChar = { ' ' };
            string[] msg = message.Split(splitChar);
            if (msg[0] == null || msg[0] == "") return;

            string command = msg[0].ToLower();
            if (command.Substring(0, 1) == "/") command = command.Substring(1);
            else if (console)
            {
                Client.Self.Chat(message, 0, MainAvatar.ChatType.Normal);
                return;
            }

            string response = "";

            //Store command arguments in "details" variable
            string details = null;
            int i = 1;
            if (command == "re" || command == "im") i++;
            while (i < msg.Length)
            {
                details += msg[i];
                if (i + 1 < msg.Length) details += " ";
                i++;
            }

            switch (command)
            {
                case "button1":
                    {
                        Client.Self.Touch(9835045);
                        break;
                    }
                case "button2":
                    {
                        Client.Self.Touch(9835044);
                        break;
                    }
                case "anim":
                    {
                        SendAgentAnimation((LLUUID)details, true);
                        break;
                    }
                case "stopanim":
                    {
                        SendAgentAnimation((LLUUID)details, false);
                        break;
                    }
                case "backflip":
                    {
                        SendAgentAnimation((LLUUID)"c4ca6188-9127-4f31-0158-23c4e2f93304", true); //backflip
                        break;
                    }
                case "camp":
                    {
                        uint localID = FindObjectByText(details.ToLower());
                        if (localID > 0)
                        {
                            response = "Match found. Camping...";
                            Sit(prims[localID].ID, new LLVector3(0, 0, 0));
                        }
                        else response = "No matching objects found.";
                        break;
                    }
                case "clear":
                    {
                        Console.Clear();
                        break;
                    }
                case "clone":
                    {
                        if (msg.Length != 3) return;
                        if (Clone(msg[1], msg[2])) response = "Cloning...";
                        else response = "Error: Avatar not found";
                        break;
                    }
                case "die":
                    {
                        logout = true;
                        response = "Shutting down...";
                        break;
                    }
                case "quit":
                    {
                        logout = true;
                        response = "Shutting down...";
                        break;
                    }
                case "drag":
                    {
                        LLUUID findID = (LLUUID)msg[1];
                        foreach (PrimObject prim in prims.Values)
                        {
                            if (prim.ID != findID) continue;
                            LLVector3 targetPos = new LLVector3(prim.Position.X,prim.Position.Y,prim.Position.Z + 10);
                            Client.Self.Grab(prim.LocalID);
                            Client.Self.GrabUpdate(prim.ID, targetPos);
                            Client.Self.DeGrab(prim.LocalID);
                            response = "DRAGGED OBJECT " + prim.LocalID + " TO <" + targetPos + ">";
                            break;
                        }
                        if (response == "") response = "NO OBJECT FOUND MATCHING " + findID;
                        break;
                    }
                case "face":
                    {
                        LLUUID findID = (LLUUID)msg[1];
                        foreach (PrimObject prim in prims.Values)
                        {
                            if (prim.ID != findID) continue;
                            LLVector3 targetPos = new LLVector3(prim.Position.X,prim.Position.Y,prim.Position.Z + 10);
                            LLQuaternion between = Helpers.RotBetween(Client.Self.Position, prim.Position);
                            response = "FACING <" + targetPos + "> "+between;
                            //FIXME!!!

                            break;
                        }
                        if (response == "") response = "NO OBJECT FOUND MATCHING " + findID;
                        break;
                    }
                case "follow":
                    {
                        if (msg.Length == 2 && msg[1].ToLower() == "off")
                        {
                            followName = null;
                            response = "Stopped following";
                        }
                        else if (msg.Length == 3)
                        {
                            if (Follow(msg[1] + " " + msg[2])) response = "Following " + followName + "...";
                            else
                            {
                                response = "Error: Avatar not found";
                                followName = null;
                            }
                        }                    
                    }
                    break;
                case "fly":
                    {
                        controls = controls | (uint)Avatar.AgentUpdateFlags.AGENT_CONTROL_FLY;
                        SendAgentUpdate(); //AGENT_CONTROL_FLY
                        break;
                    }
                case "im":
                    {
                        if (msg.Length > 2)
                        {
                            Client.Self.InstantMessage((LLUUID)msg[1], msg[2]);
                            response = "Message sent.";
                        }
                        break;
                    }
                case "inventory":
                    {
                        if (msg.Length > 2)
                        {
                            FetchInventoryPacket p = new FetchInventoryPacket();
                            p.AgentData.AgentID = Client.Network.AgentID;
                            p.AgentData.SessionID = Client.Network.SessionID;
                            FetchInventoryPacket.InventoryDataBlock data = new FetchInventoryPacket.InventoryDataBlock();
                            data.ItemID = (LLUUID)msg[1];
                            data.OwnerID = Client.Network.AgentID;
                            Client.Network.SendPacket(p);
                        }
                        break;
                    }
                case "land":
                    {
                        //controls = controls | ~(uint)Avatar.AgentUpdateFlags.AGENT_CONTROL_FLY; //WRONG
                        controls = 0; //still not right
                        SendAgentUpdate();
                        break;
                    }
                case "listen":
                    {
                        Client.Self.OnChat += new ChatCallback(OnChatEvent);
                        response = "Displaying object/avatar chat.";
                        break;
                    }
                case "me":
                    {
                        Client.Self.Chat("/me "+details, 0, MainAvatar.ChatType.Normal);
                        break;
                    }
                case "pay":
                    {
                        Client.Self.GiveMoney((LLUUID)msg[2], int.Parse(msg[1]), "");
                        response = "Payment sent to "+msg[2]+".";
                        break;
                    }
                case "payme":
                    {
                        if (console) Client.Self.GiveMoney(masterID, int.Parse(msg[1]), "");
                        else Client.Self.GiveMoney(fromAgentID, int.Parse(msg[1]), "");
                        response = "Payment sent.";
                        break;
                    }
                case "ping":
                    {
                        Client.Self.InstantMessage(fromAgentID, "pong", imSessionID);
                        break;
                    }
                case "quiet":
                    {
                        response = "Stopped listening to chat.";
                        Client.Self.OnChat -= new ChatCallback(OnChatEvent);
                        break;
                    }
                case "re":
                    {
                        if (msg.Length == 1)
                        {
                            int count = imWindows.Count;
                            response = count + " active IM session";
                            if (count != 1) response += "s";
                            foreach (Avatar av in imWindows.Values)
                            {
                                response += "\n"+av.LocalID+". "+av.Name;
                            }
                        }
                        else if (msg.Length == 2)
                        {
                            int isNumeric;
                            if (!int.TryParse(msg[1], out isNumeric))
                            {
                                response = "Invalid IM window number";
                            }
                            else
                            {
                                uint index = (uint)(-1 + int.Parse(msg[1]));
                                if (index < 0 || index >= imWindows.Count) response = "Invalid IM window number";
                                else
                                {
                                    Client.Self.InstantMessage(imWindows[index].ID, details, imWindows[index].PartnerID);
                                    response = "Message sent.";
                                }
                            }
                        }
                        break;
                    }
                case "relog":
                    {
                        response = "Relogging...";
                        Client.Network.Logout();
                        Thread.Sleep(1000);
                        while (!Login()) Thread.Sleep(5000);
                        break;
                    }
                case "run":
                    {
                        Client.Self.SetAlwaysRun(true);
                        response = "Running enabled";
                        break;
                    }
                case "say":
                    {
                        Client.Self.Chat(details, 0, MainAvatar.ChatType.Normal);
                        break;
                    }
                case "script":
                    {
                        if (msg.Length > 0) LoadScript(msg[1]+".script");
                        break;
                    }
                case "shout":
                    {
                        Client.Self.Chat(details, 0, MainAvatar.ChatType.Shout);
                        break;
                    }
                case "teleport":
                    {
                        if (msg.Length < 5) return;
                        string simName = String.Join(" ",msg,1,msg.Length - 4);
                        if (console) Console.WriteLine("* Teleporting to " + simName + "...");
                        else Client.Self.InstantMessage(fromAgentID,"Teleporting to " + simName + "...");
                        float x = float.Parse(msg[msg.Length - 3]);
                        float y = float.Parse(msg[msg.Length - 2]);
                        float z = float.Parse(msg[msg.Length - 1]);
                        LLVector3 tPos;
                        if (x == 0 || y == 0 || z == 0) tPos = new LLVector3(128, 128, 0);
                        else tPos = new LLVector3(x, y, z);
                        Client.Self.Teleport(simName, tPos);
                        break;
                    }
                case "sit":
                    {
                        if (msg.Length < 2) return;
                        Sit((LLUUID)details, new LLVector3());
                        break;
                    }
                case "sitg":
                    {
                        controls = (uint)Avatar.AgentUpdateFlags.AGENT_CONTROL_SIT_ON_GROUND;
                        SendAgentUpdate();
                        break;
                    }
                case "stand":
                    {
                        controls = (uint)Avatar.AgentUpdateFlags.AGENT_CONTROL_STAND_UP;
                        SendAgentUpdate();
                        //SendAgentAnimation((LLUUID)"2408fe9e-df1d-1d7d-f4ff-1384fa7b350f", true); //stand
                        break;
                    }
                case "time":
                    {
                        response = RPGWeather();
                        break;
                    }
                case "touch":
                    {
                        LLUUID findID = (LLUUID)msg[1];
                        foreach (PrimObject prim in prims.Values)
                        {
                            if (prim.ID != findID) continue;
                            Client.Self.Touch(prim.LocalID);
                            response = "TOUCHED OBJECT " + prim.LocalID;
                            break;
                        }
                        if (response == "") response = "NO OBJECT FOUND MATCHING " + findID;
                        break;
                    }
                case "touchid":
                    {
                        Client.Self.Touch(uint.Parse(msg[1]));
                        break;
                    }
                case "tp": //FIXME!!!
                    {
                        //send me a tp when I ask for one
                        StartLurePacket p = new StartLurePacket();
                        p.AgentData.AgentID = Client.Network.AgentID;
                        p.AgentData.SessionID = Client.Network.SessionID;
                        string invite = "Join me in " + Client.Network.CurrentSim.Region.Name + "!";
                        p.Info.Message = Helpers.StringToField(invite);
                        p.Info.TargetID = fromAgentID;
                        p.Info.LureType = 4;
                        Client.Network.SendPacket(p);
                        break;
                    }
                case "walk":
                    {
                        Client.Self.SetAlwaysRun(false);
                        response = "Running disabled";
                        break;
                    }
                case "whisper":
                    {
                        Client.Self.Chat(details, 0, MainAvatar.ChatType.Whisper);
                        break;
                    }
                case "who":
                    {
                        if (avatars.Count == 1) response = "1 person is nearby.";
                        else response = avatars.Count + " people are nearby.";
                        foreach(Avatar a in avatars.Values) response += "\n"+a.Name+" ("+(int)Helpers.VecDist(Client.Self.Position,a.Position)+"m) : "+a.ID;
                        break;
                    }
            }
            if (response == "") return;
            else if (console) Console.WriteLine(TimeStamp()+"* " + response);
            else Client.Self.InstantMessage(fromAgentID, response, imSessionID);
        }
        //END OF COMMAND PARSING ##############################################


        //RPG-STYLE SUN DESCRIPTION ###########################################
        string RPGWeather()
        {
            LLVector3 SunDirection = Client.Grid.SunDirection;
            //Console.WriteLine("Sun: " + SunDirection.X + " " + SunDirection.Y + " " + SunDirection.Z);
            string response = "";
            if (SunDirection.Z > 0.9) response = "It is midday";
            else if (SunDirection.Z > 0.5) response = "The sun is shining";
            else if (SunDirection.Z > 0.1)
            {
                if (SunDirection.X > 0) response = "It is still morning";
                else response = "It is late afternoon";
            }
            else if (SunDirection.Z > 0)
            {
                if (SunDirection.X > 0) response = "The sun is rising";
                else response = "The sun is setting";
            }
            else if (SunDirection.Z < -0.9) response = "It is the middle of the night";
            else if (SunDirection.Z < -0.5) response = "The moon lingers overhead";
            else if (SunDirection.Z < -0.1) response = "It is nighttime";
            else if (SunDirection.Z < 0)
            {
                if (SunDirection.X > 0) response = "It is not yet dawn";
                else response = "The night is still young";
            }
            return response+" in "+Client.Network.CurrentSim.Region.Name+".";
        }
        //END OF RPG WEATHER ##################################################


        uint FindObjectByText(string textValue)
        {
            campChairTextMatch = textValue;
            uint localID = 0;
            foreach (PrimObject prim in prims.Values)
            {
                int len = campChairTextMatch.Length;
                string match = prim.Text.Replace("\n", ""); //Strip newlines
                if (match.Length < len) continue; //Text is too short to be a match
                else if (match.Substring(0, len).ToLower() == campChairTextMatch)
                {
                    localID = prim.LocalID;
                    break;
                }
            }
            return localID;
        }


        bool Clone(string firstName, string lastName)
        {
            lock (avatars)
            {
                string testName = firstName.ToLower() + " " + lastName.ToLower();
                foreach (Avatar av in avatars.Values)
                {
                    if (av.Name.ToLower() == testName)
                    {
                        CopyAppearance(av);
                        return true;
                    }
                }
                return false;
            }
        }
        void CopyAppearance(Avatar av)
        {
            lock (appearances)
            {
                if (appearances.ContainsKey(av.ID))
                {
                    AvatarAppearancePacket appearance = appearances[av.ID];
                    AgentSetAppearancePacket set = new AgentSetAppearancePacket();

                    set.AgentData.AgentID = Client.Network.AgentID;
                    set.AgentData.SessionID = Client.Network.SessionID;
                    set.AgentData.SerialNum = 1;
                    set.AgentData.Size = new LLVector3(0.45f, 0.6f, 1.986565f);
                    set.ObjectData.TextureEntry = appearance.ObjectData.TextureEntry;
                    set.VisualParam = new AgentSetAppearancePacket.VisualParamBlock[appearance.VisualParam.Length];

                    int i = 0;
                    foreach (AvatarAppearancePacket.VisualParamBlock block in appearance.VisualParam)
                    {
                        set.VisualParam[i] = new AgentSetAppearancePacket.VisualParamBlock();
                        set.VisualParam[i].ParamValue = block.ParamValue;
                        i++;
                    }

                    set.WearableData = new AgentSetAppearancePacket.WearableDataBlock[0];

                    Client.Network.SendPacket(set);
                    lastAppearance = set;

                    //SavePacket("Appearance.xml", lastAppearance);
                }
            }
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
                        controls = 0;
                        SendAgentUpdate();
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
        
        string TimeStamp()
        {
            return "[" + (DateTime.Now.Hour % 12) + ":" + DateTime.Now.Minute + "] ";
        }

        bool Follow(string name)
        {
            followName = name;
            foreach (Avatar av in avatars.Values)
            {
                if (av.Name == name)
                {
                    if (Helpers.VecDist(av.Position, Client.Self.Position) > 4)
                    {
                        //GridRegion region = Client.Network.CurrentSim.Region.GridRegionData;
                        Client.Self.AutoPilot((ulong)(av.Position.X + regionX), (ulong)(av.Position.Y + regionY), av.Position.Z);
                        Thread.Sleep(100);
                        controls = 0;
                        SendAgentUpdate();
                    }
                    else
                    {
                        controls = 0;
                        SendAgentUpdate();
                    }
                    return true;
                }
            }
            return false;
        }

        bool LoadScript(string scriptFile)
        {
            if (!File.Exists(scriptFile))
            {
                Console.WriteLine("File not found: "+scriptFile);
                return false;
            }
            string[] script = { };
            string input;
            int error = 0;
            StreamReader read = File.OpenText(scriptFile);
            for (int i = 0; (input = read.ReadLine()) != null; i++)
            {
                char[] splitChar = { ' ' };
                string[] args = input.ToLower().Split(splitChar);
                string[] commandsWithArgs = { "camp", "goto", "label", "pay", "payme", "say", "shout", "sit", "teleport", "touch", "touchid", "wait", "whisper" };
                string[] commandsWithoutArgs = { "fly", "land", "quit", "relog", "run", "sitg", "stand", "walk" };
                if (Array.IndexOf(commandsWithArgs,  args[0]) > -1)
                {
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Missing argument(s) for command \"{0}\" on line {1} of {2}",args[0],i+1,scriptFile);
                        error++;
                    }
                    else
                    {
                        Array.Resize(ref script, i + 1);
                        script[i] = input;
                    }
                }
                else if (Array.IndexOf(commandsWithoutArgs, args[0]) < 0)
                {
                    Console.WriteLine("Unknown command \"{0}\" on line {1} of {2}",args[0],i+1,scriptFile);
                    error++;
                }
            }
            read.Close();
            if (error > 0)
            {
                Console.WriteLine("* Error loading script \"{0}\"", scriptFile);
                return false;
            }
            else
            {
                Console.WriteLine("* Running script \"{0}\"", scriptFile);
                RunScript(script);
                return true;
            }
        }

        void RunScript(string[] script)
        {
            for (int i = 0; i < script.Length; i++)
            {
                char[] splitChar = { ' ' };
                string[] cmd = script[i].Split(splitChar);
                switch (cmd[0])
                {
                    case "wait":
                        {
                            Console.WriteLine("* Sleeping {0} seconds...", cmd[1]);
                            Thread.Sleep(int.Parse(cmd[1]) * 1000);
                            continue;
                        }
                    case "goto":
                        {
                            int findLabel = Array.IndexOf(script, "label " + cmd[1]);
                            if (findLabel > -1) i = findLabel;
                            else Console.WriteLine("* Label \"{0}\" not found on line {1}", cmd[1], i+1);
                            continue;
                        }
                    case "label":
                        {
                            continue;
                        }
                }
                Console.WriteLine("* SCRIPTED COMMAND: "+script[i]);
                ParseCommand(true, "/"+script[i], "", new LLUUID(), new LLUUID());
            }
        }

    }
}

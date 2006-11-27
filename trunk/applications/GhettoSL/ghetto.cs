using System;
using System.Threading;
using System.Collections.Generic;
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
                Console.WriteLine("Usage: GhettoSL <firstName> <lastName> <password> <passPhrase> <masterID>");
                return;
            }
            bool quiet = false;
            if (args.Length > 5 && args[5].ToLower() == "quiet") quiet = true;
            GhettoSL ghetto = new GhettoSL(args[0], args[1], args[2], args[3], new LLUUID(args[4]), quiet);

        }
        //END OF MAIN VOID ####################################################


        //GHETTOSL VOID ######################################################
        public GhettoSL(string first, string last, string pass, string phrase, LLUUID master, bool quiet)
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
            prims = new Dictionary<uint, PrimObject>();
            appearances = new Dictionary<LLUUID, AvatarAppearancePacket>();
            imWindows = new Dictionary<uint, Avatar>();

            //Add callbacks for events
            Client.Network.OnConnected += new NetworkManager.ConnectedCallback(OnConnectedEvent);
            Client.Network.OnSimDisconnected += new NetworkManager.SimDisconnectCallback(OnSimDisconnectEvent);
            //Client.Network.RegisterCallback(PacketType.AgentToNewRegion, new NetworkManager.PacketCallback(OnAgentToNewRegion));
            Client.Network.RegisterCallback(PacketType.MoneyBalanceReply, new NetworkManager.PacketCallback(OnMoneyBalanceReplyEvent));
            Client.Network.RegisterCallback(PacketType.RequestFriendship, new NetworkManager.PacketCallback(OnRequestFriendshipEvent));
            Client.Network.RegisterCallback(PacketType.AvatarAppearance, new NetworkManager.PacketCallback(OnAppearance));
            Client.Network.RegisterCallback(PacketType.TeleportFinish, new NetworkManager.PacketCallback(OnTeleportFinish));
            Client.Network.RegisterCallback(PacketType.ObjectUpdate, new NetworkManager.PacketCallback(OnObjectUpdateEvent));
            Client.Objects.OnNewPrim += new ObjectManager.NewPrimCallback(OnNewPrimEvent);
            Client.Objects.OnPrimMoved += new ObjectManager.PrimMovedCallback(OnPrimMovedEvent);
            Client.Objects.OnObjectKilled += new ObjectManager.KillObjectCallback(OnObjectKilledEvent);
            Client.Objects.OnNewAvatar += new ObjectManager.NewAvatarCallback(OnNewAvatarEvent);
            Client.Objects.OnAvatarMoved += new ObjectManager.AvatarMovedCallback(OnAvatarMovedEvent);
            Client.Self.OnInstantMessage += new InstantMessageCallback(OnInstantMessageEvent);
            Client.Self.OnTeleport += new TeleportCallback(OnTeleportEvent);
            if (!quiet) Client.Self.OnChat += new ChatCallback(OnChatEvent);

            //Attempt to login, and exit if failed
            while (!Login()) Thread.Sleep(5000);

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


        //FRIEND REQUESTS (FIX ME!!!) #########################################
        void OnRequestFriendshipEvent(Packet packet, Simulator simulator)
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


        //MONEY BALANCE UPDATE ################################################
        void OnMoneyBalanceReplyEvent(Packet packet, Simulator simulator)
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
        //END OF MONEY BALANCE ################################################

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

        //AUTO-CAMP OBJECT-FINDING STUFF ######################################
        void OnObjectUpdateEvent(Packet packet, Simulator sim)
        {
            ObjectUpdatePacket p = (ObjectUpdatePacket)packet;
            foreach (ObjectUpdatePacket.ObjectDataBlock obj in p.ObjectData)
            {
                //FIX ME!!!
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
                            response = "FACING <" + targetPos + "> "+Helpers.RotBetween(Client.Self.Position, prim.Position);
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
                case "shout":
                    {
                        Client.Self.Chat(details, 0, MainAvatar.ChatType.Shout);
                        break;
                    }
                case "sim":
                    {
                        response = "Teleporting to " + details;
                        Client.Self.Teleport(details,new LLVector3(128,128,0));
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
                case "tp": //FIX ME!!!
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
                    //Console.WriteLine("* Cloning "+av.Name+".");
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

                    //set.WearableData = new AgentSetAppearancePacket.WearableDataBlock[appearance.
                    set.WearableData = new AgentSetAppearancePacket.WearableDataBlock[0];

                    Client.Network.SendPacket(set);
                    lastAppearance = set;
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
        //void OnAgentToNewRegion(Packet packet, Simulator simulator)
        //{
        //    AgentToNewRegionPacket reply = (AgentToNewRegionPacket)packet;
        //    AgentToNewRegionPacket.RegionDataBlock region = reply.RegionData;
        //    Console.WriteLine("* NEW REGION: " + region.Handle);
        //}
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

    }
}

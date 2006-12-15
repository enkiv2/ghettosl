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
using System.Text;
using libsecondlife;
using libsecondlife.Packets;

namespace ghetto
{
    partial class GhettoSL
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
        int currentBalance;
        int regionX;
        int regionY;
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

            Client.Self.Status.Camera.Far = 96.0f;
            Client.Self.Status.Camera.CameraAtAxis = new LLVector3(0, 0, 0);
            Client.Self.Status.Camera.CameraCenter = new LLVector3(0, 0, 0);
            Client.Self.Status.Camera.CameraLeftAxis = new LLVector3(0, 0, 0);
            Client.Self.Status.Camera.CameraUpAxis = new LLVector3(0, 0, 0);
            Client.Self.Status.Camera.HeadRotation = new LLQuaternion(0, 0, 0, 1);
            Client.Self.Status.Camera.BodyRotation = new LLQuaternion(0, 0, 0, 1);

            //Add callbacks for events
            InitializeCallbacks();
            
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
        //END OF GHETTOSL VOID ################################################


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
            if (File.Exists("default.appearance")) LoadAppearance("default.appearance");
            Console.WriteLine(RPGWeather());
            Console.WriteLine("Location: " + Client.Self.Position);

            //Fix the "bot squat" animation
            Client.Self.Status.SendUpdate();

            return true;
        }
        //END OF LOGIN ########################################################


        void OnObjectSelect(Packet packet, Simulator sim)
        {
            ObjectSelectPacket reply = (ObjectSelectPacket)packet;
            //if (reply.AgentData.AgentID == masterID)
            //{
                Console.WriteLine("* Touched/grabbed object " + reply.ObjectData[0].ObjectLocalID);
            //}
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
                        Client.Self.Status.SendUpdate();
                    }
                    else
                    {
                        Client.Self.Status.SendUpdate();
                    }
                    return true;
                }
            }
            return false;
        }

        bool RideWith(string name)
        {
            string findName = name.ToLower();
            foreach (Avatar av in avatars.Values)
            {

                if (av.Name.ToLower().Substring(0, findName.Length) == findName)
                {
                    if (av.SittingOn > 0)
                    {
                        Console.WriteLine("* Riding with " + av.Name + ".");
                        Client.Self.RequestSit(prims[av.SittingOn].ID, new LLVector3(0, 0, 0));
                        Client.Self.Sit();
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("* " + av.Name + " is not sitting.");
                        return false;
                    }
                }
            }
            return false;
        }

    }
}

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
using System.Threading;
using System.Collections.Generic;
using System.Text;
using libsecondlife;
using libsecondlife.Packets;

namespace ghetto
{
    partial class GhettoSL
    {

        static Dictionary<uint, GhettoSL> connections;

        UserSession Session;
        SecondLife Client = new SecondLife();

        string platform;
        static uint currentSession;
        static bool logout;

        public struct UserSettings
        {
            public string FirstName;
            public string LastName;
            public string Password;
            public string PassPhrase;
            public LLUUID MasterID;
            public bool Quiet;
            public string Script;
            public bool SendUpdates;
            public string CampChairMatchText;
            public string FollowName;
        }

        public struct UserSession
        {
            public UserSettings Settings;
            public int Balance;
            public Dictionary<uint, Avatar> Avatars;
            public Dictionary<LLUUID, AvatarAppearancePacket> Appearances;
            public Dictionary<uint, PrimObject> Prims;
            public Dictionary<LLUUID, Avatar> Friends;
            public Dictionary<uint, Avatar> IMSession;
            public AgentSetAppearancePacket LastAppearance;
            public int MoneySpent;
            public int MoneyReceived;
            public LLUUID MasterIMSession;
            public int RegionX;
            public int RegionY;
            public uint StartTime;
            public UserScript Script;
        }

        public struct UserScript
        {
            public string[] Lines;
            public int CurrentStep;
            public uint ScriptTime;
            public uint SleepingSince;
            public System.Timers.Timer SleepTimer;
            public Dictionary<string, Event> Events;
        }
        

        //BEGIN MAIN VOID #####################################################
        static void Main(string[] args)
        {
            //Make sure command line arguments are valid
            string[] commandLineArguments = args;
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: GhettoSL <firstName> <lastName> <password> [passPhrase] [masterID] [quiet] [scriptFile]");
                return;
            }
            bool quiet = false;
            string passPhrase = "";
            string scriptFile = "";
            LLUUID masterID = new LLUUID();
            if (args.Length > 3) passPhrase = args[3];
            if (args.Length > 4) masterID = (LLUUID)args[4];
            if (args.Length > 5 && (args[5].ToLower() == "quiet" || args[5].ToLower() == "true")) quiet = true;
            if (args.Length > 6) scriptFile = args[6];

            UserSession session = new UserSession();
            session.Settings.FirstName = args[0];
            session.Settings.LastName = args[1];
            session.Settings.Password = args[2];
            session.Settings.PassPhrase = args[3];
            session.Settings.MasterID = masterID;
            session.Settings.Quiet = quiet;
            session.Settings.Script = scriptFile;

            connections = new Dictionary<uint, GhettoSL>();
            currentSession = 1;
            connections.Add(currentSession, new GhettoSL(session));

            //Accept commands
            do
            {
                string read = Console.ReadLine();
                if (read.Length > 0)
                {
                    if (read.Substring(0, 1) == "/")
                    {
                        read = read.Substring(1);
                        string[] cmdScript = { read };
                        connections[currentSession].ParseScriptLine(cmdScript, 0);
                    }
                    else connections[currentSession].Client.Self.Chat(read, 0, MainAvatar.ChatType.Normal);
                }
            }
            while (!logout);

            connections[1].Client.Network.Logout();
            Thread.Sleep(500);
            //Exit application


        }

        //END OF MAIN VOID ####################################################


        //GHETTOSL VOID #######################################################
        public GhettoSL(UserSession session)
        {
            //RotBetween Test
            //LLVector3 a = new LLVector3(1, 0, 0);
            //LLVector3 b = new LLVector3(0, 0, 1);
            //Console.WriteLine("RotBetween: " + Helpers.RotBetween(Helpers.VecNorm(a), Helpers.VecNorm(b)));
            //Console.ReadLine();
            //return;

            platform = System.Convert.ToString(Environment.OSVersion.Platform);
            Console.WriteLine("\r\nRunning on platform " + platform);

            Random random = new Random();
            IntroArt(random.Next(1, 3));

            Session = session;
            Session.IMSession = new Dictionary<uint, Avatar>();
            Session.LastAppearance = new AgentSetAppearancePacket();

            logout = false;
            Session.Settings.SendUpdates = true;

            Session.Appearances = new Dictionary<LLUUID, AvatarAppearancePacket>();
            Session.Avatars = new Dictionary<uint, Avatar>();
            Session.Friends = new Dictionary<LLUUID, Avatar>();
            Session.Prims = new Dictionary<uint, PrimObject>();
            
            Session.Script.Events = new Dictionary<string, Event>();

            Stalked = new Dictionary<LLUUID, Location>();

            // Unix timestamp of when the client was launched
            Session.StartTime = Helpers.GetUnixTime();
            // L$ paid out to objects/avatars since login
            Session.MoneySpent = 0;
            // L$ received since login (before subtracting .MoneySpent)
            Session.MoneyReceived = 0;

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

            Client.Self.OnChat += new MainAvatar.ChatCallback(OnChatEvent);

            //Attempt to login, and exit if failed
            while (!Login()) Thread.Sleep(5000);

            //Run script
            if (Session.Settings.Script != "") LoadScript(Session.Settings.Script);

        }
        //END OF GHETTOSL VOID ################################################


        //LOGIN SEQUENCE ######################################################
        bool Login()
        {
            Console.Title = "GhettoSL - Logging in...";
            Console.ForegroundColor = System.ConsoleColor.White;
            Console.WriteLine(TimeStamp() + "Logging in as " + Session.Settings.FirstName + " " + Session.Settings.LastName + "...");
            Console.ForegroundColor = System.ConsoleColor.Gray;

            //Attempt to log in
            if (!Client.Network.Login(Session.Settings.FirstName, Session.Settings.LastName, Session.Settings.Password, "GhettoSL", "ghetto@obsoleet.com"))
            {
                Console.WriteLine("Login failed.");
                return false;
            }

            Console.Title = Client.Self.FirstName + " " + Client.Self.LastName + " - GhettoSL";

            //Succeeded - Wait for simulator name or disconnection
            Simulator sim = Client.Network.CurrentSim;
            while (Client.Network.Connected && (!sim.Connected || sim.Region.Name == "" || Client.Grid.SunDirection.X == 0))
            {
                Thread.Sleep(100);
            }

            //Halt if disconnected
            if (!Client.Network.Connected) return false;

            //We are in!
            Console.ForegroundColor = System.ConsoleColor.White;
            Console.WriteLine(TimeStamp() + RPGWeather());
            Console.WriteLine(TimeStamp() + "Location: " + Client.Self.Position);
            Console.ForegroundColor = System.ConsoleColor.Gray;

            //Fix the "bot squat" animation
            Client.Self.Status.SendUpdate();

            return true;
        }
        //END OF LOGIN ########################################################

    }
}

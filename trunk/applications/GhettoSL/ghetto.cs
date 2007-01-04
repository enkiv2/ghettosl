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

        //Main void
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
            do ReadCommand();
            while (!logout);

            connections[1].Client.Network.Logout();
            Thread.Sleep(500);
            //Exit application


        }

        //Reads one line and parses as a command or as chat
        static void ReadCommand()
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

        //GhettoSL Constructor
        public GhettoSL(UserSession session)
        {
            platform = System.Convert.ToString(Environment.OSVersion.Platform);
            Console.WriteLine("\r\nRunning on platform " + platform);

            Random random = new Random();
            IntroArt(random.Next(1, 3));

            Client.Debug = false;

            Stalked = new Dictionary<LLUUID, Location>();

            //Initialize session info for this connection
            InitializeUserSession(session);

            //Add callbacks for events
            InitializeCallbacks();

            //Set camera params for agent updates
            InitializeCamera();

            //Attempt to login, and exit if failed
            while (!Login()) Thread.Sleep(5000);

            //Run script
            if (Session.Settings.Script != "") LoadScript(Session.Settings.Script);

        } //End of GhettoSL constructor

    }
}

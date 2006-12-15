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
  
    }
}

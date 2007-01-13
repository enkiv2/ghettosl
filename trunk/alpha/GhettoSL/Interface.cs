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

using libsecondlife;
using libsecondlife.Packets;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ghetto
{

    class Interface
    {
        public static Dictionary<uint, GhettoSL.UserSession> Sessions;
        public static Dictionary<string, Scripting.UserScript> Scripts;
        public static HelpSystem Help;

        public static bool Exit;
        public static uint CurrentSession;

        //Main void
        static void Main(string[] args)
        {

            string platform = System.Convert.ToString(Environment.OSVersion.Platform);
            Console.WriteLine(Environment.NewLine + "Running on platform " + platform);
            Random random = new Random();
            Display.IntroArt(random.Next(1, 3));

            KeyValuePair<bool,GhettoSL.UserSessionSettings> loginParams = ParseCommandArguments(args);

            if (loginParams.Key == false)
            {
                Console.WriteLine("Usage: GhettoSL <firstName> <lastName> <password> [options]");
                Console.WriteLine("-m  -master <uuid> ..... set uuid of master to accept teleports and commands from");
                Console.WriteLine("-n  -noupdates ......... does not send agent updates, for minimum bandwidth usage");
                Console.WriteLine("-p  -pass <word> ....... different from account password, used for teleport requests");
                Console.WriteLine("-q  -quiet ............. run in \"quiet mode\" (public chat is not displayed)");
                Console.WriteLine("-s  -script <file> ..... load the specified script (for script help, /help scripts)");
                return;
            }

            GhettoSL.UserSession session = new GhettoSL.UserSession(1);
            session.Settings = loginParams.Value;

            Sessions = new Dictionary<uint, GhettoSL.UserSession>();
            Scripts = new Dictionary<string, Scripting.UserScript>();
            Help = new HelpSystem();
            
            CurrentSession = 1;
            Sessions.Add(1, session);

            //Make initial connection
            Sessions[CurrentSession].Login();

            //Accept commands
            Exit = false;
            do ReadCommand();
            while (!Exit);

            Thread.Sleep(500);
            //Exit application

        } //End of Main void



        /// <summary>
        /// Parse command-line arguments
        /// </summary>
        /// <param name="args">success</param>
        /// <returns>KeyValuePair(bool success, GhettoSL.UserSessionSettings settings)</returns>
        public static KeyValuePair<bool, GhettoSL.UserSessionSettings> ParseCommandArguments(string[] args)
        {
            KeyValuePair<bool, GhettoSL.UserSessionSettings> ret = new KeyValuePair<bool, GhettoSL.UserSessionSettings>(true, new GhettoSL.UserSessionSettings());
            KeyValuePair<bool, GhettoSL.UserSessionSettings> error = new KeyValuePair<bool, GhettoSL.UserSessionSettings>(false, new GhettoSL.UserSessionSettings());

            if (args.Length < 3) return error;

            ret.Value.FirstName = args[0];
            ret.Value.LastName = args[1];
            ret.Value.Password = args[2];

            for (int i = 3; i < args.Length; i++)
            {

                bool lastArg = false;
                if (i + 1 == args.Length) lastArg = true;

                string arg = args[i].ToLower();

                if (arg == "-q" || arg == "-quiet")
                    ret.Value.DisplayChat = false;
                else if (!lastArg && (arg == "-m" || arg == "-master" || arg == "-masterid"))
                    ret.Value.MasterID = new LLUUID(args[i + 1]);
                else if (arg == "-n" || arg == "-noupdates")
                    ret.Value.SendUpdates = false;
                else if (!lastArg && (arg == "-p" || arg == "-pass" || arg == "passphrase"))
                    //FIXME - detect and support multi-word passphrases in quotes
                    ret.Value.PassPhrase = args[i + 1];
                else if (!lastArg && (arg == "-s" || arg == "-script"))
                    ret.Value.Script = args[i + 1];

            }

            return ret;
        }

        /// <summary>
        /// Read commands from the user
        /// </summary>
        /// <returns>Returns false when an invalid command is received</returns>
        static bool ReadCommand()
        {
            string read = Console.ReadLine();

            if (read.Length < 1)
                return true;
            else if (read.Length > 1 && read.Substring(0, 2) == "//")
                Scripting.ParseCommand(CurrentSession, read.Substring(2), true, false);
            else if (read.Substring(0, 1) == "/")
                Scripting.ParseCommand(CurrentSession, read.Substring(1), false, false);
            else
                Sessions[CurrentSession].Client.Self.Chat(read, 0, MainAvatar.ChatType.Normal);

            return true;
        }


    }
}
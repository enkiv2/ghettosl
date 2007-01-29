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
using libsecondlife;
using libsecondlife.Packets;
using libsecondlife.AssetSystem;
using libsecondlife.Utilities;

namespace ghetto
{
    public class GhettoSL
    {

        /// <summary>
        /// Session, including SecondLife client and all associated dictionaries
        /// </summary>
        public class UserSession
        {

            public uint SessionNumber;
            public AvatarTracker Avatars;
            public CallbackManager Callbacks;
            public int Balance;
            public SecondLife Client;
            public Dictionary<LLUUID, Avatar> Friends;
            public Dictionary<LLUUID, IMSession> IMSessions;
            public Dictionary<uint, PrimObject> Prims;
            public Dictionary<string, ScriptSystem.ScriptEvent> ScriptEvents;
            public LLUUID LastDialogID;
            public int LastDialogChannel;
            public int MoneySpent;
            public int MoneyReceived;
            public LLUUID MasterIMSession;
            public int RegionX;
            public int RegionY;
            public UserSessionSettings Settings;
            public uint StartTime;

             public bool Login()
            {
                Display.InfoResponse(SessionNumber, "Logging in as " + Settings.FirstName + " " + Settings.LastName + "...");
                //Dictionary<string, object> loginParams = DefaultLoginValues(Settings.FirstName, Settings.LastName, Settings.Password, "GhettoSL", "root66@gmail.com");
                if (Settings.URI == "") return Client.Network.Login(Settings.FirstName, Settings.LastName, Settings.Password, "GhettoSL", "last", "root66@gmail.com", false);
                else
                {
                    string start = Settings.URI;
                    Console.WriteLine(start); //debug
                    return Client.Network.Login(Settings.FirstName, Settings.LastName, Settings.Password, "GhettoSL", start, "root66@gmail.com", false);
                }
            }

            public void UpdateAppearance()
            {
                Display.InfoResponse(SessionNumber, "Loading appearance from asset server...");
                AppearanceManager aManager;
                aManager = new AppearanceManager(Client);
                aManager.BeginAgentSendAppearance();
            }

            public string Name
            {
                get
                {
                    if (Client.Network.Connected) return Client.Self.FirstName + " " + Client.Self.LastName;
                    else return Settings.FirstName + " " + Settings.LastName;
                }
            }

            /// <summary>
            /// UserSession constructor
            /// </summary>
            public UserSession(uint newSessionNumber)
            {
                SessionNumber = newSessionNumber;

                Client = new SecondLife();
                Client.Settings.DEBUG = false;

                Client.Self.Status.Camera.Far = 96.0f;
                Client.Self.Status.Camera.CameraAtAxis = LLVector3.Zero;
                Client.Self.Status.Camera.CameraCenter = LLVector3.Zero;
                Client.Self.Status.Camera.CameraLeftAxis = LLVector3.Zero;
                Client.Self.Status.Camera.CameraUpAxis = LLVector3.Zero;
                Client.Self.Status.Camera.HeadRotation = LLQuaternion.Identity;
                Client.Self.Status.Camera.BodyRotation = LLQuaternion.Identity;

                Callbacks = new CallbackManager(this);
                Avatars = new AvatarTracker(Client);
                Balance = -1;
                Friends = new Dictionary<LLUUID, Avatar>();
                IMSessions = new Dictionary<LLUUID, IMSession>();
                Prims = new Dictionary<uint, PrimObject>();
                LastDialogChannel = -1;
                LastDialogID = LLUUID.Zero;
                MoneySpent = 0;
                MoneyReceived = 0;
                MasterIMSession = LLUUID.Zero;
                RegionX = 0;
                RegionY = 0;
                ScriptEvents = new Dictionary<string, ScriptSystem.ScriptEvent>();
                Settings = new UserSessionSettings();
                StartTime = Helpers.GetUnixTime();
            }
        }

        /// <summary>
        /// Used for tracking IM session details
        /// </summary>
        public class IMSession
        {
            LLUUID imSession;
            string name;

            public IMSession(LLUUID imSessionID, string fromName)
            {
                imSession = imSessionID; 
                name = fromName;
            }

            public LLUUID IMSessionID
            {
                get { return imSession; }
            }

            public string Name
            {
                get { return name; }
            }
        }

        /// <summary>
        /// Settings defined at runtime
        /// </summary>
        public class UserSessionSettings
        {
            public string FirstName;
            public string LastName;
            public string Password;
            public string PassPhrase;
            public LLUUID MasterID;
            public bool DisplayChat;
            public bool SendUpdates;
            public string CampChairMatchText;
            public string FollowName;
            public string Script;
            public string URI;
            public UserSessionSettings()
            {
                FirstName = "";
                LastName = "";
                Password = "";
                PassPhrase = "";
                MasterID = LLUUID.Zero;
                DisplayChat = true;
                SendUpdates = true;
                URI = "last";
                CampChairMatchText = "";
                FollowName = "";
            }
        }


        public UserSession Session;

        public GhettoSL(UserSession session)
        {
            Session = session;
        }


    }
}

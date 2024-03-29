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
using System.Text.RegularExpressions;
using OpenMetaverse;
using OpenMetaverse.Packets;

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
            //public AvatarTracker Avatars;
            public CallbackManager Callbacks;
            public int Balance;
            public GridClient Client;
            public Dictionary<UUID, Avatar> Friends;
            public Dictionary<UUID, OpenMetaverse.Group> Groups;
            public Dictionary<UUID, IMSession> IMSessions;
            public Dictionary<uint, Avatar> Avatars;
            public Dictionary<uint, Primitive> Prims;
            public Dictionary<string, ScriptSystem.UserTimer> Timers;
            public UUID LastDialogID;
            public int Debug;
            public int LastDialogChannel;
            public int MoneySpent;
            public int MoneyReceived;
            public UUID MasterIMSession;
            public int RegionX;
            public int RegionY;
            public UserSessionSettings Settings;
            public uint StartTime;
            public string FollowName;
            public System.Timers.Timer FollowTimer;

            public void Login()
            {
                LoginParams loginParams = Client.Network.DefaultLoginParams(
                    Settings.FirstName,
                    Settings.LastName,
                    Settings.Password,
                    "GhettoSL",
                    "root66@gmail.com"
                );
                if (Settings.StartLocation != "")
                {
                    loginParams.Start = Settings.StartLocation;
                    Display.InfoResponse(SessionNumber, "Logging in as " + Settings.FirstName + " " + Settings.LastName + "... (Location: " + Settings.StartLocation + ")");
                    //return Client.Network.Login(Settings.FirstName, Settings.LastName, Settings.Password, "GhettoSL", Settings.URI, "root66@gmail.com");
                    Client.Network.BeginLogin(loginParams);
                }
                else
                {
                    loginParams.Start = "last";
                    Display.InfoResponse(SessionNumber, "Logging in as " + Settings.FirstName + " " + Settings.LastName + "...");
                    //return Client.Network.Login(Settings.FirstName, Settings.LastName, Settings.Password, "GhettoSL", "root66@gmail.com");
                    Client.Network.BeginLogin(loginParams);
                }
            }

            public string Name
            {
                get { return Settings.FirstName + " " + Settings.LastName; }
            }

            public uint FindObjectByText(string textValue)
            {
                uint localID = 0;

                lock (Prims)
                {
                    foreach (Primitive prim in Prims.Values)
                    {
                        int len = textValue.Length;
                        string match = prim.Text.Replace("\n", ""); //Strip newlines
                        if (match.Length < len) continue; //Text is too short to be a match
                        else if (Regex.IsMatch(match.Substring(0, len).ToLower(), textValue, RegexOptions.IgnoreCase))
                        {
                            localID = prim.LocalID;
                            break;
                        }
                    }
                }
                return localID;
            }

            public uint FindAgentByName(string name)
            {
                uint localID = 0;

                lock (Avatars)
                {
                    foreach (Avatar av in Avatars.Values)
                    {
                        int len = name.Length;
                        string match;
                        if (av.Name != null) match = av.Name;
                        else continue;

                        //Console.WriteLine("test: " + av.Name + " vs. " + name); //DEBUG

                        if (match.Length < len) continue; //Name is too short to be a match
                        //FIXME - should we really use regex here? how about just partial names instead?
                        else if (Regex.IsMatch(match.Substring(0, len).ToLower(), name, RegexOptions.IgnoreCase))
                        {
                            localID = av.LocalID;
                            break;
                        }
                    }
                }
                return localID;
            }

            public void Follow(string avatarName)
            {
                if (!Client.Network.Connected) return;
                uint avatar = FindAgentByName(avatarName);
                if (avatar < 1) Display.InfoResponse(SessionNumber, "No avatar found matching \"" + avatarName + "\"");
                else
                {
                    FollowName = Avatars[avatar].Name;
                    Display.InfoResponse(SessionNumber, "Following " + FollowName + "...");
                    FollowTimer.Start();
                }
            }

            void FollowTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
            {
                GhettoSL.UserSession Session = Interface.Sessions[SessionNumber];
                Client.Self.Movement.SendUpdate();
                foreach (Avatar av in Avatars.Values)
                {
                    if (av.Name == FollowName)
                    {
                        Vector3 target;
                        if (av.ParentID > 0)
                        {
                            if (Prims.ContainsKey(av.ParentID))
                            {
                                target = Prims[av.ParentID].Position + av.Position;
                            }
                            else
                            {
                                //FIXME - show an error about missing object info?
                                return;
                            }
                        }
                        else
                        {
                            target = av.Position;
                        }

                        Session.Client.Self.Movement.TurnToward(target);

                        //Console.WriteLine(av.Position); //DEBUG
                        if (Vector3.Distance(Client.Self.SimPosition, av.Position) > 3)
                        {
                            Client.Self.AutoPilotLocal((int)av.Position.X, (int)av.Position.Y, av.Position.Z);
                        }
                        
                        return;
                    }
                }
                Display.InfoResponse(SessionNumber, "Lost track of " + FollowName + " - following disabled");
                FollowTimer.Stop();
            }

            public bool RideWith(string name)
            {
                if (!Client.Network.Connected) return false;
                uint localID = FindAgentByName(name);
                if (localID < 1) Display.InfoResponse(SessionNumber, "Avatar not found matching \"" + name + "\"");
                else if (Avatars[localID].ParentID < 1) Display.InfoResponse(SessionNumber, Avatars[localID].Name + " is not sitting.");
                else if (!Prims.ContainsKey(Avatars[localID].ParentID)) Display.Error(SessionNumber, "Object info missing for local ID " + localID);
                else
                {
                    Client.Self.RequestSit(Prims[Avatars[localID].ParentID].ID, Vector3.Zero);
                    Client.Self.Sit();
                    return true;
                }
                return false;
            }

            public void ScriptDialogReply(int channel, UUID objectid, string message)
            {
                if (!Client.Network.Connected) return;
                ScriptDialogReplyPacket reply = new ScriptDialogReplyPacket();
                reply.AgentData.AgentID = Client.Self.AgentID;
                reply.AgentData.SessionID = Client.Self.SessionID;
                reply.Data.ButtonIndex = 0;
                reply.Data.ChatChannel = channel;
                reply.Data.ObjectID = objectid;
                reply.Data.ButtonLabel = Utils.StringToBytes(message);
                Client.Network.SendPacket(reply);
            }

            public void UpdateAppearance()
            {
                if (!Client.Network.Connected) return;
                Display.InfoResponse(SessionNumber, "Loading appearance from asset server...");
                Client.Appearance.SetPreviousAppearance(false);
                //Console.WriteLine("FIXME: SetPreviousAppearance");
                Avatar a = new Avatar();
            }

            /// <summary>
            /// UserSession constructor
            /// </summary>
            public UserSession(uint newSessionNumber)
            {
                SessionNumber = newSessionNumber;

                Client = new GridClient();
                OpenMetaverse.Settings.LOG_LEVEL = Helpers.LogLevel.Error;
                Client.Settings.LOGIN_TIMEOUT = 480 * 1000;
                Client.Settings.SEND_AGENT_UPDATES = true;

                Client.Self.Movement.Camera.Far = 96.0f;
                Client.Self.Movement.Camera.AtAxis = Vector3.Zero;
                Client.Self.Movement.Camera.Position = Vector3.Zero;
                Client.Self.Movement.Camera.LeftAxis = Vector3.Zero;
                Client.Self.Movement.Camera.UpAxis = Vector3.Zero;
                Client.Self.Movement.HeadRotation = Quaternion.Identity;
                Client.Self.Movement.BodyRotation = Quaternion.Identity;

                Callbacks = new CallbackManager(this);
                Avatars = new Dictionary<uint, Avatar>();
                Balance = -1;
                Friends = new Dictionary<UUID, Avatar>();
                Groups = new Dictionary<UUID, OpenMetaverse.Group>();
                IMSessions = new Dictionary<UUID, IMSession>();
                Prims = new Dictionary<uint, Primitive>();
                Timers = new Dictionary<string, ScriptSystem.UserTimer>();
                Debug = 0;
                LastDialogChannel = -1;
                LastDialogID = UUID.Zero;
                MoneySpent = 0;
                MoneyReceived = 0;
                MasterIMSession = UUID.Zero;
                RegionX = 0;
                RegionY = 0;
                Settings = new UserSessionSettings();
                StartTime = Utils.GetUnixTime();
                FollowName = "";
                FollowTimer = new System.Timers.Timer(500);
                FollowTimer.Enabled = false;
                FollowTimer.AutoReset = true;
                FollowTimer.Elapsed += new System.Timers.ElapsedEventHandler(FollowTimer_Elapsed);
                Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
            }

            void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
            {
                Client.Network.Logout();
            }
       }

        /// <summary>
        /// Used for tracking IM session details
        /// </summary>
        public class IMSession
        {
            UUID imSession;
            string name;

            public IMSession(UUID imSessionID, string fromName)
            {
                imSession = imSessionID; 
                name = fromName;
            }

            public UUID IMSessionID
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
            public UUID MasterID;
            public bool DisplayChat;
            public bool SendUpdates;
            public string CampChairMatchText;
            public string FollowName;
            public string StartLocation;
            public UserSessionSettings()
            {
                FirstName = "";
                LastName = "";
                Password = "";
                PassPhrase = "";
                MasterID = UUID.Zero;
                DisplayChat = true;
                SendUpdates = true;
                StartLocation = "last";
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

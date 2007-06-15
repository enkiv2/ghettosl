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
using libsecondlife;
using libsecondlife.Packets;
using libsecondlife.AssetSystem;
//using libsecondlife.Utilities;

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
            public SecondLife Client;
            public Dictionary<LLUUID, Avatar> Friends;
            public Dictionary<LLUUID, libsecondlife.Group> Groups;
            public Dictionary<LLUUID, IMSession> IMSessions;
            public Dictionary<uint, Avatar> Avatars;
            public Dictionary<uint, Primitive> Prims;
            public Dictionary<string, ScriptSystem.UserTimer> Timers;
            public Dictionary<LLUUID, libsecondlife.InventorySystem.InventoryItem> Inventory;
            public LLUUID LastDialogID;
            public int LastDialogChannel;
            public int MoneySpent;
            public int MoneyReceived;
            public LLUUID MasterIMSession;
            public int RegionX;
            public int RegionY;
            public UserSessionSettings Settings;
            public uint StartTime;
            public string FollowName;
            public System.Timers.Timer FollowTimer;

            public bool Login()
            {
                NetworkManager.LoginParams loginParams = Client.Network.DefaultLoginParams(
                    Settings.FirstName,
                    Settings.LastName,
                    Settings.Password,
                    "GhettoSL",
                    "root66@gmail.com"
                );
                if (Settings.URI != "")
                {
                    loginParams.URI = Settings.URI;
                    Display.InfoResponse(SessionNumber, "Logging in as " + Settings.FirstName + " " + Settings.LastName + "... (Location: " + Settings.URI + ")");
                    return Client.Network.Login(Settings.FirstName, Settings.LastName, Settings.Password, "GhettoSL", Settings.URI, "root66@gmail.com");
                }
                else
                {
                    Display.InfoResponse(SessionNumber, "Logging in as " + Settings.FirstName + " " + Settings.LastName + "...");
                    return Client.Network.Login(Settings.FirstName, Settings.LastName, Settings.Password, "GhettoSL", "root66@gmail.com");
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
                Client.Self.Status.SendUpdate();
                foreach (Avatar av in Avatars.Values)
                {
                    if (av.Name == FollowName)
                    {
                        LLVector3 target;
                        if (av.SittingOn > 0)
                        {
                            if (Prims.ContainsKey(av.SittingOn))
                            {
                                target = Prims[av.SittingOn].Position + av.Position;
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

                        TurnToward(target);

                        //Console.WriteLine(av.Position); //DEBUG
                        if (Helpers.VecDist(Client.Self.Position, av.Position) > 3)
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
                else if (Avatars[localID].SittingOn < 1) Display.InfoResponse(SessionNumber, Avatars[localID].Name + " is not sitting.");
                else if (!Prims.ContainsKey(Avatars[localID].SittingOn)) Display.Error(SessionNumber, "Object info missing for local ID " + localID);
                else
                {
                    Client.Self.RequestSit(Prims[Avatars[localID].SittingOn].ID, LLVector3.Zero);
                    Client.Self.Sit();
                    return true;
                }
                return false;
            }

            public void ScriptDialogReply(int channel, LLUUID objectid, string message)
            {
                if (!Client.Network.Connected) return;
                ScriptDialogReplyPacket reply = new ScriptDialogReplyPacket();
                reply.AgentData.AgentID = Client.Network.AgentID;
                reply.AgentData.SessionID = Client.Network.SessionID;
                reply.Data.ButtonIndex = 0;
                reply.Data.ChatChannel = channel;
                reply.Data.ObjectID = objectid;
                reply.Data.ButtonLabel = Helpers.StringToField(message);
                Client.Network.SendPacket(reply);
            }

            public void TurnToward(LLVector3 target)
            {
                if (!Client.Network.Connected) return;
                LLVector3 myPos = Client.Self.Position;
                uint sittingOn = Client.Self.SittingOn;
                if (sittingOn > 0)
                {
                    if (Prims.ContainsKey(sittingOn)) myPos += Prims[sittingOn].Position;
                    else
                    {
                        Display.Error(SessionNumber, "Missing object info for current seat");
                        return;
                    }
                }
                //Console.WriteLine("Between " + myPos + " and " + target + " == " + Helpers.RotBetween(mypos, target)); //DEBUG
                LLQuaternion newRot = Helpers.RotBetween(new LLVector3(1, 0, 0), Helpers.VecNorm(target - myPos));
                Client.Self.Status.Camera.BodyRotation = newRot;

                //experimental aimbot shizzle
                float x = 1 - 2 * newRot.Z * newRot.Z - 2 * newRot.Y * newRot.Y;
                float y = -2 * newRot.Z * newRot.W + 2 * newRot.Y * newRot.X;
                float z = 2 * newRot.Y * newRot.W + 2 * newRot.Z * newRot.X;
                LLVector3 atAxis = new LLVector3(x, y, z);
                x = 2 * newRot.X * newRot.Y + 2 * newRot.W * newRot.Z;
                y = 1 - 2 * newRot.Z * newRot.Z - 2 * newRot.X * newRot.X;
                z = 2 * newRot.Z * newRot.Y - 2 * newRot.X * newRot.W;
                LLVector3 leftAxis = new LLVector3(x, y, z);
                x = 2 * newRot.X * newRot.Z - 2 * newRot.W * newRot.Y;
                y = 2 * newRot.Y * newRot.Z + 2 * newRot.W * newRot.X;
                z = 1 - 2 * newRot.Y * newRot.Y - 2 * newRot.X * newRot.X;
                LLVector3 upAxis = new LLVector3(x, y, z);


                    //float x = (1 - 2 * (newRot.Y * newRot.Y)) - (2 * (newRot.Z * newRot.Z));
                    //float y = (2 * newRot.X * newRot.Y) - (2 * newRot.W * newRot.Z);
                    //float z = (2 * newRot.X * newRot.Z) + (2 * newRot.W * newRot.Y);
                    //LLVector3 atAxis = new LLVector3(x, y, z);
                    //x = (2 * newRot.X * newRot.Y) + (2 * newRot.W * newRot.Z);
                    //y = (1 - 2 * (newRot.X * newRot.X)) - (2 * (newRot.Z  * newRot.Z));
                    //z = (2 * newRot.Y * newRot.Z) - (2 * newRot.W * newRot.X);
                    //LLVector3 leftAxis = new LLVector3(x, y, z);
                    //x = (2 * newRot.X * newRot.Z) - (2 * newRot.W * newRot.Y);
                    //y = (2 * newRot.Y * newRot.Z) + (2 * newRot.W * newRot.X);
                    //z = (1 - 2 * (newRot.Y * newRot.Y)) - (2 * (newRot.Y * newRot.Y));
                    //LLVector3 upAxis = new LLVector3(x, y, z);

                Client.Self.Status.Camera.CameraCenter = Client.Self.Position;
                Client.Self.Status.Camera.CameraAtAxis = atAxis;
                Client.Self.Status.Camera.CameraLeftAxis = leftAxis;
                Client.Self.Status.Camera.CameraUpAxis = upAxis;
                
                Client.Self.TurnToward(target);
            }

            public void UpdateAppearance()
            {
                if (!Client.Network.Connected) return;
                Display.InfoResponse(SessionNumber, "Loading appearance from asset server...");
                AppearanceManager aManager;
                aManager = new AppearanceManager(Client);
                aManager.BeginAgentSendAppearance();
            }

            /// <summary>
            /// UserSession constructor
            /// </summary>
            public UserSession(uint newSessionNumber)
            {
                SessionNumber = newSessionNumber;

                Client = new SecondLife();
                Client.Settings.DEBUG = false;
                Client.Settings.LOGIN_TIMEOUT = 480 * 1000;

                Client.Self.Status.Camera.Far = 96.0f;
                Client.Self.Status.Camera.CameraAtAxis = LLVector3.Zero;
                Client.Self.Status.Camera.CameraCenter = LLVector3.Zero;
                Client.Self.Status.Camera.CameraLeftAxis = LLVector3.Zero;
                Client.Self.Status.Camera.CameraUpAxis = LLVector3.Zero;
                Client.Self.Status.Camera.HeadRotation = LLQuaternion.Identity;
                Client.Self.Status.Camera.BodyRotation = LLQuaternion.Identity;

                Callbacks = new CallbackManager(this);
                Avatars = new Dictionary<uint, Avatar>();
                Balance = -1;
                Friends = new Dictionary<LLUUID, Avatar>();
                Groups = new Dictionary<LLUUID, libsecondlife.Group>();
                IMSessions = new Dictionary<LLUUID, IMSession>();
                Inventory = new Dictionary<LLUUID, libsecondlife.InventorySystem.InventoryItem>();
                Prims = new Dictionary<uint, Primitive>();
                Timers = new Dictionary<string, ScriptSystem.UserTimer>();
                LastDialogChannel = -1;
                LastDialogID = LLUUID.Zero;
                MoneySpent = 0;
                MoneyReceived = 0;
                MasterIMSession = LLUUID.Zero;
                RegionX = 0;
                RegionY = 0;
                Settings = new UserSessionSettings();
                StartTime = Helpers.GetUnixTime();
                FollowName = "";
                FollowTimer = new System.Timers.Timer(500);
                FollowTimer.Enabled = false;
                FollowTimer.AutoReset = true;
                FollowTimer.Elapsed += new System.Timers.ElapsedEventHandler(FollowTimer_Elapsed);
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

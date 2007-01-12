using System;
using System.Collections.Generic;
using libsecondlife;
using libsecondlife.Packets;
using libsecondlife.AssetSystem;
using libsecondlife.Utilities;

namespace ghetto
{
    class GhettoSL
    {

        /// <summary>
        /// Session, including SecondLife client and all associated dictionaries
        /// </summary>
        public class UserSession
        {
            public uint SessionNumber;
            public AvatarTracker Avatars;
            public EventManager Events;
            public int Balance;
            public SecondLife Client;
            public Dictionary<LLUUID, Avatar> Friends;
            public Dictionary<uint, Avatar> IMSession;
            public Dictionary<uint, PrimObject> Prims;
            public int MoneySpent;
            public int MoneyReceived;
            public LLUUID MasterIMSession;
            public int RegionX;
            public int RegionY;
            public UserSessionSettings Settings;
            public uint StartTime;

            public bool Login()
            {
                Display.InfoResponse(0, "Logging in as " + Settings.FirstName + " " + Settings.LastName + "...");
                bool success = Client.Network.Login(Settings.FirstName, Settings.LastName, Settings.Password, "GhettoSL", "ghetto@obsoleet.com");
                return success;
            }

            public void UpdateAppearance()
            {
                Display.InfoResponse(0, "Loading appearance from asset server...");
                AppearanceManager aManager;
                aManager = new AppearanceManager(Client);
                aManager.SendAgentSetAppearance();
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
                Client.Debug = false;

                Client.Self.Status.Camera.Far = 96.0f;
                Client.Self.Status.Camera.CameraAtAxis = LLVector3.Zero;
                Client.Self.Status.Camera.CameraCenter = LLVector3.Zero;
                Client.Self.Status.Camera.CameraLeftAxis = LLVector3.Zero;
                Client.Self.Status.Camera.CameraUpAxis = LLVector3.Zero;
                Client.Self.Status.Camera.HeadRotation = LLQuaternion.Identity;
                Client.Self.Status.Camera.BodyRotation = LLQuaternion.Identity;

                Events = new EventManager(this);
                Avatars = new AvatarTracker(Client);
                Balance = 0;
                Friends = new Dictionary<LLUUID, Avatar>();
                IMSession = new Dictionary<uint, Avatar>();
                Prims = new Dictionary<uint, PrimObject>();
                MoneySpent = 0;
                MoneyReceived = 0;
                MasterIMSession = LLUUID.Zero;
                RegionX = 0;
                RegionY = 0;
                Settings = new UserSessionSettings();
                StartTime = Helpers.GetUnixTime();
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
            public UserSessionSettings()
            {
                FirstName = "";
                LastName = "";
                Password = "";
                PassPhrase = "";
                MasterID = LLUUID.Zero;
                DisplayChat = true;
                SendUpdates = true;
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

/*
 * Mapstalking by Jesse Malthus
 * 
 */

using System;
using System.Collections.Generic;
using System.Text;
using libsecondlife;
using libsecondlife.Packets;
namespace ghetto
{
    public struct Location
    {
        public double GlobalX;
        public double GlobalY;
    }

    public partial class GhettoSL
    {
        public delegate void MapStalkDelegate(LLUUID stalked, Location location);
        public event MapStalkDelegate OnMapStalk;
        public Dictionary<LLUUID, Location> Stalked;
        void FindAgentCallback(Packet p, Simulator sim)
        {
            FindAgentPacket fap = (FindAgentPacket)p;
            LLUUID person = fap.AgentBlock.Prey;
            Location l = new Location();
            foreach(FindAgentPacket.LocationBlockBlock lb in fap.LocationBlock)
            {
                l.GlobalX = lb.GlobalX;
                l.GlobalY = lb.GlobalY;
            }
            lock (Stalked)
            {
                if (Stalked.ContainsKey(person))
                {
                    Stalked[person] = l;
                }
                else
                {
                    Stalked.Add(person, l);
                }
            }
            if (OnMapStalk != null)
            {
                OnMapStalk(person, l);
            }
        }

        void Stalk(LLUUID person)
        {
            FindAgentPacket fap = new FindAgentPacket();
            fap.AgentBlock = new FindAgentPacket.AgentBlockBlock();
            fap.AgentBlock.Hunter = Client.Network.AgentID;
            fap.AgentBlock.Prey = person;
            fap.AgentBlock.SpaceIP = new uint();
            FindAgentPacket.LocationBlockBlock lb = new FindAgentPacket.LocationBlockBlock();
            lb.GlobalX = 0;
            lb.GlobalY = 0;
            fap.LocationBlock = new FindAgentPacket.LocationBlockBlock[]{ lb };
            Client.Network.SendPacket(fap);
        }

        void TeleportToLocation(Location l)
        {
            uint RegionX = (uint)(l.GlobalX / 256);
            uint RegionY = (uint)(l.GlobalY / 256);
            double LocalX = l.GlobalX - RegionX;
            double LocalY = l.GlobalY - RegionY;
            ulong RegionHandle = Helpers.UIntsToLong((uint)regionX, (uint)regionY);
            Client.Self.Teleport(RegionHandle, new LLVector3(128.0f, 128.0f, 128.0f));
        }
    }
}

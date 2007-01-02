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
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace ghetto
{
    partial class GhettoSL
    {

        void SaveAppearance(string fileName, AgentSetAppearancePacket appearance)
        {
            XmlSerializer s = new XmlSerializer(typeof(AgentSetAppearancePacket));
            TextWriter w = new StreamWriter(@fileName);
            s.Serialize(w, appearance);
            w.Close();
            Console.WriteLine("* Saved " + fileName);
        }

        void LoadAppearance(string fileName)
        {
            XmlSerializer s = new XmlSerializer(typeof(AgentSetAppearancePacket));
            TextReader r = new StreamReader(fileName);
            AgentSetAppearancePacket appearance = (AgentSetAppearancePacket)s.Deserialize(r);
            r.Close();
            appearance.AgentData.AgentID = Client.Network.AgentID;
            appearance.AgentData.SessionID = Client.Network.SessionID;
            Client.Network.SendPacket(appearance);
            Console.ForegroundColor = System.ConsoleColor.DarkGray;
            Console.WriteLine("* Loaded " + fileName);
            Console.ForegroundColor = System.ConsoleColor.Gray;
            Session.LastAppearance = appearance;
        }

        void CopyAppearance(Avatar av)
        {
            lock (appearances)
            {
                if (appearances.ContainsKey(av.ID))
                {
                    AvatarAppearancePacket appearance = appearances[av.ID];
                    AgentSetAppearancePacket set = new AgentSetAppearancePacket();
                    set.AgentData.AgentID = Client.Network.AgentID;
                    set.AgentData.SessionID = Client.Network.SessionID;
                    set.AgentData.SerialNum = 1;
                    set.AgentData.Size = new LLVector3(0.45f, 0.6f, 1.986565f);
                    set.ObjectData.TextureEntry = appearance.ObjectData.TextureEntry;
                    set.VisualParam = new AgentSetAppearancePacket.VisualParamBlock[appearance.VisualParam.Length];
                    int i = 0;
                    foreach (AvatarAppearancePacket.VisualParamBlock block in appearance.VisualParam)
                    {
                        set.VisualParam[i] = new AgentSetAppearancePacket.VisualParamBlock();
                        set.VisualParam[i].ParamValue = block.ParamValue;
                        i++;
                    }
                    set.WearableData = new AgentSetAppearancePacket.WearableDataBlock[0];
                    Client.Network.SendPacket(set);
                    Session.LastAppearance = set;
                }
            }
        }

        bool Clone(string name)
        {
            lock (avatars)
            {
                string findName = Session.FirstName.ToLower() + " " + Session.LastName.ToLower();
                foreach (Avatar av in avatars.Values)
                {
                    if (av.Name.ToLower() == findName)
                    {
                        CopyAppearance(av);
                        string appearanceFile = Client.Self.FirstName + " " + Client.Self.LastName + ".appearance";
                        SaveAppearance(appearanceFile, Session.LastAppearance);
                        return true;
                    }
                }
                return false;
            }
        }

    }
}

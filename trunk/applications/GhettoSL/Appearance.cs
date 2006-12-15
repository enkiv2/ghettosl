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
            Console.WriteLine("* Loaded " + fileName);
            lastAppearance = appearance;
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
                    lastAppearance = set;
                }
            }
        }

        bool Clone(string name)
        {
            lock (avatars)
            {
                string findName = firstName.ToLower() + " " + lastName.ToLower();
                foreach (Avatar av in avatars.Values)
                {
                    if (av.Name.ToLower() == findName)
                    {
                        CopyAppearance(av);
                        SaveAppearance("default.appearance", lastAppearance);
                        return true;
                    }
                }
                return false;
            }
        }

    }
}

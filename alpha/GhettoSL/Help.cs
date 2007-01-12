using System;
using System.Collections.Generic;
using System.Text;

namespace ghetto
{

    public class HelpSystem
    {
        public static Dictionary<string, string> HelpDict;

        public HelpSystem()
        {
            HelpDict = new Dictionary<string, string>();
            HelpDict.Add("anim <uuid>", "Start the specified animation");
            HelpDict.Add("camp <text>", "Find a chair with text matching the specified string");
            HelpDict.Add("clear", "Clear the console display");
            HelpDict.Add("fly", "Enable flying");
            HelpDict.Add("*follow <name|off>", "Follow the specified avatar, or \"off\" to disable");
            HelpDict.Add("go <X> <Y> [Z]", "Move to the specified coordinates using autopilot");
            HelpDict.Add("land", "Disable flying");
            HelpDict.Add("listen", "Listen to local chat (on by default)");
            HelpDict.Add("look", "Displays time and region sun direction");
            HelpDict.Add("quiet", "Stop listening to local chat");
            HelpDict.Add("*re [# message]", "List active IM \"windows\" or reply by window ID");
            HelpDict.Add("relog", "Log out and back in");
            HelpDict.Add("ride <name>", "Sit on the same object as the specified name");
            HelpDict.Add("run", "Enable running");
            HelpDict.Add("*script <scriptName>", "Execute the specified script file");
            HelpDict.Add("shout <message>", "Shout the specified message to users within 100m");
            HelpDict.Add("sit <uuid>", "Sit on the specified UUID");
            HelpDict.Add("sitg", "Sit on the ground at current location");
            HelpDict.Add("stand", "Stand while seated on an object or on the ground");
            HelpDict.Add("stopanim <uuid>", "Stop the specified animation");
            HelpDict.Add("teleport <sim> [x y z]", "Teleports to the specified destination");
            HelpDict.Add("touch <uuid>", "Touch the specified object");
            HelpDict.Add("touchid <localID>", "Touch the specified object LocalID");
            HelpDict.Add("updates <on|off>", "Toggles AgentUpdate timer (on by default)");
            HelpDict.Add("walk", "Disable running");
            HelpDict.Add("whisper", "Whisper the specified message to users within 5m");
            HelpDict.Add("who", "List avatars within viewing range");
        }
    }
}

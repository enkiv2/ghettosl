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
            HelpDict.Add("*re [name] [message]", "List IM sessions or reply by partial name match");
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

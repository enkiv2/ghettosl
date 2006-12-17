using System;
using System.Collections.Generic;
using System.Text;

namespace ghetto
{
    partial class GhettoSL
    {
        public void Help(string helpTopic)
        {
            string topic = "";
            if (helpTopic != null) topic = helpTopic.ToLower();
            switch (topic)
            {
                case "":
                    {
                        Console.WriteLine(
                            "--[ Commands ]--------------------------------------------------------\r\n" +
                            "anim <uuid> \t\tStart the specified animation\r\n" +
                            "camp <text> \t\tFind a chair with text matching the specified string\r\n" +
                            "clear \t\t\tClear the console display\r\n" +
                            "die \t\t\tLog out and exits\r\n" +
                            "fly \t\t\tEnable flying\r\n" +
                            "follow <name|off> \tFollow the specified avatar, or \"off\" to disable\r\n" +
                            "land \t\t\tDisable flying\r\n" +
                            "listen \t\t\tListen to local chat (on by default)\r\n" +
                            "quiet \t\t\tStop listening to local chat\r\n" +
                            "re [# message] \t\tList active IM \"windows\" or reply by window ID\r\n" +
                            "relog \t\t\tLog out and back in\r\n" +
                            "ride <name> \t\tSit on the same object as the specified name\r\n" +
                            "run \t\t\tEnable running\r\n" +
                            "script <scriptName> \tExecute the specified script file\r\n" +
                            "shout <message> \tShout the specified message to users within 100m\r\n" +
                            "sit <uuid> \t\tSit on the specified UUID\r\n" +
                            "sitg \t\t\tSit on the ground at current location\r\n" +
                            "stand \t\t\tStand while seated on an object or on the ground\r\n" +
                            "stopanim <uuid> \tStop the specified animation\r\n" +
                            "teleport <sim> [x y z] \tTeleports to the specified destination\r\n" +
                            "time \t\t\tDisplays time and region sun direction\r\n" +
                            "touch <uuid> \t\tTouch the specified object\r\n" +
                            "touchid <localID> \tTouch the specified object LocalID\r\n" +
                            "walk \t\t\tDisable running\r\n" +
                            "whisper <message> \tWhisper the specified message to users within 5m\r\n" +
                            "who \t\t\tList avatars within viewing range\r\n" +
                            "----------------------------------------------------------------------\r\n"
                        );
                        return;
                    }
            }

            Console.WriteLine("No help available for that topic. Type /help for a list of commands.");

        }
    }
}

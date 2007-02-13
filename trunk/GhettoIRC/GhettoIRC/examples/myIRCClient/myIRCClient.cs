using GhettoIRC;
using System;
using System.Collections.Generic;
using System.Text;

namespace Program
{
    class myIRCClient
    {
        static void Main(string[] argv)
        {
            IRCClient Client = new IRCClient();
            Client.Login("66.225.225.225", 6667, "test66", "testing", "testy mctesterson");
            while (true)
            {
                string read = Console.ReadLine();
                if (read == "/quit") break;
                else Client.SendCommand(read);
            }

        }
    }
}

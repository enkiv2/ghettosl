using libsecondlife;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace ghetto
{
    partial class GhettoSL
    {

        bool LoadScript(string scriptFile)
        {
            if (!File.Exists(scriptFile))
            {
                Console.WriteLine("File not found: " + scriptFile);
                return false;
            }
            string[] script = { };
            string input;
            int error = 0;
            StreamReader read = File.OpenText(scriptFile);
            for (int i = 0; (input = read.ReadLine()) != null; i++)
            {
                char[] splitChar = { ' ' };
                string[] args = input.ToLower().Split(splitChar);
                string[] commandsWithArgs = { "camp", "goto", "label", "pay", "payme", "say", "shout", "sit", "teleport", "touch", "touchid", "wait", "whisper" };
                string[] commandsWithoutArgs = { "fly", "land", "quit", "relog", "run", "sitg", "stand", "walk" };
                if (Array.IndexOf(commandsWithArgs, args[0]) > -1)
                {
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Missing argument(s) for command \"{0}\" on line {1} of {2}", args[0], i + 1, scriptFile);
                        error++;
                    }
                    else
                    {
                        Array.Resize(ref script, i + 1);
                        script[i] = input;
                    }
                }
                else if (Array.IndexOf(commandsWithoutArgs, args[0]) < 0)
                {
                    Console.WriteLine("Unknown command \"{0}\" on line {1} of {2}", args[0], i + 1, scriptFile);
                    error++;
                }
            }
            read.Close();
            if (error > 0)
            {
                Console.WriteLine("* Error loading script \"{0}\"", scriptFile);
                return false;
            }
            else
            {
                Console.WriteLine("* Running script \"{0}\"", scriptFile);
                RunScript(script);
                return true;
            }
        }


        void RunScript(string[] script)
        {
            for (int i = 0; i < script.Length; i++)
            {
                char[] splitChar = { ' ' };
                string[] cmd = script[i].Split(splitChar);
                switch (cmd[0])
                {
                    case "wait":
                        {
                            Console.WriteLine("* Sleeping {0} seconds...", cmd[1]);
                            Thread.Sleep(int.Parse(cmd[1]) * 1000);
                            continue;
                        }
                    case "goto":
                        {
                            int findLabel = Array.IndexOf(script, "label " + cmd[1]);
                            if (findLabel > -1) i = findLabel;
                            else Console.WriteLine("* Label \"{0}\" not found on line {1}", cmd[1], i + 1);
                            continue;
                        }
                    case "label":
                        {
                            continue;
                        }
                }
                Console.WriteLine("* SCRIPTED COMMAND: " + script[i]);
                ParseCommand(true, "/" + script[i], "", new LLUUID(), new LLUUID());
            }
        }

    }
}

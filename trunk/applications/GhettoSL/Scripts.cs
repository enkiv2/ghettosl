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
                string[] commandsWithArgs = { "camp", "go", "goto", "if", "label", "pay", "payme", "say", "shout", "sit", "teleport", "touch", "touchid", "updates", "wait", "whisper" };
                string[] commandsWithoutArgs = { "fly", "land", "quit", "relog", "run", "sitg", "stand", "walk" };
                if (Array.IndexOf(commandsWithArgs, args[0]) > -1 && args.Length < 2)
                {
                    Console.WriteLine("Missing argument(s) for command \"{0}\" on line {1} of {2}", args[0], i + 1, scriptFile);
                    error++;
                }
                else if (Array.IndexOf(commandsWithArgs, args[0]) < 0 && Array.IndexOf(commandsWithoutArgs, args[0]) < 0)
                {
                    Console.WriteLine("Unknown command \"{0}\" on line {1} of {2}", args[0], i + 1, scriptFile);
                    error++;
                }
                else
                {
                    Array.Resize(ref script, i + 1);
                    script[i] = input;
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

                //split command string into array
                char[] splitChar = { ' ' };
                string[] cmd = script[i].Split(splitChar);


                //check conditional statement
                bool fail = false; //FIXME - works, but can this go inside the "if" condition?
                if (cmd[0] == "if")
                {
                    switch (cmd[1])
                    {
                        case "standing":
                            {
                                if (Client.Self.SittingOn > 0 || Client.Self.Status.Controls.Fly) fail = true;
                                break;
                            }
                        case "sitting":
                            {
                                if (Client.Self.SittingOn == 0) fail = true;
                                break;
                            }
                        case "flying":
                            {
                                if (!Client.Self.Status.Controls.Fly) fail = true;
                                break;
                            }
                    }
                    //strip conditional statement from command array
                    //string[] temp = new string[cmd.Length - 2];
                    //for (int t = 2, s = 0; t < cmd.Length; t++, s++) temp[s] = cmd[t];
                    //cmd = temp;
                    Array.Copy(cmd, 2, cmd, 0, cmd.Length - 2); //More efficient

                }

                //if condition failed, skip to next loop iteration - FIXME same as above
                if (fail) continue;
                
                //process command
                switch (cmd[0])
                {
                    case "wait":
                        {
                            Console.WriteLine("* Sleeping {0} seconds...", cmd[1]);
                            Thread.Sleep(int.Parse(cmd[1]) * 1000);
                            continue;
                        }
                    case "go":
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

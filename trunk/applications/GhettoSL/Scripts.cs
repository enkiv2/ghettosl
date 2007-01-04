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

        string[] script = { };
        int scriptStep;
        uint scriptTime;
        System.Timers.Timer scriptWait;
        Dictionary<string, Event> scriptEvents;


        public enum EventTypes
        {
            Chat = 0,
            IM = 1,
            GetMoney =  2,
            GiveMoney = 3
        }

        public struct Event
        {
            /// <summary>
            /// Event type, enumerated in EventTypes.*
            /// </summary>
            public int Type;
            /// <summary>
            /// Used for text-matching events, or events with messages attached
            /// </summary>
            public string Text;
            /// <summary>
            /// Used for events which contain a numerical value
            /// </summary>
            public int Number;
            /// <summary>
            /// Command to execute on the specified event
            /// </summary>
            public string Command;
        }



        bool LoadScript(string scriptFile)
        {
            if (!File.Exists(scriptFile))
            {
                Console.WriteLine(TimeStamp() + "File not found: " + scriptFile);
                return false;
            }
            string input;
            int error = 0;
            StreamReader read = File.OpenText(scriptFile);
            for (int i = 0; (input = read.ReadLine()) != null; i++)
            {
                char[] splitChar = { ' ' };
                string[] args = input.ToLower().Split(splitChar);
                string[] commandsWithArgs = { "camp", "event", "go", "goto", "if", "label", "pay", "payme", "say", "shout", "sleep", "sit", "teleport", "touch", "touchid", "updates", "wait", "whisper" };
                string[] commandsWithoutArgs = { "balance", "die", "fly", "land", "quit", "relog", "run", "settime", "sitg", "stand", "walk" };
                if (Array.IndexOf(commandsWithArgs, args[0]) > -1 && args.Length < 2)
                {
                    Console.WriteLine(TimeStamp() + "Missing argument(s) for command \"{0}\" on line {1} of {2}", args[0], i + 1, scriptFile);
                    error++;
                }
                else if (Array.IndexOf(commandsWithArgs, args[0]) < 0 && Array.IndexOf(commandsWithoutArgs, args[0]) < 0)
                {
                    Console.WriteLine(TimeStamp() + "Unknown command \"{0}\" on line {1} of {2}", args[0], i + 1, scriptFile);
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
                Console.WriteLine("Error loading script \"{0}\"", scriptFile);
                return false;
            }
            else
            {
                Console.WriteLine(TimeStamp() + "Running script \"{0}\"", scriptFile);

                //initialize script
                scriptStep = 0;
                scriptEvents = new Dictionary<string, Event>();
                scriptWait = new System.Timers.Timer();
                scriptWait.AutoReset = false;
                scriptWait.Elapsed += new System.Timers.ElapsedEventHandler(ScriptWaitEvent);
                //run until wait or break
                while (ParseScriptLine(script, scriptStep)) scriptStep++;

                return true;
            }
        }

        void ScriptWaitEvent(object target, System.Timers.ElapsedEventArgs eventArgs)
        {
            do scriptStep++;
            while (ParseScriptLine(script, scriptStep));
        }

        /// <summary>
        /// Return false if there is a script error and the script should stop running
        /// </summary>
        bool ParseScriptLine(string[] script, int line)
        {

            if (line >= script.Length)
            {
                Console.WriteLine(TimeStamp() + "END OF SCRIPT");
                return false;
            }

            //split command string into array
            char[] splitChar = { ' ' };
            string[] cmd = script[line].Split(splitChar);

            int preArgs = 0; //number of arguments preceding actual command

            //check conditional statement
            if (cmd[0] == "if")
            {
                bool fail = false; //whether or not the condition fails
                bool not; //set to true if NOT is used
                if (cmd[1] == "not")
                {
                    not = true;
                    preArgs = 3; //"if not blah" = at least 3 words
                }
                else
                {
                    not = false;
                    preArgs = 2; //"if blah" = at least 2 words
                }

                string condition;
                if (not) condition = cmd[2];
                else condition = cmd[1];

                switch (condition)
                {
                    case "elapsed":
                        {
                            int waitTime;

                            if (cmd.Length < preArgs + 2)
                            {
                                Console.WriteLine(TimeStamp() + "Script error in line " + line + ": Should be IF ELAPSED 10 COMMAND for 10 seconds since SETTIME command. (time or command missing?)");
                                return false;
                            }
                            else if (int.TryParse(cmd[preArgs], out waitTime) == false)
                            {
                                Console.WriteLine(TimeStamp() + "Script error in line " + line + ": Should be IF ELAPSED 10 COMMAND for 10 seconds since SETTIME command. (invalid time?)");
                                return false;
                            }
                            else
                            {
                                uint elapsed = Helpers.GetUnixTime() - scriptTime;
                                if (elapsed < waitTime) fail = true;
                            }
                            preArgs++; //account for extra preArg (the number of seconds)
                            break;
                        }
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
                    case "region":
                        {
                            char[] splitQuotes = { '"' };
                            string[] sq = script[line].ToLower().Split(splitQuotes);
                            if (sq.Length < 3)
                            {
                                Console.WriteLine(TimeStamp() + "Script error in line " + line + ": Should be IF REGION \"Simulator Name\" COMMAND statement (missing quotes?)");
                                return false;
                            }
                            else
                            {
                                string testRegion = sq[1];
                                char[] splitSpaces = { ' ' };
                                string[] regionNameWords = testRegion.Split(splitSpaces);
                                preArgs += regionNameWords.Length; //add number of words to preArgs count
                                if (testRegion != Client.Network.CurrentSim.Region.Name.ToLower()) fail = true;
                            }
                            break;
                        }
                }

                if (not) fail = !fail; //swap fail value

                Array.Copy(cmd, preArgs, cmd, 0, cmd.Length - preArgs);
                Array.Resize(ref cmd, cmd.Length - preArgs);

                if (fail) return true; //just a logical fail, not a script error
            }



            //process command
            switch (cmd[0])
            {
                case "settime":
                    {
                        scriptTime = Helpers.GetUnixTime();
                        return true;
                    }
                case "goto":
                    {
                        int findLabel = Array.IndexOf(script, "label " + cmd[1]);
                        if (findLabel > -1)
                        {
                            scriptStep = findLabel - 1;
                            Thread.Sleep(100); //imposed 1ms delay to prevent bad scripts from hogging cpu
                        }
                        else
                        {
                            Console.WriteLine(TimeStamp() + "Script error: Label \"{0}\" not found on line {1}", cmd[1], line + 1);
                            return false;
                        }
                        return true;
                    }
                case "label":
                    {
                        return true;
                    }
                case "event":
                    {
                        //check for bad scripting
                        string eventLabel = cmd[1];
                        if (cmd[2] == "off")
                        {
                            scriptEvents.Remove(eventLabel);
                            Console.WriteLine(TimeStamp() + "Removed event " + eventLabel);
                            return true;
                        }

                        Event newEvent = new Event();

                        if (cmd[2] == "chat") newEvent.Type = (int)EventTypes.Chat;
                        else if (cmd[2] == "im") newEvent.Type = (int)EventTypes.IM;
                        else if (cmd[2] == "getmoney") newEvent.Type = (int)EventTypes.GetMoney;
                        else if (cmd[2] == "givemoney") newEvent.Type = (int)EventTypes.GiveMoney;
                        else
                        {
                            Console.WriteLine(TimeStamp() + "Script error: Invalid event description on line {0} (events supported are CHAT, IM, GETMONEY, GIVEMONEY)", line);
                            return false;
                        }

                        if (cmd[2] == "chat" || cmd[2] == "im")
                        {
                            char[] splitChr = { '"' };
                            string[] splitQuotes = script[line].Split(splitChr);
                            if (splitQuotes.Length < 3)
                            {
                                Console.WriteLine(TimeStamp() + "Script error: Invalid event text on line {0} (missing quotations)", line);
                                return false;
                            }
                            newEvent.Text = splitQuotes[1];

                        }
                        
                        //ok, the script looks good


                        int start = 3;
                        if (newEvent.Text != null)
                        {
                            char[] splitSp = { ' ' };
                            string[] splitSpaces = newEvent.Text.Split(splitSp);
                            start += splitSpaces.Length;
                        }
                        int len = cmd.Length - start;
                        string[] command = new string[len];
                        Array.Copy(cmd, start, command, 0, len);
                        newEvent.Command = String.Join(" ", command);

                        scriptEvents[eventLabel] = newEvent;

                        Console.WriteLine(TimeStamp() + "ADDED EVENT {0} ({1}): {2}", eventLabel, newEvent.Type, newEvent.Command);
                        return true;
                    }
                case "sleep":
                    {
                        float interval;
                        if (cmd.Length < 2) Console.WriteLine(TimeStamp() + "No sleep time specified");
                        else if (!float.TryParse(cmd[1], out interval)) Console.WriteLine(TimeStamp() + "Invalid sleep time specified");
                        else ScriptSleep(interval);
                        return false; //pause script
                    }
                case "wait": //same as sleep
                    {
                        float interval;
                        if (cmd.Length < 2) Console.WriteLine(TimeStamp() + "No sleep time specified");
                        if (!float.TryParse(cmd[1], out interval)) Console.WriteLine(TimeStamp() + "Invalid sleep time specified");
                        else ScriptSleep(interval);
                        return false; //pause script
                    }
            }
            if (script.Length > 1)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(TimeStamp() + "SCRIPTED COMMAND: " + script[line]);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            ParseCommand(true, String.Join(" ", cmd), "", new LLUUID(), new LLUUID());
            return true;
        }

        string ParseScriptVariables(string scriptCommand, string name, LLUUID id, int amount, string message)
        {
            string sc = scriptCommand;
            sc = sc.Replace("$amount", amount.ToString());
            sc = sc.Replace("$earned", Session.MoneyReceived.ToString());
            sc = sc.Replace("$id", id.ToString());
            sc = sc.Replace("$master", Session.MasterID.ToString());
            sc = sc.Replace("$message", message);
            sc = sc.Replace("$name", name);
            sc = sc.Replace("$spent", Session.MoneySpent.ToString());
            return sc;
        }

        void ScriptSleep(float seconds)
        {
            Console.WriteLine(TimeStamp() + "Sleeping {0} seconds...", seconds);
            scriptWait.Interval = (int)(seconds * 1000);
            scriptWait.Enabled = true;
        }

    }
}

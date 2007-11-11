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

//using libsecondlife.Utilities;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace ghetto
{
    public class ScriptSystem
    {

        public class UserScript
        {
            /// <summary>
            /// The session number the script was loaded from
            /// FIXME - This is really just for $session in the Load event. do we need it?
            /// </summary>
            public uint SessionNumber;
            /// <summary>
            /// Same as the key in the Scripts dictionary
            /// </summary>
            public string ScriptName;
            /// <summary>
            /// Current line script step, referenced after a sleep
            /// </summary>
            public int CurrentStep;
            /// <summary>
            /// Timestamp of the last settime command
            /// </summary>
            public uint SetTime;
            /// <summary>
            /// Vector supplied by /settarget
            /// </summary>
            public LLVector3 SetTarget;
            /// <summary>
            /// Dictionary of scripted aliases
            /// </summary>
            public Dictionary<string, string[]> Aliases;
            /// <summary>
            /// Dictionary of scripted events
            /// </summary>
            public Dictionary<EventTypes, ScriptEvent> Events;
            /// <summary>
            /// Variables accessed with /set %var value, and %var
            /// </summary>
            public Dictionary<string, string> Variables;

            /// <summary>
            /// Load the specified script file into the Lines array
            /// </summary>
            /// <returns>-1 if read-error, 0 if no errors, or the line number where an error occurred</returns>
            public int Load(string scriptFile)
            {

                StreamReader read;
                try { read = File.OpenText(scriptFile); }
                catch { return -1; }

                string[] script = { };
                string input;
                for (int i = 0; (input = read.ReadLine()) != null; i++)
                {
                    string[] splitChar = { " " };
                    string[] args = input.Trim().Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
                    Array.Resize(ref script, i + 1);
                    script[i] = String.Join(" ", args);
                }

                read.Close();

                string currentAlias = "";
                EventTypes currentEvent = EventTypes.NULL;
                string[] currentList = { };
                for (int i = 0, level = 0; i < script.Length; i++)
                {
                    string[] splitChar = { " " };
                    string[] cmd = script[i].Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
                    if (currentAlias != "" || currentEvent != EventTypes.NULL)
                    {
                        if (script.Length == 0) continue;
                        else if (script[i] == "{") { level++; continue; }
                        else if (script[i] == "}")
                        {
                            level--;

                            if (level < 0)
                            {
                                Display.Error(0, "Bracket mismatch");
                                return i + 1;
                            }
                            else if (level == 0)
                            {
                                //all { have been closed with } - Time to store the list we collected and move on
                                if (currentList.Length == 0)
                                {
                                    if (currentAlias != "") Display.Error(0, "Empty alias \"" + currentAlias + "\" on line " + (i + 1));
                                    else if (currentEvent != EventTypes.NULL) Display.Error(0, "Empty event \"" + currentEvent + "\" on line " + (i + 1));
                                    return i + 1;
                                }
                                if (currentAlias != "")
                                {
                                    Aliases.Add(currentAlias, currentList);
                                    //DEBUG
                                    //Display.InfoResponse(0, "Added alias \"" + currentAlias + "\" (" + currentList.Length + " lines)");
                                    //Console.WriteLine(String.Join(Environment.NewLine, currentList));
                                    currentAlias = "";
                                }
                                else if (currentEvent != EventTypes.NULL)
                                {
                                    Events.Add(currentEvent, new ScriptEvent(ScriptName, currentList));
                                    //DEBUG
                                    //Display.InfoResponse(0, "Added " + currentEvent + " event (" + currentList.Length + " lines)");
                                    //Console.WriteLine(String.Join(Environment.NewLine, currentList));
                                    currentEvent = EventTypes.NULL;
                                }
                                //we already stored the event or alias so we can clear this info
                                string[] empty = { };
                                currentList = empty;
                                continue;
                            }
                        }
                        else //not a { or a }, and level > 0, so add to the current list
                        {
                            if (cmd.Length == 0) continue;
                            int len = currentList.Length;
                            Array.Resize(ref currentList, len + 1);
                            currentList[len] = script[i];
                        }
                    }
                    else
                    {
                        if (cmd.Length == 0) continue;
                        string prefix = cmd[0].ToLower();
                        string name = cmd[1];
                        if (prefix == "alias" && cmd.Length == 2)
                        {
                            if (Aliases.ContainsKey(name))
                            {
                                Display.Error(0, "Duplicate definition for alias \"" + name + "\"");
                                return i + 1;
                            }
                            currentAlias = cmd[1];
                        }
                        else if (prefix == "event" && cmd.Length == 2)
                        {
                            ScriptSystem.EventTypes type = EventTypeByName(cmd[1]);
                            if (type == EventTypes.NULL)
                            {
                                Display.Error(0, "Unknown event type \"" + cmd[1] + "\"");
                                return i + 1;
                            }                            
                            else if (Events.ContainsKey(type))
                            {
                                Display.Error(0, "Duplicate definition for event \"" + name + "\"");
                                return i + 1;
                            }
                            currentEvent = type;
                        }
                        else return i + 1; //unknown prefix
                    }
                }

                return 0; //no errors
            }

            /*
            public void Sleep(float seconds)
            {
                Display.InfoResponse(SessionNumber, "Sleeping " + seconds + " seconds...");
                SleepTimer.Interval = (int)(seconds * 1000);
                SleepTimer.AutoReset = false;
                SleepTimer.Enabled = true;
            }
            */

            public UserScript(uint sessionNum, string scriptFile)
            {
                SetTarget = LLVector3.Zero;
                SetTime = Helpers.GetUnixTime();
                Aliases = new Dictionary<string, string[]>();
                Events = new Dictionary<EventTypes, ScriptEvent>();
                SessionNumber = sessionNum;
                ScriptName = scriptFile;
                CurrentStep = 0;
                SetTime = Helpers.GetUnixTime();
                Variables = new Dictionary<string, string>();
            }
        }

        public class UserTimer : IDisposable
        {
            uint sessionNum;
            System.Timers.Timer Timer;
            string Name;
            public string Command;
            public int RepeatsRemaining;

            public void Dispose()
            {
                Timer.AutoReset = false;
                Timer.Stop();
                Timer.Dispose();
                Stop();
            }

            public void Stop()
            {
                Display.InfoResponse(sessionNum, "Timer \"" + Name + "\" halted");
            }

            public UserTimer(uint sessionNumber, string name, int interval, bool milliseconds, int repeats, string command)
            {
                sessionNum = sessionNumber;
                Name = name;
                Command = command;
                RepeatsRemaining = repeats;
                Timer = new System.Timers.Timer(interval);
                if (milliseconds) Timer.Interval = interval;
                else Timer.Interval = interval * 1000;
                Timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Elapsed);
                Timer.AutoReset = true;
                Timer.Enabled = true;
            }

            void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
            {

                bool limited = false;
                if (RepeatsRemaining == 0) limited = false;
                if (RepeatsRemaining > 0)
                {
                    limited = true;
                    RepeatsRemaining--;
                }
                CommandResult result = Parse.Command(sessionNum, "", Command, true, false);
                if ((limited && RepeatsRemaining == 0) || (result != CommandResult.NoError && result != CommandResult.ConditionFailed && result != CommandResult.Return))
                {
                    Interface.Sessions[sessionNum].Timers[Name].Dispose();
                    //System.Timers.Timer t = (System.Timers.Timer)sender;
                    Interface.Sessions[sessionNum].Timers[Name] = null;
                    Interface.Sessions[sessionNum].Timers.Remove(Name);
                }
            }
        }

        public class ScriptEvent
        {
            /// <summary>
            /// Command to execute on the specified event
            /// </summary>
            public string[] Commands;

            /// <summary>
            /// Script name attached to the event, if any
            /// </summary>
            public string ScriptName;

            public ScriptEvent(string scriptName, string[] commandList)
            {
                Commands = commandList;
                ScriptName = scriptName;
                //EventType = eventType;
            }
        }

        //Returned by Parse.Command
        public enum CommandResult
        {
            NoError = 0,
            Return = 1,
            ConditionFailed = 2,
            InvalidUsage = 3,
            UnexpectedError = 4
        }

        //Used by scripted events
        public enum EventTypes
        {
            NULL = 0,
            Load = 1,
            Connect = 2,
            Disconnect = 3,
            TeleportFinish = 4,
            Chat = 5,
            IM = 6,
            Update = 7,
            Sit = 8,
            Unsit = 9,
            GroupIM = 10, //FIXME - still missing/incorrectly handled as IM
            ScriptDialog = 11,
            GetMoney = 12,
            GiveMoney = 13,
            GetItem = 14
        }

        public static EventTypes EventTypeByName(string typeString)
        {
            for (int t = 1; t < 14; t++)
            {
                string compare = (ScriptSystem.EventTypes)t + "";
                if (compare == typeString)
                {
                    return (ScriptSystem.EventTypes)t;
                }
            }
            return EventTypes.NULL;
        }

        public static void TriggerEvents(uint sessionNum, ScriptSystem.EventTypes eventType, Dictionary<string, string> identifiers)
        {
            GhettoSL.UserSession Session = Interface.Sessions[sessionNum];
            if (identifiers == null) identifiers = new Dictionary<string, string>();
            identifiers.Add("$me", Session.Client.Self.FirstName + " " + Session.Client.Self.LastName);
            identifiers.Add("$myid", Session.Client.Self.AgentID.ToStringHyphenated());
            lock (Interface.Scripts)
            {
                foreach (KeyValuePair<string, ScriptSystem.UserScript> s in Interface.Scripts)
                {
                    if (s.Value.Events.ContainsKey(eventType))
                    {
                        Parse.CommandArray(sessionNum, s.Value.ScriptName, s.Value.Events[eventType].Commands, identifiers);
                    }
                }
            }
        }

        /// <summary>
        /// Returns a multi-word (quoted) argument from a command array with quotations stripped
        /// </summary>
        /// <param name="cmd">Command string (split by spaces)</param>
        /// <param name="start">Position in cmd array where quoted argument starts</param>
        /// <returns></returns>
        public static string QuoteArg(string[] args, int start)
        {
            if (start >= args.Length) return "";
            else if (args[start].Substring(0, 1) != "\"") return args[start];
            else if (args[start].Substring(args[start].Length - 1, 1) == "\"") return args[start].Replace("\"", "");

            string ret = args[start];
            for (int i = start + 1; i < args.Length; i++)
            {
                ret += " " + args[i];
                if (args[i].Substring(args[i].Length - 1, 1) == "\"") break;
            }
            return ret.Replace("\"", "");
        }

        public static void DirList(uint sessionNum, LLUUID folder)
        {
            GhettoSL.UserSession Session = Interface.Sessions[sessionNum];
            List<InventoryBase> contents = Session.Client.Inventory.FolderContents(folder, Session.Client.Self.AgentID, true, true, InventorySortOrder.ByDate, 1000);
            foreach (InventoryBase inv in contents)
            {
                //Console.WriteLine(inv.UUID.ToStringHyphenated() + " - " + inv.Name);
                InventoryItem item = null;

                //FIXME - move to Display
                if (inv is InventoryFolder)
                {
                    Display.SetColor(ConsoleColor.Yellow);
                }
                else
                {
                    item = (InventoryItem)inv;
                    if (item.InventoryType == InventoryType.Notecard) Display.SetColor(ConsoleColor.Gray);
                    else if (item.InventoryType == InventoryType.Texture) Display.SetColor(ConsoleColor.Cyan);
                    else if (item.InventoryType == InventoryType.LSL) Display.SetColor(ConsoleColor.Magenta);
                    else if (item.InventoryType == InventoryType.Wearable) Display.SetColor(ConsoleColor.Blue);
                    else if (item.InventoryType == InventoryType.Object || item.InventoryType == InventoryType.Attachment) Display.SetColor(ConsoleColor.DarkYellow);
                    else Display.SetColor(ConsoleColor.DarkGray);
                }
                if (inv is InventoryFolder) Console.Write(Display.Pad("Folder", 9) + " ");
                else Console.Write(Display.Pad(item.AssetType.ToString(), 9) + " ");
                Display.SetColor(ConsoleColor.DarkGray);
                Console.Write(inv.UUID.ToStringHyphenated() + "  ");
                Display.SetColor(ConsoleColor.DarkCyan);
                string iName = inv.Name;
                if (iName.Length > 20) iName = iName.Substring(0, 20) + "...";
                Console.Write(Display.Pad(iName, 24) + " ");
                Console.Write("\n");
                Display.SetColor(ConsoleColor.Gray);
            }
        }

    }
}

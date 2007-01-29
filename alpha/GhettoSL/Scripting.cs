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
using libsecondlife.InventorySystem;
using libsecondlife.Utilities;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace ghetto
{
    public class ScriptSystem
    {

        //public static uint sessionNum;

        //public static GhettoSL.UserSession Session;

        //public ScriptSystem(uint sessionNumber)
        //{
        //    sessionNum = sessionNumber;
        //}

        public class UserScript
        {
            public uint SessionNumber;
            public string ScriptName;
            /// <summary>
            /// Array containing each line of the script
            /// </summary>
            public string[] Lines;
            /// <summary>
            /// Current line script step, referenced after a sleep
            /// </summary>
            public int CurrentStep;
            /// <summary>
            /// Timestamp of the last "settime" command
            /// </summary>
            public uint ScriptTime;
            /// <summary>
            /// Timestamp of the last (or currently active) sleep
            /// </summary>
            public uint SleepingSince;
            /// <summary>
            /// Timer to invoke the next command after sleeping
            /// </summary>
            public System.Timers.Timer SleepTimer;
            /// <summary>
            /// Dictionary of scripted events
            /// </summary>
            public Dictionary<string, ScriptEvent> Events;
            /// <summary>
            /// Variables accessed with /set %var value, and %var
            /// </summary>
            public Dictionary<string, string> Variables;
            /// <summary>
            /// Load the specified script file into the Lines array
            /// </summary>
            public bool Load(string scriptFile)
            {

                if (!File.Exists(scriptFile)) return false;

                string[] script = { };
                string input;
                int error = 0;
                //FIXME - display file access error if file is in use
                StreamReader read = File.OpenText(scriptFile);
                for (int i = 0; (input = read.ReadLine()) != null; i++)
                {
                    char[] splitChar = { ' ' };
                    string[] args = input.ToLower().Trim().Split(splitChar);
                    string[] commandsWithArgs = { "camp", "echo", "event", "go", "goto", "if", "inc", "label", "login", "pay", "payme", "say", "set", "shout", "sit", "sleep", "teleport", "touch", "touchid", "updates", "wait", "whisper" };
                    string[] commandsWithoutArgs = { "break", "fly", "land", "listen", "quiet", "quit", "relog", "run", "sitg", "stand", "walk" };

                    bool skip = false;
                    if (args.Length == 1 && (args[0] == "" || args[0].Substring(args[0].Length - 1, 1) == ":")) skip = true;

                    if (!skip && Array.IndexOf(commandsWithArgs, args[0]) > -1 && args.Length < 2)
                    {
                        Console.WriteLine("Missing argument(s) for command \"{0}\" on line {1} of {2}", args[0], i + 1, scriptFile);
                        error++;
                    }
                    else if (!skip && Array.IndexOf(commandsWithArgs, args[0]) < 0 && Array.IndexOf(commandsWithoutArgs, args[0]) < 0)
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
                if (error > 0) return false;
                else
                {
                    Lines = script;
                    return true;
                }
            }

            public void Sleep(float seconds)
            {
                SleepingSince = Helpers.GetUnixTime();
                Display.InfoResponse(SessionNumber, "Sleeping " + seconds + " seconds...");
                SleepTimer.Interval = (int)(seconds * 1000);
                SleepTimer.AutoReset = false;
                SleepTimer.Enabled = true;
            }

            public void Step(int stepNum)
            {
                if (stepNum >= Lines.Length) return;

                CurrentStep = stepNum;
                string line = Lines[CurrentStep].Trim();

                while (CurrentStep < Lines.Length && (line.Length < 1 || line.Substring(line.Length - 1,1) == ":" || ParseCommand(SessionNumber, ScriptName, Lines[CurrentStep], true, false)))
                {
                    CurrentStep++;
                    line = Lines[CurrentStep].Trim();
                }
            }

            public void ScriptTimerHandler(object target, System.Timers.ElapsedEventArgs args)
            {
                Step(CurrentStep + 1);
            }

            public UserScript(uint sessionNum, string scriptFile)
            {
                SessionNumber = sessionNum;
                ScriptName = scriptFile;
                CurrentStep = 0;
                ScriptTime = Helpers.GetUnixTime();
                SleepingSince = ScriptTime;
                SleepTimer = new System.Timers.Timer();
                SleepTimer.Elapsed += new System.Timers.ElapsedEventHandler(ScriptTimerHandler);
                Variables = new Dictionary<string, string>();
            }

        }


        public class ScriptEvent
        {
            /// <summary>
            /// Event type, enumerated in EventTypes.*
            /// </summary>
            public EventTypes EventType;

            /// <summary>
            /// Command to execute on the specified event
            /// </summary>
            public string Command;

            /// <summary>
            /// Script name attached to the event, if any
            /// </summary>
            public string ScriptName;

            public ScriptEvent(string scriptName)
            {
                ScriptName = scriptName;
                EventType = EventTypes.NULL;
                Command = "";
            }
        }


        //Used by scripted events
        public enum EventTypes
        {
            NULL = 0,
            Connect = 1,
            Disconnect = 2,
            TeleportFinish = 3,
            Chat = 4,
            IM = 5,
            Sit = 6,
            Unsit = 7,
            GroupIM = 8, //FIXME - still missing/incorrectly handled as IM
            ScriptDialog = 9,
            GetMoney = 10,
            GiveMoney = 11,
            GetItem = 12
        }


        /// <summary>
        /// Triggered by callbacks for matching events
        /// </summary>
        /// <param name="command">Command triggered on this event</param>
        /// <param name="name">Avatar/object name associated with event</param>
        /// <param name="message">Message/text associated with event</param>
        /// <param name="id">UUID associated with event</param>
        /// <param name="amount">L$ amount associated with event</param>
        public static void TriggerEvent(uint sessionNum, string command, string scriptName)
        {
            ParseCommand(sessionNum, scriptName, command, true, false);
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
            else if (args[start].Substring(args.Length - 1, 1) == "\"") return args[start].Replace("\"", "");

            string ret = args[start];
            for (int i = start + 1; i < args.Length; i++)
            {
                ret += " " + args[i];
                if (args[i].Substring(args.Length - 1, 1) == "\"") break;
            }
            return ret.Replace("\"", "");
        }

        public static void DirList(uint sessionNum, string folder)
        {
            GhettoSL.UserSession Session = Interface.Sessions[sessionNum];
            InventoryFolder iFolder = Session.Client.Inventory.getFolder(folder);
            if (iFolder == null)
            {
                Display.Error(Session.SessionNumber, "Folder not found: " + folder);
                return;
            }
            iFolder.RequestDownloadContents(false, true, true, false).RequestComplete.WaitOne(15000, false);
            foreach (InventoryBase inv in iFolder.GetContents())
            {
                if (!(inv is InventoryItem)) return;

                InventoryItem item = (InventoryItem)inv;
                string type;

                if (inv is InventoryNotecard)
                {
                    Display.SetColor(ConsoleColor.Gray);
                    type = "Notecard";
                }

                else if (inv is InventoryImage)
                {
                    Display.SetColor(ConsoleColor.Cyan);
                    type = "Image";
                }

                else if (inv is InventoryScript)
                {
                    Display.SetColor(ConsoleColor.Magenta);
                    type = "Script";
                }

                else if (inv is InventoryWearable)
                {
                    Display.SetColor(ConsoleColor.Blue);
                    type = "Wearable";
                }

                else if (item.Type == 6)
                {
                    Display.SetColor(ConsoleColor.DarkYellow);
                    type = "Object";
                }

                else
                {
                    Display.SetColor(ConsoleColor.DarkGray);
                    int t = (int)(item.Type);
                    type = t.ToString();
                }
                //FIXME - move to Display
                Console.Write(Display.Pad(type, 9) + " ");
                Display.SetColor(ConsoleColor.DarkCyan);
                string iName = item.Name;
                if (iName.Length > 18) iName = iName.Substring(0, 18) + "...";
                Console.Write(Display.Pad(iName, 22) + " ");
                Display.SetColor(ConsoleColor.DarkGray);
                Console.Write(Display.Pad(item.ItemID.ToString(), 34) + "\n");
                Display.SetColor(ConsoleColor.Gray);
            }
        }

        /// <summary>
        /// /login command
        /// </summary>
        public static bool ParseLoginCommand(uint sessionNum, string[] cmd)
        {
            GhettoSL.UserSession Session = Interface.Sessions[sessionNum];
            uint newSession = sessionNum;

            if (cmd.Length < 2) { Display.Help("login");  return false; }

            string flag = cmd[1].ToLower();
            int index = 0;

            if (flag == "-r")
            {
                if (cmd.Length != 2 && cmd.Length < 5) return false;

                if (cmd.Length > 2)
                {
                    Session.Settings.FirstName = cmd[2];
                    Session.Settings.LastName = cmd[3];
                    Session.Settings.Password = cmd[4];
                    index = 5;
                }
            }

            else if (flag == "-m")
            {
                if (cmd.Length < 5) return false;

                do newSession++;
                while (Interface.Sessions.ContainsKey(newSession));

                Interface.Sessions.Add(newSession, new GhettoSL.UserSession(Interface.CurrentSession));

                Session = Interface.Sessions[newSession];
                Session.SessionNumber = newSession;                
                Session.Settings.FirstName = cmd[2];
                Session.Settings.LastName = cmd[3];
                Session.Settings.Password = cmd[4];
                Display.InfoResponse(newSession, "Creating session for " + Session.Name + "...");

                index = 5;
            }

            else if (cmd.Length < 4)
            {
                //no flags, but we should still have the command + 3 args
                return false;
            }

            else
            {
                Session.Settings.FirstName = cmd[1];
                Session.Settings.LastName = cmd[2];
                Session.Settings.Password = cmd[3];
                index = 4;
            }

            for (bool lastArg = false; index < cmd.Length; index++ , lastArg = false)
            {
                string arg = cmd[index];
                if (index >= cmd.Length) lastArg = true;

                if (arg == "-q" || arg == "-quiet")
                    Session.Settings.DisplayChat = false;
                else if (!lastArg && (arg == "-m" || arg == "-master" || arg == "-masterid"))
                {
                    LLUUID master;
                    if (LLUUID.TryParse(cmd[index + 1], out master)) Session.Settings.MasterID = master;
                }
                else if (arg == "-n" || arg == "-noupdates")
                    Session.Settings.SendUpdates = false;
                else if (!lastArg && (arg == "-p" || arg == "-pass" || arg == "-passphrase"))
                    Session.Settings.PassPhrase = ScriptSystem.QuoteArg(cmd, index + 1);
                else if (!lastArg && (arg == "-s" || arg == "-script"))
                    Session.Settings.Script = ScriptSystem.QuoteArg(cmd, index + 1);
                else if (arg == "-here")
                {
                    GhettoSL.UserSession fromSession = Interface.Sessions[sessionNum];
                    if (fromSession.Client.Network.Connected)
                    {
                        Session.Settings.URI = "uri:" + fromSession.Client.Network.CurrentSim.Region.Name + "&" + (int)fromSession.Client.Self.Position.X + "&" + (int)fromSession.Client.Self.Position.Y + "&" + (int)fromSession.Client.Self.Position.Z;
                    }
                    else
                    {
                        Session.Settings.URI = fromSession.Settings.URI;
                    }
                }
                else if (arg.Length > 13 && arg.Substring(0, 13) == "secondlife://")
                {
                    string url = ScriptSystem.QuoteArg(cmd, index);
                    Session.Settings.URI = "uri:" + url.Substring(13, arg.Length - 13).Replace("%20", " ").Replace("/", "&");
                }
            }

            Interface.CurrentSession = newSession;
            
            if (Session.Client.Network.Connected)
            {
                //FIXME - Add RelogTimer to UserSession, in place of this
                if (Session != null) Session.Client.Network.Logout();
                Thread.Sleep(1000);
            }

            Session.Login();
            return true;

        }


        /// <summary>
        /// /script command
        /// </summary>
        public static bool ParseLoadScriptCommand(uint sessionNum, string[] cmd)
        {
            GhettoSL.UserSession Session = Interface.Sessions[sessionNum];

            if (cmd.Length == 1 || cmd[1] == "help")
            {
                //FIXME - add more verbose script help to Display
                Display.Help(cmd[0]);
                return false;
            }

            string arg = cmd[1].ToLower();
            string scriptFile = cmd[1];

            if (arg == "-u" || arg == "-unload" || arg == "unload")
            {
                if (cmd.Length < 3) { Display.Help(arg); return false; }
                scriptFile = cmd[2];
                if (!Interface.Scripts.ContainsKey(cmd[2])) Display.InfoResponse(sessionNum, "No such script loaded. For a list of active scripts, use /scripts.");
                else Interface.Scripts.Remove(cmd[2]);
                //remove all events tied to this script
                foreach (KeyValuePair<string, ScriptEvent> pair in Session.ScriptEvents)
                {
                    if (pair.Value.ScriptName == cmd[2]) Session.ScriptEvents.Remove(pair.Key);
                }
                if (scriptFile == cmd[2]) return false; //script unloaded itself
                else return true;
            }

            if (!File.Exists(scriptFile))
            {
                if (!File.Exists(scriptFile + ".script"))
                {
                    Display.Error(sessionNum, "File not found: " + scriptFile);
                    return false;
                }
                else scriptFile = scriptFile + ".script";
            }

            if (Interface.Scripts.ContainsKey(scriptFile))
            {
                //scriptFile is already loaded. refresh.
                Display.InfoResponse(sessionNum, "Reloading script: " + scriptFile);
            }
            else
            {
                //add entry for scriptFile
                Display.InfoResponse(sessionNum, "Loading script: " + scriptFile);
                Interface.Scripts.Add(scriptFile, new UserScript(sessionNum, scriptFile));
            }

            if (Interface.Scripts[scriptFile].Load(scriptFile))
            {
                //start the script
                Interface.Scripts[scriptFile].Step(0);
            }
            return true;
        }


        /// <summary>
        /// /event command
        /// </summary>
        public static bool ParseEventCommand(uint sessionNum, string[] cmd, string scriptName)
        {
            GhettoSL.UserSession Session = Interface.Sessions[sessionNum];
            if (cmd.Length < 3) {
                Display.Help(cmd[0]);
                return false;
            }
            else if (cmd[1] == "-r" || cmd[1] == "-remove" || cmd[1] == "remove")
            {
                if (cmd.Length < 3) Display.Help(cmd[0]);
                else if (!Session.ScriptEvents.ContainsKey(cmd[2])) Display.Error(sessionNum, "No such event: " + cmd[2]);
                else
                {
                    Session.ScriptEvents.Remove(cmd[2]);
                    Display.InfoResponse(sessionNum, "Removed event: " + cmd[2]);
                    return true;
                }
            }
            else if (cmd.Length < 4) {
                Display.Help(cmd[0]);
            }
            else
            {
                string eventLabel = cmd[1];
                string eventType = "";
                int eventNum = 0;
                foreach (int e in Enum.GetValues(typeof(EventTypes)))
                {
                    if (((EventTypes)e).ToString().ToLower() == cmd[2].ToLower())
                    {
                        eventNum = e;
                        eventType = ((EventTypes)e).ToString();
                        break;
                    }
                }
                if (eventNum == 0)
                {
                    Display.Error(sessionNum, "Unrecognized event type: " + cmd[2]);
                    return false;
                }

                string eventCommand = "";
                for (int i = 3; i < cmd.Length; i++)
                {
                    if (eventCommand != "") eventCommand += " ";
                    eventCommand += cmd[i];
                }

                ScriptEvent newEvent = new ScriptEvent(scriptName);
                newEvent.EventType = (EventTypes)eventNum;
                newEvent.Command = eventCommand;
                if (Session.ScriptEvents.ContainsKey(eventLabel))
                {
                    Session.ScriptEvents.Remove(eventLabel);
                    Session.ScriptEvents.Add(eventLabel, newEvent);
                    Display.InfoResponse(sessionNum, "Replaced " + eventType + " event: " + eventLabel);
                }
                else
                {
                    Session.ScriptEvents.Add(eventLabel, newEvent);
                    Display.InfoResponse(sessionNum, "Added " + eventType + " event: " + eventLabel);
                }
                return true;
            }
            return false;
        }

        public static bool ParseConditions(uint sessionNum, string conditions)
        {
            //FIXME - actually parse paren grouping instead of just stripping parens
            string c = conditions.Replace("(", "").Replace(")", "");

            bool pass = true;

            string[] splitLike = { " like ", " LIKE ", " Like " };
            string[] splitMatch = { " match ", " MATCH ", " Match " };
            string[] splitAnd = { " and ", " AND ", " And ", "&&" };
            string[] splitOr = { " or ", " OR ", " Or ", " || " };
            string[] splitEq = { " == " , " = "};
            string[] splitNot = { " != " , " <> " };
            string[] splitLT = { " < " };
            string[] splitGT = { " > " };
            string[] splitLE = { " <= " , " =< "};
            string[] splitGE = { " >= " , " => "};


            string[] condOr = ParseVariables(sessionNum, c.Trim(), "").Split(splitOr, StringSplitOptions.RemoveEmptyEntries);

            foreach (string or in condOr)
            {
                pass = true;

                string[] condAnd = or.Trim().Split(splitAnd, StringSplitOptions.RemoveEmptyEntries);
                foreach (string and in condAnd)
                {
                    string[] not = and.ToLower().Split(splitNot, StringSplitOptions.RemoveEmptyEntries);
                    string[] eq = and.ToLower().Split(splitEq, StringSplitOptions.RemoveEmptyEntries);
                    string[] like = and.ToLower().Split(splitLike, StringSplitOptions.RemoveEmptyEntries);
                    string[] match = and.ToLower().Split(splitMatch, StringSplitOptions.RemoveEmptyEntries);
                    string[] less = and.ToLower().Split(splitLT, StringSplitOptions.RemoveEmptyEntries);
                    string[] greater = and.ToLower().Split(splitGT, StringSplitOptions.RemoveEmptyEntries);
                    string[] lessEq = and.ToLower().Split(splitLE, StringSplitOptions.RemoveEmptyEntries);
                    string[] greaterEq = and.ToLower().Split(splitGE, StringSplitOptions.RemoveEmptyEntries);

                    //only one term
                    if (eq.Length == 1 && not.Length == 1 && less.Length == 1 && greater.Length == 1 && lessEq.Length == 1 && greaterEq.Length == 1 && like.Length == 1 && match.Length == 1)
                    {
                        if (eq[0].Trim() == "$false" || eq[0].Trim() == "0") pass = false;
                        break;
                    }

                    //check "like" (wildcards, which are converted to regex)
                    if (like.Length > 1)
                    {
                        string v1 = like[0].Trim();
                        string v2 = like[1].Trim();
                        //Console.WriteLine("Comparing {0} LIKE {1}", v1, v2); //DEBUG
                        string regex = "^" + Regex.Escape(v1).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                        if (like.Length > 1 && !Regex.IsMatch(like[0].Trim(), regex, RegexOptions.IgnoreCase)) pass = false;
                        break;
                    }

                    //check "match" (regex)
                    if (match.Length > 1)
                    {
                        string v1 = match[0].Trim();
                        string v2 = match[1].Trim();
                        bool isMatch = Regex.IsMatch(v1, v2, RegexOptions.IgnoreCase);
                        //Console.WriteLine("Comparing {0} MATCH {1} == {2}", v1, v2, isMatch); //DEBUG
                        if (!isMatch) pass = false;
                        break;
                    }

                    //check ==
                    if (eq.Length > 1)
                    {
                        if (eq[0].Trim() != eq[1].Trim()) pass = false;
                        break;
                    }

                    //check !=
                    if (not.Length > 1)
                    {
                        if (not[0].Trim() == not[1].Trim()) pass = false;
                        break;
                    }

                    int val1;
                    int val2;

                    //check <
                    if (less.Length > 1)
                    {
                        if (!int.TryParse(less[0].Trim(), out val1) || !int.TryParse(less[1].Trim(), out val2) || val1 >= val2) pass = false;
                        break;
                    }

                    //check >
                    if (greater.Length > 1)
                    {
                        if (!int.TryParse(greater[0].Trim(), out val1) || !int.TryParse(greater[1].Trim(), out val2) || val1 <= val2) pass = false;
                        break;
                    }

                    //check <=
                    if (lessEq.Length > 1)
                    {
                        if (!int.TryParse(lessEq[0].Trim(), out val1) || !int.TryParse(lessEq[1].Trim(), out val2) || val1 > val2) pass = false;
                        break;
                    }

                    //check >=
                    if (greaterEq.Length > 1)
                    {
                        if (!int.TryParse(greaterEq[0].Trim(), out val1) || !int.TryParse(greaterEq[1].Trim(), out val2) || val1 < val2) pass = false;
                        break;
                    }

                }

                if (pass) return true; //FIXME - not sure if this is right

            }

            return pass;
        }

        public static string ParseVariables(uint sessionNum, string originalString, string scriptName)
        {
            GhettoSL.UserSession Session = Interface.Sessions[sessionNum];
            string ret = originalString;

            //parse %vars
            if (scriptName != "")
            {
                foreach (KeyValuePair<string, string> var in Interface.Scripts[scriptName].Variables)
                {
                    ret = ret.Replace(var.Key, var.Value);
                }
            }

            //parse $identifiers
            ret = ret.Replace("$null", "");
            ret = ret.Replace("$myname", Session.Name);
            ret = ret.Replace("$master", Session.Settings.MasterID.ToString());
            ret = ret.Replace("$balance", Session.Balance.ToString());
            ret = ret.Replace("$earned", Session.MoneyReceived.ToString());
            ret = ret.Replace("$spent", Session.MoneySpent.ToString());

            if (Session.Client.Network.Connected) ret = ret.Replace("$myid", Session.Client.Network.AgentID.ToString());
            else ret = ret.Replace("$myid", LLUUID.Zero.ToString());

            if (Session.Client.Network.Connected) ret = ret.Replace("$connected", "$true");
            else ret = ret.Replace("$connected", "$false");

            if (Session.Client.Self.Status.Controls.Fly) ret = ret.Replace("$flying", "$true");
            else ret = ret.Replace("$flying", "$false");

            if (Session.Client.Network.Connected) ret = ret.Replace("$region", Session.Client.Network.CurrentSim.Region.Name);
            else ret = ret.Replace("$region", "$null");

            if (Session.Client.Self.SittingOn > 0) ret = ret.Replace("$sitting", "$true");
            else ret = ret.Replace("$sitting", "$false");

            return ret;
        }

        public static string ParseTokens(string originalString, string message)
        {
            string newString = originalString;
            char[] splitChar = { ' ' };
            string[] tok = message.Split(splitChar);
            newString = newString.Replace("$0", "" + tok.Length);
            int i;
            for (i = 0; i < tok.Length; i++) newString = newString.Replace("$"+(i + 1),tok[i]);
            return newString;
        }

        public static bool ParseCommand(uint sessionNum, string scriptName, string commandString, bool parseVariables, bool fromMasterIM)
        {

            //DEBUG - testing scriptName value
            //if (scriptName != "") Console.WriteLine("({0}) [{1}] SCRIPTED COMMAND: {2}", sessionNum, scriptName, commandString);
            //FIXME - change display output if fromMasterIM == true

            if (scriptName != "" && !Interface.Scripts.ContainsKey(scriptName)) return false; //invalid or unloaded script

            GhettoSL.UserSession Session = Interface.Sessions[sessionNum];

            //First we trim up the original command string and then split it by spaces
            string commandToParse = commandString.Trim();
            char[] splitChar = { ' ' };
            string[] cmd = commandToParse.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);

            //The reason for splitting in this step is to save an un-parsed version of the first arg
            //For "inc" and "set" commands, the first arg is a variable name, like "set %var %othervar"
            //Let's say %var and %othervar were 1 and 5. Parsed would be "set 1 5", which is invalid.
            string variableName = null;
            if (cmd.Length > 1 && cmd[1].Substring(0, 1) == "%") variableName = cmd[1];

            //NOW we can parse the variables
            if (parseVariables)
            {
                commandToParse = ParseVariables(sessionNum, commandString, scriptName);
            }

            //Next we split again. This time everthing is parsed.
            cmd = commandToParse.Trim().Split(splitChar);
            string command = cmd[0].ToLower();
            int commandStart = 0;

            //Time to check for IF statements and check for validity and pass/fail
            //If they are invalid, the entire function will return false, halting a parent script.
            //If they are valid but just fail, the command will halt, but it returns true.
            //FIXME - add "else" and multi-line "if/end if" routines
            if (command == "if")
            {
                string conditions = "";
                string[] ifCmd = new string[0];
                for (int i = 1; i < cmd.Length; i++)
                {
                    if (commandStart > 0) //already found THEN statement
                    {
                        Array.Resize(ref ifCmd, ifCmd.Length + 1);
                        ifCmd[ifCmd.Length - 1] = cmd[i];
                    }
                    else if (cmd[i].ToLower() == "then") //this is our THEN statement
                    {
                        if (i >= cmd.Length - 1)
                        {
                            Display.Error(sessionNum, "Script error: Missing statement after THEN");
                            return false;
                        }
                        commandStart = i + 1;
                    }
                    else if (i >= cmd.Length - 1)
                    {
                        Display.Error(sessionNum, "Script error: IF without THEN");
                        return false;
                    }
                    else
                    {
                        if (conditions != "") conditions += " ";
                        conditions += cmd[i];
                    }
                }
                if (!ParseConditions(sessionNum, conditions)) return true; //condition failed, but no errors
                cmd = ifCmd;
                command = cmd[0].ToLower();
            }

            //The purpose of this part is to separate the message from the rest of the command.
            //For example, in the command "im some-uuid-here Hi there!", details = "Hi there!"
            string details = "";
            int detailsStart = 1;
            if (command == "im" || command == "lure" || command == "re" || command == "s" || command == "session" || command == "set") detailsStart++;
            else if (command == "dialog") detailsStart += 2;
            for (; detailsStart < cmd.Length; detailsStart++)
            {
                if (details != "") details += " ";
                details += cmd[detailsStart];                
            }


            //And on to the actual commands...

            if (command == "anim")
            {
                LLUUID anim;
                if (cmd.Length < 2 || !LLUUID.TryParse(cmd[1], out anim)) {
                    Display.Help(command);
                    return false;
                }
                Session.Client.Self.AnimationStart(anim);
            }

            else if (command == "answer")
            {
                int channel = Session.LastDialogChannel;
                LLUUID id = Session.LastDialogID;
                if (cmd.Length < 2) { Display.Help(command); return false; }
                else if (channel < 0 || id == LLUUID.Zero) Display.Error(sessionNum, "No dialogs received. Try /dialog <channel> <id> <message>.");
                else if (ParseCommand(sessionNum, scriptName, "dialog " + channel + " " + id + " " + details, parseVariables, fromMasterIM))
                {
                    Display.InfoResponse(sessionNum, "Dialog reply sent.");
                }
            }

            else if (command == "balance")
            {
                Session.Client.Self.RequestBalance();
            }

            else if (command == "break")
            {
                if (scriptName == "") return false;
                Interface.Scripts[scriptName].SleepTimer.Stop();
                return false;
            }

            else if (command == "camp")
            {
                if (cmd.Length < 2) { Display.Help(command); return false; }
                uint localID = FindObjectByText(sessionNum, details.ToLower());
                if (localID > 0)
                {
                    Display.InfoResponse(sessionNum, "Match found. Camping...");
                    Session.Client.Self.RequestSit(Session.Prims[localID].ID, LLVector3.Zero);
                    Session.Client.Self.Sit();
                    //Session.Client.Self.Status.Controls.FinishAnim = false;
                    Session.Client.Self.Status.Controls.SitOnGround = false;
                    Session.Client.Self.Status.Controls.StandUp = false;
                    Session.Client.Self.Status.SendUpdate();
                    return true;
                }
                Display.InfoResponse(sessionNum, "No matching objects found.");
            }

            else if (command == "clear")
            {
                Console.Clear();
            }

            else if (command == "dialog")
            {
                int channel;
                LLUUID objectid;
                if (cmd.Length <= 3 || !int.TryParse(cmd[1], out channel) || !LLUUID.TryParse(cmd[2], out objectid))
                {
                    Display.Help(command);
                    return false;
                }
                Display.SendMessage(sessionNum, channel, objectid, details);
                SendDialog(sessionNum, channel, objectid, details);
            }

            else if (command == "dir" || command == "ls")
            {
                //FIXME - remember folder and allow dir/ls without args
                if (cmd.Length < 2) { Display.Help(command); return false; }
                DirList(sessionNum, details);
            }

            else if (command == "echo")
            {
                //FIXME - move to Display.Echo
                if (cmd.Length < 1) return true;
                Console.WriteLine(details);
            }

            else if (command == "event" || command == "events")
            {
                //FIXME - add -b flag to block events that occur after this one?
                if (cmd.Length == 1) Display.EventList(sessionNum);
                else return ParseEventCommand(sessionNum, cmd, scriptName);
            }

            else if (command == "exit")
            {
                ParseCommand(sessionNum, scriptName, "s -a quit", false, fromMasterIM);
                Interface.Exit = true;
            }

            else if (command == "fly")
            {
                if (Session.Client.Self.Status.Controls.Fly)
                {
                    Display.InfoResponse(sessionNum, "You are already flying.");
                }
                else
                {
                    Display.InfoResponse(sessionNum, "Suddenly, you feel weightless...");
                }
                //Send either way, for good measure
                Session.Client.Self.Status.Controls.Fly = true;
                Session.Client.Self.Status.SendUpdate();
            }

            else if (command == "go")
            {
                int x = 0;
                int y = 0;
                float z = 0f;

                if (cmd.Length < 3 || !int.TryParse(cmd[1], out x) || !int.TryParse(cmd[2], out y))
                {
                    Display.Help(command);
                    return false;
                }

                if (cmd.Length < 4 || !float.TryParse(cmd[4], out z)) z = Session.Client.Self.Position.Z;

                //FIXME - core library returns incorrect RegionHandle, RegionX, and RegionY?
                //ulong regionHandle = Session.Client.Network.CurrentSim.Region.Handle;
                //Console.WriteLine("Client.Network.CurrentSim.Region.Handle: " + regionHandle); //DEBUG
                //int regionX = (int)(regionHandle >> 32);
                //int regionY = (int)(regionHandle & 0xFFFFFFFF);

                Session.Client.Self.AutoPilotLocal(x, y, z);
            }

            else if (command == "goto")
            {
                if (scriptName == "") return false;
                int i = 0;
                foreach (string line in Interface.Scripts[scriptName].Lines)
                {
                    if (line.Trim() == cmd[1] + ":")
                    {
                        Interface.Scripts[scriptName].CurrentStep = i;
                        break;
                    }
                    i++;
                }
            }

            else if (command == "help")
            {
                string topic = "";
                if (cmd.Length > 1) topic = cmd[1];
                Display.Help(topic);
            }

            else if (command == "im")
            {
                LLUUID target;
                if (cmd.Length < 2 || !LLUUID.TryParse(cmd[1], out target))
                {
                    Display.Help(command);
                    return false;
                }
                Session.Client.Self.InstantMessage(target, details);
            }

            else if (command == "land")
            {
                Session.Client.Self.Status.Controls.Fly = false;
                Session.Client.Self.Status.SendUpdate();
            }

            else if (command == "listen")
            {
                Session.Settings.DisplayChat = true;
            }

            else if (command == "login")
            {
                if (!ParseLoginCommand(sessionNum, cmd))
                {
                    Display.InfoResponse(sessionNum, "Invalid login parameters");
                }
            }

            else if (command == "look")
            {
                int countText = 0;

                if (cmd.Length == 1)
                {
                    LLVector3 sunDirection = Session.Client.Grid.SunDirection;
                    string simName = Session.Client.Network.CurrentSim.Region.Name;
                    string weather = Display.RPGWeather(sessionNum, simName, sunDirection);
                    if (simName != "" && Helpers.VecMag(sunDirection) != 0)
                    {
                        Display.InfoResponse(sessionNum, weather);
                    }

                    lock (Session.Prims)
                    {
                        foreach (KeyValuePair<uint, PrimObject> pair in Session.Prims)
                        {
                            if (pair.Value.Text != "") countText++;
                        }
                    }
                    Display.InfoResponse(sessionNum, "There are " + countText + " objects with text nearby.");
                }

                else
                {
                    lock (Session.Prims)
                    {
                        foreach (KeyValuePair<uint, PrimObject> pair in Session.Prims)
                        {
                            if (Regex.IsMatch(pair.Value.Text, details, RegexOptions.IgnoreCase))
                            {
                                //FIXME - move to Display
                                Console.WriteLine(pair.Value.LocalID + " " + pair.Value.ID + " " + pair.Value.Text);
                                countText++;
                            }
                        }
                    }
                    Display.InfoResponse(sessionNum, "There are " + countText + " objects matching your query.");
                }

            }

            else if (command == "lure")
            {
                LLUUID target;
                if (cmd.Length < 2 || !LLUUID.TryParse(cmd[1], out target))
                {
                    Display.Help(command);
                }
                string reason;
                if (cmd.Length > 2) reason = details;
                else reason = "Join me in " + Session.Client.Network.CurrentSim.Region.Name + "!";

                //FIXME - Add teleport lure

            }

            else if (command == "pay")
            {
                LLUUID id;
                int amount;
                if (cmd.Length < 3 || !int.TryParse(cmd[1], out amount) || !LLUUID.TryParse(cmd[2], out id))
                {
                    Display.Help(command);
                    return false;
                }
                Session.Client.Self.GiveMoney(id, amount, "");
            }

            else if (command == "payme")
            {
                int amount;
                if (cmd.Length < 2 || !int.TryParse(cmd[1], out amount))
                {
                    Display.Help(command);
                    return false;
                }
                else if (Session.Settings.MasterID == LLUUID.Zero)
                {
                    Display.Error(sessionNum, "MasterID not defined");
                    return false;
                }
                else
                {
                    Session.Client.Self.GiveMoney(Session.Settings.MasterID, amount, "Payment to master");
                }
            }

            else if (command == "quiet")
            {
                if (cmd.Length == 1 || cmd[1].ToLower() == "on")
                {
                    Session.Settings.DisplayChat = false;
                    Display.InfoResponse(sessionNum, "Hiding chat from objects/avatars.");
                }
                else if (cmd[1].ToLower() == "off")
                {
                    Session.Settings.DisplayChat = true;
                    Display.InfoResponse(sessionNum, "Showing chat from objects/avatars.");
                }
                else Display.Help(command);
            }

            else if (command == "quit")
            {
                Display.InfoResponse(sessionNum, "Disconnecting " + Session.Name + "...");
                Session.Client.Network.Logout();
            }

            else if (command == "re")
            {
                if (cmd.Length == 1)
                {
                    if (Session.IMSessions.Count == 0) Display.InfoResponse(sessionNum, "No active IM sessions");
                    else Display.IMSessions(sessionNum);
                }
                else if (cmd.Length < 3)
                {
                    Display.Help(command);
                    return false;
                }
                else
                {
                    LLUUID match = LLUUID.Zero;
                    foreach (KeyValuePair<LLUUID, GhettoSL.IMSession> pair in Session.IMSessions)
                    {
                        if (pair.Value.Name.Length < cmd[1].Length) continue; //too short to be a match
                        else if (pair.Value.Name.Substring(0, cmd[1].Length).ToLower() == cmd[1].ToLower())
                        {
                            if (match != LLUUID.Zero)
                            {
                                Display.Error(sessionNum, "\"" + cmd[1] + "\" could refer to more than one active IM session.");
                                return false;
                            }
                            match = pair.Key;
                        }
                    }
                    if (match != LLUUID.Zero)
                    {
                        Display.SendMessage(sessionNum, 0, match, details);
                        Session.Client.Self.InstantMessage(match, details, Session.IMSessions[match].IMSessionID);
                    }
                }
            }

            else if (command == "relog")
            {
                if (Session.Client.Network.Connected)
                {
                    //FIXME - Add RelogTimer to UserSession, in place of this
                    Session.Client.Network.Logout();
                    Thread.Sleep(1000);
                }

                Session.Login();
            }

            else if (command == "reseturi")
            {
                Session.Settings.URI = "last";
            }

            else if (command == "rez")
            {
                Console.WriteLine("FIXME");
            }

            else if (command == "ride")
            {
                RideWith(sessionNum, details);
            }

            else if (command == "run")
            {
                Session.Client.Self.SetAlwaysRun(true);
            }

            else if (command == "say")
            {
                Session.Client.Self.Chat(details, 0, MainAvatar.ChatType.Normal);
            }

            else if (command == "script")
            {
                return ParseLoadScriptCommand(sessionNum, cmd);
            }

            else if (command == "s" || command == "session")
            {
                if (cmd.Length > 1)
                {
                    if (cmd[1] == "-a" || cmd[1] == "*")
                    {
                        foreach (KeyValuePair<uint, GhettoSL.UserSession> pair in Interface.Sessions)
                        {
                            ParseCommand(pair.Key, scriptName, details, parseVariables, fromMasterIM);
                        }
                    }
                    uint switchTo;
                    if (!uint.TryParse(cmd[1], out switchTo) || switchTo < 1 || !Interface.Sessions.ContainsKey(switchTo))
                    {
                        Display.Error(sessionNum, "Invalid session number");
                        return false;
                    }
                    else if (cmd.Length == 2)
                    {
                        Interface.CurrentSession = switchTo;
                        string name = Interface.Sessions[switchTo].Name;
                        Display.InfoResponse(switchTo, "Switched to " + name);
                        Console.Title = name + " - GhettoSL";
                    }
                    else
                    {
                        ParseCommand(switchTo, scriptName, details, parseVariables, fromMasterIM);
                    }
                }
                else Display.SessionList();
            }

            else if (command == "inc" && scriptName != "")
            {
                int amount = 1;
                if (cmd.Length < 2 || variableName == null || (cmd.Length > 2 && !int.TryParse(cmd[2], out amount)))
                {
                    Display.Help(command);
                    return false;
                }
                ScriptSystem.UserScript Script = Interface.Scripts[scriptName];
                int value = 0;
                if (Script.Variables.ContainsKey(variableName) && !int.TryParse(Script.Variables[variableName], out value)) return false;
                //FIXME - change in the following code, int + "" to a proper string-to-int conversion
                else if (Script.Variables.ContainsKey(variableName)) Script.Variables[variableName] = "" + (value + amount);
                else Script.Variables.Add(variableName, "" + amount);
                //QUESTION - Right now, inc creates a new %var if the specified one doesn't exist. Should it?
            }

            else if (command == "set" && scriptName != "")
            {
                if (cmd.Length < 2 || variableName == null)
                {
                    Display.Help(command);
                    return false;
                }
                ScriptSystem.UserScript Script = Interface.Scripts[scriptName];
                if (Script.Variables.ContainsKey(variableName)) Script.Variables[variableName] = details;
                else Script.Variables.Add(variableName, details);
            }

            else if (command == "shout")
            {
                Session.Client.Self.Chat(details, 0, MainAvatar.ChatType.Shout);
            }

            else if (command == "sit")
            {
                LLUUID target;
                if (cmd.Length < 2 || LLUUID.TryParse(cmd[1], out target))
                {
                    Display.Help(command);
                    return false;
                }
                Session.Client.Self.Status.Controls.SitOnGround = false;
                Session.Client.Self.Status.Controls.StandUp = false;
                Session.Client.Self.RequestSit(target, LLVector3.Zero);
                Session.Client.Self.Sit();
                Session.Client.Self.Status.SendUpdate();
            }

            else if (command == "sitg")
            {
                Session.Client.Self.Status.Controls.SitOnGround = true;
                Session.Client.Self.Status.SendUpdate();
                Session.Client.Self.Status.Controls.SitOnGround = false;
            }

            else if (command == "sleep")
            {
                float interval;
                if (cmd.Length < 2 || scriptName == "" || !float.TryParse(cmd[1], out interval))
                {
                    Display.Help(command);
                    return false;
                }
                Interface.Scripts[scriptName].Sleep(interval);
                return false; //pause script
            }

            else if (command == "stand")
            {
                Session.Client.Self.Status.Controls.SitOnGround = false;
                Session.Client.Self.Status.Controls.StandUp = true;
                Session.Client.Self.Status.SendUpdate();
                Session.Client.Self.Status.Controls.StandUp = false;
            }

            else if (command == "stats")
            {
                Display.Stats(sessionNum);
            }

            else if (command == "stopanim")
            {
                LLUUID anim;
                if (cmd.Length < 2 || !LLUUID.TryParse(cmd[1], out anim))
                {
                    Display.Help(command);
                    return false;
                }
                Session.Client.Self.AnimationStop(anim);
            }

            else if (command == "teleport")
            {
                if (cmd.Length < 2) { Display.Help(command); return false; }
                string simName;
                LLVector3 tPos;
                if (cmd.Length >= 4)
                {
                    if (cmd.Length > 4)
                    {
                        simName = String.Join(" ", cmd, 1, cmd.Length - 4);
                        tPos.X = float.Parse(cmd[cmd.Length - 3]);
                        tPos.Y = float.Parse(cmd[cmd.Length - 2]);
                        tPos.Z = float.Parse(cmd[cmd.Length - 1]);
                    }
                    else
                    {
                        simName = String.Join(" ", cmd, 1, cmd.Length - 3);
                        tPos.X = float.Parse(cmd[cmd.Length - 2]);
                        tPos.Y = float.Parse(cmd[cmd.Length - 1]);
                        tPos.Z = Session.Client.Self.Position.Z;
                    }

                }
                else
                {
                    simName = details;
                    tPos = new LLVector3(128, 128, 0);
                }

                Display.Teleporting(sessionNum, simName);
                Session.Client.Self.Teleport(simName, tPos);

                return true;
            }

            //FIXME - move to /teleport and just check for ulong
            else if (command == "teleporth")
            {
                ulong handle;
                if (cmd.Length < 2 || !ulong.TryParse(cmd[1], out handle))
                {
                    Display.Help(command);
                    return false;
                }
                Session.Client.Self.Teleport(handle, new LLVector3(128, 128, 0));
            }

            else if (command == "touch")
            {
                LLUUID findID;
                if (cmd.Length < 2 || LLUUID.TryParse(cmd[1], out findID))
                {
                    Display.Help(command);
                    return false;
                }
                lock (Session.Prims)
                {
                    foreach (PrimObject prim in Session.Prims.Values)
                    {
                        if (prim.ID != findID) continue;
                        Session.Client.Self.Touch(prim.LocalID);
                        Display.InfoResponse(sessionNum, "You touch an object...");
                        break;
                    }
                }
                Display.Error(sessionNum, "Object not found");
            }

            else if (command == "touchid")
            {
                if (cmd.Length < 2) { Display.Help(command); return false; }
                uint touchid;
                if (uint.TryParse(cmd[1], out touchid)) Session.Client.Self.Touch(touchid);
            }

            else if (command == "updates")
            {
                if (cmd.Length < 2) { Display.Help(command); return false; }
                string toggle = cmd[1].ToLower();
                if (toggle == "on")
                {
                    Display.InfoResponse(sessionNum, "Update timer ON");
                    Session.Client.Self.Status.UpdateTimer.Start();
                }
                else if (toggle == "off")
                {
                    Display.InfoResponse(sessionNum, "Update timer OFF");
                    Session.Client.Self.Status.UpdateTimer.Stop();
                }
                else { Display.Help(command); return false; }
            }

            else if (command == "walk")
            {
                Session.Client.Self.SetAlwaysRun(false);
            }

            else if (command == "whisper")
            {
                Session.Client.Self.Chat(details, 0, MainAvatar.ChatType.Whisper);
            }

            else if (command == "who")
            {
                Display.Who(sessionNum);
            }

            else
            {
                Display.InfoResponse(0, "Unknown command: " + command.ToUpper());
                return false;
            }

            return true;
        }


        public static uint FindObjectByText(uint sessionID, string textValue)
        {
            uint localID = 0;

            lock (Interface.Sessions[sessionID].Prims)
            {
                foreach (PrimObject prim in Interface.Sessions[sessionID].Prims.Values)
                {
                    int len = textValue.Length;
                    string match = prim.Text.Replace("\n", ""); //Strip newlines
                    if (match.Length < len) continue; //Text is too short to be a match
                    else if (Regex.IsMatch(match.Substring(0, len).ToLower(), textValue, RegexOptions.IgnoreCase))
                    {
                        localID = prim.LocalID;
                        break;
                    }
                }
            }
            return localID;
        }


        public static bool RideWith(uint sessionNum, string name)
        {
            GhettoSL.UserSession Session = Interface.Sessions[sessionNum];

            string findName = name.ToLower();

            foreach (Avatar av in Session.Avatars.SimLocalAvatars().Values)
            {

                if (av.Name.ToLower().Substring(0, findName.Length) == findName)
                {
                    if (av.SittingOn > 0)
                    {
                        //FIXME - request prim info (to get the uuid) if fails
                        if (!Session.Prims.ContainsKey(av.SittingOn))
                        {
                            Display.InfoResponse(sessionNum, "Object info missing");
                            return false;
                        }
                        Display.InfoResponse(sessionNum, "Riding with " + av.Name + ".");
                        Session.Client.Self.RequestSit(Session.Prims[av.SittingOn].ID, LLVector3.Zero);
                        Session.Client.Self.Sit();
                        return true;
                    }
                    else
                    {
                        Console.WriteLine(av.Name + " is not sitting.");
                        return false;
                    }
                }
            }
            return false;
        }

        public static void SendDialog(uint sessionNum, int channel, LLUUID objectid, string message)
        {
            GhettoSL.UserSession Session = Interface.Sessions[sessionNum];
            ScriptDialogReplyPacket reply = new ScriptDialogReplyPacket();
            reply.AgentData.AgentID = Session.Client.Network.AgentID;
            reply.AgentData.SessionID = Session.Client.Network.SessionID;
            reply.Data.ButtonIndex = 0;
            reply.Data.ChatChannel = channel;
            reply.Data.ObjectID = objectid;
            reply.Data.ButtonLabel = Helpers.StringToField(message);
            Session.Client.Network.SendPacket(reply);
        }



    }
}

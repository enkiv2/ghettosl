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

        public class UserScript
        {
            public uint SessionNumber;
            public string ScriptName;

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

            public void Sleep(float seconds)
            {
                SleepingSince = Helpers.GetUnixTime();
                Display.InfoResponse(SessionNumber, "Sleeping " + seconds + " seconds...");
                SleepTimer.Interval = (int)(seconds * 1000);
                SleepTimer.AutoReset = false;
                SleepTimer.Enabled = true;
            }

            public UserScript(uint sessionNum, string scriptFile)
            {
                Aliases = new Dictionary<string, string[]>();
                Events = new Dictionary<EventTypes, ScriptEvent>();
                SessionNumber = sessionNum;
                ScriptName = scriptFile;
                CurrentStep = 0;
                ScriptTime = Helpers.GetUnixTime();
                Variables = new Dictionary<string, string>();
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
            foreach (KeyValuePair<string, ScriptSystem.UserScript> s in Interface.Scripts)
            {
                if (s.Value.Events.ContainsKey(eventType))
                {
                    foreach (String command in s.Value.Events[eventType].Commands)
                    {
                        string c = command;
                        if (identifiers != null)
                        {
                            foreach (KeyValuePair<string, string> pair in identifiers)
                                c = c.Replace(pair.Key, pair.Value);
                        }
                        ParseCommand(sessionNum, s.Value.ScriptName, c, true, false);
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
                        Session.Settings.URI = "uri:" + fromSession.Client.Network.CurrentSim.Name
                            + "&" + (int)fromSession.Client.Self.Position.X
                            + "&" + (int)fromSession.Client.Self.Position.Y
                            + "&" + (int)fromSession.Client.Self.Position.Z;
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

            //FIXME - move this to a callback for login-failed and add login-failed event
            if (!Session.Login())
            {
                Display.Error(1, "Login failed");
            }

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
                if (Interface.Scripts.ContainsKey(cmd[2])) Display.InfoResponse(sessionNum, "No such script loaded. For a list of active scripts, use /scripts.");
                else Interface.Scripts.Remove(cmd[2]);
                Interface.Scripts[cmd[2]] = null;
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
                Display.InfoResponse(0, "Reloading script: " + scriptFile);
                Interface.Scripts[scriptFile] = new UserScript(0, scriptFile);
            }
            else
            {
                //add entry for scriptFile
                Display.InfoResponse(0, "Loading script: " + scriptFile);
                Interface.Scripts.Add(scriptFile, new UserScript(0, scriptFile));
            }
            int loaded = Interface.Scripts[scriptFile].Load(scriptFile);
            if (loaded < 0)
            {
                Display.Error(0, "Error opening " + scriptFile + " (invalid file name or file in use)");
                return false;
            }
            else if (loaded > 0)
            {
                Display.Error(0, "Error on line " + loaded + " of script " + scriptFile);
                return false;
            }
            else
            {
                int a = Interface.Scripts[scriptFile].Aliases.Count;
                int e = Interface.Scripts[scriptFile].Events.Count;
                Display.InfoResponse(0, "Loaded " + a + " alias(es) and " + e + " event(s).");
                return true;
            }
        }

        public static bool ParseConditions(uint sessionNum, string conditions)
        {
            //FIXME - actually parse paren grouping instead of just stripping parens
            //FIXME - possible code injection point
            string c = conditions.Replace("(", "").Replace(")", "");

            bool pass = true;

            string[] splitLike = { " iswm ", " ISWM " };
            string[] splitMatch = { " match ", " MATCH " };
            string[] splitAnd = { " and ", " AND ", "&&" };
            string[] splitOr = { " or ", " OR ", " || " };
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
                    string[] not = and.ToLower().Split(splitNot, StringSplitOptions.None);
                    string[] eq = and.ToLower().Split(splitEq, StringSplitOptions.None);
                    string[] like = and.ToLower().Split(splitLike, StringSplitOptions.None);
                    string[] match = and.ToLower().Split(splitMatch, StringSplitOptions.None);
                    string[] less = and.ToLower().Split(splitLT, StringSplitOptions.None);
                    string[] greater = and.ToLower().Split(splitGT, StringSplitOptions.None);
                    string[] lessEq = and.ToLower().Split(splitLE, StringSplitOptions.None);
                    string[] greaterEq = and.ToLower().Split(splitGE, StringSplitOptions.None);

                    //only one term (like a number or $true or $false)
                    if (eq.Length == 1 && not.Length == 1 && less.Length == 1 && greater.Length == 1 && lessEq.Length == 1 && greaterEq.Length == 1 && like.Length == 1 && match.Length == 1)
                    {
                        if (eq[0].Trim() == "$false" || eq[0].Trim() == "0") pass = false;
                        break;
                    }

                    //check "iswm" (wildcards, which are converted to regex)
                    if (like.Length > 1)
                    {
                        string v1 = like[0].Trim();
                        string v2 = like[1].Trim();
                        if (v1.Length == 0) v1 = "$null";
                        if (v2.Length == 0) v2 = "$null";
                        //Console.WriteLine("Comparing {0} LIKE {1} == {2}", v1, v2); //DEBUG
                        string regex = "^" + Regex.Escape(v2).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                        bool isMatch = Regex.IsMatch(v1, regex, RegexOptions.IgnoreCase);
                        if (!isMatch) pass = false;
                        break;
                    }

                    //check "match" (regex)
                    if (match.Length > 1)
                    {
                        string v1 = match[0].Trim();
                        string v2 = match[1].Trim();
                        try {
                            bool isMatch = Regex.IsMatch(v1, v2, RegexOptions.IgnoreCase);
                            if (!isMatch) pass = false;
                        }
                        catch {
                            Display.Error(sessionNum, "/if: invalid regular expression");
                            return false;
                        }
                        //Console.WriteLine("Comparing {0} MATCH {1} == {2}", v1, v2, isMatch); //DEBUG
                        
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
            ret = ret.Replace("$myfirst", Session.Settings.FirstName);
            ret = ret.Replace("$mylast", Session.Settings.LastName);
            ret = ret.Replace("$myname", Session.Name);
            ret = ret.Replace("$mypos", Display.VectorString(Session.Client.Self.Position));
            ret = ret.Replace("$mypos.x", Session.Client.Self.Position.X.ToString());
            ret = ret.Replace("$mypos.y", Session.Client.Self.Position.Y.ToString());
            ret = ret.Replace("$mypos.z", Session.Client.Self.Position.Z.ToString());
            ret = ret.Replace("$session", Session.SessionNumber.ToString());
            ret = ret.Replace("$master", Session.Settings.MasterID.ToString());
            ret = ret.Replace("$balance", Session.Balance.ToString());
            ret = ret.Replace("$earned", Session.MoneyReceived.ToString());
            ret = ret.Replace("$spent", Session.MoneySpent.ToString());

            if (Session.Client.Network.Connected) ret = ret.Replace("$myid", Session.Client.Network.AgentID.ToString());
            else ret = ret.Replace("$myid", LLUUID.Zero.ToString());

            if (Session.Client.Network.Connected) ret = ret.Replace("$connected", "$true");
            else ret = ret.Replace("$connected", "$false");

            if (Session.Client.Self.Status.AlwaysRun) ret = ret.Replace("$flying", "$true");
            else ret = ret.Replace("$flying", "$false");

            if (Session.Client.Self.Status.Controls.Fly) ret = ret.Replace("$flying", "$true");
            else ret = ret.Replace("$flying", "$false");

            if (Session.Client.Network.Connected) ret = ret.Replace("$region", Session.Client.Network.CurrentSim.Name);
            else ret = ret.Replace("$region", "$null");

            if (Session.Client.Self.SittingOn > 0) ret = ret.Replace("$sitting", "$true");
            else ret = ret.Replace("$sitting", "$false");

            return ret;
        }

        public static string ParseTokens(string originalString, string message)
        {
            string[] splitChar = { " " };
            string[] orig = originalString.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
            string[] tok = message.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);

            //FIXME - parse all $1 $2 etc

            string newString = "";
            foreach (string o in orig)
            {
                int number;
                if (o.Substring(0, 1) == "$" && int.TryParse(o.Substring(1), out number))
                {
                    if (newString != "") newString += " ";
                    if (tok.Length >= number) newString += tok[number - 1];
                }
                else
                {
                    if (newString != "") newString += " ";
                    newString += o;
                }
            }
            return newString;
        }

        public static bool ParseCommand(uint sessionNum, string scriptName, string commandString, bool parseVariables, bool fromMasterIM)
        {

            //DEBUG - testing scriptName value
            //if (scriptName != "") Console.WriteLine("({0}) [{1}] SCRIPTED COMMAND: {2}", sessionNum, scriptName, commandString);
            //FIXME - change display output if fromMasterIM == true

            GhettoSL.UserSession Session = Interface.Sessions[sessionNum];

            if (scriptName != "" && !Interface.Scripts.ContainsKey(scriptName)) return false; //invalid or unloaded script
            
            //First we clean up the original command string, removing whitespace and command slashes
            string commandToParse = commandString.Trim();
            while (commandToParse.Length > 0 && commandToParse.Substring(0, 1) == "/")
            {
                commandToParse = commandToParse.Substring(1).Trim();
                if (commandToParse.Length == 0) return true;
            }

            //Next we split it by spaces
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
                    if (commandStart > 0) //already found THEN statement, adding to command string
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

            //Check for user-defined aliases
            foreach (UserScript s in Interface.Scripts.Values)
            {
                foreach (KeyValuePair<string, string[]> pair in s.Aliases)
                {
                    if (command == pair.Key.ToLower())
                    {
                        foreach (string c in pair.Value)
                        {
                            string ctok = ParseTokens(c, details);
                            if (!ParseCommand(sessionNum, s.ScriptName, ctok, true, fromMasterIM)) break;
                        }
                        return true;
                    }
                }
            }

            //And on to the actual commands...
            if (command == "anim")
            {
                LLUUID anim;
                if (cmd.Length < 2 || !LLUUID.TryParse(cmd[1], out anim))
                {
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

            else if (command == "paybytext")
            {
                int amount;
                if (cmd.Length < 3 || int.TryParse(cmd[1],out amount)) {
                    Display.Help(command);
                    return false;
                }
                uint localID = FindObjectByText(sessionNum, details.ToLower());
                if (!Session.Prims.ContainsKey(localID))
                {
                    Display.Error(sessionNum, "Object info missing for local object ID " + localID);
                    return false; //FIXME - should this return false and stop scripts?
                }
                if (localID > 0)
                {
                    Session.Client.Self.GiveMoney(Session.Prims[localID].ID, amount, "");
                    Display.InfoResponse(sessionNum, "Paid L$" + amount + " to object " + Session.Prims[localID].ID);
                }

            }

            else if (command == "sitbytext")
            {
                if (cmd.Length < 2) { Display.Help(command); return false; }
                uint localID = FindObjectByText(sessionNum, details.ToLower());
                if (localID > 0)
                {
                    Display.InfoResponse(sessionNum, "Match found. Sitting...");
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

            else if (command == "events")
            {
                if (cmd.Length == 1) Display.EventList(sessionNum);
            }

            else if (command == "exit")
            {
                ParseCommand(sessionNum, scriptName, "s -a quit", false, fromMasterIM);
                Interface.Exit = true;
            }

            else if (command == "fly")
            {
                if (cmd.Length == 1 || cmd[1].ToLower() == "on")
                {
                    if (Session.Client.Self.Status.Controls.Fly)
                    {
                        Display.InfoResponse(sessionNum, "You are already flying.");
                    }
                    else
                    {
                        Session.Client.Self.Status.Controls.Fly = true;
                        Display.InfoResponse(sessionNum, "Suddenly, you feel weightless...");
                    }
                }
                else if (cmd[1].ToLower() == "off")
                {
                    if (Session.Client.Self.Status.Controls.Fly)
                    {
                        Session.Client.Self.Status.Controls.Fly = false;
                        Display.InfoResponse(sessionNum, "You drop to the ground.");
                    }
                    else
                    {
                        Display.InfoResponse(sessionNum, "You are not flying.");
                    }
                }
                //Send either way, for good measure                
                Session.Client.Self.Status.SendUpdate();
            }

            else if (command == "go")
            {
                LLVector3 target;
                if (cmd.Length < 2 || !LLVector3.TryParse(details, out target))
                {
                    Display.Help(command);
                    return false;
                }
                Session.Client.Self.AutoPilotLocal((int)target.X, (int)target.Y, target.Z);
            }

            else if (command == "fixme")
            {
                Session.UpdateAppearance();
                Session.Client.Self.Status.Controls.FinishAnim = true;
                Session.Client.Self.Status.SendUpdate();
                Session.Client.Self.Status.Controls.FinishAnim = false;
                Session.Client.Self.Status.SendUpdate();
            }

            else if (command == "groups")
            {
                Display.GroupList(sessionNum);
            }

            else if (command == "roles")
            {
                LLUUID groupID;
                if (cmd.Length < 2 || !LLUUID.TryParse(cmd[1], out groupID))
                {
                    Display.Help(command);
                    return false;
                }
                Session.Client.Groups.BeginGetGroupRoles(groupID, new GroupManager.GroupRolesCallback(GroupRolesHandler));
            }

            else if (command == "groupinvite")
            {
                LLUUID inviteeID;
                LLUUID groupID;
                LLUUID roleID;
                if (cmd.Length < 4 || !LLUUID.TryParse(cmd[1], out inviteeID) || !LLUUID.TryParse(cmd[2], out groupID) || !LLUUID.TryParse(cmd[3], out roleID))
                {
                    Display.Help(command);
                    return false;
                }
                InviteGroupRequestPacket p = new InviteGroupRequestPacket();
                InviteGroupRequestPacket.InviteDataBlock b = new InviteGroupRequestPacket.InviteDataBlock();
                b.InviteeID = inviteeID;
                b.RoleID = roleID;
                p.InviteData[0] = b;
                p.GroupData.GroupID = groupID;
                p.AgentData.AgentID = Session.Client.Network.AgentID;
                p.AgentData.SessionID = Session.Client.Network.SessionID;
                Display.InfoResponse(sessionNum, "Inviting user " + inviteeID + " to group " + groupID + " with the role " + roleID + ".");
                Session.Client.Network.SendPacket(p);
            }

            else if (command == "help")
            {
                string topic = "";
                if (cmd.Length > 1) topic = cmd[1];
                Display.Help(topic);
            }

            else if (command == "http")
            {
                string flag = cmd[1].ToLower();
                if (cmd.Length < 2 || (flag != "on" && flag != "off"))
                {
                    Display.Help(command);
                    return false;
                }
                if (flag == "off")
                {
                    Interface.HTTPServer.DisableServer();
                }
                else if (flag == "on")
                {
                    //FIXME - add port number argument
                    Interface.HTTPServer.Listen(8066);
                }
            }

            else if (command == "im")
            {
                LLUUID target;
                if (cmd.Length < 3 || !LLUUID.TryParse(cmd[1], out target))
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
                    string simName = Session.Client.Network.CurrentSim.Name;
                    string weather = Display.RPGWeather(sessionNum, simName, sunDirection);
                    if (simName != "" && Helpers.VecMag(sunDirection) != 0)
                    {
                        Display.InfoResponse(sessionNum, weather);
                    }

                    lock (Session.Prims)
                    {
                        foreach (KeyValuePair<uint, Primitive> pair in Session.Prims)
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
                        foreach (KeyValuePair<uint, Primitive> pair in Session.Prims)
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
                else reason = "Join me in " + Session.Client.Network.CurrentSim.Name + "!";

                //FIXME - Add teleport lure

            }

            else if (command == "mlook")
            {
                if (cmd.Length < 2 || cmd[1].ToLower() == "on") Session.Client.Self.Status.Controls.Mouselook = true;
                else if (cmd[1].ToLower() == "off") Session.Client.Self.Status.Controls.Mouselook = false;
                else { Display.Error(sessionNum, command); return false; }
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
                else
                {
                    Display.Help(command);
                    return false;
                }
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

            else if (command == "rot" || command == "rotate")
            {
                char[] space = { ' ' };
                string vString = details.Replace("<", "").Replace(">", "").Replace(",", " ");
                string[] v = vString.Split(space, StringSplitOptions.RemoveEmptyEntries);
                float x; float y; float z;
                if (v.Length != 3
                    || !float.TryParse(v[0].Trim(), out x)
                    || !float.TryParse(v[1].Trim(), out y)
                    || !float.TryParse(v[2].Trim(), out z)
                )
                {
                    Display.Help(command);
                    return false;
                }

                Session.Client.Self.Status.Camera.BodyRotation = Helpers.Axis2Rot(new LLVector3(x, y, z));
            }

            else if (command == "run" || command == "running")
            {
                if (cmd.Length == 1 || cmd[1].ToLower() == "on")
                {
                    Session.Client.Self.Status.AlwaysRun = true;
                    Display.InfoResponse(sessionNum, "Running enabled.");
                }
                else if (cmd[1].ToLower() == "off")
                {
                    Session.Client.Self.Status.AlwaysRun = false;
                    Display.InfoResponse(sessionNum, "Running disabled.");
                }
                else
                {
                    Display.Help(command);
                    return false;
                }
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
                        return true;
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
                if (cmd.Length < 2)
                {
                    Display.Help(command);
                    return false;
                }

                //assumed values
                string simName = QuoteArg(cmd, 1);
                LLVector3 target = new LLVector3(128, 128, 0);

                //assumed wrong
                if (cmd.Length > 2)
                {
                    //find what pos the vector starts at and how many tokens it is
                    int start = simName.Split(splitChar).Length + 1;
                    int len = cmd.Length - start;
                    if (len > 0)
                    {
                        string[] v = new string[len];
                        Array.Copy(cmd, start, v, 0, len);
                        if (!LLVector3.TryParse(String.Join(" ", v), out target))
                        {
                            Display.Help(command);
                            return false;
                        }
                    }
                }

                Display.Teleporting(sessionNum, simName);
                Session.Client.Self.Teleport(simName, target);
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
                    foreach (Primitive prim in Session.Prims.Values)
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
                Session.Client.Self.Status.AlwaysRun = false;
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
                foreach (Primitive prim in Interface.Sessions[sessionID].Prims.Values)
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

        public static void GroupRolesHandler(Dictionary<LLUUID, GroupRole> roles)
        {
            //FIXME - move to display
            foreach (GroupRole role in roles.Values)
            {
                Console.WriteLine(role.ID + " " + role.Name + " \"" + role.Title + "\"");
            }
        }

    }
}

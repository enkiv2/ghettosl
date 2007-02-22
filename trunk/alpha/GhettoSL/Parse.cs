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
using libsecondlife.InventorySystem;
using libsecondlife.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ghetto
{
    class Parse
    {

        public static bool Conditions(uint sessionNum, string conditions)
        {
            //FIXME - actually parse paren grouping instead of just stripping parens
            //FIXME - possible code injection point
            string[] splitSpace = { " " };
            string c = String.Join(" ", conditions.Replace("(", "").Replace(")", "").Trim().Split(splitSpace, StringSplitOptions.RemoveEmptyEntries));

            bool pass = true;

            string[] splitLike = { " iswm ", " ISWM " };
            string[] splitMatch = { " match ", " MATCH " };
            string[] splitAnd = { " and ", " AND ", "&&" };
            string[] splitOr = { " or ", " OR ", " || " };
            string[] splitEq = { " == ", " = " };
            string[] splitNot = { " != ", " <> " };
            string[] splitLT = { " < " };
            string[] splitGT = { " > " };
            string[] splitLE = { " <= ", " =< " };
            string[] splitGE = { " >= ", " => " };

            string[] condOr = Variables(sessionNum, c.Trim(), "").Split(splitOr, StringSplitOptions.RemoveEmptyEntries);

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
                        string val = eq[0].Trim();
                        if (val == "$false" || val == "$null" || val == "0") { pass = false; break; }
                        continue;
                    }

                    //check "iswm" (wildcards, which are converted to regex)
                    if (like.Length > 1)
                    {
                        string v1 = like[0].Trim();
                        string v2 = like[1].Trim();
                        if (v1.Length == 0) v1 = "$null";
                        if (v2.Length == 0) v2 = "$null";
                        string regex = "^" + Regex.Escape(v2).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                        bool isMatch = Regex.IsMatch(v1, regex, RegexOptions.IgnoreCase);
                        Console.WriteLine("Comparing {0} ISWM {1} == {2}", v1, v2, isMatch); //DEBUG
                        if (!isMatch) { pass = false; break; }
                        continue;
                    }

                    //check "match" (regex)
                    if (match.Length > 1)
                    {
                        string v1 = match[0].Trim();
                        string v2 = match[1].Trim();
                        try
                        {
                            bool isMatch = Regex.IsMatch(v1, v2, RegexOptions.IgnoreCase);
                            if (!isMatch) { pass = false; break; }
                        }
                        catch
                        {
                            Display.Error(sessionNum, "/if: invalid regular expression");
                            return false;
                        }
                        //Console.WriteLine("Comparing {0} MATCH {1} == {2}", v1, v2, isMatch); //DEBUG

                        continue;
                    }

                    //check ==
                    if (eq.Length > 1)
                    {
                        string v1 = eq[0].Trim();
                        string v2 = eq[1].Trim();
                        //Console.WriteLine("comparing ==: " + v1 + " vs. " + v2); //DEBUG
                        if (v1 != v2) { pass = false; break; }
                        continue;
                    }

                    //check !=
                    if (not.Length > 1)
                    {
                        string v1 = not[0].Trim();
                        string v2 = not[1].Trim();
                        //Console.WriteLine("comparing !=: " + v1 + " vs. " + v2); //DEBUG
                        if (v1 == v2) { pass = false; break; }
                        continue;
                    }

                    float val1;
                    float val2;

                    //check <
                    if (less.Length > 1)
                    {
                        if (!float.TryParse(less[0].Trim(), out val1) || !float.TryParse(less[1].Trim(), out val2) || val1 >= val2) { pass = false; break; }
                        continue;
                    }

                    //check >
                    if (greater.Length > 1)
                    {
                        if (!float.TryParse(greater[0].Trim(), out val1) || !float.TryParse(greater[1].Trim(), out val2) || val1 <= val2) { pass = false; break; }
                        continue;
                    }

                    //check <=
                    if (lessEq.Length > 1)
                    {
                        if (!float.TryParse(lessEq[0].Trim(), out val1) || !float.TryParse(lessEq[1].Trim(), out val2) || val1 > val2) { pass = false; break; }
                        continue;
                    }

                    //check >=
                    if (greaterEq.Length > 1)
                    {
                        if (!float.TryParse(greaterEq[0].Trim(), out val1) || !float.TryParse(greaterEq[1].Trim(), out val2) || val1 < val2) { pass = false; break; }
                        continue;
                    }

                }

                if (pass) return true; //FIXME - not sure if this is right

            }

            return pass;
        }


        public static string Variables(uint sessionNum, string originalString, string scriptName)
        {
            GhettoSL.UserSession Session = Interface.Sessions[sessionNum];
            string ret = originalString;

            //parse $identifiers
            ret = ret.Replace("$nullkey", LLUUID.Zero.ToString());
            ret = ret.Replace("$myfirst", Session.Settings.FirstName);
            ret = ret.Replace("$mylast", Session.Settings.LastName);
            ret = ret.Replace("$myname", Session.Name);
            ret = ret.Replace("$mypos", Session.Client.Self.Position.ToString());
            ret = ret.Replace("$mypos.x", Session.Client.Self.Position.X.ToString());
            ret = ret.Replace("$mypos.y", Session.Client.Self.Position.Y.ToString());
            ret = ret.Replace("$mypos.z", Session.Client.Self.Position.Z.ToString());
            ret = ret.Replace("$session", Session.SessionNumber.ToString());
            ret = ret.Replace("$master", Session.Settings.MasterID.ToString());
            ret = ret.Replace("$balance", Session.Balance.ToString());
            ret = ret.Replace("$earned", Session.MoneyReceived.ToString());
            ret = ret.Replace("$spent", Session.MoneySpent.ToString());

            if (scriptName != "")
            {
                ScriptSystem.UserScript script = Interface.Scripts[scriptName];
                uint elapsed = Helpers.GetUnixTime() - script.SetTime;
                ret = ret.Replace("$elapsed", elapsed.ToString());
                uint sittingOn = Session.Client.Self.SittingOn;
                LLVector3 myPos;
                if (sittingOn > 0 && Session.Prims.ContainsKey(sittingOn))
                {
                    myPos = Session.Prims[sittingOn].Position + Session.Client.Self.Position;
                }
                else myPos = Session.Client.Self.Position;
                ret = ret.Replace("$target", script.SetTarget.ToString());
                ret = ret.Replace("$distance", Helpers.VecDist(myPos, script.SetTarget).ToString());
            }

            if (Session.Client.Network.Connected && Session.Client.Self.SittingOn > 0 && Session.Prims.ContainsKey(Session.Client.Self.SittingOn))
            {
                ret = ret.Replace("$seattext", Session.Prims[Session.Client.Self.SittingOn].Text);
                ret = ret.Replace("$seatid", Session.Prims[Session.Client.Self.SittingOn].ID.ToString());
            }
            else ret = ret.Replace("$seattext", "$null").Replace("$seatid", LLUUID.Zero.ToString());

            if (Session.Client.Network.Connected)
            {
                ret = ret.Replace("$myid", Session.Client.Network.AgentID.ToString());
                ret = ret.Replace("$connected", "$true");
                ret = ret.Replace("$region", Session.Client.Network.CurrentSim.Name);
            }
            else
            {
                ret = ret.Replace("$myid", LLUUID.Zero.ToString());
                ret = ret.Replace("$connected", "$false");
                ret = ret.Replace("$region", "$null");
                ret = ret.Replace("$distance", "$null");
            }            

            if (Session.Client.Self.Status.AlwaysRun) ret = ret.Replace("$flying", "$true");
            else ret = ret.Replace("$flying", "$false");

            if (Session.Client.Self.Status.Controls.Fly) ret = ret.Replace("$flying", "$true");
            else ret = ret.Replace("$flying", "$false");

            if (Session.Client.Self.SittingOn > 0) ret = ret.Replace("$sitting", "$true");
            else ret = ret.Replace("$sitting", "$false");

            //parse %vars
            if (scriptName != "")
            {
                foreach (KeyValuePair<string, string> var in Interface.Scripts[scriptName].Variables)
                {
                    ret = ret.Replace(var.Key, var.Value);
                }
            }

            ret = ret.Replace("$null", "");
            return ret;
        }


        public static string Tokens(string originalString, string message)
        {
            string[] splitChar = { " " };
            string[] orig = originalString.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
            string[] tok = message.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);

            //FIXME - parse all $1 $2 etc

            string newString = originalString;


            for (int i = 64; i > 0; i--)
            {
                if (tok.Length >= i) newString = newString.Replace("$" + i, tok[i - 1]);
                else newString = newString.Replace("$" + i, "");
                string[] spaceChar = { " " };
                newString = String.Join(" ", newString.Split(spaceChar, StringSplitOptions.RemoveEmptyEntries));
            }

            /*
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
            */

            return newString;
        }


        public static bool LoginCommand(uint sessionNum, string[] cmd)
        {
            GhettoSL.UserSession Session = Interface.Sessions[sessionNum];
            uint newSession = sessionNum;

            if (cmd.Length < 2) { Display.Help("login"); return false; }

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

            for (bool lastArg = false; index < cmd.Length; index++, lastArg = false)
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
        public static bool LoadScriptCommand(uint sessionNum, string[] cmd)
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
                Interface.Scripts[scriptFile] = new ScriptSystem.UserScript(0, scriptFile);
            }
            else
            {
                //add entry for scriptFile
                Display.InfoResponse(0, "Loading script: " + scriptFile);
                Interface.Scripts.Add(scriptFile, new ScriptSystem.UserScript(0, scriptFile));
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
                ScriptSystem.UserScript script = Interface.Scripts[scriptFile];
                int a = script.Aliases.Count;
                int e = script.Events.Count;
                Display.InfoResponse(0, "Loaded " + a + " alias(es) and " + e + " event(s).");
                //ScriptSystem.TriggerEvents(sessionNum, ScriptSystem.EventTypes.Load, null);
                if (script.Events.ContainsKey(ScriptSystem.EventTypes.Load))
                {
                    CommandArray(sessionNum, scriptFile, script.Events[ScriptSystem.EventTypes.Load].Commands, null);
                }
                return true;
            }
        }


        public static void CommandArray(uint sessionNum, string scriptName, string[] commands, Dictionary<string,string> identifiers)
        {
            bool checkElse = false;
            foreach (string command in commands)
            {
                if (command.Substring(0, 1) == ";") continue;

                //FIXME - make sure we don't need to trim command first
                string[] splitSpace = { " " };
                string[] cmd = command.Split(splitSpace, StringSplitOptions.RemoveEmptyEntries);
                string arg = cmd[0].ToLower();

                if (!checkElse)
                {
                    if (arg == "elseif" || arg == "else") continue;
                }

                checkElse = false;

                string newCommand = command; //FIXME - parse tokens and variables

                //Gets $1 $2 $3 etc from $message
                string tokmessage = "";
                if (identifiers != null && identifiers.ContainsKey("$message")) tokmessage = identifiers["$message"];
                else tokmessage = "";
                newCommand = Parse.Tokens(newCommand, tokmessage);

                if (identifiers != null)
                {
                    foreach (KeyValuePair<string, string> pair in identifiers)
                        newCommand = newCommand.Replace(pair.Key, pair.Value);
                }
                ScriptSystem.CommandResult result = Parse.Command(sessionNum, scriptName, newCommand, true, false);
                if (arg == "if" || arg == "elseif")
                {
                    if (result == ScriptSystem.CommandResult.ConditionFailed) checkElse = true;
                    else checkElse = false;
                }
            }
        }

        public static ScriptSystem.CommandResult Command(uint sessionNum, string scriptName, string commandString, bool parseVariables, bool fromMasterIM)
        {

            //DEBUG - testing scriptName value
            //if (scriptName != "") Console.WriteLine("({0}) [{1}] SCRIPTED COMMAND: {2}", sessionNum, scriptName, commandString);
            //FIXME - change display output if fromMasterIM == true

            GhettoSL.UserSession Session = Interface.Sessions[sessionNum];

            if (scriptName != "" && !Interface.Scripts.ContainsKey(scriptName)) return ScriptSystem.CommandResult.UnexpectedError; //invalid or unloaded script

            //First we clean up the original command string, removing whitespace and command slashes
            string commandToParse = commandString.Trim();
            while (commandToParse.Length > 0 && commandToParse.Substring(0, 1) == "/")
            {
                commandToParse = commandToParse.Substring(1).Trim();
                if (commandToParse.Length == 0) return ScriptSystem.CommandResult.NoError;
            }

            //Next we save the unparsed command, split it by spaces, for commands like /set and /inc
            char[] splitChar = { ' ' };
            string[] unparsedCommand = commandToParse.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);

            //Now we parse the variables
            if (parseVariables)
            {
                commandToParse = Variables(sessionNum, commandString, scriptName);
            }

            //Next we split again. This time everthing is parsed.
            string[] cmd = commandToParse.Trim().Split(splitChar);
            string command = cmd[0].ToLower();
            int commandStart = 0;

            //Time to check for IF statements and check for validity and pass/fail
            //If they are invalid, the entire function will return false, halting a parent script.
            //If they are valid but just fail, the command will halt, but it returns true.
            //FIXME - add "else" and multi-line "if/end if" routines
            if (command == "if" || command == "elseif")
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
                            return ScriptSystem.CommandResult.InvalidUsage;
                        }
                        commandStart = i + 1;
                    }
                    else if (i >= cmd.Length - 1)
                    {
                        Display.Error(sessionNum, "Script error: IF without THEN");
                        return ScriptSystem.CommandResult.InvalidUsage;
                    }
                    else
                    {
                        if (conditions != "") conditions += " ";
                        conditions += cmd[i];
                    }
                }
                if (!Conditions(sessionNum, conditions)) return ScriptSystem.CommandResult.ConditionFailed; //condition failed, but no errors
                cmd = ifCmd;
                command = cmd[0].ToLower();
            }
            else if (command == "else")
            {
                string c = "";
                for (int i = 1; i < cmd.Length; i++)
                {
                    if (c != "") c += " ";
                    c +=  cmd[i];
                }
                cmd = c.Split(splitChar);
                if (cmd.Length > 0) command = cmd[0];
                else return ScriptSystem.CommandResult.InvalidUsage;
            }

            //The purpose of this part is to separate the message from the rest of the command.
            //For example, in the command "im some-uuid-here Hi there!", details = "Hi there!"
            string details = "";
            int detailsStart = 1;
            if (command == "cam" || command == "im" || command == "lure" || command == "re" || command == "s" || command == "session" || command == "paybytext" || command == "paybyname") detailsStart++;
            else if (command == "dialog") detailsStart += 2;
            else if (command == "timer" && cmd.Length > 1)
            {
                if (cmd.Length > 1)
                {
                    if (cmd[1].Substring(0, 1) == "-") detailsStart += 4;
                    else detailsStart += 3;
                }
            }
            while (detailsStart < cmd.Length)
            {
                if (details != "") details += " ";
                details += cmd[detailsStart];
                detailsStart++;
            }
            
            //Check for user-defined aliases
            lock (Interface.Scripts)
            {
                foreach (ScriptSystem.UserScript s in Interface.Scripts.Values)
                {
                    foreach (KeyValuePair<string, string[]> pair in s.Aliases)
                    {
                        if (command == pair.Key.ToLower())
                        {
                            //ScriptSystem.CommandResult result = ScriptSystem.CommandResult.NoError;

                            Dictionary<string, string> identifiers = new Dictionary<string, string>();
                            identifiers.Add("$message", details);
                            Parse.CommandArray(sessionNum, s.ScriptName, pair.Value, identifiers);

                            //foreach (string c in pair.Value)
                            //{
                            //    string ctok = Tokens(c, details);
                            //    ScriptSystem.CommandResult aResult = Command(sessionNum, s.ScriptName, ctok, true, fromMasterIM);
                            //    if (aResult != ScriptSystem.CommandResult.NoError && aResult != ScriptSystem.CommandResult.ConditionFailed) return result;
                            //}

                            return ScriptSystem.CommandResult.NoError; //FIXME - make Parse.CommandArray return a CommandResult
                        }
                    }
                }
            }

            if (!Session.Client.Network.Connected)
            {
                string[] okIfNotConnected = { "echo", "exit", "login", "s", "session", "sessions", "script", "stats", "timer" };
                int ok;
                for (ok = 0; ok < okIfNotConnected.Length; ok++)
                {
                    if (okIfNotConnected[ok] == command) break;
                }
                if (ok == okIfNotConnected.Length)
                {
                    Display.Error(sessionNum, "/" + command + " Not connected");
                    return ScriptSystem.CommandResult.UnexpectedError;
                }
            }

            //Check for "/1 text" for channel 1, etc

            int chatChannel;
            if (int.TryParse(cmd[0], out chatChannel))
            {
                Session.Client.Self.Chat(details, chatChannel, MainAvatar.ChatType.Normal);
                Display.SendMessage(sessionNum, chatChannel, LLUUID.Zero, details);
            }

            //And on to the actual commands...

            else if (command == "anim")
            {
                LLUUID anim;
                if (cmd.Length < 2 || !LLUUID.TryParse(cmd[1], out anim))
                {
                    Display.Help(command);
                    return ScriptSystem.CommandResult.InvalidUsage;
                }
                Session.Client.Self.AnimationStart(anim);
            }

            else if (command == "answer")
            {
                int channel = Session.LastDialogChannel;
                LLUUID id = Session.LastDialogID;
                if (cmd.Length < 2) { Display.Help(command); return ScriptSystem.CommandResult.InvalidUsage; }
                else if (channel < 0 || id == LLUUID.Zero) Display.Error(sessionNum, "No dialogs received. Try /dialog <channel> <id> <message>.");
                else if (Command(sessionNum, scriptName, "dialog " + channel + " " + id + " " + details, parseVariables, fromMasterIM) == ScriptSystem.CommandResult.NoError)
                {
                    Display.InfoResponse(sessionNum, "Dialog reply sent.");
                }
            }

            else if (command == "balance")
            {
                Session.Client.Self.RequestBalance();
            }

            else if (command == "detachall")
            {
                //FIXME - detach all worn objects
                List<uint> attachments = new List<uint>();
                foreach (KeyValuePair<uint, Primitive> pair in Session.Prims)
                {
                    if (pair.Value.ParentID == Session.Client.Self.LocalID) attachments.Add(pair.Value.LocalID);
                }
                Session.Client.Objects.DetachObjects(Session.Client.Network.CurrentSim, attachments);
                Display.InfoResponse(sessionNum, "Detached " + attachments.Count + " objects");                                
            }

            else if (command == "return")
            {
                return ScriptSystem.CommandResult.Return;
            }

            else if (command == "paybytext" || command == "paybyname")
            {
                int amount;
                if (cmd.Length < 3 || !int.TryParse(cmd[1], out amount))
                {
                    Display.Help(command);
                    return ScriptSystem.CommandResult.InvalidUsage;
                }
                uint localID = 0;
                LLUUID uuid = LLUUID.Zero;

                Console.WriteLine(command + " " + details);

                if (command == "paybytext")
                {
                    localID = Session.FindObjectByText(details.ToLower());
                    if (!Session.Prims.ContainsKey(localID))
                    {
                        Display.Error(sessionNum, "Missing info for local ID " + localID);
                        return ScriptSystem.CommandResult.UnexpectedError; //FIXME - should this return false and stop scripts?
                    }
                    uuid = Session.Prims[localID].ID;
                }
                else if (command == "paybyname")
                {
                    localID = Session.FindAgentByName(details.ToLower());
                    if (!Session.Avatars.ContainsKey(localID))
                    {
                        Display.Error(sessionNum, "Missing info for local ID " + localID);
                        return ScriptSystem.CommandResult.UnexpectedError; //FIXME - should this return false and stop scripts?
                    }
                    uuid = Session.Avatars[localID].ID;
                }
                else return ScriptSystem.CommandResult.UnexpectedError; //this should never happen

                if (localID > 0)
                {
                    Session.Client.Self.GiveMoney(uuid, amount, "");
                    Display.InfoResponse(sessionNum, "Paid L$" + amount + " to " + uuid);
                }

            }

            else if (command == "setmaster")
            {
                LLUUID master;
                if (cmd.Length != 2 || LLUUID.TryParse(cmd[1], out master)) return ScriptSystem.CommandResult.InvalidUsage;
                Session.Settings.MasterID = master;
                Display.InfoResponse(sessionNum, "Set master to " + cmd[1]);
            }

            else if (command == "settarget" && scriptName != "")
            {
                LLVector3 target;
                if (cmd.Length < 2 || !LLVector3.TryParse(details, out target))
                {
                    Display.Help(command);
                    return ScriptSystem.CommandResult.InvalidUsage;
                }
                else Interface.Scripts[scriptName].SetTarget = target;
            }

            else if (command == "settime" && scriptName != "")
            {
                Interface.Scripts[scriptName].SetTime = Helpers.GetUnixTime();
            }

            else if (command == "sitbytext")
            {
                if (cmd.Length < 2) { Display.Help(command); return ScriptSystem.CommandResult.InvalidUsage; }
                uint localID = Session.FindObjectByText(details.ToLower());
                if (localID > 0)
                {
                    Display.InfoResponse(sessionNum, "Match found. Sitting...");
                    Session.Client.Self.RequestSit(Session.Prims[localID].ID, LLVector3.Zero);
                    Session.Client.Self.Sit();
                    //Session.Client.Self.Status.Controls.FinishAnim = false;
                    Session.Client.Self.Status.Controls.SitOnGround = false;
                    Session.Client.Self.Status.Controls.StandUp = false;
                    Session.Client.Self.Status.SendUpdate();
                }
                else Display.InfoResponse(sessionNum, "No matching objects found.");
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
                    return ScriptSystem.CommandResult.InvalidUsage;
                }
                Display.SendMessage(sessionNum, channel, objectid, details);
                Session.ScriptDialogReply(channel, objectid, details);
            }

            else if (command == "dir" || command == "ls")
            {
                //FIXME - remember folder and allow dir/ls without args
                //FIXME - move DirList function to UserSession and move output to Display class
                if (cmd.Length < 2) { Display.Help(command); return ScriptSystem.CommandResult.InvalidUsage; }
                ScriptSystem.DirList(sessionNum, details);
            }

            else if (command == "echo")
            {
                //FIXME - move to Display.Echo
                if (cmd.Length < 1) return ScriptSystem.CommandResult.NoError;
                Console.WriteLine(details);
            }

            else if (command == "events")
            {
                if (cmd.Length == 1) Display.EventList(sessionNum);
            }

            else if (command == "exit")
            {
                Command(sessionNum, scriptName, "s -a quit", false, fromMasterIM);
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

            else if (command == "follow")
            {
                if (cmd.Length < 2) { Display.Help(command); return ScriptSystem.CommandResult.InvalidUsage; }
                if (cmd.Length == 1 || cmd[1].ToLower() == "on")
                {
                    if (Session.FollowName == "")
                    {
                        Display.InfoResponse(sessionNum, "You are not following anyone.");
                    }
                    else
                    {
                        Session.Follow(Session.FollowName);
                    }
                }
                else if (cmd[1].ToLower() == "off")
                {
                    if (Session.FollowTimer.Enabled == true)
                    {
                        Session.FollowTimer.Stop();
                        Display.InfoResponse(sessionNum, "You stopped following " + Session.FollowName + ".");
                        Session.Client.Self.Status.SendUpdate();
                    }
                    else
                    {
                        Display.InfoResponse(sessionNum, "You are not following.");
                    }
                }
                else Session.Follow(details);
            }

            else if (command == "go")
            {
                LLVector3 target;
                if (cmd.Length < 2 || !LLVector3.TryParse(details, out target))
                {
                    Display.Help(command);
                    return ScriptSystem.CommandResult.InvalidUsage;
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

            else if (command == "gc")
            {
                Display.InfoResponse(sessionNum, "Performing garbage collection...");
                GC.Collect();
                GC.WaitForPendingFinalizers();
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
                    return ScriptSystem.CommandResult.InvalidUsage;
                }
                Session.Client.Groups.BeginGetGroupRoles(groupID, new GroupManager.GroupRolesCallback(ScriptSystem.GroupRolesHandler));
            }

            else if (command == "groupinvite")
            {
                LLUUID inviteeID;
                LLUUID groupID;
                LLUUID roleID;
                if (cmd.Length < 4 || !LLUUID.TryParse(cmd[1], out inviteeID) || !LLUUID.TryParse(cmd[2], out groupID) || !LLUUID.TryParse(cmd[3], out roleID))
                {
                    Display.Help(command);
                    return ScriptSystem.CommandResult.InvalidUsage;
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
                    return ScriptSystem.CommandResult.InvalidUsage;
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
                    return ScriptSystem.CommandResult.InvalidUsage;
                }

                if (Session.IMSessions.ContainsKey(target))
                {
                    Session.Client.Self.InstantMessage(target, details, Session.IMSessions[target].IMSessionID);
                }
                else Session.Client.Self.InstantMessage(target, details);
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
                if (!LoginCommand(sessionNum, cmd))
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
                            try
                            {
                                if (Regex.IsMatch(pair.Value.Text, details, RegexOptions.IgnoreCase))
                                {
                                    //FIXME - move to Display
                                    Console.WriteLine(pair.Value.LocalID + " " + pair.Value.ID + " " + pair.Value.Text);
                                    countText++;
                                }
                            }
                            catch
                            {
                                Display.Error(sessionNum, "/look: invalid regular expression");
                                return ScriptSystem.CommandResult.InvalidUsage;
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
                if (cmd.Length < 2 || cmd[1].ToLower() == "on")
                {
                    Session.Client.Self.Status.Controls.Mouselook = true;
                    Display.InfoResponse(sessionNum, "Mouselook enabled");
                }
                else if (cmd[1].ToLower() == "off")
                {
                    Session.Client.Self.Status.Controls.Mouselook = false;
                    Display.InfoResponse(sessionNum, "Mouselook disabled");
                }
                else {
                    Display.Help(command);
                    return ScriptSystem.CommandResult.InvalidUsage;
                }
            }

            else if (command == "shoot")
            {
                bool inMouselook = Session.Client.Self.Status.Controls.Mouselook;
                if (!inMouselook) Session.Client.Self.Status.Controls.Mouselook = true;
                Session.Client.Self.Status.Controls.MLButtonDown = true;
                Session.Client.Self.Status.Controls.FinishAnim = true;
                Session.Client.Self.Status.SendUpdate();
                Session.Client.Self.Status.Controls.MLButtonDown = false;
                Session.Client.Self.Status.Controls.MLButtonUp = true;
                Session.Client.Self.Status.SendUpdate();
                Session.Client.Self.Status.Controls.MLButtonUp = false;
                Session.Client.Self.Status.Controls.FinishAnim = false;
                if (!inMouselook) Session.Client.Self.Status.Controls.Mouselook = false;
                Session.Client.Self.Status.SendUpdate();
            }

            else if (command == "pay")
            {
                LLUUID id;
                int amount;
                if (cmd.Length < 3 || !int.TryParse(cmd[1], out amount) || !LLUUID.TryParse(cmd[2], out id))
                {
                    Display.Help(command);
                    return ScriptSystem.CommandResult.InvalidUsage;
                }
                Session.Client.Self.GiveMoney(id, amount, "");
            }

            else if (command == "payme")
            {
                int amount;
                if (cmd.Length < 2 || !int.TryParse(cmd[1], out amount))
                {
                    Display.Help(command);
                    return ScriptSystem.CommandResult.InvalidUsage;
                }
                else if (Session.Settings.MasterID == LLUUID.Zero)
                {
                    Display.Error(sessionNum, "MasterID not defined");
                    return ScriptSystem.CommandResult.UnexpectedError;
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
                    return ScriptSystem.CommandResult.InvalidUsage;
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
                    return ScriptSystem.CommandResult.InvalidUsage;
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
                                return ScriptSystem.CommandResult.InvalidUsage;
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
                Session.RideWith(details);
            }


            else if (command == "rotto")
            {
                LLVector3 target;
                if (cmd.Length < 2 || !LLVector3.TryParse(details, out target))
                {
                    Display.Help(command);
                    return ScriptSystem.CommandResult.InvalidUsage;
                }
                Session.TurnToward(target);
            }
            else if (command == "brot")
            {
                LLVector3 target;
                if (cmd.Length < 2 || !LLVector3.TryParse(details, out target))
                {
                    Display.Help(command);
                    return ScriptSystem.CommandResult.InvalidUsage;
                }
                Session.Client.Self.Status.Camera.BodyRotation = Helpers.Axis2Rot(target);
                Session.Client.Self.Status.SendUpdate();
            }
            else if (command == "hrot")
            {
                LLVector3 target;
                if (cmd.Length < 2 || !LLVector3.TryParse(details, out target))
                {
                    Display.Help(command);
                    return ScriptSystem.CommandResult.InvalidUsage;
                }
                Session.Client.Self.Status.Camera.HeadRotation = Helpers.Axis2Rot(target);
                Session.Client.Self.Status.SendUpdate();
            }
            else if (command == "cam")
            {
                LLVector3 target;
                if (cmd.Length < 3 || !LLVector3.TryParse(details, out target))
                {
                    Display.Help(command);
                    return ScriptSystem.CommandResult.InvalidUsage;
                }
                string arg = cmd[1].ToLower();
                if (arg != "center" && arg != "left" && arg != "up" && arg != "at")
                {
                    Display.Help(command);
                    return ScriptSystem.CommandResult.InvalidUsage;
                }

                char[] space = { ' ' };
                string vString = details.Replace("<", "").Replace(">", "").Replace(",", " ");
                string[] v = vString.Split(space, StringSplitOptions.RemoveEmptyEntries);

                if (arg == "center") Session.Client.Self.Status.Camera.CameraCenter = target;
                else if (arg == "at") Session.Client.Self.Status.Camera.CameraAtAxis = target;
                else if (arg == "left") Session.Client.Self.Status.Camera.CameraLeftAxis = target;
                else if (arg == "up") Session.Client.Self.Status.Camera.CameraUpAxis = target;

                Session.Client.Self.Status.SendUpdate();
                Display.InfoResponse(sessionNum, "Camera settings updated");
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
                    return ScriptSystem.CommandResult.InvalidUsage;
                }
            }

            else if (command == "say")
            {
                Session.Client.Self.Chat(details, 0, MainAvatar.ChatType.Normal);
            }

            else if (command == "script")
            {
                if (!LoadScriptCommand(sessionNum, cmd)) return ScriptSystem.CommandResult.UnexpectedError;
            }

            else if (command == "s" || command == "session")
            {
                if (cmd.Length > 1)
                {
                    if (cmd[1] == "-a" || cmd[1] == "*")
                    {
                        foreach (KeyValuePair<uint, GhettoSL.UserSession> pair in Interface.Sessions)
                        {
                            Command(pair.Key, scriptName, details, parseVariables, fromMasterIM);
                        }
                        return ScriptSystem.CommandResult.NoError;
                    }
                    uint switchTo;
                    if (!uint.TryParse(cmd[1], out switchTo) || switchTo < 1 || !Interface.Sessions.ContainsKey(switchTo))
                    {
                        Display.Error(sessionNum, "Invalid session number");
                        return ScriptSystem.CommandResult.InvalidUsage;
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
                        Command(switchTo, scriptName, details, parseVariables, fromMasterIM);
                    }
                }
                else Display.SessionList();
            }

            else if (command == "search")
            {
                //FIXME - add group, land, etc searching


            }

            else if (command == "inc" && scriptName != "")
            {
                int amount = 1;
                if (unparsedCommand.Length < 2 || (unparsedCommand.Length > 2 && !int.TryParse(Variables(sessionNum, unparsedCommand[2], scriptName), out amount)))
                {
                    Display.Help(command);
                    return ScriptSystem.CommandResult.InvalidUsage;
                }
                ScriptSystem.UserScript Script = Interface.Scripts[scriptName];
                int value = 0;
                string variableName = unparsedCommand[1];
                if (Script.Variables.ContainsKey(variableName) && !int.TryParse(Script.Variables[variableName], out value)) return ScriptSystem.CommandResult.InvalidUsage;
                //FIXME - change in the following code, int + "" to a proper string-to-int conversion
                else if (Script.Variables.ContainsKey(variableName)) Script.Variables[variableName] = "" + (value + amount);
                else Script.Variables.Add(variableName, "" + amount);
                //QUESTION - Right now, inc creates a new %var if the specified one doesn't exist. Should it?
            }

            else if (command == "set" && scriptName != "")
            {
                if (unparsedCommand.Length < 3 || unparsedCommand[1].Substring(0, 1) != "%")
                {
                    Display.Help(command);
                    return ScriptSystem.CommandResult.InvalidUsage;
                }
                details = "";
                for (int d = 2; d < unparsedCommand.Length; d++)
                {
                    if (details != "") details += " ";
                    details += unparsedCommand[d];
                }
                string variableName = unparsedCommand[1];
                ScriptSystem.UserScript Script = Interface.Scripts[scriptName];
                if (Script.Variables.ContainsKey(variableName)) Script.Variables[variableName] = Variables(sessionNum, details, scriptName);
                else Script.Variables.Add(variableName, Variables(sessionNum, details, scriptName));

            }

            else if (command == "shout")
            {
                Session.Client.Self.Chat(details, 0, MainAvatar.ChatType.Shout);
            }

            else if (command == "sit")
            {
                LLUUID target;
                if (cmd.Length < 2 || !LLUUID.TryParse(cmd[1], out target))
                {
                    Display.Help(command);
                    return ScriptSystem.CommandResult.InvalidUsage;
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

            /*
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
            */

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
                    return ScriptSystem.CommandResult.InvalidUsage;
                }
                Session.Client.Self.AnimationStop(anim);
            }

            else if (command == "teleport" || command == "tp")
            {
                if (!Session.Client.Network.Connected) { Display.Error(sessionNum, "Not connected"); return ScriptSystem.CommandResult.UnexpectedError; }

                if (cmd.Length < 2)
                {
                    Display.Help(command);
                    return ScriptSystem.CommandResult.InvalidUsage;
                }

                //assumed values
                string simName = ScriptSystem.QuoteArg(cmd, 1);
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
                            return ScriptSystem.CommandResult.InvalidUsage;
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
                    return ScriptSystem.CommandResult.InvalidUsage;
                }
                Session.Client.Self.Teleport(handle, new LLVector3(128, 128, 0));
            }

            else if (command == "timer")
            {
                if (cmd.Length < 3)
                {
                    Display.Help(command);
                    return ScriptSystem.CommandResult.InvalidUsage;
                }
                else if (cmd[2] == "off")
                {
                    if (Session.Timers.ContainsKey(cmd[1]))
                    {
                        Session.Timers[cmd[1]].Stop();
                        Session.Timers.Remove(cmd[1]);
                    }
                }
                else
                {
                    string flags = "";
                    int start = 1;
                    if (cmd[1].Substring(0, 1) == "-")
                    {
                        flags = cmd[1];
                        start++;
                    }
                    string name = cmd[start];
                    int repeats;
                    int interval;
                    bool milliseconds = false;
                    foreach (char f in flags.ToCharArray())
                    {
                        if (f == 'm') milliseconds = true;
                    }
                    if (!int.TryParse(cmd[start + 1], out repeats) || !int.TryParse(cmd[start + 2], out interval))
                    {
                        Display.Help(command);
                        return ScriptSystem.CommandResult.InvalidUsage;
                    }
                    string m;
                    if (milliseconds) m = "ms";
                    else m = "sec";
                    Display.InfoResponse(sessionNum, "Timer \"" + name + "\" activated (Repeat: " + repeats + ", Interval: " + interval + m + ")");
                    ScriptSystem.UserTimer timer = new ScriptSystem.UserTimer(sessionNum, name, interval, milliseconds, repeats, details);
                    if (!Session.Timers.ContainsKey(name)) Session.Timers.Add(name, timer);
                    else Session.Timers[name] = timer;
                }
            }

            else if (command == "timers")
            {
                foreach (KeyValuePair<string, ScriptSystem.UserTimer> pair in Session.Timers)
                {
                    //FIXME - move to Display
                    Display.InfoResponse(sessionNum, Display.Pad(pair.Key, 15) + " " + Display.Pad(pair.Value.RepeatsRemaining.ToString(), 3) + " " + pair.Value.Command);
                }
            }

            else if (command == "touch")
            {
                LLUUID findID;
                if (cmd.Length < 2 || LLUUID.TryParse(cmd[1], out findID))
                {
                    Display.Help(command);
                    return ScriptSystem.CommandResult.InvalidUsage;
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
                if (cmd.Length < 2) { Display.Help(command); return ScriptSystem.CommandResult.InvalidUsage; }
                uint touchid;
                if (uint.TryParse(cmd[1], out touchid)) Session.Client.Self.Touch(touchid);
            }

            else if (command == "updates")
            {
                if (cmd.Length < 2) { Display.Help(command); return ScriptSystem.CommandResult.InvalidUsage; }
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
                else { Display.Help(command); return ScriptSystem.CommandResult.InvalidUsage; }
            }

            else if (command == "walk")
            {
                Session.Client.Self.Status.AlwaysRun = false;
            }

            else if (command == "wear")
            {
                LLUUID itemid;
                if (cmd.Length < 2 || !LLUUID.TryParse(cmd[1], out itemid))
                {
                    Display.Help(command);
                    return ScriptSystem.CommandResult.InvalidUsage;
                }

                if (!Session.Inventory.ContainsKey(itemid))
                {
                    Display.Error(sessionNum, "Asset id not found in inventory cache");
                    return ScriptSystem.CommandResult.UnexpectedError;
                }

                InventoryItem item = Session.Inventory[itemid];

                if (cmd.Length > 2)
                {
                    ObjectManager.AttachmentPoint point = ObjectManager.AttachmentPoint.RightHand;
                    string p = cmd[2].ToLower();
                    if (p == "skull") point = ObjectManager.AttachmentPoint.Skull;
                    else if (p == "chest") point = ObjectManager.AttachmentPoint.Chest;
                    else if (p == "lhand") point = ObjectManager.AttachmentPoint.LeftHand;
                    else if (p == "lhip") point = ObjectManager.AttachmentPoint.LeftHip;
                    else if (p == "llleg") point = ObjectManager.AttachmentPoint.LeftLowerLeg;
                    else if (p == "mouth") point = ObjectManager.AttachmentPoint.Mouth;
                    else if (p == "nose") point = ObjectManager.AttachmentPoint.Nose;
                    else if (p == "pelvis") point = ObjectManager.AttachmentPoint.Pelvis;
                    else if (p == "rhand") point = ObjectManager.AttachmentPoint.RightHand;
                    else if (p == "rhip") point = ObjectManager.AttachmentPoint.RightHip;
                    else if (p == "rlleg") point = ObjectManager.AttachmentPoint.RightLowerLeg;
                    else if (p == "spine") point = ObjectManager.AttachmentPoint.Spine;
                    //FIXME - support all other points

                    else
                    {
                        Display.InfoResponse(sessionNum, "Unknown attachment point \"" + p + "\" - using object default");
                        point = ObjectManager.AttachmentPoint.Default;
                    }
                    item.Attach(point);
                }
                else item.Attach();
                Display.InfoResponse(sessionNum, "Attached: " + item.Name + " (" + item.Description + ")");
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
                Display.InfoResponse(sessionNum, "Unknown command: " + command.ToUpper());
                return ScriptSystem.CommandResult.InvalidUsage;
            }

            return ScriptSystem.CommandResult.NoError;
        }


    }
}

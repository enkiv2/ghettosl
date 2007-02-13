
	      ----------------------------------------------------
		
		          +--+
		          ¦  ¦
		 +---------+ ¦      +-------------+
		 ¦         ¦ ¦   +-------+        ¦+-------+
		 ¦  +---+  ¦ ¦   ¦       ++  +----+¦       ¦
		 ¦  ¦   ¦  ¦ +----++---+ ¦¦  ¦ +-+ ¦   .   ¦
		 ¦  +---+  ¦ +--+ ¦+---+ ¦¦  +-+ +--+ ¦¦   ¦
		 +-------+ ¦ ¦  ¦ ¦      ¦¦  +-+ +--+ ¦¦   ¦
		  +-+    ¦ ¦ ¦  ¦ ¦+-----+¦  ¦ ¦ ¦ ¦  ¦¦   ¦
		  ¦ +----+ ¦ ¦  ¦ ¦+-----+¦  ¦ ¦ ¦ ¦  `    ¦ SL
		  +--------+-+  +-+------++--+ +-+ +-------+
		
	      ----------------------------------------------------
		
	           GhettoSL - by root66 A.K.A. Dirty McLean
	               (c)2006-2007 obsoleet industries
              Thanks to mcortez, jhurliman, everyone in the libsl &
                      openSL crews, and Dimentox Travanti.
       
		


CONTENTS
----------------------
1. COMMANDS
2. DEFAULT IDENTIFIERS
3. ALIASES
4. EVENTS
5. COMPARISONS




1. COMMANDS:
---------------------------------------------------------------------------
anim <uuid>             Start the specified animation
answer <text>           Answer the last-received script dialog
clear                   Clear the console display
dir <path>              View inventory folder contents
fly [on|off]            Toggles avatar flying
follow <name|off|on>    Follow the specified avatar, or toggle following
go <x,y,z>              Move to the specified coordinates using autopilot
groups                  Lists information about groups you are in
inc <%var> [amount]     Increases %var by 1 or any other number/%var
land                    Disable flying
listen                  Same as /quiet off
look [text]             Display info about area, or searches by prim text
pay <uuid> <amount>     Pay the specified avatar or object
paybytext <amt> <text>  Pay an object with text matching the specified string
paybytext <amt> <text>  Pay an avatar with a name matching the specified string
payme <amount>          Pay the specified amount to $masterid, if defined
quiet [on|off]          Toggle the display of object/avatar chat
re [name] [message]     List IM sessions or reply by partial name match
relog                   Log out and back in
ride <name>             Sit on the same object as the specified name
roles <groupID>         List role information for the specified group ID
rot <x,y,z>             rotate your avatar's body (Z affects direction)
run [on|off]            Toggles avatar running
session <#> [command]   Switch or send a command to another session
set <%var> <value>      Used in scripts to define or change a variable
script <scriptName>     Execute the specified script file
shout <message>         Shout the specified message to users within 100m
sit <uuid>              Sit on the specified UUID
sitbytext <text>        Find a chair with text matching the specified string
sitg                    Sit on the ground at current location
stand                   Stand while seated on an object or on the ground
stopanim <uuid>         Stop the specified animation
teleport "sim" <x,y,z>  Teleports to the specified destination
timer [-m] <label> [off] [reps] [interval] [command]
touch <uuid>            Touch the specified object
touchid <localID>       Touch the specified object LocalID
updates <on|off>        Toggle AgentUpdate timer (on by default)
walk                    Disable running
wear <itemid> [point]   Attach an object from inventory after /dir
whisper                 Whisper the specified message to users within 5m
who                     List avatars within viewing range



2. DEFAULT IDENTIFIERS:
---------------------------------------------------------------------------
$myfirst .............. Avatar's first name (or name supplied, if not connected)
$mylast ............... Avatar's last name (or name supplied, if not connected)
$myname ............... Avatar's full name (or name supplied, if not connected)
$mypos ................ Avatar's position, in a friendly integer format
$mypos.x .............. Avatar's X position, in decimal format
$mypos.y .............. Avatar's Y position, in decimal format
$mypos.z .............. Avatar's Z position, in decimal format
$myid ................. Avatar UUID (or LLUUID.Zero if not connected)
$master ............... Master UUID (or UUID.Zero if none specified)
$session .............. Returns the session number that it is called from
$connected ............ Returns $true if connected, $false if not
$flying ............... Returns $true if flying/hovering, $false if not
$sitting .............. Returns $true if sitting, $false if not
$region ............... Current region (or $null if not connected)
$balance .............. L$ balance (or -1 if there is a problem retrieving)
$earned ............... L$ earned since session was created
$spent ................ L$ spent since session was created



3. ALIASES
---------------------------------------------------------------------------
Aliases allow you to bind a series of commands to a single command, or to
perform certain actions based on the arguments supplied to it. Arguments
are referenced using token identifiers, such as $1 for the first word.

EXAMPLES:

alias sayhi
{
  echo This is a test. We will say "hi" now.
  say Hi, my name is $myfirst. Let's make out!
}

alias add
{
  set %i $1
  inc %i $2
  echo $1 + $2 = %i
}



4. EVENTS:
---------------------------------------------------------------------------
Event commands are processed by the same means as any other scripted command.
They are not "pre-loaded" since users may wish to alter events on the fly.
Remember, scripts affect all sessions in your client, so be careful with
events. The following event types are supported:

Event Name		Associated Identifiers

Connect ...............
Chat .................. $name $id $message $pos
Disconnect. ...........
GetItem ............... $item $itemid
GetMoney .............. $name $amount
GiveMoney ............. $name $amount
IM .................... $name $id $dialog $message $pos
ScriptDialog .......... $name $id $channel $message
Sit ...................
TeleportFinish ........
Unsit .................

EXAMPLES:

event GetMoney
{
  pay $amount $master
}

event Chat
{
  say $name just said $message. Second word: $2
}



5. COMPARISONS:
---------------------------------------------------------------------------
if (v1 == v2) then <command>     v1 is equal to v2
if (v1 != v2) then <command>     v1 is not equal to v2
if (v1 <> v2) then <command>     v1 is not equal to v2 (same as !=)
if (v1 > v2) then <command>      v1 is greater than v2
if (v1 < v2) then <command>      v1 is less than v2
if (v1 >= v2) then <command>     v1 is greater than or equal to v2
if (v1 <= v2) then <command>     v1 is less than or equal to v2
if (v1 match v2) then <command>  v1 matches regex pattern v2
if (v1 iswm v2) then <command>   v1 matches wildcard pattern v2

NOTES:

- ( and ) are currently optional (this will probably change)
- && and || can be used, although grouping using ( ) does nothing
- Don't get used to "then". This will be replaced with { }
- Wildcard matching may not be perfect. Let me know!

EXAMPLE:

event Load
{
  set %favoriteSim Testville
}

event TeleportFinish
{
  if ($region == %favoriteSim) then celebrate $region
}

alias celebrate
{
  say I am so happy to be in $1!
}

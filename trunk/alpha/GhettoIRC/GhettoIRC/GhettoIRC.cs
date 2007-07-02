using System;
using System.Net;
using System.Net.Sockets;

namespace GhettoIRC
{
    public class IRCClient
    {
        Socket sockIRC;
        IAsyncResult AsyncResult;
        AsyncCallback DataCallback;
        string buffer;

        public delegate void OnIRCReceivedLineCallback(string message);
        public event OnIRCReceivedLineCallback OnReceivedLine;


        public IRCClient()
        {
            buffer = "";
            OnReceivedLine += new OnIRCReceivedLineCallback(ReceivedLine);
        }

        public void Login(string ipAddress, int port, string nickname, string username, string realname)
        {
            try
            {
                IPAddress ip;
                if (!IPAddress.TryParse(ipAddress, out ip))
                {
                    Console.WriteLine("Invalid IP address specified.");
                    return;
                }
                sockIRC = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipEnd = new IPEndPoint(ip, port);
                sockIRC.Connect(ipEnd);
                if (sockIRC.Connected)
                {
                    SendCommand("USER " + username + " 0 0 :" + realname);
                    SendCommand("NICK " + nickname);
                    WaitForData();
                }
            }
            catch (SocketException se)
            {
                Console.WriteLine("Connection failed." + se.Message);
            }
        }

        String GetIP()
        {
            String strHostName = Dns.GetHostName();
            IPHostEntry iphostentry = Dns.GetHostEntry(strHostName);
            String IPStr = "";
            foreach (IPAddress ipaddress in iphostentry.AddressList)
            {
                IPStr = ipaddress.ToString();
                return IPStr;
            }
            return IPStr;
        }

        public void SendCommand(string message)
        {
            try
            {
                byte[] byData = System.Text.Encoding.ASCII.GetBytes(message + "\r\n");
                if (sockIRC != null) sockIRC.Send(byData);
            }
            catch (SocketException se)
            {
                Console.WriteLine(se.Message);
            }
        }

        void WaitForData()
        {
            try
            {
                if (DataCallback == null) DataCallback = new AsyncCallback(OnDataReceived);
                SocketPacket theSocPkt = new SocketPacket();
                theSocPkt.thisSocket = sockIRC;
                AsyncResult = sockIRC.BeginReceive(theSocPkt.dataBuffer, 0, theSocPkt.dataBuffer.Length, SocketFlags.None, DataCallback, theSocPkt);
            }
            catch (SocketException se)
            {
                Console.WriteLine(se.Message);
            }
        }

        class SocketPacket
        {
            public System.Net.Sockets.Socket thisSocket;
            public byte[] dataBuffer = new byte[1];
        }

        void OnDataReceived(IAsyncResult asyn)
        {
            try
            {
                SocketPacket theSockId = (SocketPacket)asyn.AsyncState;
                int iRx = theSockId.thisSocket.EndReceive(asyn);
                char[] chars = new char[iRx + 1];
                System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
                int charLen = d.GetChars(theSockId.dataBuffer, 0, iRx, chars, 0);
                System.String szData = new System.String(chars);
                ReceivedData(szData);
                WaitForData();
            }
            catch (ObjectDisposedException)
            {
                System.Diagnostics.Debugger.Log(0, "1", "\nOnDataReceived: Socket has been closed\n");
            }
            catch (SocketException se)
            {
                Console.WriteLine(se.Message);
            }
        }

        void ReceivedData(string data)
        {
            string[] splitNull = { "\0" };
            string[] line = data.Split(splitNull, StringSplitOptions.None);
            buffer += line[0];
            string[] splitLines = { "\r\n", "\r", "\n" };
            string[] lines = buffer.Split(splitLines, StringSplitOptions.None);
            if (lines.Length > 1)
            {
                int i;
                for (i = 0; i < lines.Length - 1; i++)
                {
                    if (lines[i].Trim().Length > 0) FireOnIRCReceivedLine(lines[i]);
                }
                buffer = lines[i];
            }
        }

        void ReceivedLine(string message)
        {
            Console.WriteLine(message);
        }

        protected void FireOnIRCReceivedLine(string message)
        {
            string[] splitSpace = { " " };
            string[] tokens = message.Split(splitSpace, StringSplitOptions.None);
            if (tokens[0] == "PING" && tokens.Length > 1) SendCommand("PONG " + tokens[1]);
            if (OnReceivedLine != null) OnReceivedLine(message); //trigger callback
        }

    }
}

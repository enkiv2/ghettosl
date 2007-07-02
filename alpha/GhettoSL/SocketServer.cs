/*
 * Thanks to Jayan Nair for posting a socket server tutorial,
 * which is the guts of this class.
*/

using System;
using System.Net;
using System.Net.Sockets;

namespace ghetto
{
    /// <summary>
    /// HTTP Server Class	
    /// </summary>
    public class SocketServer
    {
        const int MAX_CLIENTS = 10;
        const int MAX_QUEUE = 4;
        string IP;

        public AsyncCallback socketCallback;
        private Socket sockListen;
        private Socket[] sockClients = new Socket[MAX_CLIENTS];
        private int clientCount = 0;

        public SocketServer()
        {
            IP = GetIP();
        }

        public bool Listen(int port)
        {
            try
            {
                sockListen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipLocal = new IPEndPoint(IPAddress.Loopback, port);
                sockListen.Bind(ipLocal);
                sockListen.Listen(MAX_QUEUE);
                sockListen.BeginAccept(new AsyncCallback(OnClientConnect), null);
                Console.WriteLine("HTTP: Listening (" + ipLocal + ")");
                return true;
            }

            catch (SocketException se)
            {
                Console.WriteLine("HTTP: " + se.Message);
                return false;
            }
        }

        public void DisableServer()
        {
            Console.WriteLine("HTTP: Server disabled");
            CloseSockets();
        }
        
        public void OnClientConnect(IAsyncResult asyn)
        {
            try
            {
                sockClients[clientCount] = sockListen.EndAccept(asyn);
                WaitForData(sockClients[clientCount]);
                ++clientCount;
                String str = String.Format("HTTP: Client #{0} connected", clientCount);
                sockListen.BeginAccept(new AsyncCallback(OnClientConnect), null);
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("HTTP: Socket has been closed\n");
            }
            catch (SocketException se)
            {
                Console.WriteLine("HTTP ERROR: " + se.Message);
            }

        }
        public class Packet
        {
            public System.Net.Sockets.Socket CurrentSocket;
            public byte[] DataBuffer = new byte[1];
        }

        public void WaitForData(System.Net.Sockets.Socket socket)
        {
            try
            {
                if (socketCallback == null) socketCallback = new AsyncCallback(OnDataReceived);
                Packet packet = new Packet();
                packet.CurrentSocket = socket;
                socket.BeginReceive(packet.DataBuffer, 0, packet.DataBuffer.Length, SocketFlags.None, socketCallback, packet);
            }
            catch (SocketException se)
            {
                Console.WriteLine(se.Message);
            }

        }

        public void OnDataReceived(IAsyncResult asyn)
        {
            try
            {
                Packet socketData = (Packet)asyn.AsyncState;

                int iRx = 0;
                // Complete the BeginReceive() asynchronous call by EndReceive() method
                // which will return the number of characters written to the stream by the client
                iRx = socketData.CurrentSocket.EndReceive(asyn);
                char[] chars = new char[iRx + 1];
                System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
                int charLen = d.GetChars(socketData.DataBuffer, 0, iRx, chars, 0);
                System.String szData = new System.String(chars);

                Console.WriteLine("HTTP RECEIVED: " + szData);

                // Continue the waiting for data on the Socket
                WaitForData(socketData.CurrentSocket);
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

        void Broadcast(byte[] byData)
        {
            try
            {
                for (int i = 0; i < clientCount; i++)
                {
                    if (sockClients[i] != null && sockClients[i].Connected) sockClients[i].Send(byData);
                }

            }
            catch (SocketException se)
            {
                Console.WriteLine(se.Message);
            }
        }

        String GetIP()
        {
            String strHostName = Dns.GetHostName();
            IPHostEntry iphostentry = Dns.GetHostEntry(strHostName);
            //Take the first IP addresses
            String IPStr = "";
            foreach (IPAddress ipaddress in iphostentry.AddressList)
            {
                IPStr = ipaddress.ToString();
                return IPStr;
            }
            return IPStr;
        }

        void CloseSockets()
        {
            if (sockListen != null) sockListen.Close();
            for (int i = 0; i < clientCount; i++)
            {
                if (sockClients[i] != null)
                {
                    sockClients[i].Close();
                    sockClients[i] = null;
                }
            }
        }
    }
}

using System;
using System.Net;
using System.Net.Sockets;

namespace SimpleTCP
{
    public class TCPClient
    {
        private Socket TCPSocket;
        private IAsyncResult Result;
        private AsyncCallback Callback;
        private string buffer;

        public delegate void OnReceiveLineCallback(string line);
        public event OnReceiveLineCallback OnReceiveLine;

        public delegate void OnConnectCallback();
        public event OnConnectCallback OnConnect;

        public delegate void OnConnectFailCallback(SocketException se);
        public event OnConnectFailCallback OnConnectFail;

        public delegate void OnDisconnectedCallback(SocketException se);
        public event OnDisconnectedCallback OnDisconnected;

        class SocketPacket
        {
            public System.Net.Sockets.Socket TCPSocket;
            public byte[] DataBuffer = new byte[1];
        }

        public TCPClient()
        {
            buffer = "";
        }

        public TCPClient(string address, int port)
        {
            buffer = "";
            Connect(address, port);
        }

        public void Connect(string address, int port)
        {
            try
            {
                IPAddress ip;
                if (!IPAddress.TryParse(address, out ip))
                {
                    IPAddress[] ips = Dns.GetHostAddresses(address);
                    ip = ips[0];
                }
                TCPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipEndPoint = new IPEndPoint(ip, port);
                TCPSocket.Connect(ipEndPoint);
                if (TCPSocket.Connected)
                {
                    OnConnect();
                    WaitForData();
                }
            }
            catch (SocketException se)
            {
                OnConnectFail(se);
            }
        }

        public void Disconnect()
        {
            TCPSocket.Disconnect(true);
        }

        public void SendData(byte[] data)
        {
            try
            {
                if (TCPSocket != null && TCPSocket.Connected) TCPSocket.Send(data);
            }
            catch (SocketException se)
            {
                Console.WriteLine(se.Message);
            }
        }

        public void SendLine(string message)
        {
            try
            {
                byte[] byData = System.Text.Encoding.ASCII.GetBytes(message + "\r\n");
                if (TCPSocket != null && TCPSocket.Connected) TCPSocket.Send(byData);
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
                if (Callback == null) Callback = new AsyncCallback(OnDataReceived);
                SocketPacket packet = new SocketPacket();
                packet.TCPSocket = TCPSocket;
                Result = TCPSocket.BeginReceive(packet.DataBuffer, 0, packet.DataBuffer.Length, SocketFlags.None, Callback, packet);
            }
            catch (SocketException se)
            {
                Console.WriteLine(se.Message);
            }
        }

        void ReceiveData(string data)
        {
            if (OnReceiveLine == null) return;

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
                    if (lines[i].Trim().Length > 0) OnReceiveLine(lines[i]);
                }
                buffer = lines[i];
            }
        }

        void OnDataReceived(IAsyncResult asyn)
        {
            try
            {
                SocketPacket packet = (SocketPacket)asyn.AsyncState;
                int end = packet.TCPSocket.EndReceive(asyn);
                char[] chars = new char[end + 1];
                System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
                int charLen = d.GetChars(packet.DataBuffer, 0, end, chars, 0);
                System.String data = new System.String(chars);
                ReceiveData(data);
                WaitForData();
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("WARNING: Socket closed unexpectedly");
            }
            catch (SocketException se)
            {
                if (!TCPSocket.Connected) OnDisconnected(se);
            }
        }

    }
}

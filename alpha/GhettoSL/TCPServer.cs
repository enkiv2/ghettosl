using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace SimpleTCP
{
    public class TCPServer
    {

        private Socket ListenerSocket;
        private int BUFFER_SIZE = 1024;
        private byte[] buffer;
        private bool _Listening;

        public delegate void OnReceiveLineCallback(string line);
        public event OnReceiveLineCallback OnReceiveLine;

        public delegate void OnConnectCallback(EndPoint localEndPoint, EndPoint remoteEndPoint);
        public event OnConnectCallback OnConnect;

        public delegate void OnDisconnectCallback(EndPoint localEndPoint, EndPoint remoteEndPoint);
        public event OnDisconnectCallback OnDisconnect;
        
        /// <summary>
        /// Default TCPServer constructor (requires Listen() to be called)
        /// </summary>
        public TCPServer()
        {
            buffer = new byte[BUFFER_SIZE];
        }

        /// <summary>
        /// Initialize and begin listening on any address at the specified port
        /// </summary>
        /// <param name="port">Port number to listen on for incoming connections</param>
        public TCPServer(int port)
        {
            buffer = new byte[BUFFER_SIZE];
            Listen(IPAddress.Any, port);
        }

        /// <summary>
        /// Initialize and begin listening on the specified address and port number
        /// </summary>
        /// <param name="bindIP">IPAddress to use for incoming connections</param>
        /// <param name="port">Port number to listen on for incoming connections</param>
        public TCPServer(IPAddress bindIP, int port)
        {
            buffer = new byte[BUFFER_SIZE];
            Listen(bindIP, port);
        }

        /// <summary>
        /// Close the listening socket
        /// </summary>
        public void Close()
        {
            ListenerSocket.Close();
            _Listening = false;
        }

        /// <summary>
        /// Begin listening on any address at the specified port
        /// </summary>
        /// <param name="port">Port number to listen on for incoming connections</param>
        public void Listen(int port)
        {
            Listen(IPAddress.Any, port);
        }

        /// <summary>
        /// Begin listening on the specified address and port number
        /// </summary>
        /// <param name="bindIP">IPAddress to use for incoming connections</param>
        /// <param name="port">Port number to listen on for incoming connections</param>
        public void Listen(IPAddress bindIP, int port)
        {
            if (_Listening) ListenerSocket.Close();
            ListenerSocket = new Socket(AddressFamily.InterNetwork,
                          SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ip = new IPEndPoint(bindIP, port);
            ListenerSocket.Bind(ip);
            ListenerSocket.Listen(5);
            ReceiveNextConnection();
            _Listening = true;
        }

        /// <summary>
        /// Returns whether or not ListenerSocket is listening
        /// </summary>
        public bool Listening
        {
            get { return _Listening; }
        }

        private void ReceiveNextConnection()
        {
            ListenerSocket.BeginAccept(new AsyncCallback(ReceiveConnection), ListenerSocket);
        }

        private void ReceiveNextData(Socket sock)
        {
            sock.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, new AsyncCallback(ReceiveData), sock);
        }

        void ReceiveConnection(IAsyncResult result)
        {
            Socket previous = (Socket)result.AsyncState;
            Socket sock = previous.EndAccept(result);
            OnConnect(sock.LocalEndPoint, sock.RemoteEndPoint);
            ReceiveNextData(sock);
            ReceiveNextConnection();
        }

        void ReceiveData(IAsyncResult result)
        {
            Socket socket = (Socket)result.AsyncState;
            int len = socket.EndReceive(result);
            if (len == 0)
            {
                OnDisconnect(socket.LocalEndPoint, socket.RemoteEndPoint);
                socket.Close();
                return;
            }
            string data = Encoding.ASCII.GetString(buffer, 0, len);

            string[] splitLines = { "\r\n", "\r", "\n" };
            string oldbuffer = Encoding.ASCII.GetString(buffer);
            string[] lines = oldbuffer.Split(splitLines, StringSplitOptions.None);
            if (lines.Length > 1)
            {
                int i;
                for (i = 0; i < lines.Length - 1; i++)
                {
                    if (lines[i].Trim().Length > 0) OnReceiveLine(lines[i]);
                }
                //buffer = Encoding.ASCII.GetBytes(lines[i]);
            }

            ReceiveNextData(socket);
        }

    }
}

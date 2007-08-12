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
        private const string LINE_TERMINATOR = "\r\n";
        private byte[] buffer;
        private bool _Listening;

        public delegate void OnReceiveLineCallback(Socket socket, string line);
        public event OnReceiveLineCallback OnReceiveLine;

        public delegate void OnConnectCallback(Socket socket);
        public event OnConnectCallback OnConnect;

        public delegate void OnDisconnectCallback(Socket socket);
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
        /// Send a message to the specified socket
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="message"></param>
        public void SendLine(Socket socket, string message)
        {
            byte[] data = Encoding.ASCII.GetBytes(message + LINE_TERMINATOR);
            socket.BeginSend(data, 0, data.Length, SocketFlags.None,
                  new AsyncCallback(SentLine), socket);
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
            ListenerSocket.BeginAccept(new AsyncCallback(ReceivedConnection), ListenerSocket);
        }

        private void ReceiveNextData(Socket socket)
        {
            if (socket.Connected)
            {
                socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, new AsyncCallback(ReceivedData), socket);
            }
        }

        private void ReceivedConnection(IAsyncResult result)
        {
            Socket previous = (Socket)result.AsyncState;
            Socket socket = previous.EndAccept(result);
            OnConnect(socket);
            ReceiveNextData(socket);
            ReceiveNextConnection();
        }

        private void SentLine(IAsyncResult result)
        {
            Socket socket = (Socket)result.AsyncState;
            int sent = socket.EndSend(result);
        }

        private void ReceivedData(IAsyncResult result)
        {
            Socket socket = (Socket)result.AsyncState;
            int len = socket.EndReceive(result);
            if (len == 0)
            {
                OnDisconnect(socket);
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
                    if (lines[i].Trim().Length > 0) OnReceiveLine(socket, lines[i]);
                }
                //buffer = Encoding.ASCII.GetBytes(lines[i]);
            }

            ReceiveNextData(socket);
        }

    }
}

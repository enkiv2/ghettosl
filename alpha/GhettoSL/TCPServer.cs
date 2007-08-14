using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace SimpleTCP
{
    public class TCPServer
    {

        private const string LINE_TERMINATOR = "\r\n";

        private Socket _ListenerSocket;
        private bool _Listening;

        /// <summary>
        /// Indexed by Socket.Handle
        /// </summary>
        public Dictionary<IntPtr, ClientUser> Clients = new Dictionary<IntPtr, ClientUser>();

        /// <summary>
        /// The dictionary entry for each client's Socket.Handle
        /// </summary>
        public class ClientUser
        {
            private Socket Socket;
            public byte[] Buffer = new byte[1024];
            public DateTime ConnectTime;
            public int BytesReceived;
            public int LinesReceived;

            public ClientUser(Socket socket)
            {
                Socket = socket;
                ConnectTime = DateTime.Now;
            }

            public void Disconnect()
            {
                Socket.Close();
            }

            public bool Connected
            {
                get { return Socket.Connected; }
            }

        }

        public delegate void OnReceiveLineCallback(Socket socket, string line);
        public event OnReceiveLineCallback OnReceiveLine;

        public delegate void OnReceiveDataCallback(Socket socket, string line);
        public event OnReceiveDataCallback OnReceiveData;

        public delegate void OnConnectCallback(Socket socket);
        public event OnConnectCallback OnConnect;

        public delegate void OnDisconnectCallback(Socket socket);
        public event OnDisconnectCallback OnDisconnect;

        /// <summary>
        /// Default TCPServer constructor (requires Listen() to be called)
        /// </summary>
        public TCPServer()
        {

        }

        /// <summary>
        /// Initialize and begin listening on any address at the specified port
        /// </summary>
        /// <param name="port">Port number to listen on for incoming connections</param>
        public TCPServer(int port)
        {
            Listen(IPAddress.Any, port);
        }

        /// <summary>
        /// Initialize and begin listening on the specified address and port number
        /// </summary>
        /// <param name="bindIP">IPAddress to use for incoming connections</param>
        /// <param name="port">Port number to listen on for incoming connections</param>
        public TCPServer(IPAddress bindIP, int port)
        {
            Listen(bindIP, port);
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
            if (_Listening) _ListenerSocket.Close();
            _ListenerSocket = new Socket(AddressFamily.InterNetwork,
                          SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ip = new IPEndPoint(bindIP, port);
            _ListenerSocket.Bind(ip);
            _ListenerSocket.Listen(5);
            ReceiveNextConnection();
            _Listening = true;
        }

        /// <summary>
        /// Close the listening socket
        /// </summary>
        public void StopListening()
        {
            _ListenerSocket.Close();
            _Listening = false;
        }

        /// <summary>
        /// Returns whether or not ListenerSocket is listening
        /// </summary>
        public bool Listening
        {
            get { return _Listening; }
        }

        /// <summary>
        /// Disconnect and remove Clients entry for the specified socket.Handle
        /// </summary>
        /// <param name="handle"></param>
        public void KillUser(IntPtr handle)
        {
            if (Clients[handle].Connected) Clients[handle].Disconnect();
            Clients.Remove(handle);
        }

        /// <summary>
        /// Send a line of text to the specified socket
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
        /// Start waiting for a new connection
        /// </summary>
        private void ReceiveNextConnection()
        {
            _ListenerSocket.BeginAccept(new AsyncCallback(ReceivedConnection), _ListenerSocket);
        }

        /// <summary>
        /// Triggered when a new connection is received
        /// </summary>
        /// <param name="result"></param>
        private void ReceivedConnection(IAsyncResult result)
        {
            Socket previous = (Socket)result.AsyncState;
            Socket socket = previous.EndAccept(result);

            Clients.Add(socket.Handle, new ClientUser(socket));

            if (OnConnect != null) OnConnect(socket);
            ReceiveNextData(socket);
            ReceiveNextConnection();
        }

        /// <summary>
        /// Triggered when text is successfully sent
        /// </summary>
        /// <param name="result"></param>
        private void SentLine(IAsyncResult result)
        {
            Socket socket = (Socket)result.AsyncState;
            int sent;
            if (socket.Connected) sent = socket.EndSend(result);
        }

        /// <summary>
        /// Wait for new data to arrive from the specified socket
        /// </summary>
        /// <param name="socket"></param>
        private void ReceiveNextData(Socket socket)
        {
            if (socket.Connected)
            {
                ClientUser client = Clients[socket.Handle];
                socket.BeginReceive(client.Buffer, 0, client.Buffer.Length, SocketFlags.None, new AsyncCallback(ReceivedData), socket);
            }
        }

        /// <summary>
        /// Triggered when data is received
        /// </summary>
        /// <param name="result"></param>
        private void ReceivedData(IAsyncResult result)
        {
            Socket socket = (Socket)result.AsyncState;
            ClientUser client = Clients[socket.Handle];
            int len = socket.EndReceive(result);
            if (len == 0)
            {
                OnDisconnect(socket);
                Clients.Remove(socket.Handle);
                socket.Close();
                return;
            }
            string data = Encoding.ASCII.GetString(client.Buffer, 0, len);
            if (OnReceiveData != null) OnReceiveData(socket, data);
            client.BytesReceived += client.Buffer.Length;

            string[] splitLines = { "\r\n", "\r", "\n" };
            string oldbuffer = Encoding.ASCII.GetString(client.Buffer);
            string[] lines = oldbuffer.Split(splitLines, StringSplitOptions.None);
            if (lines.Length > 1)
            {
                int i;
                for (i = 0; i < lines.Length - 1; i++)
                {
                    if (OnReceiveLine != null) OnReceiveLine(socket, lines[i]);
                    client.LinesReceived++;
                }
                //FIXME (testing needed): what happens with large packets?
                //client.Buffer = Encoding.ASCII.GetBytes(lines[i]);
            }

            ReceiveNextData(socket);
        }

    }
}

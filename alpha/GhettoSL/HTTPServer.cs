using SimpleTCP;
using System;
using System.Collections.Generic;
using System.Text;

namespace ghetto
{
    class HTTPServer
    {

        public delegate void OnHTTPRequestCallback(string method, string path, string host, string userAgent, string contentType, int contentLength, Dictionary<string, string> GetVars);
        public event OnHTTPRequestCallback OnHTTPRequest;

        public TCPServer Server;
        public Dictionary<IntPtr, PendingRequest> PendingRequests = new Dictionary<IntPtr, PendingRequest>();

        public enum ClientStatus
        {
            New = 0,
            Requesting = 1,
            Posting = 2,
            Complete = 3
        }

        public class PendingRequest
        {
            public ClientStatus Status = ClientStatus.New;
            public string Method = "";
            public string Path = "";
            public string Host = "";
            public string UserAgent = "";
            public string ContentType = "";
            public int ContentLength = 0;
            public Dictionary<string, string> GetVars = new Dictionary<string,string>();
        }

        public enum HTTPErrorCode
        {
            OK = 200,
            BadRequest = 400,
            Forbidden = 403,
            NotFound = 404,
            MethodNotAllowed = 405,
            Error = 500
        }

        public HTTPServer()
        {
            Server = new TCPServer();
            Server.OnReceiveLine += new TCPServer.OnReceiveLineCallback(Server_OnReceiveLine);
            Server.OnDisconnect += new TCPServer.OnDisconnectCallback(Server_OnDisconnect);
        }

        void Server_OnDisconnect(System.Net.Sockets.Socket socket)
        {
            lock (PendingRequests)
            {
                if (PendingRequests.ContainsKey(socket.Handle)) PendingRequests.Remove(socket.Handle);
            }
        }

        void Server_OnReceiveLine(System.Net.Sockets.Socket socket, string line)
        {
            TCPServer.ClientUser client = Server.Clients[socket.Handle];
            string[] splitChar = { " " };
            string[] args = line.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
            if (client.LinesReceived == 0)
            {
                string method = args[0].ToLower();
                if (args.Length < 2 || (method != "get" && method != "post"))
                {
                    //FIXME- add event
                    Server.SendLine(socket, (int)HTTPErrorCode.MethodNotAllowed + " method not allowed");
                    Server.KillUser(socket.Handle);
                }
                else
                {
                    PendingRequest request = new PendingRequest();
                    request.Status = ClientStatus.Requesting;
                    request.Method = args[0];
                    char[] splitPath = { '?' };
                    string[] path = args[1].Split(splitPath);
                    request.Path = path[0];
                    if (path.Length > 0)
                    {
                        char[] splitVars = { '&' };
                        char[] splitVal = { '=' };
                        string[] vars = path[1].Split(splitVars);
                        foreach (string v in vars)
                        {
                            string[] keyval = v.Split(splitVal);
                            if (keyval.Length > 0) request.GetVars.Add(keyval[0], keyval[1]);
                            else request.GetVars.Add(keyval[0], "");
                        }
                    }
                    PendingRequests.Add(socket.Handle, request);
                }
            }
            else if (PendingRequests.ContainsKey(socket.Handle))
            {
                PendingRequest request = PendingRequests[socket.Handle];
                if (args.Length == 0)
                {
                    request.Status = ClientStatus.Complete;
                    OnHTTPRequest(request.Method, request.Path, request.Host, request.UserAgent, request.ContentType, request.ContentLength, request.GetVars);
                }
                else if (args.Length > 1)
                {
                    string param = args[0].ToLower();
                    if (param == "host:") request.Host = args[1];
                    else if (param == "user-agent:") request.UserAgent = args[1];
                    else if (param == "content-type:") request.ContentType = args[1];
                    else if (param == "content-length:") int.TryParse(args[1], out request.ContentLength);
                }
                PendingRequests[socket.Handle] = request;
            }
        }
 
    }
}

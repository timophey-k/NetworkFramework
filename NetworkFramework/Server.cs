using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace NetworkFramework {

    public class Server {
        int IDCounter = 1;
        Socket _socket;

        readonly TSQueue<Msg> _messagesIn = new TSQueue<Msg>();
        readonly HashSet<Connection> _clients = new HashSet<Connection>();
        readonly object _cLock = new object();

        protected virtual bool OnClientConnect(Connection client) {
            return true;
        }
        protected virtual void OnClientDisconnect(Connection client) {

        }
        protected virtual void OnMessage(Msg msg) {

        }
        public virtual void OnClientValidated(Connection client) {

        }

        void SaveConnection(Connection con) {
            lock (_cLock) _clients.Add(con);
        }

        void RemoveConnection(Connection con) {
            lock (_cLock) _clients.Remove(con);
        }

        public void Start(EndPoint endPoint) {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(endPoint);
            _socket.Listen(100);
            WaitForClientConnection();
        }

        void WaitForClientConnection() {
            var ac = new AsyncCallback((ar) => {
                try {
                    Socket srv = (Socket)ar.AsyncState;
                    Socket client = srv.EndAccept(ar);
                    var con = new Connection(true, client, _messagesIn);
                    if (OnClientConnect(con)) {
                        SaveConnection(con);
                        con.ConnectToClient(this, IDCounter++);
                    }
                    else {
                        con.Disconnect();
                    }
                }
                catch {
                    //
                }
                finally {
                    WaitForClientConnection();
                }
            });
            _socket.BeginAccept(ac, _socket);
        }

        public void Stop() {
            _socket?.Close();
        }

        public void MessageAllClients(Msg msg, Connection ignore = null) {
            HashSet<Connection> lost = null;
            lock (_cLock) {
                foreach (var con in _clients) {
                    if (!con.IsConnected) {
                        lost = lost ?? new HashSet<Connection>();
                        lost.Add(con);
                        continue;
                    }
                    if (con == ignore)
                        continue;
                    MessageClientCore(con, msg);
                }
                if (lost != null) {
                    foreach (var con in lost)
                        RemoveClientCore(con);
                    lost.Clear();
                }
            }
        }

        public void MessageClient(Connection con, Msg msg) {
            if (con.IsConnected)
                MessageClientCore(con, msg);
            else
                RemoveClientCore(con);
        }

        void MessageClientCore(Connection con, Msg msg) {
            con.Send(msg);
        }

        void RemoveClientCore(Connection con) {
            OnClientDisconnect(con);
            con.Disconnect();
            RemoveConnection(con);
        }

        public void Update(bool wait = false) {
            if (wait)
                _messagesIn.Wait();
            while (_messagesIn.TryPopFront(out var msg)) {
                OnMessage(msg);
            }
            _messagesIn.Trim();
        }
    }
}

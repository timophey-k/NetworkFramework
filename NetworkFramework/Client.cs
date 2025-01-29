using System.Net;
using System.Net.Sockets;

namespace NetworkFramework {

    public class Client {
        readonly TSQueue<Msg> _messagesIn = new TSQueue<Msg>();
        Connection _connection;

        public bool IsConnected => _connection?.IsConnected ?? false;
        public TSQueue<Msg> Incoming => _messagesIn;

        public bool Connect(EndPoint endPoint) {
            try {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _connection = new Connection(false, socket, _messagesIn);
                _connection.ConnectToServer(endPoint);
            }
            catch {
                _connection = null;
                return false;
            }
            return true;
        }

        public void Disconnect() {
            _connection?.Disconnect();
        }

        public void Send(Msg msg) {
            if (IsConnected)
                _connection?.Send(msg);
        }
    }
}

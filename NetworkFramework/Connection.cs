using System;
using System.Net;
using System.Net.Sockets;

namespace NetworkFramework {

    public class Connection {
        readonly bool _isServer;
        readonly Socket _socket;
        readonly TSQueue<Msg> _messagesIn;
        readonly TSQueue<Msg> _messagesOut;
        readonly Msg _tempMsgIn = -1;

        int _id = -1;
        long _secretQuestion;
        long _secretAnswer;

        public bool IsConnected => _socket.Connected;
        public int ID => _id;

        public Connection(bool isServer, Socket socket, TSQueue<Msg> messagesIn) {
            _isServer = isServer;
            _socket = socket;
            _messagesIn = messagesIn;
            _messagesOut = new TSQueue<Msg>();

            if (isServer) {
                _secretQuestion = DateTime.Now.Ticks;
                _secretAnswer = SolveSecret(_secretQuestion);
            }
        }

        protected virtual long SolveSecret(long input) {
            unchecked { return (input - 69) * 420; }
        }

        public void Disconnect() {
            if (!_socket.Connected)
                return;
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        public void Send(Msg msg) {
            if (_messagesOut.PushBackGetWasEmpty(msg))
                WriteHeader();
        }

        public void ConnectToClient(Server srv, int id) {
            if (!_isServer)
                return;
            if (!_socket.Connected)
                return;
            _id = id;
            WriteValidation();
            ReadValidation(srv);
        }

        public void ConnectToServer(EndPoint endPoint) {
            if (_isServer)
                return;
            var ac = new AsyncCallback((ar) => {
                bool failed = false;
                var s = (Socket)ar.AsyncState;
                try { s.EndConnect(ar); }
                catch { failed = true; }
                if (failed)
                    Disconnect();
                else
                    ReadValidation();
            });
            _socket.BeginConnect(endPoint, ac, _socket);
        }

        void WriteValidation() {
            var buf = Bytes.Get(_secretQuestion);
            var st = new BuffPack(_socket, buf);
            var ac = new AsyncCallback((ar) => {
                var pack = (BuffPack)ar.AsyncState;
                pack.Socket.EndSend(ar);
                if (!_isServer)
                    ReadHeader();
            });
            _socket.BeginSend(st.Buf, 0, st.Buf.Length, SocketFlags.None, ac, st);
        }

        void ReadValidation(Server srv = null) {
            var st = new BuffPack(_socket);
            var ac = new AsyncCallback((ar) => {
                var pack = (BuffPack)ar.AsyncState;
                int bytesCount = pack.Socket.EndReceive(ar);
                if (bytesCount == 0) {
                    Disconnect();
                    return;
                }
                long secret = 0;
                try { secret = Bytes.ToLong(pack.Buf); }
                catch { }
                if (_isServer) {
                    if (secret == _secretAnswer) {
                        srv?.OnClientValidated(this);
                        ReadHeader();
                    }
                    else {
                        Disconnect();
                    }
                }
                else {
                    _secretQuestion = SolveSecret(secret);
                    WriteValidation();
                }
            });
            _socket.BeginReceive(st.Buf, 0, st.Buf.Length, SocketFlags.None, ac, st);
        }

        void WriteHeader() {
            var msg = _messagesOut.Front();
            var buf = msg.GetHeader();
            var st = new BuffPack(_socket, buf);
            var ac = new AsyncCallback((ar) => {
                var pack = (BuffPack)ar.AsyncState;
                var sended = pack.Socket.EndSend(ar);
                if (_messagesOut.Front().Size > 0) {
                    WriteBody();
                }
                else {
                    _messagesOut.PopFront();
                    if (!_messagesOut.IsEmpty())
                        WriteHeader();
                }
            });
            _socket.BeginSend(st.Buf, 0, st.Buf.Length, SocketFlags.None, ac, st);
        }

        void WriteBody() {
            var msg = _messagesOut.Front();
            var st = new BuffPack(_socket, msg.Body);
            var ac = new AsyncCallback((ar) => {
                var pack = (BuffPack)ar.AsyncState;
                pack.Socket.EndSend(ar);
                _messagesOut.PopFront();
                if (!_messagesOut.IsEmpty())
                    WriteHeader();
            });
            _socket.BeginSend(st.Buf, 0, st.Buf.Length, SocketFlags.None, ac, st);
        }

        void ReadHeader() {
            var buf = new byte[_tempMsgIn.HeaderSize];
            var st = new BuffPack(_socket, buf);
            var ac = new AsyncCallback((ar) => {
                var pack = (BuffPack)ar.AsyncState;
                int bytesCount;
                try { bytesCount = pack.Socket.EndReceive(ar); }
                catch { bytesCount = 0; }
                if (bytesCount > 0) {
                    _tempMsgIn.SetHeader(pack.Buf);
                    if (_tempMsgIn.Size > 0) {
                        ReadBody();
                    }
                    else {
                        AddToIncoming();
                    }
                }
                else {
                    Disconnect();
                }
            });
            _socket.BeginReceive(st.Buf, 0, st.Buf.Length, SocketFlags.None, ac, st);
        }

        void ReadBody() {
            var buf = new byte[_tempMsgIn.Size];
            var st = new BuffPack(_socket, buf);
            var ac = new AsyncCallback((ar) => {
                var pack = (BuffPack)ar.AsyncState;
                int bytesCount;
                int targetCount = pack.Buf.Length;
                try { bytesCount = pack.Socket.EndReceive(ar); }
                catch { bytesCount = 0; }

                if (bytesCount == 0) {
                    Disconnect();
                }
                else if (bytesCount == targetCount) {
                    _tempMsgIn.Body = pack.Buf;
                    AddToIncoming();
                }
                else if (bytesCount < targetCount) {
                    _tempMsgIn.Body = new byte[0];
                    AppendToTempBuffer(pack.Buf, bytesCount);
                    ReadBodyContinue(targetCount - bytesCount);
                }
                else {
                    // WTF
                }
            });
            _socket.BeginReceive(st.Buf, 0, st.Buf.Length, SocketFlags.None, ac, st);
        }

        void ReadBodyContinue(int size) {
            var buf = new byte[size];
            var st = new BuffPack(_socket, buf);
            var ac = new AsyncCallback((ar) => {
                var pack = (BuffPack)ar.AsyncState;
                int bytesCount;
                int targetCount = pack.Buf.Length;
                try { bytesCount = pack.Socket.EndReceive(ar); }
                catch { bytesCount = 0; }

                if (bytesCount == 0) {
                    Disconnect();
                }
                else {
                    AppendToTempBuffer(pack.Buf, bytesCount);
                    if (bytesCount == targetCount) {
                        AddToIncoming();
                    }
                    else if (bytesCount < targetCount) {
                        ReadBodyContinue(targetCount - bytesCount);
                    }
                    else {
                        // WTF
                    }
                }
            });
            _socket.BeginReceive(st.Buf, 0, st.Buf.Length, SocketFlags.None, ac, st);
        }

        void AppendToTempBuffer(byte[] data, int appendSize) {
            int insertAt = _tempMsgIn.Body.Length;
            Array.Resize(ref _tempMsgIn.Body, _tempMsgIn.Body.Length + appendSize);
            Array.Copy(data, 0, _tempMsgIn.Body, insertAt, appendSize);
        }

        void AddToIncoming() {
            var msg = _tempMsgIn.Clone();
            _tempMsgIn.Reset();
            if (_isServer) msg.Remote = this;
            _messagesIn.PushBack(msg);
            ReadHeader();
        }
    }
}

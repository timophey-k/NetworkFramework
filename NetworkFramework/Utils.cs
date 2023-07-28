using System;
using System.Net.Sockets;
using System.Text;

namespace NetworkFramework {

    public static class Bytes {
        public static byte[] Get(int i) => BitConverter.GetBytes(i);
        public static byte[] Get(long l) => BitConverter.GetBytes(l);
        public static byte[] Get(float f) => BitConverter.GetBytes(f);
        public static byte[] Get(string s) => Encoding.Unicode.GetBytes(s);

        public static int ToInt(byte[] data, int start = 0) => BitConverter.ToInt32(data, start);
        public static long ToLong(byte[] data, int start = 0) => BitConverter.ToInt64(data, start);
        public static float ToFloat(byte[] data, int start = 0) => BitConverter.ToSingle(data, start);
        public static string ToStr(byte[] data) => Encoding.Unicode.GetString(data);
    }

    class BuffPack {
        public Socket Socket;
        public byte[] Buf;

        public BuffPack(Socket socket, byte[] buf = null) {
            this.Socket = socket;
            this.Buf = buf ?? new byte[1024];
        }
    }
}

using System;

namespace NetworkFramework {

    public class Msg {
        // header
        public int Id;
        public int Size;
        //
        public int HeaderSize => 2 * 4;
        public byte[] Body;
        public Connection Remote;

        public Msg(int id) {
            Id = id;
            Body = new byte[0];
        }

        public byte[] GetHeader() {
            var res = new byte[HeaderSize];
            Array.Copy(Bytes.Get(Id), 0, res, 0, 4);
            Array.Copy(Bytes.Get(Size), 0, res, 4, 4);
            return res;
        }

        public void SetHeader(byte[] data) {
            Id = Bytes.ToInt(data);
            Size = Bytes.ToInt(data, 4);
        }

        public Msg Clone() {
            var res = new Msg(Id);
            if (Body.Length > 0) {
                Array.Resize(ref res.Body, Body.Length);
                Array.Copy(Body, res.Body, Body.Length);
            }
            res.Size = Body.Length;
            return res;
        }

        public void Reset() {
            Id = -1;
            Size = 0;
            Body = new byte[0];
        }

        public void PushBytes(byte[] data) => Push(data.Length, data);
        public void PushInt(int val) => Push(sizeof(int), Bytes.Get(val));
        public void PushFloat(float val) => Push(sizeof(float), Bytes.Get(val));
        public void PushStr(string str) {
            byte[] data = Bytes.Get(str);
            PushBytes(data);
            PushInt(data.Length);
        }

        public byte[] PopBytes(int size) => Pop(size);
        public int PopInt() => Bytes.ToInt(Pop(sizeof(int)), 0);
        public float PopFloat() => Bytes.ToFloat(Pop(sizeof(float)), 0);
        public string PopStr() => Bytes.ToStr(PopBytes(PopInt()));

        void Push(int pushSize, byte[] data) {
            int size = Body.Length;
            Array.Resize(ref Body, size + pushSize);
            Array.Copy(data, 0, Body, size, data.Length);
            Size = Body.Length;
        }

        public byte[] Pop(int popSize) {
            int start = Body.Length - popSize;
            byte[] data = new byte[popSize];
            Array.Copy(Body, start, data, 0, popSize);
            Array.Resize(ref Body, Body.Length - popSize);
            Size = Body.Length;
            return data;
        }
    }
}

using System.Collections.Generic;
using System.Threading;

namespace NetworkFramework {

    public class TSQueue<T> {
        readonly Queue<T> queue = new Queue<T>();
        readonly AutoResetEvent notifier = new AutoResetEvent(false);
        readonly object qlocker = new object();
        readonly object nlocker = new object();

        public bool IsEmpty() {
            return Count() == 0;
        }
        public void Trim() {
            lock (qlocker) queue.TrimExcess();
        }
        public int Count() {
            lock (qlocker) return queue.Count;
        }

        public void PushBack(T obj) {
            lock (qlocker) queue.Enqueue(obj);
            lock (nlocker) notifier.Set();
        }

        public bool PushBackGetWasEmpty(T obj) {
            bool wasEmpty;
            lock (qlocker) {
                queue.Enqueue(obj);
                wasEmpty = Count() == 1;
            }
            lock (nlocker) notifier.Set();
            return wasEmpty;
        }

        public T Front() {
            lock (qlocker) return queue.Peek();
        }

        public T PopFront() {
            lock (qlocker) return queue.Dequeue();
        }

        public void Wait() {
            while (IsEmpty()) {
                notifier.WaitOne();
            }
        }
    }
}

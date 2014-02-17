using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TrionicCANLib.Log
{
    public class LogQueue<T> where T : new()
    {
        private readonly Queue<T> _queue = new Queue<T>();

        public void Enqueue(T item)
        {
            lock (_queue)
            {
                _queue.Enqueue(item);
            }
        }

        public T Dequeue()
        {
            while (_queue.Count == 0)
                Thread.Sleep(1000);
            lock (_queue)
            {
                return _queue.Dequeue();
            }
        }
    }
}

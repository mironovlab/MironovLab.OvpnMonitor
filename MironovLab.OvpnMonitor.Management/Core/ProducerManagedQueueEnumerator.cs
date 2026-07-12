using System;
using System.Collections;
using System.Collections.Generic;

namespace MironovLab.OpenVPN.Management.Core
{
    internal class ProducerManagedQueueEnumerator<T> : IEnumerator<T>
    {
        private readonly BlockingQueue<T> _queue = new BlockingQueue<T>();
        public T Current { get; private set; }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            _queue.Dispose();
        }

        public bool MoveNext()
        {
            if (_queue.TryDequeueWait(out var current))
            {
                Current = current;
                return true;
            }

            Current = default;
            return false;
        }

        public void Reset()
        {
            throw new NotSupportedException(nameof(Reset));
        }

        public void Enqueue(T item)
        {
            _queue.Enqueue(item);
        }
    }
}

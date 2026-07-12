using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace MironovLab.OpenVPN.Management.Core
{
    internal class ConsumerManagedQueueEnumerator<T> : IEnumerator<T>
    {
        private readonly AutoResetEvent _request = new AutoResetEvent(false);
        private readonly AutoResetEvent _response = new AutoResetEvent(false);
        private readonly object _lock = new object();
        private T _object;
        private volatile bool _disposed;
        public T Current { get; private set; }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _request.Set();

                lock (_lock)
                {
                    _request.Dispose();
                    _response.Dispose();
                }
            }
        }

        public bool MoveNext()
        {
            _request.Set();
            _response.WaitOne();
            Current = _object;
            return true;
        }

        public void Reset()
        {
            throw new NotSupportedException(nameof(Reset));
        }

        public void Enqueue(T item)
        {
            _request.Reset();
            _object = item;
            _response.Set();
        }

        public bool WaitForItemRequested()
        {
            lock (_lock)
            {
                if (_disposed)
                    return false;

                _request.WaitOne();
            }
            
            return !_disposed;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;

namespace MironovLab.OpenVPN.Management.Core
{
    public class BlockingQueue<T> : IDisposable
    {
        private readonly Queue<T> _queue = new Queue<T>();
        private readonly SemaphoreSlim _sem = new SemaphoreSlim(0, int.MaxValue);
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private volatile bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _cancellationTokenSource.Cancel();
                lock (_cancellationTokenSource)
                    _cancellationTokenSource.Dispose();
                _sem.Dispose();
            }
        }

        public void Enqueue(T item)
        {
            ThrowIfDisposed();

            lock (_queue)
            {
                _queue.Enqueue(item);
            }

            _sem.Release();
        }

        public T Dequeue()
        {
            lock(_cancellationTokenSource)
            {
                ThrowIfDisposed();
                _sem.Wait(_cancellationTokenSource.Token);
            }

            lock (_queue)
            {
                return _queue.Dequeue();
            }
        }

        public bool TryDequeue(out T item, int millisecondsTimeout)
        {
            item = default;
            if (_disposed || !_sem.Wait(millisecondsTimeout))
                return false;

            lock (_queue)
            {
                item = _queue.Dequeue();
                return true;
            }
        }

        public bool TryDequeueWait(out T item)
        {
            item = default;
            try
            {
                lock (_cancellationTokenSource)
                {
                    if (_disposed)
                    {
                        if (_queue.Count == 0)
                            return false;
                    }
                    else
                    {
                        _sem.Wait(_cancellationTokenSource.Token);
                    }
                }

                lock (_queue)
                {
                    item = _queue.Dequeue();
                    return true;
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().ToString());
        }
    }
}

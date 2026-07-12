using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MironovLab.OpenVPN.Management.Core;

namespace MironovLab.OpenVPN.Management.RealTimeMessages.Parsers
{
    internal abstract class RealTimeMessageParser<T> : IRealTimeMessageParser where T : RealTimeMessage
    {
        private readonly ConsumerManagedQueueEnumerator<string> _enumerator = new ConsumerManagedQueueEnumerator<string>();
        private readonly Task<T> _resultTask;
        private bool _disposed;
        public abstract RealTimeMessageType MessageType { get; }
        public bool ObjectIsReady => _resultTask.IsCompleted;
        public T ParsedMessage => _resultTask.Result;
        public event EventHandler<T> MessageParsed;

        protected RealTimeMessageParser()
        {
            _resultTask = Task.Factory.StartNew(Parse);
            if (!_enumerator.WaitForItemRequested())
                throw new InvalidOperationException($"Parser {GetType()} did not requested any lines");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _enumerator.Dispose();
            }
        }

        public void Parse(string dataLine)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().ToString());

            if (ObjectIsReady)
                throw new InvalidOperationException(Resources.RTMessageParser_AlreadyParsed);

            _enumerator.Enqueue(dataLine);
            if (!_enumerator.WaitForItemRequested())
            {
                var parsedObject = _resultTask.Result; // We need to wait for parsed object result whether we have event handler or not
                MessageParsed?.Invoke(this, parsedObject);
            }
        }

        protected abstract T ParseInternal(IEnumerable<string> lines);

        private T Parse()
        {
            return ParseInternal(new OneTimeEnumerable<string>(_enumerator));
        }
    }
}

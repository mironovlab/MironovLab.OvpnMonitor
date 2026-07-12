using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MironovLab.OpenVPN.Management.Core;
using MironovLab.OpenVPN.Management.Exceptions;

namespace MironovLab.OpenVPN.Management.CommandResults.Parsers
{
    internal abstract class ParserBase<T> : IDisposable
    {
        private readonly ProducerManagedQueueEnumerator<string> _enumerator;
        private readonly Task<T> _task;
        protected ILogger Logger;

        protected ParserBase(ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger(GetType());
            _enumerator = new ProducerManagedQueueEnumerator<string>();
            _task = Task.Factory.StartNew(ParseInternal);
        }

        public void Dispose()
        {
            _enumerator.Dispose();
        }

        public T ParseResult(OvpnDataExchanger reader)
        {
            while (true)
            {
                var line = reader.Read();
                if (line == Constants.EndOfResult)
                {
                    Logger.LogDebug("End of result found, disposing enumerator");
                    _enumerator.Dispose();
                    return _task.GetAwaiter().GetResult();
                }

                ParseUtils.ParseSimpleMessage(line, out var messageType, out var text);
                if (messageType == Constants.MessageTypes.Error)
                {
                    Logger.LogDebug("Error received, disposing enumerator");
                    _enumerator.Dispose();
                    throw new CommandResultError(text);
                }

                _enumerator.Enqueue(line);
            }
        }

        protected abstract T Parse(IEnumerable<string> lines);

        private T ParseInternal()
        {
            return Parse(new OneTimeEnumerable<string>(_enumerator));
        }
    }
}

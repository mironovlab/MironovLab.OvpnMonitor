using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MironovLab.OpenVPN.Management.Core
{
    internal class OvpnDataExchanger : IDisposable
    {
        private const int BufferSize = 4096;
        private const int MaxCharLength = BufferSize / 4;
        private readonly ManualResetEventSlim _dataRead = new ManualResetEventSlim(false);
        private readonly byte[] _readBuffer = new byte[BufferSize];
        private readonly byte[] _writeBuffer = new byte[BufferSize];
        private readonly Queue<string> _strings = new Queue<string>();
        private readonly StringBuilder _sb = new StringBuilder(MaxCharLength);
        private readonly BlockingQueue<string> _outputMessageQueue;
        private readonly Encoding _encoding;
        private readonly ILogger<OvpnDataExchanger> _logger;
        private Socket _socket;
        public Task Reading { get; private set; } = Task.CompletedTask;
        public event EventHandler<string> RealTimeOutputReceived;
        public event EventHandler<EventArgs> Disconnected;

        public OvpnDataExchanger(BlockingQueue<string> outputMessageQueue, Encoding encoding, ILogger<OvpnDataExchanger> logger)
        {
            _outputMessageQueue = outputMessageQueue ?? throw new ArgumentNullException(nameof(outputMessageQueue));
            _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Dispose()
        {
            Reading.Wait();
            _dataRead.Dispose();
        }

        public void SetSocket(Socket socket)
        {
            _socket = socket;
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
#if NETCOREAPP3_1
            _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 60);
            _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 5);
            _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 3);
#endif
            _sb.Clear();
        }

        public void WaitForFirstDataRead()
        {
            _dataRead.Reset();
            _dataRead.Wait();
        }

        public string ReadDirect()
        {
            var len = _socket.Receive(_readBuffer);
            string result = null;
            if (len > 0)
            {
                result = _encoding.GetString(_readBuffer, 0, len);
                _logger.LogTrace("Read data of size {0}: {1}", len, result);
            }
            return result;
        }

        public string GetBuffer()
        {
            return _sb.ToString();
        }

        public void BeginReading()
        {
            Reading = Task.Factory.StartNew(ReadingCycle);
        }

        public string Read()
        {
            return _outputMessageQueue.Dequeue();
        }

        public string Read(int millisecondsTimeout)
        {
            _outputMessageQueue.TryDequeue(out var result, millisecondsTimeout);
            return result;
        }

        public void Write(string data, bool password = false)
        {
            var strPos = 0;
            _logger.LogTrace("Begin writing string {0}", password ? "*** PASSWORD ***" : data);
            while (strPos < data.Length)
            {
                var length = Math.Min(data.Length - strPos, MaxCharLength);
                var count = _encoding.GetBytes(data, strPos, length, _writeBuffer, 0);
                strPos += length;

                if (strPos == data.Length)
                {
                    try
                    {
                        count += _encoding.GetBytes(Constants.NewLine, 0, Constants.NewLine.Length, _writeBuffer, count);
                    }
                    catch (ArgumentException)
                    {
                        _socket.Send(_writeBuffer, count, SocketFlags.None);
                        count = _encoding.GetBytes(Constants.NewLine, 0, Constants.NewLine.Length, _writeBuffer, 0);
                    }
                }

                _socket.Send(_writeBuffer, count, SocketFlags.None);
            }
            _logger.LogTrace("End writing");
        }

        private void ReadingCycle()
        {
            while (true)
            {
                string data;
                try
                {
                    data = ReadDirect();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception when reading from socket");
                    Disconnected();
                    return;
                }
                if (data == null)
                {
                    Disconnected();
                    return;
                }

                ProcessReceivedData(data);
                _dataRead.Set();
                while (_strings.Count > 0)
                {
                    var str = _strings.Dequeue();
                    if (str.StartsWith(Constants.RealTimeMessageSign.ToString()))
                    {
                        try
                        {
                            RealTimeOutputReceived?.Invoke(this, str);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error invoking {0} event", nameof(RealTimeOutputReceived));
                        }
                    }
                    else
                        _outputMessageQueue.Enqueue(str);
                }
            }

            void Disconnected()
            {
                _dataRead.Set();
                _socket.Close();
                try
                {
                    this.Disconnected?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, nameof(this.Disconnected));
                }
            }
        }

        private void ProcessReceivedData(string data)
        {
            var startIndex = 0;

            while (true)
            {
                var pos = Constants.NewLine
                    .Select(x => data.IndexOf(x, startIndex))
                    .Where(x => x >= 0)
                    .DefaultIfEmpty(-1)
                    .Min();
                if (pos < 0)
                {
                    if (startIndex < data.Length)
                        _sb.Append(data.Substring(startIndex));
                    return;
                }

                _sb.Append(data.Substring(startIndex, pos - startIndex));
                var str = _sb.ToString();
                _logger.LogTrace("Extracted string: {0}", str);
                _strings.Enqueue(str);
                _sb.Clear();
                while (pos < data.Length && data[pos] is var c && Constants.NewLine.Any(x => c == x))
                {
                    pos++;
                }
                startIndex = pos;
            }
        }
    }
}

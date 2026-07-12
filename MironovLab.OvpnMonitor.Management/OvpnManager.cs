using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MironovLab.OpenVPN.Management.CommandResults;
using MironovLab.OpenVPN.Management.CommandResults.Parsers;
using MironovLab.OpenVPN.Management.Common;
using MironovLab.OpenVPN.Management.Common.Pf;
using MironovLab.OpenVPN.Management.Core;
using MironovLab.OpenVPN.Management.Exceptions;
using MironovLab.OpenVPN.Management.RealTimeMessages;

namespace MironovLab.OpenVPN.Management
{
    public class OvpnManager : IDisposable
    {
        private static readonly Regex HostNameValidator = new Regex("^([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\\-]{0,61}[a-zA-Z0-9])(\\.([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\\-]{0,61}[a-zA-Z0-9]))*$");
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<OvpnManager> _logger;
        private readonly OvpnDataExchanger _io;
        private readonly object _lock = new object();
        private TcpClient _client;
        private bool _disposed;
        public event EventHandler<ByteCount> BytesCountReceived;
        public event EventHandler<ByteCountCli> BytesCountCliReceived;
        public event EventHandler<Client> ClientReceived;
        public event EventHandler<Echo> EchoReceived;
        public event EventHandler<SimpleTextMessage> FatalReceived;
        public event EventHandler<SimpleTextMessage> HoldReceived;
        public event EventHandler<SimpleTextMessage> InfoReceived;
        public event EventHandler<Log> LogReceived;
        public event EventHandler<NeedOk> NeedOkReceived;
        public event EventHandler<NeedStr> NeedStrReceived;
        public event EventHandler<Password> PasswordReceived;
        public event EventHandler<State> StateReceived;
        public event EventHandler<EventArgs> Disconnected;

        public OvpnManager() : this(NullLoggerFactory.Instance)
        {
        }

        public OvpnManager(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<OvpnManager>();
            var outputMessageQueue = new BlockingQueue<string>();
            _io = new OvpnDataExchanger(outputMessageQueue, Encoding.UTF8, loggerFactory.CreateLogger<OvpnDataExchanger>());
            var messageReSender = new MessageReSender(outputMessageQueue, loggerFactory.CreateLogger<MessageReSender>());
            var realTimeMessageReceiver = new RealTimeMessageReceiver(loggerFactory.CreateLogger<RealTimeMessageReceiver>());
            _io.RealTimeOutputReceived += realTimeMessageReceiver.OnRealTimeMessageReceived;
            realTimeMessageReceiver.ByteCountReceived += Receiver_ByteCountReceived;
            realTimeMessageReceiver.ByteCountCliReceived += Receiver_ByteCountCliReceived;
            realTimeMessageReceiver.ClientReceived += Receiver_ClientReceived;
            realTimeMessageReceiver.EchoReceived += Receiver_EchoReceived;
            realTimeMessageReceiver.FatalReceived += Receiver_FatalReceived;
            realTimeMessageReceiver.HoldReceived += Receiver_HoldReceived;
            realTimeMessageReceiver.InfoReceived += Receiver_InfoReceived;
            realTimeMessageReceiver.LogReceived += Receiver_LogReceived;
            realTimeMessageReceiver.NeedOkReceived += Receiver_NeedOkReceived;
            realTimeMessageReceiver.NeedStrReceived += Receiver_NeedStrReceived;
            realTimeMessageReceiver.PasswordReceived += Receiver_PasswordReceived;
            realTimeMessageReceiver.StateReceived += Receiver_StateReceived;
            realTimeMessageReceiver.UnrecognizedMessageReceived += messageReSender.OnUnrecognizedRealTimeMessageReceived;
            _io.Disconnected += Exchanger_Disconnect;
            realTimeMessageReceiver.UnhandledException += Receiver_UnhandledException;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_lock)
            {
                _disposed = true;
                if (_client?.Client is Socket socket && socket.Connected)
                {
                    try
                    {
                        _io.Write(Constants.Commands.Exit);
                        _io.Reading.Wait();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, nameof(Dispose));
                    }
                }

                _io.Dispose();
                _client?.Dispose();
            }
        }

        public string Connect(string hostname, int port, string password)
        {
            lock (_lock)
            {
                if (_client != null && _client.Connected)
                    throw new InvalidOperationException(Resources.OvpnAlreadyConnected);

                _client?.Dispose();
                _client = new TcpClient();
                _io.SetSocket(_client.Client);
                _client.Connect(hostname, port);

                try
                {
                    ParseUtils.ParseSimpleMessage(_io.ReadDirect(), out var messageType, out _);
                    if (messageType != Constants.EnterPassword)
                        throw new OvpnManagerException(Resources.NoPasswordRequest);

                    _io.Write(password, true);
                    _io.BeginReading();
                    if (!ReadCommandSimpleResult(out var message))
                        throw AuthenticationException.Create(message);

                    return message;
                }
                catch
                {
                    _client.Close();
                    throw;
                }
            }
        }

        public void Connect(string hostname, int port)
        {
            lock (_lock)
            {
                if (_client != null && _client.Connected)
                    throw new InvalidOperationException(Resources.OvpnAlreadyConnected);
                
                _client?.Dispose();
                _client = new TcpClient();
                _io.SetSocket(_client.Client);
                _client.Connect(hostname, port);

                try
                {
                    _io.BeginReading();
                    _io.WaitForFirstDataRead();
                    ParseUtils.ParseSimpleMessage(_io.GetBuffer(), out var messageType, out _);
                    if (messageType == Constants.EnterPassword)
                        throw AuthenticationException.Create(Resources.ManagementPasswordRequired);
                }
                catch
                {
                    _client.Close();
                    throw;
                }
            }
        }

        public string ByteCount(int intervalSec = 0)
        {
            if (intervalSec < 0) throw new ArgumentOutOfRangeException(nameof(intervalSec));

            lock (_lock)
            {
                _io.Write(MakeCommandLine(Constants.Commands.ByteCount, false, intervalSec));
                return GetCommandSimpleResult();
            }
        }

        public string Echo(Switch @switch)
        {
            string arg;
            switch(@switch)
            {
                case Switch.Off:
                    arg = Constants.CommandArguments.Off;
                    break;
                case Switch.On:
                    arg = Constants.CommandArguments.On;
                    break;
                case Switch.Release:
                    throw new NotSupportedException(string.Format(Resources.SwitchEnumMemberIsNotSupported, Constants.Commands.Echo, @switch));
                default:
                    throw new ArgumentOutOfRangeException(nameof(@switch), @switch, null);
            }
            lock (_lock)
            {
                _io.Write(MakeCommandLine(Constants.Commands.Echo, false, arg));
                return GetCommandSimpleResult();
            }
        }

        public List<EchoItem> Echo(bool enableRealTimeLogOutput = false)
        {
            lock (_lock)
            {
                var cmdLine = enableRealTimeLogOutput
                    ? MakeCommandLine(Constants.Commands.Echo, false, Constants.CommandArguments.On, Constants.CommandArguments.All)
                    : MakeCommandLine(Constants.Commands.Echo, false, Constants.CommandArguments.All);
                List<EchoItem> result;
                try
                {
                    _io.Write(cmdLine);
                    if (enableRealTimeLogOutput && !ReadCommandSimpleResult(out var text))
                        throw new CommandResultError(text);
                }
                finally
                {
                    using (var parser = new EchoParser(_loggerFactory))
                        result = parser.ParseResult(_io);
                }

                return result;
            }
        }

        public Switch Hold()
        {
            lock (_lock)
            {
                _io.Write(Constants.Commands.Hold);
                return HoldParser.Parse(_io.Read());
            }
        }

        public string Hold(Switch @switch)
        {
            string arg;
            switch (@switch)
            {
                case Switch.Off:
                    arg = Constants.CommandArguments.Off;
                    break;
                case Switch.On:
                    arg = Constants.CommandArguments.On;
                    break;
                case Switch.Release:
                    arg = Constants.CommandArguments.Release;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(@switch), @switch, null);
            }

            lock (_lock)
            {
                _io.Write(MakeCommandLine(Constants.Commands.Hold, false, arg));
                return GetCommandSimpleResult();
            }
        }

        public string Kill(string commonName)
        {
            lock (_lock)
            {
                _io.Write(MakeCommandLine(Constants.Commands.Kill, false, commonName));
                return GetCommandSimpleResult();
            }
        }

        public string Kill(IPEndPoint sourceAddress)
        {
            lock (_lock)
            {
                _io.Write(MakeCommandLine(Constants.Commands.Kill, false, sourceAddress));
                return GetCommandSimpleResult();
            }
        }

        public string Log(Switch @switch)
        {
            string arg;
            switch(@switch)
            {
                case Switch.Off:
                    arg = Constants.CommandArguments.Off;
                    break;
                case Switch.On:
                    arg = Constants.CommandArguments.On;
                    break;
                case Switch.Release:
                    throw new NotSupportedException(string.Format(Resources.SwitchEnumMemberIsNotSupported, Constants.Commands.Log, @switch));
                default:
                    throw new ArgumentOutOfRangeException(nameof(@switch), @switch, null);
            }

            lock (_lock)
            {
                _io.Write(MakeCommandLine(Constants.Commands.Log, false, arg));
                return GetCommandSimpleResult();
            }
        }

        public List<LogRecord> Log(bool enableRealTimeLogOutput = false)
        {
            lock (_lock)
            {
                var cmdLine = enableRealTimeLogOutput
                    ? MakeCommandLine(Constants.Commands.Log, false, Constants.CommandArguments.On, Constants.CommandArguments.All)
                    : MakeCommandLine(Constants.Commands.Log, false, Constants.CommandArguments.All);
                List<LogRecord> result;
                try
                {
                    _io.Write(cmdLine);
                    if (enableRealTimeLogOutput && !ReadCommandSimpleResult(out var text))
                        throw new CommandResultError(text);
                }
                finally
                {
                    using (var parser = new LogParser(Constants.DefaultLogFileHistoryCacheSize, _loggerFactory))
                        result = parser.ParseResult(_io);
                }

                return result;
            }
        }

        public List<LogRecord> Log(int count, bool enableRealTimeLogOutput = false)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));

            lock (_lock)
            {
                var cmdLine = enableRealTimeLogOutput
                    ? MakeCommandLine(Constants.Commands.Log, false, Constants.CommandArguments.On, count)
                    : MakeCommandLine(Constants.Commands.Log, false, count);

                List<LogRecord> result;
                try
                {
                    _io.Write(cmdLine);
                    if (enableRealTimeLogOutput && !ReadCommandSimpleResult(out var text))
                        throw new CommandResultError(text);
                }
                finally
                {
                    using (var parser = new LogParser(Constants.DefaultLogFileHistoryCacheSize, _loggerFactory))
                        result = parser.ParseResult(_io);
                }

                return result;
            }
        }

        public int Mute()
        {
            lock (_lock)
            {
                _io.Write(Constants.Commands.Mute);
                return KeyValueParser.ParseMute(_io.Read());
            }
        }

        public string Mute(int parameter)
        {
            if (parameter < 0) throw new ArgumentOutOfRangeException(nameof(parameter));

            lock (_lock)
            {
                _io.Write(MakeCommandLine(Constants.Commands.Mute, false, parameter));
                return GetCommandSimpleResult();
            }
        }

        public int Pid()
        {
            lock (_lock)
            {
                _io.Write(Constants.Commands.Pid);
                return KeyValueParser.ParsePid(_io.Read());
            }
        }

        public string UserName(PasswordType passwordType, string userName)
        {
            string arg;
            switch (passwordType)
            {
                case PasswordType.PrivateKey:
                    arg = Constants.CommandArguments.PasswordTypePrivateKey;
                    break;
                case PasswordType.Auth:
                    arg = Constants.CommandArguments.PasswordTypeAuth;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(passwordType), passwordType, null);
            }

            lock (_lock)
            {
                _io.Write(MakeCommandLine(Constants.Commands.UserName, true, arg, userName));
                return GetCommandSimpleResult();
            }
        }

        public string Password(PasswordType passwordType, string password)
        {
            string arg;
            switch (passwordType)
            {
                case PasswordType.PrivateKey:
                    arg = Constants.CommandArguments.PasswordTypePrivateKey;
                    break;
                case PasswordType.Auth:
                    arg = Constants.CommandArguments.PasswordTypeAuth;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(passwordType), passwordType, null);
            }

            lock (_lock)
            {
                _io.Write(MakeCommandLine(Constants.Commands.Password, true, arg, password));
                return GetCommandSimpleResult();
            }
        }

        public string ForgetPasswords()
        {
            lock (_lock)
            {
                _io.Write(Constants.Commands.ForgetPasswords);
                return GetCommandSimpleResult();
            }
        }

        public string Signal(Signal signal)
        {
            string arg;
            switch (signal)
            {
                case Common.Signal.HangUp:
                    arg = Constants.CommandArguments.SignalHangUp;
                    break;
                case Common.Signal.Terminate:
                    arg = Constants.CommandArguments.SignalTerminate;
                    break;
                case Common.Signal.UserDefined1:
                    arg = Constants.CommandArguments.SignalUserDefined1;
                    break;
                case Common.Signal.UserDefined2:
                    arg = Constants.CommandArguments.SignalUserDefined2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(signal), signal, null);
            }

            lock (_lock)
            {
                _io.Write(MakeCommandLine(Constants.Commands.Signal, false, arg));
                return GetCommandSimpleResult();
            }
        }

        public StateRecord State()
        {
            lock (_lock)
            {
                _io.Write(Constants.Commands.State);
                var stateParser = new StateParser(_loggerFactory);
                return stateParser.ParseResult(_io).First();
            }
        }

        public string State(Switch @switch)
        {
            string arg;
            switch (@switch)
            {
                case Switch.Off:
                    arg = Constants.CommandArguments.Off;
                    break;
                case Switch.On:
                    arg = Constants.CommandArguments.On;
                    break;
                case Switch.Release:
                    throw new NotSupportedException(string.Format(Resources.SwitchEnumMemberIsNotSupported, Constants.Commands.State, @switch));
                default:
                    throw new ArgumentOutOfRangeException(nameof(@switch), @switch, null);
            }

            lock (_lock)
            {
                _io.Write(MakeCommandLine(Constants.Commands.State, false, arg));
                return GetCommandSimpleResult();
            }
        }

        public List<StateRecord> State(int? count, bool enableRealTimeNotification = false)
        {
            var countArg = count.HasValue ? (object) count.Value : Constants.CommandArguments.All;
            var cmdLine = enableRealTimeNotification
                ? MakeCommandLine(Constants.Commands.State, false, Constants.CommandArguments.On, countArg)
                : MakeCommandLine(Constants.Commands.State, false, countArg);

            lock (_lock)
            {
                _io.Write(cmdLine);
                var stateParser = new StateParser(_loggerFactory);
                return stateParser.ParseResult(_io);
            }
        }

        public Status Status()
        {
            lock (_lock)
            {
                _io.Write(Constants.Commands.Status);
                using (var statusParser = new StatusParser(_loggerFactory))
                    return statusParser.ParseResult(_io);
            }
        }

        public int Verb()
        {
            lock (_lock)
            {
                _io.Write(Constants.Commands.Verb);
                return KeyValueParser.ParseVerb(_io.Read());
            }
        }

        public string Verb(int verbosityLevel)
        {
            if (verbosityLevel < 0 || verbosityLevel > 15)
                throw new ArgumentOutOfRangeException(nameof(verbosityLevel), verbosityLevel, Resources.VerbosityLevelOutOfRange);

            lock (_lock)
            {
                _io.Write(MakeCommandLine(Constants.Commands.Verb, false, verbosityLevel));
                return GetCommandSimpleResult();
            }
        }

        public CommandResults.Version Version()
        {
            lock (_lock)
            {
                _io.Write(Constants.Commands.Version);
                using (var versionParser = new VersionParser(_loggerFactory))
                    return versionParser.ParseResult(_io);
            }
        }

        public AuthRetryType AuthRetry()
        {
            lock (_lock)
            {
                _io.Write(Constants.Commands.AuthRetry);
                return KeyValueParser.ParseAuthRetryType(_io.Read());
            }
        }

        public string AuthRetry(AuthRetryType type)
        {
            string arg;
            switch (type)
            {
                case AuthRetryType.None:
                    arg = Constants.CommandArguments.AuthRetryTypeNone;
                    break;
                case AuthRetryType.NoInteract:
                    arg = Constants.CommandArguments.AuthRetryTypeNoInteract;
                    break;
                case AuthRetryType.Interact:
                    arg = Constants.CommandArguments.AuthRetryTypeInteract;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            lock (_lock)
            {
                _io.Write(MakeCommandLine(Constants.Commands.AuthRetry, false, arg));
                return GetCommandSimpleResult();
            }
        }

        public string NeedOk(NeedOkRequestType requestType, NeedOkResponseType responseType)
        {
            string request;
            switch (requestType)
            {
                case NeedOkRequestType.TokenInsertionRequest:
                    request = Constants.CommandArguments.NeedOkTokenInsertionRequest;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(requestType), requestType, null);
            }
            string response;
            switch (responseType)
            {
                case NeedOkResponseType.Ok:
                    response = Constants.CommandArguments.NeedOkResponseOk;
                    break;
                case NeedOkResponseType.Cancel:
                    response = Constants.CommandArguments.NeedOkResponseCancel;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(responseType), responseType, null);
            }
            lock (_lock)
            {
                _io.Write(MakeCommandLine(Constants.Commands.NeedOk, false, request, response));
                return GetCommandSimpleResult();
            }
        }

        public string NeedStr(string inputType, string inputData)
        {
            lock (_lock)
            {
                _io.Write(MakeCommandLine(Constants.Commands.NeedStr, false, inputType, inputData));
                return GetCommandSimpleResult();
            }
        }

        // ReSharper disable once IdentifierTypo
        // ReSharper disable once InconsistentNaming
        public int PKCS11IdCount()
        {
            lock (_lock)
            {
                _io.Write(Constants.Commands.PKCS11IdCount);
                var result = _io.Read();
                ThrowIfError(result);
                return PKCS11IdParser.ParseCount(result);
            }
        }

        // ReSharper disable once IdentifierTypo
        // ReSharper disable once InconsistentNaming
        public PKCS11IdEntry PKCS11IdGet(int id)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));

            lock (_lock)
            {
                _io.Write(MakeCommandLine(Constants.Commands.PKCS11IdGet, false, id));
                var result = _io.Read();
                ThrowIfError(result);
                return PKCS11IdParser.ParseGet(result);
            }
        }

        public string ClientAuth(int clientId, int keyId, IEnumerable<string> clientConfiguration)
        {
            if (clientId <= 0) throw new ArgumentOutOfRangeException(nameof(clientId));
            if (keyId <= 0) throw new ArgumentOutOfRangeException(nameof(keyId));

            lock (_lock)
            {
                _io.Write(MakeCommandLine(Constants.Commands.ClientAuth, false, clientId, keyId));
                try
                {
                    if (clientConfiguration != null)
                    {
                        foreach (var configLine in clientConfiguration)
                        {
                            var line = ParseUtils.SanitizeNewLine(configLine);
                            if (!string.IsNullOrEmpty(line) && line != Constants.EndOfResult)
                                _io.Write(line);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, nameof(ClientAuth));
                }

                _io.Write(Constants.EndOfResult);
                return GetCommandSimpleResult();
            }
        }

        public string ClientAuthNt(int clientId, int keyId)
        {
            if (clientId <= 0) throw new ArgumentOutOfRangeException(nameof(clientId));
            if (keyId <= 0) throw new ArgumentOutOfRangeException(nameof(keyId));

            lock (_lock)
            {
                _io.Write(MakeCommandLine(Constants.Commands.ClientAuthNt, false, clientId, keyId));
                return GetCommandSimpleResult();
            }
        }

        public string ClientDeny(int clientId, int keyId, string reasonText, string clientReasonText = null)
        {
            if (clientId <= 0) throw new ArgumentOutOfRangeException(nameof(clientId));
            if (keyId <= 0) throw new ArgumentOutOfRangeException(nameof(keyId));
            if (reasonText == null) throw new ArgumentNullException(nameof(reasonText));

            reasonText = ParseUtils.SanitizeNewLine(reasonText);
            if (clientReasonText != null)
                clientReasonText = ParseUtils.SanitizeNewLine(clientReasonText);

            lock (_lock)
            {
                _io.Write(MakeCommandLine(Constants.Commands.ClientDeny, true, clientId, keyId, reasonText, clientReasonText));
                return GetCommandSimpleResult();
            }
        }

        public string ClientKill(int clientId)
        {
            if (clientId <= 0) throw new ArgumentOutOfRangeException(nameof(clientId));

            lock (_lock)
            {
                _io.Write(MakeCommandLine(Constants.Commands.ClientKill, false, clientId));
                return GetCommandSimpleResult();
            }
        }

        public string ClientPacketFilter(int clientId, PacketFilter packetFilter)
        {
            if (clientId <= 0) throw new ArgumentOutOfRangeException(nameof(clientId));
            if (packetFilter == null) throw new ArgumentNullException(nameof(packetFilter));

            lock (_lock)
            {
                _io.Write(MakeCommandLine(Constants.Commands.ClientPacketFilter, false, clientId));
                try
                {
                    foreach (var line in packetFilter.ToString().Split(new []{Constants.NewLine}, StringSplitOptions.RemoveEmptyEntries))
                    {
                        _io.Write(line);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, nameof(ClientPacketFilter));
                }

                _io.Write(Constants.EndOfResult);
                return GetCommandSimpleResult();
            }
        }

        public string Remote(RemoteAction action, string hostAddress = null, int port = 0)
        {
            string cmdLine;
            switch (action)
            {
                case RemoteAction.Accept:
                    cmdLine = MakeCommandLine(Constants.Commands.Remote, false, Constants.CommandArguments.RemoteAccept);
                    break;
                case RemoteAction.Modify:
                    if (hostAddress == null) throw new ArgumentNullException(nameof(hostAddress));
                    if (port <= 0) throw new ArgumentOutOfRangeException(nameof(port));
                    if (!IsHostAddressValid(hostAddress))
                        throw new ArgumentException(string.Format(Resources.OvpnManager_HostNameIsNotValid, hostAddress));

                    cmdLine = MakeCommandLine(Constants.Commands.Remote, false, Constants.CommandArguments.RemoteModify, hostAddress, port);
                    break;
                case RemoteAction.Skip:
                    cmdLine = MakeCommandLine(Constants.Commands.Remote, false, Constants.CommandArguments.RemoteSkip);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }

            lock (_lock)
            {
                _io.Write(cmdLine);
                return GetCommandSimpleResult();
            }
        }

        public string Proxy(ProxyType type, string hostAddress = null, int port = 0, bool nonClearTextAuthOnly = false)
        {
            string cmdLine;
            switch (type)
            {
                case ProxyType.None:
                    cmdLine = MakeCommandLine(Constants.Commands.Proxy, false, Constants.CommandArguments.ProxyNone);
                    break;
                case ProxyType.Http:
                case ProxyType.Socks:
                    if (hostAddress == null) throw new ArgumentNullException(nameof(hostAddress));
                    if (port <= 0) throw new ArgumentOutOfRangeException(nameof(port));
                    if (!IsHostAddressValid(hostAddress))
                        throw new ArgumentException(string.Format(Resources.OvpnManager_HostNameIsNotValid, hostAddress));

                    switch (type)
                    {
                        case ProxyType.Http:
                            cmdLine = Constants.CommandArguments.ProxyHttp;
                            break;
                        case ProxyType.Socks:
                            cmdLine = Constants.CommandArguments.ProxySocks;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(type), type, null);
                    }

                    cmdLine = nonClearTextAuthOnly
                        ? MakeCommandLine(Constants.Commands.Proxy, false, cmdLine, hostAddress, port, Constants.CommandArguments.ProxyNonClearText)
                        : MakeCommandLine(Constants.Commands.Proxy, false, cmdLine, hostAddress, port);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            lock (_lock)
            {
                _io.Write(cmdLine);
                return GetCommandSimpleResult();
            }
        }

        public string RSASign(byte[] data)
        {
            lock (_lock)
            {
                _io.Write(Constants.Commands.RSASign);
                var result = _io.Read(Constants.ResponseWaitingMSec);
                if (!string.IsNullOrEmpty(result))
                {
                    ThrowIfError(result);
                    _logger.LogWarning("Unexpected data read: {0}", result);
                }

                var b64 = Convert.ToBase64String(data, Base64FormattingOptions.InsertLineBreaks);
                foreach (var line in ParseUtils.SplitAndSanitize(b64))
                    _io.Write(line);
                _io.Write(Constants.EndOfResult);
                return GetCommandSimpleResult();
            }
        }

        private bool ReadCommandSimpleResult(out string message)
        {
            var result = _io.Read();

            ParseUtils.ParseSimpleMessage(result, out var messageType, out message);
            switch (messageType)
            {
                case Constants.MessageTypes.Success:
                    return true;
                case Constants.MessageTypes.Error:
                    return false;
                default:
                    throw new OvpnManagerException(string.Format(Resources.UnexpectedCommandResult, result));
            }
        }

        private string GetCommandSimpleResult()
        {
            if (!ReadCommandSimpleResult(out var text))
                throw new CommandResultError(text);
            return text;
        }

        #region Real-Time message handlers

        private void Receiver_ByteCountReceived(object sender, ByteCount e)
        {
            BytesCountReceived?.Invoke(this, e);
        }

        private void Receiver_ByteCountCliReceived(object sender, ByteCountCli e)
        {
            BytesCountCliReceived?.Invoke(this, e);
        }

        private void Receiver_ClientReceived(object sender, Client e)
        {
            ClientReceived?.Invoke(this, e);
        }

        private void Receiver_EchoReceived(object sender, Echo e)
        {
            EchoReceived?.Invoke(this, e);
        }

        private void Receiver_FatalReceived(object sender, SimpleTextMessage e)
        {
            FatalReceived?.Invoke(this, e);
        }

        private void Receiver_HoldReceived(object sender, SimpleTextMessage e)
        {
            HoldReceived?.Invoke(this, e);
        }

        private void Receiver_InfoReceived(object sender, SimpleTextMessage e)
        {
            InfoReceived?.Invoke(this, e);
        }

        private void Receiver_LogReceived(object sender, Log e)
        {
            LogReceived?.Invoke(this, e);
        }

        private void Receiver_NeedOkReceived(object sender, NeedOk e)
        {
            NeedOkReceived?.Invoke(this, e);
        }

        private void Receiver_NeedStrReceived(object sender, NeedStr e)
        {
            NeedStrReceived?.Invoke(this, e);
        }

        private void Receiver_PasswordReceived(object sender, Password e)
        {
            PasswordReceived?.Invoke(this, e);
        }

        private void Receiver_StateReceived(object sender, State e)
        {
            StateReceived?.Invoke(this, e);
        }

        private void Exchanger_Disconnect(object sender, EventArgs e)
        {
            if (!_disposed)
                Disconnected?.Invoke(this, e);
        }

        private void Receiver_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _logger.LogError((Exception)e.ExceptionObject, "Exception thrown from event handler");
        }

        #endregion

        private static string MakeCommandLine(string commandName, bool withQuotes, params object[] args)
        {
            var toString = withQuotes
                ? (Func<object, string>) ParseUtils.ToStringWithQuotes
                : ParseUtils.ToString;
            var parameters = Enumerable.Repeat(commandName, 1)
                .Concat(args
                    .Select(toString)
                    .Where(x => x != null));
            return string.Join(Constants.WhiteSpace.ToString(), parameters);
        }

        private static void ThrowIfError(string data)
        {
            ParseUtils.ParseSimpleMessage(data, out var messageType, out var messageText);
            if (messageType == Constants.MessageTypes.Error)
                throw new CommandResultError(messageText);
        }

        private static bool IsHostAddressValid(string hostAddress)
        {
            return HostNameValidator.IsMatch(hostAddress) ||
                   IPAddress.TryParse(hostAddress, out var ipAddress) &&
                   ipAddress.AddressFamily is AddressFamily addressFamily &&
                   (addressFamily == AddressFamily.InterNetwork ||
                    addressFamily == AddressFamily.InterNetworkV6);
        }
    }
}
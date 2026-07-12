using System;
using Microsoft.Extensions.Logging;
using MironovLab.OpenVPN.Management.RealTimeMessages;
using MironovLab.OpenVPN.Management.RealTimeMessages.Parsers;

namespace MironovLab.OpenVPN.Management.Core
{
    internal class RealTimeMessageReceiver
    {
        private readonly ILogger<RealTimeMessageReceiver> _logger;
        private IRealTimeMessageParser _parser;

        public event EventHandler<ByteCount> ByteCountReceived;
        public event EventHandler<ByteCountCli> ByteCountCliReceived;
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
        public event EventHandler<Notify> NotifyReceived; 
        public event EventHandler<string> UnrecognizedMessageReceived;
        public event UnhandledExceptionEventHandler UnhandledException;

        public RealTimeMessageReceiver(ILogger<RealTimeMessageReceiver> logger)
        {
            _logger = logger;
        }

        public void OnRealTimeMessageReceived(object sender, string message)
        {
            if (_parser == null)
            {
                var pos = message.IndexOf(Constants.SourceTypeDataSplitter, 1);
                if (pos == -1)
                    pos = message.Length;
                var messageName = message.Substring(1, pos - 1);

                switch (messageName)
                {
                    case Constants.RealTimeMessages.ByteCount:
                        var byteCountParser = new ByteCountParser();
                        byteCountParser.MessageParsed += ByteCountReceived;
                        _parser = byteCountParser;
                        break;

                    case Constants.RealTimeMessages.ByteCountCli:
                        var bytesCountCliParser = new BytesCountCliParser();
                        bytesCountCliParser.MessageParsed += ByteCountCliReceived;
                        _parser = bytesCountCliParser;
                        break;

                    case Constants.RealTimeMessages.Client:
                        var clientParser = new ClientParser();
                        clientParser.MessageParsed += ClientReceived;
                        _parser = clientParser;
                        break;

                    case Constants.RealTimeMessages.Echo:
                        var echoParser = new EchoParser();
                        echoParser.MessageParsed += EchoReceived;
                        _parser = echoParser;
                        break;

                    case Constants.RealTimeMessages.Fatal:
                        var fatalParser = new SimpleTextMessageParser(RealTimeMessageType.Fatal);
                        fatalParser.MessageParsed += FatalReceived;
                        _parser = fatalParser;
                        break;

                    case Constants.RealTimeMessages.Hold:
                        var holdParser = new SimpleTextMessageParser(RealTimeMessageType.Hold);
                        holdParser.MessageParsed += HoldReceived;
                        _parser = holdParser;
                        break;

                    case Constants.RealTimeMessages.Info:
                        var infoParser = new SimpleTextMessageParser(RealTimeMessageType.Info);
                        infoParser.MessageParsed += InfoReceived;
                        _parser = infoParser;
                        break;

                    case Constants.RealTimeMessages.Log:
                        var logParser = new LogParser();
                        logParser.MessageParsed += LogReceived;
                        _parser = logParser;
                        break;

                    case Constants.RealTimeMessages.NeedOk:
                        var needOkParser = new NeedOkParser();
                        needOkParser.MessageParsed += NeedOkReceived;
                        _parser = needOkParser;
                        break;

                    case Constants.RealTimeMessages.NeedStr:
                        var needStrParser = new NeedStrParser();
                        needStrParser.MessageParsed += NeedStrReceived;
                        _parser = needStrParser;
                        break;

                    case Constants.RealTimeMessages.Password:
                        var passwordParser = new PasswordParser();
                        passwordParser.MessageParsed += PasswordReceived;
                        _parser = passwordParser;
                        break;

                    case Constants.RealTimeMessages.State:
                        var stateParser = new StateParser();
                        stateParser.MessageParsed += StateReceived;
                        _parser = stateParser;
                        break;

                    case Constants.RealTimeMessages.Notify:
                        var notifyParser = new NotifyParser();
                        notifyParser.MessageParsed += NotifyReceived;
                        _parser = notifyParser;
                        break;

                    default:
                        UnrecognizedMessageReceived?.Invoke(this, message);
                        return;
                }
            }

            try
            {
                _parser.Parse(message);
            }
            catch (Exception e)
            {
                _logger.LogWarning(_parser.GetType().ToString());
                _logger.LogWarning(message);
                UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(e, false));
            }

            if (_parser.ObjectIsReady)
            {
                _parser.Dispose();
                _parser = null;
            }
        }
    }
}

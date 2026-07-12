using System;
using Microsoft.Extensions.Logging;

namespace MironovLab.OpenVPN.Management.Core
{
    internal class MessageReSender
    {
        private readonly BlockingQueue<string> _syncMessageQueue;
        private readonly ILogger<MessageReSender> _logger;

        public MessageReSender(BlockingQueue<string> syncMessageQueue, ILogger<MessageReSender> logger)
        {
            _syncMessageQueue = syncMessageQueue ?? throw new ArgumentNullException(nameof(syncMessageQueue));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void OnUnrecognizedRealTimeMessageReceived(object sender, string message)
        {
            ParseUtils.ParseSimpleMessage(message, out var messageType, out _);
            switch (messageType)
            {
                case Constants.MessageTypes.PKCS11IdCount:
                case Constants.MessageTypes.PKCS11IdGet:
                    _syncMessageQueue.Enqueue(message);
                    break;
                default:
                    _logger.LogWarning("Unrecognized RT message dropped: {0}", message);
                    break;
            }
        }
    }
}

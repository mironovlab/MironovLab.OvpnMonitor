using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MironovLab.OpenVPN.Management;
using MironovLab.OpenVPN.Management.CommandResults;
using MironovLab.OpenVPN.Management.RealTimeMessages;
using MironovLab.OvpnMonitor.Service.AddressTranslation;
using MironovLab.OvpnMonitor.Service.Configuration;
using MironovLab.OvpnMonitor.Service.DbConnection;
using MironovLab.OvpnMonitor.Service.Logging;

namespace MironovLab.OvpnMonitor.Service
{
    public class Worker : IDisposable
    {
        private readonly ILogger<Worker> _logger;
        private readonly string _dbcs;
        private readonly TranslationService _translator;
        private readonly OpenVPNConfiguration _config;
        private readonly OvpnManager _ovpn;
        private readonly Dictionary<string, int> _userIdByCommonName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly IDictionary<int, Session> _sessionsByClientId = new ConcurrentDictionary<int, Session>();
        private readonly Dictionary<int, int> _clientIdByUserId = new Dictionary<int, int>();
        private readonly DelayedSaver _delayedSaver;
        private readonly ManualResetEventSlim _processingEnabled = new ManualResetEventSlim(false);
        private CancellationToken _stoppingToken;
        private DateTime _dateTime;

        public Worker(ILoggerFactory loggerFactory, TranslationService translator, OpenVPNConfiguration config, IOptions<MySqlConfiguration> mysqlConfig)
        {
            _translator = translator ?? throw new ArgumentNullException(nameof(translator));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = loggerFactory.CreateLogger<Worker>(config);
            _dbcs = mysqlConfig.Value.ToString();
            _delayedSaver = new DelayedSaver(mysqlConfig, loggerFactory.CreateLogger<DelayedSaver>());
            _ovpn = new OvpnManager(loggerFactory);
            _ovpn.ClientReceived += OvpnOnClientReceived;
            _ovpn.BytesCountCliReceived += OvpnOnBytesCount;
            _ovpn.Disconnected += OvpnOnDisconnected;
        }

        public void Dispose()
        {
            _processingEnabled.Dispose();
        }

        private void OvpnOnClientReceived(object sender, Client e)
        {
            if (e.NotificationType == ClientNotificationType.Established)
            {
                _processingEnabled.Wait(_stoppingToken);
                using var ctx = new MySqlContext(_dbcs);
                var envVars = e.GetEnvironmentVariablesReader();
                var userId = GetUserId(ctx, envVars.CommonName, envVars.VirtualAddress.ToString());
                if (_clientIdByUserId.TryGetValue(userId, out var clientId))
                {
                    _sessionsByClientId.Remove(clientId);
                }

                var session = new Session
                {
                    UserId = userId,
                    SessionId = e.ClientID,
                    Connected = envVars.Time.ToLocalTime(),
                    IPAddress = TryTranslate(envVars.RealAddress),
                    Platform = envVars.Platform.ToString(),
                    LastUpdated = DateTime.Now,
                };
                _sessionsByClientId[e.ClientID] = session;
                _clientIdByUserId[userId] = e.ClientID;
                ctx.Sessions.Add(session);
                ctx.SaveChanges();
            }
        }

        private void OvpnOnBytesCount(object sender, ByteCountCli e)
        {
            var session = _sessionsByClientId[e.ClientID];
            session.BytesIn = e.BytesOut;
            session.BytesOut = e.BytesIn;
            session.LastUpdated = DateTime.Now;
            _delayedSaver.Add(session);
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _stoppingToken = stoppingToken;
            await Task.Yield();
            OvpnOnDisconnected(this, EventArgs.Empty);
            try
            {
                _dateTime = DateTime.Today;
                while (!stoppingToken.IsCancellationRequested)
                {
                    var toWait = _dateTime.AddDays(1) - DateTime.Now;
                    if (toWait > TimeSpan.Zero)
                        await Task.Delay(toWait, stoppingToken);
                    var today = DateTime.Today;
                    if (_dateTime != today)
                    {
                        _processingEnabled.Reset();
                        _delayedSaver.Save();
                        try
                        {
                            var status = _ovpn.Status();
                            using var ctx = new MySqlContext(_dbcs);
                            UpdateClients(ctx, status);
                            foreach (var session in status.Clients)
                            {
                                var savedSession = _sessionsByClientId[session.ClientID];
                                ctx.IntermediateDatas.Add(new IntermediateData
                                {
                                    SessionId = savedSession.Id,
                                    Date = today,
                                    BytesIn = session.BytesSent,
                                    BytesOut = session.BytesReceived,
                                });
                            }

                            await ctx.SaveChangesAsync(stoppingToken);
                        }
                        finally
                        {
                            _processingEnabled.Set();
                        }
                        _dateTime = today;
                        _logger.LogInformation("Intermediate data added!");
                    }
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(ExecuteAsync));
            }
            _ovpn.Dispose();
        }

        private async void OvpnOnDisconnected(object sender, EventArgs e)
        {
            if (sender == _ovpn)
                _logger.LogWarning("Connection with server is lost, will try to reconnect");

            _processingEnabled.Reset();
            while (!_stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!string.IsNullOrEmpty(_config.Password))
                        _ovpn.Connect(_config.HostAddress, _config.Port, _config.Password);
                    else
                        _ovpn.Connect(_config.HostAddress, _config.Port);
                    _logger.LogInformation("Successfully connected to OpenVPN Management Interface");
                    InitMonitoring();
                    _processingEnabled.Set();
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error when (re-)connecting to OpenVPN Management Interface");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), _stoppingToken);
            }
        }

        private void InitMonitoring()
        {
            using var ctx = new MySqlContext(_dbcs);
            foreach (var dbUser in ctx.Users)
                _userIdByCommonName[dbUser.CommonName] = dbUser.Id;

            var status = _ovpn.Status();
            UpdateClients(ctx, status);
            _ovpn.ByteCount(20);

            _logger.LogInformation("Initialization finished!");
        }

        private void UpdateClients(MySqlContext context, Status status)
        {
            _logger.LogInformation("Total clients found {0}", status.Clients.Count);
            foreach (var client in status.Clients)
            {
                var userId = GetUserId(context, client.CommonName,
                    (client.VirtualAddress ?? client.VirtualIPv6Address)?.ToString()!);
                var connected = client.ConnectedSince.ToLocalTime();
                var session = context.Sessions.FirstOrDefault(x => x.UserId == userId && x.SessionId == client.ClientID && x.Connected == connected);
                if (session == null)
                {
                    _logger.LogTrace("Creating a new session for userId {0}, sessionId {1}", userId, client.ClientID);
                    session = new Session
                    {
                        UserId = userId,
                        SessionId = client.ClientID,
                        Connected = connected,
                        IPAddress = TryTranslate(client.RealAddress),
                        BytesIn = client.BytesSent,
                        BytesOut = client.BytesReceived,
                        LastUpdated = DateTime.Now,
                    };
                    context.Sessions.Add(session);
                    context.SaveChanges();
                }
                else
                {
                    _logger.LogTrace("Updating existing session {0} for userId {1}", client.ClientID, userId);
                    session.BytesIn = client.BytesSent;
                    session.BytesOut = client.BytesReceived;
                    session.LastUpdated = DateTime.Now;
                }

                _sessionsByClientId[client.ClientID] = session;
            }

            context.SaveChanges();
        }

        private int GetUserId(MySqlContext ctx, string commonName, string ipAddress)
        {
            if (!_userIdByCommonName.TryGetValue(commonName, out var userId))
            {
                var user = new User
                {
                    CommonName = commonName,
                    IPAddress = ipAddress,
                };
                ctx.Users.Add(user);
                ctx.SaveChanges();
                userId = user.Id;
                _userIdByCommonName[commonName] = userId;
            }

            return userId;
        }

        private string TryTranslate(IPEndPoint address)
        {
            var translated = _translator.Translate(address);

            return translated == null
                ? "[Proxy]"
                : Equals(translated, address)
                    ? address.Address.ToString()
                    : translated.Address + " [P]";
        }
    }
}
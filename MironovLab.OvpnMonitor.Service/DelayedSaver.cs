using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MironovLab.OvpnMonitor.Service.Configuration;
using MironovLab.OvpnMonitor.Service.DbConnection;

namespace MironovLab.OvpnMonitor.Service
{
    internal class DelayedSaver : IDisposable
    {
        private static readonly TimeSpan Delay = TimeSpan.FromSeconds(5);
        private readonly string _connectionString;
        private readonly Timer _timer;
        private readonly Dictionary<int, Session> _sessions = new();
        private readonly ILogger _logger;
        private bool _timerActive;

        public DelayedSaver(IOptions<MySqlConfiguration> mysqlConfig, ILogger<DelayedSaver> logger)
        {
            _connectionString = mysqlConfig.Value.ToString() ?? throw new ArgumentNullException(nameof(mysqlConfig));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timer = new Timer(OnTimer, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
        public void Dispose()
        {
            _timer.Dispose();
        }

        public void Add(Session session)
        {
            lock (_timer)
            {
                _sessions[session.SessionId] = session;
                if (!_timerActive)
                {
                    if (_timer.Change(Delay, Timeout.InfiniteTimeSpan))
                    {
                        _timerActive = true;
                    }
                    else
                    {
                        _logger.LogError("Could not schedule the timer");
                    }
                }
            }
        }

        public void Save()
        {
            OnTimer(null!);
        }

        private void OnTimer(object _)
        {
            var sessions = new List<Session>();
            lock (_timer)
            {
                sessions.AddRange(_sessions.Values);
                _sessions.Clear();
                _timerActive = false;
            }

            try
            {
                using (var ctx = new MySqlContext(_connectionString))
                {
                    foreach (var session in sessions)
                    {
                        ctx.Sessions.Attach(session);
                        ctx.Entry(session).State = EntityState.Modified;
                    }

                    ctx.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(OnTimer));
                lock (_timer)
                {
                    foreach (var session in sessions)
                    {
                        _sessions[session.SessionId] = session;
                    }
                }
            }
        }
    }
}

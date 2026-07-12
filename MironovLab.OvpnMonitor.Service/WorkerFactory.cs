using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MironovLab.OvpnMonitor.Service.AddressTranslation;
using MironovLab.OvpnMonitor.Service.Configuration;

namespace MironovLab.OvpnMonitor.Service
{
    internal class WorkerFactory(
        ILoggerFactory loggerFactory,
        TranslationService translator,
        IOptions<AppConfiguration> config,
        IOptions<MySqlConfiguration> mysqlConfig
        ) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var servers = config.Value.OpenVPNConfigurations;
            var tasks = new List<ValueTuple<Worker, Task>>(servers.Length);
            foreach (var server in servers)
            {
                var worker = new Worker(
                    loggerFactory,
                    translator,
                    server,
                    mysqlConfig);
                var task = worker.ExecuteAsync(stoppingToken);
                tasks.Add((worker, task));
            }

            try
            {
                await Task.WhenAll(tasks.Select(x => x.Item2));
            }
            finally
            {
                tasks.ForEach(x => x.Item1.Dispose());
            }
        }
    }
}

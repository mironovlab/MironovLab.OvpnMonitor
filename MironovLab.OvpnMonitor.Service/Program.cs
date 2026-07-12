using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MironovLab.OvpnMonitor.Service.AddressTranslation;
using MironovLab.OvpnMonitor.Service.Configuration;

namespace MironovLab.OvpnMonitor.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var appConfig = hostContext.Configuration.GetSection(nameof(AppConfiguration));
                    services.Configure<AppConfiguration>(appConfig);
                    services.Configure<MySqlConfiguration>(appConfig.GetSection(nameof(MySqlConfiguration)));
                    services.Configure<AddressTranslatorConfiguration>(appConfig.GetSection(nameof(AddressTranslatorConfiguration)));
                    services.AddSingleton<TranslationService>();
                    services.AddSingleton<IAddressResolver>(sp => sp.GetService<TranslationService>());
                    services.AddHostedService(sp => sp.GetService<TranslationService>());
                    services.AddHostedService<WorkerFactory>();
                })
                .UseConsoleLifetime()
                .UseSystemd();
    }
}

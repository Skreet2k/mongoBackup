using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MongoBackupService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureHostConfiguration(config =>
                {
                    config.AddJsonFile("appsettings.json", true, true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions<BackupOptions>()
                        .Bind(hostContext.Configuration.GetSection("Backup"))
                        .Validate(c => !string.IsNullOrEmpty(c.Expression));
                    services.AddHangfire(x => x.UseMemoryStorage());
                    services.AddHangfireServer();
                    services.AddHostedService<Worker>();
                });
        }
    }
}
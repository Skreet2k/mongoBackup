using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using SFTP.Wrapper;
using SFTP.Wrapper.Configs;

namespace MongoBackupService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
            CreateHostBuilder(args).RunConsoleAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(config =>
                {
                    config.AddJsonFile("appsettings.json", true, true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions<BackupOptions>()
                        .Bind(hostContext.Configuration.GetSection("Backup"))
                        .Validate(c => c.IsValid());
                    services.AddSingleton<BackupWorker>();
                    services.AddOptions<SftpConfig>().Bind(hostContext.Configuration.GetSection("Sftp"))
                        .Validate(c => c.IsValid());
                    services.UseSftp(x => x.GetRequiredService<IOptions<SftpConfig>>().Value);
                    services.AddHostedService(x => x.GetService<BackupWorker>());
                })
                .UseSerilog();
        }
    }
}
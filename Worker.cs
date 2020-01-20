using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Annotations;
using Hangfire.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MongoBackupService
{
    [UsedImplicitly]
    public class Worker : BackgroundService
    {
        private readonly BackupOptions _options;
        private readonly ILogger<Worker> _logger;


        public Worker(IOptions<BackupOptions> options, ILogger<Worker> logger)
        {
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            RecurringJob.AddOrUpdate(() => CreateBackupJob(), _options.Expression);
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        [UsedImplicitly]
        // ReSharper disable once MemberCanBePrivate.Global
        public void CreateBackupJob()
        {
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = false,
                UseShellExecute = false,
                FileName = "mongodump.exe",
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = $"--host \"{_options.Host}\" --db \"{_options.BackupDatabase}\" --out \"{_options.OutFolder}/{DateTime.Now:yyyy-MM-dd HH-mm}\""
            };

            if (!string.IsNullOrWhiteSpace(_options.AuthDatabase))
            {
                startInfo.Arguments +=
                    $" --authenticationDatabase {_options.AuthDatabase} -u {_options.User} -p {_options.Password}";
            }

            try
            {
                using (var exeProcess = Process.Start(startInfo))
                {
                    exeProcess?.WaitForExit();
                }
            }
            catch(Exception e)
            {
                _logger.LogError(e, "Error occured while try to start backup");
            }
        }
    }
}
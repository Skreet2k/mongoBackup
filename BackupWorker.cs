using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFTP.Wrapper;
using SFTP.Wrapper.Requests;

namespace MongoBackupService
{
    public class BackupWorker : IHostedService
    {
        private readonly string _fileName = $"{DateTime.Now:yyyy-MM-dd HH-mm}";
        private readonly ILogger<BackupWorker> _logger;
        private readonly BackupOptions _options;
        private readonly ISftpManager _sftpManager;

        public BackupWorker(IOptions<BackupOptions> options, ILogger<BackupWorker> logger, ISftpManager sftpManager)
        {
            _logger = logger;
            _sftpManager = sftpManager;
            _options = options.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Backup Job started");
            Create();
            await Upload();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Backup Job finished");
            return Task.CompletedTask;
        }

        private void Create()
        {
            var mongoArg = $"mongodump --host '{_options.Host}' --db '{_options.BackupDatabase}' --archive='{_fileName}' --gzip";
            
            if (!string.IsNullOrWhiteSpace(_options.AuthDatabase))
                mongoArg +=
                    $" --authenticationDatabase {_options.AuthDatabase} -u {_options.User} -p {_options.Password}";
            
            var escapedArgs = mongoArg.Replace("\"", "\\\"");
            
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = "/bin/sh",
                RedirectStandardOutput = true,
                Arguments = $"-c \"{escapedArgs}\""
            };
            
            _logger.LogInformation($"Run '{startInfo.FileName}' with arguments '{startInfo.Arguments}'");

            try
            {
                using (var exeProcess = Process.Start(startInfo))
                {
                    exeProcess?.WaitForExit();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error occured while try to start backup");
                throw;
            }
        }

        private async Task Upload()
        {
            _logger.LogInformation("Upload backup started");

            using var fs = new FileStream(_fileName, FileMode.Open);
            var result = await _sftpManager.UploadFileAsync(new UploadFileRequest(fs, _options.OutFolder, _fileName));
            if (!result.Status)
                _logger.LogError($"Error occured while try upload file by ssh '{result.Message}', '{result.Data}'", result.Exception);
            else
                _logger.LogInformation("Upload backup finished");
        }
    }
}
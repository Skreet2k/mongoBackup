namespace MongoBackupService
{
    public class BackupOptions
    {
        public string Expression { get; set; }
        public string BackupDatabase { get; set; }
        public string Host { get; set; }
        public string OutFolder { get; set; }
        public string AuthDatabase { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
    }
}
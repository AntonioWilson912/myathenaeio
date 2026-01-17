using MyAthenaeio.Data;
using System.IO;
using Serilog;

namespace MyAthenaeio.Services
{
    public class BackupService
    {
        private static readonly ILogger _logger = Log.ForContext<BackupService>();

        private static readonly string BackupFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "myAthenaeio", "Backups"
        );

        public static async Task CheckAndCreateAutomaticBackupAsync()
        {
            try
            {
                Directory.CreateDirectory(BackupFolder);

                var lastBackup = GetMostRecentBackup();

                if (lastBackup == null || File.GetCreationTime(lastBackup) < DateTime.Now.AddDays(-7))
                {
                    _logger.Information("Creating automatic backup (last backup: {LastBackup})",
                        lastBackup == null ? "never" : File.GetCreationTime(lastBackup).ToString("yyyy-MM-dd"));
                    await CreateAutomaticBackupAsync();
                }
                else
                {
                    _logger.Debug("Automatic backup not needed, last backup: {LastBackup}",
                        File.GetCreationTime(lastBackup).ToString("yyyy-MM-dd"));
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed during automatic backup check/creation");
            }
        }

        public static List<string> GetAvailableBackups()
        {
            if (!Directory.Exists(BackupFolder))
                return [];

            return [.. Directory.GetFiles(BackupFolder, "library_backup_*.db").OrderByDescending(f => File.GetCreationTime(f))];
        }

        private static async Task CreateAutomaticBackupAsync()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFilePath = Path.Combine(BackupFolder, $"library_backup_{timestamp}.db");

            try
            {
                File.Copy(AppDbContext.DbPath, backupFilePath);
                CleanupOldBackups(keepCount: 5);
                _logger.Information("Automatic backup created: {BackupPath}", backupFilePath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to create automatic backup at {BackupPath}", backupFilePath);
                throw;
            }
        }

        private static string? GetMostRecentBackup()
        {
            var backupFiles = Directory.GetFiles(BackupFolder, "library_backup_*.db");
            return backupFiles
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .FirstOrDefault()?.FullName;
        }

        private static void CleanupOldBackups(int keepCount)
        {
            var backupFiles = Directory.GetFiles(BackupFolder, "library_backup_*.db")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .Skip(keepCount)
                .ToList();

            foreach (var oldBackup in backupFiles)
            {
                try
                {
                    oldBackup.Delete();
                    _logger.Debug("Deleted old backup: {BackupPath}", oldBackup.FullName);
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to delete old backup: {BackupPath}", oldBackup.FullName);
                }
            }
        }
    }
}

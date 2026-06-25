using CreditCardRewards.DataRefresh.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CreditCardRewards.DataRefresh.Services
{
    public class StatementFolderWatcher : IHostedService, IDisposable
    {
        private readonly IStatementParserService _parser;
        private readonly IConfiguration _config;
        private readonly ILogger<StatementFolderWatcher> _logger;
        private FileSystemWatcher? _watcher;
        private readonly HashSet<string> _processing = new();

        private static readonly string[] SupportedExtensions = [".pdf", ".csv"];

        public StatementFolderWatcher(
            IStatementParserService parser,
            IConfiguration config,
            ILogger<StatementFolderWatcher> logger)
        {
            _parser = parser;
            _config = config;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var folder = _config["Statements:WatchFolder"] ?? "statements";
            var absoluteFolder = Path.IsPathRooted(folder)
                ? folder
                : Path.Combine(AppContext.BaseDirectory, folder);

            Directory.CreateDirectory(absoluteFolder);
            Directory.CreateDirectory(Path.Combine(absoluteFolder, "processed"));

            _watcher = new FileSystemWatcher(absoluteFolder)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                Filter = "*.*",
                EnableRaisingEvents = true
            };

            _watcher.Created += OnFileCreated;
            _watcher.Renamed += OnFileRenamed;

            _logger.LogInformation("Watching for statements in: {Folder}", absoluteFolder);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _watcher?.Dispose();
            return Task.CompletedTask;
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e) => ProcessFile(e.FullPath);
        private void OnFileRenamed(object sender, RenamedEventArgs e) => ProcessFile(e.FullPath);

        private void ProcessFile(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (!SupportedExtensions.Contains(ext)) return;
            if (path.Contains("processed")) return;

            lock (_processing)
            {
                if (!_processing.Add(path)) return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    // Brief delay to ensure file is fully written
                    await Task.Delay(1500);
                    _logger.LogInformation("Parsing statement: {File}", Path.GetFileName(path));
                    await _parser.ParseAsync(path);
                    _logger.LogInformation("Statement parsed and queued for review: {File}", Path.GetFileName(path));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process statement file: {File}", path);
                }
                finally
                {
                    lock (_processing) { _processing.Remove(path); }
                }
            });
        }

        public void Dispose() => _watcher?.Dispose();
    }
}

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace OCM.Web.Services
{
    public class ImportBackgroundService : BackgroundService
    {
        private readonly IImportQueueService _importQueueService;
        private readonly ILogger<ImportBackgroundService> _logger;

        public ImportBackgroundService(IImportQueueService importQueueService, ILogger<ImportBackgroundService> logger)
        {
            _importQueueService = importQueueService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Agreement import background service started.");
            await _importQueueService.ProcessQueueAsync(stoppingToken);
        }
    }
}

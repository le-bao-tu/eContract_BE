using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetCore.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NetCore.WorkerService
{
    public class Worker : BackgroundService
    {
        private Task _executingTask;
        private CancellationTokenSource _stoppingCts;
        private readonly ILogger<Worker> _logger;
        private INotifyConfig _notifyConfig;
        private readonly IServiceProvider _serviceProvider;

        public int timeRepeat = 1800000;
        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            int.TryParse(Utils.GetConfig("timeRepeat"), out timeRepeat);
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _executingTask = ExecuteAsync(_stoppingCts.Token);

            if (_executingTask.IsCompleted)
                return _executingTask;

            return Task.CompletedTask;
        }

        public async override Task StopAsync(CancellationToken cancellationToken)
        {
            if (_executingTask == null)
                return;

            _stoppingCts.Cancel();
            await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await DoWorkAsync();
                await Task.Delay(timeRepeat, stoppingToken);
            }
        }

        private async Task DoWorkAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                _notifyConfig = scope.ServiceProvider.GetRequiredService<INotifyConfig>();

                #region Gửi thông báo nhắc nhở
                _notifyConfig.SendNotifyRemind().Wait();
                _notifyConfig.SendNotifyExpire().Wait();
                #endregion

                #region Gửi thông báo hết hạn ký
                //_notifyConfig.SendEmailExpire();
                //_notifyConfig.SendNotifyExpire();
                //_notifyConfig.SendSMSExpire();

                //_notifyConfig.SendSMSExpiredAsync();
                //_notifyConfig.SendEmailExpiredAsync();
                //_notifyConfig.SendNotifyExpiredAsync();
                #endregion
            }
        }
    }
}

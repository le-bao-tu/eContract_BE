using NetCore.Business;
using NetCore.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetCore.Business.Core.CronJob
{
    public class DelaySendMail : CronJobService
    {
        public DelaySendMail(IScheduleConfig<DelaySendMail> config)
             : base(config.CronExpression, config.TimeZoneInfo)
        {
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Log.Information("Start Send Mail");
            return base.StartAsync(cancellationToken);
        }

        public override Task DoWork(CancellationToken cancellationToken)
        {
            Log.Information("Working Send Mail");
            try
            {
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Log.Error("Có lỗi xảy ra: ", ex);
                return Task.CompletedTask;
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Log.Information("Stop Send Mail");
            return base.StopAsync(cancellationToken);
        }
    }
}

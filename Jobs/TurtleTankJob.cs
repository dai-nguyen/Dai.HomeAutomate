using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using System.Device.Gpio;

namespace Dai.HomeAutomate.Jobs
{
    public class TurtleTankJob : IJob
    {
        readonly ILogger _logger;

        //pin 26, 20, 21
        public static readonly JobKey Key = new JobKey("turtle-tank-job", "backyard");        
        TimeZoneInfo _pacificZone = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");

        public TurtleTankJob(
            ILogger<TurtleTankJob> log)
        {
            _logger = log;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("{0} has started", GetType().Name);
            try
            {
                JobDataMap dataMap = context.MergedJobDataMap;
                bool on = dataMap.GetBoolean("on");
                _logger.LogInformation("turn on {0}", on); 

                int pin = 26;
                using (var controller = new GpioController())
                {
                   controller.OpenPin(pin, PinMode.Output);

                   if (on)
                   {
                       _logger.LogInformation("Turn on pin {0}", pin);
                       controller.Write(pin, PinValue.High);
                   }
                   else
                   {
                       _logger.LogInformation("Turn off pin {0}", pin);
                       controller.Write(pin, PinValue.Low);
                   }                    
                }

                if (on)
                {
                    _logger.LogInformation($"Schedule next {Key.Name} to turn off in 5 minutes");

                    var newJob = new JobDetailImpl(Key.Name, typeof(TurtleTankJob))
                    {
                        JobDataMap = new JobDataMap() { { "on", false } }
                    };

                    var oldTrigger = context.Trigger;
                    var newTrigger = TriggerBuilder.Create()
                        .WithIdentity($"{oldTrigger.Key.Name}-off", oldTrigger.Key.Group)                                                
                        .StartAt(DateTimeOffset.UtcNow.AddMinutes(5))
                        .Build();

                    await context.Scheduler.ScheduleJob(newJob, newTrigger);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                _logger.LogInformation("{0} has ended", GetType().Name);
            }
            await Task.FromResult(0);
        }
    }
}

using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace TimerFunctionDemo;

public class MyTimerFunction
{
    private readonly ILogger _logger;

    public MyTimerFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<MyTimerFunction>();
    }

    [Function("MyTimerFunction")]
    public void Run([TimerTrigger("*/5 * * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {executionTime}", DateTime.Now);

        if (myTimer.IsPastDue)
        {
            _logger.LogWarning("The timer is running late!");
        }

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
        }
    }
}
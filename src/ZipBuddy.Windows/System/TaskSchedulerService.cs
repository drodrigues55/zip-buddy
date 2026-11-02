namespace ZipBuddy.Windows.System;

[Obsolete("Use ZipBuddy.Infrastructure.Services.TaskSchedulerService instead.")]
public class TaskSchedulerService : ZipBuddy.Infrastructure.Services.TaskSchedulerService
{
    public TaskSchedulerService(ZipBuddy.Core.Interfaces.ILoggingService? loggingService = null) : base(loggingService) { }
}

using System;

namespace PCL.Core.Lifecycle;

public record LifecycleLogItem(
    ILifecycleService Source,
    string Message,
    Exception? Ex = null,
    LifecycleLogLevel Level = LifecycleLogLevel.Trace,
    LifecycleActionLevel? ActionLevel = null
);

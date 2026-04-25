#if NET9_0_OR_GREATER

using System.Collections.Concurrent;

namespace JustGo.Authentication.Helper;

public static class LongRunningTasks
{
    public static ConcurrentDictionary<string, CancellationTokenSource> OperationIds = new();
}

#endif
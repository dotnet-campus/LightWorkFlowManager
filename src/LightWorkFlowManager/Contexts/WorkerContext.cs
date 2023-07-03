using System;
using System.Collections.Concurrent;

namespace LightWorkFlowManager.Contexts;

public class WorkerContext : IWorkerContext
{
    [System.Diagnostics.DebuggerStepThrough]
    public T? GetContext<T>()
    {
        if (_contextDictionary.TryGetValue(typeof(T), out var value))
        {
            return (T?) value;
        }

        return default;
    }

    public void SetContext<T>(T context)
    {
        _contextDictionary[typeof(T)] = context;
    }

    private readonly ConcurrentDictionary<Type, object?> _contextDictionary = new ConcurrentDictionary<Type, object?>();
}

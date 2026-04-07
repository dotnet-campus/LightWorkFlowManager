using System;
using System.Collections.Concurrent;

namespace DC.LightWorkFlowManager.Contexts;

/// <summary>
/// 提供基于类型索引的工作器上下文实现。
/// </summary>
public class WorkerContext : IWorkerContext
{
    /// <inheritdoc />
    [System.Diagnostics.DebuggerStepThrough]
    public T? GetContext<T>()
    {
        if (_contextDictionary.TryGetValue(typeof(T), out var value))
        {
            return (T?) value;
        }

        return default;
    }

    /// <inheritdoc />
    public void SetContext<T>(T context)
    {
        _contextDictionary[typeof(T)] = context;
    }

    private readonly ConcurrentDictionary<Type, object?> _contextDictionary = new ConcurrentDictionary<Type, object?>();
}

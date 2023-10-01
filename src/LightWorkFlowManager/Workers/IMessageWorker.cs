using System;
using System.Threading.Tasks;
using DC.LightWorkFlowManager.Contexts;
using DC.LightWorkFlowManager.Protocols;

namespace DC.LightWorkFlowManager.Workers;

/// <summary>
/// 工作器
/// </summary>
public interface IMessageWorker
{
    string TaskId { get; set; }

    /// <summary>
    /// 工作器名，调试和上报时使用
    /// </summary>
    string WorkerName { get; }

    /// <summary>
    /// 是否可以重试
    /// </summary>
    bool CanRetry { get; }

    /// <summary>
    /// 重试的等待时间
    /// </summary>
    TimeSpan RetryDelayTime { get; }

    ///// <summary>
    ///// 是否需要重试
    ///// </summary>
    ///// <returns></returns>
    //bool NeedRetry();

    /// <summary>
    /// 遇到失败时，是否能执行
    /// </summary>
    /// <returns></returns>
    bool CanRunWhenFail { get; }

    ValueTask<WorkerResult> Do(IWorkerContext context);

    ValueTask OnDisposeAsync(IWorkerContext context);
}

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
    /// <summary>
    /// 获取或设置当前任务标识。
    /// </summary>
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
    bool CanRunWhenFail { get; }

    /// <summary>
    /// 执行当前工作器。
    /// </summary>
    /// <param name="context">工作器上下文。</param>
    /// <returns>工作器执行结果。</returns>
    ValueTask<WorkerResult> Do(IWorkerContext context);

    /// <summary>
    /// 在工作器被释放时执行清理逻辑。
    /// </summary>
    /// <param name="context">工作器上下文。</param>
    /// <returns>表示异步清理操作的任务。</returns>
    ValueTask OnDisposeAsync(IWorkerContext context);
}
